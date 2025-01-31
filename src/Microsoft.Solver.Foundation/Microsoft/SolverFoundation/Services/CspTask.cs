using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	internal class CspTask : Task
	{
		private readonly Dictionary<Domain, CspDomain> _domains = new Dictionary<Domain, CspDomain>();

		private ConstraintSystem _sfSolver;

		protected Dictionary<IVariable, ValueTable<int>> _keyFromDecisionAndIndex = new Dictionary<IVariable, ValueTable<int>>();

		protected Dictionary<IVariable, int> _keyFromDecisionWithoutIndex = new Dictionary<IVariable, int>();

		protected int _nextKey;

		internal ConstraintSystem Solver => _sfSolver;

		public CspTask(SolverContext context, ConstraintSystem fs, ISolverParameters solverParams, Directive directive)
			: base(context, solverParams, directive)
		{
			_sfSolver = fs;
		}

		protected override SolverContext.TaskSummary Solve(Func<bool> queryAbort)
		{
			MsfException exception = null;
			_solverParams.QueryAbort = queryAbort;
			ConstraintSolverSolution solution = null;
			try
			{
				solution = _sfSolver.Solve(_solverParams);
			}
			catch (MsfException ex)
			{
				exception = ex;
			}
			SolverContext.TaskSummary taskSummary = new SolverContext.TaskSummary();
			taskSummary.SolutionMapping = GetSolutionMapping(solution);
			taskSummary.Solution = new Solution(_context, ResultQuality(solution));
			taskSummary.Directive = _directive;
			taskSummary.Solver = _sfSolver;
			taskSummary.Exception = exception;
			return taskSummary;
		}

		private static SolverQuality ResultQuality(ConstraintSolverSolution solution)
		{
			if (solution == null)
			{
				return SolverQuality.Unknown;
			}
			if (solution.HasFoundSolution)
			{
				if (solution.Quality == ConstraintSolverSolution.SolutionQuality.Optimal)
				{
					return SolverQuality.Optimal;
				}
				return SolverQuality.Feasible;
			}
			if (solution.Quality == ConstraintSolverSolution.SolutionQuality.Infeasible)
			{
				return SolverQuality.Infeasible;
			}
			return SolverQuality.Unknown;
		}

		public override void AbortTask()
		{
			base.AbortTask();
			_sfSolver.Parameters.Abort = true;
		}

		public override void AddConstraints(IEnumerable<Constraint> constraints)
		{
			_evaluationContext.Goal = null;
			foreach (Constraint constraint in constraints)
			{
				if (!constraint.Enabled)
				{
					continue;
				}
				_evaluationContext.Constraint = constraint;
				foreach (Term item in constraint.Term.AllValues(_evaluationContext))
				{
					CspTerm cspTerm = Expand(item);
					_sfSolver.AddConstraints(cspTerm);
				}
			}
		}

		public override void AddGoal(Goal goal, int goalId)
		{
			CspTerm cspTerm = Expand(goal.Term);
			bool flag;
			if (goal.Direction == GoalKind.Minimize)
			{
				flag = _sfSolver.TryAddMinimizationGoals(cspTerm);
			}
			else
			{
				CspTerm cspTerm2 = _sfSolver.Neg(cspTerm);
				flag = _sfSolver.TryAddMinimizationGoals(cspTerm2);
			}
			if (!flag)
			{
				throw new InvalidTermException(Resources.TheGoalCouldNotBeAddedToTheModel, goal.Term);
			}
			int num = _nextKey++;
			CspTerm cspTerm3 = _sfSolver.CreateVariable(_sfSolver.DefaultInterval, num);
			_sfSolver.AddConstraints(_sfSolver.Equal(cspTerm, cspTerm3));
			AddGoalKey(goal, num);
		}

		internal override SolutionMapping GetSolutionMapping(ISolverSolution solution)
		{
			if (solution == null)
			{
				return null;
			}
			ConstraintSolverSolution constraintSolverSolution = solution as ConstraintSolverSolution;
			DebugContracts.NonNull(constraintSolverSolution);
			return new CspSolutionMapping(_context.CurrentModel, _sfSolver, constraintSolverSolution, _keyFromDecisionAndIndex, _keyFromDecisionWithoutIndex);
		}

		private CspTerm Expand(Term term)
		{
			if (!term.HasStructure(TermStructure.Integer))
			{
				throw new InvalidTermException(Resources.UnrecognizedCspTerm, term, "n");
			}
			while (term is IdentityTerm)
			{
				term = ((IdentityTerm)term)._input;
			}
			if (term.TryEvaluateConstantValue(out Rational value, _evaluationContext))
			{
				if (value.IsInteger())
				{
					return _sfSolver.Constant((int)value);
				}
				throw new InvalidTermException(Resources.ConstantValueIsNotAnInteger, term);
			}
			if (term is OperatorTerm operatorTerm)
			{
				List<CspTerm> list = new List<CspTerm>(operatorTerm._inputs.Length);
				Domain enumeratedDomain = GetEnumeratedDomain(operatorTerm);
				foreach (Term item in operatorTerm.AllInputs(_evaluationContext))
				{
					if (item.ValueClass == TermValueClass.String)
					{
						list.Add(Expand(enumeratedDomain, item));
					}
					else
					{
						list.Add(Expand(item));
					}
				}
				switch (operatorTerm.Operation)
				{
				case Operator.Abs:
					return _sfSolver.Abs(list[0]);
				case Operator.And:
					return _sfSolver.And(list.ToArray());
				case Operator.Equal:
					return _sfSolver.Equal(list.ToArray());
				case Operator.Greater:
					return _sfSolver.Greater(list.ToArray());
				case Operator.GreaterEqual:
					return _sfSolver.GreaterEqual(list.ToArray());
				case Operator.Less:
					return _sfSolver.Less(list.ToArray());
				case Operator.LessEqual:
					return _sfSolver.LessEqual(list.ToArray());
				case Operator.Minus:
					return _sfSolver.Neg(list[0]);
				case Operator.Not:
					return _sfSolver.Not(list[0]);
				case Operator.Or:
					return _sfSolver.Or(list.ToArray());
				case Operator.Plus:
					return _sfSolver.Sum(list.ToArray());
				case Operator.Power:
					return ExpandPower(list[0], operatorTerm.Inputs[1]);
				case Operator.Quotient:
					return ExpandQuotient(list[0], list[1]);
				case Operator.Times:
					return _sfSolver.Product(list.ToArray());
				case Operator.Unequal:
					return _sfSolver.Unequal(list.ToArray());
				case Operator.Max:
					return _sfSolver.Max(list.ToArray());
				case Operator.Min:
					return _sfSolver.Min(list.ToArray());
				case Operator.Cos:
				case Operator.Sin:
				case Operator.Tan:
				case Operator.ArcCos:
				case Operator.ArcSin:
				case Operator.ArcTan:
				case Operator.Cosh:
				case Operator.Sinh:
				case Operator.Tanh:
				case Operator.Exp:
				case Operator.Log:
				case Operator.Log10:
				case Operator.Sqrt:
				case Operator.If:
				case Operator.Ceiling:
				case Operator.Floor:
					throw new NotSupportedException();
				default:
					throw new InvalidTermException(Resources.InternalError, term);
				}
			}
			if (IsDecision(term))
			{
				int num = KeyForDecision(term);
				if (!_sfSolver.TryGetVariableFromKey(num, out var term2))
				{
					throw new InvalidTermException(Resources.InternalError, term);
				}
				return term2;
			}
			if (term is ElementOfTerm elementOfTerm)
			{
				Term[] tuple = elementOfTerm._tuple;
				Tuples tupleList = elementOfTerm._tupleList;
				Domain[] domains = tupleList._domains;
				foreach (Domain domain in domains)
				{
					if (!domain.IntRestricted)
					{
						throw new InvalidTermException(Resources.ElementOfOnlyAllowsIntegerAndEnumeratedDomains, term);
					}
				}
				if (tupleList.IsConstant)
				{
					if (tupleList.Data == null)
					{
						throw new InvalidTermException(Resources.ElementOfTuplesAreUnbound, term);
					}
				}
				else if (tupleList.Binding == null)
				{
					throw new InvalidTermException(Resources.ElementOfTuplesAreUnbound, term);
				}
				CspTerm[] array = new CspTerm[tuple.Length];
				for (int j = 0; j < array.Length; j++)
				{
					array[j] = Expand(tuple[j]);
				}
				int[][] inputs = tupleList.ExpandValuesInt().ToArray();
				return _sfSolver.TableInteger(array, inputs);
			}
			throw new InvalidTermException(Resources.UnrecognizedTerm, term);
		}

		private Domain GetEnumeratedDomain(OperatorTerm operatorTerm)
		{
			Domain result = null;
			foreach (Term item in operatorTerm.AllInputs(_evaluationContext))
			{
				if (item.ValueClass == TermValueClass.Enumerated)
				{
					result = item.EnumeratedDomain;
					break;
				}
				if (item.ValueClass != TermValueClass.String)
				{
					break;
				}
			}
			return result;
		}

		private CspTerm Expand(Domain enumeratedDomain, Term input)
		{
			CspTerm cspTerm = null;
			if (enumeratedDomain != null && enumeratedDomain.EnumeratedNames != null)
			{
				input.TryEvaluateConstantValue(out object value, _evaluationContext);
				string text = (string)value;
				for (int i = 0; i < enumeratedDomain.EnumeratedNames.Length; i++)
				{
					if (enumeratedDomain.EnumeratedNames[i] == text)
					{
						cspTerm = _sfSolver.Constant(i);
						break;
					}
				}
			}
			if (cspTerm == null)
			{
				throw new InvalidTermException(Resources.StringIsNotAMemberOfEnumeratedDomain, input);
			}
			return cspTerm;
		}

		private CspTerm ExpandPower(CspTerm baseTerm, Term powerTerm)
		{
			if (!powerTerm.TryEvaluateConstantValue(out Rational value, _evaluationContext))
			{
				throw new InvalidTermException(Resources.PowerIsNotConstant, powerTerm);
			}
			if (!value.IsInteger())
			{
				throw new InvalidTermException(Resources.PowerIsNotInteger, powerTerm);
			}
			if (value > ConstraintSystem.MaxFinite || value < ConstraintSystem.MinFinite)
			{
				throw new InvalidTermException(Resources.PowerIsOutsideOfAllowedBounds, powerTerm);
			}
			return _sfSolver.Power(baseTerm, (int)value);
		}

		private CspTerm ExpandQuotient(CspTerm dividend, CspTerm divisor)
		{
			CspTerm cspTerm = _sfSolver.CreateVariable(_sfSolver.DefaultInterval);
			_sfSolver.AddConstraints(_sfSolver.Equal(dividend, _sfSolver.Product(cspTerm, divisor)));
			return cspTerm;
		}

		private CspDomain Expand(Domain domain)
		{
			if (!_domains.TryGetValue(domain, out var value))
			{
				if (!domain.IntRestricted)
				{
					throw new ModelException(Resources.CSPSolverDoesNotSupportNonIntegerValues);
				}
				value = domain.MakeCspDomain(_sfSolver);
				_domains[domain] = value;
			}
			return value;
		}

		protected int KeyForNewDecision(Decision decision)
		{
			Domain domain = decision._domain;
			int num = _nextKey++;
			CspDomain domain2 = Expand(domain);
			_sfSolver.CreateVariable(domain2, num);
			return num;
		}

		public override void Dispose()
		{
			if (!_disposed && _sfSolver != null)
			{
				_sfSolver.Shutdown();
				_sfSolver = null;
				_disposed = true;
				GC.SuppressFinalize(this);
			}
			base.Dispose();
		}

		internal void SetDomain(Decision decision, Domain domain)
		{
			int num = KeyForDecision(decision, Task._emptyArray);
			if (_sfSolver.TryGetVariableFromKey(num, out var term))
			{
				CspVariable cspVariable = (CspVariable)term;
				cspVariable._values[0] = (CspSolverDomain)Expand(domain);
			}
		}

		internal Domain GetDomainAfterPresolve(Decision decision)
		{
			int num = KeyForDecision(decision, Task._emptyArray);
			if (!_sfSolver.TryGetVariableFromKey(num, out var term))
			{
				return decision._domain;
			}
			CspVariable cspVariable = (CspVariable)term;
			CspIntervalDomain cspIntervalDomain = cspVariable.FiniteValue as CspIntervalDomain;
			CspSetDomain cspSetDomain = cspVariable.FiniteValue as CspSetDomain;
			if (cspIntervalDomain != null)
			{
				return Domain.IntegerRange(cspIntervalDomain.First, cspIntervalDomain.Last);
			}
			if (cspSetDomain != null)
			{
				return Domain.Set(cspSetDomain.Set);
			}
			return decision._domain;
		}

		protected virtual void CleanModel()
		{
			_keyFromDecisionAndIndex = new Dictionary<IVariable, ValueTable<int>>();
			_keyFromDecisionWithoutIndex = new Dictionary<IVariable, int>();
			_nextKey = 0;
		}

		protected int KeyForDecision(Decision decision, object[] indexes)
		{
			if (indexes.Length == 0)
			{
				return KeyForDecision(decision);
			}
			if (!_keyFromDecisionAndIndex.ContainsKey(decision))
			{
				ValueSet[] indexSets = Task.DecisionValueSets(decision);
				_keyFromDecisionAndIndex[decision] = ValueTable<int>.Create(null, indexSets);
			}
			if (!_keyFromDecisionAndIndex[decision].TryGetValue(out var value, indexes))
			{
				for (int i = 0; i < indexes.Length; i++)
				{
					if (!decision._indexSets[i]._domain.IsValidValue(indexes[i]))
					{
						throw new MsfException(Resources.DomainIndexOutOfRange);
					}
				}
				value = KeyForNewDecision(decision);
				_keyFromDecisionAndIndex[decision].Add(value, indexes);
			}
			return value;
		}

		protected int KeyForDecision(Decision decision)
		{
			if (!_keyFromDecisionWithoutIndex.TryGetValue(decision, out var value))
			{
				value = KeyForNewDecision(decision);
				_keyFromDecisionWithoutIndex.Add(decision, value);
			}
			return value;
		}

		private int KeyForDecisionWithoutIndex(TermWithContext term)
		{
			Decision decision = GetDecision(term);
			if (decision._indexSets.Length > 0)
			{
				throw new InvalidTermException(Resources.IndexedDecisionUsedWithoutIndex, term.Term);
			}
			return KeyForDecision(decision, Task._emptyArray);
		}

		private int KeyForDecisionWithIndex(TermWithContext term)
		{
			Decision decision = null;
			object[] decisionIndexes = GetDecisionIndexes(term, out decision);
			return KeyForDecision(decision, decisionIndexes);
		}

		protected object[] GetDecisionIndexes(TermWithContext term, out Decision decision)
		{
			IndexTerm indexTerm = (IndexTerm)term.Term;
			decision = GetDecision(term, indexTerm);
			TermWithContext[] inputs = term.GetInputs();
			object[] array = new object[inputs.Length];
			for (int i = 0; i < inputs.Length; i++)
			{
				if (!inputs[i].TryEvaluateConstantValue(out object value))
				{
					throw new InvalidTermException(Resources.IndexIsNotConstant, inputs[i].Term);
				}
				Domain domain = indexTerm._table.IndexSets[i]._domain;
				if (domain.EnumeratedNames != null && value is string)
				{
					value = domain.GetOrdinal((string)value);
				}
				array[i] = value;
			}
			return array;
		}

		protected int KeyForDecision(Term term)
		{
			return KeyForDecision(new TermWithContext(term, _evaluationContext));
		}

		protected int KeyForDecision(TermWithContext term)
		{
			if (!IsDecision(term.Term))
			{
				throw new InvalidTermException(Resources.InternalError, term.Term);
			}
			if (term.Term is IndexTerm)
			{
				return KeyForDecisionWithIndex(term);
			}
			return KeyForDecisionWithoutIndex(term);
		}

		protected void AddGoalKey(Goal goal, int keyRow)
		{
			_keyFromDecisionAndIndex[goal] = ValueTable<int>.Create(null);
			_keyFromDecisionAndIndex[goal].Add(keyRow, Task._emptyArray);
		}

		internal override ISolverProperties GetSolverPropertiesInstance()
		{
			throw new InvalidSolverPropertyException(Resources.SolverDoesNotSupportGettingOrSettingProperties, InvalidSolverPropertyReason.SolverDoesNotSupportEvents);
		}

		internal override int FindDecisionVid(Decision decision, object[] indexes)
		{
			throw new NotSupportedException();
		}

		internal override object GetSolverProperty(string property, Decision decision, object[] indexes)
		{
			throw new InvalidSolverPropertyException(Resources.SolverDoesNotSupportGettingOrSettingProperties, InvalidSolverPropertyReason.SolverDoesNotSupportEvents);
		}

		internal override void SetSolverProperty(string property, Decision decision, object[] indexes, object value)
		{
			throw new InvalidSolverPropertyException(Resources.SolverDoesNotSupportGettingOrSettingProperties, InvalidSolverPropertyReason.SolverDoesNotSupportEvents);
		}

		internal override IEnumerable<object[]> GetIndexes(Decision decision)
		{
			if (_keyFromDecisionAndIndex != null && _keyFromDecisionAndIndex.TryGetValue(decision, out var value))
			{
				return value.Keys;
			}
			if (_keyFromDecisionWithoutIndex != null && _keyFromDecisionWithoutIndex.TryGetValue(decision, out var _))
			{
				return new object[1][] { SolverContext._emptyArray };
			}
			throw new MsfException(Resources.InternalError + string.Format(CultureInfo.InvariantCulture, "Cannot find indexes for decision {0}", new object[1] { decision.Name }));
		}
	}
}
