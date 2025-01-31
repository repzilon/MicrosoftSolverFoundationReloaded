using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Generator of Gomory fractional cuts
	/// </summary>
	internal sealed class GomoryCutBuilder : CutBuilder
	{
		private SimplexReducedModel _mod;

		private BoundManager _boundManager;

		private int _basicVar;

		public GomoryCutBuilder(MipNode node)
			: base(node)
		{
			_mod = _node.Task.Model;
			_boundManager = _node.Task.BoundManager;
			_basicVar = -1;
		}

		public override IEnumerable<RowCut> Build()
		{
			int count = 0;
			foreach (VectorDouble tableauRow in FindTableauRow())
			{
				count++;
				yield return ComputeGomoryCut(tableauRow);
			}
		}

		private IEnumerable<VectorDouble> FindTableauRow()
		{
			SimplexBasis bas = _node.Task.Basis;
			int rowPos = _mod.RowLim;
			while (true)
			{
				int num;
				rowPos = (num = rowPos - 1);
				if (num < 0)
				{
					break;
				}
				_basicVar = bas.GetBasicVar(rowPos);
				if (_mod.IsVarInteger(_basicVar))
				{
					double numVal = _node.GetUserVarValue(_basicVar);
					if (!Statics.IsInteger(numVal))
					{
						int ivar = bas.GetBasisSlot(_basicVar);
						yield return _node.ComputeSimplexTableauRow(ivar);
					}
				}
			}
		}

		private RowCut ComputeGomoryCut(VectorDouble tableauRow)
		{
			VectorRational vectorRational = new VectorRational(tableauRow.RcCount);
			Rational rational = _mod.GetScale(_basicVar) / _mod.GetScaleDbl(_basicVar);
			Rational val = _node.GetUserVarValue(_basicVar);
			Rational rational2 = 0;
			Rational nonNegativeFraction = GetNonNegativeFraction(val);
			Vector<double>.Iter iter = new Vector<double>.Iter(tableauRow);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				if (_node.Task.Basis.GetVvk(rc) != SimplexVarValKind.Fixed && _node.Task.Basis.GetVvk(rc) != SimplexVarValKind.Zero)
				{
					Rational rational3 = _mod.GetScaleDbl(rc) / _mod.GetScale(rc);
					Rational rational4 = 1 / rational3;
					Rational delta = _mod.GetDelta(rc);
					Rational rational5 = iter.Value * rational;
					Rational rational6 = rational5 * rational3;
					if (_mod.IsVarInteger(rc))
					{
						_mod.UserModel.GetBounds(_mod.GetVid(rc), out var lower, out var upper);
						Rational nonNegativeFraction2 = GetNonNegativeFraction(rational6);
						Rational nonNegativeFraction3 = GetNonNegativeFraction(-rational6);
						if (IsIntegerVarAtLoWithSmallCoefFraction(rc, nonNegativeFraction, nonNegativeFraction2))
						{
							if (!(nonNegativeFraction2 == 0))
							{
								Rational num = nonNegativeFraction2 * rational4;
								vectorRational.SetCoefNonZero(rc, num);
								if (!lower.IsInteger())
								{
									lower = lower.GetCeiling();
								}
								rational2 += nonNegativeFraction2 * (lower - delta);
							}
						}
						else if (IsIntegerVarAtLoWithLargeCoefFraction(rc, nonNegativeFraction, nonNegativeFraction2))
						{
							Rational rational7 = nonNegativeFraction * (1 - nonNegativeFraction2) / (1 - nonNegativeFraction);
							Rational num2 = rational7 * rational4;
							vectorRational.SetCoefNonZero(rc, num2);
							if (!lower.IsInteger())
							{
								lower = lower.GetCeiling();
							}
							rational2 += rational7 * (lower - delta);
						}
						else if (IsIntegerVarAtHiWithSmallCoefFraction(rc, nonNegativeFraction, nonNegativeFraction3))
						{
							if (!(nonNegativeFraction3 == 0))
							{
								Rational num3 = -nonNegativeFraction3 * rational4;
								vectorRational.SetCoefNonZero(rc, num3);
								if (!upper.IsInteger())
								{
									upper = upper.GetFloor();
								}
								rational2 -= nonNegativeFraction3 * (upper - delta);
							}
						}
						else if (IsIntegerVarAtHiWithLargeCoefFraction(rc, nonNegativeFraction, nonNegativeFraction3))
						{
							Rational rational8 = nonNegativeFraction * (1 - nonNegativeFraction3) / (1 - nonNegativeFraction);
							Rational num4 = -rational8 * rational4;
							vectorRational.SetCoefNonZero(rc, num4);
							if (!upper.IsInteger())
							{
								upper = upper.GetFloor();
							}
							rational2 -= rational8 * (upper - delta);
						}
					}
					else if (IsRealVarAtLoWithPositiveCoef(rc, rational5))
					{
						vectorRational.SetCoefNonZero(rc, rational5);
						Rational rational9 = _boundManager.GetLowerBoundDbl(rc);
						rational2 += rational5 * rational9;
					}
					else if (IsRealVarAtLoWithNegativeCoef(rc, rational5))
					{
						Rational rational10 = nonNegativeFraction / (1 - nonNegativeFraction) * rational5;
						vectorRational.SetCoefNonZero(rc, -rational10);
						Rational rational11 = _boundManager.GetLowerBoundDbl(rc);
						rational2 -= rational10 * rational11;
					}
					else if (IsRealVarAtHiWithPositiveCoef(rc, rational5))
					{
						Rational rational12 = nonNegativeFraction / (1 - nonNegativeFraction) * rational5;
						vectorRational.SetCoefNonZero(rc, -rational12);
						Rational rational13 = _boundManager.GetUpperBoundDbl(rc);
						rational2 -= rational12 * rational13;
					}
					else if (IsRealVarAtHiWithNegativeCoef(rc, rational5))
					{
						vectorRational.SetCoefNonZero(rc, rational5);
						Rational rational14 = _boundManager.GetUpperBoundDbl(rc);
						rational2 += rational5 * rational14;
					}
				}
				iter.Advance();
			}
			Rational rational15 = nonNegativeFraction + rational2;
			if (vectorRational.EntryCount == 0 && rational15 > 0)
			{
				_isNodeIntegerInfeasible = true;
				return null;
			}
			VectorRational row = RemoveSlacksInCutRow(_mod, vectorRational);
			Rational rational16 = ComputeCutRowValue(row);
			if (rational16 < rational15)
			{
				return new RowCut(row, rational15, Rational.PositiveInfinity);
			}
			return new RowCut(vectorRational, rational15, Rational.PositiveInfinity);
		}

		private bool IsIntegerVarAtLo(int var)
		{
			SimplexVarValKind vvk = _node.Task.Basis.GetVvk(var);
			if (vvk != SimplexVarValKind.Fixed)
			{
				return vvk == SimplexVarValKind.Lower;
			}
			return true;
		}

		private bool IsIntegerVarAtHi(int var)
		{
			SimplexVarValKind vvk = _node.Task.Basis.GetVvk(var);
			return vvk == SimplexVarValKind.Upper;
		}

		private bool IsRealVarAtLo(int var)
		{
			SimplexVarValKind vvk = _node.Task.Basis.GetVvk(var);
			if (vvk != SimplexVarValKind.Fixed)
			{
				return vvk == SimplexVarValKind.Lower;
			}
			return true;
		}

		private bool IsRealVarAtHi(int var)
		{
			SimplexVarValKind vvk = _node.Task.Basis.GetVvk(var);
			return vvk == SimplexVarValKind.Upper;
		}

		private bool IsIntegerVarAtLoWithSmallCoefFraction(int var, Rational fzero, Rational fajbar)
		{
			if (IsIntegerVarAtLo(var))
			{
				return fajbar <= fzero;
			}
			return false;
		}

		private bool IsIntegerVarAtLoWithLargeCoefFraction(int var, Rational fzero, Rational fajbar)
		{
			if (IsIntegerVarAtLo(var))
			{
				return fajbar > fzero;
			}
			return false;
		}

		private bool IsIntegerVarAtHiWithSmallCoefFraction(int var, Rational fzero, Rational fminusajbar)
		{
			if (IsIntegerVarAtHi(var))
			{
				return fminusajbar <= fzero;
			}
			return false;
		}

		private bool IsIntegerVarAtHiWithLargeCoefFraction(int var, Rational fzero, Rational fminusajbar)
		{
			if (IsIntegerVarAtHi(var))
			{
				return fminusajbar > fzero;
			}
			return false;
		}

		private bool IsRealVarAtLoWithNegativeCoef(int var, Rational varCoefInTableauRow)
		{
			if (IsRealVarAtLo(var))
			{
				return varCoefInTableauRow < 0;
			}
			return false;
		}

		private bool IsRealVarAtLoWithPositiveCoef(int var, Rational varCoefInTableauRow)
		{
			if (IsRealVarAtLo(var))
			{
				return varCoefInTableauRow > 0;
			}
			return false;
		}

		private bool IsRealVarAtHiWithNegativeCoef(int var, Rational varCoefInTableauRow)
		{
			if (IsRealVarAtHi(var))
			{
				return varCoefInTableauRow < 0;
			}
			return false;
		}

		private bool IsRealVarAtHiWithPositiveCoef(int var, Rational varCoefInTableauRow)
		{
			if (IsRealVarAtHi(var))
			{
				return varCoefInTableauRow > 0;
			}
			return false;
		}

		private static Rational GetNonNegativeFraction(Rational val)
		{
			return val - val.GetFloor();
		}

		private static VectorRational RemoveSlacksInCutRow(SimplexReducedModel mod, VectorRational cutRow)
		{
			VectorRational vectorRational = new VectorRational(cutRow.RcCount);
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(cutRow);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				Rational value = iter.Value;
				if (mod.IsSlackVar(rc))
				{
					Stack<int> stack = new Stack<int>();
					Stack<Rational> stack2 = new Stack<Rational>();
					stack.Push(rc);
					stack2.Push(value);
					while (stack.Count > 0)
					{
						int num = stack.Pop();
						Rational rational = stack2.Pop();
						int vid = mod.GetVid(num);
						int row = ((vid >= mod.UserModel._vidLim) ? SearchForRowId(mod, num) : mod.GetRow(vid));
						CoefMatrix matrix = mod.Matrix;
						Rational rational2 = 0.0 - matrix.GetCoefDouble(row, num);
						CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(matrix, row);
						while (rowIter.IsValid)
						{
							int column = rowIter.Column;
							if (column != num)
							{
								Rational rational3 = rowIter.Approx * rational / rational2;
								if (mod.IsSlackVar(column))
								{
									stack.Push(column);
									stack2.Push(rational3);
								}
								else
								{
									Rational coef = vectorRational.GetCoef(column);
									rational3 += coef;
									vectorRational.SetCoef(column, rational3);
								}
							}
							rowIter.Advance();
						}
					}
				}
				else
				{
					Rational value2 = vectorRational.GetCoef(rc) + value;
					vectorRational.SetCoef(rc, value2);
				}
				iter.Advance();
			}
			return vectorRational;
		}

		private static int SearchForRowId(SimplexReducedModel mod, int slackVar)
		{
			for (int num = mod.RowLim - 1; num >= 0; num--)
			{
				if (mod.GetSlackVarForRow(num) == slackVar)
				{
					return num;
				}
			}
			return -1;
		}

		[Conditional("DEBUG")]
		internal static void DumpUserModelCutRow(SimplexReducedModel mod, Dictionary<int, VectorDouble> mpVarCutRow, RowCut cut)
		{
		}

		[Conditional("DEBUG")]
		private void ValidateCutRow(VectorRational row, Rational lowerBound)
		{
		}
	}
}
