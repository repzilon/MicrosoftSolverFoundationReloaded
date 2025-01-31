using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class Presolving
	{
		private Rational[] _impliedRowLowerBounds;

		private Rational[] _impliedRowUpperBounds;

		/// <summary>
		/// Applies a set of techniques to reduce a mixed-integer program.
		/// </summary>
		/// <param name="thread">The thread for which presolve is performed.</param>
		/// <param name="node">The node to tighten.</param>
		/// <param name="tightenRowBoundCount">The number of row bounds that were tighten.</param>
		/// <param name="tightenVariableBoundCount">The number of variable bounds that were tighten.</param>
		/// <returns>False if the model is detected to be infeasible; true otherwise.</returns>
		internal bool NodeMipPreSolve(SimplexTask thread, ref Node node, out int tightenRowBoundCount, out int tightenVariableBoundCount)
		{
			DebugContracts.NonNull(thread);
			int num = 5;
			if (_impliedRowLowerBounds == null || _impliedRowLowerBounds.Length != thread.Model.RowLim + 1)
			{
				_impliedRowLowerBounds = new Rational[thread.Model.RowLim + 1];
			}
			if (_impliedRowUpperBounds == null || _impliedRowUpperBounds.Length != thread.Model.RowLim + 1)
			{
				_impliedRowUpperBounds = new Rational[thread.Model.RowLim + 1];
			}
			tightenRowBoundCount = 0;
			tightenVariableBoundCount = 0;
			bool flag = true;
			bool flag2 = true;
			int num2 = 0;
			while (flag2 && num2 < num)
			{
				flag2 = false;
				num2++;
				if (!ComputeImpliedRowBounds(thread, _impliedRowLowerBounds, _impliedRowUpperBounds))
				{
					return false;
				}
				flag2 |= TightenVariableBounds(thread, ref node, _impliedRowLowerBounds, _impliedRowUpperBounds, ref tightenVariableBoundCount);
				flag2 |= TightenRowBounds(thread, ref node, _impliedRowLowerBounds, _impliedRowUpperBounds, ref tightenRowBoundCount);
			}
			return ComputeImpliedRowBounds(thread, _impliedRowLowerBounds, _impliedRowUpperBounds);
		}

		/// <summary>
		/// Computes the lower and upper bounds of the rows based on the lower and upper bounds on the variables.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="impliedLowerBounds">An array containing the implied lower bounds. The array gets populated inside the method.</param>
		/// <param name="impliedUpperBounds">An array containing the implied upper bounds. The array gets populated inside the method.</param>
		/// <returns>False if the model is detected to be infeasible; true otherwise.</returns>
		private static bool ComputeImpliedRowBounds(SimplexTask thread, Rational[] impliedLowerBounds, Rational[] impliedUpperBounds)
		{
			Array.Clear(impliedLowerBounds, 0, impliedLowerBounds.Length);
			Array.Clear(impliedUpperBounds, 0, impliedUpperBounds.Length);
			int num = thread.Model.RowLim;
			while (--num >= 0)
			{
				if (thread.Model.IsRowEliminated(num) || thread.Model.IsGoal(num))
				{
					continue;
				}
				int slackVarForRow = thread.Model.GetSlackVarForRow(num);
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(thread.Model.Matrix, num);
				while (rowIter.IsValid)
				{
					int column = rowIter.Column;
					if (column != slackVarForRow)
					{
						Rational lowerBound = thread.BoundManager.GetLowerBound(column);
						Rational upperBound = thread.BoundManager.GetUpperBound(column);
						if (upperBound < lowerBound)
						{
							return false;
						}
						Rational exact = rowIter.Exact;
						if (exact > 0)
						{
							impliedUpperBounds[num] += exact * upperBound;
							impliedLowerBounds[num] += exact * lowerBound;
						}
						else
						{
							impliedUpperBounds[num] -= exact.AbsoluteValue * lowerBound;
							impliedLowerBounds[num] -= exact.AbsoluteValue * upperBound;
						}
					}
					rowIter.Advance();
				}
				thread.BoundManager.GetRowBounds(num, out var lowerBound2, out var upperBound2);
				if (impliedLowerBounds[num] > upperBound2 || impliedUpperBounds[num] < lowerBound2)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Tightens the variable bounds based on the bounds of rows and other variables.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="node"></param>
		/// <param name="impliedRowLowerBounds">An array containing the implied lower bounds.</param>
		/// <param name="impliedRowUpperBounds">An array containing the implied lower bounds.</param>
		/// <param name="tightenVariableBoundCount"></param>
		/// <returns>True if a change was made; false otherwise.</returns>
		private static bool TightenVariableBounds(SimplexTask thread, ref Node node, Rational[] impliedRowLowerBounds, Rational[] impliedRowUpperBounds, ref int tightenVariableBoundCount)
		{
			bool result = false;
			int num = thread.Model.RowLim;
			while (--num >= 0)
			{
				if (thread.Model.IsRowEliminated(num) || thread.Model.IsGoal(num))
				{
					continue;
				}
				int slackVarForRow = thread.Model.GetSlackVarForRow(num);
				thread.BoundManager.GetRowBounds(num, out var lowerBound, out var upperBound);
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(thread.Model.Matrix, num);
				while (rowIter.IsValid)
				{
					int column = rowIter.Column;
					if (column != slackVarForRow && !thread.BoundManager.IsFixed(column))
					{
						Rational exact = rowIter.Exact;
						if (thread.BoundManager.IsBinary(column))
						{
							Rational lowerBound2 = thread.BoundManager.GetLowerBound(column);
							Rational upperBound2 = thread.BoundManager.GetUpperBound(column);
							if (exact > 0)
							{
								if (impliedRowLowerBounds[num].IsFinite && impliedRowLowerBounds[num] + exact > upperBound)
								{
									if (upperBound2 > 0)
									{
										node.ExtendConstraint(new UpperBoundConstraint(thread.Model.GetVid(column), thread.Model.MapValueFromVarToVid(column, 0.0)));
									}
									thread.BoundManager.TrimVariableRange(column, 0, 0);
									tightenVariableBoundCount++;
									result = true;
								}
								if (impliedRowUpperBounds[num] - exact < lowerBound && impliedRowUpperBounds[num] >= lowerBound)
								{
									if (lowerBound2 < 1)
									{
										node.ExtendConstraint(new LowerBoundConstraint(thread.Model.GetVid(column), thread.Model.MapValueFromVarToVid(column, 1.0)));
									}
									thread.BoundManager.TrimVariableRange(column, 1, 1);
									tightenVariableBoundCount++;
									result = true;
								}
							}
							else
							{
								if (impliedRowUpperBounds[num].IsFinite && impliedRowUpperBounds[num] + exact < lowerBound)
								{
									if (upperBound2 > 0)
									{
										node.ExtendConstraint(new UpperBoundConstraint(thread.Model.GetVid(column), thread.Model.MapValueFromVarToVid(column, 0.0)));
									}
									thread.BoundManager.TrimVariableRange(column, 0, 0);
									tightenVariableBoundCount++;
									result = true;
								}
								if (impliedRowLowerBounds[num].IsFinite && impliedRowLowerBounds[num] - exact > upperBound)
								{
									if (lowerBound2 < 1)
									{
										node.ExtendConstraint(new LowerBoundConstraint(thread.Model.GetVid(column), thread.Model.MapValueFromVarToVid(column, 1.0)));
									}
									thread.BoundManager.TrimVariableRange(column, 1, 1);
									tightenVariableBoundCount++;
									result = true;
								}
							}
						}
						else
						{
							Rational lowerBound3 = thread.BoundManager.GetLowerBound(column);
							Rational upperBound3 = thread.BoundManager.GetUpperBound(column);
							if (exact > 0)
							{
								if (impliedRowLowerBounds[num].IsFinite && lowerBound3.IsFinite && upperBound3 > (upperBound - (impliedRowLowerBounds[num] - exact * lowerBound3)) / exact)
								{
									Rational bound = (upperBound - (impliedRowLowerBounds[num] - exact * lowerBound3)) / exact;
									bound = RoundDown(thread, column, bound);
									thread.BoundManager.SetUpperBound(column, bound);
									node.ExtendConstraint(new UpperBoundConstraint(thread.Model.GetVid(column), thread.Model.MapValueFromVarToVid(column, bound)));
									tightenVariableBoundCount++;
									result = true;
								}
								if (impliedRowUpperBounds[num].IsFinite && upperBound3.IsFinite && lowerBound3 < (lowerBound - (impliedRowUpperBounds[num] - exact * upperBound3)) / exact)
								{
									Rational bound2 = (lowerBound - (impliedRowUpperBounds[num] - exact * upperBound3)) / exact;
									bound2 = RoundUp(thread, column, bound2);
									thread.BoundManager.SetLowerBound(column, bound2);
									node.ExtendConstraint(new LowerBoundConstraint(thread.Model.GetVid(column), thread.Model.MapValueFromVarToVid(column, bound2)));
									tightenVariableBoundCount++;
									result = true;
								}
							}
							else
							{
								if (impliedRowLowerBounds[num].IsFinite && upperBound3.IsFinite && lowerBound3 < (upperBound - (impliedRowLowerBounds[num] + exact.AbsoluteValue * upperBound3)) / exact)
								{
									Rational bound3 = (upperBound - (impliedRowLowerBounds[num] + exact.AbsoluteValue * upperBound3)) / exact;
									bound3 = RoundUp(thread, column, bound3);
									thread.BoundManager.SetLowerBound(column, bound3);
									node.ExtendConstraint(new LowerBoundConstraint(thread.Model.GetVid(column), thread.Model.MapValueFromVarToVid(column, bound3)));
									tightenVariableBoundCount++;
									result = true;
								}
								if (impliedRowUpperBounds[num].IsFinite && lowerBound3.IsFinite && upperBound3 > (lowerBound - (impliedRowUpperBounds[num] + exact.AbsoluteValue * lowerBound3)) / exact)
								{
									Rational bound4 = (lowerBound - (impliedRowUpperBounds[num] + exact.AbsoluteValue * lowerBound3)) / exact;
									bound4 = RoundDown(thread, column, bound4);
									thread.BoundManager.SetUpperBound(column, bound4);
									node.ExtendConstraint(new UpperBoundConstraint(thread.Model.GetVid(column), thread.Model.MapValueFromVarToVid(column, bound4)));
									tightenVariableBoundCount++;
									result = true;
								}
							}
						}
					}
					rowIter.Advance();
				}
			}
			return result;
		}

		/// <summary>
		/// Tightens the row bounds to match the implied row bounds if these are more restrictive.
		/// If both the lower and upper implied row bounds are more restrictive than the row bounds,
		/// the row can be eliminated.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="node"></param>
		/// <param name="impliedRowLowerBounds">An array containing the implied lower bounds.</param>
		/// <param name="impliedRowUpperBounds">An array containing the implied lower bounds.</param>
		/// <param name="tightenRowCount"></param>
		/// <returns>True if a change was made; false otherwise.</returns>
		private static bool TightenRowBounds(SimplexTask thread, ref Node node, Rational[] impliedRowLowerBounds, Rational[] impliedRowUpperBounds, ref int tightenRowCount)
		{
			bool result = false;
			int num = thread.Model.RowLim;
			while (--num >= 0)
			{
				if (thread.Model.IsRowEliminated(num) || thread.Model.IsGoal(num))
				{
					continue;
				}
				thread.BoundManager.GetRowBounds(num, out var lowerBound, out var upperBound);
				if (!impliedRowLowerBounds[num].IsFinite || !(lowerBound < impliedRowLowerBounds[num]) || !impliedRowUpperBounds[num].IsFinite || !(upperBound > impliedRowUpperBounds[num]))
				{
					if (impliedRowLowerBounds[num].IsFinite && lowerBound < impliedRowLowerBounds[num])
					{
						Rational rational = RoundUp(thread, thread.Model.GetSlackVarForRow(num), impliedRowLowerBounds[num]);
						thread.BoundManager.SetRowBounds(num, rational, upperBound);
						node.ExtendConstraint(new RowBoundConstraint(num, thread.Model.MapValueFromVarToVid(thread.Model.GetSlackVarForRow(num), rational), thread.Model.MapValueFromVarToVid(thread.Model.GetSlackVarForRow(num), upperBound)));
						tightenRowCount++;
						result = true;
					}
					if (impliedRowUpperBounds[num].IsFinite && upperBound > impliedRowUpperBounds[num])
					{
						Rational rational2 = RoundDown(thread, thread.Model.GetSlackVarForRow(num), impliedRowUpperBounds[num]);
						thread.BoundManager.SetRowBounds(num, lowerBound, rational2);
						node.ExtendConstraint(new RowBoundConstraint(num, thread.Model.MapValueFromVarToVid(thread.Model.GetSlackVarForRow(num), lowerBound), thread.Model.MapValueFromVarToVid(thread.Model.GetSlackVarForRow(num), rational2)));
						tightenRowCount++;
						result = true;
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Applies a set of techniques to reduce a mixed-integer program.
		/// </summary>
		internal static bool MipPreSolve(SimplexSolver solver, SimplexReducedModel model, bool fCutGen)
		{
			DebugContracts.NonNull(model);
			DebugContracts.NonNull(solver);
			int num = 15;
			Rational[] impliedLowerBounds = new Rational[model.RowLim + 1];
			Rational[] impliedUpperBounds = new Rational[model.RowLim + 1];
			int eliminatedRowCount = 0;
			int tightenRowCount = 0;
			int tightenVariableBoundCount = 0;
			int tightenVariableCoefficientCount = 0;
			int tightenVariableBoundCount2 = 0;
			EliminateDuplicateColumns(model, ref tightenVariableBoundCount2);
			BitArray bitArray = new BitArray(model.RowLim + 1, defaultValue: true);
			BitArray newInterestingRows = new BitArray(model.RowLim + 1, defaultValue: false);
			if (!SetIntegrality(solver, model))
			{
				return false;
			}
			bool flag = true;
			int num2 = 0;
			while (flag && num2 < 100 && (fCutGen || num2 < num))
			{
				num2++;
				flag = false;
				int num3 = model.RowLim;
				while (--num3 >= 0)
				{
					if (model.IsRowEliminated(num3) || model.IsGoal(num3) || !bitArray[num3])
					{
						continue;
					}
					bool flag2 = true;
					while (flag2)
					{
						flag2 = false;
						ComputeImpliedRowBounds(model, num3, ref impliedLowerBounds, ref impliedUpperBounds, ref newInterestingRows);
						flag2 |= TightenRowBounds(model, num3, impliedLowerBounds, impliedUpperBounds, ref eliminatedRowCount, ref tightenRowCount, ref newInterestingRows);
						if (model.IsRowEliminated(num3))
						{
							flag = flag || flag2;
							break;
						}
						flag2 |= TightenVariableBounds(model, num3, impliedLowerBounds, impliedUpperBounds, ref tightenVariableBoundCount, ref newInterestingRows);
						flag2 |= TightenVariableCoefficients(model, num3, impliedLowerBounds, impliedUpperBounds, ref tightenVariableCoefficientCount, ref newInterestingRows);
						flag = flag || flag2;
					}
				}
				BitArray bitArray2 = bitArray;
				bitArray = newInterestingRows;
				newInterestingRows = bitArray2;
				newInterestingRows.SetAll(value: false);
			}
			solver.Logger.LogEvent(13, "Eliminated {0} rows in MIP presolve. \r\n                      Eliminated {1} duplicate columns in MIP presolve.\r\n                      Performed {2} row tightenings in MIP presolve. \r\n                      Performed {3} variable tightenings. \r\n                      Performed {4} coefficient tightenings.", eliminatedRowCount, tightenVariableBoundCount2, tightenRowCount, tightenVariableBoundCount, tightenVariableCoefficientCount);
			return true;
		}

		/// <summary>
		/// Sets the integrality of the slack variable based on the integrality of other variables.
		/// </summary>
		/// <param name="solver"></param>
		/// <param name="model"></param>
		/// <remarks>
		/// Setting the integrality is important for algorithms such as Gomory cuts which use the information.
		/// </remarks>
		private static bool SetIntegrality(SimplexSolver solver, SimplexReducedModel model)
		{
			int num = model.RowLim;
			while (--num >= 0)
			{
				if (model.IsRowEliminated(num))
				{
					continue;
				}
				bool flag = true;
				int slackVarForRow = model.GetSlackVarForRow(num);
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(model.Matrix, num);
				while (rowIter.IsValid && flag)
				{
					if (rowIter.Column != slackVarForRow && (!rowIter.Exact.IsInteger() || !model.IsVarInteger(rowIter.Column)))
					{
						flag = false;
					}
					rowIter.Advance();
				}
				if (flag)
				{
					int vid = model.GetVid(slackVarForRow);
					Rational rational = solver._mpvidnumLo[vid];
					Rational rational2 = solver._mpvidnumHi[vid];
					if (rational.IsFinite && rational2.IsFinite && rational.GetCeiling() > rational2)
					{
						return false;
					}
					solver.SetIntegrality(model.GetVid(slackVarForRow), flag);
				}
			}
			return true;
		}

		/// <summary>
		/// Eliminates duplicate columns by merging variables.
		/// </summary>
		/// <remarks>
		/// A post processing step is required to find the values of the merged variables.
		/// </remarks>
		/// <param name="model"></param>
		/// <param name="tightenVariableBoundCount"></param>
		private static void EliminateDuplicateColumns(SimplexReducedModel model, ref int tightenVariableBoundCount)
		{
			Dictionary<int, List<int>> dictionary = new Dictionary<int, List<int>>();
			for (int i = 0; i < model.VarLim; i++)
			{
				if (model.IsBinary(i) && model.GetLowerBound(i).IsZero && model.GetUpperBound(i).IsOne)
				{
					int num = 0;
					CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(model.Matrix, i);
					while (colIter.IsValid)
					{
						num ^= colIter.Row.GetHashCode() | colIter.Exact.GetHashCode();
						colIter.Advance();
					}
					if (!dictionary.ContainsKey(num))
					{
						dictionary.Add(num, new List<int>());
					}
					dictionary[num].Add(i);
				}
			}
			foreach (int key in dictionary.Keys)
			{
				if (dictionary[key].Count <= 1)
				{
					continue;
				}
				int num2 = model.Matrix.ColEntryCount(dictionary[key][0]);
				int num3 = 1;
				for (int j = 1; j < dictionary[key].Count; j++)
				{
					bool flag = true;
					if (model.Matrix.ColEntryCount(dictionary[key][j]) != num2)
					{
						flag = false;
					}
					CoefMatrix.ColIter colIter2 = new CoefMatrix.ColIter(model.Matrix, dictionary[key][j]);
					while (colIter2.IsValid && flag)
					{
						if (colIter2.Exact != model.Matrix.GetCoefExact(colIter2.Row, dictionary[key][0]))
						{
							flag = false;
							break;
						}
						colIter2.Advance();
					}
					if (flag)
					{
						model.TrimVariableRange(dictionary[key][j], 0, 0);
						tightenVariableBoundCount++;
						num3++;
						model.MarkAsMergedVid(dictionary[key][0], dictionary[key][j]);
					}
				}
				model.SetVariableUpperBound(dictionary[key][0], num3);
			}
		}

		/// <summary>
		/// Computes the lower and upper bounds of the rows based on the lower and upper bounds on the variables.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="row"></param>
		/// <param name="impliedLowerBounds">An array containing the implied lower bounds. The array gets populated inside the method.</param>
		/// <param name="impliedUpperBounds">An array containing the implied upper bounds. The array gets populated inside the method.</param>
		/// <param name="newInterestingRows"></param>
		private static void ComputeImpliedRowBounds(SimplexReducedModel model, int row, ref Rational[] impliedLowerBounds, ref Rational[] impliedUpperBounds, ref BitArray newInterestingRows)
		{
			model.GetRowBounds(row, out var lowerBound, out var upperBound);
			ref Rational reference = ref impliedUpperBounds[row];
			reference = 0;
			ref Rational reference2 = ref impliedLowerBounds[row];
			reference2 = 0;
			int slackVarForRow = model.GetSlackVarForRow(row);
			CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(model.Matrix, row);
			while (rowIter.IsValid && (impliedLowerBounds[row].IsFinite || impliedUpperBounds[row].IsFinite))
			{
				int column = rowIter.Column;
				if (column != slackVarForRow)
				{
					Rational exact = rowIter.Exact;
					if (exact > 0)
					{
						impliedUpperBounds[row] += exact * model.GetUpperBound(column);
						impliedLowerBounds[row] += exact * model.GetLowerBound(column);
					}
					else
					{
						impliedUpperBounds[row] -= exact.AbsoluteValue * model.GetLowerBound(column);
						impliedLowerBounds[row] -= exact.AbsoluteValue * model.GetUpperBound(column);
					}
				}
				rowIter.Advance();
			}
			if (impliedLowerBounds[row] != lowerBound || impliedUpperBounds[row] != upperBound)
			{
				newInterestingRows.Set(row, value: true);
			}
		}

		/// <summary>
		/// Tightens the coefficients of variables based on the bounds of rows and other variables.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="row"></param>
		/// <param name="impliedRowLowerBounds">An array containing the implied lower bounds.</param>
		/// <param name="impliedRowUpperBounds">An array containing the implied lower bounds.</param>
		/// <param name="tightenVariableCoefficientCount"></param>
		/// <param name="newInterestingRows"></param>
		/// <returns>True if a change was made; false otherwise.</returns>
		private static bool TightenVariableCoefficients(SimplexReducedModel model, int row, Rational[] impliedRowLowerBounds, Rational[] impliedRowUpperBounds, ref int tightenVariableCoefficientCount, ref BitArray newInterestingRows)
		{
			bool result = false;
			int slackVarForRow = model.GetSlackVarForRow(row);
			model.GetRowBounds(row, out var lowerBound, out var upperBound);
			CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(model.Matrix, row);
			while (rowIter.IsValid)
			{
				int column = rowIter.Column;
				if (column != slackVarForRow && !model.IsFixed(column))
				{
					Rational exact = rowIter.Exact;
					if (model.IsBinary(column))
					{
						if (exact > 0)
						{
							if ((lowerBound.IsNegativeInfinity || lowerBound == impliedRowLowerBounds[row]) && impliedRowUpperBounds[row].IsFinite && upperBound.IsFinite && impliedRowUpperBounds[row] - exact < upperBound)
							{
								Rational rational = upperBound - (impliedRowUpperBounds[row] - exact);
								if (!(exact <= rational))
								{
									Rational upperBound2 = RoundDown(model, model.GetSlackVarForRow(row), upperBound - rational);
									model.SetRowBounds(row, lowerBound, upperBound2);
									model.Matrix.SetCoefExact(row, rowIter.Column, exact - rational);
									model.MarkAsModifiedRow(row);
									newInterestingRows.Set(row, value: true);
									tightenVariableCoefficientCount++;
									result = true;
									break;
								}
							}
							else if ((upperBound.IsPositiveInfinity || upperBound == impliedRowUpperBounds[row]) && impliedRowLowerBounds[row].IsFinite && lowerBound.IsFinite && impliedRowLowerBounds[row] + exact > lowerBound)
							{
								Rational rational2 = impliedRowLowerBounds[row] + exact - lowerBound;
								if (!(exact <= rational2))
								{
									model.Matrix.SetCoefExact(row, rowIter.Column, exact - rational2);
									model.MarkAsModifiedRow(row);
									newInterestingRows.Set(row, value: true);
									tightenVariableCoefficientCount++;
									result = true;
									break;
								}
							}
						}
						else if ((lowerBound.IsNegativeInfinity || lowerBound == impliedRowLowerBounds[row]) && impliedRowUpperBounds[row].IsFinite && upperBound.IsFinite && impliedRowUpperBounds[row] + exact < upperBound)
						{
							Rational rational3 = upperBound - (impliedRowUpperBounds[row] + exact);
							if (!(exact + rational3 >= 0))
							{
								model.Matrix.SetCoefExact(row, rowIter.Column, exact + rational3);
								model.MarkAsModifiedRow(row);
								newInterestingRows.Set(row, value: true);
								tightenVariableCoefficientCount++;
								result = true;
								break;
							}
						}
						else if ((upperBound.IsNegativeInfinity || upperBound == impliedRowUpperBounds[row]) && impliedRowLowerBounds[row].IsFinite && lowerBound.IsFinite && impliedRowLowerBounds[row] - exact > lowerBound)
						{
							Rational rational4 = impliedRowLowerBounds[row] - exact - lowerBound;
							if (!(exact + rational4 >= 0))
							{
								Rational lowerBound2 = RoundUp(model, model.GetSlackVarForRow(row), lowerBound + rational4);
								model.SetRowBounds(row, lowerBound2, upperBound);
								model.Matrix.SetCoefExact(row, rowIter.Column, exact + rational4);
								model.MarkAsModifiedRow(row);
								newInterestingRows.Set(row, value: true);
								tightenVariableCoefficientCount++;
								result = true;
								break;
							}
						}
					}
				}
				rowIter.Advance();
			}
			return result;
		}

		/// <summary>
		/// Tightens the variable bounds based on the bounds of rows and other variables.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="row"></param>
		/// <param name="impliedRowLowerBounds">An array containing the implied lower bounds.</param>
		/// <param name="impliedRowUpperBounds">An array containing the implied lower bounds.</param>
		/// <param name="tightenVariableBoundCount"></param>
		/// <param name="newInterestingRows"></param>
		/// <returns>True if a change was made; false otherwise.</returns>
		private static bool TightenVariableBounds(SimplexReducedModel model, int row, Rational[] impliedRowLowerBounds, Rational[] impliedRowUpperBounds, ref int tightenVariableBoundCount, ref BitArray newInterestingRows)
		{
			bool result = false;
			int slackVarForRow = model.GetSlackVarForRow(row);
			model.GetRowBounds(row, out var lowerBound, out var upperBound);
			CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(model.Matrix, row);
			while (rowIter.IsValid)
			{
				int column = rowIter.Column;
				if (column != slackVarForRow && !model.IsFixed(column))
				{
					Rational exact = rowIter.Exact;
					if (model.IsBinary(column))
					{
						if (exact > 0)
						{
							if (impliedRowLowerBounds[row].IsFinite && impliedRowLowerBounds[row] + exact > upperBound)
							{
								model.TrimVariableRange(column, 0, 0);
								CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(model.Matrix, column);
								while (colIter.IsValid)
								{
									newInterestingRows.Set(colIter.Row, value: true);
									colIter.Advance();
								}
								tightenVariableBoundCount++;
								result = true;
							}
							if (impliedRowUpperBounds[row] - exact < lowerBound && impliedRowUpperBounds[row] >= lowerBound)
							{
								model.TrimVariableRange(column, 1, 1);
								CoefMatrix.ColIter colIter2 = new CoefMatrix.ColIter(model.Matrix, column);
								while (colIter2.IsValid)
								{
									newInterestingRows.Set(colIter2.Row, value: true);
									colIter2.Advance();
								}
								tightenVariableBoundCount++;
								result = true;
							}
						}
						else
						{
							if (impliedRowUpperBounds[row].IsFinite && impliedRowUpperBounds[row] + exact < lowerBound)
							{
								model.TrimVariableRange(column, 0, 0);
								CoefMatrix.ColIter colIter3 = new CoefMatrix.ColIter(model.Matrix, column);
								while (colIter3.IsValid)
								{
									newInterestingRows.Set(colIter3.Row, value: true);
									colIter3.Advance();
								}
								tightenVariableBoundCount++;
								result = true;
							}
							if (impliedRowLowerBounds[row].IsFinite && impliedRowLowerBounds[row] - exact > upperBound)
							{
								model.TrimVariableRange(column, 1, 1);
								CoefMatrix.ColIter colIter4 = new CoefMatrix.ColIter(model.Matrix, column);
								while (colIter4.IsValid)
								{
									newInterestingRows.Set(colIter4.Row, value: true);
									colIter4.Advance();
								}
								tightenVariableBoundCount++;
								result = true;
							}
						}
					}
					else
					{
						Rational lowerBound2 = model.GetLowerBound(column);
						Rational upperBound2 = model.GetUpperBound(column);
						if (exact > 0)
						{
							if (impliedRowLowerBounds[row].IsFinite && lowerBound2.IsFinite)
							{
								Rational rational = RoundDown(model, column, (upperBound - (impliedRowLowerBounds[row] - exact * lowerBound2)) / exact);
								if (upperBound2 > rational)
								{
									model.SetVariableUpperBound(column, rational);
									CoefMatrix.ColIter colIter5 = new CoefMatrix.ColIter(model.Matrix, column);
									while (colIter5.IsValid)
									{
										newInterestingRows.Set(colIter5.Row, value: true);
										colIter5.Advance();
									}
									tightenVariableBoundCount++;
									result = true;
								}
							}
							if (impliedRowUpperBounds[row].IsFinite && upperBound2.IsFinite)
							{
								Rational rational2 = RoundUp(model, column, upperBound2 + (lowerBound - impliedRowUpperBounds[row]) / exact);
								if (lowerBound2 < rational2)
								{
									model.SetVariableLowerBound(column, rational2);
									CoefMatrix.ColIter colIter6 = new CoefMatrix.ColIter(model.Matrix, column);
									while (colIter6.IsValid)
									{
										newInterestingRows.Set(colIter6.Row, value: true);
										colIter6.Advance();
									}
									tightenVariableBoundCount++;
									result = true;
								}
							}
						}
						else
						{
							if (impliedRowLowerBounds[row].IsFinite && upperBound2.IsFinite)
							{
								Rational rational3 = RoundUp(model, column, upperBound2 + (upperBound - impliedRowLowerBounds[row]) / exact);
								if (lowerBound2 < rational3)
								{
									model.SetVariableLowerBound(column, rational3);
									CoefMatrix.ColIter colIter7 = new CoefMatrix.ColIter(model.Matrix, column);
									while (colIter7.IsValid)
									{
										newInterestingRows.Set(colIter7.Row, value: true);
										colIter7.Advance();
									}
									tightenVariableBoundCount++;
									result = true;
								}
							}
							if (impliedRowUpperBounds[row].IsFinite && lowerBound2.IsFinite)
							{
								Rational rational4 = RoundDown(model, column, (lowerBound - (impliedRowUpperBounds[row] + exact.AbsoluteValue * lowerBound2)) / exact);
								if (upperBound2 > rational4)
								{
									model.SetVariableUpperBound(column, rational4);
									CoefMatrix.ColIter colIter8 = new CoefMatrix.ColIter(model.Matrix, column);
									while (colIter8.IsValid)
									{
										newInterestingRows.Set(colIter8.Row, value: true);
										colIter8.Advance();
									}
									tightenVariableBoundCount++;
									result = true;
								}
							}
						}
					}
				}
				rowIter.Advance();
			}
			return result;
		}

		/// <summary>
		/// Tightens the row bounds to match the implied row bounds if these are more restrictive.
		/// If both the lower and upper implied row bounds are more restrictive than the row bounds,
		/// the row can be eliminated.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="row"></param>
		/// <param name="impliedRowLowerBounds">An array containing the implied lower bounds.</param>
		/// <param name="impliedRowUpperBounds">An array containing the implied lower bounds.</param>
		/// <param name="eliminatedRowCount"></param>
		/// <param name="tightenRowCount"></param>
		/// <param name="newInterestingRows"></param>
		/// <returns>True if a change was made; false otherwise.</returns>
		private static bool TightenRowBounds(SimplexReducedModel model, int row, Rational[] impliedRowLowerBounds, Rational[] impliedRowUpperBounds, ref int eliminatedRowCount, ref int tightenRowCount, ref BitArray newInterestingRows)
		{
			bool result = false;
			int slackVarForRow = model.GetSlackVarForRow(row);
			model.GetRowBounds(row, out var lowerBound, out var upperBound);
			if (impliedRowLowerBounds[row].IsFinite && lowerBound < impliedRowLowerBounds[row] && impliedRowUpperBounds[row].IsFinite && upperBound > impliedRowUpperBounds[row])
			{
				model.EliminateRow(row);
				newInterestingRows.Set(row, value: false);
				eliminatedRowCount++;
				result = true;
			}
			else
			{
				if (impliedRowLowerBounds[row].IsFinite && lowerBound < impliedRowLowerBounds[row])
				{
					Rational lowerBound2 = RoundUp(model, slackVarForRow, impliedRowLowerBounds[row]);
					model.SetRowBounds(row, lowerBound2, upperBound);
					newInterestingRows.Set(row, value: true);
					tightenRowCount++;
					result = true;
				}
				if (impliedRowUpperBounds[row].IsFinite && upperBound > impliedRowUpperBounds[row])
				{
					Rational upperBound2 = RoundDown(model, slackVarForRow, impliedRowUpperBounds[row]);
					model.SetRowBounds(row, lowerBound, upperBound2);
					newInterestingRows.Set(row, value: true);
					tightenRowCount++;
					result = true;
				}
			}
			return result;
		}

		private static Rational RoundUp(SimplexTask thread, int variable, Rational bound)
		{
			return RoundUp(thread.Model, variable, bound);
		}

		private static Rational RoundDown(SimplexTask thread, int variable, Rational bound)
		{
			return RoundDown(thread.Model, variable, bound);
		}

		private static Rational RoundUp(SimplexReducedModel model, int variable, Rational bound)
		{
			if (model.IsVarInteger(variable))
			{
				return model.MapValueFromVidToVar(variable, model.MapValueFromVarToVid(variable, bound).GetCeiling());
			}
			return bound;
		}

		private static Rational RoundDown(SimplexReducedModel model, int variable, Rational bound)
		{
			if (model.IsVarInteger(variable))
			{
				return model.MapValueFromVidToVar(variable, model.MapValueFromVarToVid(variable, bound).GetFloor());
			}
			return bound;
		}
	}
}
