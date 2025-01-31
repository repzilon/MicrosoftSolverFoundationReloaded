using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	internal class NonlinearTask : Task
	{
		private struct VidPair
		{
			public int TermVid;

			public int NonlinearVid;
		}

		private class DelegateBuilder
		{
			/// <summary>
			/// This is the maximum depth of an expression; anything larger will be broken up using temporary variables.
			/// Setting this too low will impact performance, while setting it too high may cause stack overflow.
			/// </summary>
			private const int MaxExpressionDepth = 10;

			private SimplifiedTermModelWrapper _termModel;

			private TermModel _innerTermModel;

			private INonlinearSolver _solver;

			/// <summary>
			/// This contains the vids of constraints and goals (not all rows) in the TermModel.
			/// </summary>
			private List<VidPair> _rowVids;

			private List<VidPair> _variableVids;

			private List<VidPair> _gradientVids;

			internal int[] _nonlinearVid;

			private int _maxRowVid;

			private bool _gradientEvaluated;

			private double[] _resultStorage;

			private Action<double[], ValuesByIndex, bool, bool> _recalculateDelegate;

			private readonly ParameterExpression _recalculateNeededExpr = Expression.Parameter(typeof(bool), "recalculateNeeded");

			public DelegateBuilder(SimplifiedTermModelWrapper termModel, INonlinearSolver solver, List<VidPair> rowVids, List<VidPair> variableVids, List<VidPair> gradientVids)
			{
				_termModel = termModel;
				_solver = solver;
				_rowVids = rowVids;
				_variableVids = variableVids;
				_gradientVids = gradientVids;
			}

			public void InitializeInterpreted()
			{
				int maxVid = _termModel.MaxVid;
				BitArray bitArray = new BitArray(maxVid + 1);
				int[] array = new int[maxVid + 1];
				_nonlinearVid = new int[maxVid + 1];
				_maxRowVid = 0;
				foreach (VidPair rowVid in _rowVids)
				{
					_nonlinearVid[rowVid.TermVid] = rowVid.NonlinearVid;
					bitArray[rowVid.TermVid] = true;
					if (_maxRowVid < rowVid.TermVid)
					{
						_maxRowVid = rowVid.TermVid;
					}
				}
				foreach (VidPair variableVid in _variableVids)
				{
					_nonlinearVid[variableVid.TermVid] = variableVid.NonlinearVid;
				}
				int length = 0;
				for (int i = 0; i <= maxVid; i++)
				{
					if (!_termModel.IsRow(i))
					{
						array[i] = length++;
					}
					else
					{
						array[i] = -1;
					}
				}
				BitArray[] array2 = new BitArray[maxVid + 1];
				for (int j = 0; j <= maxVid; j++)
				{
					array2[j] = new BitArray(length);
				}
				for (int k = 0; k <= maxVid; k++)
				{
					if (_termModel.IsConstant(k))
					{
						continue;
					}
					if (!_termModel.IsRow(k))
					{
						array2[k][array[k]] = true;
					}
					else
					{
						if (!_termModel.IsOperation(k))
						{
							continue;
						}
						int operandCount = _termModel.GetOperandCount(k);
						for (int l = 0; l < operandCount; l++)
						{
							int operand = _termModel.GetOperand(k, l);
							array2[k] = array2[k].Or(array2[operand]);
						}
						if (!bitArray[k])
						{
							continue;
						}
						for (int m = 0; m <= maxVid; m++)
						{
							if (array[m] >= 0 && array2[k][array[m]])
							{
								_solver.SetActiveVariable(_nonlinearVid[k], _nonlinearVid[m], active: true);
							}
						}
					}
				}
				_resultStorage = new double[maxVid + 1];
				_recalculateDelegate = DoRecalculate;
				_innerTermModel = (TermModel)_termModel._model;
			}

			private void DoRecalculate(double[] values, ValuesByIndex x, bool recalcValues, bool recalcGradients)
			{
				int num = ((!recalcValues) ? (_maxRowVid + 1) : 0);
				int num2 = (recalcGradients ? (values.Length - 1) : _maxRowVid);
				for (int i = num; i <= num2; i++)
				{
					if (!_innerTermModel._mpvidvi[i].IsRow)
					{
						values[i] = x[_nonlinearVid[i]];
						continue;
					}
					if (_innerTermModel._vars[i].isConstant)
					{
						values[i] = (double)_innerTermModel._mpvidnum[i];
						continue;
					}
					TermModelOperation op = _innerTermModel._vars[i].op;
					switch (_innerTermModel._vars[i].operandCount)
					{
					case 1:
					{
						double num7 = values[_innerTermModel._operands[_innerTermModel._vars[i].operandStart]];
						double num8;
						switch (op)
						{
						case TermModelOperation.Identity:
							num8 = num7;
							break;
						case TermModelOperation.Minus:
							num8 = 0.0 - num7;
							break;
						default:
							num8 = OperationHelpers.EvaluateUnaryOp(op, num7);
							break;
						}
						values[i] = num8;
						break;
					}
					case 2:
					{
						double num9 = values[_innerTermModel._operands[_innerTermModel._vars[i].operandStart]];
						double num10 = values[_innerTermModel._operands[_innerTermModel._vars[i].operandStart + 1]];
						double num11;
						switch (op)
						{
						case TermModelOperation.Plus:
							num11 = num9 + num10;
							break;
						case TermModelOperation.Times:
							num11 = num9 * num10;
							break;
						case TermModelOperation.Quotient:
							num11 = num9 / num10;
							break;
						default:
							num11 = OperationHelpers.EvaluateBinaryOp(op, num9, num10);
							break;
						}
						values[i] = num11;
						break;
					}
					case 3:
					{
						double num3 = values[_innerTermModel._operands[_innerTermModel._vars[i].operandStart]];
						double num4 = values[_innerTermModel._operands[_innerTermModel._vars[i].operandStart + 1]];
						double num5 = values[_innerTermModel._operands[_innerTermModel._vars[i].operandStart + 2]];
						TermModelOperation termModelOperation = op;
						if (termModelOperation == TermModelOperation.If)
						{
							double num6 = ((num3 != 0.0) ? num4 : num5);
							values[i] = num6;
							break;
						}
						throw new NotSupportedException();
					}
					default:
						throw new NotSupportedException();
					}
				}
			}

			public Func<INonlinearModel, ValuesByIndex, bool, double> BuildFunctionEvaluatorInterpreted(VidPair vids)
			{
				return delegate(INonlinearModel nonlinearModel, ValuesByIndex x, bool needRecalc)
				{
					if (needRecalc)
					{
						_recalculateDelegate(_resultStorage, x, arg3: true, arg4: false);
						_gradientEvaluated = false;
					}
					return _resultStorage[vids.TermVid];
				};
			}

			public Action<INonlinearModel, ValuesByIndex, bool, ValuesByIndex> BuildGradientEvaluatorInterpreted(VidPair vids, IEnumerable<GradientEntry> gradientEntries)
			{
				GradientEntry[] entries = gradientEntries.ToArray();
				return delegate(INonlinearModel nonlinearModel, ValuesByIndex x, bool needRecalc, ValuesByIndex gradient)
				{
					if (needRecalc)
					{
						_recalculateDelegate(_resultStorage, x, arg3: true, arg4: true);
						_gradientEvaluated = true;
					}
					else if (!_gradientEvaluated)
					{
						_recalculateDelegate(_resultStorage, x, arg3: false, arg4: true);
						_gradientEvaluated = true;
					}
					for (int i = 0; i < entries.Length; i++)
					{
						gradient[_nonlinearVid[entries[i].varVid]] = _resultStorage[entries[i].derivRowVid];
					}
				};
			}

			public SolutionMapping GetSolutionMapping(Model model, TermTask.Builder builder, INonlinearSolution solution)
			{
				int[] decisionVids = builder._decisionVids;
				ValueTable<int>[] decisionIndexedVids = builder._decisionIndexedVids;
				int[] goalVids = builder._goalVids;
				Constraint[] rowConstraint = builder._rowConstraint;
				int[] rowSubconstraint = builder._rowSubconstraint;
				object[][] rowIndexes = builder._rowIndexes;
				for (int i = 0; i < decisionVids.Length; i++)
				{
					if (decisionVids[i] >= 0)
					{
						decisionVids[i] = _nonlinearVid[decisionVids[i]];
					}
					else
					{
						decisionVids[i] = -1;
					}
					if (decisionIndexedVids != null && decisionIndexedVids[i] != null)
					{
						object[][] array = decisionIndexedVids[i].Keys.ToArray();
						object[][] array2 = array;
						foreach (object[] indexes in array2)
						{
							decisionIndexedVids[i].TryGetValue(out var value, indexes);
							decisionIndexedVids[i].Set(_nonlinearVid[value], indexes);
						}
					}
				}
				for (int k = 0; k < goalVids.Length; k++)
				{
					goalVids[k] = _nonlinearVid[goalVids[k]];
				}
				return new NonlinearSolutionMapping(model, _solver, solution, decisionVids, decisionIndexedVids, goalVids, rowConstraint, rowSubconstraint, rowIndexes);
			}
		}

		private INonlinearSolver _solver;

		private Model _model;

		private SimplifiedTermModelWrapper _termModel;

		private TermTask.Builder _builder;

		private DelegateBuilder _delegateBuilder;

		private TermModelDifferentiator _differentiator;

		private Func<INonlinearModel, ValuesByIndex, bool, double>[] _functionEvaluators;

		private Action<INonlinearModel, ValuesByIndex, bool, ValuesByIndex>[] _gradientEvaluators;

		private bool _doDifferentiate;

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

		public NonlinearTask(SolverContext context, Model model, INonlinearSolver solver, ISolverParameters solverParams, Directive directive, bool doDifferentiate)
			: base(context, solverParams, directive)
		{
			_solver = solver;
			_model = model;
			_doDifferentiate = doDifferentiate;
			_termModel = new SimplifiedTermModelWrapper(new TermModel(EqualityComparer<object>.Default));
			_builder = new TermTask.Builder(model, _termModel);
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
			taskSummary.Solution = new Solution(_context, ResultQuality(solution));
			taskSummary.Directive = _directive;
			taskSummary.Solver = _solver;
			taskSummary.Exception = exception;
			return taskSummary;
		}

		internal static SolverQuality ResultQuality(INonlinearSolution solution)
		{
			if (solution == null)
			{
				return SolverQuality.Unknown;
			}
			switch (solution.Result)
			{
			case NonlinearResult.Feasible:
				return SolverQuality.Feasible;
			case NonlinearResult.Infeasible:
				return SolverQuality.Infeasible;
			case NonlinearResult.LocalInfeasible:
				return SolverQuality.LocalInfeasible;
			case NonlinearResult.InfeasibleOrUnbounded:
				return SolverQuality.InfeasibleOrUnbounded;
			case NonlinearResult.Interrupted:
				return SolverQuality.Unknown;
			case NonlinearResult.Invalid:
				return SolverQuality.Unknown;
			case NonlinearResult.LocalOptimal:
				return SolverQuality.LocalOptimal;
			case NonlinearResult.Optimal:
				return SolverQuality.Optimal;
			case NonlinearResult.Unbounded:
				return SolverQuality.Unbounded;
			default:
				return SolverQuality.Unknown;
			}
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
			List<VidPair> rowVids = BuildRows();
			List<VidPair> variableVids = BuildVariables();
			List<VidPair> list = new List<VidPair>();
			list = ((!_doDifferentiate) ? new List<VidPair>() : BuildGradients(rowVids));
			int maxVid = ((_solver is NonlinearModel) ? (_solver.RowCount - 1) : _solver.RowIndices.Max());
			_delegateBuilder = new DelegateBuilder(_termModel, _solver, rowVids, variableVids, list);
			_delegateBuilder.InitializeInterpreted();
			if (_solver.RowCount == 0)
			{
				_termModel = null;
				return;
			}
			BuildFunctionEvaluators(rowVids, maxVid);
			if (_doDifferentiate)
			{
				BuildGradientEvaluators(rowVids, maxVid);
			}
			_termModel = null;
		}

		private List<VidPair> BuildRows()
		{
			List<VidPair> list = new List<VidPair>();
			foreach (int rowIndex in _termModel.RowIndices)
			{
				if (_termModel.IsConstant(rowIndex))
				{
					continue;
				}
				_termModel.GetBounds(rowIndex, out var numLo, out var numHi);
				bool flag = _termModel.IsGoal(rowIndex);
				if (flag || !numLo.IsNegativeInfinity || !numHi.IsPositiveInfinity)
				{
					_solver.AddRow(null, out var vid);
					list.Add(new VidPair
					{
						NonlinearVid = vid,
						TermVid = rowIndex
					});
					if (flag)
					{
						IGoal goalFromIndex = _termModel.GetGoalFromIndex(rowIndex);
						_solver.AddGoal(vid, goalFromIndex.Priority, goalFromIndex.Minimize);
					}
					if (!numLo.IsNegativeInfinity || !numHi.IsPositiveInfinity)
					{
						_solver.SetBounds(vid, numLo, numHi);
					}
				}
			}
			return list;
		}

		private List<VidPair> BuildVariables()
		{
			List<VidPair> list = new List<VidPair>();
			foreach (int variableIndex in _termModel.VariableIndices)
			{
				_solver.AddVariable(null, out var vid);
				list.Add(new VidPair
				{
					NonlinearVid = vid,
					TermVid = variableIndex
				});
				_termModel.GetBounds(variableIndex, out var numLo, out var numHi);
				if (!numLo.IsNegativeInfinity || !numHi.IsPositiveInfinity)
				{
					_solver.SetBounds(vid, numLo, numHi);
				}
				Rational value = _termModel.GetValue(variableIndex);
				if (!value.IsIndeterminate)
				{
					_solver.SetValue(vid, value);
				}
				bool integrality = _termModel.GetIntegrality(variableIndex);
				if (integrality)
				{
					_solver.SetIntegrality(vid, integrality);
				}
			}
			return list;
		}

		private List<VidPair> BuildGradients(List<VidPair> rowVids)
		{
			List<VidPair> list = new List<VidPair>();
			PickDifferentiator();
			_differentiator.Differentiate(_termModel, rowVids.Select((VidPair v) => v.TermVid));
			foreach (int differentiatedRow in _differentiator.GetDifferentiatedRows())
			{
				foreach (GradientEntry rowGradientEntry in _differentiator.GetRowGradientEntries(differentiatedRow))
				{
					list.Add(new VidPair
					{
						TermVid = rowGradientEntry.derivRowVid,
						NonlinearVid = -1
					});
				}
			}
			return list;
		}

		private void BuildFunctionEvaluators(List<VidPair> rowVids, int maxVid)
		{
			_functionEvaluators = new Func<INonlinearModel, ValuesByIndex, bool, double>[maxVid + 1];
			foreach (VidPair rowVid in rowVids)
			{
				_functionEvaluators[rowVid.NonlinearVid] = _delegateBuilder.BuildFunctionEvaluatorInterpreted(rowVid);
			}
			_solver.FunctionEvaluator = (INonlinearModel m, int vid, ValuesByIndex x, bool newValues) => _functionEvaluators[vid](m, x, newValues);
		}

		private void BuildGradientEvaluators(List<VidPair> rowVids, int maxVid)
		{
			_gradientEvaluators = new Action<INonlinearModel, ValuesByIndex, bool, ValuesByIndex>[maxVid + 1];
			foreach (VidPair rowVid in rowVids)
			{
				_gradientEvaluators[rowVid.NonlinearVid] = _delegateBuilder.BuildGradientEvaluatorInterpreted(rowVid, _differentiator.GetRowGradientEntries(rowVid.TermVid));
			}
			_solver.GradientEvaluator = delegate(INonlinearModel m, int vid, ValuesByIndex x, bool newValues, ValuesByIndex value)
			{
				_gradientEvaluators[vid](m, x, newValues, value);
			};
		}

		/// <summary>
		/// Picks the right Differentiator depends on the model.
		/// Currently picks ReverseDifferentiator if there is just one row, 
		/// otherwise picks ForwardDifferentiator
		/// </summary>
		private void PickDifferentiator()
		{
			if (_solver.RowCount == 1)
			{
				_differentiator = new TermModelReverseDifferentiator();
			}
			else
			{
				_differentiator = new TermModelForwardDifferentiator();
			}
		}

		internal override ISolverProperties GetSolverPropertiesInstance()
		{
			return Task.GetSolverPropertiesInstance(_solver);
		}

		internal override int FindDecisionVid(Decision decision, object[] indexes)
		{
			int num = _builder.FindDecisionVid(decision, indexes);
			if (num < 0)
			{
				return -1;
			}
			return _delegateBuilder._nonlinearVid[num];
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
			return _delegateBuilder.GetSolutionMapping(_model, _builder, nonlinearSolution);
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
	}
}
