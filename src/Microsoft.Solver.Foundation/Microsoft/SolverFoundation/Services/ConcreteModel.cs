using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class ConcreteModel
	{
		internal class SfsModelExtractor
		{
			private delegate bool TryGetItem<T>(T key, out T val);

			private SolverContext _context;

			private ConcreteModel _mod;

			internal Dictionary<Expression, Set> _sets = new Dictionary<Expression, Set>();

			internal Dictionary<Expression, Term> _sfsMap = new Dictionary<Expression, Term>();

			private Model _sfsModel;

			/// <summary>
			/// This comes from the parser, and maps the Expression for a constraint or goal (not including
			/// the Rule[] if there is one) to the actual source text for that Expression.
			/// </summary>
			internal Dictionary<Expression, string> _exprStrings;

			internal Dictionary<Symbol, SubmodelInstance> _sfsSubmodelInstances = new Dictionary<Symbol, SubmodelInstance>();

			internal Dictionary<Symbol, Model> _sfsSubmodels = new Dictionary<Symbol, Model>();

			internal Dictionary<Expression, Tuples> _tuples = new Dictionary<Expression, Tuples>();

			private SolveRewriteSystem Rewrite => _mod.Rewrite;

			internal SfsModelExtractor(ConcreteModel mod, SolverContext context)
			{
				_mod = mod;
				_context = context;
				_mod._modelExtractor = this;
			}

			internal Term TranslateExpression(Model sfsModel, Expression expression)
			{
				foreach (Decision allDecision in sfsModel.AllDecisions)
				{
					AddTermToScope(allDecision, allDecision.Name, allDecision._indexSets);
				}
				foreach (RecourseDecision allRecourseDecision in sfsModel.AllRecourseDecisions)
				{
					AddTermToScope(allRecourseDecision, allRecourseDecision.Name, allRecourseDecision._indexSets);
				}
				foreach (Parameter allParameter in sfsModel.AllParameters)
				{
					AddTermToScope(allParameter, allParameter.Name, allParameter._indexSets);
				}
				foreach (RandomParameter allRandomParameter in sfsModel.AllRandomParameters)
				{
					AddTermToScope(allRandomParameter, allRandomParameter.Name, allRandomParameter._indexSets);
				}
				foreach (Tuples allTuple in sfsModel.AllTuples)
				{
					AddTuplesToScope(allTuple, allTuple.Name);
				}
				foreach (NamedConstantTerm allNamedConstant in sfsModel.AllNamedConstants)
				{
					AddTermToScope(allNamedConstant, allNamedConstant.Name, allNamedConstant._indexSets);
				}
				return ConvertSfsExpression(expression);
			}

			private void AddTermToScope(Term term, string name, IEnumerable<Set> indexSets)
			{
				if (!Rewrite.Scope.GetSymbolAll(name, out var sym))
				{
					sym = new Symbol(Rewrite, name);
				}
				_sfsMap.Add(sym, term);
				foreach (Set indexSet in indexSets)
				{
					if (!Rewrite.Scope.GetSymbolAll(indexSet.Name, out sym))
					{
						sym = new Symbol(Rewrite, indexSet.Name);
					}
					_sets[sym] = indexSet;
				}
			}

			private void AddTuplesToScope(Tuples tuples, string name)
			{
				if (!Rewrite.Scope.GetSymbolAll(name, out var sym))
				{
					sym = new Symbol(Rewrite, name);
				}
				_tuples.Add(sym, tuples);
			}

			internal void TryGetSfsModel(ModelParser parser, Model sfsModel, Dictionary<Expression, string> exprStrings, out Dictionary<Expression, Term> sfsMap)
			{
				_exprStrings = exprStrings;
				_sfsModel = sfsModel;
				sfsMap = _sfsMap;
				Dictionary<Expression, Domain> dictionary = new Dictionary<Expression, Domain>();
				foreach (Invocation domainsSection in parser.domainsSections)
				{
					if (_context._abortFlag)
					{
						throw new MsfException(Resources.Aborted);
					}
					if (domainsSection.Arity != 2)
					{
						throw new MsfException(Resources.InternalError);
					}
					Expression dom = domainsSection[0];
					Domain value = _mod.DomainFromExpression(dom, domainsSection[1] as Symbol);
					dictionary.Add(domainsSection[1], value);
				}
				GetSfsModelInner(parser, sfsModel, dictionary);
			}

			private void GetSfsModelInner(ModelParser parser, Model sfsModel, Dictionary<Expression, Domain> domainAliases)
			{
				_sfsModel = sfsModel;
				foreach (KeyValuePair<Expression, Domain> domainAlias in domainAliases)
				{
					_mod._domainCache.Add(domainAlias.Key, domainAlias.Value);
				}
				foreach (KeyValuePair<Symbol, ConcreteModel> item in _mod._mapSubmodelNameToConcreteModel)
				{
					if (_context._abortFlag)
					{
						throw new MsfException(Resources.Aborted);
					}
					SfsModelExtractor sfsModelExtractor = new SfsModelExtractor(item.Value, _context);
					sfsModelExtractor._exprStrings = _exprStrings;
					Model model = sfsModel.CreateSubModel(item.Key.ToString());
					sfsModelExtractor.GetSfsModelInner(item.Value._parser, model, domainAliases);
					_sfsSubmodels.Add(item.Key, model);
				}
				foreach (Invocation parametersSection in parser.parametersSections)
				{
					if (parametersSection.Arity != 0)
					{
						GetParameterSectionInner(sfsModel, parametersSection);
					}
				}
				foreach (Invocation decisionsSection in parser.decisionsSections)
				{
					GetDecisionSectionInner(sfsModel, decisionsSection);
				}
				foreach (Invocation constraintsSection in parser.constraintsSections)
				{
					GetConstraintSectionInner(sfsModel, constraintsSection);
				}
				foreach (Invocation goalSection in parser.goalSections)
				{
					GetGoalSectionInner(sfsModel, goalSection);
				}
			}

			private void GetParameterSectionInner(Model sfsModel, Invocation parameterSection)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				Expression expression = parameterSection[0];
				Invocation invDom = expression as Invocation;
				Expression exprValue = null;
				if (expression.FirstSymbolHead == Rewrite.Builtin.Sets)
				{
					exprValue = GetSetsParameterSection(parameterSection, expression, invDom, exprValue);
					return;
				}
				if (expression.FirstSymbolHead == Rewrite.Builtin.Tuples)
				{
					exprValue = GetTuplesParameterSection(sfsModel, parameterSection, expression, invDom, exprValue);
					return;
				}
				if (expression.FirstSymbolHead == Rewrite.Builtin.Scenarios)
				{
					exprValue = GetScenariosParameterSection(sfsModel, parameterSection, expression, invDom, exprValue);
					return;
				}
				Domain domain = _mod.DomainFromExpression(expression);
				if (domain.ValueClass == TermValueClass.Distribution)
				{
					GetRandomParameters(sfsModel, parameterSection, expression);
					return;
				}
				for (int i = 1; i < parameterSection.Arity; i++)
				{
					Expression exprParam = parameterSection[i];
					exprParam = GetAnnotations(exprParam, out var description, out var enabled);
					if (!enabled)
					{
						throw new MsfException(Resources.ParametersCannotBeDisabled);
					}
					if (exprParam.FirstSymbolHead == Rewrite.Builtin.Foreach || exprParam.FirstSymbolHead == Rewrite.Builtin.FilteredForeach || exprParam.FirstSymbolHead == Rewrite.Builtin.Set)
					{
						exprParam = ConvertNamedConstant(exprParam, domain);
						continue;
					}
					List<Set> indexSets = GetIndexSets(ref exprParam);
					Parameter parameter = new Parameter(domain, ExprToName(exprParam), indexSets.ToArray());
					parameter.Description = description;
					sfsModel.AddParameter(parameter);
					_sfsMap[exprParam] = parameter;
				}
			}

			private Expression GetScenariosParameterSection(Model sfsModel, Invocation parameterSection, Expression exprDom, Invocation invDom, Expression exprValue)
			{
				Domain[] array = new Domain[exprDom.Arity + 1];
				array[0] = Domain.Probability;
				if (invDom != null)
				{
					for (int i = 0; i < exprDom.Arity; i++)
					{
						array[i + 1] = _mod.DomainFromExpression(exprDom[i]);
					}
				}
				for (int j = 1; j < parameterSection.Arity; j++)
				{
					Expression exprParam = parameterSection[j];
					exprParam = GetAnnotations(exprParam, out var description, out var enabled);
					if (!enabled)
					{
						throw new MsfException(Resources.ParametersCannotBeDisabled);
					}
					if (exprParam.FirstSymbolHead == Rewrite.Builtin.Set)
					{
						exprValue = exprParam[1];
						exprParam = exprParam[0];
					}
					List<Set> indexSets = GetIndexSets(ref exprParam);
					if (exprValue != null && indexSets.Count > 0)
					{
						throw new NotSupportedException(Resources.OmlInvalidIndexedRandomParameters);
					}
					string name = ExprToName(exprParam);
					ScenariosParameter value = MakeScenarios(sfsModel, array, exprValue, indexSets, name, description);
					_sfsMap[exprParam] = value;
				}
				return exprValue;
			}

			private Expression GetTuplesParameterSection(Model sfsModel, Invocation parameterSection, Expression exprDom, Invocation invDom, Expression exprValue)
			{
				Domain[] array = new Domain[exprDom.Arity];
				if (invDom != null)
				{
					for (int i = 0; i < exprDom.Arity; i++)
					{
						array[i] = _mod.DomainFromExpression(exprDom[i]);
					}
				}
				for (int j = 1; j < parameterSection.Arity; j++)
				{
					Expression exprParam = parameterSection[j];
					exprParam = GetAnnotations(exprParam, out var description, out var enabled);
					if (!enabled)
					{
						throw new MsfException(Resources.ParametersCannotBeDisabled);
					}
					if (exprParam.FirstSymbolHead == Rewrite.Builtin.Set)
					{
						exprValue = exprParam[1];
						exprParam = exprParam[0];
					}
					string name = ExprToName(exprParam);
					Tuples value = MakeTuples(sfsModel, array, exprValue, name, description);
					_tuples[exprParam] = value;
				}
				return exprValue;
			}

			private Expression GetSetsParameterSection(Invocation parameterSection, Expression exprDom, Invocation invDom, Expression exprValue)
			{
				Domain domain = Domain.Any;
				if (invDom != null)
				{
					domain = _mod.DomainFromExpression(exprDom[0]);
				}
				for (int i = 1; i < parameterSection.Arity; i++)
				{
					Expression expression = parameterSection[i];
					Set set = null;
					if (expression.FirstSymbolHead == Rewrite.Builtin.Set)
					{
						exprValue = expression[1];
						expression = expression[0];
						Term[] values = ConvertSfsList(exprValue);
						set = new Set(values, domain, ExprToName(expression));
					}
					else
					{
						set = new Set(domain, ExprToName(expression));
					}
					_sets[expression] = set;
				}
				return exprValue;
			}

			private Term[] ConvertSfsList(Expression exprValue)
			{
				if (exprValue.FirstSymbolHead == Rewrite.Builtin.List)
				{
					Term[] array = new Term[exprValue.Arity];
					for (int i = 0; i < exprValue.Arity; i++)
					{
						array[i] = ConvertSfsExpression(exprValue[i]);
					}
					return array;
				}
				throw new MsfException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidList0, new object[1] { exprValue }));
			}

			private void GetDecisionSectionInner(Model sfsModel, Invocation decisionSection)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				Expression dom = decisionSection[0];
				Model submodel;
				Domain domain = _mod.DomainFromExpression(dom, out submodel);
				for (int i = 1; i < decisionSection.Arity; i++)
				{
					Expression expr = decisionSection[i];
					bool flag = TryRemoveRecourse(ref expr);
					expr = GetAnnotations(expr, out var description, out var enabled);
					if (!enabled)
					{
						throw new MsfException(Resources.DecisionsCannotBeDisabled);
					}
					List<Set> indexSets = GetIndexSets(ref expr);
					if (domain != null)
					{
						if (flag)
						{
							RecourseDecision recourseDecision = new RecourseDecision(domain, ExprToName(expr), indexSets.ToArray());
							recourseDecision.Description = description;
							sfsModel.AddDecision(recourseDecision);
							_sfsMap[expr] = recourseDecision;
						}
						else
						{
							Decision decision = new Decision(domain, ExprToName(expr), indexSets.ToArray());
							decision.Description = description;
							sfsModel.AddDecision(decision);
							_sfsMap[expr] = decision;
						}
					}
					else
					{
						if (submodel == null || indexSets.Count != 0)
						{
							throw new ModelClauseException(Resources.InvalidDecisionDefinition, decisionSection, OmlParseExceptionReason.InvalidDecision);
						}
						SubmodelInstance value = submodel.CreateInstance(ExprToName(expr));
						_sfsSubmodelInstances.Add(expr as Symbol, value);
					}
				}
			}

			private void GetConstraintSectionInner(Model sfsModel, Invocation constraintSection)
			{
				for (int i = 0; i < constraintSection.Arity; i++)
				{
					if (_context._abortFlag)
					{
						throw new MsfException(Resources.Aborted);
					}
					Expression expr = constraintSection[i];
					string exprAndName = GetExprAndName(ref expr);
					string value = null;
					if (!_exprStrings.TryGetValue(expr, out value))
					{
						value = null;
					}
					expr = GetAnnotations(expr, out var description, out var enabled);
					Term constraint = ConvertSfsExpression(expr);
					Constraint constraint2 = sfsModel.AddConstraint(exprAndName, constraint);
					constraint2.Description = description;
					constraint2.Enabled = enabled;
					constraint2._expression = value;
				}
			}

			private void GetGoalSectionInner(Model sfsModel, Invocation goalSection)
			{
				bool flag = goalSection.Head == Rewrite.Builtin.Minimize;
				for (int i = 0; i < goalSection.Arity; i++)
				{
					if (_context._abortFlag)
					{
						throw new MsfException(Resources.Aborted);
					}
					Expression expr = goalSection[i];
					string exprAndName = GetExprAndName(ref expr);
					string value = null;
					if (!_exprStrings.TryGetValue(expr, out value))
					{
						value = null;
					}
					expr = GetGoalAnnotations(expr, out var order, out var description, out var enabled);
					Term goal = ConvertSfsExpression(expr);
					Microsoft.SolverFoundation.Services.Goal goal2 = sfsModel.AddGoal(exprAndName, flag ? GoalKind.Minimize : GoalKind.Maximize, goal);
					goal2.Order = order;
					goal2.Description = description;
					goal2.Enabled = enabled;
					goal2._expression = value;
				}
			}

			/// <summary> Try to remove Recourse from an expression.
			/// </summary>
			/// <param name="expr">The expression to be modified.</param>
			/// <returns>Returns true if Recourse was stripped off.</returns>
			private static bool TryRemoveRecourse(ref Expression expr)
			{
				if (expr.Head is RecourseSymbol)
				{
					expr = expr[0];
					return true;
				}
				return false;
			}

			private ScenariosParameter MakeScenarios(Model sfsModel, Domain[] doms, Expression exprValue, IEnumerable<Set> sets, string name, string description)
			{
				if (doms.Length != 2)
				{
					throw new NotSupportedException("Currently only Probability-Value couples are supported");
				}
				ScenariosParameter scenariosParameter;
				if (exprValue != null)
				{
					if (exprValue.Head != Rewrite.Builtin.List)
					{
						throw new MsfException();
					}
					IEnumerable<Scenario> scenarios = from scenario in GetTupleData(doms, exprValue)
						select new Scenario(scenario[0], scenario[1]);
					scenariosParameter = new ScenariosParameter(name, scenarios);
				}
				else
				{
					scenariosParameter = new ScenariosParameter(name, sets.ToArray());
				}
				scenariosParameter.Description = description;
				sfsModel.AddParameter(scenariosParameter);
				return scenariosParameter;
			}

			private Tuples MakeTuples(Model sfsModel, Domain[] doms, Expression exprValue, string name, string description)
			{
				Tuples tuples;
				if (exprValue != null)
				{
					if (exprValue.Head != Rewrite.Builtin.List)
					{
						throw new MsfException();
					}
					Rational[][] data = GetTupleData(doms, exprValue).ToArray();
					tuples = new Tuples(name, doms, data);
				}
				else
				{
					tuples = new Tuples(name, doms);
				}
				tuples.Description = description;
				sfsModel.AddTuples(tuples);
				return tuples;
			}

			private Expression GetGoalAnnotations(Expression exprParam, out int order, out string description, out bool enabled)
			{
				return GetAnnotations(exprParam, isGoal: true, out order, out description, out enabled);
			}

			private Expression GetAnnotations(Expression exprParam, out string description, out bool enabled)
			{
				int order;
				return GetAnnotations(exprParam, isGoal: false, out order, out description, out enabled);
			}

			private Expression GetAnnotations(Expression exprParam, bool isGoal, out int order, out string description, out bool enabled)
			{
				description = null;
				enabled = true;
				order = 0;
				while (exprParam.Head == Rewrite.Builtin.Annotation)
				{
					if (exprParam.Arity < 3)
					{
						throw new ModelClauseException(Resources.BadAnnotation, exprParam, OmlParseExceptionReason.InvalidAnnotation);
					}
					if (!exprParam[1].GetValue(out string val))
					{
						throw new ModelClauseException(Resources.BadAnnotation, exprParam, OmlParseExceptionReason.InvalidAnnotation);
					}
					switch (val)
					{
					case "description":
						if (!exprParam[2].GetValue(out description))
						{
							throw new ModelClauseException(Resources.BadAnnotation, exprParam, OmlParseExceptionReason.InvalidAnnotation);
						}
						break;
					case "enabled":
						if (!exprParam[2].GetValue(out enabled))
						{
							throw new ModelClauseException(Resources.BadAnnotation, exprParam, OmlParseExceptionReason.InvalidAnnotation);
						}
						break;
					case "order":
						if (!exprParam[2].GetValue(out order) || !isGoal)
						{
							throw new ModelClauseException(Resources.BadAnnotation, exprParam, OmlParseExceptionReason.InvalidAnnotation);
						}
						break;
					default:
						throw new ModelClauseException(Resources.BadAnnotation, exprParam, OmlParseExceptionReason.InvalidAnnotation);
					}
					exprParam = exprParam[0];
				}
				return exprParam;
			}

			private void GetRandomParameters(Model sfsModel, Invocation parameterSection, Expression exprDom)
			{
				DistributionSymbol distributionSymbol = exprDom.FirstSymbolHead as DistributionSymbol;
				double[] randomParameterArguments = GetRandomParameterArguments(exprDom);
				for (int i = 1; i < parameterSection.Arity; i++)
				{
					Expression exprParam = parameterSection[i];
					exprParam = GetAnnotations(exprParam, out var description, out var enabled);
					if (!enabled)
					{
						throw new MsfException(Resources.ParametersCannotBeDisabled);
					}
					RandomParameter randomParameter = GetRandomParameter(distributionSymbol, ref exprParam, randomParameterArguments);
					randomParameter.Description = description;
					sfsModel.AddParameter(randomParameter);
					_sfsMap[exprParam] = randomParameter;
				}
			}

			private RandomParameter GetRandomParameter(DistributionSymbol distributionSymbol, ref Expression exprParam, double[] args)
			{
				List<Set> indexSets = GetIndexSets(ref exprParam);
				if (args != null && args.Length > 0 && indexSets.Count > 0)
				{
					throw new NotSupportedException(Resources.OmlInvalidIndexedRandomParameters);
				}
				RandomParameter result = null;
				if (distributionSymbol is UniformDistributionSymbol)
				{
					result = ((args == null) ? new UniformDistributionParameter(ExprToName(exprParam), indexSets.ToArray()) : new UniformDistributionParameter(ExprToName(exprParam), args[0], args[1]));
				}
				else if (distributionSymbol is NormalDistributionSymbol)
				{
					result = ((args == null) ? new NormalDistributionParameter(ExprToName(exprParam), indexSets.ToArray()) : new NormalDistributionParameter(ExprToName(exprParam), args[0], args[1]));
				}
				else if (distributionSymbol is DiscreteUniformDistributionSymbol)
				{
					result = ((args == null) ? new DiscreteUniformDistributionParameter(ExprToName(exprParam), indexSets.ToArray()) : new DiscreteUniformDistributionParameter(ExprToName(exprParam), args[0], args[1]));
				}
				else if (distributionSymbol is ExponentialDistributionSymbol)
				{
					result = ((args == null) ? new ExponentialDistributionParameter(ExprToName(exprParam), indexSets.ToArray()) : new ExponentialDistributionParameter(ExprToName(exprParam), args[0]));
				}
				else if (distributionSymbol is GeometricDistributionSymbol)
				{
					result = ((args == null) ? new GeometricDistributionParameter(ExprToName(exprParam), indexSets.ToArray()) : new GeometricDistributionParameter(ExprToName(exprParam), args[0]));
				}
				else if (distributionSymbol is BinomialDistributionSymbol)
				{
					result = ((args == null) ? new BinomialDistributionParameter(ExprToName(exprParam), indexSets.ToArray()) : new BinomialDistributionParameter(ExprToName(exprParam), (int)args[0], args[1]));
				}
				else if (distributionSymbol is LogNormalDistributionSymbol)
				{
					result = ((args == null) ? new LogNormalDistributionParameter(ExprToName(exprParam), indexSets.ToArray()) : new LogNormalDistributionParameter(ExprToName(exprParam), args[0], args[1]));
				}
				return result;
			}

			private static double[] GetRandomParameterArguments(Expression exprDom)
			{
				if (exprDom.Arity == 0)
				{
					return null;
				}
				int requiredArgumentCount = (exprDom.FirstSymbolHead as DistributionSymbol).RequiredArgumentCount;
				double[] array = new double[requiredArgumentCount];
				for (int i = 0; i < array.Length; i++)
				{
					exprDom[i].GetNumericValue(out var val);
					array[i] = val.ToDouble();
				}
				return array;
			}

			private IEnumerable<Rational[]> GetTupleData(Domain[] doms, Expression exprValue)
			{
				for (int row = 0; row < exprValue.Arity; row++)
				{
					Expression exprRow = exprValue[row];
					Rational[] result = new Rational[doms.Length];
					if (exprRow.Head != Rewrite.Builtin.List)
					{
						throw new ModelClauseException(Resources.OmlInvalidTuplesCanOnlyBeAssignedToListOfLists, exprRow, OmlParseExceptionReason.InvalidTuples);
					}
					if (exprRow.Arity != doms.Length)
					{
						throw new ModelClauseException(Resources.OmlInvalidEachListMustContainSameNumberOfElements, exprRow, OmlParseExceptionReason.InvalidTuples);
					}
					for (int i = 0; i < exprRow.Arity; i++)
					{
						Expression expression = exprRow[i];
						Term term = ConvertSfsExpression(expression);
						Rational rational;
						if (term.TryEvaluateConstantValue(out Rational value, new Term.EvaluationContext()))
						{
							rational = value;
						}
						else
						{
							if (!term.TryEvaluateConstantValue(out object value2, new Term.EvaluationContext()))
							{
								throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownDataValue0, new object[1] { expression }));
							}
							rational = doms[i].GetOrdinal((string)value2);
						}
						if (!doms[i].IsValidValue(rational))
						{
							throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.TupleDataDoesNotBelongToDomainSpecifiedIn01, new object[2]
							{
								expression,
								doms[i]
							}));
						}
						result[i] = rational;
					}
					yield return result;
				}
			}

			/// <summary>
			/// Take a Parameter in the form "P = const" or "Foreach[..., P = const]" (possibly nested)
			/// and create a NamedConstantTerm object to represent it.
			/// </summary>
			private Expression ConvertNamedConstant(Expression exprParam, Domain domain)
			{
				exprParam = FindNamedConstantIterators(exprParam, out var allIterTerms, out var allIterSets);
				Expression expr = exprParam[1];
				Term innerTerm = ConvertSfsExpression(expr);
				Expression expression = exprParam[0];
				Expression expression2;
				int num;
				if (expression is Invocation)
				{
					expression2 = expression.Head;
					num = expression.Arity;
				}
				else
				{
					expression2 = expression;
					num = 0;
				}
				IterationTerm[] array = new IterationTerm[num];
				Set[] array2 = new Set[num];
				if (num > 0)
				{
					for (int i = 0; i < num; i++)
					{
						Expression expression3 = expression[i];
						if (!allIterTerms.TryGetValue(expression3, out array[i]) || !allIterSets.TryGetValue(expression3, out array2[i]))
						{
							throw new MsfException(string.Format(CultureInfo.CurrentCulture, Resources.IsNotAValidIterationTerm, new object[1] { expression3 }));
						}
					}
				}
				NamedConstantTerm namedConstantTerm = new NamedConstantTerm(ExprToName(expression2), innerTerm, array, array2, domain);
				_sfsMap[expression2] = namedConstantTerm;
				_sfsModel._namedConstants.Add(namedConstantTerm);
				return exprParam;
			}

			/// <summary>
			/// Unwraps a series of nested Foreach statements "Foreach[..., Foreach[..., P[x,y] = expr]]"
			/// and extracts the iterators and the inner expression "P[x,y] = expr".
			/// </summary>
			/// <param name="exprParam"></param>
			/// <param name="allIterTerms"></param>
			/// <param name="allIterSets"></param>
			/// <returns></returns>
			private Expression FindNamedConstantIterators(Expression exprParam, out Dictionary<Expression, IterationTerm> allIterTerms, out Dictionary<Expression, Set> allIterSets)
			{
				allIterTerms = new Dictionary<Expression, IterationTerm>();
				allIterSets = new Dictionary<Expression, Set>();
				while (exprParam.FirstSymbolHead == Rewrite.Builtin.Foreach)
				{
					IterationTerm[] array = new IterationTerm[exprParam.Arity - 1];
					Set[] array2 = new Set[exprParam.Arity - 1];
					BuildForeachIters(exprParam, exprParam.Arity - 1, array, array2);
					for (int i = 0; i < array.Length; i++)
					{
						Expression key = exprParam[i][0];
						allIterTerms[key] = array[i];
						allIterSets[key] = array2[i];
					}
					exprParam = exprParam[exprParam.Arity - 1];
				}
				return exprParam;
			}

			/// <summary>
			/// </summary>
			/// <returns>Either full or empty List, but never null</returns>
			private List<Set> GetIndexSets(ref Expression expr)
			{
				List<Set> list = new List<Set>();
				if (expr is Invocation)
				{
					if (expr.FirstSymbolHead == Rewrite.Builtin.Foreach)
					{
						int num = expr.Arity - 1;
						IterationTerm[] iterTerms = new IterationTerm[num];
						Set[] array = new Set[num];
						BuildForeachIters(expr, num, iterTerms, array);
						for (int i = 0; i < num; i++)
						{
							_sfsMap.Remove(expr[i][0]);
							_sets.Add(expr[i][0], array[i]);
						}
						Expression expr2 = expr[num];
						list = GetIndexSets(ref expr2);
						for (int j = 0; j < num; j++)
						{
							_sets.Remove(expr[j][0]);
						}
						expr = expr2;
					}
					else if (expr.FirstSymbolHead == Rewrite.Builtin.FilteredForeach)
					{
						int num2 = expr.Arity - 2;
						IterationTerm[] iterTerms2 = new IterationTerm[num2];
						Set[] array2 = new Set[num2];
						BuildForeachIters(expr, num2, iterTerms2, array2);
						for (int k = 0; k < num2; k++)
						{
							_sfsMap.Remove(expr[k][0]);
							_sets.Add(expr[k][0], array2[k]);
						}
						Expression expr3 = expr[num2 + 1];
						list = GetIndexSets(ref expr3);
						for (int l = 0; l < num2; l++)
						{
							_sets.Remove(expr[l][0]);
						}
						expr = expr3;
					}
					else
					{
						for (int m = 0; m < expr.Arity; m++)
						{
							Expression expression = expr[m];
							if (!_sets.TryGetValue(expression, out var value))
							{
								throw new ModelClauseException(string.Format(CultureInfo.CurrentCulture, Resources.IsNotAValidSet, new object[1] { expression }), expr, OmlParseExceptionReason.InvalidSet);
							}
							list.Add(value);
						}
						expr = expr.Head;
					}
				}
				return list;
			}

			private string GetExprAndName(ref Expression expr)
			{
				string result = null;
				if (expr is Invocation && expr.Head == Rewrite.Builtin.Rule)
				{
					result = ExprToName(expr[0]);
					expr = expr[1];
				}
				return result;
			}

			internal static string ExprToName(Expression expr)
			{
				if (!expr.GetValue(out string val))
				{
					if (expr is Invocation)
					{
						throw new MsfException();
					}
					return expr.ToString();
				}
				return val;
			}

			internal bool TryGetSfsModel(Model sfsModel, out Dictionary<Expression, Term> sfsMap, out string strError, out Expression exprError)
			{
				_sfsModel = sfsModel;
				try
				{
					sfsMap = _sfsMap;
					for (int i = 0; i < _mod._mpvidvar.Count; i++)
					{
						Variable variable = _mod._mpvidvar[i];
						Decision decision = new Decision(variable.Domain, variable.Key.ToString());
						sfsModel.AddDecision(decision);
						_sfsMap[variable.Key] = decision;
					}
					foreach (Expression key in _mod._mpkeycon.Keys)
					{
						if (!AddSfsConstraint(key))
						{
							exprError = key;
							strError = Resources.InvalidFiniteConstraint;
							return false;
						}
					}
					foreach (Goal item in _mod._rggoal)
					{
						if (!AddSfsGoal(sfsModel, item))
						{
							exprError = item._expr;
							strError = Resources.InvalidFiniteGoal;
							return false;
						}
					}
				}
				catch (ModelClauseException ex)
				{
					exprError = ex.Expr;
					strError = ex.Message;
					sfsMap = null;
					return false;
				}
				catch (ModelException ex2)
				{
					exprError = null;
					strError = ex2.Message;
					sfsMap = null;
					return false;
				}
				finally
				{
					_sfsMap = null;
				}
				strError = null;
				exprError = null;
				return true;
			}

			private bool AddSfsConstraint(Expression con)
			{
				if (con.Head is RuleSymbol)
				{
					_sfsModel.AddConstraint(con[0].ToString(), ConvertSfsExpression(con[1]));
				}
				else
				{
					_sfsModel.AddConstraint(con.ToString(), ConvertSfsExpression(con));
				}
				return true;
			}

			private Term TryGetSubmodelInstanceMember(Expression instanceMemberExpr)
			{
				SfsModelExtractor sfsModelExtractor = this;
				Expression expression = instanceMemberExpr;
				List<SubmodelInstance> list = new List<SubmodelInstance>();
				while (expression != null && expression is Invocation invocation)
				{
					if (invocation.Arity != 1)
					{
						return null;
					}
					Symbol firstSymbolHead = invocation.FirstSymbolHead;
					if (!sfsModelExtractor._sfsSubmodelInstances.TryGetValue(firstSymbolHead, out var value))
					{
						return null;
					}
					list.Add(value);
					if (!sfsModelExtractor._mod._mapSubmodelInstanceNameToConcreteModel.TryGetValue(firstSymbolHead, out var value2))
					{
						return null;
					}
					sfsModelExtractor = value2._modelExtractor;
					expression = invocation.ArgsArray[0];
				}
				if (!sfsModelExtractor._sfsMap.TryGetValue(expression, out var value3))
				{
					return null;
				}
				if (list.Count == 0)
				{
					return value3;
				}
				SubmodelInstance submodelInstance = list[0];
				for (int i = 1; i < list.Count; i++)
				{
					if (!submodelInstance.TryGetSubmodelInstance(list[i], out var val))
					{
						return null;
					}
					submodelInstance = val;
				}
				Decision decision = value3 as Decision;
				RecourseDecision recourseDecision = value3 as RecourseDecision;
				Parameter parameter = value3 as Parameter;
				RandomParameter randomParameter = value3 as RandomParameter;
				if ((object)decision != null)
				{
					return TryGetFinalMemberTerm(decision, submodelInstance.TryGetDecision);
				}
				if ((object)recourseDecision != null)
				{
					return TryGetFinalMemberTerm(recourseDecision, submodelInstance.TryGetRecourseDecision);
				}
				if ((object)parameter != null)
				{
					return TryGetFinalMemberTerm(parameter, submodelInstance.TryGetParameter);
				}
				if ((object)randomParameter != null)
				{
					return TryGetFinalMemberTerm(randomParameter, submodelInstance.TryGetRandomParameter);
				}
				return null;
			}

			private static Term TryGetFinalMemberTerm<T>(T templateMember, TryGetItem<T> tryGetItem) where T : Term
			{
				if ((object)templateMember == null)
				{
					return null;
				}
				if (!tryGetItem(templateMember, out var val))
				{
					return null;
				}
				return val;
			}

			private Term ConvertSfsExpression(Expression expr)
			{
				if (_sfsMap.TryGetValue(expr, out var value))
				{
					return value;
				}
				if (expr.GetValue(out string val))
				{
					return new StringConstantTerm(val);
				}
				if (expr.GetValue(out double val2))
				{
					return val2;
				}
				if (expr.GetValue(out Rational val3))
				{
					return new ConstantTerm(val3);
				}
				if (expr.GetValue(out bool val4))
				{
					return val4;
				}
				if (expr.GetValue(out int val5))
				{
					return val5;
				}
				if (_tuples.TryGetValue(expr, out var _))
				{
					throw new ModelClauseException(Resources.OmlInvalidTupleOutsideTableConstraint, expr, OmlParseExceptionReason.InvalidTuples);
				}
				if (expr is Invocation invocation)
				{
					Expression head = expr.Head;
					if (head is PlusSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						return Model.Sum(terms);
					}
					if (head is TimesSymbol)
					{
						if (invocation.Arity == 2 && invocation[1].GetNumericValue(out var val6) && !val6.IsInteger() && (1 / val6).IsInteger())
						{
							return Model.Quotient(ConvertSfsExpression(invocation[0]), 1 / val6);
						}
						Term[] terms;
						if (invocation.Arity > 2 && invocation[invocation.Arity - 1].GetNumericValue(out val6) && !val6.IsInteger() && (1 / val6).IsInteger())
						{
							terms = (from arg in invocation.Args.Take(invocation.Arity - 1)
								select ConvertSfsExpression(arg)).ToArray();
							return Model.Quotient(Model.Product(terms), 1 / val6);
						}
						terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						return Model.Product(terms);
					}
					if (head is PowerSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 2);
						return Model.Power(terms[0], terms[1]);
					}
					if (head is MinusSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Negate(terms[0]);
					}
					if (head is AbsSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Abs(terms[0]);
					}
					if (head is MaxSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						return Model.Max(terms);
					}
					if (head is MinSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						return Model.Min(terms);
					}
					if (head is IfSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 3);
						return Model.If(terms[0], terms[1], terms[2]);
					}
					if (head is CosSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Cos(terms[0]);
					}
					if (head is SinSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Sin(terms[0]);
					}
					if (head is TanSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Tan(terms[0]);
					}
					if (head is ArcCosSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.ArcCos(terms[0]);
					}
					if (head is ArcSinSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.ArcSin(terms[0]);
					}
					if (head is ArcTanSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.ArcTan(terms[0]);
					}
					if (head is CoshSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Cosh(terms[0]);
					}
					if (head is SinhSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Sinh(terms[0]);
					}
					if (head is TanhSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Tanh(terms[0]);
					}
					if (head is LogSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Log(terms[0]);
					}
					if (head is Log10Symbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Log10(terms[0]);
					}
					if (head is ExpSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Exp(terms[0]);
					}
					if (head is SqrtSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Sqrt(terms[0]);
					}
					if (head is CeilingSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Ceiling(terms[0]);
					}
					if (head is FloorSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Floor(terms[0]);
					}
					if (head is Sos1Symbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Sos1(terms[0]);
					}
					if (head is Sos2Symbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Sos2(terms[0]);
					}
					if (head is EqualSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						return Model.Equal(terms);
					}
					if (head is UnequalSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						return Model.AllDifferent(terms);
					}
					if (head is LessSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						return Model.Less(terms);
					}
					if (head is LessEqualSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						return Model.LessEqual(terms);
					}
					if (head is GreaterSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						return Model.Greater(terms);
					}
					if (head is GreaterEqualSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						return Model.GreaterEqual(terms);
					}
					if (head is AndSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						return Model.And(terms);
					}
					if (head is OrSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						return Model.Or(terms);
					}
					if (head is NotSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 1);
						return Model.Not(terms[0]);
					}
					if (head is ImpliesSymbol)
					{
						Term[] terms = invocation.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						CheckInputLength(expr, terms.Length, 2);
						return Model.Implies(terms[0], terms[1]);
					}
					if (head is AsIntSymbol)
					{
						return ConvertSfsExpression(invocation[0]);
					}
					if (head is SumSymbol)
					{
						return ConvertSfsExpression(Rewrite.Builtin.Plus.Invoke(Rewrite.Builtin.Foreach.Invoke(invocation.ArgsArray)));
					}
					if (head is FilteredSumSymbol)
					{
						return ConvertSfsExpression(Rewrite.Builtin.Plus.Invoke(Rewrite.Builtin.FilteredForeach.Invoke(invocation.ArgsArray)));
					}
					if (head is ElementOfSymbol)
					{
						Expression expression = expr[0];
						Expression key = expr[1];
						if (expression.Head != Rewrite.Builtin.List)
						{
							throw new ModelClauseException(Resources.ExpectedTuples, expression, OmlParseExceptionReason.InvalidTuples);
						}
						if (!_tuples.TryGetValue(key, out var value3))
						{
							throw new ModelClauseException(Resources.TuplesMustBeAddedToTheModelBeforeBeingUsed, expression, OmlParseExceptionReason.InvalidTuples);
						}
						Term[] tuple = ((Invocation)expression).Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
						return Model.Equal(tuple, value3);
					}
					if (head is ForeachSymbol)
					{
						int num = expr.Arity - 1;
						IterationTerm[] array = new IterationTerm[num];
						Set[] array2 = new Set[num];
						BuildForeachIters(expr, num, array, array2);
						Term term = ConvertSfsExpression(expr[expr.Arity - 1]);
						for (int num2 = num - 1; num2 >= 0; num2--)
						{
							Expression expression2 = expr[num2];
							term = new ForEachTerm(array[num2], array2[num2], term);
							_sfsMap.Remove(expression2[0]);
						}
						return term;
					}
					if (head is FilteredForeachSymbol)
					{
						int num3 = expr.Arity - 2;
						IterationTerm[] array3 = new IterationTerm[num3];
						Set[] array4 = new Set[num3];
						BuildForeachIters(expr, num3, array3, array4);
						Term term2 = ConvertSfsExpression(expr[expr.Arity - 1]);
						Term condExpression = ConvertSfsExpression(expr[expr.Arity - 2]);
						for (int num4 = num3 - 1; num4 >= 0; num4--)
						{
							Expression expression3 = expr[num4];
							term2 = ((num4 != num3 - 1) ? new ForEachTerm(array3[num4], array4[num4], term2) : new ForEachWhereTerm(array3[num4], array4[num4], term2, condExpression));
							_sfsMap.Remove(expression3[0]);
						}
						return term2;
					}
					if (head is Symbol)
					{
						Term term3 = TryGetSubmodelInstanceMember(invocation);
						if ((object)term3 != null)
						{
							return term3;
						}
					}
					if (head is Invocation instanceMemberExpr)
					{
						Term term4 = TryGetSubmodelInstanceMember(instanceMemberExpr);
						if ((object)term4 != null)
						{
							return ConvertIndexedTermExpression(term4, invocation);
						}
					}
					if (_sfsMap.TryGetValue(head, out var value4))
					{
						return ConvertIndexedTermExpression(value4, invocation);
					}
				}
				throw new ModelClauseException(Resources.ExpressionCannotBeConvertedToTerm, expr, OmlParseExceptionReason.ExpressionCannotBeConvertedIntoTerm);
			}

			private Term ConvertIndexedTermExpression(Term headTerm, Invocation invoc)
			{
				if (headTerm is IIndexable indexable)
				{
					Term[] array = invoc.Args.Select((Expression arg) => ConvertSfsExpression(arg)).ToArray();
					if (array.Length != indexable.IndexSets.Length)
					{
						throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.ExpectsExactly1IndexesBut2WereProvided, new object[3]
						{
							indexable.Name,
							indexable.IndexSets.Length,
							array.Length
						}), invoc, OmlParseExceptionReason.InvalidIndexCount);
					}
					if (array.Length > 0)
					{
						return new IndexTerm(indexable, headTerm._owningModel, array, indexable.DomainValueClass);
					}
					return headTerm;
				}
				throw new ModelClauseException(Resources.ExpressionCannotBeConvertedToTerm, invoc, OmlParseExceptionReason.ExpressionCannotBeConvertedIntoTerm);
			}

			private void BuildForeachIters(Expression expr, int iterCount, IterationTerm[] iterTerms, Set[] sets)
			{
				for (int i = 0; i < iterCount; i++)
				{
					Expression expression = expr[i];
					if (expression.Arity != 2 || !_sets.TryGetValue(expression[1], out sets[i]))
					{
						if (expression.Arity == 2 && expression[1].Head == Rewrite.Builtin.List)
						{
							Expression expression2 = expression[1];
							if (expression2.Arity < 1)
							{
								throw new ModelClauseException(Resources.ListMustHaveAtLeastOneElement, expression, OmlParseExceptionReason.InvalidArgumentCount);
							}
							Term[] array = new Term[expression2.Arity];
							for (int j = 0; j < expression2.Arity; j++)
							{
								array[j] = ConvertSfsExpression(expression2[j]);
							}
							sets[i] = new Set(array);
						}
						else
						{
							Term limit = 0.0;
							Term start = 0.0;
							Term step = 1.0;
							if (expression.Arity == 2)
							{
								limit = ConvertSfsExpression(expression[1]);
							}
							else if (expression.Arity >= 3)
							{
								start = ConvertSfsExpression(expression[1]);
								limit = ConvertSfsExpression(expression[2]);
							}
							if (expression.Arity >= 4)
							{
								step = ConvertSfsExpression(expression[3]);
							}
							sets[i] = new Set(start, limit, step);
						}
					}
					iterTerms[i] = new IterationTerm(ExprToName(expression[0]), sets[i].ItemValueClass, sets[i]._domain);
					_sfsMap[expression[0]] = iterTerms[i];
				}
			}

			private static void CheckInputLength(Expression expr, int length, int expected)
			{
				if (length != expected)
				{
					throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.Expected0ArgumentsButSaw1, new object[2] { expected, length }), expr, OmlParseExceptionReason.InvalidArgumentCount);
				}
			}

			private bool AddSfsGoal(Model sfsModel, Goal goal)
			{
				Term goal2;
				Expression expression;
				if (goal._expr.Head is RuleSymbol)
				{
					goal2 = ConvertSfsExpression(goal._expr[1]);
					expression = goal._expr[0];
				}
				else
				{
					goal2 = ConvertSfsExpression(goal._expr);
					expression = goal._expr;
				}
				sfsModel.AddGoal(expression.ToString(), goal._fMinimize ? GoalKind.Minimize : GoalKind.Maximize, goal2);
				return true;
			}
		}

		protected struct Goal
		{
			public Expression _expr;

			public bool _fMinimize;

			public Goal(Expression expr, bool fMinimize)
			{
				_expr = expr;
				_fMinimize = fMinimize;
			}
		}

		internal class ModelParser
		{
			internal List<Invocation> constraintsSections = new List<Invocation>();

			internal List<Invocation> decisionsSections = new List<Invocation>();

			internal List<Invocation> domainsSections = new List<Invocation>();

			internal List<Invocation> goalSections = new List<Invocation>();

			internal Invocation inputSection;

			internal List<Invocation> parametersSections = new List<Invocation>();

			internal HashSet<Symbol> submodelNames = new HashSet<Symbol>();

			internal List<KeyValuePair<ConcreteModel, ModelParser>> submodels = new List<KeyValuePair<ConcreteModel, ModelParser>>();

			internal List<Invocation> submodelSections = new List<Invocation>();

			public void ParseAndBuildModel(Invocation invModel, out ConcreteModel mod)
			{
				mod = ParseModel(invModel);
				BuildModel(mod);
			}

			public ConcreteModel ParseModel(Invocation invModel)
			{
				ConcreteModel concreteModel;
				if (!(invModel.Rewrite is SolveRewriteSystem solveRewriteSystem))
				{
					concreteModel = null;
					string expectedModel = Resources.ExpectedModel;
					Expression head = invModel.Head;
					throw new ModelClauseException(expectedModel, head);
				}
				concreteModel = new ConcreteModel(this, solveRewriteSystem, solveRewriteSystem.Builtin.Reals);
				if (invModel.Head != solveRewriteSystem.Builtin.Model)
				{
					string expectedModel = Resources.ExpectedModel;
					Expression head = invModel.Head;
					throw new ModelClauseException(expectedModel, head);
				}
				CollectModelSections(invModel, solveRewriteSystem);
				BuildConcreteSubmodels(concreteModel);
				ValidateModel(concreteModel);
				return concreteModel;
			}

			public ConcreteModel ParseSubmodel(Invocation invModel)
			{
				SolveRewriteSystem solveRewriteSystem = invModel.Rewrite as SolveRewriteSystem;
				ConcreteModel concreteModel = new ConcreteModel(this, solveRewriteSystem, solveRewriteSystem.Builtin.Reals);
				if (invModel.Head != solveRewriteSystem.Builtin.Model)
				{
					string expectedModel = Resources.ExpectedModel;
					Expression head = invModel.Head;
					throw new ModelClauseException(expectedModel, head);
				}
				CollectModelSections(invModel, solveRewriteSystem);
				BuildConcreteSubmodels(concreteModel);
				return concreteModel;
			}

			public void BuildModel(ConcreteModel mod)
			{
				mod.CreateValueTables(parametersSections);
				ValidateBindings(mod);
				ValidateOperators(mod);
				ExpandModel(mod);
			}

			private void CollectModelSections(Invocation invModel, SolveRewriteSystem rs)
			{
				for (int i = 0; i < invModel.Arity; i++)
				{
					if (!(invModel[i] is Invocation invocation))
					{
						throw new ModelClauseException(Resources.ExpectedAllowedSectionSymbols, invModel[i]);
					}
					Symbol symbol = invocation.Head as Symbol;
					if (symbol == rs.Builtin.Rule)
					{
						if (invocation.Arity != 2)
						{
							throw new ModelClauseException(Resources.OmlInvalidSubmodelClause, invocation, OmlParseExceptionReason.SubmodelError);
						}
						Symbol symbol2 = invocation[0] as Symbol;
						Invocation invocation2 = invocation[1] as Invocation;
						if (symbol2 == null || invocation2 == null)
						{
							throw new ModelClauseException(Resources.OmlInvalidSubmodelClause, invocation, OmlParseExceptionReason.SubmodelError);
						}
						if (submodelNames.Contains(symbol2))
						{
							throw new ModelClauseException(Resources.OmlInvalidSubmodelNameHasBeenTaken, symbol2, OmlParseExceptionReason.DuplicateName);
						}
						submodelSections.Add(invocation);
						submodelNames.Add(symbol2);
					}
					else if (symbol == rs.Builtin.Decisions)
					{
						if (invocation.Arity <= 1)
						{
							throw new ModelClauseException(Resources.InvalidVariablesClause, invocation, OmlParseExceptionReason.InvalidDecision);
						}
						decisionsSections.Add(invocation);
					}
					else if (symbol == rs.Builtin.Domains)
					{
						if (invocation.Arity != 2)
						{
							throw new ModelClauseException(Resources.InvalidVariablesClause, invocation, OmlParseExceptionReason.InvalidDomain);
						}
						domainsSections.Add(invocation);
					}
					else if (symbol == rs.Builtin.Goals)
					{
						AddGoalsToGoalsList(goalSections, invocation);
					}
					else if (symbol == rs.Builtin.Constraints)
					{
						constraintsSections.Add(invocation);
					}
					else if (symbol == rs.Builtin.Parameters)
					{
						parametersSections.Add(invocation);
					}
					else
					{
						if (symbol != rs.Builtin.InputSection)
						{
							throw new ModelClauseException(Resources.ExpectedAllowedSectionSymbols, invocation);
						}
						if (inputSection != null)
						{
							throw new ModelClauseException(Resources.ExpectedAllowedSectionSymbols, invocation.Head);
						}
						inputSection = invocation;
					}
				}
				if (decisionsSections.Count == 0)
				{
					throw new ModelClauseException(Resources.ModelShouldContainAtLeastOneDecisionsSection, null);
				}
			}

			private void BuildConcreteSubmodels(ConcreteModel model)
			{
				foreach (Invocation submodelSection in submodelSections)
				{
					Symbol key = submodelSection[0] as Symbol;
					Invocation invModel = submodelSection[1] as Invocation;
					ModelParser modelParser = new ModelParser();
					ConcreteModel concreteModel = modelParser.ParseSubmodel(invModel);
					model._mapSubmodelNameToConcreteModel.Add(key, concreteModel);
					submodels.Add(new KeyValuePair<ConcreteModel, ModelParser>(concreteModel, modelParser));
				}
				foreach (Invocation decisionsSection in decisionsSections)
				{
					if (!(decisionsSection[0] is Symbol symbol) || !submodelNames.Contains(symbol))
					{
						continue;
					}
					ConcreteModel value = model._mapSubmodelNameToConcreteModel[symbol];
					for (int i = 1; i < decisionsSection.Arity; i++)
					{
						if (!(decisionsSection[i] is Symbol key2) || model._mapSubmodelInstanceNameToConcreteModel.ContainsKey(key2))
						{
							throw new ModelClauseException(Resources.OmlInvalidSubmodelInstanceSymbolUnknownOrDuplicated, decisionsSection[i], OmlParseExceptionReason.SubmodelError);
						}
						model._mapSubmodelInstanceNameToConcreteModel.Add(key2, value);
					}
				}
			}

			private void ValidateModel(ConcreteModel mod)
			{
				List<Invocation> baseDomainsSections = domainsSections;
				ValidateSubmodel(mod, baseDomainsSections, isBaseModel: true);
			}

			private void ValidateSubmodel(ConcreteModel mod, List<Invocation> baseDomainsSections, bool isBaseModel)
			{
				foreach (Invocation baseDomainsSection in baseDomainsSections)
				{
					mod._validator.AnalyzeDomainsSection(baseDomainsSection);
				}
				foreach (Invocation domainsSection in domainsSections)
				{
					if (!isBaseModel)
					{
						throw new ModelClauseException(Resources.DomainsSectionIsNotAllowedInSubmodels, domainsSection, OmlParseExceptionReason.SubmodelError);
					}
				}
				foreach (Invocation submodelSection in submodelSections)
				{
					mod._validator.AnalyzeSubmodelSection(submodelSection);
				}
				foreach (KeyValuePair<ConcreteModel, ModelParser> submodel in submodels)
				{
					ConcreteModel key = submodel.Key;
					ModelParser value = submodel.Value;
					value.ValidateSubmodel(key, baseDomainsSections, isBaseModel: false);
				}
				foreach (Invocation parametersSection in parametersSections)
				{
					mod._validator.AnalyzeParametersSection(parametersSection);
				}
				foreach (Invocation decisionsSection in decisionsSections)
				{
					mod._validator.AnalyzeDecisionsSection(decisionsSection);
				}
				foreach (Invocation constraintsSection in constraintsSections)
				{
					mod._validator.AnalyzeConstraintsSection(constraintsSection);
				}
				foreach (Invocation goalSection in goalSections)
				{
					mod._validator.AnalyzeGoalsSection(goalSection);
				}
			}

			private void ValidateBindings(ConcreteModel mod)
			{
				if (inputSection != null && !mod.BindData(inputSection, out var strError, out var exprError))
				{
					throw new ModelClauseException(strError, exprError, OmlParseExceptionReason.InvalidDataBinding);
				}
				if ((inputSection == null && mod._paramsValueTables.Count > 0) || (inputSection != null && inputSection.Arity < mod._paramsValueTables.Count))
				{
					throw new ModelClauseException(Resources.AllBindableParametersShouldHaveBindClause, null, OmlParseExceptionReason.InvalidDataBinding);
				}
			}

			private void ValidateOperators(ConcreteModel mod)
			{
				mod._validator.ValidateOperators(constraintsSections, goalSections);
			}

			private void ExpandModel(ConcreteModel mod)
			{
				if (inputSection != null)
				{
					mod.SubsituteValueTables(constraintsSections, goalSections);
				}
				mod.ExpandAndAddVariablesToModel(decisionsSections);
				mod.ExpandAndAddGoalsToModel(goalSections);
				mod.ExpandAndAddConstraintsToModel(constraintsSections);
			}
		}

		internal Dictionary<Expression, Expression> _clausesSubstituteMap;

		private Dictionary<Expression, Domain> _domainCache = new Dictionary<Expression, Domain>();

		protected Expression _domDef;

		internal Dictionary<Symbol, ConcreteModel> _mapSubmodelInstanceNameToConcreteModel;

		internal Dictionary<Symbol, ConcreteModel> _mapSubmodelNameToConcreteModel;

		internal SfsModelExtractor _modelExtractor;

		protected Dictionary<Expression, Expression> _mpkeycon;

		protected Dictionary<Expression, Variable> _mpkeyvar;

		protected List<Variable> _mpvidvar;

		internal Dictionary<Symbol, ValueTableAdapter> _paramsValueTables;

		internal ModelParser _parser;

		protected List<Goal> _rggoal;

		protected SolveRewriteSystem _rs;

		internal Dictionary<Symbol, ValueSetAdapter> _sets;

		protected string _strGoalTemplate = "#Goal{0}";

		protected string _strRowTemplate = "#Row{0}";

		internal OmlValidator _validator;

		public SolveRewriteSystem Rewrite => _rs;

		public ConcreteModel(ModelParser parser, SolveRewriteSystem rs, Expression domDef)
		{
			_parser = parser;
			_rs = rs;
			if (domDef == null)
			{
				domDef = _rs.Builtin.Reals;
			}
			else
			{
				ValidateRs(domDef);
			}
			_validator = new OmlValidator(this);
			_domDef = domDef;
			_mpkeyvar = new Dictionary<Expression, Variable>(ExpressionComparer.Instance);
			_mpvidvar = new List<Variable>();
			_mpkeycon = new Dictionary<Expression, Expression>(ExpressionComparer.Instance);
			_rggoal = new List<Goal>();
			_mapSubmodelNameToConcreteModel = new Dictionary<Symbol, ConcreteModel>();
			_mapSubmodelInstanceNameToConcreteModel = new Dictionary<Symbol, ConcreteModel>();
			_sets = new Dictionary<Symbol, ValueSetAdapter>();
			_paramsValueTables = new Dictionary<Symbol, ValueTableAdapter>();
			_clausesSubstituteMap = new Dictionary<Expression, Expression>();
		}

		protected void ValidateRs(Expression expr)
		{
			if (expr.Rewrite != _rs)
			{
				throw new InvalidOperationException(Resources.ExpressionFromWrongRewriteSystem);
			}
		}

		public static bool ParseModel(Invocation invModel, out ConcreteModel mod)
		{
			try
			{
				ModelParser modelParser = new ModelParser();
				mod = modelParser.ParseModel(invModel);
				modelParser.BuildModel(mod);
				return true;
			}
			catch (ModelClauseException ex)
			{
				LogGeneralError(invModel, ex.Expr, ex.Message);
				mod = null;
				return false;
			}
		}

		private static void LogGeneralError(Invocation invModel, Expression exprError, string strError)
		{
			string text = "";
			if (exprError != null && exprError.PlacementInformation != null && !(exprError is Symbol))
			{
				exprError.PlacementInformation.Map.MapSpanToPos(exprError.PlacementInformation.Span, out var spos);
				text = string.Format(CultureInfo.InvariantCulture, "({0},{1})-({2},{3}): ", spos.lineMin, spos.colMin, spos.lineLim, spos.colLim);
			}
			text = ((exprError != null) ? (text + string.Format(CultureInfo.InvariantCulture, Resources.ParsingModelFailed01, new object[2] { strError, exprError })) : (text + string.Format(CultureInfo.InvariantCulture, Resources.ParsingModelFailed0, new object[1] { strError })));
			invModel.Rewrite.Log(text);
		}

		/// <summary>
		/// Verify that all args are Minimize Or maximize and add them
		/// </summary>
		/// <param name="goalsSections"></param>
		/// <param name="goalInvocation"></param>
		private static void AddGoalsToGoalsList(List<Invocation> goalsSections, Invocation goalInvocation)
		{
			for (int i = 0; i < goalInvocation.Arity; i++)
			{
				if (goalInvocation[i].FirstSymbolHead != goalInvocation.Rewrite.Builtin.Minimize && goalInvocation[i].FirstSymbolHead != goalInvocation.Rewrite.Builtin.Maximize)
				{
					if (goalInvocation[i] is Invocation)
					{
						throw new ModelClauseException(Resources.GoalsSectionCanContainsOnlyMinimizeOrMaximize, goalInvocation[i], OmlParseExceptionReason.InvalidGoal);
					}
					throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.GoalsSectionCanContainsOnlyMinimizeOrMaximizeAndNot0, new object[1] { goalInvocation[i] }), goalInvocation, OmlParseExceptionReason.InvalidGoal);
				}
				if (!(goalInvocation[i] is Invocation item))
				{
					throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.HasToBeInvocation0, new object[1] { goalInvocation[i] }), goalInvocation, OmlParseExceptionReason.InvalidGoal);
				}
				goalsSections.Add(item);
			}
		}

		/// <summary>
		/// Makes a Substitution for the ValueSets and ValueTables and apply to goals and constraints
		/// </summary>
		/// <param name="constraintsSections">section of constraints of the model</param>
		/// <param name="goalSections">section of goal(Minimize/Maximize) of the model</param>
		public void SubsituteValueTables(List<Invocation> constraintsSections, List<Invocation> goalSections)
		{
			Substitution substitution = new Substitution();
			foreach (KeyValuePair<Symbol, ValueTableAdapter> paramsValueTable in _paramsValueTables)
			{
				substitution.Add(paramsValueTable.Key, paramsValueTable.Value);
			}
			foreach (KeyValuePair<Symbol, ValueSetAdapter> set in _sets)
			{
				substitution.Add(set.Key, set.Value);
			}
			for (int i = 0; i < constraintsSections.Count; i++)
			{
				Expression expression = substitution.Apply(constraintsSections[i]);
				for (int j = 0; j < constraintsSections[i].Arity; j++)
				{
					_clausesSubstituteMap.Add(expression[j], constraintsSections[i][j]);
				}
				constraintsSections[i] = expression as Invocation;
			}
			for (int k = 0; k < goalSections.Count; k++)
			{
				Expression expression = substitution.Apply(goalSections[k]);
				for (int l = 0; l < goalSections[k].Arity; l++)
				{
					_clausesSubstituteMap.Add(expression[l], goalSections[k][l]);
				}
				goalSections[k] = expression as Invocation;
			}
		}

		/// <summary>
		/// Go over the parameters sections and create the needed ValueSets and ValueTables
		/// </summary>
		/// <param name="paramsSections">section of parameters of the model</param>
		/// <returns>true if succeeded, false otherwise</returns>
		public void CreateValueTables(List<Invocation> paramsSections)
		{
			foreach (Invocation paramsSection in paramsSections)
			{
				if (paramsSection.Arity <= 1 || paramsSection[0].FirstSymbolHead != Rewrite.Builtin.Sets)
				{
					continue;
				}
				Domain domain;
				if (paramsSection[0] is Invocation invocation)
				{
					if (invocation.Arity != 1)
					{
						throw new ModelClauseException(Resources.OnlyOneDomainForSetsSection, paramsSection[0], OmlParseExceptionReason.InvalidDomain);
					}
					if (!IsDomain(invocation[0]))
					{
						throw new ModelClauseException(Resources.CanNotFindDomain, invocation[0], OmlParseExceptionReason.InvalidDomain);
					}
					domain = DomainFromExpression(invocation[0]);
				}
				else
				{
					domain = Domain.Any;
				}
				int i = 1;
				try
				{
					for (i = 1; i < paramsSection.Arity; i++)
					{
						if (IsReservedSymbol(paramsSection[i].FirstSymbolHead))
						{
							if (paramsSection[i] is Invocation)
							{
								throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolIsReserved0, new object[1] { paramsSection[i].FirstSymbolHead }), paramsSection[i], OmlParseExceptionReason.InvalidName);
							}
							throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolIsReserved0, new object[1] { paramsSection[i].FirstSymbolHead }), paramsSection, OmlParseExceptionReason.InvalidName);
						}
						if (paramsSection[i] is Invocation)
						{
							throw new ModelClauseException(Resources.InvocationCannotBeUsedAsSetDeclaration, paramsSection[i], OmlParseExceptionReason.InvalidSet);
						}
						ValueSetAdapter valueSetAdapter = new ValueSetAdapter(Rewrite, domain);
						valueSetAdapter.PlacementInformation = paramsSection[i].PlacementInformation;
						_sets.Add(paramsSection[i].FirstSymbolHead, valueSetAdapter);
					}
				}
				catch (ArgumentException)
				{
					throw new ModelClauseException(Resources.DuplicatedSet, paramsSection[i], OmlParseExceptionReason.DuplicateName);
				}
			}
			foreach (Invocation paramsSection2 in paramsSections)
			{
				if (paramsSection2.Arity <= 0 || paramsSection2[0].FirstSymbolHead == Rewrite.Builtin.Sets)
				{
					continue;
				}
				int j = 0;
				try
				{
					Domain domain2;
					if (IsDomain(paramsSection2[0]))
					{
						domain2 = DomainFromExpression(paramsSection2[0]);
						if (domain2 == Domain.Any)
						{
							throw new ModelClauseException(Resources.CanNotSpecifyDomainAnyToParameter, paramsSection2[0], OmlParseExceptionReason.InvalidDomain);
						}
						j = 1;
					}
					else
					{
						domain2 = Domain.Real;
						j = 0;
					}
					for (; j < paramsSection2.Arity; j++)
					{
						if (IsReservedSymbol(paramsSection2[j].FirstSymbolHead) && paramsSection2[j].FirstSymbolHead != Rewrite.Builtin.Foreach && paramsSection2[j].FirstSymbolHead != Rewrite.Builtin.Set && IsReservedSymbol(paramsSection2[j].FirstSymbolHead))
						{
							if (paramsSection2[j] is Invocation)
							{
								throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolIsReserved0, new object[1] { paramsSection2[j].FirstSymbolHead }), paramsSection2[j], OmlParseExceptionReason.InvalidName);
							}
							throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolIsReserved0, new object[1] { paramsSection2[j].FirstSymbolHead }), paramsSection2, OmlParseExceptionReason.InvalidName);
						}
						if (paramsSection2[j] is Invocation invocation2)
						{
							if (invocation2.FirstSymbolHead == Rewrite.Builtin.Foreach)
							{
								if (IsForeachOverSets(paramsSection2[j]))
								{
									throw new ModelClauseException(Resources.ProhibitedForeachInParameters, paramsSection2[j], OmlParseExceptionReason.InvalidParameter);
								}
								Expression expression = invocation2.Evaluate();
								if (expression.FirstSymbolHead == Rewrite.Builtin.Foreach)
								{
									throw new ModelClauseException(Resources.GeneralWrongForeach, invocation2);
								}
								Invocation invocation3 = expression as Invocation;
								for (int k = 0; k < invocation3.Arity; k++)
								{
									if (!CanBeAssignedToParameter(invocation3[k], domain2))
									{
										throw new ModelClauseException(Resources.ParameterShouldBeAssignConstantOfItsDomain, invocation2, OmlParseExceptionReason.InvalidParameter);
									}
								}
								continue;
							}
							if (paramsSection2[j].FirstSymbolHead == Rewrite.Builtin.Set)
							{
								if (invocation2.Arity != 2)
								{
									throw new ModelClauseException(Resources.WrongParameterAssignClause, invocation2, OmlParseExceptionReason.InvalidParameter);
								}
								if (!CanBeAssignedToParameter(invocation2[1], domain2))
								{
									throw new ModelClauseException(Resources.ParameterShouldBeAssignConstantOfItsDomain, invocation2, OmlParseExceptionReason.InvalidParameter);
								}
								paramsSection2[j].Evaluate();
								continue;
							}
							if (invocation2.Head != invocation2.FirstSymbolHead)
							{
								throw new ModelClauseException(Resources.WrongSyntaxParameterTwoInvocation, paramsSection2[j], OmlParseExceptionReason.InvalidArgumentCount);
							}
							ValueSetAdapter[] array = new ValueSetAdapter[invocation2.Arity];
							for (int l = 0; l < invocation2.Arity; l++)
							{
								if (!_sets.TryGetValue(invocation2[l].FirstSymbolHead, out array[l]))
								{
									throw new ModelClauseException(Resources.NoSuchSet, invocation2[l], OmlParseExceptionReason.InvalidSet);
								}
							}
							ValueTableAdapter valueTableAdapter = new ValueTableAdapter(Rewrite, domain2, array);
							valueTableAdapter.PlacementInformation = paramsSection2[j].PlacementInformation;
							_paramsValueTables.Add(paramsSection2[j].FirstSymbolHead, valueTableAdapter);
							continue;
						}
						throw new ModelClauseException(Resources.ParameterShouldUseSetsOrBeAssignedConstant, paramsSection2[j], OmlParseExceptionReason.InvalidParameter);
					}
				}
				catch (ArgumentException)
				{
					throw new ModelClauseException(Resources.DuplicateParameter, paramsSection2[j], OmlParseExceptionReason.DuplicateName);
				}
			}
		}

		/// <summary>
		/// Return true if the expression is constant, false otherwise.
		/// </summary>
		/// <param name="expression"></param>
		/// <param name="domain"></param>
		/// <returns>true if Constant false otherwize</returns>
		private static bool CanBeAssignedToParameter(Expression expression, Domain domain)
		{
			if (expression is Constant)
			{
				if (domain == Domain.Any)
				{
					return true;
				}
				if (domain == Domain.Boolean)
				{
					return expression is BooleanConstant;
				}
				expression.GetNumericValue(out var val);
				return domain.IsValidValue(val);
			}
			return false;
		}

		/// <summary>
		/// Calls for the delegate which suppose to bind the data to the ValueTabres parameters
		/// </summary>
		/// <param name="inputSection">section of Input of the model</param>
		/// <param name="strError">out string to be used when error</param>
		/// <param name="exprError">out expression to be used when error</param>
		/// <returns>true if succeeded, false otherwise</returns>
		public bool BindData(Invocation inputSection, out string strError, out Expression exprError)
		{
			int ivSub = 0;
			if (inputSection.Arity > 0 && inputSection[0].FirstSymbolHead == Rewrite.Builtin.ExcelInputType)
			{
				ivSub = 1;
			}
			return ExcelBindData(inputSection, ivSub, out strError, out exprError);
		}

		/// <summary>
		/// Calls the excel delegate which suppose to bind the data to the ValueTabres parameters 
		/// </summary>
		/// <param name="inputSection">section of Input of the model</param>
		/// <param name="ivSub">first bind clause in Invocation</param>
		/// <param name="strError">out string to be used when error</param>
		/// <param name="exprError">out expression to be used when error</param>
		/// <returns>true if succeeded, false otherwise</returns>
		public bool ExcelBindData(Invocation inputSection, int ivSub, out string strError, out Expression exprError)
		{
			HashSet<Symbol> hashSet = new HashSet<Symbol>();
			if (Rewrite.BindParamDelegate == null)
			{
			}
			while (ivSub < inputSection.Arity)
			{
				if (inputSection[ivSub].FirstSymbolHead != Rewrite.Builtin.BindIn)
				{
					strError = Resources.OnlyBinddataClausesAllowed;
					exprError = inputSection[ivSub];
					return false;
				}
				if (!(inputSection[ivSub] is Invocation invocation))
				{
					strError = Resources.BinddataClausesShouldHaveParameterAndInput;
					exprError = inputSection[ivSub];
					return false;
				}
				if (invocation.Arity != 2)
				{
					strError = Resources.OneOnlyAddressAllowed;
					exprError = invocation;
					return false;
				}
				if (!_paramsValueTables.TryGetValue(invocation[0].FirstSymbolHead, out var value))
				{
					strError = Resources.OnlyParametersDeclaredOnParametersSectionCanBeBound;
					exprError = invocation[0];
					return false;
				}
				if (!hashSet.Add(invocation[0].FirstSymbolHead))
				{
					strError = Resources.SameParametersBoundMoreThanOnce;
					exprError = invocation[0];
					return false;
				}
				string[] array = new string[invocation[0].Arity];
				for (int i = 0; i < invocation[0].Arity; i++)
				{
					array[i] = invocation[0][i].ToString();
				}
				try
				{
					invocation[1].GetValue(out string val);
					Rewrite.BindParamDelegate(value, val, array);
				}
				catch (InvalidModelDataException ex)
				{
					strError = ex.Message;
					exprError = invocation;
					return false;
				}
				ivSub++;
			}
			strError = null;
			exprError = null;
			return true;
		}

		/// <summary>
		/// Expand each constraint and add to the model
		/// </summary>
		/// <param name="constraintsSections">Constraints sections of the model</param>
		public void ExpandAndAddConstraintsToModel(List<Invocation> constraintsSections)
		{
			Rewrite.Builtin.Constraints.RemoveAttributes(Rewrite.Attributes.HoldAll);
			foreach (Invocation constraintsSection in constraintsSections)
			{
				Expression[] array = new Expression[constraintsSection.Arity];
				for (int i = 0; i < constraintsSection.Arity; i++)
				{
					Expression expression = EvaluateClauseFromSection(constraintsSection[i]);
					array[i] = expression;
				}
				Invocation invocation = new Invocation(constraintsSection.FirstSymbolHead, fCanOwnArray: true, array);
				invocation = invocation.Evaluate() as Invocation;
				for (int j = 0; j < invocation.Arity; j++)
				{
					AddConstraint(fEvaluate: false, invocation[j]);
				}
			}
		}

		/// <summary>
		/// Expand each goal and add to the model
		/// Remark: the order of the goals matters
		/// </summary>
		/// <param name="goalSections">Minimize/Maximize sections of the model</param>
		public void ExpandAndAddGoalsToModel(List<Invocation> goalSections)
		{
			Rewrite.Builtin.Maximize.RemoveAttributes(Rewrite.Attributes.HoldAll);
			Rewrite.Builtin.Minimize.RemoveAttributes(Rewrite.Attributes.HoldAll);
			foreach (Invocation goalSection in goalSections)
			{
				Expression[] array = new Expression[goalSection.Arity];
				for (int i = 0; i < goalSection.Arity; i++)
				{
					Expression expression = EvaluateClauseFromSection(goalSection[i]);
					array[i] = expression;
				}
				Invocation invocation = new Invocation(goalSection.FirstSymbolHead, fCanOwnArray: true, array);
				invocation = invocation.Evaluate() as Invocation;
				Symbol symbol = invocation.Head as Symbol;
				if (symbol == goalSection.Rewrite.Builtin.Minimize)
				{
					for (int j = 0; j < invocation.Arity; j++)
					{
						Minimize(fEvaluate: false, invocation[j]);
					}
				}
				else
				{
					for (int k = 0; k < invocation.Arity; k++)
					{
						Maximize(fEvaluate: false, invocation[k]);
					}
				}
			}
		}

		private Expression EvaluateClauseFromSection(Expression clause)
		{
			Expression expression;
			try
			{
				expression = clause.Evaluate();
			}
			catch (ModelClauseException ex)
			{
				if (ex.Expr is ValueTableAdapter)
				{
					foreach (KeyValuePair<Symbol, ValueTableAdapter> paramsValueTable in _paramsValueTables)
					{
						if (paramsValueTable.Value == ex.Expr)
						{
							Expression key = paramsValueTable.Key;
							key.PlacementInformation = null;
							string strMsg = string.Format(CultureInfo.InvariantCulture, Resources.IndexWrongForParameter01, new object[2] { ex.Message, key });
							throw new ModelClauseException(strMsg, _clausesSubstituteMap[clause], OmlParseExceptionReason.InvalidIndex);
						}
					}
				}
				throw;
			}
			_validator.ValidateSymbols(null, expression);
			return expression;
		}

		/// <summary>
		/// Expand variables which are "set-indexed" and add all variables to the model
		/// </summary>
		/// <param name="decisionsSections">list of all Decisions sections</param>
		/// <returns>false if something is wrong, true otherwise</returns>
		public void ExpandAndAddVariablesToModel(List<Invocation> decisionsSections)
		{
			foreach (Invocation decisionsSection in decisionsSections)
			{
				Expression expression = decisionsSection[0];
				expression = expression.Evaluate();
				for (int i = 1; i < decisionsSection.Arity; i++)
				{
					Expression expression2 = decisionsSection[i];
					if (expression2.FirstSymbolHead == Rewrite.Builtin.Foreach)
					{
						AddVariableForeachToModel(decisionsSection, expression2, expression);
					}
					else
					{
						AddVariableToModel(decisionsSection, expression2, expression);
					}
				}
			}
		}

		private void RecordDecisions(Expression[] spliceVars, Expression exprDom, Invocation closestInvocation)
		{
			foreach (Expression expression in spliceVars)
			{
				if (!AddVariable(expression, exprDom, out var _, out var fNew))
				{
					OmlValidator.MakeModelClauseException(Resources.InvalidVariable, Resources.InvalidVariable0, expression, closestInvocation);
				}
				if (!fNew)
				{
					OmlValidator.MakeModelClauseException(Resources.DuplicateVariable, Resources.DuplicateVariable0, expression, closestInvocation);
				}
				RecordDecision(expression);
			}
		}

		private void AddVariableForeachToModel(Invocation decisionsSection, Expression expressionVar, Expression exprDom)
		{
			if (IsForeachOverSets(expressionVar))
			{
				throw new ModelClauseException(Resources.ProhibitedForeachInDecisions, expressionVar, OmlParseExceptionReason.InvalidDecision);
			}
			Expression expression = expressionVar.Evaluate();
			if (expression.FirstSymbolHead == Rewrite.Builtin.Foreach)
			{
				throw new ModelClauseException(Resources.GeneralWrongForeach, expressionVar);
			}
			if (expression.Head != Rewrite.Builtin.ArgumentSplice)
			{
				expression = Rewrite.Builtin.ArgumentSplice.Invoke(expression);
			}
			Invocation invocation = expression as Invocation;
			foreach (Expression arg in invocation.Args)
			{
				if (arg.Head is ForeachSymbol || arg.Head is FilteredForeachSymbol)
				{
					AddVariableForeachToModel(decisionsSection, arg, exprDom);
				}
				else
				{
					AddVariableToModel(decisionsSection, arg, exprDom);
				}
			}
		}

		private void AddVariableToModel(Invocation decisionsSection, Expression expressionVar, Expression exprDom)
		{
			Invocation invocation = expressionVar as Invocation;
			if (IsReservedSymbol(expressionVar.FirstSymbolHead))
			{
				if (invocation != null)
				{
					throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolIsReserved0, new object[1] { expressionVar.FirstSymbolHead }), expressionVar, OmlParseExceptionReason.InvalidDecision);
				}
				throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolIsReserved0, new object[1] { expressionVar.FirstSymbolHead }), decisionsSection, OmlParseExceptionReason.InvalidDecision);
			}
			Expression[] spliceVars;
			if (invocation == null)
			{
				spliceVars = new Expression[1] { expressionVar };
			}
			else
			{
				int arity = invocation.Arity;
				Expression[] array = new Expression[arity];
				List<Expression> list = new List<Expression>();
				for (int i = 0; i < invocation.Arity; i++)
				{
					if (_sets.ContainsKey(invocation[i].FirstSymbolHead))
					{
						array[i] = new Symbol(Rewrite, null, "iter`" + invocation[i].FirstSymbolHead.Name);
						list.Add(Rewrite.Builtin.List.Invoke(array[i], invocation[i].FirstSymbolHead));
					}
					else
					{
						array[i] = invocation[i].Evaluate();
					}
				}
				Substitution substitution = new Substitution();
				foreach (KeyValuePair<Symbol, ValueSetAdapter> set in _sets)
				{
					substitution.Add(set.Key, set.Value);
				}
				list.Add(invocation.FirstSymbolHead.Invoke(array));
				Invocation invocation2 = substitution.Apply(Rewrite.Builtin.Foreach.Invoke(list.ToArray())).Evaluate() as Invocation;
				spliceVars = ((!(invocation2.Head is ArgumentSpliceSymbol)) ? new Expression[1] { invocation2 } : invocation2.ArgsArray);
			}
			RecordDecisions(spliceVars, exprDom, decisionsSection);
		}

		private void RecordDecision(Expression oneDecision)
		{
			_validator._decisionsSymbols[oneDecision.FirstSymbolHead] = oneDecision.Arity;
		}

		internal bool IsReservedSymbol(Symbol sym)
		{
			return sym.HasAttribute(_rs.Attributes.ValuesLocked);
		}

		/// <summary>
		/// Checks Foreach statement
		/// Can be used before or after substitue
		/// </summary>
		/// <param name="foreachExp"></param>
		/// <returns></returns>
		private bool IsForeachOverSets(Expression foreachExp)
		{
			bool result = false;
			if (!(foreachExp is Invocation invocation) || invocation.Arity < 2)
			{
				throw new ModelClauseException(Resources.GeneralWrongForeach, foreachExp);
			}
			for (int i = 0; i < invocation.Arity - 1; i++)
			{
				if (!(invocation[i] is Invocation invocation2))
				{
					throw new ModelClauseException(Resources.BadIterator, invocation[i], OmlParseExceptionReason.InvalidIterator);
				}
				Expression expression = invocation2[invocation2.Arity - 1];
				if (expression is Invocation)
				{
					expression.Evaluate();
				}
				if (expression is ValueSetAdapter || _sets.ContainsKey(expression.FirstSymbolHead))
				{
					result = true;
				}
			}
			return result;
		}

		/// <summary>
		/// Check if the expression seems to be domain expression
		/// </summary>
		/// <param name="dom"></param>
		/// <returns>true if the expression looks like domain expression, false otherwise</returns>
		private bool IsDomain(Expression dom)
		{
			if (dom.FirstSymbolHead == Rewrite.Builtin.Any || dom.FirstSymbolHead == Rewrite.Builtin.Reals || dom.FirstSymbolHead == Rewrite.Builtin.Booleans || dom.FirstSymbolHead == Rewrite.Builtin.Integers || dom.FirstSymbolHead == Rewrite.Builtin.Enum)
			{
				return true;
			}
			return false;
		}

		/// <summary> Gets Domain from an Expression
		/// </summary>
		/// <param name="dom">domain expression</param>
		/// <returns>null if something is wrong, the new domain otherwise</returns>
		public Domain DomainFromExpression(Expression dom)
		{
			return DomainFromExpression(dom, null);
		}

		/// <summary> Gets Domain from an Expression
		/// </summary>
		/// <param name="dom">domain expression</param>
		/// <param name="exprName">Name expression (for user-named domains such as Enum)</param>
		/// <returns>null if something is wrong, the new domain otherwise</returns>
		public Domain DomainFromExpression(Expression dom, Symbol exprName)
		{
			Model submodel;
			Domain domain = DomainFromExpression(dom, exprName, out submodel);
			if (submodel != null || domain == null)
			{
				string thisCanNotExpressDomain = Resources.ThisCanNotExpressDomain;
				throw new ModelClauseException(thisCanNotExpressDomain, dom, OmlParseExceptionReason.InvalidDomain);
			}
			return domain;
		}

		/// <summary> Gets Domain from an Expression
		/// </summary>
		/// <param name="dom">domain expression</param>
		/// <param name="submodel">submodel object if the domain expression is actually a submodel name</param>
		/// <returns>the SFS domain; null if the domain expression actually refers to a submodel; 
		/// something is wrong if both submodel is null and returns null</returns>
		public Domain DomainFromExpression(Expression dom, out Model submodel)
		{
			return DomainFromExpression(dom, null, out submodel);
		}

		/// <summary>
		/// Gets Domain from an Expression
		/// REVIEW shahark: for now this method dosn't deal with list of strings, it does not clear on the side of 
		/// the ValueSets and ValueTable if those are really fully support.
		/// </summary>
		/// <param name="dom">domain expression</param>
		/// <param name="exprName">Name expression (for user-named domains such as Enum)</param>
		/// <param name="submodel">submodel object if the domain expression is actually a submodel name</param>
		/// <returns>the SFS domain; null if the domain expression actually refers to a submodel; 
		/// something is wrong if both submodel is null and returns null</returns>
		public Domain DomainFromExpression(Expression dom, Symbol exprName, out Model submodel)
		{
			submodel = null;
			if (_domainCache.TryGetValue(dom, out var value))
			{
				return value;
			}
			Invocation invocation = dom as Invocation;
			if (dom.FirstSymbolHead == Rewrite.Builtin.Any)
			{
				if (dom != Rewrite.Builtin.Any)
				{
					string strMsg = string.Format(CultureInfo.InvariantCulture, Resources.DomainCanNotHaveBundaries0, new object[1] { Rewrite.Builtin.Any.Name });
					throw new ModelClauseException(strMsg, dom, OmlParseExceptionReason.InvalidDomain);
				}
				return Domain.Any;
			}
			if (dom.FirstSymbolHead == Rewrite.Builtin.Reals)
			{
				if (dom == Rewrite.Builtin.Reals)
				{
					return Domain.Real;
				}
			}
			else
			{
				if (dom.FirstSymbolHead == Rewrite.Builtin.Booleans)
				{
					if (dom == Rewrite.Builtin.Booleans)
					{
						return Domain.Boolean;
					}
					string strMsg2 = string.Format(CultureInfo.InvariantCulture, Resources.DomainCanNotHaveBundaries0, new object[1] { Rewrite.Builtin.Booleans.Name });
					throw new ModelClauseException(strMsg2, dom, OmlParseExceptionReason.InvalidDomain);
				}
				if (dom.FirstSymbolHead == Rewrite.Builtin.Integers)
				{
					if (dom == Rewrite.Builtin.Integers)
					{
						return Domain.Integer;
					}
				}
				else
				{
					if (dom.FirstSymbolHead == Rewrite.Builtin.UniformDistribution || dom.FirstSymbolHead == Rewrite.Builtin.NormalDistribution || dom.FirstSymbolHead == Rewrite.Builtin.DiscreteUniformDistribution || dom.FirstSymbolHead == Rewrite.Builtin.ExponentialDistribution || dom.FirstSymbolHead == Rewrite.Builtin.GeometricDistribution || dom.FirstSymbolHead == Rewrite.Builtin.BinomialDistribution || dom.FirstSymbolHead == Rewrite.Builtin.LogNormalDistribution)
					{
						return Domain.DistributedValue;
					}
					if (_modelExtractor != null && _modelExtractor._sfsSubmodels.TryGetValue(dom.FirstSymbolHead, out submodel))
					{
						return null;
					}
					if (invocation == null || invocation.Head != Rewrite.Builtin.Enum)
					{
						string thisCanNotExpressDomain = Resources.ThisCanNotExpressDomain;
						throw new ModelClauseException(thisCanNotExpressDomain, dom, OmlParseExceptionReason.InvalidDomain);
					}
				}
			}
			if (invocation.Head == Rewrite.Builtin.Enum)
			{
				if (invocation.Arity == 1 && invocation[0].Head == Rewrite.Builtin.List)
				{
					string[] array = new string[invocation[0].Arity];
					for (int i = 0; i < invocation[0].Arity; i++)
					{
						if (!invocation[0][i].GetValue(out array[i]))
						{
							string listContainsANonStringValue = Resources.ListContainsANonStringValue;
							throw new ModelClauseException(listContainsANonStringValue, dom, OmlParseExceptionReason.InvalidArgumentType);
						}
					}
					if (array.Length == 0)
					{
						throw new ModelClauseException(Resources.EnumDomainMustHaveAtLeastOneElement, dom, OmlParseExceptionReason.InvalidArgumentCount);
					}
					Domain domain = Domain.Enum(array);
					if (exprName != null)
					{
						domain.Name = SfsModelExtractor.ExprToName(exprName);
					}
					_domainCache[dom] = domain;
					return domain;
				}
				string wrongSyntaxForDomain = Resources.WrongSyntaxForDomain;
				throw new ModelClauseException(wrongSyntaxForDomain, dom, OmlParseExceptionReason.InvalidDomain);
			}
			DebugContracts.NonNull(invocation);
			if (invocation.Head != invocation.FirstSymbolHead)
			{
				string wrongSyntaxForDomain2 = Resources.WrongSyntaxForDomain;
				throw new ModelClauseException(wrongSyntaxForDomain2, dom, OmlParseExceptionReason.InvalidDomain);
			}
			if (invocation.Arity == 1 && invocation[0].Head == Rewrite.Builtin.List)
			{
				Rational[] array2 = new Rational[invocation[0].Arity];
				for (int j = 0; j < invocation[0].Arity; j++)
				{
					if (!invocation[0][j].GetNumericValue(out array2[j]))
					{
						string listContainsNonNumericValue = Resources.ListContainsNonNumericValue;
						throw new ModelClauseException(listContainsNonNumericValue, dom, OmlParseExceptionReason.InvalidDomain);
					}
				}
				Domain domain2 = Domain.Set(array2);
				_domainCache[dom] = domain2;
				return domain2;
			}
			if (invocation.Arity != 2)
			{
				string rangeOfDomainWrongNumberOfBoundaries = Resources.RangeOfDomainWrongNumberOfBoundaries;
				throw new ModelClauseException(rangeOfDomainWrongNumberOfBoundaries, dom, OmlParseExceptionReason.InvalidDomain);
			}
			if (!invocation[0].GetNumericValue(out var val) || !invocation[1].GetNumericValue(out var val2))
			{
				string bundariesAreNotNumeric = Resources.BundariesAreNotNumeric;
				throw new ModelClauseException(bundariesAreNotNumeric, dom, OmlParseExceptionReason.InvalidDomain);
			}
			if (val > val2)
			{
				string minimumBundaryBiggerThanMaximumBundary = Resources.MinimumBundaryBiggerThanMaximumBundary;
				throw new ModelClauseException(minimumBundaryBiggerThanMaximumBundary, dom, OmlParseExceptionReason.InvalidDomain);
			}
			if (dom.FirstSymbolHead == Rewrite.Builtin.Integers)
			{
				val = val.GetCeiling();
				val2 = val2.GetFloor();
				if (val > val2)
				{
					string strMsg3 = string.Format(CultureInfo.InvariantCulture, Resources.MinimumBundaryBiggerThanMaximumBundaryAfterRound01, new object[2] { val, val2 });
					throw new ModelClauseException(strMsg3, dom, OmlParseExceptionReason.InvalidDomain);
				}
				Domain domain3 = Domain.IntegerRange(val, val2);
				_domainCache[dom] = domain3;
				return domain3;
			}
			Domain domain4 = Domain.RealRange(val, val2);
			_domainCache[dom] = domain4;
			return domain4;
		}

		public bool AddVariable(Expression key, Expression dom, out Variable var, out bool fNew)
		{
			ValidateRs(key);
			if (dom == null)
			{
				dom = _domDef;
			}
			else
			{
				ValidateRs(dom);
			}
			Domain domain = DomainFromExpression(dom);
			return AddVariable(key, domain, out var, out fNew);
		}

		private bool AddVariable(Expression key, Domain domain, out Variable var, out bool fNew)
		{
			string text = null;
			if (key.FirstSymbolHead == Rewrite.Builtin.BindOut)
			{
				if (key.Arity != 2)
				{
					var = null;
					fNew = false;
					return false;
				}
				if (key[1].FirstSymbolHead != Rewrite.Builtin.String)
				{
					var = null;
					fNew = false;
					return false;
				}
				text = key[1].ToString();
				key = key[0];
			}
			if (key.FirstSymbolHead.HasAttribute(_rs.Attributes.ValuesLocked))
			{
				var = null;
				fNew = false;
				return false;
			}
			if (_mpkeyvar.TryGetValue(key, out var))
			{
				var.Domain = domain;
				fNew = false;
			}
			else
			{
				var = new Variable(key, domain, _mpkeyvar.Count);
				_mpkeyvar.Add(key, var);
				_mpvidvar.Add(var);
				fNew = true;
				if (!string.IsNullOrEmpty(text))
				{
					var.DataOut = text;
				}
			}
			return true;
		}

		public Variable GetVariable(int vid)
		{
			return _mpvidvar[vid];
		}

		public void AddConstraint(bool fEvaluate, Expression con)
		{
			ValidateRs(con);
			Expression expression = (fEvaluate ? con.Evaluate() : con);
			_mpkeycon[expression] = expression;
		}

		public void AddConstraints(bool fEvaluate, params Expression[] rgcon)
		{
			foreach (Expression expression in rgcon)
			{
				ValidateRs(expression);
				Expression expression2 = (fEvaluate ? expression.Evaluate() : expression);
				_mpkeycon[expression2] = expression2;
			}
		}

		public void Maximize(bool fEvaluate, Expression expr)
		{
			ValidateRs(expr);
			Goal item = new Goal(fEvaluate ? expr.Evaluate() : expr, fMinimize: false);
			_rggoal.Add(item);
		}

		public void Minimize(bool fEvaluate, Expression expr)
		{
			ValidateRs(expr);
			Goal item = new Goal(fEvaluate ? expr.Evaluate() : expr, fMinimize: true);
			_rggoal.Add(item);
		}

		internal bool TryGetSfsModel(Model sfsModel, SolverContext context, out Dictionary<Expression, Term> sfsMap, out string strError, out Expression exprError)
		{
			SfsModelExtractor sfsModelExtractor = new SfsModelExtractor(this, context);
			return sfsModelExtractor.TryGetSfsModel(sfsModel, out sfsMap, out strError, out exprError);
		}
	}
}
