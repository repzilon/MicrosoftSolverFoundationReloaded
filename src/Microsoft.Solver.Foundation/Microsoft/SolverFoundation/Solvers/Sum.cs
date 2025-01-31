using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A constraint of the form resultV == i0 + i1 + i2 + ... where the iN are integers.
	/// </summary>
	internal sealed class Sum : CspFunction
	{
		internal override string Name => "Sum";

		/// <summary> An integer equals the sum of a sequence of integers.
		/// </summary>
		internal Sum(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, inputs)
		{
			ExcludeSymbols(inputs);
			InitMaximalScales();
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.Sum(inputs);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.Sum(inputs);
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			int width = Width;
			if (base.Count == 0)
			{
				conflict = this;
				return false;
			}
			long num = 0L;
			long num2 = 0L;
			long num3 = 0L;
			int num4 = 0;
			CspSolverTerm cspSolverTerm = null;
			CspSolverTerm cspSolverTerm2 = null;
			int num5 = -1;
			int id = -1;
			if (1 == base.Count)
			{
				num = -First;
			}
			else
			{
				num4++;
				cspSolverTerm = this;
				num5 = base.OutputScaleId;
				num2 = -Last;
				num3 = -First;
			}
			for (int i = 0; i < width; i++)
			{
				CspSolverTerm cspSolverTerm3 = _inputs[i];
				int count = cspSolverTerm3.Count;
				if (count == 0)
				{
					conflict = cspSolverTerm3;
					return false;
				}
				if (1 == count)
				{
					num += ScaleToOutput(cspSolverTerm3.First, i);
					continue;
				}
				num4++;
				cspSolverTerm2 = cspSolverTerm;
				id = num5;
				cspSolverTerm = cspSolverTerm3;
				num5 = i;
				num2 += ScaleToOutput(cspSolverTerm3.First, i);
				num3 += ScaleToOutput(cspSolverTerm3.Last, i);
			}
			num2 += num;
			num3 += num;
			if (num3 < 0 || 0 < num2)
			{
				return Intersect(ConstraintSystem.DEmpty, out conflict);
			}
			conflict = null;
			bool flag = false;
			if (1 == num4)
			{
				if (num < ConstraintSystem.MinFinite || ConstraintSystem.MaxFinite < num)
				{
					return cspSolverTerm.Intersect(ConstraintSystem.DEmpty, out conflict);
				}
				int num6 = (int)num;
				if (this == cspSolverTerm)
				{
					return Intersect(num6, num6, out conflict);
				}
				return cspSolverTerm.Intersect(ScaleToInput(-num6, num5), ScaleToInput(-num6, num5), out conflict);
			}
			if (2 == num4)
			{
				long num7 = Math.Abs(num);
				long num8 = Math.Min(ScaleToOutput(cspSolverTerm.First, num5), ScaleToOutput(cspSolverTerm2.First, id));
				long num9 = Math.Max(ScaleToOutput(cspSolverTerm.Last, num5), ScaleToOutput(cspSolverTerm2.Last, id));
				if (num7 + num9 <= int.MaxValue && int.MinValue <= num8 - num7)
				{
					int num10 = (int)num;
					if (this == cspSolverTerm2)
					{
						if (0 == num)
						{
							flag = cspSolverTerm.Intersect(ScaleToInput(base.FiniteValue, num5), out conflict);
							if (conflict == null)
							{
								flag |= Intersect(ScaleToOutput(cspSolverTerm.FiniteValue, num5), out conflict);
							}
							return flag;
						}
						flag = cspSolverTerm.Intersect(ScaleToInput(First - num10, num5), ScaleToInput(Last - num10, num5), out conflict);
						if (conflict == null)
						{
							flag |= Intersect(ScaleToOutput(cspSolverTerm.First, num5) + num10, ScaleToOutput(cspSolverTerm.Last, num5) + num10, out conflict);
						}
						if (conflict != null || (cspSolverTerm.Count == base.Count && base.FiniteValue is CspIntervalDomain))
						{
							return flag;
						}
						if (base.Count < 1000 && base.FiniteValue is CspSetDomain)
						{
							int[] array = new int[base.Count];
							int num11 = 0;
							foreach (int item in Forward())
							{
								array[num11++] = item - num10;
							}
							flag |= cspSolverTerm.Intersect(out conflict, ScaleToInput(array, num5));
							if (conflict != null || base.Count == cspSolverTerm.Count)
							{
								return flag;
							}
						}
						if (cspSolverTerm.Count < 1000 && cspSolverTerm.FiniteValue is CspSetDomain)
						{
							int[] array2 = new int[cspSolverTerm.Count];
							int num12 = 0;
							foreach (int item2 in cspSolverTerm.Forward())
							{
								array2[num12++] = ScaleToOutput(item2, num5) + num10;
							}
							return Intersect(out conflict, array2) || flag;
						}
					}
					else
					{
						flag = cspSolverTerm.Intersect(ScaleToInput(ScaleToOutput(-cspSolverTerm2.Last, id) - num10, num5), ScaleToInput(ScaleToOutput(-cspSolverTerm2.First, id) - num10, num5), out conflict);
						if (conflict == null)
						{
							flag |= cspSolverTerm2.Intersect(ScaleToInput(ScaleToOutput(-cspSolverTerm.Last, num5) - num10, id), ScaleToInput(ScaleToOutput(-cspSolverTerm.First, num5) - num10, id), out conflict);
						}
						if (conflict != null || (cspSolverTerm.Count == cspSolverTerm2.Count && cspSolverTerm.FiniteValue is CspIntervalDomain))
						{
							return flag;
						}
						if (cspSolverTerm.Count < 1000 && cspSolverTerm.FiniteValue is CspSetDomain)
						{
							int[] array3 = new int[cspSolverTerm.Count];
							int num13 = 0;
							foreach (int item3 in cspSolverTerm.Backward())
							{
								array3[num13++] = ScaleToOutput(-item3, num5) - num10;
							}
							flag |= cspSolverTerm2.Intersect(out conflict, ScaleToInput(array3, id));
							if (conflict != null || cspSolverTerm.Count == cspSolverTerm2.Count)
							{
								return flag;
							}
						}
						if (cspSolverTerm2.Count < 1000 && cspSolverTerm2.FiniteValue is CspSetDomain)
						{
							int[] array4 = new int[cspSolverTerm2.Count];
							int num14 = 0;
							foreach (int item4 in cspSolverTerm2.Backward())
							{
								array4[num14++] = ScaleToOutput(-item4, id) - num10;
							}
							return cspSolverTerm.Intersect(out conflict, ScaleToInput(array4, num5)) || flag;
						}
					}
					return flag;
				}
			}
			for (int j = 0; j < width; j++)
			{
				CspSolverTerm cspSolverTerm4 = _inputs[j];
				if (1 >= cspSolverTerm4.Count)
				{
					continue;
				}
				long num15 = ScaleToOutput(cspSolverTerm4.First, j);
				long num16 = ScaleToOutput(cspSolverTerm4.Last, j);
				long num17 = num16 - num3;
				long num18 = num15 - num2;
				if (num15 < num17 || num18 < num16)
				{
					flag |= cspSolverTerm4.Intersect(ScaleToInput((int)Math.Max(num17, num15), j), ScaleToInput((int)Math.Min(num18, num16), j), out conflict);
					if (conflict != null)
					{
						return flag;
					}
					if (cspSolverTerm4.Count == 1)
					{
						num += ScaleToOutput(cspSolverTerm4.FiniteValue.First, j);
						num4--;
					}
				}
			}
			int count2 = base.Count;
			if (1 < base.Count)
			{
				long num19 = num3 + First;
				long num20 = num2 + Last;
				if (First < num20 || num19 < Last)
				{
					flag |= Intersect((int)Math.Max(num20, First), (int)Math.Min(num19, Last), out conflict);
				}
			}
			if (1 == base.Count && num4 == 0)
			{
				if (count2 == 1)
				{
					if (num != 0)
					{
						conflict = this;
						return flag;
					}
				}
				else if (num != base.FiniteValue.First)
				{
					conflict = this;
					return flag;
				}
			}
			return flag;
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			double num = 0.0;
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm expr in inputs)
			{
				num += (double)ls.GetIntegerValue(expr);
			}
			if (!CspSolverTerm.IsSafe(num))
			{
				ls.SignalOverflow(this);
			}
			else
			{
				ls[this] = (int)num;
			}
		}

		/// <summary> Incremental recomputation: update the value of the term 
		///           when one of its arguments is changed
		/// </summary>
		internal override void PropagateChange(LocalSearchSolver ls, CspSolverTerm modifiedArg, int oldValue, int newValue)
		{
			if (modifiedArg.IsBoolean)
			{
				oldValue = LocalSearchSolver.ViolationToZeroOne(oldValue);
				newValue = LocalSearchSolver.ViolationToZeroOne(newValue);
			}
			double num = ls[this];
			double num2 = newValue - oldValue;
			double num3 = num + num2;
			if (CspSolverTerm.IsSafe(num3))
			{
				ls[this] = (int)num3;
			}
			else
			{
				ls.SignalOverflow(this);
			}
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			ValueWithGradients valueWithGradients = 0;
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm term in inputs)
			{
				ValueWithGradients integerGradients = ls.GetIntegerGradients(term);
				valueWithGradients = Gradients.Sum(valueWithGradients, integerGradients);
			}
			ls.SetGradients(this, valueWithGradients);
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping,
		///           together with a suggestion of value for this subterm
		/// </summary>
		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			Random randomSource = ls.RandomSource;
			CspSolverTerm cspSolverTerm = PickInput(randomSource);
			int num = ls[this] - ls.GetIntegerValue(cspSolverTerm);
			int val = target - num;
			return CspSolverTerm.CreateFlipSuggestion(cspSolverTerm, val, randomSource);
		}
	}
}
