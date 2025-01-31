using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class CutGenerator
	{
		private struct SetCover
		{
			private List<int> _cover;

			private List<int> _liftedIndices;

			private Rational _lambda;

			public List<int> CoverIndices
			{
				get
				{
					if (_cover == null)
					{
						_cover = new List<int>();
					}
					return _cover;
				}
			}

			public List<int> LiftedIndices
			{
				get
				{
					if (_liftedIndices == null)
					{
						_liftedIndices = new List<int>();
					}
					return _liftedIndices;
				}
			}

			public Rational Lambda
			{
				get
				{
					return _lambda;
				}
				set
				{
					_lambda = value;
				}
			}
		}

		/// <summary>
		/// Generate cover cuts
		/// </summary>
		/// <param name="thread">the thread that stores the user model</param>
		/// <param name="nodeLimit">the maximum number of cuts to generate</param>
		/// <param name="pool">the cutting plane pool to store the generated cuts</param>
		internal static void Cover(SimplexTask thread, int nodeLimit, CuttingPlanePool pool)
		{
			if (pool.PathCutCount >= CuttingPlanePool.PathCutLimit)
			{
				return;
			}
			SimplexSolver solver = thread.Solver;
			List<int> list = new List<int>();
			List<int> list2 = new List<int>();
			List<int> list3 = new List<int>();
			List<Rational> list4 = new List<Rational>();
			foreach (int variableIndex in solver.VariableIndices)
			{
				int matchedBoolVid = -1;
				Rational matchedBoolCoef = Rational.Indeterminate;
				bool integrality = solver.GetIntegrality(variableIndex);
				if (!integrality && !FindRealVariablesConstrainedByBooleanVariables(thread, variableIndex, out matchedBoolVid, out matchedBoolCoef))
				{
					continue;
				}
				if (!integrality)
				{
					list2.Add(variableIndex);
					list3.Add(matchedBoolVid);
					list4.Add(matchedBoolCoef);
				}
				foreach (LinearEntry variableEntry in solver.GetVariableEntries(variableIndex))
				{
					int index = variableEntry.Index;
					string text = solver.GetKeyFromIndex(index).ToString();
					if (!text.Contains("cutCover"))
					{
						list.Add(index);
					}
				}
			}
			int count = 0;
			int num = 0;
			int num2 = list.Count + 1;
			foreach (int item in list)
			{
				num++;
				if (pool.PathCutCount >= CuttingPlanePool.PathCutLimit || GenerateCoverCutFromRow(thread, item, list2, list3, list4, pool, (nodeLimit - count) / (num2 - num), ref count))
				{
					break;
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="vidRow"></param>
		/// <param name="matchedRealVids"></param>
		/// <param name="matchedBoolVids"></param>
		/// <param name="matchedBoolCoefs"></param>
		/// <param name="pool"></param>
		/// <param name="rowLimit"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		/// <remarks>
		/// Let S = {x \in {0,1}^N : sum_{j \in N}(aj*xj &lt;= b)}, where aj &gt;= 0 and b &gt;= 0. Let C be a cover, i.e.
		/// \lambda = sum_{j \in C}(aj) - b &gt; 0. Then the following inequality is valid for S:
		///
		/// sum_{j\in C}(xj) &lt;= |C| - 1
		///
		/// Now we deal with the case where some aj's are negative. Formally, we assume, for 0 &lt;= j &lt;= k, aj &gt;= 0, and, for
		/// k+1 &lt;= j &lt; N, aj &lt; 0. Then the inequality becomes:
		///
		/// sum_{j\in C and j &lt;= k}(xj) - sum_{j\in C and j &gt;= k+1}(xj) &lt;= |C| - 1 - |{xj : j\in C and j &gt;= k+1}|
		///
		/// Moveover, the definition of a cover C becomes: \lambda = sum_{j\in C}(|aj|) - b' &gt; 0,
		/// where b' = b - sum_{k+1 &lt;= j &lt;= n-1}(aj) (note that b is the upper-bound of the slack variable of the row).
		/// </remarks>
		private static bool GenerateCoverCutFromRow(SimplexTask thread, int vidRow, List<int> matchedRealVids, List<int> matchedBoolVids, List<Rational> matchedBoolCoefs, CuttingPlanePool pool, int rowLimit, ref int count)
		{
			int row = thread.Model.GetRow(vidRow);
			if (row == -1 || thread.Model.GetSlackVarForRow(row) == -1)
			{
				return false;
			}
			bool flag = GenerateCoverCutFromRowUpperBound(thread, vidRow, matchedRealVids, matchedBoolVids, matchedBoolCoefs, pool, ref count);
			return flag | GenerateCoverCutFromRowLowerBound(thread, vidRow, matchedRealVids, matchedBoolVids, matchedBoolCoefs, pool, ref count);
		}

		/// <summary>
		/// Generate a cover cut for a row.
		/// </summary>
		private static bool GenerateCoverCutFromRowUpperBound(SimplexTask thread, int vidRow, List<int> matchedRealVids, List<int> matchedBoolVids, List<Rational> matchedBoolCoefs, CuttingPlanePool pool, ref int count)
		{
			return GenerateCoverCutFromRow(thread, vidRow, useLowerBound: false, matchedRealVids, matchedBoolVids, matchedBoolCoefs, pool, ref count);
		}

		private static bool GenerateCoverCutFromRowLowerBound(SimplexTask thread, int vidRow, List<int> matchedRealVids, List<int> matchedBoolVids, List<Rational> matchedBoolCoefs, CuttingPlanePool pool, ref int count)
		{
			return GenerateCoverCutFromRow(thread, vidRow, useLowerBound: true, matchedRealVids, matchedBoolVids, matchedBoolCoefs, pool, ref count);
		}

		private static bool GenerateCoverCutFromRow(SimplexTask thread, int vidRow, bool useLowerBound, List<int> matchedRealVids, List<int> matchedBoolVids, List<Rational> matchedBoolCoefs, CuttingPlanePool pool, ref int count)
		{
			int row = thread.Model.GetRow(vidRow);
			SimplexSolver solver = thread.Solver;
			thread.BoundManager.GetRowBounds(row, out var lowerBound, out var upperBound);
			Rational rational = ((!useLowerBound) ? upperBound : (-lowerBound));
			if (!rational.IsFinite || solver.GetRowEntryCount(vidRow) > 20)
			{
				return false;
			}
			bool flag = false;
			bool[] array = new bool[solver.GetRowEntryCount(vidRow)];
			double[] array2 = new double[solver.GetRowEntryCount(vidRow)];
			double[] array3 = new double[solver.GetRowEntryCount(vidRow)];
			int num = 0;
			foreach (LinearEntry rowEntry in solver.GetRowEntries(vidRow))
			{
				int num2 = rowEntry.Index;
				if (num2 == vidRow)
				{
					continue;
				}
				int var = thread.Model.GetVar(num2);
				if (var == -1)
				{
					continue;
				}
				Rational rational2 = thread.Model.Matrix.GetCoefExact(row, var);
				if (rational2.IsZero)
				{
					continue;
				}
				if (useLowerBound)
				{
					rational2 = -rational2;
				}
				int num3 = -1;
				if ((num3 = matchedRealVids.IndexOf(num2)) != -1 && rational2 < 0 && thread.Model.GetVar(matchedBoolVids[num3]) != -1)
				{
					var = thread.Model.GetVar(matchedBoolVids[num3]);
					num2 = matchedBoolVids[num3];
					rational2 *= matchedBoolCoefs[num3];
				}
				Rational lowerBound2 = thread.BoundManager.GetLowerBound(var);
				Rational upperBound2 = thread.BoundManager.GetUpperBound(var);
				if (!solver.GetBoolean(num2) || lowerBound2 == upperBound2)
				{
					if (rational2 > 0)
					{
						rational -= rational2 * lowerBound2;
					}
					else
					{
						rational -= rational2 * upperBound2;
					}
					if (!rational.IsFinite)
					{
						return false;
					}
					continue;
				}
				Rational varValue = thread.AlgorithmExact.GetVarValue(var);
				flag |= !varValue.IsInteger();
				if (rational2 > 0)
				{
					if (!varValue.IsOne)
					{
						array2[num] = (double)(1 - varValue);
					}
					array3[num] = (double)rational2;
				}
				else
				{
					if (!varValue.IsZero)
					{
						array2[num] = (double)varValue;
					}
					array[num] = true;
					array3[num] = (double)(-rational2);
					rational -= rational2;
				}
				num++;
			}
			if (!flag || rational <= 0)
			{
				return false;
			}
			bool result = false;
			double num4 = 1E-07;
			if (KnapsackSolver.Solve(array2, array3, solver.GetRowEntryCount(vidRow), (double)(rational + 1E-07), longer: true, 1.0, out var selection, out var value) && value < 1.0 - num4)
			{
				AddCutToPool(thread, vidRow, useLowerBound, matchedRealVids, matchedBoolVids, matchedBoolCoefs, pool, ref count, selection, array);
				result = true;
			}
			return result;
		}

		/// <summary>
		/// Adds the cut to the model.
		/// </summary>
		private static void AddCutToPool(SimplexTask thread, int vidRow, bool useLowerBound, List<int> matchedRealVids, List<int> matchedBoolVids, List<Rational> matchedBoolCoefs, CuttingPlanePool pool, ref int count, bool[] selectedIndexes, bool[] negated)
		{
			int row = thread.Model.GetRow(vidRow);
			SimplexSolver solver = thread.Solver;
			Rational negativeInfinity = Rational.NegativeInfinity;
			Rational upper = 0;
			VectorRational vectorRational = new VectorRational(solver.KeyCount);
			int num = 0;
			foreach (LinearEntry rowEntry in solver.GetRowEntries(vidRow))
			{
				int num2 = rowEntry.Index;
				if (num2 == vidRow)
				{
					continue;
				}
				int var = thread.Model.GetVar(num2);
				if (var == -1)
				{
					continue;
				}
				Rational rational = thread.Model.Matrix.GetCoefExact(row, var);
				if (rational.IsZero)
				{
					continue;
				}
				if (useLowerBound)
				{
					rational = -rational;
				}
				int num3 = -1;
				if ((num3 = matchedRealVids.IndexOf(num2)) != -1 && rational < 0 && thread.Model.GetVar(matchedBoolVids[num3]) != -1)
				{
					num2 = matchedBoolVids[num3];
					var = thread.Model.GetVar(matchedBoolVids[num3]);
				}
				Rational lowerBound = thread.BoundManager.GetLowerBound(var);
				Rational upperBound = thread.BoundManager.GetUpperBound(var);
				if (!solver.GetBoolean(num2) || lowerBound == upperBound)
				{
					continue;
				}
				if (selectedIndexes[num])
				{
					vectorRational.SetCoef(num2, (!negated[num]) ? 1 : (-1));
					if (!negated[num])
					{
						upper += (Rational)1;
					}
				}
				num++;
			}
			upper -= (Rational)1;
			CuttingPlanePool.CuttingPlane cut = new CuttingPlanePool.CuttingPlane(CutKind.Cover, vectorRational, negativeInfinity, upper);
			pool.AddCut(cut);
			pool.IncrementCoverCutCount();
			count++;
		}

		internal static void FlowCover(SimplexTask thread, int nodeLimit, CuttingPlanePool pool)
		{
			if (pool.PathCutCount >= CuttingPlanePool.PathCutLimit)
			{
				return;
			}
			SimplexSolver solver = thread.Solver;
			List<List<int>> list = new List<List<int>>();
			List<List<Rational>> list2 = new List<List<Rational>>();
			List<Rational> list3 = new List<Rational>();
			foreach (int rowIndex in solver.RowIndices)
			{
				if (CuttingPlanePool.IsUsedRowFlowCover(thread, rowIndex))
				{
					continue;
				}
				pool.AddUsedRowFlowCover(thread, rowIndex);
				thread.BoundManager.GetVidBounds(rowIndex, out var _, out var upper);
				if (upper < 0 || upper.IsIndeterminate || upper.IsInfinite)
				{
					continue;
				}
				List<int> list4 = new List<int>();
				List<Rational> list5 = new List<Rational>();
				bool flag = true;
				foreach (LinearEntry rowEntry in solver.GetRowEntries(rowIndex))
				{
					if (rowEntry.Index != rowIndex)
					{
						if (solver.GetIntegrality(rowEntry.Index))
						{
							flag = false;
							break;
						}
						thread.BoundManager.GetVidBounds(rowEntry.Index, out var lower2, out var _);
						if (lower2 < 0)
						{
							flag = false;
							break;
						}
						list4.Add(rowEntry.Index);
						list5.Add(rowEntry.Value);
					}
				}
				if (flag && list4.Count > 0)
				{
					list.Add(list4);
					list2.Add(list5);
					list3.Add(upper);
				}
			}
			int count = 0;
			int num = 0;
			int num2 = list.Count + 1;
			for (int i = 0; i < list.Count; i++)
			{
				List<int> vidRealVars = list[i];
				List<Rational> realVarCoefs = list2[i];
				Rational b = list3[i];
				num++;
				if (GenerateFlowCoverCutFromRow(thread, pool, vidRealVars, realVarCoefs, b, (nodeLimit - count) / (num2 - num), ref count) && (count >= nodeLimit || pool.PathCutCount >= CuttingPlanePool.PathCutLimit))
				{
					break;
				}
			}
		}

		private static bool GenerateFlowCoverCutFromRow(SimplexTask thread, CuttingPlanePool pool, List<int> vidRealVars, List<Rational> realVarCoefs, Rational b, int nodeLimit, ref int count)
		{
			SimplexSolver solver = thread.Solver;
			List<int> list = new List<int>();
			List<int> list2 = new List<int>();
			List<Rational> list3 = new List<Rational>();
			List<Rational> list4 = new List<Rational>();
			List<int> list5 = new List<int>();
			List<Rational> list6 = new List<Rational>();
			for (int i = 0; i < vidRealVars.Count; i++)
			{
				if (FindRealVariablesConstrainedByBooleanVariables(thread, vidRealVars[i], out var matchedBoolVid, out var matchedBoolCoef))
				{
					list.Add(vidRealVars[i]);
					list2.Add(matchedBoolVid);
					list3.Add(matchedBoolCoef * realVarCoefs[i]);
					list4.Add(realVarCoefs[i]);
				}
				else
				{
					list5.Add(vidRealVars[i]);
					list6.Add(realVarCoefs[i]);
				}
			}
			Rational rational = 0;
			for (int j = 0; j < list6.Count; j++)
			{
				thread.BoundManager.GetVidBounds(list5[j], out var lower, out var upper);
				if (lower.IsInfinite || upper.IsInfinite)
				{
					return false;
				}
				if (list6[j] > 0)
				{
					rational += list6[j] * upper;
				}
				else
				{
					rational += list6[j] * lower;
				}
			}
			if (rational > b)
			{
				rational = b;
				VectorRational vectorRational = new VectorRational(solver.KeyCount);
				Rational cutLowerBound = Rational.NegativeInfinity;
				Rational cutUpperBound = rational;
				for (int k = 0; k < list5.Count; k++)
				{
					vectorRational.SetCoefNonZero(list5[k], list6[k]);
				}
				IsBetterThanImpliedBounds(thread, vectorRational, ref cutLowerBound, ref cutUpperBound);
				CuttingPlanePool.CuttingPlane cut = new CuttingPlanePool.CuttingPlane(CutKind.FlowCover, vectorRational, cutLowerBound, cutUpperBound);
				pool.AddCut(cut);
				pool.IncrementFlowCoverCutCount();
			}
			int num = 0;
			foreach (SetCover item in FindMinCover(list3.ToArray(), b - rational))
			{
				VectorRational vectorRational = new VectorRational(solver.KeyCount);
				Rational cutUpperBound2 = b;
				foreach (int coverIndex in item.CoverIndices)
				{
					vectorRational.SetCoefNonZero(list[coverIndex], list4[coverIndex]);
					Rational rational2 = list3[coverIndex] - item.Lambda;
					if (rational2 > 0)
					{
						vectorRational.SetCoefNonZero(list2[coverIndex], -rational2);
						cutUpperBound2 -= rational2;
					}
				}
				if (vectorRational.EntryCount <= 0)
				{
					continue;
				}
				for (int l = 0; l < list5.Count; l++)
				{
					vectorRational.SetCoefNonZero(list5[l], list6[l]);
				}
				Rational cutLowerBound2 = Rational.NegativeInfinity;
				if (IsBetterThanImpliedBounds(thread, vectorRational, ref cutLowerBound2, ref cutUpperBound2))
				{
					CuttingPlanePool.CuttingPlane cut = new CuttingPlanePool.CuttingPlane(CutKind.FlowCover, vectorRational, cutLowerBound2, cutUpperBound2);
					pool.AddCut(cut);
					pool.IncrementFlowCoverCutCount();
					if (++num >= nodeLimit || pool.PathCutCount >= CuttingPlanePool.PathCutLimit)
					{
						break;
					}
				}
			}
			count += num;
			return num > 0;
		}

		internal static bool FindRealVariablesConstrainedByBooleanVariables(SimplexTask thread, int vidRealVar, out int matchedBoolVid, out Rational matchedBoolCoef)
		{
			matchedBoolVid = -1;
			matchedBoolCoef = Rational.Indeterminate;
			SimplexSolver solver = thread.Solver;
			bool flag = false;
			foreach (LinearEntry variableEntry in solver.GetVariableEntries(vidRealVar))
			{
				int index = variableEntry.Index;
				if (solver.GetRowEntryCount(index) != 3)
				{
					continue;
				}
				thread.BoundManager.GetVidBounds(index, out var lower, out var upper);
				if (!lower.IsZero && !upper.IsZero)
				{
					continue;
				}
				foreach (LinearEntry rowEntry in solver.GetRowEntries(index))
				{
					if (rowEntry.Index == index)
					{
						continue;
					}
					if (rowEntry.Index == vidRealVar)
					{
						if ((lower.IsZero && rowEntry.Value == -1) || (upper.IsZero && rowEntry.Value == 1))
						{
							continue;
						}
						break;
					}
					if (solver.GetBoolean(rowEntry.Index) && (!lower.IsZero || !(rowEntry.Value <= 0)) && (!upper.IsZero || !(rowEntry.Value >= 0)))
					{
						matchedBoolVid = rowEntry.Index;
						matchedBoolCoef = (lower.IsZero ? rowEntry.Value : (-rowEntry.Value));
						flag = true;
					}
					break;
				}
				if (flag)
				{
					break;
				}
			}
			return flag;
		}

		/// <summary>
		/// Test if the two bounds of the cut row are tighter than the implied bounds of the row. If implied bounds are tighter,
		/// the cut bounds will be updated.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="cutRow"></param>
		/// <param name="cutLowerBound">Ref param, value will be set to a better lower-bound</param>
		/// <param name="cutUpperBound">Ref param, value will be set to a better upper-bound</param>
		/// <returns>true if any cut bound is better than the implied bound.</returns>
		private static bool IsBetterThanImpliedBounds(SimplexTask thread, VectorRational cutRow, ref Rational cutLowerBound, ref Rational cutUpperBound)
		{
			BoundManager.ComputeImpliedBounds(thread, cutRow, out var impliedLowerBound, out var impliedUpperBound);
			if (impliedLowerBound < cutLowerBound && cutUpperBound < impliedUpperBound)
			{
				return true;
			}
			if (cutUpperBound < impliedUpperBound)
			{
				cutLowerBound = impliedLowerBound;
				return true;
			}
			if (impliedLowerBound < cutLowerBound)
			{
				cutUpperBound = impliedUpperBound;
				return true;
			}
			cutLowerBound = impliedLowerBound;
			cutUpperBound = impliedUpperBound;
			return false;
		}

		/// <summary>
		/// Generate gomory fractional cuts
		/// </summary>
		/// <param name="thread">the thread that solves the relaxation</param>
		/// <param name="result">the result of the relaxation (expect Optimal)</param>
		/// <param name="nodeLimit">the maximum number of cuts to generate</param>
		/// <param name="pool">the cutting plane pool to store the generated cuts</param>
		internal static void GomoryFractional(SimplexTask thread, LinearResult result, int nodeLimit, CuttingPlanePool pool)
		{
			if (pool.PathCutCount >= CuttingPlanePool.PathCutLimit || result != LinearResult.Optimal)
			{
				return;
			}
			SimplexSolver solver = thread.Solver;
			int num = 0;
			foreach (int variableIndex in solver.VariableIndices)
			{
				if (solver.GetIntegrality(variableIndex) && !solver.GetValue(variableIndex).IsInteger() && thread.IsBasicVarInReducedModel(variableIndex) && GenerateGomoryFractionalCutFromVid(thread, variableIndex, pool))
				{
					pool.IncrementGomoryFractionalCutCount();
					if (++num >= nodeLimit || pool.PathCutCount >= CuttingPlanePool.PathCutLimit)
					{
						break;
					}
				}
			}
		}

		private static bool GenerateGomoryFractionalCutFromVid(SimplexTask thread, int basicVarVid, CuttingPlanePool pool)
		{
			SimplexSolver solver = thread.Solver;
			VectorRational vectorRational = thread.ComputeTableauRow(basicVarVid);
			thread.BoundManager.GetVidBounds(basicVarVid, out var lower, out var upper);
			if ((lower.IsNegativeInfinity && upper.IsPositiveInfinity) || lower.IsNegativeInfinity || lower < 0)
			{
				return false;
			}
			Rational rational;
			if (lower.IsNegativeInfinity)
			{
				rational = ((upper > 0) ? upper : ((Rational)0));
			}
			else
			{
				Vector<Rational>.Iter iter = new Vector<Rational>.Iter(vectorRational);
				while (iter.IsValid)
				{
					vectorRational.SetCoefNonZero(iter.Rc, -1 * iter.Value);
					iter.Advance();
				}
				rational = ((lower < 0) ? (-lower) : ((Rational)0));
			}
			Vector<Rational>.Iter iter2 = new Vector<Rational>.Iter(vectorRational);
			while (iter2.IsValid)
			{
				if (iter2.Rc != basicVarVid)
				{
					thread.BoundManager.GetVidBounds(iter2.Rc, out lower, out upper);
					if ((lower.IsNegativeInfinity && upper.IsPositiveInfinity) || lower.IsNegativeInfinity || lower < 0)
					{
						return false;
					}
				}
				iter2.Advance();
			}
			Rational rational2 = rational - rational.GetFloor();
			Rational rational3 = rational2 / (1 - rational2);
			Rational cutLowerBound = rational2;
			VectorRational vectorRational2 = new VectorRational(vectorRational.RcCount);
			Vector<Rational>.Iter iter3 = new Vector<Rational>.Iter(vectorRational);
			while (iter3.IsValid)
			{
				int rc = iter3.Rc;
				Rational value = iter3.Value;
				Rational rational4 = value - value.GetFloor();
				thread.BoundManager.GetVidBounds(rc, out lower, out upper);
				if (solver.GetIntegrality(rc))
				{
					if (!lower.IsNegativeInfinity)
					{
						if (rational4 <= rational2)
						{
							if (rational4 != 0)
							{
								vectorRational2.SetCoefNonZero(rc, rational4);
							}
						}
						else if (rational3 != 0)
						{
							vectorRational2.SetCoefNonZero(rc, rational3 * (1 - rational4));
						}
					}
				}
				else if (!lower.IsNegativeInfinity)
				{
					if (value > 0)
					{
						vectorRational2.SetCoefNonZero(rc, value);
					}
					else if (rational3 != 0)
					{
						vectorRational2.SetCoefNonZero(rc, rational3 * value);
					}
				}
				iter3.Advance();
			}
			if (vectorRational2.EntryCount <= 0)
			{
				return false;
			}
			Rational cutUpperBound = Rational.PositiveInfinity;
			IsBetterThanImpliedBounds(thread, vectorRational2, ref cutLowerBound, ref cutUpperBound);
			CuttingPlanePool.CuttingPlane cut = new CuttingPlanePool.CuttingPlane(CutKind.GomoryFractional, vectorRational2, cutLowerBound, cutUpperBound);
			pool.AddCut(cut);
			return true;
		}

		/// <summary>
		/// Enumerate the minimal covers for the given array absCoef.
		/// </summary>
		/// <param name="absCoef">Array of non-negative values</param>
		/// <param name="bPrime"></param>
		/// <returns>Minimal covers. The indices stored in each cover are indices of absCoef.</returns>
		private static IEnumerable<SetCover> FindMinCover(Rational[] absCoef, Rational bPrime)
		{
			int[] iAbsCoef = new int[absCoef.Length];
			for (int i = 0; i < absCoef.Length; i++)
			{
				iAbsCoef[i] = i;
			}
			int coefCount = absCoef.Length;
			if (coefCount > 1)
			{
				Statics.QuickSortIndirect(iAbsCoef, absCoef, 0, coefCount - 1);
			}
			SetCover cover = default(SetCover);
			cover.Lambda = -1 * bPrime;
			int num;
			int p2 = (num = coefCount - 1);
			int p3 = num;
			while (p2 >= 0)
			{
				cover.CoverIndices.Add(iAbsCoef[p2]);
				cover.Lambda += absCoef[iAbsCoef[p2]];
				if (cover.Lambda > 0)
				{
					yield return cover;
					cover.CoverIndices.Remove(iAbsCoef[p3]);
					cover.LiftedIndices.Add(iAbsCoef[p3]);
					cover.Lambda -= absCoef[iAbsCoef[p3]];
					p3--;
				}
				p2--;
			}
		}

		internal static void MixedCover(SimplexTask thread, int nodeLimit, CuttingPlanePool pool)
		{
			if (pool.PathCutCount >= CuttingPlanePool.PathCutLimit)
			{
				return;
			}
			SimplexSolver solver = thread.Solver;
			List<int> list = new List<int>();
			foreach (int variableIndex in solver.VariableIndices)
			{
				if (!solver.GetIntegrality(variableIndex))
				{
					continue;
				}
				foreach (LinearEntry variableEntry in solver.GetVariableEntries(variableIndex))
				{
					int index = variableEntry.Index;
					if (!CuttingPlanePool.IsUsedRowMixedCover(thread, index))
					{
						pool.AddUsedRowMixedCover(thread, index);
						list.Add(index);
					}
				}
			}
			int count = 0;
			int num = 0;
			int num2 = list.Count + 1;
			foreach (int item in list)
			{
				num++;
				if (GenerateMixedCoverCutFromRow(thread, item, pool, (nodeLimit - count) / (num2 - num), ref count) && (count >= nodeLimit || pool.PathCutCount >= CuttingPlanePool.PathCutLimit))
				{
					break;
				}
			}
		}

		private static bool GenerateMixedCoverCutFromRow(SimplexTask thread, int vidRow, CuttingPlanePool pool, int rowLimit, ref int count)
		{
			SimplexSolver solver = thread.Solver;
			int rowEntryCount = solver.GetRowEntryCount(vidRow);
			if (rowEntryCount <= 2)
			{
				return false;
			}
			Rational[] array = new Rational[rowEntryCount - 1];
			Rational[] array2 = new Rational[rowEntryCount - 1];
			int[] array3 = new int[rowEntryCount - 1];
			bool[] array4 = new bool[rowEntryCount - 1];
			List<int> list = new List<int>();
			List<Rational> list2 = new List<Rational>();
			Rational rational = 0;
			thread.BoundManager.GetVidBounds(vidRow, out var _, out var upper);
			BoundManager.ComputeImpliedBounds(thread, vidRow, out var _, out var impliedUpperBound);
			Rational rational2 = ((upper >= impliedUpperBound) ? impliedUpperBound : upper);
			if (rational2.IsInfinite || rational2.IsIndeterminate)
			{
				return false;
			}
			int num = 0;
			foreach (LinearEntry rowEntry in solver.GetRowEntries(vidRow))
			{
				if (rowEntry.Index == vidRow)
				{
					continue;
				}
				thread.BoundManager.GetVidBounds(rowEntry.Index, out var lower2, out var upper2);
				if (!solver.GetIntegrality(rowEntry.Index))
				{
					list.Add(rowEntry.Index);
					list2.Add(rowEntry.Value);
					ref Rational reference = ref array[num];
					reference = 0;
					array3[num] = rowEntry.Index;
					array4[num] = rowEntry.Value < 0;
					if (rowEntry.Value > 0)
					{
						rational -= rowEntry.Value * upper2;
					}
					else
					{
						rational -= rowEntry.Value * lower2;
					}
					continue;
				}
				if (lower2 == Rational.NegativeInfinity || upper2 == Rational.PositiveInfinity)
				{
					return false;
				}
				if (rowEntry.Value > 0 && lower2 < 0)
				{
					rational2 -= rowEntry.Value * lower2;
				}
				if (rowEntry.Value < 0 && upper2 > 0)
				{
					rational2 -= rowEntry.Value * upper2;
				}
				ref Rational reference2 = ref array[num];
				Rational value = rowEntry.Value;
				reference2 = value.AbsoluteValue;
				if (rowEntry.Value > 0 && lower2 >= 0)
				{
					ref Rational reference3 = ref array2[num];
					reference3 = array[num] * upper2;
				}
				else if (rowEntry.Value > 0 && lower2 < 0)
				{
					ref Rational reference4 = ref array2[num];
					reference4 = array[num] * (upper2 - lower2);
				}
				else if (rowEntry.Value < 0 && upper2 <= 0)
				{
					ref Rational reference5 = ref array2[num];
					reference5 = array[num] * -lower2;
				}
				else if (rowEntry.Value < 0 && upper2 > 0)
				{
					ref Rational reference6 = ref array2[num];
					reference6 = array[num] * (upper2 - lower2);
				}
				array3[num] = rowEntry.Index;
				array4[num] = rowEntry.Value < 0;
				num++;
			}
			if (list.Count <= 0)
			{
				return false;
			}
			if (rational == Rational.NegativeInfinity)
			{
				return false;
			}
			rational2 += rational;
			int num2 = 0;
			if (rational2 < 0)
			{
				VectorRational vectorRational = new VectorRational(solver.KeyCount);
				Rational cutLowerBound = Rational.NegativeInfinity;
				Rational cutUpperBound = rational2 - rational;
				for (int i = 0; i < list.Count; i++)
				{
					vectorRational.SetCoefNonZero(list[i], list2[i]);
				}
				IsBetterThanImpliedBounds(thread, vectorRational, ref cutLowerBound, ref cutUpperBound);
				CuttingPlanePool.CuttingPlane cut = new CuttingPlanePool.CuttingPlane(CutKind.MixedCover, vectorRational, cutLowerBound, cutUpperBound);
				pool.AddCut(cut);
				pool.IncrementMixedCoverCutCount();
				rational2 = 0;
			}
			foreach (SetCover item in FindMinCover(array2, rational2))
			{
				VectorRational vectorRational = new VectorRational(solver.KeyCount);
				Rational cutLowerBound = Rational.NegativeInfinity;
				Rational cutUpperBound = 0;
				Rational rational3 = Rational.NegativeInfinity;
				foreach (int coverIndex in item.CoverIndices)
				{
					if (array[coverIndex] > rational3)
					{
						rational3 = array[coverIndex];
					}
					thread.BoundManager.GetVidBounds(array3[coverIndex], out var lower3, out var upper3);
					if (array4[coverIndex])
					{
						vectorRational.SetCoefNonZero(array3[coverIndex], -1);
						cutUpperBound -= lower3;
					}
					else
					{
						vectorRational.SetCoefNonZero(array3[coverIndex], 1);
						cutUpperBound += upper3;
					}
				}
				Rational ceiling = (item.Lambda / rational3).GetCeiling();
				Rational rational4 = item.Lambda - (ceiling - 1) * rational3;
				cutUpperBound -= ceiling + rational / rational4;
				for (int j = 0; j < list.Count; j++)
				{
					vectorRational.SetCoefNonZero(list[j], list2[j] / rational4);
				}
				if (IsBetterThanImpliedBounds(thread, vectorRational, ref cutLowerBound, ref cutUpperBound))
				{
					CuttingPlanePool.CuttingPlane cut = new CuttingPlanePool.CuttingPlane(CutKind.MixedCover, vectorRational, cutLowerBound, cutUpperBound);
					pool.AddCut(cut);
					pool.IncrementMixedCoverCutCount();
					if (++num2 >= rowLimit || pool.PathCutCount >= CuttingPlanePool.PathCutLimit)
					{
						break;
					}
				}
			}
			count += num2;
			return num2 > 0;
		}
	}
}
