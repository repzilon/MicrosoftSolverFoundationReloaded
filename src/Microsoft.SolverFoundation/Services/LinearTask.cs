using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// This is the Linear task to be used for solving models. 
	/// It has integer keys.
	/// </summary>
	internal class LinearTask : Task
	{
		internal const string ConvexityRowTag = "SFCX!";

		protected ILinearSolver _linearSolver;

		protected int _nextKey;

		protected int[] _decisionVids;

		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
		protected int[,] _recourseDecisionVids;

		private ValueTable<int>[] _decisionIndexedVids;

		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
		protected ValueTable<int>[,] _recourseDecisionIndexedVids;

		protected readonly int[] _goalVids;

		private readonly Rational[] _goalBoundsAdjustments;

		internal int _currentScenario;

		internal Constraint _currentConstraint;

		internal int _currentSubconstraint;

		internal int _currentConstraintRow;

		internal object[] _currentIndexes;

		internal Goal _currentGoal;

		internal Constraint[] _rowConstraint;

		internal int[] _rowSubconstraint;

		internal object[][] _rowIndexes;

		internal bool _useRecourseDecisions = true;

		internal bool _useNormalDecisions = true;

		internal bool _useRowBoundsAdjustments;

		internal bool _useRowNames = true;

		internal bool _useNamesAsKeys;

		internal bool _useEquality = true;

		internal ILinearSolver Solver => _linearSolver;

		public LinearTask(SolverContext context, Model model, ILinearSolver ls, ISolverParameters solverParams, Directive directive)
			: base(context, solverParams, directive)
		{
			if (ls == null)
			{
				throw new ArgumentNullException("ls");
			}
			_linearSolver = ls;
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
			_goalBoundsAdjustments = new Rational[model._goals.Count];
			if (_useRowNames)
			{
				_rowConstraint = new Constraint[model._decisions.Count + model._constraints.Count + model._goals.Count];
			}
			if (ls is LinearModel linearModel)
			{
				int count = model._decisions.Count;
				int num = model._constraints.Count + model._goals.Count;
				linearModel.Preallocate(count + num, num, count + 3 * num);
			}
		}

		internal override SolutionMapping GetSolutionMapping(ISolverSolution solution)
		{
			if (solution == null)
			{
				return null;
			}
			ILinearSolution linearSolution = solution as ILinearSolution;
			DebugContracts.NonNull(linearSolution);
			return new LinearSolutionMapping(_context.CurrentModel, _linearSolver, linearSolution, _decisionVids, _decisionIndexedVids, _recourseDecisionVids, _recourseDecisionIndexedVids, _goalVids, _goalBoundsAdjustments, _rowConstraint, _rowSubconstraint, _rowIndexes);
		}

		protected void CleanModel()
		{
			try
			{
				throw new NotImplementedException();
			}
			finally
			{
				_linearSolver.Shutdown();
				_linearSolver = null;
			}
		}

		protected override SolverContext.TaskSummary Solve(Func<bool> queryAbort)
		{
			MsfException exception = null;
			_solverParams.QueryAbort = queryAbort;
			if (_context.HasSolvingEvent && _solverParams is ISolverEvents solverEvents)
			{
				solverEvents.Solving = base.SolvingHook;
			}
			ILinearSolution linearSolution = null;
			try
			{
				linearSolution = _linearSolver.Solve(_solverParams);
			}
			catch (MsfException ex)
			{
				exception = ex;
			}
			SolverContext.TaskSummary taskSummary = new SolverContext.TaskSummary();
			taskSummary.SolutionMapping = GetSolutionMapping(linearSolution);
			taskSummary.Solution = new Solution(_context, GetSolverQuality(linearSolution));
			taskSummary.Directive = _directive;
			taskSummary.Solver = _linearSolver;
			taskSummary.Exception = exception;
			return taskSummary;
		}

		protected int KeyForDummyVariable()
		{
			return _nextKey++;
		}

		protected static SolverQuality GetSolverQuality(ILinearSolution linearSolution)
		{
			if (linearSolution == null)
			{
				return SolverQuality.Unknown;
			}
			return ConvertLinearResultToSolverQuality(linearSolution.Result, linearSolution.MipResult);
		}

		private static SolverQuality ConvertLinearResultToSolverQuality(LinearResult linearResult, LinearResult mipResult)
		{
			switch (linearResult)
			{
			case LinearResult.InfeasibleOrUnbounded:
				return SolverQuality.InfeasibleOrUnbounded;
			case LinearResult.InfeasiblePrimal:
				return SolverQuality.Infeasible;
			case LinearResult.Interrupted:
				if (mipResult == LinearResult.Feasible)
				{
					return SolverQuality.Feasible;
				}
				return SolverQuality.Unknown;
			case LinearResult.Invalid:
				return SolverQuality.Unknown;
			case LinearResult.Optimal:
				return SolverQuality.Optimal;
			case LinearResult.Feasible:
				return SolverQuality.Feasible;
			case LinearResult.UnboundedDual:
				return SolverQuality.Infeasible;
			case LinearResult.UnboundedPrimal:
				return SolverQuality.Unbounded;
			default:
				return SolverQuality.Unknown;
			}
		}

		/// <summary>
		/// This is called the Task creation process.
		/// It should be ommited when (sfs) model analysis is on.
		/// Even before that, there can be a parameter says if Mip is allowed
		/// just like we have for QP, so filling the model with throw
		/// </summary>
		/// <returns></returns>
		public bool IsMipModel()
		{
			return _linearSolver.IntegerIndexCount > 0;
		}

		public override void Dispose()
		{
			if (!_disposed && _linearSolver != null)
			{
				_linearSolver.Shutdown();
				_linearSolver = null;
				_disposed = true;
				GC.SuppressFinalize(this);
			}
			base.Dispose();
		}

		protected void SetVariableBounds(Domain domain, int vid)
		{
			if (domain.ValidValues != null)
			{
				throw new ModelException(Resources.OnlyIntervalDomainsAreAllowedForSimplex);
			}
			_linearSolver.SetBounds(vid, domain.MinValue, domain.MaxValue);
			_linearSolver.SetIntegrality(vid, domain.IntRestricted);
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
				_currentConstraint = constraint;
				_currentConstraintRow = 0;
				if (constraint.Term.HasStructure(TermStructure.Multivalue))
				{
					foreach (Term item in constraint.Term.AllValues(_evaluationContext))
					{
						_currentSubconstraint = 0;
						_currentIndexes = _evaluationContext.Keys.ToArray();
						AddConstraintInner(item);
					}
				}
				else
				{
					_currentSubconstraint = 0;
					_currentIndexes = Task._emptyArray;
					AddConstraintInner(constraint.Term);
				}
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		private void AddConstraintInner(Term term)
		{
			while (true)
			{
				if (term.HasStructure(TermStructure.Constant))
				{
					Rational rational = EvaluateConstant(term);
					if (rational == 0)
					{
						ForceInfeasibility();
					}
					return;
				}
				switch (term.TermType)
				{
				case TermType.Identity:
					goto IL_003d;
				case TermType.Operator:
				{
					OperatorTerm operatorTerm = (OperatorTerm)term;
					Term[] inputs = operatorTerm._inputs;
					switch (operatorTerm.Operation)
					{
					case Operator.LessEqual:
					{
						switch (inputs.Length)
						{
						case 2:
							AddHalfInequality(inputs[0], inputs[1]);
							return;
						case 3:
							if (inputs[0].HasStructure(TermStructure.Constant) && inputs[2].HasStructure(TermStructure.Constant))
							{
								Rational rational2 = EvaluateConstant(inputs[0]);
								Rational rational3 = EvaluateConstant(inputs[2]);
								if (_useEquality || !(rational2 == rational3))
								{
									int num = AddRow();
									Rational boundsAdjustment = 0;
									AddTermToRow(inputs[1], num, ref boundsAdjustment, 1);
									_linearSolver.SetBounds(num, rational2 + boundsAdjustment, rational3 + boundsAdjustment);
									_ = _useRowBoundsAdjustments;
									return;
								}
							}
							break;
						}
						for (int j = 0; j < inputs.Length - 1; j++)
						{
							AddHalfInequality(inputs[j], inputs[j + 1]);
						}
						return;
					}
					case Operator.GreaterEqual:
					{
						switch (inputs.Length)
						{
						case 2:
							AddHalfInequality(inputs[1], inputs[0]);
							return;
						case 3:
							if (inputs[0].HasStructure(TermStructure.Constant) && inputs[2].HasStructure(TermStructure.Constant))
							{
								Rational rational4 = EvaluateConstant(inputs[0]);
								Rational rational5 = EvaluateConstant(inputs[2]);
								if (_useEquality || !(rational5 == rational4))
								{
									int num2 = AddRow();
									Rational boundsAdjustment2 = 0;
									AddTermToRow(inputs[1], num2, ref boundsAdjustment2, 1);
									_linearSolver.SetBounds(num2, rational5 + boundsAdjustment2, rational4 + boundsAdjustment2);
									_ = _useRowBoundsAdjustments;
									return;
								}
							}
							break;
						}
						for (int l = 0; l < inputs.Length - 1; l++)
						{
							AddHalfInequality(inputs[l + 1], inputs[l]);
						}
						return;
					}
					case Operator.Equal:
					{
						if (inputs.Length == 2 && !inputs[0].HasStructure(TermStructure.Multivalue) && !inputs[1].HasStructure(TermStructure.Multivalue))
						{
							AddEquality(inputs[0], inputs[1]);
							return;
						}
						int vidVar = AddDummyVariable();
						for (int k = 0; k < inputs.Length; k++)
						{
							if (inputs[k].HasStructure(TermStructure.Multivalue))
							{
								foreach (Term item in inputs[k].AllValues(_evaluationContext))
								{
									AddEquality(vidVar, item);
								}
							}
							else
							{
								AddEquality(vidVar, inputs[k]);
							}
						}
						return;
					}
					case Operator.And:
					{
						for (int i = 0; i < inputs.Length; i++)
						{
							if (inputs[i].HasStructure(TermStructure.Multivalue))
							{
								foreach (Term item2 in inputs[i].AllValues(_evaluationContext))
								{
									AddConstraintInner(item2);
								}
							}
							else
							{
								AddConstraintInner(inputs[i]);
							}
						}
						return;
					}
					case Operator.Or:
						AddOr(inputs);
						return;
					case Operator.Sos1:
						AddSos(inputs, SpecialOrderedSetType.SOS1, hasConvexity: false);
						return;
					case Operator.Sos2:
						AddSos(inputs, SpecialOrderedSetType.SOS2, hasConvexity: true);
						return;
					case Operator.Sos1Row:
						AddSosRow(inputs[1] as RowTerm, inputs[0], inputs[2], SpecialOrderedSetType.SOS1);
						return;
					case Operator.Sos2Row:
						AddSosRow(inputs[1] as RowTerm, inputs[0], inputs[2], SpecialOrderedSetType.SOS2);
						return;
					}
					break;
				}
				}
				break;
				IL_003d:
				term = ((IdentityTerm)term)._input;
			}
			if (term.HasStructure(TermStructure.Linear))
			{
				Rational boundsAdjustment3 = 1;
				int num3 = AddRow();
				AddTermToRow(term, num3, ref boundsAdjustment3, 1);
				_linearSolver.SetBounds(num3, boundsAdjustment3, boundsAdjustment3);
				_ = _useRowBoundsAdjustments;
				return;
			}
			throw new MsfException(Resources.InternalError);
		}

		private string GetRowName()
		{
			if (_currentConstraint == null)
			{
				return _currentGoal.Name;
			}
			if (_currentConstraintRow == 0)
			{
				_currentConstraintRow++;
				return _currentConstraint.Name;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(_currentConstraint.Name);
			stringBuilder.Append("#");
			stringBuilder.Append(_currentConstraintRow++);
			return stringBuilder.ToString();
		}

		private void ForceInfeasibility()
		{
			int num = AddRow();
			int num2 = AddDummyVariable();
			_linearSolver.SetCoefficient(num, num2, 1);
			_linearSolver.SetBounds(num, 0, 0);
			_linearSolver.SetBounds(num2, 1, 1);
		}

		private int AddRow()
		{
			int vid;
			if (!_useNamesAsKeys)
			{
				_linearSolver.AddRow(null, out vid);
			}
			else
			{
				_linearSolver.AddRow(GetRowName(), out vid);
			}
			if (_useRowNames)
			{
				SaveCurrentConstraint(vid);
			}
			return vid;
		}

		private int AddRow(string tag)
		{
			int vid;
			if (!_useNamesAsKeys)
			{
				_linearSolver.AddRow(null, out vid);
			}
			else
			{
				_linearSolver.AddRow(tag + GetRowName(), out vid);
			}
			if (_useRowNames)
			{
				SaveCurrentConstraint(vid);
			}
			return vid;
		}

		private int AddRow(SpecialOrderedSetType type)
		{
			int vidRow;
			if (!_useNamesAsKeys)
			{
				_linearSolver.AddRow(null, type, out vidRow);
			}
			else
			{
				_linearSolver.AddRow(GetRowName(), type, out vidRow);
			}
			if (_useRowNames)
			{
				SaveCurrentConstraint(vidRow);
			}
			return vidRow;
		}

		private int AddDummyVariable()
		{
			int vid;
			if (!_useNamesAsKeys)
			{
				_linearSolver.AddVariable(null, out vid);
			}
			else
			{
				_linearSolver.AddVariable(KeyForDummyVariable(), out vid);
			}
			return vid;
		}

		private void SaveCurrentConstraint(int vidRow)
		{
			Task.EnsureArraySize(ref _rowConstraint, vidRow);
			_rowConstraint[vidRow] = _currentConstraint;
			if (_currentSubconstraint > 0)
			{
				Task.EnsureArraySize(ref _rowSubconstraint, vidRow);
				_rowSubconstraint[vidRow] = _currentSubconstraint;
			}
			if (_currentIndexes.Length > 0)
			{
				Task.EnsureArraySize(ref _rowIndexes, vidRow);
				_rowIndexes[vidRow] = _currentIndexes;
			}
			_currentSubconstraint++;
		}

		private void AddOr(Term[] inputs)
		{
			int num = 0;
			for (int i = 0; i < inputs.Length; i++)
			{
				if (inputs[i].HasStructure(TermStructure.Multivalue))
				{
					if (!inputs[i].HasStructure(TermStructure.Constant))
					{
						throw new MsfException(Resources.InternalError);
					}
					foreach (Term item in inputs[i].AllValues(_evaluationContext))
					{
						Rational rational = EvaluateConstant(item);
						if (rational != 0)
						{
							return;
						}
					}
				}
				else if (inputs[i].HasStructure(TermStructure.Constant))
				{
					Rational rational2 = EvaluateConstant(inputs[i]);
					if (rational2 != 0)
					{
						break;
					}
				}
				else
				{
					if (++num > 1)
					{
						throw new MsfException(Resources.InternalError);
					}
					AddConstraintInner(inputs[i]);
				}
			}
		}

		private void AddSosRow(RowTerm sosRow, Term lowerBound, Term upperBound, SpecialOrderedSetType type)
		{
			int num = AddRow(type);
			Rational rational = EvaluateConstant(lowerBound);
			Rational rational2 = EvaluateConstant(upperBound);
			Rational boundsAdjustment = 0;
			AddRowTerm(num, sosRow, 1, ref boundsAdjustment);
			_linearSolver.SetBounds(num, rational + boundsAdjustment, rational2 + boundsAdjustment);
			_ = _useRowBoundsAdjustments;
		}

		private void AddSos(Term[] inputs, SpecialOrderedSetType type, bool hasConvexity)
		{
			Term term = null;
			bool flag = false;
			Term term2;
			if (inputs[0].HasStructure(TermStructure.LinearInequality))
			{
				if (!(inputs[0] is EqualTerm equalTerm))
				{
					throw new MsfException(Resources.InternalError);
				}
				if (equalTerm._inputs.Length != 2)
				{
					throw new MsfException(Resources.InternalError);
				}
				term2 = equalTerm._inputs[1];
				term = equalTerm._inputs[0];
				flag = true;
			}
			else
			{
				if (!inputs[0].HasStructure(TermStructure.Linear))
				{
					throw new MsfException(Resources.InternalError);
				}
				term2 = inputs[0];
			}
			int vidRowSos = AddRow(type);
			int num = 0;
			int num2 = 0;
			if (flag)
			{
				num = AddRow();
			}
			if (hasConvexity)
			{
				num2 = AddRow("SFCX!");
				_linearSolver.SetBounds(num2, 1, 1);
			}
			if (!(term2 is PlusTerm plusTerm))
			{
				throw new MsfException(Resources.InternalError);
			}
			for (int i = 0; i < plusTerm._inputs.Length; i++)
			{
				HandleSosTermElement(plusTerm._inputs[i], vidRowSos, hasConvexity, num2, flag, num);
			}
			if (flag)
			{
				Rational boundsAdjustment = 0;
				AddTermToRow(term, num, ref boundsAdjustment, -1);
				_linearSolver.SetBounds(num, boundsAdjustment, boundsAdjustment);
				_ = _useRowBoundsAdjustments;
			}
		}

		private void HandleSosTermElement(Term element, int vidRowSos2, bool hasConvexity, int vidRowConvexity, bool hasReferenceTerm, int vidRowReference)
		{
			while (element is PlusTerm plusTerm)
			{
				for (int i = 1; i < plusTerm._inputs.Length; i++)
				{
					HandleSosTermElement(plusTerm._inputs[i], vidRowSos2, hasConvexity, vidRowConvexity, hasReferenceTerm, vidRowReference);
				}
				element = plusTerm._inputs[0];
			}
			if (element.HasStructure(TermStructure.Multivalue))
			{
				foreach (Term item in element.AllValues(_evaluationContext))
				{
					HandleBasicSosTermElement(item, vidRowSos2, hasConvexity, vidRowConvexity, hasReferenceTerm, vidRowReference);
				}
				return;
			}
			HandleBasicSosTermElement(element, vidRowSos2, hasConvexity, vidRowConvexity, hasReferenceTerm, vidRowReference);
		}

		private Rational HandleBasicSosTermElement(Term e, int vidRowSos2, bool hasConvexity, int vidRowConvexity, bool hasReferenceTerm, int vidRowReference)
		{
			Rational coefficient = 1;
			int vidVarRow;
			int vidVar = LinearTermVid(e, ref coefficient, out vidVarRow);
			_linearSolver.SetCoefficient(vidRowSos2, vidVar, coefficient);
			if (hasConvexity)
			{
				_linearSolver.SetCoefficient(vidRowConvexity, vidVar, 1);
			}
			if (hasReferenceTerm)
			{
				_linearSolver.SetCoefficient(vidRowReference, vidVar, coefficient);
			}
			return coefficient;
		}

		public override void AddGoal(Goal goal, int priority)
		{
			AddGoal(priority, goal, 1);
		}

		internal void AddGoal(int priority, Goal goal, Rational weight)
		{
			_currentConstraint = null;
			_currentSubconstraint = 0;
			_currentIndexes = Task._emptyArray;
			_currentGoal = goal;
			_currentConstraintRow = 0;
			int num = AddRow();
			_linearSolver.AddGoal(num, priority, goal.Direction == GoalKind.Minimize);
			Rational boundsAdjustment = 0;
			AddTermToRow(goal._term, num, ref boundsAdjustment, weight);
			_ = _useRowBoundsAdjustments;
			_goalVids[goal._id] = num;
			_goalBoundsAdjustments[goal._id] = boundsAdjustment;
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

		private void AddHalfInequality(Term lower, Term upper)
		{
			int num = AddRow();
			Rational boundsAdjustment = 0;
			if (upper.HasStructure(TermStructure.Constant))
			{
				Rational rational = EvaluateConstant(upper);
				AddTermToRow(lower, num, ref boundsAdjustment, 1);
				_linearSolver.SetUpperBound(num, rational + boundsAdjustment);
				_ = _useRowBoundsAdjustments;
			}
			else if (lower.HasStructure(TermStructure.Constant))
			{
				Rational rational2 = EvaluateConstant(lower);
				AddTermToRow(upper, num, ref boundsAdjustment, 1);
				_linearSolver.SetLowerBound(num, rational2 + boundsAdjustment);
				_ = _useRowBoundsAdjustments;
			}
			else
			{
				AddTermToRow(upper, num, ref boundsAdjustment, 1);
				AddTermToRow(lower, num, ref boundsAdjustment, -1);
				_linearSolver.SetLowerBound(num, boundsAdjustment);
				_ = _useRowBoundsAdjustments;
			}
		}

		private void AddEquality(Term left, Term right)
		{
			if (!_useEquality)
			{
				AddHalfInequality(left, right);
				AddHalfInequality(right, left);
				return;
			}
			int num = AddRow();
			Rational boundsAdjustment = 0;
			if (left.HasStructure(TermStructure.Constant))
			{
				Rational rational = EvaluateConstant(left);
				AddTermToRow(right, num, ref boundsAdjustment, 1);
				rational += boundsAdjustment;
				_linearSolver.SetBounds(num, rational, rational);
				_ = _useRowBoundsAdjustments;
			}
			else if (right.HasStructure(TermStructure.Constant))
			{
				Rational rational2 = EvaluateConstant(right);
				AddTermToRow(left, num, ref boundsAdjustment, 1);
				rational2 += boundsAdjustment;
				_linearSolver.SetBounds(num, rational2, rational2);
				_ = _useRowBoundsAdjustments;
			}
			else
			{
				AddTermToRow(left, num, ref boundsAdjustment, 1);
				AddTermToRow(right, num, ref boundsAdjustment, -1);
				_linearSolver.SetBounds(num, boundsAdjustment, boundsAdjustment);
				_ = _useRowBoundsAdjustments;
			}
		}

		private void AddEquality(int vidVar, Term term)
		{
			if (!_useEquality)
			{
				AddEqualityAsTwoInequalities(vidVar, term);
				return;
			}
			int num = AddRow();
			Rational boundsAdjustment = 0;
			if (term.HasStructure(TermStructure.Constant))
			{
				Rational rational = EvaluateConstant(term);
				_linearSolver.SetCoefficient(num, vidVar, 1);
				_linearSolver.SetBounds(num, rational, rational);
			}
			else
			{
				AddTermToRow(term, num, ref boundsAdjustment, 1);
				_linearSolver.SetCoefficient(num, vidVar, -1);
				_linearSolver.SetBounds(num, boundsAdjustment, boundsAdjustment);
				_ = _useRowBoundsAdjustments;
			}
		}

		private void AddEqualityAsTwoInequalities(int vidVar, Term term)
		{
			int num = AddRow();
			int num2 = AddRow();
			Rational boundsAdjustment = Rational.Zero;
			if (term.HasStructure(TermStructure.Constant))
			{
				Rational rational = EvaluateConstant(term);
				_linearSolver.SetCoefficient(num, vidVar, Rational.One);
				_linearSolver.SetCoefficient(num2, vidVar, Rational.One);
				_linearSolver.SetBounds(num, Rational.NegativeInfinity, rational);
				_linearSolver.SetBounds(num2, rational, Rational.PositiveInfinity);
			}
			else
			{
				Rational num3 = Rational.One;
				Rational.Negate(ref num3);
				AddTermToRow(term, num, ref boundsAdjustment, Rational.One);
				AddTermToRow(term, num2, ref boundsAdjustment, Rational.One);
				_linearSolver.SetCoefficient(num, vidVar, num3);
				_linearSolver.SetCoefficient(num2, vidVar, num3);
				_linearSolver.SetBounds(num, Rational.NegativeInfinity, boundsAdjustment);
				_linearSolver.SetBounds(num2, boundsAdjustment, Rational.PositiveInfinity);
				_ = _useRowBoundsAdjustments;
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		protected void AddTermToRow(Term term, int vidRow, ref Rational boundsAdjustment, Rational coefficient)
		{
			while (!term.HasStructure(TermStructure.Constant))
			{
				Term term3;
				switch (term.TermType)
				{
				case TermType.Decision:
					AddDecision(term, vidRow, coefficient);
					return;
				case TermType.Operator:
				{
					OperatorTerm operatorTerm = (OperatorTerm)term;
					Term[] inputs = operatorTerm._inputs;
					switch (operatorTerm.Operation)
					{
					case Operator.Plus:
					{
						for (int i = 1; i < inputs.Length; i++)
						{
							Term term2 = inputs[i];
							if (term2.HasStructure(TermStructure.Multivalue))
							{
								foreach (Term item in term2.AllValues(_evaluationContext))
								{
									AddTermToRow(item, vidRow, ref boundsAdjustment, coefficient);
								}
							}
							else if (term2.TermType == TermType.Decision)
							{
								AddDecision(term2, vidRow, coefficient);
							}
							else
							{
								AddTermToRow(term2, vidRow, ref boundsAdjustment, coefficient);
							}
						}
						term3 = inputs[0];
						if (!term3.HasStructure(TermStructure.Multivalue))
						{
							break;
						}
						{
							foreach (Term item2 in term3.AllValues(_evaluationContext))
							{
								AddTermToRow(item2, vidRow, ref boundsAdjustment, coefficient);
							}
							return;
						}
					}
					case Operator.Times:
						AddTimes(inputs, vidRow, ref boundsAdjustment, coefficient);
						return;
					case Operator.Minus:
						AddTermToRow(inputs[0], vidRow, ref boundsAdjustment, -coefficient);
						return;
					case Operator.Quotient:
						AddQuotient(inputs, vidRow, ref boundsAdjustment, coefficient);
						return;
					case Operator.Power:
						AddPower(inputs, vidRow, ref boundsAdjustment, coefficient);
						return;
					default:
						throw new MsfException(Resources.InternalError);
					}
					break;
				}
				case TermType.Index:
					AddIndexTerm((IndexTerm)term, vidRow, coefficient);
					return;
				case TermType.Row:
					AddRowTerm(vidRow, (RowTerm)term, coefficient, ref boundsAdjustment);
					return;
				case TermType.RecourseDecision:
					AddRecourseDecision(vidRow, (RecourseDecision)term, coefficient);
					return;
				default:
					throw new MsfException(Resources.InternalError);
				}
				term = term3;
			}
			AddConstant(term, ref boundsAdjustment, coefficient);
		}

		private void AddDecision(Term term, int vidRow, Rational coefficient)
		{
			if (_useNormalDecisions)
			{
				int decisionVid = GetDecisionVid((Decision)term);
				Rational coefficient2 = _linearSolver.GetCoefficient(vidRow, decisionVid);
				coefficient2 += coefficient;
				_linearSolver.SetCoefficient(vidRow, decisionVid, coefficient2);
			}
		}

		private void AddConstant(Term term, ref Rational boundsAdjustment, Rational coefficient)
		{
			Rational rational = EvaluateConstant(term);
			boundsAdjustment -= rational * coefficient;
		}

		private void AddQuotient(Term[] inputs, int vidRow, ref Rational boundsAdjustment, Rational coefficient)
		{
			if (inputs[1].HasStructure(TermStructure.Constant))
			{
				Rational rational = EvaluateConstant(inputs[1]);
				AddTermToRow(inputs[0], vidRow, ref boundsAdjustment, coefficient / rational);
				return;
			}
			throw new MsfException(Resources.InternalError);
		}

		private void AddTimes(Term[] inputs, int vidRow, ref Rational boundsAdjustment, Rational coefficient)
		{
			Term term = null;
			Term term2 = null;
			int num = 0;
			for (int i = 0; i < inputs.Length; i++)
			{
				if (inputs[i].HasStructure(TermStructure.Multivalue))
				{
					if (!inputs[i].HasStructure(TermStructure.Constant))
					{
						throw new MsfException(Resources.InternalError);
					}
					foreach (Term item in inputs[i].AllValues(_evaluationContext))
					{
						Rational rational = EvaluateConstant(item);
						coefficient *= rational;
					}
				}
				if (inputs[i].HasStructure(TermStructure.Constant))
				{
					Rational rational2 = EvaluateConstant(inputs[i]);
					coefficient *= rational2;
					continue;
				}
				switch (++num)
				{
				case 1:
					term = inputs[i];
					break;
				case 2:
					term2 = inputs[i];
					break;
				default:
					throw new MsfException(Resources.InternalError);
				}
			}
			switch (num)
			{
			case 0:
				boundsAdjustment -= coefficient;
				return;
			case 1:
				AddTermToRow(term, vidRow, ref boundsAdjustment, coefficient);
				return;
			case 2:
				if (term.HasStructure(TermStructure.Linear) && term2.HasStructure(TermStructure.Linear))
				{
					AddQuadraticProduct(vidRow, coefficient, term, term2, ref boundsAdjustment);
					return;
				}
				break;
			}
			throw new MsfException(Resources.InternalError);
		}

		private void AddRecourseDecision(int vidRow, RecourseDecision recourseDecisionTerm, Rational coefficient)
		{
			if (_useRecourseDecisions)
			{
				int id = recourseDecisionTerm._id;
				if (id < 0)
				{
					recourseDecisionTerm = Task.GetSubmodelInstanceDecision(recourseDecisionTerm, _evaluationContext);
					id = recourseDecisionTerm._id;
				}
				int num = _recourseDecisionVids[id, _currentScenario];
				if (num == 0)
				{
					num = MakeDecisionVid(recourseDecisionTerm, recourseDecisionTerm._domain);
					_recourseDecisionVids[id, _currentScenario] = num;
				}
				Rational coefficient2 = _linearSolver.GetCoefficient(vidRow, num);
				coefficient2 += coefficient;
				_linearSolver.SetCoefficient(vidRow, num, coefficient2);
			}
		}

		private void AddIndexTerm(IndexTerm indexTerm, int vidRow, Rational coefficient)
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
				if (_useNormalDecisions)
				{
					int decisionVid = GetDecisionVid(decisionTerm, array);
					Rational coefficient2 = _linearSolver.GetCoefficient(vidRow, decisionVid);
					coefficient2 += coefficient;
					_linearSolver.SetCoefficient(vidRow, decisionVid, coefficient2);
				}
				return;
			}
			RecourseDecision recourseDecision = indexTerm._table as RecourseDecision;
			if ((object)recourseDecision != null)
			{
				if (!_useRecourseDecisions)
				{
					return;
				}
				int id = recourseDecision._id;
				if (id < 0)
				{
					recourseDecision = Task.GetSubmodelInstanceDecision(recourseDecision, _evaluationContext);
					id = recourseDecision._id;
				}
				ValueTable<int> valueTable = _recourseDecisionIndexedVids[id, _currentScenario];
				if (valueTable == null)
				{
					ValueSet[] array2 = new ValueSet[recourseDecision._indexSets.Length];
					for (int j = 0; j < array2.Length; j++)
					{
						array2[j] = recourseDecision._indexSets[j].ValueSet;
					}
					valueTable = ValueTable<int>.Create(null, array2);
					_recourseDecisionIndexedVids[id, _currentScenario] = valueTable;
				}
				if (!valueTable.TryGetValue(out var value, array))
				{
					for (int k = 0; k < array.Length; k++)
					{
						if (!recourseDecision._indexSets[k]._domain.IsValidValue(array[k]))
						{
							throw new MsfException(Resources.DomainIndexOutOfRange);
						}
					}
					value = MakeDecisionVid(recourseDecision, recourseDecision._domain, array);
					valueTable.Add(value, array);
				}
				Rational coefficient3 = _linearSolver.GetCoefficient(vidRow, value);
				coefficient3 += coefficient;
				_linearSolver.SetCoefficient(vidRow, value, coefficient3);
				return;
			}
			throw new NotImplementedException();
		}

		private void AddPower(Term[] inputs, int vidRow, ref Rational boundsAdjustment, Rational coefficient)
		{
			if (inputs[1].HasStructure(TermStructure.Constant))
			{
				Rational rational = EvaluateConstant(inputs[1]);
				if (rational == 0)
				{
					boundsAdjustment -= (Rational)1;
					return;
				}
				if (rational == 1)
				{
					AddTermToRow(inputs[0], vidRow, ref boundsAdjustment, coefficient);
					return;
				}
				if (rational == 2)
				{
					if (inputs[0].HasStructure(TermStructure.Linear))
					{
						int vidVarRow;
						int num = LinearTermVid(inputs[0], ref coefficient, out vidVarRow);
						Rational coefficient2 = _linearSolver.GetCoefficient(vidRow, num, num);
						coefficient2 += coefficient;
						_linearSolver.SetCoefficient(vidRow, coefficient2, num, num);
						return;
					}
					throw new MsfException(Resources.InternalError);
				}
				throw new MsfException(Resources.InternalError);
			}
			throw new MsfException(Resources.InternalError);
		}

		private void AddRowTerm(int vidRow, RowTerm rowTerm, Rational coefficient, ref Rational boundsAdjustment)
		{
			foreach (LinearEntry rowEntry in rowTerm._model.GetRowEntries(rowTerm._vid))
			{
				Rational value = rowEntry.Value;
				Term term = rowTerm._variables[rowEntry.Index];
				if (term is Decision decisionTerm)
				{
					if (_useNormalDecisions)
					{
						int decisionVid = GetDecisionVid(decisionTerm);
						Rational coefficient2 = _linearSolver.GetCoefficient(vidRow, decisionVid);
						coefficient2 += coefficient * value;
						_linearSolver.SetCoefficient(vidRow, decisionVid, coefficient2);
					}
				}
				else
				{
					AddTermToRow(term, vidRow, ref boundsAdjustment, coefficient * value);
				}
			}
		}

		private void AddQuadraticProduct(int vidRow, Rational coefficient, Term term1, Term term2, ref Rational boundsAdjustment)
		{
			Rational coefficient2 = 1;
			Rational coefficient3 = 1;
			int vidVarRow;
			int num = LinearTermVid(term1, ref coefficient2, out vidVarRow);
			int vidVarRow2;
			int num2 = LinearTermVid(term2, ref coefficient3, out vidVarRow2);
			if (vidVarRow < 0 && vidVarRow2 < 0)
			{
				Rational coefficient4 = _linearSolver.GetCoefficient(vidRow, num, num2);
				coefficient4 += coefficient * coefficient2 * coefficient3;
				_linearSolver.SetCoefficient(vidRow, coefficient4, num, num2);
			}
			else if (vidVarRow < 0 && vidVarRow2 >= 0)
			{
				AddQuadraticProductInner(vidRow, num, num2, vidVarRow2, coefficient * coefficient2);
			}
			else if (vidVarRow >= 0 && vidVarRow2 < 0)
			{
				AddQuadraticProductInner(vidRow, num2, num, vidVarRow, coefficient * coefficient3);
			}
			else
			{
				AddQuadraticProductInner2(vidRow, vidVarRow, vidVarRow2, num, num2, coefficient, ref boundsAdjustment);
			}
		}

		private void AddQuadraticProductInner2(int vidRow, int vidVarRow1, int vidVarRow2, int vidVar1, int vidVar2, Rational coefficient, ref Rational boundsAdjustment)
		{
			_linearSolver.GetBounds(vidVarRow1, out var numLo, out var _);
			_linearSolver.GetBounds(vidVarRow2, out var numLo2, out var _);
			LinearEntry[] array = _linearSolver.GetRowEntries(vidVarRow1).ToArray();
			LinearEntry[] array2 = _linearSolver.GetRowEntries(vidVarRow2).ToArray();
			LinearEntry[] array3 = array;
			for (int i = 0; i < array3.Length; i++)
			{
				LinearEntry linearEntry = array3[i];
				if (linearEntry.Index == vidVar1)
				{
					continue;
				}
				LinearEntry[] array4 = array2;
				for (int j = 0; j < array4.Length; j++)
				{
					LinearEntry linearEntry2 = array4[j];
					if (linearEntry2.Index != vidVar2)
					{
						Rational coefficient2 = _linearSolver.GetCoefficient(vidRow, linearEntry.Index, linearEntry2.Index);
						coefficient2 += coefficient * linearEntry.Value * linearEntry2.Value;
						_linearSolver.SetCoefficient(vidRow, coefficient2, linearEntry.Index, linearEntry2.Index);
					}
				}
			}
			LinearEntry[] array5 = array;
			for (int k = 0; k < array5.Length; k++)
			{
				LinearEntry linearEntry3 = array5[k];
				if (linearEntry3.Index != vidVar1)
				{
					Rational coefficient2 = _linearSolver.GetCoefficient(vidRow, linearEntry3.Index);
					coefficient2 += coefficient * -numLo2 * linearEntry3.Value;
					_linearSolver.SetCoefficient(vidRow, linearEntry3.Index, coefficient2);
				}
			}
			LinearEntry[] array6 = array2;
			for (int l = 0; l < array6.Length; l++)
			{
				LinearEntry linearEntry4 = array6[l];
				if (linearEntry4.Index != vidVar2)
				{
					Rational coefficient2 = _linearSolver.GetCoefficient(vidRow, linearEntry4.Index);
					coefficient2 += coefficient * -numLo * linearEntry4.Value;
					_linearSolver.SetCoefficient(vidRow, linearEntry4.Index, coefficient2);
				}
			}
			boundsAdjustment -= numLo * numLo2;
		}

		private void AddQuadraticProductInner(int vidRow, int vidVar1, int vidVar2, int vidVarRow2, Rational innerCoeff)
		{
			LinearEntry[] array = _linearSolver.GetRowEntries(vidVarRow2).ToArray();
			LinearEntry[] array2 = array;
			Rational coefficient;
			for (int i = 0; i < array2.Length; i++)
			{
				LinearEntry linearEntry = array2[i];
				if (linearEntry.Index != vidVar2)
				{
					coefficient = _linearSolver.GetCoefficient(vidRow, vidVar1, linearEntry.Index);
					coefficient += innerCoeff * linearEntry.Value;
					_linearSolver.SetCoefficient(vidRow, coefficient, vidVar1, linearEntry.Index);
				}
			}
			_linearSolver.GetBounds(vidVarRow2, out var numLo, out var _);
			coefficient = _linearSolver.GetCoefficient(vidRow, vidVar1);
			coefficient += innerCoeff * -numLo;
			_linearSolver.SetCoefficient(vidRow, vidVar1, coefficient);
		}

		private int MakeDecisionVid(Decision decision, Domain domain)
		{
			int vid;
			if (!_useNamesAsKeys)
			{
				_linearSolver.AddVariable(null, out vid);
			}
			else
			{
				_linearSolver.AddVariable(decision.Name, out vid);
			}
			_linearSolver.SetBounds(vid, domain.MinValue, domain.MaxValue);
			if (domain.IntRestricted)
			{
				_linearSolver.SetIntegrality(vid, fInteger: true);
			}
			return vid;
		}

		private int MakeDecisionVid(Decision decision, Domain domain, object[] indexes)
		{
			int vid;
			if (!_useNamesAsKeys)
			{
				_linearSolver.AddVariable(null, out vid);
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(decision.Name);
				stringBuilder.Append(Statics.JoinArrayToString(indexes));
				_linearSolver.AddVariable(stringBuilder.ToString(), out vid);
			}
			_linearSolver.SetBounds(vid, domain.MinValue, domain.MaxValue);
			if (domain.IntRestricted)
			{
				_linearSolver.SetIntegrality(vid, fInteger: true);
			}
			return vid;
		}

		private int MakeDecisionVid(RecourseDecision decision, Domain domain)
		{
			int vid;
			if (!_useNamesAsKeys)
			{
				_linearSolver.AddVariable(null, out vid);
			}
			else
			{
				_linearSolver.AddVariable(decision.Name, out vid);
			}
			_linearSolver.SetBounds(vid, domain.MinValue, domain.MaxValue);
			if (domain.IntRestricted)
			{
				_linearSolver.SetIntegrality(vid, fInteger: true);
			}
			return vid;
		}

		private int MakeDecisionVid(RecourseDecision decision, Domain domain, object[] indexes)
		{
			int vid;
			if (!_useNamesAsKeys)
			{
				_linearSolver.AddVariable(null, out vid);
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(decision.Name);
				stringBuilder.Append(Statics.JoinArrayToString(indexes));
				_linearSolver.AddVariable(stringBuilder.ToString(), out vid);
			}
			_linearSolver.SetBounds(vid, domain.MinValue, domain.MaxValue);
			if (domain.IntRestricted)
			{
				_linearSolver.SetIntegrality(vid, fInteger: true);
			}
			return vid;
		}

		private int LinearTermVid(Term term, ref Rational coefficient, out int vidVarRow)
		{
			vidVarRow = -1;
			while (true)
			{
				if (term is Decision decisionTerm)
				{
					return GetDecisionVid(decisionTerm);
				}
				if (!(term is OperatorTerm operatorTerm))
				{
					break;
				}
				Operator operation = operatorTerm.Operation;
				Term[] inputs = operatorTerm._inputs;
				if (operation == Operator.Times && inputs.Length == 2)
				{
					if (inputs[0].HasStructure(TermStructure.Constant))
					{
						Rational rational = EvaluateConstant(inputs[0]);
						coefficient *= rational;
						term = inputs[1];
						continue;
					}
					if (!inputs[1].HasStructure(TermStructure.Constant))
					{
						break;
					}
					Rational rational2 = EvaluateConstant(inputs[1]);
					coefficient *= rational2;
					term = inputs[0];
				}
				else
				{
					if (operation != Operator.Minus || inputs.Length != 1)
					{
						break;
					}
					coefficient = -coefficient;
					term = inputs[0];
				}
			}
			int num = AddDummyVariable();
			int num2 = AddRow();
			_linearSolver.SetCoefficient(num2, num, -1);
			Rational boundsAdjustment = 0;
			AddTermToRow(term, num2, ref boundsAdjustment, 1);
			_linearSolver.SetBounds(num2, boundsAdjustment, boundsAdjustment);
			_ = _useRowBoundsAdjustments;
			vidVarRow = num2;
			return num;
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
					_linearSolver.SetValue(value, value2);
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
					_linearSolver.SetValue(num, value);
				}
				_decisionVids[id] = num;
			}
			return num;
		}

		protected void ModifyCoefficient(int vid, Rational multiplier, int vidVar)
		{
			Rational coefficient = _linearSolver.GetCoefficient(vid, vidVar);
			coefficient += multiplier;
			_linearSolver.SetCoefficient(vid, vidVar, coefficient);
		}

		internal override ISolverProperties GetSolverPropertiesInstance()
		{
			return Task.GetSolverPropertiesInstance(_linearSolver);
		}

		internal override int FindDecisionVid(Decision decision, object[] indexes)
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

		internal override IEnumerable<object[]> GetIndexes(Decision decision)
		{
			if (_decisionIndexedVids != null && _decisionIndexedVids[decision._id] != null)
			{
				ValueTable<int> valueTable = _decisionIndexedVids[decision._id];
				return valueTable.Keys;
			}
			if (_decisionVids != null && _decisionVids[decision._id] >= 0)
			{
				return new object[1][] { SolverContext._emptyArray };
			}
			throw new MsfException(Resources.InternalError + string.Format(CultureInfo.InvariantCulture, Resources.CannotFindIndexesForDecision0, new object[1] { decision.Name }));
		}
	}
}
