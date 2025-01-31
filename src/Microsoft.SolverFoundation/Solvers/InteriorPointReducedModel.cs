using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>Base class for interior point solver models.
	/// </summary>
	internal abstract class InteriorPointReducedModel : IInteriorPointStatistics
	{
		private struct InferredBounds
		{
			internal Rational lo;

			internal Rational hi;

			public override string ToString()
			{
				return "[" + lo.ToString() + ", " + hi.ToString() + "]";
			}
		}

		/// <summary> -1 == maximize, 0 == unknown, +1 = minimize (the standard)
		/// </summary>
		protected int _goalDirection;

		/// <summary>VID of the (single) goal.
		/// </summary>
		private int _goalVid;

		/// <summary>Solution.
		/// </summary>
		protected StandardSolution _solution;

		/// <summary>The user model.
		/// </summary>
		protected readonly LinearModel model;

		/// <summary>The user model (SOCP).
		/// </summary>
		protected readonly SecondOrderConicModel _socpModel;

		private readonly LogSource _logger;

		/// <summary> Keep track of rows removed due to unbounded slacks
		/// </summary>
		protected Dictionary<int, bool> removedRowsByVid;

		private InteriorPointSolveState _solveState;

		/// <summary> List of &lt;rowVid, varVid&gt; pairs found infeasible (varVid == -1 for row itself).
		/// </summary>
		private List<KeyValuePair<int, int>> _infeasibleRowVarPairs;

		private int _maxPresolveIterations = 5;

		private bool _allowRowBoundTightening;

		/// <summary>Remove rows that are found to be redundant due to upper and lower bounds on vars.
		/// </summary>
		private bool _removeRedundantMinMax;

		/// <summary> Track tighter inferred bounds assigned to user variables
		/// </summary>
		private Dictionary<int, InferredBounds> _inferredBounds;

		/// <summary>The LogSource.
		/// </summary>
		protected LogSource Logger => _logger;

		/// <summary> Phases of the IPM solution process.
		/// </summary>
		public InteriorPointSolveState SolveState
		{
			get
			{
				return _solveState;
			}
			set
			{
				_solveState = value;
			}
		}

		protected abstract double SolveTolerance { get; }

		/// <summary>VID of the (single) goal.
		/// </summary>
		public int GoalVid => _goalVid;

		/// <summary>Solution.
		/// </summary>
		public StandardSolution Solution => _solution;

		/// <summary> The number of rows in the solver model.
		/// </summary>
		public abstract int RowCount { get; }

		/// <summary> Total number of variables, user and slack.
		/// </summary>
		public abstract int VarCount { get; }

		/// <summary>Iteration count.
		/// </summary>
		public abstract int IterationCount { get; }

		/// <summary> The primal version of the objective.
		/// </summary>
		public abstract double Primal { get; }

		/// <summary> The dual version of the objective.
		/// </summary>
		public abstract double Dual { get; }

		/// <summary> The gap between primal and dual objective values.
		/// </summary>
		public abstract double Gap { get; }

		/// <summary> The kind of IPM algorithm used.
		/// </summary>
		public abstract InteriorPointAlgorithmKind Algorithm { get; }

		/// <summary> The form of KKT matrices used.
		/// </summary>
		public abstract InteriorPointKktForm KktForm { get; }

		/// <summary> List of infeasible (row, var) pairs.
		/// </summary>
		public List<KeyValuePair<int, int>> InfeasibleRowVarPairs => _infeasibleRowVarPairs;

		/// <summary>Remove rows that are found to be redundant due to upper and lower bounds on vars.
		/// </summary>
		public bool RemoveRedundantMinMax
		{
			get
			{
				return _removeRedundantMinMax;
			}
			set
			{
				_removeRedundantMinMax = value;
			}
		}

		/// <summary> Return the dual value for a row constraint.
		/// </summary>
		/// <param name="vidRow">Row vid.</param>
		/// <returns></returns>
		public abstract Rational GetDualValue(int vidRow);

		private bool AddInferredBounds(int colVid, Rational lo, Rational hi)
		{
			InferredBounds value = default(InferredBounds);
			value.lo = lo;
			value.hi = hi;
			_inferredBounds[colVid] = value;
			if (!(lo < hi))
			{
				if (lo == hi)
				{
					return lo.IsFinite;
				}
				return false;
			}
			return true;
		}

		/// <summary>Solves the model.
		/// </summary>
		/// <param name="prm">IPM solver parameters.</param>
		/// <returns>A LinearResult.</returns>
		public abstract LinearResult Solve(InteriorPointSolverParams prm);

		/// <summary> Confirm the direction of the goal.
		/// </summary>
		public bool IsGoalMinimize(int igoal)
		{
			if (igoal == 0)
			{
				return 1 == _goalDirection;
			}
			return false;
		}

		/// <summary>Map solution to user model.
		/// </summary>
		public abstract void MapVarValues(Rational[] vidValues);

		/// <summary> Check to see if a vid represents a goal.
		/// </summary>
		public bool IsGoalVid(int rowVid)
		{
			return rowVid == GoalVid;
		}

		protected void DetermineGoal()
		{
			if (model.GoalCount == 0)
			{
				throw new ModelException(Resources.InteriorPointCurrentlyRequiresAGoal);
			}
			if (1 < model.GoalCount)
			{
				Logger.LogEvent(15, Resources.IPMGoalCountHasBeenReducedTo1);
			}
			using (IEnumerator<ILinearGoal> enumerator = model.Goals.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					ILinearGoal current = enumerator.Current;
					_goalVid = current.Index;
					_goalDirection = (current.Minimize ? 1 : (-1));
				}
			}
		}

		public InteriorPointReducedModel(LinearModel model, LogSource logger)
		{
			this.model = model;
			_socpModel = model as SecondOrderConicModel;
			_logger = logger;
			_inferredBounds = new Dictionary<int, InferredBounds>();
			_infeasibleRowVarPairs = new List<KeyValuePair<int, int>>();
			removedRowsByVid = new Dictionary<int, bool>();
		}

		/// <summary> Get the user model bounds, imposing any inferred tightening.
		/// </summary>
		protected void GetUserBounds(int vid, out Rational lo, out Rational hi)
		{
			if (!_inferredBounds.TryGetValue(vid, out var value))
			{
				model.GetBounds(vid, out lo, out hi);
				return;
			}
			lo = value.lo;
			hi = value.hi;
		}

		private bool IsSocpRow(int vidRow)
		{
			if (_socpModel != null)
			{
				return _socpModel.IsConicRow(vidRow);
			}
			return false;
		}

		private bool IsLinearModel()
		{
			if (!model.IsQuadraticModel)
			{
				if (_socpModel != null)
				{
					return !_socpModel.IsSocpModel;
				}
				return true;
			}
			return false;
		}

		private bool IsSocpModel()
		{
			if (_socpModel != null)
			{
				return _socpModel.IsSocpModel;
			}
			return false;
		}

		protected void ClearUserBounds()
		{
			_inferredBounds.Clear();
		}

		[SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "Presolve")]
		protected void Presolve(int presolveLevel)
		{
			Func<int, PresolveResult>[] array = new Func<int, PresolveResult>[1] { TightenBounds };
			PresolveResult presolveResult = RemoveEmptyRows();
			if (presolveResult.Terminate)
			{
				Solution.status = presolveResult.Status;
				return;
			}
			Func<int, PresolveResult>[] array2 = array;
			foreach (Func<int, PresolveResult> func in array2)
			{
				if (!InnerPresolve(presolveLevel))
				{
					break;
				}
				presolveResult = func(presolveLevel);
				if (presolveResult.Terminate)
				{
					Solution.status = presolveResult.Status;
					return;
				}
				if (presolveResult.ChangeCount > 0)
				{
					Logger.LogEvent(15, "{0}\t{1}", func.Method.Name, presolveResult.ChangeCount);
				}
			}
			Logger.LogEvent(15, "Presolve complete.");
		}

		private bool InnerPresolve(int presolveLevel)
		{
			Func<PresolveResult>[] array = new Func<PresolveResult>[3] { RemoveRedundant, SingletonRows, CheapDual };
			bool flag = true;
			for (int i = 0; i < _maxPresolveIterations; i++)
			{
				if (!flag)
				{
					break;
				}
				flag = false;
				Func<PresolveResult>[] array2 = array;
				foreach (Func<PresolveResult> func in array2)
				{
					PresolveResult presolveResult = func();
					if (presolveResult.Terminate)
					{
						Solution.status = presolveResult.Status;
						return false;
					}
					flag |= presolveResult.ChangeCount > 0;
					if (presolveResult.ChangeCount > 0)
					{
						Logger.LogEvent(15, "{0}\t{1}", func.Method.Name, presolveResult.ChangeCount);
					}
				}
			}
			return true;
		}

		/// <summary> Some models have redundant variables.
		///           It is best to remove them and any rows they play in.
		/// </summary>
		protected PresolveResult RemoveRedundant()
		{
			bool flag = false;
			PresolveResult result = default(PresolveResult);
			for (int i = 0; i < model.ColCount; i++)
			{
				if (model.IsRow(i))
				{
					continue;
				}
				int num = model.GetVariableEntryCount(i);
				GetUserBounds(i, out var lo, out var hi);
				if ((num != 0 && (!lo.IsNegativeInfinity || !hi.IsPositiveInfinity)) || num > 1 || !IsLinearModel() || 0 >= num)
				{
					continue;
				}
				foreach (LinearEntry colValue in model.GetColValues(i))
				{
					num--;
					int index = colValue.Index;
					if (!removedRowsByVid.ContainsKey(index))
					{
						removedRowsByVid[index] = true;
						result.ChangeCount++;
					}
				}
				flag = flag || 0 < num;
			}
			if (flag)
			{
				result.Terminate = true;
				result.Status = LinearResult.UnboundedPrimal;
			}
			return result;
		}

		/// <summary> Singleton rows can be removed by adjusting variable bounds.
		/// </summary>
		/// <remarks>Here is an example of an LP where this rule applies.  We can introduce
		/// a lower bound x1 &gt;= 2, and remove the constraint:
		/// min x1
		/// 2 x1 &gt;= 4
		/// x1 &lt;= 10
		/// </remarks>    
		protected PresolveResult SingletonRows()
		{
			PresolveResult result = default(PresolveResult);
			if (IsSocpModel())
			{
				return result;
			}
			for (int i = 0; i < model.ColCount; i++)
			{
				if (!model.IsRow(i) || IsSocpRow(i) || i == _goalVid || removedRowsByVid.ContainsKey(i) || !IsSingletonRow(i))
				{
					continue;
				}
				result.ChangeCount++;
				LinearEntry linearEntry = model.GetRowEntries(i).First();
				int index = linearEntry.Index;
				Rational value = linearEntry.Value;
				GetUserBounds(i, out var lo, out var hi);
				GetUserBounds(index, out var lo2, out var hi2);
				if (value < 0)
				{
					Rational rational = lo / value;
					lo = hi / value;
					hi = rational;
				}
				else
				{
					lo /= value;
					hi /= value;
				}
				if (lo2 < lo || hi2 > hi)
				{
					Rational lo3 = ((lo2 < lo) ? lo : lo2);
					Rational hi3 = ((hi2 > hi) ? hi : hi2);
					if (!AddInferredBounds(index, lo3, hi3))
					{
						result.Terminate = true;
						result.Status = LinearResult.InfeasiblePrimal;
						break;
					}
				}
				removedRowsByVid[i] = true;
			}
			return result;
		}

		/// <summary> Empty rows can be removed.
		/// </summary>
		/// <remarks>Here is an example of an LP where this rule applies. It is infeasible because 0 is not greater than 2.
		/// min x1
		/// 4 &gt;= 0 * x1 &gt;= 2
		/// x1 &lt;= 10
		/// </remarks>    
		protected PresolveResult RemoveEmptyRows()
		{
			PresolveResult result = default(PresolveResult);
			foreach (int rowIndex in model.RowIndices)
			{
				if (IsEmptyRow(rowIndex) && !IsSocpRow(rowIndex) && rowIndex != _goalVid && !removedRowsByVid.ContainsKey(rowIndex))
				{
					result.ChangeCount++;
					GetUserBounds(rowIndex, out var lo, out var hi);
					if (lo > Rational.Zero || hi < Rational.Zero || lo > hi)
					{
						result.Terminate = true;
						result.Status = LinearResult.InfeasiblePrimal;
					}
					removedRowsByVid[rowIndex] = true;
				}
			}
			return result;
		}

		protected bool IsEmptyRow(int rowVid)
		{
			return model.GetRowEntryCount(rowVid) == 1;
		}

		protected bool IsSingletonRow(int rowVid)
		{
			return model.GetRowEntryCount(rowVid) == 2;
		}

		/// <summary>Cheap dual tests - see section 2.1.4 of Meszaros "Advanced preprocessing techniques for LP and QP" (2003).
		/// </summary>
		/// <returns>True if any bounds were changed.</returns>
		/// <remarks>Here is an example of an LP where this rule applies.  x1 can be fixed
		/// at its upper bound 10:
		/// min -x1
		/// 2x1 + x2 &gt;= -1
		/// -Inf &lt;= x1 &lt;= 10
		/// </remarks>
		protected PresolveResult CheapDual()
		{
			PresolveResult result = default(PresolveResult);
			if (!IsLinearModel())
			{
				return result;
			}
			foreach (LinearEntry rowEntry in model.GetRowEntries(_goalVid))
			{
				int index = rowEntry.Index;
				GetUserBounds(index, out var lo, out var hi);
				if (!(lo != hi))
				{
					continue;
				}
				Rational rational = _goalDirection * rowEntry.Value;
				bool flag = true;
				int num = 0;
				Rational lo2;
				Rational hi2;
				foreach (LinearEntry colValue in model.GetColValues(index))
				{
					num++;
					int index2 = colValue.Index;
					Rational rational2 = ((rational <= 0) ? colValue.Value : (-colValue.Value));
					GetUserBounds(index2, out lo2, out hi2);
					flag &= (hi2.IsPositiveInfinity && rational2 > 0) || (lo2.IsNegativeInfinity && rational2 < 0);
				}
				GetUserBounds(_goalVid, out lo2, out hi2);
				if (!lo2.IsNegativeInfinity || !hi2.IsPositiveInfinity)
				{
					flag = false;
				}
				if (flag)
				{
					result.ChangeCount++;
					Rational rational3 = ((rational <= 0) ? hi : lo);
					AddInferredBounds(index, rational3, rational3);
				}
			}
			return result;
		}

		/// <summary> Find rows which reduce the bounds of variables
		/// </summary>
		protected PresolveResult TightenBounds(int presolveLevel)
		{
			PresolveResult result = default(PresolveResult);
			if (IsSocpModel())
			{
				return result;
			}
			int num = 0;
			int changeCount = 1;
			result.ChangeCount = -1;
			int[] vids = new int[10];
			Rational[] vLo = new Rational[10];
			Rational[] vHi = new Rational[10];
			Rational[] coeffs = new Rational[10];
			while (0 < changeCount && num < 8)
			{
				result.ChangeCount += changeCount;
				changeCount = 0;
				foreach (int rowIndex in model.RowIndices)
				{
					if (!IsSocpRow(rowIndex))
					{
						bool infeasiblePrimal = false;
						if (model.GetRowEntryCount(rowIndex) <= 11)
						{
							TightenBoundsPass(ref result, ref changeCount, vids, vLo, vHi, coeffs, rowIndex, ref infeasiblePrimal);
						}
						if (infeasiblePrimal)
						{
							result.Status = LinearResult.InfeasiblePrimal;
							result.Terminate = true;
							return result;
						}
					}
				}
				result.ChangeCount += changeCount;
				num++;
			}
			return result;
		}

		private void TightenBoundsPass(ref PresolveResult result, ref int changeCount, int[] vids, Rational[] vLo, Rational[] vHi, Rational[] coeffs, int rowVid, ref bool infeasiblePrimal)
		{
			GetUserBounds(rowVid, out var lo, out var hi);
			if (IsGoalVid(rowVid))
			{
				if (lo.IsNegativeInfinity && hi.IsPositiveInfinity)
				{
					return;
				}
				if (model.IsQuadraticModel)
				{
					throw new InvalidModelDataException(Resources.CannotSetBoundsOnAQuadraticGoal);
				}
			}
			int rowIndexFromVid = model.GetRowIndexFromVid(rowVid);
			double cutoff = 1000.0 * (100.0 + Math.Min(Math.Abs((double)lo), Math.Abs((double)hi)));
			int count = ComputeAdjustedBounds(vids, vLo, vHi, coeffs, rowIndexFromVid);
			InferVariableBounds(ref changeCount, vids, vLo, vHi, coeffs, lo, hi, rowIndexFromVid, cutoff, count, ref infeasiblePrimal);
			InferRowBounds(ref changeCount, vids, vLo, vHi, coeffs, rowVid, lo, hi, rowIndexFromVid, cutoff, count, ref infeasiblePrimal);
		}

		private int ComputeAdjustedBounds(int[] vids, Rational[] vLo, Rational[] vHi, Rational[] coeffs, int row)
		{
			int num = 0;
			foreach (LinearEntry rowValue in model.GetRowValues(row))
			{
				vids[num] = rowValue.Index;
				Rational value = rowValue.Value;
				coeffs[num] = value;
				GetUserBounds(vids[num], out vLo[num], out vHi[num]);
				if (0 < value)
				{
					if (!value.IsOne)
					{
						vLo[num] *= value;
						vHi[num] *= value;
					}
				}
				else
				{
					Rational rational = vLo[num] * value;
					ref Rational reference = ref vLo[num];
					reference = vHi[num] * value;
					vHi[num] = rational;
				}
				num++;
			}
			return num;
		}

		private void InferVariableBounds(ref int changeCount, int[] vids, Rational[] vLo, Rational[] vHi, Rational[] coeffs, Rational rLo, Rational rHi, int row, double cutoff, int count, ref bool infeasiblePrimal)
		{
			for (int i = 0; i < count; i++)
			{
				Rational rational = rLo;
				Rational rational2 = rHi;
				int num = count;
				while (0 <= --num)
				{
					if (i != num)
					{
						rational -= vHi[num];
						rational2 -= vLo[num];
					}
				}
				bool flag = false;
				if (vLo[i] < rational && (!vLo[i].IsNegativeInfinity || 0.0 - cutoff < rational))
				{
					flag = true;
					vLo[i] = rational;
				}
				if (rational2 < vHi[i] && (!vHi[i].IsPositiveInfinity || rational2 < cutoff))
				{
					flag = true;
					vHi[i] = rational2;
				}
				if (flag)
				{
					TightenVariableBounds(ref changeCount, vids, vLo, vHi, coeffs, row, ref infeasiblePrimal, i);
				}
			}
		}

		private void TightenVariableBounds(ref int changeCount, int[] vids, Rational[] vLo, Rational[] vHi, Rational[] coeffs, int row, ref bool infeasiblePrimal, int v)
		{
			Rational rational = coeffs[v];
			InferredBounds value = default(InferredBounds);
			if (0 < rational)
			{
				value.lo = vLo[v] / rational;
				value.hi = vHi[v] / rational;
			}
			else
			{
				value.hi = vLo[v] / rational;
				value.lo = vHi[v] / rational;
			}
			if (value.hi < value.lo)
			{
				_infeasibleRowVarPairs.Add(new KeyValuePair<int, int>(row, vids[v]));
				infeasiblePrimal = true;
			}
			_inferredBounds[vids[v]] = value;
			changeCount++;
		}

		private void InferRowBounds(ref int changeCount, int[] vids, Rational[] vLo, Rational[] vHi, Rational[] coeffs, int rowVid, Rational rLo, Rational rHi, int row, double cutoff, int count, ref bool infeasiblePrimal)
		{
			Rational rational;
			Rational rational2 = (rational = 0);
			for (int i = 0; i < count; i++)
			{
				rational2 += vLo[i];
				rational += vHi[i];
			}
			if (_removeRedundantMinMax && rLo <= rational2 && rational2 <= rational && rational <= rHi)
			{
				RemoveRedundantRow(vids, coeffs, rowVid, rLo, rHi, count, rational2, rational);
				changeCount++;
			}
			else
			{
				if (!_allowRowBoundTightening)
				{
					return;
				}
				bool flag = false;
				if (rLo < rational2 && (!rLo.IsNegativeInfinity || 0.0 - cutoff < rational2))
				{
					flag = true;
					rLo = rational2;
				}
				if (rational < rHi && (!rHi.IsPositiveInfinity || rational < cutoff))
				{
					flag = true;
					rHi = rational;
				}
				if (flag)
				{
					if (!AddInferredBounds(rowVid, rLo, rHi))
					{
						_infeasibleRowVarPairs.Add(new KeyValuePair<int, int>(row, -1));
						infeasiblePrimal = true;
					}
					changeCount++;
				}
			}
		}

		private void RemoveRedundantRow(int[] vids, Rational[] coeffs, int rowVid, Rational rLo, Rational rHi, int count, Rational lo, Rational hi)
		{
			if (lo == rHi || hi == rLo)
			{
				for (int i = 0; i < count; i++)
				{
					GetUserBounds(vids[i], out var lo2, out var hi2);
					if (lo2 != hi2)
					{
						Rational rational = (((!(coeffs[i] > 0) || !(lo == rHi)) && (!(coeffs[i] < 0) || !(hi == rLo))) ? hi2 : lo2);
						AddInferredBounds(vids[i], rational, rational);
					}
				}
			}
			removedRowsByVid[rowVid] = true;
		}

		protected abstract double GetDualFromDualVector(Vector y, int vid);

		protected Rational SingletonRowDual(Vector y, int vid)
		{
			LinearEntry singleton = model.GetRowEntries(vid).First();
			if (!IsSingletonRowActive(vid, singleton))
			{
				return Rational.Zero;
			}
			double num = model.GetCoefficient(GoalVid, singleton.Index).ToDouble();
			foreach (LinearEntry colValue in model.GetColValues(singleton.Index))
			{
				if (colValue.Index != vid)
				{
					double num2 = GetDualValue(colValue.Index).ToDouble();
					double num3 = num;
					Rational value = colValue.Value;
					num = num3 - value.ToDouble() * num2;
				}
			}
			return num / singleton.Value.ToDouble();
		}

		protected bool IsSingletonRowActive(int vid, LinearEntry singleton)
		{
			Rational value = model.GetValue(singleton.Index);
			GetUserBounds(vid, out var lo, out var hi);
			double num = Math.Abs((lo - value).ToDouble());
			double num2 = Math.Abs((hi - value).ToDouble());
			double num3 = Math.Min(SolveTolerance, 0.001);
			if (num < num3 || num2 < num3)
			{
				return true;
			}
			return false;
		}
	}
}
