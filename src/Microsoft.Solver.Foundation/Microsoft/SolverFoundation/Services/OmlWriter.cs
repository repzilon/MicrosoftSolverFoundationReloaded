using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A method object for converting a Model to an OML Expression.
	///
	/// This preserves the structure of the model, including parameters and labels of constraints/goals.
	/// </summary>
	internal class OmlWriter : ITermVisitor<Expression, TermValueClass>
	{
		private readonly Dictionary<string, Expression> _cachedSubmodelExpressions = new Dictionary<string, Expression>();

		private readonly Dictionary<SubmodelInstance, Expression> _cachedSubmodelInstanceExpressions = new Dictionary<SubmodelInstance, Expression>();

		/// <summary>
		/// A cache of expressions resulting for terms. This cache is not necessary for correctness.
		/// </summary>
		private readonly Dictionary<Term, Expression> _cachedTermExpressions = new Dictionary<Term, Expression>();

		private readonly Dictionary<Tuples, Expression> _cachedTuplesExpressions = new Dictionary<Tuples, Expression>();

		/// <summary>
		/// A cache from named constants to symbols. Required to ensure that each parameter has a unique symbol.
		/// </summary>
		private readonly Dictionary<NamedConstantTerm, Symbol> _constantSymbols = new Dictionary<NamedConstantTerm, Symbol>();

		private readonly SolverContext _context;

		/// <summary>
		/// A cache from decisions to symbols. Required to ensure that each decision has a unique symbol.
		/// </summary>
		private readonly Dictionary<Decision, Expression> _decisionSymbols = new Dictionary<Decision, Expression>();

		private readonly Dictionary<Term, SubmodelInstance> _decisionToOwningSubmodelInstance = new Dictionary<Term, SubmodelInstance>();

		private readonly Dictionary<Domain, Expression> _domainNames = new Dictionary<Domain, Expression>();

		private readonly List<Expression> _extraSections = new List<Expression>();

		/// <summary>
		/// A cache mapping iteration terms to symbols. This is required to ensure that each iteration term gets a unique symbol.
		/// </summary>
		private readonly Dictionary<IterationTerm, Symbol> _iterationSymbols = new Dictionary<IterationTerm, Symbol>();

		private readonly Stack<Dictionary<string, int>> _nameContext = new Stack<Dictionary<string, int>>();

		/// <summary>
		/// A cache from parameters to symbols. Required to ensure that each parameter has a unique symbol.
		/// </summary>
		private readonly Dictionary<Parameter, Expression> _parameterSymbols = new Dictionary<Parameter, Expression>();

		/// <summary>
		/// A cache from parameters to symbols. Required to ensure that each parameter has a unique symbol.
		/// </summary>
		private readonly Dictionary<RandomParameter, Expression> _randomParameterSymbols = new Dictionary<RandomParameter, Expression>();

		/// <summary>
		/// A cache from recourse decisions to symbols. Required to ensure that each decision has a unique symbol.
		/// </summary>
		private readonly Dictionary<RecourseDecision, Expression> _recourseDecisionSymbols = new Dictionary<RecourseDecision, Expression>();

		/// <summary>
		/// A cache mapping sets to terms. This is necessary so that each set gets only one unique symbol.
		/// </summary>
		private Dictionary<Set, Expression> _cachedSets = new Dictionary<Set, Expression>();

		/// <summary>A mapping from random parameter type to its corresponding expression in OML
		/// </summary>
		private readonly Dictionary<Type, Expression> _randomParameterTypes = new Dictionary<Type, Expression>();

		private int _nextDomainSymbolIndex;

		private int _nextIteratorIndex;

		private SolveRewriteSystem Rewrite { get; set; }

		public OmlWriter(SolverContext context)
			: this(new SolveRewriteSystem(), context)
		{
		}

		public OmlWriter(SolveRewriteSystem rs, SolverContext context)
		{
			Rewrite = rs;
			_context = context;
			InitRandomParameterTypes();
			Dictionary<string, int> item = new Dictionary<string, int>(StringComparer.CurrentCulture);
			_nameContext.Push(item);
		}

		private void InitRandomParameterTypes()
		{
			_randomParameterTypes.Add(typeof(NormalDistributionParameter), Rewrite.Builtin.NormalDistribution);
			_randomParameterTypes.Add(typeof(UniformDistributionParameter), Rewrite.Builtin.UniformDistribution);
			_randomParameterTypes.Add(typeof(DiscreteUniformDistributionParameter), Rewrite.Builtin.DiscreteUniformDistribution);
			_randomParameterTypes.Add(typeof(ExponentialDistributionParameter), Rewrite.Builtin.ExponentialDistribution);
			_randomParameterTypes.Add(typeof(GeometricDistributionParameter), Rewrite.Builtin.GeometricDistribution);
			_randomParameterTypes.Add(typeof(BinomialDistributionParameter), Rewrite.Builtin.BinomialDistribution);
			_randomParameterTypes.Add(typeof(LogNormalDistributionParameter), Rewrite.Builtin.LogNormalDistribution);
			_randomParameterTypes.Add(typeof(ScenariosParameter), Rewrite.Builtin.Scenarios);
		}

		public Expression Visit(Decision term, TermValueClass arg)
		{
			if (!_decisionSymbols.TryGetValue(term, out var value))
			{
				value = TranslateSubmodelInstanceMemberAccess(term, (Term t) => ((Decision)t)._refKey, (Term t) => ((Decision)t).Name);
				_decisionSymbols.Add(term, value);
			}
			return value;
		}

		public Expression Visit(RecourseDecision term, TermValueClass arg)
		{
			if (!_recourseDecisionSymbols.TryGetValue(term, out var value))
			{
				value = TranslateSubmodelInstanceMemberAccess(term, (Term t) => ((RecourseDecision)t)._refKey, (Term t) => ((RecourseDecision)t).Name);
				_recourseDecisionSymbols.Add(term, value);
			}
			return value;
		}

		public Expression Visit(Parameter term, TermValueClass arg)
		{
			if (!_parameterSymbols.TryGetValue(term, out var value))
			{
				value = TranslateSubmodelInstanceMemberAccess(term, (Term t) => ((Parameter)t)._refKey, (Term t) => ((Parameter)t).Name);
				_parameterSymbols.Add(term, value);
			}
			return value;
		}

		public Expression Visit(RandomParameter term, TermValueClass arg)
		{
			if (!_randomParameterSymbols.TryGetValue(term, out var value))
			{
				value = TranslateSubmodelInstanceMemberAccess(term, (Term t) => ((RandomParameter)t)._refKey, (Term t) => ((RandomParameter)t).Name);
				_randomParameterSymbols.Add(term, value);
			}
			return value;
		}

		public Expression Visit(NamedConstantTerm term, TermValueClass arg)
		{
			if (!_constantSymbols.TryGetValue(term, out var value))
			{
				value = CreateSymbol(term.Name);
				_constantSymbols.Add(term, value);
			}
			return value;
		}

		public Expression Visit(ConstantTerm term, TermValueClass arg)
		{
			return RationalConstant.Create(Rewrite, term._value);
		}

		public Expression Visit(StringConstantTerm term, TermValueClass arg)
		{
			return new StringConstant(Rewrite, term._value);
		}

		public Expression Visit(BoolConstantTerm term, TermValueClass arg)
		{
			if (!(term._value != 0))
			{
				return Rewrite.Builtin.Boolean.False;
			}
			return Rewrite.Builtin.Boolean.True;
		}

		public Expression Visit(EnumeratedConstantTerm term, TermValueClass arg)
		{
			return new StringConstant(Rewrite, term.Value);
		}

		public Expression Visit(IdentityTerm term, TermValueClass arg)
		{
			return term._input.Visit(this, arg);
		}

		public Expression Visit(OperatorTerm term, TermValueClass arg)
		{
			TermValueClass inputClass;
			Expression expression;
			switch (term.Operation)
			{
			case Operator.Abs:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Abs;
				break;
			case Operator.And:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.And;
				break;
			case Operator.Equal:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Equal;
				break;
			case Operator.Greater:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Greater;
				break;
			case Operator.GreaterEqual:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.GreaterEqual;
				break;
			case Operator.Less:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Less;
				break;
			case Operator.LessEqual:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.LessEqual;
				break;
			case Operator.Minus:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Minus;
				break;
			case Operator.Not:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Not;
				break;
			case Operator.Or:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Or;
				break;
			case Operator.Plus:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Plus;
				break;
			case Operator.Power:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Power;
				break;
			case Operator.Quotient:
				inputClass = TermValueClass.Numeric;
				expression = null;
				break;
			case Operator.Sos1:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Sos1;
				break;
			case Operator.Sos1Row:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Sos1;
				break;
			case Operator.Sos2:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Sos2;
				break;
			case Operator.Sos2Row:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Sos2;
				break;
			case Operator.Times:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Times;
				break;
			case Operator.Unequal:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Unequal;
				break;
			case Operator.Max:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Max;
				break;
			case Operator.Min:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Min;
				break;
			case Operator.Cos:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Cos;
				break;
			case Operator.Sin:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Sin;
				break;
			case Operator.Tan:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Tan;
				break;
			case Operator.ArcCos:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.ArcCos;
				break;
			case Operator.ArcSin:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.ArcSin;
				break;
			case Operator.ArcTan:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.ArcTan;
				break;
			case Operator.Cosh:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Cosh;
				break;
			case Operator.Sinh:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Sinh;
				break;
			case Operator.Tanh:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Tanh;
				break;
			case Operator.Exp:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Exp;
				break;
			case Operator.Log:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Log;
				break;
			case Operator.Log10:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Log10;
				break;
			case Operator.Sqrt:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Sqrt;
				break;
			case Operator.Ceiling:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Ceiling;
				break;
			case Operator.Floor:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.Floor;
				break;
			case Operator.If:
				inputClass = TermValueClass.Numeric;
				expression = Rewrite.Builtin.If;
				break;
			default:
				throw new NotSupportedException();
			}
			List<Expression> list = TranslateInputs(term, inputClass);
			if (term.Operation == Operator.If && list[0].Head is AsIntSymbol)
			{
				list[0] = list[0][0];
			}
			if (term.Operation == Operator.Quotient)
			{
				if (list[1].GetValue(out Rational val))
				{
					return Rewrite.Builtin.Times.Invoke(list[0], RationalConstant.Create(Rewrite, 1 / val));
				}
				return Rewrite.Builtin.Times.Invoke(list[0], Rewrite.Builtin.Power.Invoke(list[1], Rewrite.Builtin.Integer.MinusOne));
			}
			if (term.Operation == Operator.Sos1Row || term.Operation == Operator.Sos2Row)
			{
				if (list[0].GetNumericValue(out var val2) && list[2].GetNumericValue(out var val3) && val2.IsNegativeInfinity && val3.IsPositiveInfinity)
				{
					return expression.Invoke(list[1]);
				}
				return Rewrite.Builtin.And.Invoke(Rewrite.Builtin.LessEqual.Invoke(list[0], list[1], list[2]), expression.Invoke(list[1]));
			}
			if (expression == Rewrite.Builtin.Plus && list.Count == 1 && list[0].Head == Rewrite.Builtin.Foreach)
			{
				Invocation invocation = list[0] as Invocation;
				return Rewrite.Builtin.Sum.Invoke(invocation.ArgsArray);
			}
			if (expression == Rewrite.Builtin.Plus && list.Count == 1 && list[0].Head == Rewrite.Builtin.FilteredForeach)
			{
				Invocation invocation2 = list[0] as Invocation;
				return Rewrite.Builtin.FilteredSum.Invoke(invocation2.ArgsArray);
			}
			return expression.Invoke(list.ToArray());
		}

		public Expression Visit(RowTerm term, TermValueClass arg)
		{
			List<Expression> list = new List<Expression>();
			foreach (LinearEntry rowEntry in term._model.GetRowEntries(term._vid))
			{
				Term term2 = term._variables[rowEntry.Index];
				Rational value = rowEntry.Value;
				Expression expression = Translate(term2, TermValueClass.Numeric);
				if (value != 1)
				{
					list.Add(Rewrite.Builtin.Times.Invoke(RationalConstant.Create(Rewrite, value), expression));
				}
				else
				{
					list.Add(expression);
				}
			}
			if (list.Count == 0)
			{
				return Rewrite.Builtin.Integer.Zero;
			}
			if (list.Count == 1)
			{
				return list[0];
			}
			return Rewrite.Builtin.Plus.Invoke(list.ToArray());
		}

		public Expression Visit(ElementOfTerm tuple, TermValueClass arg)
		{
			Term[] tuple2 = tuple._tuple;
			Tuples tupleList = tuple._tupleList;
			Domain[] domains = tupleList.Domains;
			Expression[] array = new Expression[tuple2.Length];
			for (int i = 0; i < tuple2.Length; i++)
			{
				array[i] = Translate(tuple2[i], domains[i].ValueClass);
			}
			Expression expression = _cachedTuplesExpressions[tupleList];
			return Rewrite.Builtin.ElementOf.Invoke(Rewrite.Builtin.List.Invoke(array), expression);
		}

		public Expression Visit(Tuples tuples, TermValueClass arg)
		{
			throw new MsfException(Resources.InternalError);
		}

		public Expression Visit(IndexTerm term, TermValueClass arg)
		{
			Expression expression = Translate((Term)term._table, TermValueClass.Table);
			List<Expression> list = new List<Expression>();
			Term[] inputs = term._inputs;
			foreach (Term term2 in inputs)
			{
				list.Add(Translate(term2, term2.ValueClass));
			}
			return expression.Invoke(list.ToArray());
		}

		public Expression Visit(IterationTerm term, TermValueClass arg)
		{
			if (!_iterationSymbols.TryGetValue(term, out var value))
			{
				value = NewIterationSymbol();
				_iterationSymbols.Add(term, value);
			}
			return value;
		}

		public Expression Visit(ForEachTerm term, TermValueClass arg)
		{
			Expression expression = TranslateIterator(term);
			Expression expression2 = Translate(term._valueExpression, arg);
			return Rewrite.Builtin.Foreach.Invoke(expression, expression2);
		}

		public Expression Visit(ForEachWhereTerm term, TermValueClass arg)
		{
			Expression expression = Translate(term._condExpression, TermValueClass.Numeric);
			Expression expression2 = Translate(term._valueExpression, arg);
			Expression expression3 = TranslateIterator(term);
			return Rewrite.Builtin.FilteredForeach.Invoke(expression3, expression, expression2);
		}

		/// <summary>
		/// Translate a model into OML. The result is an invocation with Model as its head. The invocation might be empty.
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public Expression Translate(Model model)
		{
			List<Expression> list = new List<Expression>();
			Dictionary<string, int> item = new Dictionary<string, int>(StringComparer.CurrentCulture);
			_nameContext.Push(item);
			AddSubmodels(model, list);
			_cachedSets = new Dictionary<Set, Expression>();
			AddSets(model, list);
			AddParameterSections(model, list);
			AddTuples(model, list);
			AddDecisionSections(model, list);
			AddConstraints(model, list);
			AddGoals(model, list);
			AddNamedConstants(list);
			if (model._level == 0)
			{
				list.AddRange(_extraSections);
			}
			Expression result = Rewrite.Builtin.Model.Invoke(list.ToArray());
			_nameContext.Pop();
			return result;
		}

		private Expression TranslateSubmodelDefinition(Model model)
		{
			if (!_cachedSubmodelExpressions.TryGetValue(model.Name, out var value))
			{
				value = CreateSymbol(model.Name);
				_cachedSubmodelExpressions.Add(model.Name, value);
			}
			return value;
		}

		/// <summary>
		/// Add all SubmodelName -&gt; Model[...] sections.
		/// </summary>
		private void AddSubmodels(Model model, List<Expression> modelSections)
		{
			foreach (Model submodel in model.Submodels)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				modelSections.Add(Rewrite.Builtin.Rule.Invoke(TranslateSubmodelDefinition(submodel), Translate(submodel)));
			}
		}

		/// <summary>
		/// Add all Parameters[Sets, ...] sections. Each group of sets sharing a domain are grouped together.
		/// Domains are compared with object identity, so sets whose domains are two different calls to
		/// Domain.RealRange(...) will be placed in different sections.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="modelSections"></param>
		private void AddSets(Model model, List<Expression> modelSections)
		{
			List<List<Expression>> list = new List<List<Expression>>();
			Dictionary<Domain, List<Expression>> domainToSection = new Dictionary<Domain, List<Expression>>();
			List<Set> sets = GetSets(model);
			Set set;
			foreach (Set item in sets)
			{
				set = item;
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				if (!CanTranslateSet(set))
				{
					continue;
				}
				List<Expression> sectionForDomain = GetSectionForDomain(list, domainToSection, set._domain, isSets: true);
				Expression expression = Translate(set);
				if (set.IsConstant)
				{
					Expression expression2 = Rewrite.Builtin.List.Invoke(set._fixedValues.Select((Term t) => Translate(t, set.ItemValueClass)).ToArray());
					expression = Rewrite.Builtin.Set.Invoke(true, expression, expression2);
				}
				sectionForDomain.Add(expression);
			}
			foreach (List<Expression> item2 in list)
			{
				modelSections.Add(Rewrite.Builtin.Parameters.Invoke(item2.ToArray()));
			}
		}

		/// <summary>
		/// Get all sets from the model. This is similar to Model.ModelSets, but guarantees
		/// that the sets are added in a consistent order (given a consistent order of Parameters and Decisions).
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		private static List<Set> GetSets(Model model)
		{
			HashSet<Set> knownSets = new HashSet<Set>();
			List<Set> list = new List<Set>();
			foreach (Parameter parameter in model.Parameters)
			{
				AddToSets(knownSets, list, parameter);
			}
			foreach (RandomParameter randomParameter in model.RandomParameters)
			{
				AddToSets(knownSets, list, randomParameter);
			}
			foreach (Decision decision in model.Decisions)
			{
				AddToSets(knownSets, list, decision);
			}
			foreach (RecourseDecision recourseDecision in model.RecourseDecisions)
			{
				AddToSets(knownSets, list, recourseDecision);
			}
			return list;
		}

		private static void AddToSets(HashSet<Set> knownSets, List<Set> sets, IIndexable indexed)
		{
			Set[] indexSets = indexed.IndexSets;
			foreach (Set item in indexSets)
			{
				if (!knownSets.Contains(item))
				{
					sets.Add(item);
					knownSets.Add(item);
				}
			}
		}

		private Expression TranslateSubmodelInstanceDefinition(SubmodelInstance si)
		{
			if (!_cachedSubmodelInstanceExpressions.TryGetValue(si, out var value))
			{
				value = CreateSymbol(si.Name);
				_cachedSubmodelInstanceExpressions.Add(si, value);
			}
			return value;
		}

		/// <summary>
		/// Add all Decisions[...] sections to the model. Like AddSets, this groups decisions with the same domain.
		/// Indexed decisions are handled appropriately.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="modelSections"></param>
		private void AddDecisionSections(Model model, List<Expression> modelSections)
		{
			List<List<Expression>> list = new List<List<Expression>>();
			Dictionary<Domain, List<Expression>> domainToSection = new Dictionary<Domain, List<Expression>>();
			Dictionary<Model, List<Expression>> submodelToSection = new Dictionary<Model, List<Expression>>();
			foreach (SubmodelInstance submodelInstance in model.SubmodelInstances)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				List<Expression> sectionForSubmodel = GetSectionForSubmodel(list, submodelToSection, submodelInstance._domain);
				Expression item = TranslateSubmodelInstanceDefinition(submodelInstance);
				sectionForSubmodel.Add(item);
			}
			var second = model.Decisions.Select((Decision decision) => new
			{
				Domain = decision._domain,
				Term = (Term)decision,
				RefKey = (object)decision._refKey,
				IndexSets = decision._indexSets,
				Description = decision.Description
			});
			var first = model.RecourseDecisions.Select((RecourseDecision recourseDecision) => new
			{
				Domain = recourseDecision._domain,
				Term = (Term)recourseDecision,
				RefKey = (object)recourseDecision._refKey,
				IndexSets = recourseDecision._indexSets,
				Description = recourseDecision.Description
			});
			var enumerable = first.Union(second);
			foreach (var item2 in enumerable)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				List<Expression> sectionForDomain = GetSectionForDomain(list, domainToSection, item2.Domain, isSets: false);
				Expression expression = Translate(item2.Term, TermValueClass.Any);
				if (item2.IndexSets.Length != 0)
				{
					List<Expression> list2 = new List<Expression>();
					List<Expression> list3 = new List<Expression>();
					Set[] indexSets = item2.IndexSets;
					foreach (Set set in indexSets)
					{
						if (!CanTranslateSet(set))
						{
							Symbol symbol = NewIterationSymbol();
							list3.Add(FixedIterator(symbol, set));
							list2.Add(symbol);
						}
						else
						{
							list2.Add(Translate(set));
						}
					}
					expression = expression.Invoke(list2.ToArray());
					if (list3.Count > 0)
					{
						list3.Add(expression);
						expression = Rewrite.Builtin.Foreach.Invoke(list3.ToArray());
					}
				}
				string description = item2.Description;
				if (description != null)
				{
					expression = Rewrite.Builtin.Annotation.Invoke(expression, new StringConstant(Rewrite, "description"), new StringConstant(Rewrite, description));
				}
				if (item2.Term is RecourseDecision)
				{
					expression = GetRecourseDecisionDefintion(expression);
				}
				sectionForDomain.Add(expression);
			}
			foreach (List<Expression> item3 in list)
			{
				modelSections.Add(Rewrite.Builtin.Decisions.Invoke(item3.ToArray()));
			}
		}

		private static bool CanTranslateSet(Set set)
		{
			if (set.IsConstant)
			{
				return set._fixedValues != null;
			}
			return true;
		}

		/// <summary>Wrap the recourse decision name with Recourse invocation
		/// </summary>
		private Expression GetRecourseDecisionDefintion(Expression recourseDecision)
		{
			return Rewrite.Builtin.Recourse.Invoke(recourseDecision);
		}

		/// <summary>
		/// Add all Parameters[...] sections to the model. Like AddSets, this groups parameters with the same domain.
		/// Indexed parameters are handled appropriately.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="modelSections"></param>
		private void AddParameterSections(Model model, List<Expression> modelSections)
		{
			List<List<Expression>> list = new List<List<Expression>>();
			Dictionary<Domain, List<Expression>> domainToSection = new Dictionary<Domain, List<Expression>>();
			foreach (Parameter parameter in model.Parameters)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				List<Expression> sectionForDomain = GetSectionForDomain(list, domainToSection, parameter._domain, isSets: false);
				Expression expr = Translate(parameter, TermValueClass.Any);
				expr = AddSetsToExpression(expr, parameter.IndexSets);
				string description = parameter.Description;
				if (description != null)
				{
					expr = Rewrite.Builtin.Annotation.Invoke(expr, new StringConstant(Rewrite, "description"), new StringConstant(Rewrite, description));
				}
				sectionForDomain.Add(expr);
			}
			foreach (RandomParameter randomParameter in model.RandomParameters)
			{
				List<Expression> randomParameterSection = GetRandomParameterSection(randomParameter);
				list.Add(randomParameterSection);
			}
			foreach (List<Expression> item in list)
			{
				modelSections.Add(Rewrite.Builtin.Parameters.Invoke(item.ToArray()));
			}
		}

		private List<Expression> GetRandomParameterSection(RandomParameter randomParameter)
		{
			if (_context._abortFlag)
			{
				throw new MsfException(Resources.Aborted);
			}
			List<Expression> list = new List<Expression>();
			Expression expr = Translate(randomParameter, TermValueClass.Any);
			expr = AddSetsToExpression(expr, randomParameter.IndexSets);
			string description = randomParameter.Description;
			if (description != null)
			{
				expr = Rewrite.Builtin.Annotation.Invoke(expr, new StringConstant(Rewrite, "description"), new StringConstant(Rewrite, description));
			}
			if (randomParameter is ScenariosParameter scenariosParameter)
			{
				return GetScenariosSection(scenariosParameter, expr);
			}
			Expression expression = _randomParameterTypes[randomParameter.GetType()];
			if (!randomParameter.NeedsBind && randomParameter.Binding == null)
			{
				List<Expression> list2 = new List<Expression>();
				DebugContracts.NonNull(randomParameter.ValueTable);
				NormalDistributionParameter normalDistributionParameter = randomParameter as NormalDistributionParameter;
				UniformDistributionParameter uniformDistributionParameter = randomParameter as UniformDistributionParameter;
				DiscreteUniformDistributionParameter discreteUniformDistributionParameter = randomParameter as DiscreteUniformDistributionParameter;
				ExponentialDistributionParameter exponentialDistributionParameter = randomParameter as ExponentialDistributionParameter;
				GeometricDistributionParameter geometricDistributionParameter = randomParameter as GeometricDistributionParameter;
				BinomialDistributionParameter binomialDistributionParameter = randomParameter as BinomialDistributionParameter;
				LogNormalDistributionParameter logNormalDistributionParameter = randomParameter as LogNormalDistributionParameter;
				UnivariateDistribution distribution = randomParameter.ValueTable.Values.First().Distribution;
				if ((object)normalDistributionParameter != null)
				{
					NormalUnivariateDistribution normalUnivariateDistribution = distribution as NormalUnivariateDistribution;
					list2.Add(RationalConstant.Create(Rewrite, normalUnivariateDistribution.Mean));
					list2.Add(RationalConstant.Create(Rewrite, normalUnivariateDistribution.StandardDeviation));
				}
				else if ((object)discreteUniformDistributionParameter != null)
				{
					DiscreteUniformUnivariateDistribution discreteUniformUnivariateDistribution = distribution as DiscreteUniformUnivariateDistribution;
					list2.Add(RationalConstant.Create(Rewrite, discreteUniformUnivariateDistribution.LowerBound));
					list2.Add(RationalConstant.Create(Rewrite, discreteUniformUnivariateDistribution.UpperBound));
				}
				else if ((object)uniformDistributionParameter != null)
				{
					ContinuousUniformUnivariateDistribution continuousUniformUnivariateDistribution = distribution as ContinuousUniformUnivariateDistribution;
					list2.Add(RationalConstant.Create(Rewrite, continuousUniformUnivariateDistribution.LowerBound));
					list2.Add(RationalConstant.Create(Rewrite, continuousUniformUnivariateDistribution.UpperBound));
				}
				else if ((object)exponentialDistributionParameter != null)
				{
					ExponentialUnivariateDistribution exponentialUnivariateDistribution = distribution as ExponentialUnivariateDistribution;
					list2.Add(RationalConstant.Create(Rewrite, exponentialUnivariateDistribution.Rate));
				}
				else if ((object)geometricDistributionParameter != null)
				{
					GeometricUnivariateDistribution geometricUnivariateDistribution = distribution as GeometricUnivariateDistribution;
					list2.Add(RationalConstant.Create(Rewrite, geometricUnivariateDistribution.SuccessProbability));
				}
				else if ((object)binomialDistributionParameter != null)
				{
					BinomialUnivariateDistribution binomialUnivariateDistribution = distribution as BinomialUnivariateDistribution;
					list2.Add(RationalConstant.Create(Rewrite, binomialUnivariateDistribution.NumberOfTrials));
					list2.Add(RationalConstant.Create(Rewrite, binomialUnivariateDistribution.SuccessProbability));
				}
				else
				{
					if ((object)logNormalDistributionParameter == null)
					{
						throw new MsfException("Not supported random parameter type");
					}
					LogNormalUnivariateDistribution logNormalUnivariateDistribution = distribution as LogNormalUnivariateDistribution;
					list2.Add(RationalConstant.Create(Rewrite, logNormalUnivariateDistribution.MeanLog));
					list2.Add(RationalConstant.Create(Rewrite, logNormalUnivariateDistribution.StdLog));
				}
				expression = expression.Invoke(list2.ToArray());
			}
			list.Add(expression);
			list.Add(expr);
			return list;
		}

		private List<Expression> GetScenariosSection(ScenariosParameter scenariosParameter, Expression name)
		{
			List<Expression> list = new List<Expression>(2);
			Expression expression = name;
			Expression expression2 = _randomParameterTypes[scenariosParameter.GetType()];
			expression2 = expression2.Invoke(Translate(Domain.Real));
			if (!scenariosParameter.NeedsBind && scenariosParameter.Binding == null)
			{
				expression = TranslateScenarios(expression, scenariosParameter);
			}
			list.Add(expression2);
			list.Add(expression);
			return list;
		}

		private Expression AddSetsToExpression(Expression expr, IEnumerable<Set> indexSets)
		{
			DebugContracts.NonNull(indexSets);
			if (!indexSets.Any())
			{
				return expr;
			}
			List<Expression> list = new List<Expression>();
			foreach (Set indexSet in indexSets)
			{
				list.Add(Translate(indexSet));
			}
			return expr.Invoke(list.ToArray());
		}

		private void AddTuples(Model model, List<Expression> modelSections)
		{
			foreach (Tuples tuple in model.Tuples)
			{
				Domain[] domains = tuple.Domains;
				Expression[] args = domains.Select((Domain domain) => Translate(domain)).ToArray();
				Expression expression = Rewrite.Builtin.Tuples.Invoke(args);
				Expression expression2 = CreateSymbol(tuple.Name);
				_cachedTuplesExpressions[tuple] = expression2;
				Expression item;
				if (tuple.IsConstant)
				{
					Expression[] array = new Expression[tuple.Data.Length];
					for (int i = 0; i < tuple.Data.Length; i++)
					{
						Expression[] array2 = new Expression[domains.Length];
						for (int j = 0; j < domains.Length; j++)
						{
							array2[j] = TranslateConstant(tuple.Data[i][j], domains[j]);
						}
						array[i] = Rewrite.Builtin.List.Invoke(array2);
					}
					item = Rewrite.Builtin.Parameters.Invoke(expression, Rewrite.Builtin.Set.Invoke(expression2, Rewrite.Builtin.List.Invoke(array)));
				}
				else
				{
					string description = tuple.Description;
					if (description != null)
					{
						expression2 = Rewrite.Builtin.Annotation.Invoke(expression2, new StringConstant(Rewrite, "description"), new StringConstant(Rewrite, description));
					}
					item = Rewrite.Builtin.Parameters.Invoke(expression, expression2);
				}
				modelSections.Add(item);
			}
		}

		/// <summary>Gets expression with all the explicit scenarios of the ScenariosParameter
		/// e.g. gasDemand= {{0.3, 1900}, {0.4, 2000}, {0.3, 2100} }
		/// </summary>
		private Expression TranslateScenarios(Expression scenarioParameterSymbol, ScenariosParameter scenarioParameter)
		{
			Expression result = scenarioParameterSymbol;
			if (!scenarioParameter.NeedsBind && scenarioParameter.Binding == null)
			{
				List<Expression> list = new List<Expression>();
				DebugContracts.NonNull(scenarioParameter.ValueTable);
				DiscreteScenariosValue discreteScenariosValue = scenarioParameter.ValueTable.Values.First() as DiscreteScenariosValue;
				foreach (Scenario scenario in discreteScenariosValue.Scenarios)
				{
					Expression expression = RationalConstant.Create(Rewrite, scenario.Probability);
					Expression expression2 = RationalConstant.Create(Rewrite, scenario.Value);
					list.Add(Rewrite.Builtin.List.Invoke(expression, expression2));
				}
				Expression expression3 = Rewrite.Builtin.List.Invoke(list.ToArray());
				result = Rewrite.Builtin.Set.Invoke(scenarioParameterSymbol, expression3);
			}
			return result;
		}

		private Expression TranslateConstant(Rational value, Domain domain)
		{
			if (domain.IsBoolean)
			{
				if (!(value != 0))
				{
					return Rewrite.Builtin.Boolean.False;
				}
				return Rewrite.Builtin.Boolean.True;
			}
			if (domain.ValueClass == TermValueClass.Enumerated)
			{
				return new StringConstant(Rewrite, domain.EnumeratedNames[(int)value]);
			}
			return RationalConstant.Create(Rewrite, value);
		}

		/// <summary>
		/// A helper function to group something (decisions, parameters, sets) by domains.
		/// </summary>
		/// <param name="sections">A list of section contents which go into the final model.</param>
		/// <param name="domainToSection">A dictionary from domains to lists of section contents.</param>
		/// <param name="domain">The domain of the section to find. If it doesn't exist, a new list of expressions
		/// is created, and the translation of the domain is added as the first element.</param>
		/// <param name="isSets">If true, the first element is wrapped in Sets[] when creating a new list of expressions.</param>
		/// <returns>A list of expressions representing the contents of the correct section. The list can be modified.</returns>
		private List<Expression> GetSectionForDomain(List<List<Expression>> sections, Dictionary<Domain, List<Expression>> domainToSection, Domain domain, bool isSets)
		{
			if (!domainToSection.TryGetValue(domain, out var value))
			{
				value = new List<Expression>();
				Expression expression = Translate(domain);
				if (isSets)
				{
					expression = Rewrite.Builtin.Sets.Invoke(expression);
				}
				value.Add(expression);
				sections.Add(value);
				domainToSection.Add(domain, value);
			}
			return value;
		}

		private List<Expression> GetSectionForSubmodel(List<List<Expression>> sections, Dictionary<Model, List<Expression>> submodelToSection, Model submodel)
		{
			if (!submodelToSection.TryGetValue(submodel, out var value))
			{
				value = new List<Expression>();
				Expression item = TranslateSubmodelDefinition(submodel);
				value.Add(item);
				sections.Add(value);
				submodelToSection.Add(submodel, value);
			}
			return value;
		}

		/// <summary>
		/// Add a Constraints[] section to the model. Only one Constraints[] section is generated. Each constraint is labeled.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="modelSections"></param>
		private void AddConstraints(Model model, List<Expression> modelSections)
		{
			List<Expression> list = new List<Expression>();
			foreach (Constraint constraint in model.Constraints)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				Expression expression;
				if (constraint._expression == null)
				{
					expression = Translate(constraint.Term, TermValueClass.Numeric);
				}
				else
				{
					Translate(constraint.Term, TermValueClass.Numeric);
					expression = new LiteralText(Rewrite, constraint._expression);
				}
				Expression expression2 = CreateSymbol(constraint.Name);
				string description = constraint.Description;
				if (description != null)
				{
					expression = Rewrite.Builtin.Annotation.Invoke(expression, new StringConstant(Rewrite, "description"), new StringConstant(Rewrite, description));
				}
				if (!constraint.Enabled)
				{
					expression = Rewrite.Builtin.Annotation.Invoke(expression, new StringConstant(Rewrite, "enabled"), Rewrite.Builtin.Boolean.False);
				}
				list.Add(Rewrite.Builtin.Rule.Invoke(expression2, expression));
			}
			if (list.Count > 0)
			{
				modelSections.Add(Rewrite.Builtin.Constraints.Invoke(list.ToArray()));
			}
		}

		/// <summary>
		/// Add a Goals[] section to the model for each goal. Each goal is labeled.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="modelSections"></param>
		private void AddGoals(Model model, List<Expression> modelSections)
		{
			foreach (Goal goal in model.Goals)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				Expression expression;
				if (goal._expression == null)
				{
					expression = Translate(goal.Term, TermValueClass.Numeric);
				}
				else
				{
					Translate(goal.Term, TermValueClass.Numeric);
					expression = new Symbol(Rewrite, null, goal._expression);
				}
				expression = Rewrite.Builtin.Annotation.Invoke(expression, new StringConstant(Rewrite, "order"), new IntegerConstant(Rewrite, goal.Order));
				string description = goal.Description;
				if (description != null)
				{
					expression = Rewrite.Builtin.Annotation.Invoke(expression, new StringConstant(Rewrite, "description"), new StringConstant(Rewrite, description));
				}
				if (!goal.Enabled)
				{
					expression = Rewrite.Builtin.Annotation.Invoke(expression, new StringConstant(Rewrite, "enabled"), Rewrite.Builtin.Boolean.False);
				}
				Expression expression2 = CreateSymbol(goal.Name);
				Expression expression3 = Rewrite.Builtin.Rule.Invoke(expression2, expression);
				switch (goal.Direction)
				{
				case GoalKind.Maximize:
					modelSections.Add(Rewrite.Builtin.Goals.Invoke(Rewrite.Builtin.Maximize.Invoke(expression3)));
					break;
				case GoalKind.Minimize:
					modelSections.Add(Rewrite.Builtin.Goals.Invoke(Rewrite.Builtin.Minimize.Invoke(expression3)));
					break;
				}
			}
		}

		/// <summary>
		/// Add all named constants ("Parameters[..., P = constant]") to the model.
		/// Named constants go before the other model sections, but are added last because
		/// they are discovered while writing the Goals and Constraints sections.
		/// </summary>
		/// <param name="modelSections"></param>
		private void AddNamedConstants(List<Expression> modelSections)
		{
			foreach (KeyValuePair<NamedConstantTerm, Symbol> constantSymbol in _constantSymbols)
			{
				NamedConstantTerm key = constantSymbol.Key;
				Symbol value = constantSymbol.Value;
				AddNamedConstant(modelSections, key, value);
			}
		}

		private void AddNamedConstant(List<Expression> modelSections, NamedConstantTerm namedConstantTerm, Symbol namedConstantSymbol)
		{
			Expression expression = Translate(namedConstantTerm._innerTerm, TermValueClass.Numeric);
			Expression expression5;
			if (namedConstantTerm._indexSets.Length > 0)
			{
				int num = namedConstantTerm._indexSets.Length;
				Expression[] array = new Expression[num + 1];
				Expression[] array2 = new Expression[num];
				for (int i = 0; i < num; i++)
				{
					Expression expression2 = TranslateIterator(namedConstantTerm._indexes[i], namedConstantTerm._indexSets[i]);
					Expression expression3 = Translate(namedConstantTerm._indexes[i], TermValueClass.Any);
					array[i] = expression2;
					array2[i] = expression3;
				}
				array[num] = Rewrite.Builtin.Set.Invoke(namedConstantSymbol.Invoke(array2), expression);
				Expression expression4 = Rewrite.Builtin.Foreach.Invoke(array);
				expression5 = expression4;
			}
			else
			{
				expression5 = Rewrite.Builtin.Set.Invoke(namedConstantSymbol, expression);
			}
			Expression expression6 = Translate(namedConstantTerm._domain);
			modelSections.Insert(0, Rewrite.Builtin.Parameters.Invoke(expression6, expression5));
		}

		/// <summary>
		/// Translate a Domain into an OML expression representing it.
		/// </summary>
		/// <param name="domain"></param>
		/// <returns></returns>
		public Expression Translate(Domain domain)
		{
			if (_domainNames.TryGetValue(domain, out var value))
			{
				return value;
			}
			Expression expression = domain.MakeOmlDomain(this, Rewrite);
			_domainNames.Add(domain, expression);
			return expression;
		}

		internal Symbol AddDomainsSection(Domain domain, Expression domainExpr)
		{
			string strName = domain.Name ?? ("enumDomain" + _nextDomainSymbolIndex++);
			Symbol symbol = new Symbol(Rewrite, strName);
			_extraSections.Add(Rewrite.Builtin.Domains.Invoke(domainExpr, symbol));
			return symbol;
		}

		/// <summary>
		/// Translate a Set into an OML expression representing it.
		/// </summary>
		/// <param name="set"></param>
		/// <returns></returns>
		public Expression Translate(Set set)
		{
			if (!_cachedSets.TryGetValue(set, out var value))
			{
				value = CreateSymbol(set.Name);
				_cachedSets.Add(set, value);
			}
			return value;
		}

		/// <summary>
		/// Translate a Term into an OML expression representing it. This calls Visit on the term, which in turn calls the appropriate
		/// overload of Visit on this object.
		/// </summary>
		/// <param name="term"></param>
		/// <param name="targetValueClass">The value class the result should have. The value will be coerced if necessary.</param>
		/// <returns></returns>
		public Expression Translate(Term term, TermValueClass targetValueClass)
		{
			if (!_cachedTermExpressions.TryGetValue(term, out var value))
			{
				value = term.Visit(this, term.ValueClass);
				_cachedTermExpressions[term] = value;
			}
			return value;
		}

		/// <summary>
		/// A helper function to find valid identifiers. Should be no broader than Lexer.IsIdentCh
		/// </summary>
		/// <param name="ch"></param>
		/// <returns></returns>
		private static bool IsIdentCh(char ch)
		{
			if (ch >= '\u0080')
			{
				return (Lexer.GetUniCatFlags(ch) & Lexer.UniCatFlags.IdentPartChar) != 0;
			}
			switch (ch)
			{
			case 'a':
			case 'b':
			case 'c':
			case 'd':
			case 'e':
			case 'f':
			case 'g':
			case 'h':
			case 'i':
			case 'j':
			case 'k':
			case 'l':
			case 'm':
			case 'n':
			case 'o':
			case 'p':
			case 'q':
			case 'r':
			case 's':
			case 't':
			case 'u':
			case 'v':
			case 'w':
			case 'x':
			case 'y':
			case 'z':
				return true;
			case 'A':
			case 'B':
			case 'C':
			case 'D':
			case 'E':
			case 'F':
			case 'G':
			case 'H':
			case 'I':
			case 'J':
			case 'K':
			case 'L':
			case 'M':
			case 'N':
			case 'O':
			case 'P':
			case 'Q':
			case 'R':
			case 'S':
			case 'T':
			case 'U':
			case 'V':
			case 'W':
			case 'X':
			case 'Y':
			case 'Z':
				return true;
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
				return true;
			case '_':
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// A helper function to find valid identifiers. Should be no broader than Lexer.IsIdentCh
		/// </summary>
		/// <param name="ch"></param>
		/// <returns></returns>
		private static bool IsIdentStartCh(char ch)
		{
			if (ch >= '\u0080')
			{
				return (Lexer.GetUniCatFlags(ch) & Lexer.UniCatFlags.IdentPartChar) != 0;
			}
			switch (ch)
			{
			case 'a':
			case 'b':
			case 'c':
			case 'd':
			case 'e':
			case 'f':
			case 'g':
			case 'h':
			case 'i':
			case 'j':
			case 'k':
			case 'l':
			case 'm':
			case 'n':
			case 'o':
			case 'p':
			case 'q':
			case 'r':
			case 's':
			case 't':
			case 'u':
			case 'v':
			case 'w':
			case 'x':
			case 'y':
			case 'z':
				return true;
			case 'A':
			case 'B':
			case 'C':
			case 'D':
			case 'E':
			case 'F':
			case 'G':
			case 'H':
			case 'I':
			case 'J':
			case 'K':
			case 'L':
			case 'M':
			case 'N':
			case 'O':
			case 'P':
			case 'Q':
			case 'R':
			case 'S':
			case 'T':
			case 'U':
			case 'V':
			case 'W':
			case 'X':
			case 'Y':
			case 'Z':
				return true;
			case '_':
				return true;
			default:
				return false;
			}
		}

		internal static bool IsValidOmlName(string s)
		{
			if (s.Length == 0)
			{
				return false;
			}
			for (int i = 0; i < s.Length; i++)
			{
				if (!IsIdentCh(s[i]))
				{
					return false;
				}
			}
			if (!IsIdentStartCh(s[0]))
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Create a new symbol with a given name. If the name contains characters which are not valid in a symbol,
		/// translates them to characters which are valid.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private Symbol CreateSymbol(string name)
		{
			StringBuilder nameBuf = ReplaceInvalidChars(name);
			return new Symbol(Rewrite, null, EnsureUniqueName(nameBuf));
		}

		internal static StringBuilder ReplaceInvalidChars(string name)
		{
			StringBuilder stringBuilder = new StringBuilder(name);
			for (int i = 0; i < stringBuilder.Length; i++)
			{
				if (!IsIdentCh(stringBuilder[i]))
				{
					stringBuilder[i] = '_';
				}
			}
			if (stringBuilder.Length == 0 || !IsIdentStartCh(stringBuilder[0]))
			{
				stringBuilder.Insert(0, '_');
			}
			return stringBuilder;
		}

		private string EnsureUniqueName(StringBuilder nameBuf)
		{
			string text = nameBuf.ToString();
			Dictionary<string, int> dictionary = _nameContext.Peek();
			if (dictionary.ContainsKey(text))
			{
				int num = 5;
				int num2 = 0;
				do
				{
					text = text + "_" + (++dictionary[text]).ToString(NumberFormatInfo.InvariantInfo);
				}
				while (dictionary.ContainsKey(text) && num2++ < num);
				if (dictionary.ContainsKey(text))
				{
					nameBuf.Append("_");
					nameBuf.Append(Model.UniqueSuffix());
					text = nameBuf.ToString();
				}
			}
			dictionary.Add(text, 0);
			return text;
		}

		private SubmodelInstance GetOwningSubmodelInstance(Term term)
		{
			SubmodelInstance value = null;
			Decision decision = term as Decision;
			RecourseDecision recourseDecision = term as RecourseDecision;
			Parameter parameter = term as Parameter;
			RandomParameter randomParameter = term as RandomParameter;
			if ((object)decision == null && (object)recourseDecision == null && (object)parameter == null && (object)randomParameter == null)
			{
				return null;
			}
			if (!_decisionToOwningSubmodelInstance.TryGetValue(term, out value))
			{
				bool flag = true;
				if ((object)decision != null)
				{
					flag = (object)decision._refKey == null;
					foreach (SubmodelInstance submodelInstance in term._owningModel.SubmodelInstances)
					{
						if (submodelInstance.AllDecisions.Contains(decision))
						{
							value = submodelInstance;
							break;
						}
					}
				}
				else if ((object)recourseDecision != null)
				{
					flag = (object)recourseDecision._refKey == null;
					foreach (SubmodelInstance submodelInstance2 in term._owningModel.SubmodelInstances)
					{
						if (submodelInstance2.AllRecourseDecisions.Contains(recourseDecision))
						{
							value = submodelInstance2;
							break;
						}
					}
				}
				else if ((object)parameter != null)
				{
					flag = (object)parameter._refKey == null;
					foreach (SubmodelInstance submodelInstance3 in term._owningModel.SubmodelInstances)
					{
						if (submodelInstance3.AllParameters.Contains(parameter))
						{
							value = submodelInstance3;
							break;
						}
					}
				}
				else if ((object)randomParameter != null)
				{
					flag = (object)randomParameter._refKey == null;
					foreach (SubmodelInstance submodelInstance4 in term._owningModel.SubmodelInstances)
					{
						if (submodelInstance4.AllRandomParameters.Contains(randomParameter))
						{
							value = submodelInstance4;
							break;
						}
					}
				}
				if (value == null && !flag)
				{
					throw new MsfException(Resources.IllFormedModelCannotBeSaved);
				}
				_decisionToOwningSubmodelInstance.Add(term, value);
			}
			return value;
		}

		private Expression TranslateSubmodelInstanceMemberAccess(Term term, Func<Term, Term> getRefKey, Func<Term, string> getName)
		{
			if (!_cachedTermExpressions.TryGetValue(term, out var value))
			{
				if ((object)getRefKey(term) == null)
				{
					value = CreateSymbol(getName(term));
				}
				else
				{
					SubmodelInstance owningSubmodelInstance = GetOwningSubmodelInstance(term);
					Expression expression = TranslateSubmodelInstanceMemberAccess(getRefKey(term), getRefKey, getName);
					value = ((owningSubmodelInstance == null) ? CreateSymbol(getName(term)).Invoke(expression) : TranslateSubmodelInstanceDefinition(owningSubmodelInstance).Invoke(expression));
				}
				_cachedTermExpressions.Add(term, value);
			}
			return value;
		}

		private List<Expression> TranslateInputs(OperatorTerm term, TermValueClass inputClass)
		{
			List<Expression> list = new List<Expression>();
			Queue<OperatorTerm> queue = new Queue<OperatorTerm>();
			queue.Enqueue(term);
			while (queue.Count > 0)
			{
				OperatorTerm operatorTerm = queue.Dequeue();
				Term[] inputs = operatorTerm.Inputs;
				foreach (Term term2 in inputs)
				{
					OperatorTerm operatorTerm2 = term2 as OperatorTerm;
					if (term.IsAssociativeAndCommutative && (object)operatorTerm2 != null && operatorTerm2.Operation == term.Operation)
					{
						queue.Enqueue(operatorTerm2);
					}
					else
					{
						list.Add(Translate(term2, inputClass));
					}
				}
			}
			return list;
		}

		private Symbol NewIterationSymbol()
		{
			string name = "iter" + ++_nextIteratorIndex;
			return CreateSymbol(name);
		}

		private Expression TranslateIterator(ForEachTerm term)
		{
			return TranslateIterator(term._iterator, term._set);
		}

		private Expression TranslateIterator(Term iterator, Set set)
		{
			Expression expression = Translate(iterator, TermValueClass.Any);
			if (!CanTranslateSet(set))
			{
				return FixedIterator(expression, set);
			}
			Expression expression2 = Translate(set);
			return Rewrite.Builtin.List.Invoke(expression, expression2);
		}

		private Expression FixedIterator(Expression iteratorExpr, Set set)
		{
			if (set._fixedValues != null)
			{
				Expression[] array = new Expression[set._fixedValues.Length];
				for (int i = 0; i < set._fixedValues.Length; i++)
				{
					array[i] = Translate(set._fixedValues[i], TermValueClass.Numeric);
				}
				return Rewrite.Builtin.List.Invoke(iteratorExpr, Rewrite.Builtin.List.Invoke(array));
			}
			Expression expression = Translate(set.FixedStart, TermValueClass.Numeric);
			Expression expression2 = Translate(set.FixedLimit, TermValueClass.Numeric);
			Expression expression3 = Translate(set.FixedStep, TermValueClass.Numeric);
			if (!expression3.GetNumericValue(out var val) || val != 1)
			{
				return Rewrite.Builtin.List.Invoke(iteratorExpr, expression, expression2, expression3);
			}
			if (!expression.GetNumericValue(out val) || val != 0)
			{
				return Rewrite.Builtin.List.Invoke(iteratorExpr, expression, expression2);
			}
			return Rewrite.Builtin.List.Invoke(iteratorExpr, expression2);
		}
	}
}
