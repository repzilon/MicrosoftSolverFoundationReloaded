using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// This class implements methods for checking that an OML model is valid.
	///
	/// "Valid" in this case means that only a restricted set of operators are
	/// used, and that if each parameter and decision was assigned a value from
	/// the appropriate domain, all the goals and constraints would reduce to
	/// simple values (numbers for goals, booleans for constraints).
	///
	/// The validator is conservative, in that it only accepts models
	/// where it can prove the above two conditions are true.
	///
	/// Note that in some cases, applying standard reductions to an invalid model
	/// can produce a valid model.
	/// </summary>
	internal class OmlValidator
	{
		private struct ArgumentRestriction
		{
			public enum RestrictionType
			{
				Exactly,
				Atleast,
				None
			}

			public int NumberOfArgs;

			public RestrictionType RestType;

			public ArgumentRestriction(RestrictionType restType, int numberOfArgs)
			{
				RestType = restType;
				NumberOfArgs = numberOfArgs;
			}
		}

		private class DecisionParameterInfo
		{
			public TermValueClass DecisionType;

			public bool IsFullySpecifiedParameter;

			public bool IsSets;

			public bool IsTuples;

			public bool IsScenarios;

			public TermValueClass[] TupleTypes;

			public DecisionParameterInfo()
			{
				DecisionType = TermValueClass.Any;
			}
		}

		internal struct TermType
		{
			internal readonly bool _dataDependent;

			internal readonly bool _decisionDependent;

			internal readonly bool _multiValue;

			internal readonly TermValueClass[] _valueClass;

			internal TermType(bool decisionDependent, bool dataDependent, bool multiValue, params TermValueClass[] valueClass)
			{
				_decisionDependent = decisionDependent;
				_dataDependent = dataDependent;
				_multiValue = multiValue;
				_valueClass = valueClass;
			}
		}

		private enum TokenType
		{
			Decision,
			DecisionDomain,
			Parameter,
			ParameterDomain,
			AssignmentParameter,
			AssignmentParameterBase
		}

		internal Dictionary<Symbol, int> _decisionsSymbols = new Dictionary<Symbol, int>();

		private int _depth;

		private Dictionary<Expression, TermValueClass> _domainSymbols = new Dictionary<Expression, TermValueClass>();

		private Dictionary<Expression, TermType> _iteratorBindings = new Dictionary<Expression, TermType>();

		private HashSet<Symbol> _observedSymbols = new HashSet<Symbol>();

		private Dictionary<Expression, TermType> _sets = new Dictionary<Expression, TermType>();

		private Dictionary<Expression, TermType> _symbols = new Dictionary<Expression, TermType>();

		private Dictionary<Expression, TermValueClass[]> _tuples = new Dictionary<Expression, TermValueClass[]>();

		internal ConcreteModel Model { get; set; }

		internal SolveRewriteSystem Rewrite => Model.Rewrite;

		internal OmlValidator(ConcreteModel model)
		{
			Model = model;
		}

		internal bool IsReservedSymbol(Symbol sym)
		{
			return Model.IsReservedSymbol(sym);
		}

		private void ValidateRuleSymbol(Expression expr, Invocation closestInvocation, bool allowStringConstant)
		{
			if (!allowStringConstant || !(expr is StringConstant))
			{
				Symbol symbol = expr as Symbol;
				if (symbol == null)
				{
					MakeModelClauseException(Resources.OmlInvalidSubmodelGoalConstraintName, expr, closestInvocation, OmlParseExceptionReason.InvalidName);
				}
				ForbidDuplicatedSymbol(symbol, closestInvocation);
			}
		}

		private void ForbidDuplicatedSymbol(Symbol symbol, Invocation closestInvocation)
		{
			if (_observedSymbols.Contains(symbol))
			{
				string strMsg = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidDefiningDuplicatedSymbols, new object[1] { symbol.ToString() });
				throw new ModelClauseException(strMsg, closestInvocation, OmlParseExceptionReason.DuplicateName);
			}
			_observedSymbols.Add(symbol);
		}

		/// <summary>
		/// This is used when the string of error is the same no matter if we use the 
		/// Expression itself or the closest Invocation
		/// </summary>
		/// <param name="errorMessage"></param>
		/// <param name="errorExp"></param>
		/// <param name="closestInvocation"></param>
		/// <param name="reason">The reason for the exception.</param>
		internal static void MakeModelClauseException(string errorMessage, Expression errorExp, Invocation closestInvocation, OmlParseExceptionReason reason)
		{
			if (errorExp is Symbol)
			{
				throw new ModelClauseException(errorMessage, closestInvocation, reason);
			}
			throw new ModelClauseException(errorMessage, errorExp, reason);
		}

		/// <summary>
		/// get different format strings for case that the errored Expression is Symbol,
		/// so it doesn't have a reliable line info, and if it is not
		/// </summary>
		/// <param name="errorForNonSymExpression">error message for the case the Expression itself is sent</param>
		/// <param name="formatErrorForSymExpression">used to get the symbol into the error when the Expression sent to the exception is the Invocation</param>
		/// <param name="errorExp">wrong Expression</param>
		/// <param name="closestInvocation">clesest Invocation for the wrong Expression</param>
		internal static void MakeModelClauseException(string errorForNonSymExpression, string formatErrorForSymExpression, Expression errorExp, Invocation closestInvocation)
		{
			string strMsg;
			if (errorExp is Symbol)
			{
				strMsg = string.Format(CultureInfo.InvariantCulture, formatErrorForSymExpression, new object[1] { errorExp.ToString() });
				throw new ModelClauseException(strMsg, closestInvocation);
			}
			strMsg = string.Format(CultureInfo.InvariantCulture, errorForNonSymExpression);
			throw new ModelClauseException(errorForNonSymExpression, errorExp);
		}

		/// <summary>
		/// Check a Parameters[...] section.
		/// </summary>
		/// <param name="sectionInv"></param>
		internal void AnalyzeParametersSection(Invocation sectionInv)
		{
			AnalyzeDecisionsOrParametersSection(sectionInv, TokenType.Parameter);
		}

		/// <summary>
		/// Check a Decisions[...] section.
		/// </summary>
		internal void AnalyzeDecisionsSection(Invocation sectionInv)
		{
			AnalyzeDecisionsOrParametersSection(sectionInv, TokenType.Decision);
		}

		internal void AnalyzeSubmodelSection(Invocation sectionInv)
		{
			if (sectionInv.Head != Rewrite.Builtin.Rule)
			{
				MakeModelClauseException(Resources.OmlInvalidSubmodelClause, sectionInv, sectionInv, OmlParseExceptionReason.SubmodelError);
			}
			if (sectionInv.Arity != 2)
			{
				MakeModelClauseException(Resources.OmlInvalidSubmodelClause, sectionInv, sectionInv, OmlParseExceptionReason.SubmodelError);
			}
			Expression expression = sectionInv[0];
			ValidateRuleSymbol(expression, sectionInv, allowStringConstant: false);
			if (_domainSymbols.ContainsKey(expression))
			{
				MakeModelClauseException(Resources.OmlInvalidSubmodelNameIsAlsoDeclaredAsANamedDomain, expression, sectionInv, OmlParseExceptionReason.DuplicateName);
			}
		}

		internal void AnalyzeDomainsSection(Invocation sectionInv)
		{
			if (sectionInv.Arity != 2)
			{
				MakeModelClauseException(Resources.DomainsSectionMustContainADomainAndAName, sectionInv, sectionInv, OmlParseExceptionReason.InvalidDomain);
			}
			Expression expr = sectionInv[0];
			Expression expression = sectionInv[1];
			Symbol symbol = expression as Symbol;
			TermValueClass value = AnalyzeDomain(expr, sectionInv, allowDomainWithDataDependent: false);
			if (symbol == null)
			{
				MakeModelClauseException(Resources.DomainsSectionMustContainADomainAndAName, expression, sectionInv, OmlParseExceptionReason.InvalidDomain);
			}
			if (_domainSymbols.ContainsKey(expression))
			{
				MakeModelClauseException(Resources.NameIsAlreadyUsedAsADomain, expression, sectionInv, OmlParseExceptionReason.DuplicateName);
			}
			ForbidDuplicatedSymbol(symbol, sectionInv);
			_domainSymbols.Add(expression, value);
		}

		/// <summary>
		/// Check a Decisions[...] or Parameters[...] section. The two types have the same basic structure, so most of the code is shared.
		/// </summary>
		private void AnalyzeDecisionsOrParametersSection(Invocation sectionInv, TokenType sectionType)
		{
			bool flag = true;
			DecisionParameterInfo info = new DecisionParameterInfo();
			foreach (Expression arg in sectionInv.Args)
			{
				TokenType tokenType = sectionType;
				if (flag && tokenType == TokenType.Decision)
				{
					tokenType = TokenType.DecisionDomain;
				}
				if (flag && tokenType == TokenType.Parameter)
				{
					tokenType = TokenType.ParameterDomain;
				}
				AnalyzeDecisionsOrParametersSectionElement(tokenType, info, arg, sectionInv);
				flag = false;
			}
		}

		/// <summary>
		/// Check a Constraints[...] section.
		/// </summary>
		/// <param name="inv"></param>
		internal void AnalyzeConstraintsSection(Invocation inv)
		{
			foreach (Expression arg in inv.Args)
			{
				if (arg is Invocation closestInvocation && arg.Head is RuleSymbol)
				{
					ValidateRuleSymbol(arg[0], inv, allowStringConstant: true);
					AnalyzeConstraint(arg[1], closestInvocation);
				}
				else
				{
					AnalyzeConstraint(arg, inv);
				}
			}
		}

		private void AnalyzeConstraint(Expression arg, Invocation closestInvocation)
		{
			while (arg.Head == Rewrite.Builtin.Annotation)
			{
				arg = arg[0];
			}
			AnalyzeExpr(arg, closestInvocation);
		}

		/// <summary>
		/// Check a Minimize[...] or Maximize[...] section.
		/// </summary>
		/// <param name="inv"></param>
		internal void AnalyzeGoalsSection(Invocation inv)
		{
			foreach (Expression arg in inv.Args)
			{
				if (arg is Invocation closestInvocation && arg.Head is RuleSymbol)
				{
					ValidateRuleSymbol(arg[0], inv, allowStringConstant: true);
					AnalyzeGoal(arg[1], closestInvocation);
				}
				else
				{
					AnalyzeGoal(arg, inv);
				}
			}
		}

		private void AnalyzeGoal(Expression arg, Invocation closestInvocation)
		{
			while (arg.Head == Rewrite.Builtin.Annotation)
			{
				arg = arg[0];
			}
			if (AnalyzeExpr(arg, closestInvocation)._valueClass[0] != 0)
			{
				MakeModelClauseException(Resources.OmlInvalidGoalNotNumeric, Resources.OmlInvalidGoalNotNumeric0, arg, closestInvocation);
			}
		}

		/// <summary>
		/// Check a single element of a Decisions[...] or Parameters[...] section.
		/// This encompasses the EBNF grammar rules for {domain}, {set_domain}, {decision}, {parameter}, and {assignment_parameter}.
		/// </summary>
		private void AnalyzeDecisionsOrParametersSectionElement(TokenType tokenType, DecisionParameterInfo info, Expression element, Invocation closestInvocation)
		{
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool allowDomainWithDataDependent = false;
			while (element.Head == Rewrite.Builtin.Annotation)
			{
				element = element[0];
			}
			TokenType tokenType3;
			TokenType tokenType2;
			switch (tokenType)
			{
			case TokenType.DecisionDomain:
				flag = true;
				tokenType3 = TokenType.Decision;
				tokenType2 = TokenType.Decision;
				allowDomainWithDataDependent = true;
				break;
			case TokenType.Decision:
				tokenType3 = TokenType.Decision;
				tokenType2 = TokenType.Decision;
				allowDomainWithDataDependent = true;
				break;
			case TokenType.ParameterDomain:
				flag = true;
				flag2 = true;
				flag3 = true;
				tokenType3 = TokenType.Parameter;
				tokenType2 = TokenType.Parameter;
				break;
			case TokenType.Parameter:
				flag2 = true;
				flag4 = true;
				tokenType3 = TokenType.Parameter;
				tokenType2 = TokenType.AssignmentParameter;
				break;
			case TokenType.AssignmentParameter:
				flag4 = true;
				tokenType3 = TokenType.AssignmentParameter;
				tokenType2 = TokenType.AssignmentParameter;
				break;
			default:
				tokenType3 = (tokenType2 = tokenType);
				break;
			}
			Invocation invocation = element as Invocation;
			if (flag)
			{
				Symbol firstSymbolHead = element.FirstSymbolHead;
				if (firstSymbolHead == Rewrite.Builtin.Sets)
				{
					if (!flag2)
					{
						string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidSetsOutsideParametersSection, new object[1] { firstSymbolHead.ToString() });
						MakeModelClauseException(errorMessage, element, closestInvocation, OmlParseExceptionReason.InvalidSet);
					}
					AnalyzeSetSpecifier(ref info.IsSets, ref info.DecisionType, element, closestInvocation, allowDomainWithDataDependent);
					return;
				}
				if (firstSymbolHead == Rewrite.Builtin.Tuples)
				{
					if (!flag3)
					{
						string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidSetsOutsideParametersSection, new object[1] { firstSymbolHead.ToString() });
						MakeModelClauseException(errorMessage, element, closestInvocation, OmlParseExceptionReason.InvalidSet);
					}
					if (element.Arity < 1)
					{
						MakeModelClauseException(Resources.TuplesMustIncludeAtLeastOneElement, element, closestInvocation, OmlParseExceptionReason.InvalidTuples);
					}
					info.TupleTypes = new TermValueClass[element.Arity];
					for (int i = 0; i < element.Arity; i++)
					{
						if (!_domainSymbols.TryGetValue(element[i], out info.TupleTypes[i]))
						{
							info.TupleTypes[i] = AnalyzeDomain(element[i], invocation, allowDomainWithDataDependent: false);
						}
					}
					info.IsTuples = true;
					return;
				}
				if (firstSymbolHead == Rewrite.Builtin.Scenarios)
				{
					if (!flag3)
					{
						string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidSetsOutsideParametersSection, new object[1] { firstSymbolHead.ToString() });
						MakeModelClauseException(errorMessage, element, closestInvocation, OmlParseExceptionReason.InvalidSet);
					}
					if (element.Arity != 1)
					{
						MakeModelClauseException(Resources.ScenariosMustIncludeOneElement, element, closestInvocation, OmlParseExceptionReason.InvalidArgumentCount);
					}
					info.TupleTypes = new TermValueClass[element.Arity];
					for (int j = 0; j < element.Arity; j++)
					{
						if (!_domainSymbols.TryGetValue(element[j], out info.TupleTypes[j]))
						{
							info.TupleTypes[j] = AnalyzeDomain(element[j], invocation, allowDomainWithDataDependent: false);
						}
					}
					info.IsScenarios = true;
					return;
				}
				if (firstSymbolHead == Rewrite.Builtin.Integers || firstSymbolHead == Rewrite.Builtin.Reals || firstSymbolHead == Rewrite.Builtin.Booleans || firstSymbolHead == Rewrite.Builtin.Enum || Model._mapSubmodelNameToConcreteModel.ContainsKey(firstSymbolHead))
				{
					AnalyzeDomainSpecifier(ref info.DecisionType, element, closestInvocation, allowDomainWithDataDependent);
					return;
				}
				if (firstSymbolHead is DistributionSymbol)
				{
					AnalyzeDistributionSpecifier(ref info.DecisionType, element, ref info.IsFullySpecifiedParameter, closestInvocation, allowDomainWithDataDependent);
					if (firstSymbolHead is BinomialDistributionSymbol && invocation != null && !(invocation[0] is IntegerConstant))
					{
						throw new ModelClauseException(Resources.FirstArgumentForBinomialDistributionMustBeInteger, invocation, OmlParseExceptionReason.InvalidArgumentType);
					}
					return;
				}
				if (_domainSymbols.TryGetValue(element, out info.DecisionType))
				{
					return;
				}
				MakeModelClauseException(Resources.OmlInvalidSectionShouldStartWithDomain, Resources.OmlInvalidSectionShouldStartWithDomain0, element, closestInvocation);
			}
			if (element is Symbol symbol)
			{
				if (info.IsSets)
				{
					AnalyzeSetBinding(info.DecisionType, symbol, closestInvocation);
				}
				else if (info.IsTuples)
				{
					_tuples[symbol] = info.TupleTypes;
				}
				else
				{
					AnalyzeDecisionOrParameterBinding(tokenType3, info.DecisionType, symbol, false, info.IsFullySpecifiedParameter, closestInvocation);
				}
				return;
			}
			Symbol symbol2 = element.Head as Symbol;
			if (invocation != null && symbol2 != null)
			{
				if (symbol2 is SetSymbol && flag4)
				{
					AnalyzeParameterAssignment(info, invocation);
				}
				else if (symbol2 is ForeachSymbol)
				{
					List<Expression> iterators = new List<Expression>();
					Expression element2 = BeginAnalyzeIteration(invocation, symbol2, hasCondition: false, iterators);
					AnalyzeDecisionsOrParametersSectionElement(tokenType2, info, element2, invocation);
					EndAnalyzeIteration(iterators);
				}
				else if (symbol2 is FilteredForeachSymbol)
				{
					if (flag4)
					{
						MakeModelClauseException("FilteredForeach is not allowed in a Parameters section", element, invocation, OmlParseExceptionReason.NotSpecified);
					}
					List<Expression> iterators2 = new List<Expression>();
					Expression element3 = BeginAnalyzeIteration(invocation, symbol2, hasCondition: true, iterators2);
					AnalyzeDecisionsOrParametersSectionElement(tokenType2, info, element3, invocation);
					EndAnalyzeIteration(iterators2);
				}
				else
				{
					if (info.IsSets)
					{
						string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidIndexedSetsParameter);
						throw new ModelClauseException(errorMessage, element, OmlParseExceptionReason.InvalidIndex);
					}
					AnalyzeDecisionOrParameterBinding(tokenType3, info.DecisionType, symbol2, isIndexed: true, info.IsFullySpecifiedParameter, invocation, invocation.ArgsArray);
				}
			}
			else
			{
				MakeModelClauseException(Resources.OmlInvalidInvalidElement, Resources.OmlInvalidInvalidElement0, element, closestInvocation);
			}
		}

		private void AnalyzeParameterAssignment(DecisionParameterInfo info, Invocation elementInv)
		{
			if (elementInv.Arity != 2)
			{
				string strMsg = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidInvalidAssignmentFormat);
				throw new ModelClauseException(strMsg, elementInv, OmlParseExceptionReason.InvalidArgumentCount);
			}
			Expression expression = elementInv[0];
			Expression expression2 = elementInv[1];
			Symbol symbol = expression as Symbol;
			Invocation invocation = expression as Invocation;
			Symbol symbol2 = expression.Head as Symbol;
			if (info.IsTuples)
			{
				ValidateTuplesData(elementInv, expression, symbol, expression2);
				_tuples[symbol] = info.TupleTypes;
				return;
			}
			if (info.IsScenarios)
			{
				ValidateTuplesData(elementInv, expression, symbol, expression2);
				_symbols.Add(symbol, new TermType(false, true, false, info.TupleTypes[0]));
				return;
			}
			if (info.IsSets)
			{
				TermType value = new TermType(false, true, false, info.DecisionType);
				_sets.Add(expression, value);
				return;
			}
			if (symbol != null)
			{
				AnalyzeDecisionOrParameterBinding(TokenType.AssignmentParameterBase, info.DecisionType, symbol, false, false, elementInv);
			}
			else
			{
				if (invocation == null || symbol2 == null)
				{
					string strMsg2 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidInvalidAssignmentFormat);
					throw new ModelClauseException(strMsg2, elementInv);
				}
				AnalyzeDecisionOrParameterBinding(TokenType.AssignmentParameterBase, info.DecisionType, symbol2, isIndexed: true, isFullySpecifiedParameter: false, invocation, invocation.ArgsArray);
			}
			TermType termType = AnalyzeExpr(expression2, elementInv);
			if (info.DecisionType == TermValueClass.Any || info.DecisionType == termType._valueClass[0])
			{
				return;
			}
			string strMsg3 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidWrongTypeInAssignment, new object[1] { expression.ToString() });
			throw new ModelClauseException(strMsg3, elementInv, OmlParseExceptionReason.InvalidArgumentType);
		}

		private void ValidateTuplesData(Invocation elementInv, Expression assignBase, Symbol symbol, Expression assignValue)
		{
			if (symbol == null)
			{
				MakeModelClauseException(Resources.TuplesParametersCannotBeIndexed, assignBase, elementInv, OmlParseExceptionReason.InvalidIndexCount);
			}
			if (assignValue.Head != Rewrite.Builtin.List)
			{
				MakeModelClauseException(Resources.TuplesParameterDataMustBeAListOfLists, assignValue, elementInv, OmlParseExceptionReason.InvalidTuples);
			}
			for (int i = 0; i < assignValue.Arity; i++)
			{
				if (assignValue[i].Head != Rewrite.Builtin.List)
				{
					MakeModelClauseException(Resources.TuplesParameterDataMustBeAListOfLists, assignValue[i], elementInv, OmlParseExceptionReason.InvalidTuples);
				}
			}
		}

		private void AnalyzeDomainSpecifier(ref TermValueClass decisionType, Expression element, Invocation closestInvocation, bool allowDomainWithDataDependent)
		{
			decisionType = AnalyzeDomain(element, closestInvocation, allowDomainWithDataDependent);
		}

		private void AnalyzeSetSpecifier(ref bool isSets, ref TermValueClass decisionType, Expression element, Invocation closestInvocation, bool allowDomainWithDataDependent)
		{
			if (element == Rewrite.Builtin.Sets)
			{
				decisionType = TermValueClass.Any;
				isSets = true;
			}
			else if (element.Head == Rewrite.Builtin.Sets)
			{
				if (element.Arity != 1)
				{
					string strMsg = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidBadSetSpecifier, new object[1] { element.Head.ToString() });
					throw new ModelClauseException(strMsg, element, OmlParseExceptionReason.InvalidSet);
				}
				decisionType = AnalyzeDomain(element[0], closestInvocation, allowDomainWithDataDependent);
				isSets = true;
			}
			else
			{
				string strMsg = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidBadSetSpecifier, new object[1] { element.Head.ToString() });
				MakeModelClauseException(strMsg, element, closestInvocation, OmlParseExceptionReason.InvalidSet);
			}
		}

		private TermValueClass AnalyzeDistributionSpecifier(ref TermValueClass decisionType, Expression expr, ref bool isFullySpecifiedParameter, Invocation closestInvocation, bool allowDomainWithDataDependent)
		{
			DistributionSymbol distributionSymbol = expr.FirstSymbolHead as DistributionSymbol;
			TermValueClass result = TermValueClass.Distribution;
			decisionType = TermValueClass.Distribution;
			if (!(expr is Invocation invocation))
			{
				isFullySpecifiedParameter = false;
				return result;
			}
			if (!(expr.Head is Symbol))
			{
				string strMsg = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidBadDomainBounds);
				throw new ModelClauseException(strMsg, expr, OmlParseExceptionReason.InvalidDomain);
			}
			if (invocation.Arity != distributionSymbol.RequiredArgumentCount)
			{
				string strMsg2 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidWrongArgumentCount, new object[2] { distributionSymbol.Name, distributionSymbol.RequiredArgumentCount });
				throw new ModelClauseException(strMsg2, expr, OmlParseExceptionReason.InvalidArgumentCount);
			}
			foreach (Expression arg in invocation.Args)
			{
				TermType termType = AnalyzeExpr(arg, invocation);
				if (termType._valueClass[0] != 0)
				{
					MakeModelClauseException(Resources.OmlInvalidNonNumericArgument, Resources.OmlInvalidNonNumericArgument, arg, invocation);
				}
				if (termType._decisionDependent)
				{
					MakeModelClauseException(Resources.OmlInvalidDecisionInDomainBound, Resources.OmlInvalidDecisionInDomainBound0, arg, invocation);
				}
				if (termType._dataDependent && !allowDomainWithDataDependent)
				{
					MakeModelClauseException(Resources.OmlInvalidParameterInDomainBound, Resources.OmlInvalidParameterInDomainBound0, arg, invocation);
				}
				if (termType._multiValue)
				{
					MakeModelClauseException(Resources.OmlInvalidForeachInDomainBound, Resources.OmlInvalidForeachInDomainBound0, arg, invocation);
				}
			}
			isFullySpecifiedParameter = true;
			return result;
		}

		private void AnalyzeDecisionOrParameterBinding(TokenType tokenType, TermValueClass decisionType, Symbol symbol, bool isIndexed, bool isFullySpecifiedParameter, Invocation closestInvocation, params Expression[] args)
		{
			if (tokenType == TokenType.AssignmentParameter)
			{
				string strMsg = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidNonAssignmentParameterInForeach);
				throw new ModelClauseException(strMsg, closestInvocation);
			}
			if (decisionType == TermValueClass.Distribution && tokenType != TokenType.Parameter)
			{
				string strMsg2 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidSetsOutsideParametersSection, new object[1] { symbol.ToString() });
				throw new ModelClauseException(strMsg2, closestInvocation, OmlParseExceptionReason.InvalidSet);
			}
			bool decisionDependent = false;
			bool flag = false;
			switch (tokenType)
			{
			case TokenType.Decision:
				decisionDependent = true;
				break;
			case TokenType.Parameter:
			case TokenType.AssignmentParameterBase:
				flag = true;
				break;
			}
			if (symbol is RecourseSymbol)
			{
				if (args == null || args.Length != 1)
				{
					throw new ModelClauseException(Resources.OmlRecourseMustContainAtLeastOneDecision, closestInvocation, OmlParseExceptionReason.InvalidDecision);
				}
				foreach (Expression expression in args)
				{
					Symbol symbol2 = expression as Symbol;
					Symbol symbol3 = expression.Head as Symbol;
					if (symbol3 is AnnotationSymbol)
					{
						if (expression.Arity == 0)
						{
							string strMsg3 = string.Format(CultureInfo.InvariantCulture, Resources.BadAnnotation);
							throw new ModelClauseException(strMsg3, closestInvocation, OmlParseExceptionReason.InvalidAnnotation);
						}
						symbol2 = expression[0] as Symbol;
						symbol3 = expression[0].Head as Symbol;
					}
					if (symbol2 != null)
					{
						AnalyzeDecisionOrParameterBinding(tokenType, decisionType, symbol2, false, isFullySpecifiedParameter, closestInvocation);
						continue;
					}
					if (symbol3 != null)
					{
						AnalyzeDecisionOrParameterBinding(tokenType, decisionType, symbol3, true, isFullySpecifiedParameter, closestInvocation);
						continue;
					}
					throw new ModelClauseException(Resources.OmlInvalidRecourseDecision, closestInvocation, OmlParseExceptionReason.InvalidDecision);
				}
				return;
			}
			if (symbol is SetSymbol)
			{
				throw new ModelClauseException(Resources.OmlInvalidAssignedDecision, closestInvocation, OmlParseExceptionReason.InvalidDecision);
			}
			ForbidDuplicatedSymbol(symbol, closestInvocation);
			foreach (Expression expression2 in args)
			{
				if (!(expression2 is Symbol))
				{
					TermType termType = AnalyzeExpr(expression2, closestInvocation);
					if (termType._decisionDependent)
					{
						string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidDeclaredIndexUsesDecision, new object[1] { symbol.ToString() });
						MakeModelClauseException(errorMessage, expression2, closestInvocation, OmlParseExceptionReason.InvalidIndex);
					}
					if (flag && termType._dataDependent)
					{
						string errorMessage2 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidDeclaredIndexUsesData, new object[1] { symbol.ToString() });
						MakeModelClauseException(errorMessage2, expression2, closestInvocation, OmlParseExceptionReason.InvalidIndex);
					}
					if (termType._multiValue)
					{
						string errorMessage3 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidDeclaredIndexUsesForeach, new object[1] { symbol.ToString() });
						MakeModelClauseException(errorMessage3, expression2, closestInvocation, OmlParseExceptionReason.InvalidIndex);
					}
					if (termType._valueClass[0] == TermValueClass.Table)
					{
						string errorMessage4 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidDeclaredIndexIsTable, new object[1] { symbol.ToString() });
						MakeModelClauseException(errorMessage4, expression2, closestInvocation, OmlParseExceptionReason.InvalidIndex);
					}
				}
			}
			TermType value = ((!isIndexed) ? new TermType(decisionDependent, flag, false, decisionType) : new TermType(decisionDependent, flag, false, TermValueClass.Table, decisionType));
			_symbols.Add(symbol, value);
		}

		private void AnalyzeSetBinding(TermValueClass decisionType, Symbol symbol, Invocation closestInvocation)
		{
			if (_sets.ContainsKey(symbol))
			{
				string strMsg = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidSymbolRedefinedAsSet, new object[1] { symbol.ToString() });
				throw new ModelClauseException(strMsg, closestInvocation, OmlParseExceptionReason.InvalidSet);
			}
			ForbidDuplicatedSymbol(symbol, closestInvocation);
			TermType value = new TermType(false, true, false, decisionType);
			_sets.Add(symbol, value);
		}

		/// <summary>
		/// Check a domain declaration (Integers, Reals[0.0,+Infinity], etc.).
		/// </summary>
		/// <param name="expr"></param>
		/// <param name="closestInvocation"></param>
		/// <param name="allowDomainWithDataDependent"></param>
		/// <returns></returns>
		private TermValueClass AnalyzeDomain(Expression expr, Invocation closestInvocation, bool allowDomainWithDataDependent)
		{
			Dictionary<Symbol, ConcreteModel> mapSubmodelNameToConcreteModel = Model._mapSubmodelNameToConcreteModel;
			Symbol symbol = expr as Symbol;
			TermValueClass value = TermValueClass.Any;
			if (expr.FirstSymbolHead == Rewrite.Builtin.Integers)
			{
				value = TermValueClass.Numeric;
			}
			else if (expr.FirstSymbolHead == Rewrite.Builtin.Reals)
			{
				value = TermValueClass.Numeric;
			}
			else if (expr.FirstSymbolHead == Rewrite.Builtin.Booleans)
			{
				value = TermValueClass.Numeric;
			}
			else if (expr.FirstSymbolHead == Rewrite.Builtin.Enum)
			{
				value = TermValueClass.Enumerated;
			}
			else if (expr.FirstSymbolHead == Rewrite.Builtin.Any)
			{
				value = TermValueClass.Any;
			}
			else
			{
				if (symbol != null && mapSubmodelNameToConcreteModel.ContainsKey(symbol))
				{
					return TermValueClass.Submodel;
				}
				if (_domainSymbols.TryGetValue(expr, out value))
				{
					return value;
				}
				string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidBadDomain, new object[1] { expr.FirstSymbolHead.ToString() });
				MakeModelClauseException(errorMessage, expr, closestInvocation, OmlParseExceptionReason.InvalidDomain);
			}
			Invocation invocation = expr as Invocation;
			if (invocation != null)
			{
				if (!(expr.Head is Symbol))
				{
					string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidBadDomainBounds);
					throw new ModelClauseException(errorMessage, expr, OmlParseExceptionReason.InvalidDomain);
				}
				if (invocation.Head == Rewrite.Builtin.Enum)
				{
					if (invocation.Arity != 1 || invocation[0].Head != Rewrite.Builtin.List)
					{
						string errorMessage = Resources.OmlInvalidBadDomainBounds;
						throw new ModelClauseException(errorMessage, expr, OmlParseExceptionReason.InvalidDomain);
					}
					{
						foreach (Expression arg in (invocation[0] as Invocation).Args)
						{
							if (!(arg is StringConstant))
							{
								string errorMessage = Resources.OmlInvalidBadDomainBoundType;
								throw new ModelClauseException(errorMessage, expr, OmlParseExceptionReason.InvalidDomain);
							}
						}
						return value;
					}
				}
				if (invocation.Arity == 1 && invocation[0].Head == Rewrite.Builtin.List)
				{
					invocation = invocation[0] as Invocation;
				}
				else if (invocation.Arity != 2)
				{
					string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidBadDomainBounds);
					throw new ModelClauseException(errorMessage, expr, OmlParseExceptionReason.InvalidDomain);
				}
				foreach (Expression arg2 in invocation.Args)
				{
					TermType termType = AnalyzeExpr(arg2, invocation);
					if (termType._valueClass[0] != value)
					{
						MakeModelClauseException(Resources.OmlInvalidBadDomainBoundType, Resources.OmlInvalidBadDomainBoundType0, arg2, invocation);
					}
					if (termType._decisionDependent)
					{
						MakeModelClauseException(Resources.OmlInvalidDecisionInDomainBound, Resources.OmlInvalidDecisionInDomainBound0, arg2, invocation);
					}
					if (termType._dataDependent && !allowDomainWithDataDependent)
					{
						MakeModelClauseException(Resources.OmlInvalidParameterInDomainBound, Resources.OmlInvalidParameterInDomainBound0, arg2, invocation);
					}
					if (termType._multiValue)
					{
						MakeModelClauseException(Resources.OmlInvalidForeachInDomainBound, Resources.OmlInvalidForeachInDomainBound0, arg2, invocation);
					}
				}
			}
			return value;
		}

		/// <summary>
		/// This is the core of the validator. It analyzes an expression, verifies that its
		/// arguments are of appropriate types, and returns a TermType object for further analysis.
		/// </summary>
		/// <param name="expr"></param>
		/// <param name="closestInvocation"></param>
		/// <returns></returns>
		private TermType AnalyzeExpr(Expression expr, Invocation closestInvocation)
		{
			try
			{
				_depth++;
				string strMsg;
				if (_depth >= 900)
				{
					strMsg = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidStackOverflow);
					throw new ModelClauseException(strMsg, null);
				}
				if (expr is Constant)
				{
					return AnalyzeConstant(expr);
				}
				if (expr is Invocation inv)
				{
					return AnalyzeInvocation(inv);
				}
				if (_iteratorBindings.TryGetValue(expr, out var value))
				{
					return value;
				}
				if (_symbols.TryGetValue(expr, out value))
				{
					return value;
				}
				strMsg = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidBadExpr, new object[1] { expr.ToString() });
				throw new ModelClauseException(strMsg, closestInvocation);
			}
			finally
			{
				_depth--;
			}
		}

		private static TermType AnalyzeConstant(Expression expr)
		{
			if (expr is FloatConstant)
			{
				TermValueClass[] valueClass = new TermValueClass[1];
				return new TermType(decisionDependent: false, dataDependent: false, multiValue: false, valueClass);
			}
			if (expr is RationalConstant)
			{
				TermValueClass[] valueClass2 = new TermValueClass[1];
				return new TermType(decisionDependent: false, dataDependent: false, multiValue: false, valueClass2);
			}
			if (expr is IntegerConstant)
			{
				TermValueClass[] valueClass3 = new TermValueClass[1];
				return new TermType(decisionDependent: false, dataDependent: false, multiValue: false, valueClass3);
			}
			if (expr is BooleanConstant)
			{
				TermValueClass[] valueClass4 = new TermValueClass[1];
				return new TermType(decisionDependent: false, dataDependent: false, multiValue: false, valueClass4);
			}
			if (expr is StringConstant)
			{
				return new TermType(false, false, false, TermValueClass.String);
			}
			return new TermType(false, false, false, TermValueClass.Any);
		}

		/// <summary>
		/// Check an invocation
		/// </summary>
		/// <param name="inv"></param>
		/// <returns></returns>
		private TermType AnalyzeInvocation(Invocation inv)
		{
			Expression head = inv.Head;
			if (head is Symbol sym && IsReservedSymbol(sym))
			{
				return AnalyzeBuiltin(inv);
			}
			return AnalyzeSubmodelInstanceOrIndex(inv);
		}

		/// <summary>
		/// Check an invocation of a builtin operator.
		/// </summary>
		/// <param name="inv"></param>
		/// <returns></returns>
		private TermType AnalyzeBuiltin(Invocation inv)
		{
			Expression head = inv.Head;
			if (head is ForeachSymbol)
			{
				return AnalyzeIteration(inv, head, hasCondition: false);
			}
			if (head is FilteredForeachSymbol)
			{
				return AnalyzeIteration(inv, head, hasCondition: true);
			}
			if (head is SumSymbol)
			{
				return AnalyzeSum(inv, head, hasCondition: false);
			}
			if (head is FilteredSumSymbol)
			{
				return AnalyzeSum(inv, head, hasCondition: true);
			}
			if (head is OrSymbol || head is AndSymbol)
			{
				ArgumentRestriction argsRestriction = new ArgumentRestriction(ArgumentRestriction.RestrictionType.Atleast, 1);
				return AnalyzeBuiltin(inv, TermValueClass.Numeric, argsRestriction, requireNumeric: true, requireFirstNumeric: false);
			}
			if (head is ImpliesSymbol)
			{
				ArgumentRestriction argsRestriction2 = new ArgumentRestriction(ArgumentRestriction.RestrictionType.Exactly, 2);
				return AnalyzeBuiltin(inv, TermValueClass.Numeric, argsRestriction2, requireNumeric: true, requireFirstNumeric: false);
			}
			if (head is NotSymbol)
			{
				ArgumentRestriction argsRestriction3 = new ArgumentRestriction(ArgumentRestriction.RestrictionType.Exactly, 1);
				return AnalyzeBuiltin(inv, TermValueClass.Numeric, argsRestriction3, requireNumeric: true, requireFirstNumeric: false);
			}
			if (head is AsIntSymbol)
			{
				ArgumentRestriction argsRestriction4 = new ArgumentRestriction(ArgumentRestriction.RestrictionType.Exactly, 1);
				return AnalyzeBuiltin(inv, TermValueClass.Numeric, argsRestriction4, requireNumeric: true, requireFirstNumeric: false);
			}
			if (head is EqualSymbol || head is UnequalSymbol || head is LessEqualSymbol || head is LessSymbol || head is GreaterEqualSymbol || head is GreaterSymbol)
			{
				ArgumentRestriction argsRestriction5 = new ArgumentRestriction(ArgumentRestriction.RestrictionType.Atleast, 2);
				return AnalyzeBuiltin(inv, TermValueClass.Numeric, argsRestriction5, requireNumeric: false, requireFirstNumeric: false);
			}
			if (head is MaxSymbol || head is MinSymbol || head is PlusSymbol || head is TimesSymbol || head is PowerSymbol)
			{
				ArgumentRestriction argsRestriction6 = new ArgumentRestriction(ArgumentRestriction.RestrictionType.Atleast, 1);
				return AnalyzeBuiltin(inv, TermValueClass.Numeric, argsRestriction6, requireNumeric: true, requireFirstNumeric: false);
			}
			if (head is ModSymbol || head is ModTruncSymbol || head is QuotientSymbol)
			{
				ArgumentRestriction argsRestriction7 = new ArgumentRestriction(ArgumentRestriction.RestrictionType.Exactly, 2);
				return AnalyzeBuiltin(inv, TermValueClass.Numeric, argsRestriction7, requireNumeric: true, requireFirstNumeric: false);
			}
			if (head is MinusSymbol || head is AbsSymbol || head is ExpSymbol || head is SqrtSymbol || head is LogSymbol || head is Log10Symbol || head is FloorSymbol || head is CeilingSymbol || head is CosSymbol || head is SinSymbol || head is TanSymbol || head is ArcCosSymbol || head is ArcSinSymbol || head is ArcTanSymbol || head is CoshSymbol || head is SinhSymbol || head is TanhSymbol)
			{
				ArgumentRestriction argsRestriction8 = new ArgumentRestriction(ArgumentRestriction.RestrictionType.Exactly, 1);
				return AnalyzeBuiltin(inv, TermValueClass.Numeric, argsRestriction8, requireNumeric: true, requireFirstNumeric: false);
			}
			if (head is IfSymbol)
			{
				ArgumentRestriction argsRestriction9 = new ArgumentRestriction(ArgumentRestriction.RestrictionType.Exactly, 3);
				return AnalyzeBuiltin(inv, TermValueClass.Numeric, argsRestriction9, requireNumeric: false, requireFirstNumeric: true);
			}
			if (head is InOrSymbol)
			{
				return AnalyzeInOr(inv);
			}
			if (head is Sos1Symbol)
			{
				return AnalyzeSos1(inv);
			}
			if (head is Sos2Symbol)
			{
				return AnalyzeSos2(inv);
			}
			string errorMessage;
			if (head is ElementOfSymbol)
			{
				if (inv.Arity != 2)
				{
					MakeModelClauseException(Resources.ElementOfMustHaveExactlyTwoArguments, inv, inv, OmlParseExceptionReason.InvalidArgumentCount);
				}
				Expression expression = inv[0];
				Expression expression2 = inv[1];
				if (!_tuples.TryGetValue(expression2, out var value))
				{
					errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.IsNotDeclaredAsTuples, new object[1] { expression2.FirstSymbolHead });
					MakeModelClauseException(errorMessage, expression2, inv, OmlParseExceptionReason.InvalidArgumentType);
				}
				if (expression.Head != Rewrite.Builtin.List)
				{
					MakeModelClauseException(Resources.TheFirstArgumentToElementOfMustBeAList, expression, inv, OmlParseExceptionReason.InvalidArgumentType);
				}
				if (expression.Arity != value.Length)
				{
					MakeModelClauseException(Resources.NumberOfElementsInTupleDoesNotMatch, expression, inv, OmlParseExceptionReason.InvalidArgumentCount);
				}
				for (int i = 0; i < expression.Arity; i++)
				{
					Expression expr = expression[i];
					AnalyzeExpr(expr, inv);
				}
				TermValueClass[] valueClass = new TermValueClass[1];
				return new TermType(decisionDependent: true, dataDependent: true, multiValue: false, valueClass);
			}
			if (head is SetSymbol || head is IdenticalSymbol)
			{
				throw new ModelClauseException(Resources.CannotUseIdenticalOrSet, inv);
			}
			errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidBadBuiltinOperator, new object[1] { head.ToString() });
			throw new ModelClauseException(errorMessage, inv);
		}

		/// <summary>
		/// Check InOr[...]. This only accepts InOr of the form
		///   InOr[Tuple[expr, expr, ...],
		///        Tuple[set, set, ...],
		///        Tuple[set, set, ...]]
		///
		/// where a "set" is either {expr, expr, ...} or a domain.
		/// </summary>
		/// <param name="inv"></param>
		/// <returns></returns>
		private TermType AnalyzeInOr(Invocation inv)
		{
			bool flag = true;
			bool decisionDependent = false;
			bool dataDependent = false;
			int arity = 0;
			foreach (Expression arg in inv.Args)
			{
				if (flag)
				{
					arity = AnalyzeExprTuple(arg, inv, ref decisionDependent, ref dataDependent);
					flag = false;
				}
				else
				{
					AnalyzeValueSetTuple(arity, arg, inv, ref decisionDependent, ref dataDependent);
				}
			}
			bool decisionDependent2 = decisionDependent;
			bool dataDependent2 = dataDependent;
			TermValueClass[] valueClass = new TermValueClass[1];
			return new TermType(decisionDependent2, dataDependent2, multiValue: false, valueClass);
		}

		private int AnalyzeExprTuple(Expression tuple, Invocation closestInvocation, ref bool decisionDependent, ref bool dataDependent)
		{
			Invocation invocation = tuple as Invocation;
			if (invocation == null || tuple.Head != Rewrite.Builtin.Tuple)
			{
				MakeModelClauseException(Resources.OmlInvalidBadTuple, Resources.OmlInvalidBadTuple0, tuple, closestInvocation);
			}
			foreach (Expression arg in invocation.Args)
			{
				TermType termType = AnalyzeExpr(arg, invocation);
				if (termType._valueClass[0] != 0)
				{
					MakeModelClauseException(Resources.OmlInvalidTupleElementNotNumber, Resources.OmlInvalidTupleElementNotNumber0, arg, invocation);
				}
				if (termType._multiValue)
				{
					MakeModelClauseException(Resources.OmlInvalidTupleElementContainsForeach, Resources.OmlInvalidTupleElementContainsForeach0, arg, invocation);
				}
				decisionDependent |= termType._decisionDependent;
				dataDependent |= termType._dataDependent;
			}
			return invocation.Arity;
		}

		private void AnalyzeValueSetTuple(int arity, Expression tuple, Invocation closestInvocation, ref bool decisionDependent, ref bool dataDependent)
		{
			Invocation invocation = tuple as Invocation;
			if (invocation == null || tuple.Head != Rewrite.Builtin.Tuple)
			{
				MakeModelClauseException(Resources.OmlInvalidBadTuple, Resources.OmlInvalidBadTuple0, tuple, closestInvocation);
			}
			if (invocation.Arity != arity)
			{
				string strMsg = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidTupleWrongArity, new object[1] { arity });
				throw new ModelClauseException(strMsg, tuple, OmlParseExceptionReason.InvalidArgumentCount);
			}
			foreach (Expression arg in invocation.Args)
			{
				if (arg is Invocation invocation2 && arg.Head == Rewrite.Builtin.List)
				{
					foreach (Expression arg2 in invocation2.Args)
					{
						TermType termType = AnalyzeExpr(arg2, invocation2);
						if (termType._valueClass[0] != 0)
						{
							MakeModelClauseException(Resources.OmlInvalidTupleElementNotNumber, Resources.OmlInvalidTupleElementNotNumber0, arg2, invocation2);
						}
						if (termType._multiValue)
						{
							MakeModelClauseException(Resources.OmlInvalidTupleElementContainsForeach, Resources.OmlInvalidTupleElementContainsForeach0, arg2, invocation2);
						}
						if (termType._decisionDependent)
						{
							MakeModelClauseException(Resources.OmlInvalidTupleContainsDecision, Resources.OmlInvalidTupleContainsDecision0, arg2, invocation2);
						}
						decisionDependent |= termType._decisionDependent;
						dataDependent |= termType._dataDependent;
					}
				}
				else if (AnalyzeDomain(arg, invocation, allowDomainWithDataDependent: true) != 0)
				{
					MakeModelClauseException(Resources.OmlInvalidTupleElementNotNumber, Resources.OmlInvalidTupleElementNotNumber0, arg, invocation);
				}
			}
		}

		private TermType AnalyzeSubmodelInstanceOrIndex(Invocation inv)
		{
			Expression head = inv.Head;
			TermType headType = AnalyzeExpr(head, inv);
			if (headType._valueClass[0] == TermValueClass.Table)
			{
				return AnalyzeIndex(headType, inv);
			}
			if (headType._valueClass[0] == TermValueClass.Submodel && inv.Arity == 1)
			{
				return AnalyzeSubmodelInstance(headType, inv);
			}
			string strMsg = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidIndexOfNonTable0, new object[1] { inv.Head });
			throw new ModelClauseException(strMsg, inv, OmlParseExceptionReason.InvalidIndex);
		}

		private TermType AnalyzeSubmodelInstance(TermType headType, Invocation inv)
		{
			Expression expr = inv.Args.First();
			Symbol firstSymbolHead = inv.FirstSymbolHead;
			if (Model._mapSubmodelInstanceNameToConcreteModel.TryGetValue(firstSymbolHead, out var value))
			{
				return value._validator.AnalyzeExpr(expr, inv);
			}
			throw new ModelClauseException(Resources.UndefinedSubmodelInstance, inv, OmlParseExceptionReason.SubmodelError);
		}

		/// <summary>
		/// Check an invocation where the head is not a builtin operator
		/// </summary>
		private TermType AnalyzeIndex(TermType headType, Invocation inv)
		{
			foreach (Expression arg in inv.Args)
			{
				TermType termType = AnalyzeExpr(arg, inv);
				if (termType._valueClass[0] == TermValueClass.Table)
				{
					MakeModelClauseException(Resources.OmlInvalidIndexingByTable, Resources.OmlInvalidIndexingByTable0, arg, inv);
				}
				if (termType._multiValue)
				{
					MakeModelClauseException(Resources.OmlInvalidIndexingByForeach, Resources.OmlInvalidIndexingByForeach0, arg, inv);
				}
			}
			return new TermType(headType._decisionDependent, headType._dataDependent, false, headType._valueClass[1]);
		}

		private TermType AnalyzeSos1(Invocation inv)
		{
			if (inv.Arity != 1)
			{
				string omlInvalidInvalidSos = Resources.OmlInvalidInvalidSos1;
				throw new ModelClauseException(omlInvalidInvalidSos, inv, OmlParseExceptionReason.InvalidSos);
			}
			if (!(inv[0] is Invocation invocation))
			{
				string omlInvalidInvalidSos2 = Resources.OmlInvalidInvalidSos1;
				throw new ModelClauseException(omlInvalidInvalidSos2, inv, OmlParseExceptionReason.InvalidSos);
			}
			if (!(invocation.Head is PlusSymbol))
			{
				string omlInvalidInvalidSos3 = Resources.OmlInvalidInvalidSos1;
				throw new ModelClauseException(omlInvalidInvalidSos3, invocation, OmlParseExceptionReason.InvalidSos);
			}
			ArgumentRestriction argsRestriction = new ArgumentRestriction(ArgumentRestriction.RestrictionType.None, 1);
			return AnalyzeBuiltin(invocation, TermValueClass.Numeric, argsRestriction, requireNumeric: false, requireFirstNumeric: false);
		}

		private TermType AnalyzeSos2(Invocation inv)
		{
			string omlInvalidInvalidSos;
			if (inv.Arity != 1)
			{
				omlInvalidInvalidSos = Resources.OmlInvalidInvalidSos2;
				throw new ModelClauseException(omlInvalidInvalidSos, inv, OmlParseExceptionReason.InvalidSos);
			}
			if (!(inv[0] is Invocation invocation))
			{
				omlInvalidInvalidSos = Resources.OmlInvalidInvalidSos2;
				throw new ModelClauseException(omlInvalidInvalidSos, inv, OmlParseExceptionReason.InvalidSos);
			}
			if (invocation.Head is EqualSymbol)
			{
				if (invocation.Arity != 2)
				{
					omlInvalidInvalidSos = Resources.OmlInvalidInvalidSos2NeedEqualityWithTwoArguments;
					throw new ModelClauseException(omlInvalidInvalidSos, invocation, OmlParseExceptionReason.InvalidSos);
				}
				ArgumentRestriction argsRestriction = new ArgumentRestriction(ArgumentRestriction.RestrictionType.None, 0);
				return AnalyzeBuiltin(invocation, TermValueClass.Numeric, argsRestriction, requireNumeric: false, requireFirstNumeric: false);
			}
			if (invocation.Head is PlusSymbol)
			{
				ArgumentRestriction argsRestriction2 = new ArgumentRestriction(ArgumentRestriction.RestrictionType.None, 1);
				return AnalyzeBuiltin(invocation, TermValueClass.Numeric, argsRestriction2, requireNumeric: false, requireFirstNumeric: false);
			}
			omlInvalidInvalidSos = Resources.OmlInvalidInvalidSos2NeedEquality;
			throw new ModelClauseException(omlInvalidInvalidSos, invocation, OmlParseExceptionReason.InvalidSos);
		}

		/// <summary>
		/// Check a Sum[...] or FilteredSum[...]
		/// </summary>
		/// <param name="inv"></param>
		/// <param name="head"></param>
		/// <param name="hasCondition"></param>
		/// <returns></returns>
		private TermType AnalyzeSum(Invocation inv, Expression head, bool hasCondition)
		{
			TermType termType = AnalyzeIteration(inv, head, hasCondition);
			if (termType._valueClass[0] != 0)
			{
				throw new ModelClauseException(Resources.OmlInvalidNonNumericArgument, inv, OmlParseExceptionReason.InvalidArgumentType);
			}
			bool decisionDependent = termType._decisionDependent;
			bool dataDependent = termType._dataDependent;
			TermValueClass[] valueClass = new TermValueClass[1];
			return new TermType(decisionDependent, dataDependent, multiValue: false, valueClass);
		}

		/// <summary>
		/// Check a Foreach[...] or FilteredForeach[...]
		/// </summary>
		/// <param name="inv"></param>
		/// <param name="head"></param>
		/// <param name="hasCondition"></param>
		/// <returns></returns>
		private TermType AnalyzeIteration(Invocation inv, Expression head, bool hasCondition)
		{
			List<Expression> iterators = new List<Expression>();
			Expression expr = BeginAnalyzeIteration(inv, head, hasCondition, iterators);
			TermType termType = AnalyzeExpr(expr, inv);
			EndAnalyzeIteration(iterators);
			return new TermType(termType._dataDependent, termType._decisionDependent, multiValue: true, termType._valueClass);
		}

		private void EndAnalyzeIteration(List<Expression> iterators)
		{
			foreach (Expression iterator in iterators)
			{
				_iteratorBindings.Remove(iterator);
			}
		}

		private Expression BeginAnalyzeIteration(Invocation inv, Expression head, bool hasCondition, List<Expression> iterators)
		{
			int num = inv.Arity - ((!hasCondition) ? 1 : 2);
			for (int i = 0; i < num; i++)
			{
				Expression iterExpr = inv[i];
				AnalyzeIterator(head, iterators, iterExpr, inv);
			}
			if (hasCondition)
			{
				Expression conditionExpression = inv[inv.Arity - 2];
				AnalyzeFilter(head, conditionExpression, inv);
			}
			return inv[inv.Arity - 1];
		}

		private void AnalyzeFilter(Expression head, Expression conditionExpression, Invocation closestInvocation)
		{
			TermType termType = AnalyzeExpr(conditionExpression, closestInvocation);
			if (termType._decisionDependent)
			{
				string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidDecisionInFilter, new object[1] { head.ToString() });
				MakeModelClauseException(errorMessage, conditionExpression, closestInvocation, OmlParseExceptionReason.InvalidFilterCondition);
			}
			if (termType._multiValue)
			{
				string errorMessage2 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidForeachInFilter, new object[1] { head.ToString() });
				MakeModelClauseException(errorMessage2, conditionExpression, closestInvocation, OmlParseExceptionReason.InvalidFilterCondition);
			}
			if (termType._valueClass[0] != 0)
			{
				string errorMessage3 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidFilterIsNotBoolean, new object[1] { head.ToString() });
				MakeModelClauseException(errorMessage3, conditionExpression, closestInvocation, OmlParseExceptionReason.InvalidFilterCondition);
			}
		}

		private void AnalyzeIterator(Expression head, List<Expression> iterators, Expression iterExpr, Invocation closestInvocation)
		{
			if (!(iterExpr.Head is ListSymbol))
			{
				string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidBadIterator, new object[1] { head.ToString() });
				MakeModelClauseException(errorMessage, iterExpr, closestInvocation, OmlParseExceptionReason.InvalidIterator);
			}
			if (iterExpr.Arity > 4 || iterExpr.Arity < 2)
			{
				string errorMessage2 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidBadIterator, new object[1] { head.ToString() });
				MakeModelClauseException(errorMessage2, iterExpr, closestInvocation, OmlParseExceptionReason.InvalidIterator);
			}
			Expression expression = iterExpr[0];
			List<Expression> list = new List<Expression>();
			Expression iterSet;
			if (iterExpr.Arity == 2)
			{
				iterSet = iterExpr[1];
			}
			else
			{
				iterSet = iterExpr[2];
				list.Add(iterExpr[1]);
				if (iterExpr.Arity == 4)
				{
					list.Add(iterExpr[3]);
				}
			}
			if (!(expression is Symbol))
			{
				string strMsg = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidBuiltinUsedAsIterator, new object[1] { expression.ToString() });
				throw new ModelClauseException(strMsg, iterExpr, OmlParseExceptionReason.InvalidIterator);
			}
			if (_iteratorBindings.ContainsKey(expression))
			{
				string strMsg2 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidRedefiningIterator, new object[1] { expression.ToString() });
				throw new ModelClauseException(strMsg2, iterExpr, OmlParseExceptionReason.InvalidIterator);
			}
			Invocation invocation = iterExpr as Invocation;
			TermType iterType;
			foreach (Expression item in list)
			{
				iterType = AnalyzeExpr(item, invocation);
				VerifyNumericIterator(iterType, iterSet, invocation);
			}
			iterType = AnalyzeIterationSet(iterSet, invocation, list.Count != 0);
			_iteratorBindings.Add(expression, iterType);
			iterators.Add(expression);
		}

		/// <summary>
		/// Check the second part of an iterator seen in a Foreach/FilteredForeach/Sum/FilteredSum
		/// </summary>
		/// <param name="iterSet"></param>
		/// <param name="closestInvocation"></param>
		/// <param name="mustBeNumeric"></param>
		/// <returns></returns>
		private TermType AnalyzeIterationSet(Expression iterSet, Invocation closestInvocation, bool mustBeNumeric)
		{
			if (!mustBeNumeric)
			{
				if (_sets.TryGetValue(iterSet, out var value))
				{
					return value;
				}
				if (iterSet is Invocation invocation && iterSet.Head is ListSymbol)
				{
					bool flag = false;
					bool flag2 = false;
					bool flag3 = false;
					foreach (Expression arg in invocation.Args)
					{
						TermType termType = AnalyzeExpr(arg, invocation);
						if (termType._decisionDependent)
						{
							MakeModelClauseException(Resources.OmlInvalidDecisionInIteratorList, Resources.OmlInvalidDecisionInIteratorList0, arg, invocation);
						}
						flag3 |= termType._dataDependent;
						switch (termType._valueClass[0])
						{
						case TermValueClass.String:
						case TermValueClass.Any:
							flag2 = true;
							break;
						case TermValueClass.Numeric:
							flag = true;
							break;
						case TermValueClass.Table:
							MakeModelClauseException(Resources.OmlInvalidTableInIteratorList, Resources.OmlInvalidTableInIteratorList0, arg, invocation);
							break;
						}
					}
					if (flag2)
					{
						return new TermType(false, flag3, false, TermValueClass.Any);
					}
					if (flag)
					{
						bool dataDependent = flag3;
						TermValueClass[] valueClass = new TermValueClass[1];
						return new TermType(decisionDependent: false, dataDependent, multiValue: false, valueClass);
					}
					TermValueClass[] valueClass2 = new TermValueClass[1];
					return new TermType(decisionDependent: false, dataDependent: false, multiValue: false, valueClass2);
				}
			}
			TermType iterType = AnalyzeExpr(iterSet, closestInvocation);
			VerifyNumericIterator(iterType, iterSet, closestInvocation);
			bool dataDependent2 = iterType._dataDependent;
			TermValueClass[] valueClass3 = new TermValueClass[1];
			return new TermType(decisionDependent: false, dataDependent2, multiValue: false, valueClass3);
		}

		private static void VerifyNumericIterator(TermType iterType, Expression iterSet, Invocation cloesestInvocation)
		{
			if (iterType._decisionDependent)
			{
				string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidDecisionInIteratorList);
				MakeModelClauseException(errorMessage, iterSet, cloesestInvocation, OmlParseExceptionReason.InvalidIterator);
			}
			if (iterType._multiValue)
			{
				string errorMessage2 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidForeachInIterator);
				MakeModelClauseException(errorMessage2, iterSet, cloesestInvocation, OmlParseExceptionReason.InvalidIterator);
			}
			if (iterType._valueClass[0] != 0)
			{
				string errorMessage3 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidNonNumericInIterator);
				MakeModelClauseException(errorMessage3, iterSet, cloesestInvocation, OmlParseExceptionReason.InvalidIterator);
			}
		}

		private static bool CompatibleValueClass(TermValueClass class1, TermValueClass class2)
		{
			if (class1 == TermValueClass.Any || class2 == TermValueClass.Any)
			{
				return true;
			}
			if (class1 == TermValueClass.Enumerated && class2 == TermValueClass.String)
			{
				return true;
			}
			if (class2 == TermValueClass.Enumerated && class1 == TermValueClass.String)
			{
				return true;
			}
			if (class1 == TermValueClass.Numeric && class2 == TermValueClass.Distribution)
			{
				return true;
			}
			if (class2 == TermValueClass.Numeric && class1 == TermValueClass.Distribution)
			{
				return true;
			}
			if (class1 == class2)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Check an invocation of a builtin operator.
		/// </summary>
		/// <param name="inv"></param>
		/// <param name="returnType"></param>
		/// <param name="argsRestriction"></param>
		/// <param name="requireNumeric"></param>
		///
		/// <param name="requireFirstNumeric"></param>
		/// <returns></returns>
		private TermType AnalyzeBuiltin(Invocation inv, TermValueClass returnType, ArgumentRestriction argsRestriction, bool requireNumeric, bool requireFirstNumeric)
		{
			Expression head = inv.Head;
			bool flag = false;
			bool flag2 = false;
			TermValueClass termValueClass = TermValueClass.Any;
			if (argsRestriction.RestType == ArgumentRestriction.RestrictionType.Exactly && inv.Arity != argsRestriction.NumberOfArgs)
			{
				string strMsg = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidWrongArgumentCount, new object[2]
				{
					head.ToString(),
					argsRestriction.NumberOfArgs
				});
				throw new ModelClauseException(strMsg, inv, OmlParseExceptionReason.InvalidArgumentCount);
			}
			bool flag3 = false;
			foreach (Expression arg in inv.Args)
			{
				TermType termType = AnalyzeExpr(arg, inv);
				if (termValueClass == TermValueClass.Any && !requireFirstNumeric)
				{
					termValueClass = termType._valueClass[0];
				}
				flag3 |= termType._multiValue;
				if (argsRestriction.RestType == ArgumentRestriction.RestrictionType.Exactly && termType._multiValue)
				{
					string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidForeachNotAllowedInOperator, new object[1] { head.ToString() });
					MakeModelClauseException(errorMessage, arg, inv, OmlParseExceptionReason.NotSpecified);
				}
				if (requireNumeric && !CompatibleValueClass(termType._valueClass[0], TermValueClass.Numeric))
				{
					MakeModelClauseException(Resources.OmlInvalidNonNumericArgument, arg, inv, OmlParseExceptionReason.InvalidArgumentType);
				}
				if (requireFirstNumeric)
				{
					if (termType._valueClass[0] != 0)
					{
						MakeModelClauseException(Resources.OmlInvalidNonBooleanArgument, arg, inv, OmlParseExceptionReason.InvalidArgumentType);
					}
					requireFirstNumeric = false;
				}
				if (termType._valueClass[0] == TermValueClass.Table)
				{
					string errorMessage2 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidTableArgument, new object[1] { head.ToString() });
					MakeModelClauseException(errorMessage2, arg, inv, OmlParseExceptionReason.InvalidArgumentType);
				}
				if (!CompatibleValueClass(termType._valueClass[0], termValueClass))
				{
					string errorMessage3 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidIncompatibleArgument, new object[1] { head.ToString() });
					MakeModelClauseException(errorMessage3, arg, inv, OmlParseExceptionReason.InvalidArgumentType);
				}
				flag |= termType._decisionDependent;
				flag2 |= termType._dataDependent;
			}
			if (argsRestriction.RestType == ArgumentRestriction.RestrictionType.Atleast && inv.Arity < argsRestriction.NumberOfArgs && !flag3)
			{
				string strMsg2 = string.Format(CultureInfo.InvariantCulture, Resources.OmlInvalidFewerThanNeededArgumentCount01, new object[2]
				{
					head.ToString(),
					argsRestriction.NumberOfArgs
				});
				throw new ModelClauseException(strMsg2, inv, OmlParseExceptionReason.InvalidArgumentCount);
			}
			return new TermType(flag, flag2, false, returnType);
		}

		/// <summary>
		/// Go through first validation of goals and constraints
		/// </summary>
		/// <param name="constraintsSections"></param>
		/// <param name="goalSections"></param>
		internal void ValidateOperators(List<Invocation> constraintsSections, List<Invocation> goalSections)
		{
			foreach (Invocation constraintsSection in constraintsSections)
			{
				for (int i = 0; i < constraintsSection.Arity; i++)
				{
					ValidateOperators(null, constraintsSection[i]);
				}
			}
			foreach (Invocation goalSection in goalSections)
			{
				for (int j = 0; j < goalSection.Arity; j++)
				{
					ValidateOperators(null, goalSection[j]);
				}
			}
		}

		private bool IsValidLabel(Expression expr)
		{
			if (expr is StringConstant)
			{
				return true;
			}
			if (expr is Symbol symbol && !_symbols.ContainsKey(symbol))
			{
				return !IsReservedSymbol(symbol);
			}
			return false;
		}

		/// <summary>
		/// Called before evaluation
		/// Validates that no forbidden operators is in constraints or goals
		/// REVIEW shahark: 
		/// 1. This pass only validate that there is no usage of 
		/// forbidden operators, it does not valid correct usage of them for now.
		/// 2. Decisions and parameters will be checked just in second pass, as the one that
		/// belongs to foreach for example are hard to e verified
		/// </summary>
		/// <param name="exprParent"></param>
		/// <param name="expr"></param>
		private void ValidateOperators(Expression exprParent, Expression expr)
		{
			if (exprParent == null && expr.Head == Rewrite.Builtin.Rule)
			{
				if (expr.Arity != 2)
				{
					throw new ModelClauseException(Resources.RuleShouldHaveTwoParameters, expr, OmlParseExceptionReason.InvalidArgumentCount);
				}
				if (!IsValidLabel(expr[0]))
				{
					MakeModelClauseException(Resources.OmlInvalidLabel, expr[0], expr as Invocation, OmlParseExceptionReason.InvalidLabel);
				}
				exprParent = expr;
				expr = expr[1];
			}
			if (expr is Constant)
			{
				return;
			}
			if (expr.FirstSymbolHead == Rewrite.Builtin.Set || expr.FirstSymbolHead == Rewrite.Builtin.Identical)
			{
				throw new ModelClauseException(Resources.CannotUseIdenticalOrSet, expr);
			}
			if (expr is Invocation invocation)
			{
				for (int i = 0; i < invocation.Arity; i++)
				{
					ValidateOperators(invocation, invocation[i]);
				}
			}
		}

		/// <summary>
		/// called after evaluation, finds any free symbols
		/// </summary>
		/// <param name="exprParent"></param>
		/// <param name="expr"></param>
		internal void ValidateSymbols(Expression exprParent, Expression expr)
		{
			if (exprParent == null && expr.Head == Rewrite.Builtin.Rule)
			{
				exprParent = expr;
				expr = expr[1];
			}
			bool flag = false;
			Invocation invocation = expr as Invocation;
			if (_decisionsSymbols.TryGetValue(expr.FirstSymbolHead, out var value))
			{
				if (value != expr.Arity)
				{
					if (invocation != null || exprParent == null)
					{
						throw new ModelClauseException(Resources.NumberOfParametersForDecisionIsWrong, expr, OmlParseExceptionReason.InvalidIndexCount);
					}
					throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.NumberOfParametersForDecisionIsWrong0, new object[1] { expr }), exprParent, OmlParseExceptionReason.InvalidArgumentCount);
				}
				flag = true;
			}
			if (expr is Constant)
			{
				return;
			}
			if (expr is ValueTableAdapter)
			{
				ThrowErrorForUnboundValueTable(expr);
			}
			if (!IsReservedSymbol(expr.FirstSymbolHead) && !flag)
			{
				if (invocation != null || exprParent == null)
				{
					throw new ModelClauseException(Resources.CannotUseThisTerm, expr);
				}
				throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.CannotUseThisTerm0, new object[1] { expr }), exprParent);
			}
			if (invocation != null)
			{
				for (int i = 0; i < invocation.Arity; i++)
				{
					ValidateSymbols(expr, invocation[i]);
				}
			}
		}

		internal void ThrowErrorForUnboundValueTable(Expression expr)
		{
			foreach (KeyValuePair<Symbol, ValueTableAdapter> paramsValueTable in Model._paramsValueTables)
			{
				if (paramsValueTable.Value == expr)
				{
					Expression firstSymbolHead = paramsValueTable.Key.FirstSymbolHead;
					firstSymbolHead.PlacementInformation = null;
					throw new ModelClauseException(Resources.ParameterShouldHaveIndexes, firstSymbolHead, OmlParseExceptionReason.InvalidIndexCount);
				}
			}
		}
	}
}
