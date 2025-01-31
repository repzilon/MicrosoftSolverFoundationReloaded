using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	internal class TermTask : Task
	{
		internal class Builder
		{
			private ITermModel _solver;

			private Model _model;

			internal int[] _decisionVids;

			internal ValueTable<int>[] _decisionIndexedVids;

			internal readonly int[] _goalVids;

			internal Constraint[] _rowConstraint;

			internal int[] _rowSubconstraint;

			internal object[][] _rowIndexes;

			internal bool _useNamesAsKeys;

			internal bool _useRowNames;

			internal Term.EvaluationContext _evaluationContext;

			internal Builder(Model model, ITermModel solver)
				: this(model, solver, null)
			{
			}

			internal Builder(Model model, ITermModel solver, Term.EvaluationContext evaluationContext)
			{
				_model = model;
				_solver = solver;
				_evaluationContext = evaluationContext;
				_decisionVids = new int[model._maxDecisionId];
				_goalVids = new int[model._goals.Count];
				for (int i = 0; i < _decisionVids.Length; i++)
				{
					_decisionVids[i] = -1;
				}
				for (int j = 0; j < _goalVids.Length; j++)
				{
					_goalVids[j] = -1;
				}
				if (_useRowNames)
				{
					_rowConstraint = new Constraint[model._decisions.Count + model._constraints.Count + model._goals.Count];
				}
				_solver.AddConstant(0, out var _);
			}

			public void AddConstraints(IEnumerable<Constraint> constraints)
			{
				_evaluationContext.Goal = null;
				foreach (Constraint constraint in constraints)
				{
					if (!constraint.Enabled)
					{
						continue;
					}
					_evaluationContext.Constraint = constraint;
					foreach (Term item in constraint._term.AllValues(_evaluationContext))
					{
						AddConstraint(item);
					}
				}
			}

			public void AddGoal(Goal goal, int goalId)
			{
				_evaluationContext.Constraint = null;
				_evaluationContext.Goal = goal;
				int num = ExpandToUniqueVid(goal._term);
				_solver.AddGoal(num, goalId, goal.Direction == GoalKind.Minimize);
				_goalVids[goal._id] = num;
			}

			private static Decision GetDecision(TermWithContext term)
			{
				Decision decision = term.Term as Decision;
				return Task.GetSubmodelInstanceDecision(decision, term.Context);
			}

			private static Decision GetDecision(TermWithContext term, IndexTerm indexTerm)
			{
				if (!(indexTerm._table is Decision decision))
				{
					throw new InvalidTermException(Resources.CannotIndexNonDecision, indexTerm);
				}
				return Task.GetSubmodelInstanceDecision(decision, term.Context);
			}

			internal void SetSolver(ITermModel solver)
			{
				_solver = solver;
			}

			private void AddConstraint(Term term)
			{
				while (term is IdentityTerm)
				{
					term = ((IdentityTerm)term)._input;
				}
				if (term is OperatorTerm operatorTerm)
				{
					Term[] inputs = operatorTerm._inputs;
					switch (operatorTerm.Operation)
					{
					case Operator.Equal:
						if (!operatorTerm.HasStructure(TermStructure.Multivalue) && GetEnumeratedDomain(operatorTerm) == null)
						{
							AddEqualConstraint(inputs);
							return;
						}
						break;
					case Operator.GreaterEqual:
						if (!operatorTerm.HasStructure(TermStructure.Multivalue) && GetEnumeratedDomain(operatorTerm) == null)
						{
							AddGreaterEqualConstraint(inputs);
							return;
						}
						break;
					case Operator.LessEqual:
						if (!operatorTerm.HasStructure(TermStructure.Multivalue) && GetEnumeratedDomain(operatorTerm) == null)
						{
							AddLessEqualConstraint(inputs);
							return;
						}
						break;
					case Operator.And:
					{
						foreach (Term item in operatorTerm.AllInputs(_evaluationContext))
						{
							AddConstraint(item);
						}
						return;
					}
					case Operator.Or:
						if (!ShouldAddOr(operatorTerm.AllInputs(_evaluationContext)))
						{
							return;
						}
						break;
					}
				}
				foreach (Term item2 in term.AllValues(_evaluationContext))
				{
					int vid = ExpandToBoundableVid(item2);
					MergeBounds(vid, 1, 1);
				}
			}

			private void AddEqualConstraint(Term[] inputs)
			{
				if (inputs.Length == 2)
				{
					AddEquality(inputs[0], inputs[1]);
					return;
				}
				for (int i = 0; i < inputs.Length; i++)
				{
					if (!inputs[i].HasStructure(TermStructure.Constant))
					{
						continue;
					}
					for (int j = 0; j < inputs.Length; j++)
					{
						if (i != j)
						{
							AddEquality(inputs[i], inputs[j]);
						}
					}
					return;
				}
				for (int k = 1; k < inputs.Length; k++)
				{
					AddEquality(inputs[k], inputs[0]);
				}
			}

			private void AddGreaterEqualConstraint(Term[] inputs)
			{
				if (inputs.Length == 2)
				{
					AddHalfInequality(inputs[1], inputs[0]);
					return;
				}
				if (inputs.Length == 3)
				{
					AddFullInequality(inputs[2], inputs[1], inputs[0]);
					return;
				}
				for (int i = 0; i < inputs.Length - 1; i++)
				{
					AddHalfInequality(inputs[i + 1], inputs[i]);
				}
			}

			private void AddLessEqualConstraint(Term[] inputs)
			{
				if (inputs.Length == 2)
				{
					AddHalfInequality(inputs[0], inputs[1]);
					return;
				}
				if (inputs.Length == 3)
				{
					AddFullInequality(inputs[0], inputs[1], inputs[2]);
					return;
				}
				for (int i = 0; i < inputs.Length - 1; i++)
				{
					AddHalfInequality(inputs[i], inputs[i + 1]);
				}
			}

			/// <summary>
			/// This does some minor "presolve", but only the most important.
			/// If there is constant which is true there is no need for the constraint.
			/// </summary>
			/// <param name="inputs"></param>
			private bool ShouldAddOr(IEnumerable<Term> inputs)
			{
				foreach (Term input in inputs)
				{
					if (!input.HasStructure(TermStructure.Constant))
					{
						continue;
					}
					foreach (Term item in input.AllValues(_evaluationContext))
					{
						Rational rational = EvaluateConstant(item);
						if (rational != 0)
						{
							return false;
						}
					}
				}
				return true;
			}

			private void MergeBounds(int vid, Rational lower, Rational upper)
			{
				_solver.GetBounds(vid, out var lower2, out var upper2);
				Rational rational = ((lower2 < lower) ? lower : lower2);
				Rational rational2 = ((upper2 > upper) ? upper : upper2);
				if (lower2.IsIndeterminate || lower.IsIndeterminate)
				{
					rational = Rational.Indeterminate;
				}
				if (upper2.IsIndeterminate || upper.IsIndeterminate)
				{
					rational2 = Rational.Indeterminate;
				}
				if (rational > rational2)
				{
					int vid2 = MakeUniqueVid(vid);
					_solver.SetBounds(vid2, lower, upper);
				}
				else
				{
					_solver.SetBounds(vid, rational, rational2);
				}
			}

			private int ExpandToUniqueVid(Term term)
			{
				int vid = ExpandToVid(term);
				return MakeUniqueVid(vid);
			}

			private int ExpandToBoundableVid(Term term)
			{
				int vid = ExpandToVid(term);
				return MakeBoundableVid(vid);
			}

			private int MakeBoundableVid(int vid)
			{
				if (!_solver.IsOperation(vid) && !_solver.IsConstant(vid))
				{
					return vid;
				}
				return MakeUniqueVid(vid);
			}

			private int MakeUniqueVid(int vid)
			{
				if (!_solver.IsOperation(vid))
				{
					_solver.AddOperation(TermModelOperation.Identity, out vid, vid);
					return vid;
				}
				_solver.GetBounds(vid, out var lower, out var upper);
				if (lower != Rational.NegativeInfinity || upper != Rational.PositiveInfinity)
				{
					_solver.AddOperation(TermModelOperation.Identity, out vid, vid);
				}
				return vid;
			}

			private int ExpandToVid(Term term)
			{
				while (term is IdentityTerm)
				{
					term = ((IdentityTerm)term)._input;
				}
				if (term.HasStructure(TermStructure.Constant))
				{
					Rational value = EvaluateConstant(term);
					_solver.AddConstant(value, out var vid);
					return vid;
				}
				if (term is OperatorTerm operatorTerm)
				{
					List<int> list = new List<int>(operatorTerm._inputs.Length);
					Domain enumeratedDomain = GetEnumeratedDomain(operatorTerm);
					foreach (Term item in operatorTerm.AllInputs(_evaluationContext))
					{
						if (item.ValueClass == TermValueClass.String)
						{
							list.Add(ExpandToVid(enumeratedDomain, item));
						}
						else
						{
							list.Add(ExpandToVid(item));
						}
					}
					_ = list.Count;
					bool flag = false;
					bool flag2 = false;
					Operator operation = operatorTerm.Operation;
					TermModelOperation termModelOperation = GetTermModelOperation(operation);
					switch (operatorTerm.Operation)
					{
					case Operator.Minus:
					case Operator.Abs:
					case Operator.Not:
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
					case Operator.Ceiling:
					case Operator.Floor:
						flag = true;
						break;
					case Operator.Equal:
					case Operator.Unequal:
					case Operator.Greater:
					case Operator.Less:
					case Operator.GreaterEqual:
					case Operator.LessEqual:
						flag2 = true;
						break;
					case Operator.Max:
					case Operator.Min:
						flag2 = true;
						break;
					case Operator.If:
						flag2 = true;
						break;
					default:
						throw new InvalidTermException(Resources.InternalError, term);
					case Operator.Plus:
					case Operator.Times:
					case Operator.Quotient:
					case Operator.Power:
					case Operator.And:
					case Operator.Or:
						break;
					}
					if (flag)
					{
						_solver.AddOperation(termModelOperation, out var vidNew, list[0]);
						return vidNew;
					}
					if (flag2)
					{
						_solver.AddOperation(termModelOperation, out var vidNew2, list.ToArray());
						return vidNew2;
					}
					int vidNew3 = list[0];
					for (int i = 1; i < list.Count; i++)
					{
						_solver.AddOperation(termModelOperation, out vidNew3, vidNew3, list[i]);
					}
					return vidNew3;
				}
				if (term is Decision decisionTerm)
				{
					return GetDecisionVid(decisionTerm);
				}
				if (term is IndexTerm indexTerm)
				{
					return ExpandIndexTerm(indexTerm);
				}
				if (term is RowTerm rowTerm)
				{
					_solver.AddConstant(0, out var vid2);
					{
						foreach (LinearEntry rowEntry in rowTerm._model.GetRowEntries(rowTerm._vid))
						{
							int vid3 = ExpandToVid(rowTerm._variables[rowEntry.Index]);
							_solver.AddConstant(rowEntry.Value, out var vid4);
							_solver.AddOperation(TermModelOperation.Times, out var vidNew4, vid3, vid4);
							_solver.AddOperation(TermModelOperation.Plus, out vid2, vid2, vidNew4);
						}
						return vid2;
					}
				}
				throw new InvalidTermException(Resources.UnrecognizedTerm, term);
			}

			private int ExpandToVid(Domain enumeratedDomain, Term input)
			{
				input.TryEvaluateConstantValue(out object value, _evaluationContext);
				string text = (string)value;
				for (int i = 0; i < enumeratedDomain.EnumeratedNames.Length; i++)
				{
					if (enumeratedDomain.EnumeratedNames[i] == text)
					{
						_solver.AddConstant(i, out var vid);
						return vid;
					}
				}
				throw new InvalidTermException(Resources.StringIsNotAMemberOfEnumeratedDomain, input);
			}

			private Rational EvaluateConstant(Term term)
			{
				if (term is ConstantTerm constantTerm)
				{
					return constantTerm._value;
				}
				if (term.TryEvaluateConstantValue(out Rational value, _evaluationContext))
				{
					return value;
				}
				throw new MsfException(Resources.InternalError);
			}

			private int ExpandIndexTerm(IndexTerm indexTerm)
			{
				object[] array = new object[indexTerm._inputs.Length];
				for (int i = 0; i < indexTerm._inputs.Length; i++)
				{
					if (!indexTerm._inputs[i].TryEvaluateConstantValue(out array[i], _evaluationContext))
					{
						throw new MsfException(Resources.InternalError);
					}
					Domain domain = indexTerm._table.IndexSets[i]._domain;
					if (domain.EnumeratedNames != null && array[i] is string)
					{
						array[i] = domain.GetOrdinal((string)array[i]);
					}
				}
				if (indexTerm._table is Decision decisionTerm)
				{
					return GetDecisionVid(decisionTerm, array);
				}
				throw new NotImplementedException();
			}

			private void AddEquality(Term left, Term right)
			{
				if (right.HasStructure(TermStructure.Constant))
				{
					int vid = ExpandToBoundableVid(left);
					Rational rational = EvaluateConstant(right);
					MergeBounds(vid, rational, rational);
					return;
				}
				if (left.HasStructure(TermStructure.Constant))
				{
					int vid2 = ExpandToBoundableVid(right);
					Rational rational2 = EvaluateConstant(left);
					MergeBounds(vid2, rational2, rational2);
					return;
				}
				int vid3 = ExpandToVid(left);
				int vid4 = ExpandToVid(right);
				_solver.AddOperation(TermModelOperation.Minus, out var vidNew, vid4);
				_solver.AddOperation(TermModelOperation.Plus, out var vidNew2, vid3, vidNew);
				vidNew2 = MakeBoundableVid(vidNew2);
				MergeBounds(vidNew2, 0, 0);
			}

			private void AddHalfInequality(Term lower, Term upper)
			{
				if (upper.HasStructure(TermStructure.Constant))
				{
					int vid = ExpandToBoundableVid(lower);
					Rational upper2 = EvaluateConstant(upper);
					_solver.SetUpperBound(vid, upper2);
					return;
				}
				if (lower.HasStructure(TermStructure.Constant))
				{
					int vid2 = ExpandToBoundableVid(upper);
					Rational lower2 = EvaluateConstant(lower);
					_solver.SetLowerBound(vid2, lower2);
					return;
				}
				int vid3 = ExpandToVid(lower);
				int vid4 = ExpandToVid(upper);
				_solver.AddOperation(TermModelOperation.Minus, out var vidNew, vid3);
				_solver.AddOperation(TermModelOperation.Plus, out var vidNew2, vid4, vidNew);
				vidNew2 = MakeBoundableVid(vidNew2);
				_solver.SetLowerBound(vidNew2, 0);
			}

			private void AddFullInequality(Term lower, Term middle, Term upper)
			{
				if (lower.HasStructure(TermStructure.Constant) && upper.HasStructure(TermStructure.Constant))
				{
					int vid = ExpandToBoundableVid(middle);
					Rational lower2 = EvaluateConstant(lower);
					Rational upper2 = EvaluateConstant(upper);
					MergeBounds(vid, lower2, upper2);
				}
				else
				{
					AddHalfInequality(lower, middle);
					AddHalfInequality(middle, upper);
				}
			}

			private Domain GetEnumeratedDomain(OperatorTerm operatorTerm)
			{
				Domain result = null;
				Term[] inputs = operatorTerm._inputs;
				foreach (Term term in inputs)
				{
					if (term.ValueClass == TermValueClass.Enumerated)
					{
						result = term.EnumeratedDomain;
						break;
					}
					if (term.ValueClass != TermValueClass.String)
					{
						break;
					}
				}
				return result;
			}

			private int GetDecisionVid(Decision decisionTerm, object[] inputValues)
			{
				int id = decisionTerm._id;
				if (id < 0)
				{
					decisionTerm = GetDecision(new TermWithContext(decisionTerm, _evaluationContext));
					id = decisionTerm._id;
				}
				if (_decisionIndexedVids == null)
				{
					_decisionIndexedVids = new ValueTable<int>[_decisionVids.Length];
				}
				ValueTable<int> valueTable = _decisionIndexedVids[id];
				if (valueTable == null)
				{
					ValueSet[] array = new ValueSet[decisionTerm._indexSets.Length];
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = decisionTerm._indexSets[i].ValueSet;
					}
					valueTable = ValueTable<int>.Create(null, array);
					_decisionIndexedVids[id] = valueTable;
				}
				if (!valueTable.TryGetValue(out var value, inputValues))
				{
					for (int j = 0; j < inputValues.Length; j++)
					{
						if (!decisionTerm._indexSets[j]._domain.IsValidValue(inputValues[j]))
						{
							throw new MsfException(Resources.DomainIndexOutOfRange);
						}
					}
					value = MakeDecisionVid(decisionTerm, decisionTerm._domain, inputValues);
					ValueTable<Rational> hintValues = decisionTerm._hintValues;
					if (hintValues != null && hintValues.TryGetValue(out var value2, inputValues))
					{
						_solver.SetValue(value, value2);
					}
					valueTable.Add(value, inputValues);
				}
				return value;
			}

			private int GetDecisionVid(Decision decisionTerm)
			{
				int id = decisionTerm._id;
				if (id < 0)
				{
					decisionTerm = GetDecision(new TermWithContext(decisionTerm, _evaluationContext));
					id = decisionTerm._id;
				}
				int num = _decisionVids[id];
				if (num < 0)
				{
					num = MakeDecisionVid(decisionTerm, decisionTerm._domain);
					ValueTable<Rational> hintValues = decisionTerm._hintValues;
					if (hintValues != null && hintValues.TryGetValue(out var value, Task._emptyArray))
					{
						_solver.SetValue(num, value);
					}
					_decisionVids[id] = num;
				}
				return num;
			}

			private int MakeDecisionVid(Decision decision, Domain domain)
			{
				int vid;
				if (domain.ValidValues != null)
				{
					if (!_useNamesAsKeys)
					{
						_solver.AddVariable(null, out vid, domain.ValidValues);
					}
					else
					{
						_solver.AddVariable(decision.Name, out vid, domain.ValidValues);
					}
				}
				else if (!_useNamesAsKeys)
				{
					_solver.AddVariable(null, out vid, domain.MinValue, domain.MaxValue, domain.IntRestricted);
				}
				else
				{
					_solver.AddVariable(decision.Name, out vid, domain.MinValue, domain.MaxValue, domain.IntRestricted);
				}
				return vid;
			}

			private int MakeDecisionVid(Decision decision, Domain domain, object[] indexes)
			{
				int vid;
				if (!_useNamesAsKeys)
				{
					_solver.AddVariable(null, out vid, domain.MinValue, domain.MaxValue, domain.IntRestricted);
				}
				else
				{
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.Append(decision.Name);
					stringBuilder.Append(Statics.JoinArrayToString(indexes));
					_solver.AddVariable(stringBuilder.ToString(), out vid, domain.MinValue, domain.MaxValue, domain.IntRestricted);
				}
				return vid;
			}

			internal int FindDecisionVid(Decision decision, object[] indexes)
			{
				if ((object)decision == null)
				{
					return -1;
				}
				if (indexes.Length == 0)
				{
					if (_decisionVids == null)
					{
						throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.CannotFindTheDecisionAndIndexesInTheModel0, new object[1] { decision.ToString(indexes) }));
					}
					return _decisionVids[decision._id];
				}
				if (_decisionIndexedVids == null)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.CannotFindTheDecisionAndIndexesInTheModel0, new object[1] { decision.ToString(indexes) }));
				}
				ValueTable<int> valueTable = _decisionIndexedVids[decision._id];
				if (valueTable == null)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.CannotFindTheDecisionAndIndexesInTheModel0, new object[1] { decision.ToString(indexes) }));
				}
				if (!valueTable.TryGetValue(out var value, indexes))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.CannotFindTheDecisionAndIndexesInTheModel0, new object[1] { decision.ToString(indexes) }));
				}
				return value;
			}
		}

		private ITermSolver _solver;

		private Model _model;

		private Builder _builder;

		/// <summary>
		/// Set or get EvaluationContext
		/// </summary>
		/// <remarks>Update _evaluationContext of term builder of exists</remarks>
		public override Term.EvaluationContext EvaluationContext
		{
			get
			{
				return _evaluationContext;
			}
			set
			{
				_evaluationContext = value;
				if (_builder != null)
				{
					_builder._evaluationContext = value;
				}
			}
		}

		public TermTask(SolverContext context, Model model, ITermSolver solver, ISolverParameters solverParams, Directive directive)
			: base(context, solverParams, directive)
		{
			_solver = solver;
			_model = model;
			_builder = new Builder(model, new SimplifiedTermModelWrapper(solver));
		}

		protected override SolverContext.TaskSummary Solve(Func<bool> queryAbort)
		{
			MsfException exception = null;
			if (_solverParams != null)
			{
				_solverParams.QueryAbort = queryAbort;
			}
			if (_context.HasSolvingEvent && _solverParams is ISolverEvents solverEvents)
			{
				solverEvents.Solving = base.SolvingHook;
			}
			INonlinearSolution solution = null;
			try
			{
				solution = _solver.Solve(_solverParams);
			}
			catch (MsfException ex)
			{
				exception = ex;
			}
			SolverContext.TaskSummary taskSummary = new SolverContext.TaskSummary();
			taskSummary.SolutionMapping = GetSolutionMapping(solution);
			taskSummary.Solution = new Solution(_context, NonlinearTask.ResultQuality(solution));
			taskSummary.Directive = _directive;
			taskSummary.Solver = _solver;
			taskSummary.Exception = exception;
			return taskSummary;
		}

		public override void AddConstraints(IEnumerable<Constraint> constraints)
		{
			_builder.AddConstraints(constraints);
		}

		public override void AddGoal(Goal goal, int goalId)
		{
			_builder.AddGoal(goal, goalId);
		}

		public override void BuildModel()
		{
			_builder.SetSolver(_solver);
		}

		internal override ISolverProperties GetSolverPropertiesInstance()
		{
			return Task.GetSolverPropertiesInstance(_solver);
		}

		internal override int FindDecisionVid(Decision decision, object[] indexes)
		{
			return _builder.FindDecisionVid(decision, indexes);
		}

		internal override IEnumerable<object[]> GetIndexes(Decision decision)
		{
			if (_builder._decisionIndexedVids != null && _builder._decisionIndexedVids[decision._id] != null)
			{
				ValueTable<int> valueTable = _builder._decisionIndexedVids[decision._id];
				return valueTable.Keys;
			}
			if (_builder._decisionVids != null && _builder._decisionVids[decision._id] >= 0)
			{
				return new object[1][] { SolverContext._emptyArray };
			}
			throw new MsfException(Resources.InternalError + string.Format(CultureInfo.InvariantCulture, Resources.CannotFindIndexesForDecision0, new object[1] { decision.Name }));
		}

		internal override SolutionMapping GetSolutionMapping(ISolverSolution solution)
		{
			if (solution == null)
			{
				return null;
			}
			INonlinearSolution nonlinearSolution = solution as INonlinearSolution;
			DebugContracts.NonNull(nonlinearSolution);
			return new NonlinearSolutionMapping(_context.CurrentModel, _solver, nonlinearSolution, _builder._decisionVids, _builder._decisionIndexedVids, _builder._goalVids, _builder._rowConstraint, _builder._rowSubconstraint, _builder._rowIndexes);
		}

		public override void Dispose()
		{
			if (!_disposed && _solver != null)
			{
				_solver.Shutdown();
				_solver = null;
				_disposed = true;
				GC.SuppressFinalize(this);
			}
			base.Dispose();
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		internal static TermModelOperation GetTermModelOperation(Operator operatorTermOperation)
		{
			switch (operatorTermOperation)
			{
			case Operator.Abs:
				return TermModelOperation.Abs;
			case Operator.And:
				return TermModelOperation.And;
			case Operator.Equal:
				return TermModelOperation.Equal;
			case Operator.Greater:
				return TermModelOperation.Greater;
			case Operator.GreaterEqual:
				return TermModelOperation.GreaterEqual;
			case Operator.Less:
				return TermModelOperation.Less;
			case Operator.LessEqual:
				return TermModelOperation.LessEqual;
			case Operator.Minus:
				return TermModelOperation.Minus;
			case Operator.Not:
				return TermModelOperation.Not;
			case Operator.Or:
				return TermModelOperation.Or;
			case Operator.Plus:
				return TermModelOperation.Plus;
			case Operator.Power:
				return TermModelOperation.Power;
			case Operator.Quotient:
				return TermModelOperation.Quotient;
			case Operator.Times:
				return TermModelOperation.Times;
			case Operator.Unequal:
				return TermModelOperation.Unequal;
			case Operator.Max:
				return TermModelOperation.Max;
			case Operator.Min:
				return TermModelOperation.Min;
			case Operator.Cos:
				return TermModelOperation.Cos;
			case Operator.Sin:
				return TermModelOperation.Sin;
			case Operator.Tan:
				return TermModelOperation.Tan;
			case Operator.ArcCos:
				return TermModelOperation.ArcCos;
			case Operator.ArcSin:
				return TermModelOperation.ArcSin;
			case Operator.ArcTan:
				return TermModelOperation.ArcTan;
			case Operator.Cosh:
				return TermModelOperation.Cosh;
			case Operator.Sinh:
				return TermModelOperation.Sinh;
			case Operator.Tanh:
				return TermModelOperation.Tanh;
			case Operator.Exp:
				return TermModelOperation.Exp;
			case Operator.Log:
				return TermModelOperation.Log;
			case Operator.Log10:
				return TermModelOperation.Log10;
			case Operator.Sqrt:
				return TermModelOperation.Sqrt;
			case Operator.If:
				return TermModelOperation.If;
			case Operator.Ceiling:
				return TermModelOperation.Ceiling;
			case Operator.Floor:
				return TermModelOperation.Floor;
			default:
				return TermModelOperation.Identity;
			}
		}
	}
}
