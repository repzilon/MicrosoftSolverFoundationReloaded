using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> An integer equals the product of a sequence of integers.
	/// </summary>
	internal sealed class Product : ProductOrPower
	{
		internal override string Name => "Product";

		/// <summary> An integer equals the product of a sequence of integers.
		/// </summary>
		internal Product(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, inputs)
		{
			InitProductScales();
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.Product(inputs);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.Product(inputs);
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			int num = base.Count;
			if (num == 0)
			{
				conflict = this;
				return false;
			}
			conflict = null;
			bool flag = false;
			int width = Width;
			if (width < 1)
			{
				return Intersect(ConstraintSystem.DEmpty, out conflict);
			}
			if (1 == width)
			{
				CspSolverTerm cspSolverTerm = _inputs[0];
				if (cspSolverTerm.Count == 0)
				{
					conflict = cspSolverTerm;
					return false;
				}
				return RestrictProportion(cspSolverTerm, 1, out conflict);
			}
			int num2 = Math.Max(-First, Last);
			CspSolverTerm cspSolverTerm2 = null;
			CspSolverTerm cspSolverTerm3 = null;
			CspSolverTerm cspSolverTerm4 = null;
			CspSolverTerm cspSolverTerm5 = null;
			bool flag2 = false;
			long num3 = 1L;
			int num4 = (Contains(0) ? (-1) : 0);
			for (int i = 0; i < width; i++)
			{
				CspSolverTerm cspSolverTerm6 = _inputs[i];
				if (cspSolverTerm6.Count == 0)
				{
					conflict = cspSolverTerm6;
					return false;
				}
				if (1 == cspSolverTerm6.Count)
				{
					num3 *= cspSolverTerm6.First;
					if (num3 < -num2 || num2 < num3)
					{
						flag |= Intersect(0, 0, out conflict);
						if (conflict != null)
						{
							return flag;
						}
						num = 1;
						num3 = 1L;
					}
					else if (0 == num3)
					{
						return Intersect(0, 0, out conflict) || flag;
					}
					if (cspSolverTerm4 == null)
					{
						cspSolverTerm4 = cspSolverTerm6;
					}
					else if (cspSolverTerm5 == null)
					{
						cspSolverTerm5 = cspSolverTerm6;
					}
				}
				else
				{
					if (cspSolverTerm2 == null)
					{
						cspSolverTerm2 = cspSolverTerm6;
					}
					else if (cspSolverTerm3 == null)
					{
						cspSolverTerm3 = cspSolverTerm6;
					}
					else
					{
						flag2 = true;
					}
					if (0 <= num4)
					{
						num4 = Math.Max(num4, Math.Max(-cspSolverTerm6.First, cspSolverTerm6.Last));
					}
				}
			}
			int num5 = (int)num3;
			if (!flag2)
			{
				if (cspSolverTerm3 == null)
				{
					if (cspSolverTerm2 == null)
					{
						if (1 == num && num5 == First)
						{
							conflict = null;
							return flag;
						}
						return Intersect(num5, num5, out conflict) || flag;
					}
					cspSolverTerm3 = cspSolverTerm4;
				}
				int count = cspSolverTerm3.Count;
				if (1 == num && 1 == count)
				{
					int num6 = First / num5;
					flag = ((First != num6 * num5) ? (flag | cspSolverTerm2.Intersect(ConstraintSystem.DEmpty, out conflict)) : (flag | cspSolverTerm2.Intersect(num6, num6, out conflict)));
				}
				else if (cspSolverTerm2 == cspSolverTerm3)
				{
					flag |= PropagatePower(cspSolverTerm2, 2, num5, out conflict);
				}
				else if (1 == count)
				{
					flag |= RestrictProportion(cspSolverTerm2, num5, out conflict);
				}
				else
				{
					flag |= RestrictProduct(cspSolverTerm2, cspSolverTerm3, num5, out conflict);
					if (Contains(0) && (cspSolverTerm2.Contains(0) || cspSolverTerm3.Contains(0)))
					{
						conflict = null;
						return flag;
					}
					if (conflict == null)
					{
						flag |= BoundBy1stOver2nd(cspSolverTerm2, cspSolverTerm3, num5, out conflict);
					}
					if (conflict == null)
					{
						flag |= BoundBy1stOver2nd(cspSolverTerm3, cspSolverTerm2, num5, out conflict);
					}
					if (conflict == null && base.Count < 10 && !flag)
					{
						flag |= RestrictMultiples(num5, out conflict);
						if (conflict == null)
						{
							flag |= RestrictFactors(cspSolverTerm2, cspSolverTerm3, num5, out conflict);
						}
					}
				}
			}
			if (!flag && num2 / Math.Abs(num5) < num4)
			{
				num4 = num2 / Math.Abs(num5);
				for (int j = 0; j < width; j++)
				{
					CspSolverTerm cspSolverTerm7 = _inputs[j];
					if (1 < cspSolverTerm7.Count)
					{
						flag |= cspSolverTerm7.Intersect(-num4, num4, out conflict);
						if (conflict != null)
						{
							return flag;
						}
					}
				}
			}
			return flag;
		}

		internal static int[] GetMultipliers(int zCdiv, int iProd, CspSolverDomain domain, out int qIx)
		{
			int[] array = new int[zCdiv];
			qIx = 0;
			if (domain is CspIntervalDomain)
			{
				int num = domain.First % iProd;
				int num2 = ((num > 0) ? (domain.First + (iProd - num)) : (domain.First - num));
				int num3 = ((iProd > 0) ? iProd : (-iProd));
				for (int i = num2; i <= domain.Last; i += num3)
				{
					array[qIx++] = i / iProd;
				}
			}
			else
			{
				foreach (int item in domain.Forward())
				{
					if (item % iProd == 0)
					{
						array[qIx++] = item / iProd;
					}
				}
			}
			return array;
		}

		/// <summary> this == v * iProd
		/// </summary>
		private bool RestrictProportion(CspSolverTerm v, int iProd, out CspSolverTerm conflict)
		{
			bool flag = false;
			conflict = null;
			if (1 == iProd)
			{
				flag = Intersect(v.FiniteValue, out conflict);
				if (conflict == null)
				{
					flag |= v.Intersect(base.FiniteValue, out conflict);
				}
				return flag;
			}
			int count = v.Count;
			int num = Math.Min(base.Count, (Last - First) / Math.Abs(iProd) + 1);
			if (1000 < Math.Min(count, num))
			{
				long val;
				long val2;
				if (0 < iProd)
				{
					val = (long)v.First * (long)iProd;
					val2 = (long)v.Last * (long)iProd;
				}
				else
				{
					val2 = (long)v.First * (long)iProd;
					val = (long)v.Last * (long)iProd;
				}
				flag |= Intersect((int)Math.Max(ConstraintSystem.MinFinite, val), (int)Math.Min(ConstraintSystem.MaxFinite, val2), out conflict);
				if (conflict != null)
				{
					return flag;
				}
				int min;
				int max;
				if (0 < iProd)
				{
					min = First / iProd;
					max = Last / iProd;
				}
				else
				{
					max = First / iProd;
					min = Last / iProd;
				}
				return v.Intersect(min, max, out conflict) || flag;
			}
			int num2 = 0;
			bool flag2 = true;
			bool flag3 = true;
			do
			{
				if (count < 1000 && flag3 && conflict == null)
				{
					int num3 = 0;
					int num4 = First / iProd;
					int num5 = Last / iProd;
					if (num5 < num4)
					{
						int num6 = num5;
						num5 = num4;
						num4 = num6;
					}
					num4 = Math.Max(v.First, num4);
					num5 = Math.Min(v.Last, num5);
					int[] array = new int[count];
					foreach (int item in v.Forward(num4, num5))
					{
						array[num3++] = iProd * item;
					}
					if (iProd < 0)
					{
						Array.Reverse(array, 0, num3);
					}
					flag2 = Intersect(out conflict, CspSolverTerm.SubArray(array, num3));
					flag = flag || flag2;
					if (flag2 && conflict == null)
					{
						num = Math.Min(base.Count, (Last - First) / Math.Abs(iProd) + 1);
					}
				}
				if (num < 1000 && (num2 == 0 || flag2) && conflict == null)
				{
					int qIx;
					int[] multipliers = GetMultipliers(num, iProd, base.FiniteValue, out qIx);
					if (iProd < 0)
					{
						Array.Reverse(multipliers, 0, qIx);
					}
					flag3 = v.Intersect(out conflict, CspSolverTerm.SubArray(multipliers, qIx));
					flag = flag || flag3;
					if (flag3 && conflict == null)
					{
						count = v.Count;
					}
				}
			}
			while ((flag2 || flag3) && conflict != null && ++num2 < 3);
			return flag;
		}

		/// <summary> Restrict the bounds of z to the limits of (v * w * iProd)
		/// </summary>
		private bool RestrictProduct(CspSolverTerm v, CspSolverTerm w, int iProd, out CspSolverTerm conflict)
		{
			long num = v.First;
			long num2 = v.Last;
			long num3 = w.First;
			long num4 = w.Last;
			long val = num * num3;
			long val2 = num * num4;
			long val3 = num2 * num3;
			long val4 = num2 * num4;
			long num5 = iProd * Math.Min(Math.Min(val, val2), Math.Min(val3, val4));
			long num6 = iProd * Math.Max(Math.Max(val, val2), Math.Max(val3, val4));
			if (iProd < 0)
			{
				long num7 = num5;
				num5 = num6;
				num6 = num7;
			}
			return Intersect((int)Math.Max(ConstraintSystem.MinFinite, num5), (int)Math.Min(ConstraintSystem.MaxFinite, num6), out conflict);
		}

		/// <summary> Restrict the bounds of v to the limits of (z / (w * iProd))
		/// </summary>
		private bool BoundBy1stOver2nd(CspSolverTerm v, CspSolverTerm w, int iProd, out CspSolverTerm conflict)
		{
			int num = w.First;
			int num2 = w.Last;
			int num3 = First / iProd;
			int num4 = Last / iProd;
			if (num4 < num3)
			{
				int num5 = num3;
				num3 = num4;
				num4 = num5;
			}
			if (num == 0 && num2 == 0)
			{
				return w.Intersect(ConstraintSystem.DEmpty, out conflict);
			}
			if (0 <= num)
			{
				if (num == 0)
				{
					num = w.FiniteValue.Succ(0);
				}
				int num6 = Math.Min(num3 / num, num3 / num2);
				num4 = Math.Max(num4 / num, num4 / num2);
				num3 = num6;
			}
			else if (num2 <= 0)
			{
				if (num2 == 0)
				{
					num2 = w.FiniteValue.Pred(0);
				}
				int num7 = Math.Min(num4 / num, num4 / num2);
				num4 = Math.Max(num3 / num, num3 / num2);
				num3 = num7;
			}
			else
			{
				if (w.Contains(0) && Contains(0))
				{
					conflict = null;
					return false;
				}
				int num8 = w.FiniteValue.Succ(0);
				int num9 = w.FiniteValue.Pred(0);
				int num10 = Math.Min(num3 / num8, num4 / num9);
				num4 = Math.Max(num4 / num8, num3 / num9);
				num3 = num10;
			}
			return v.Intersect(num3, num4, out conflict);
		}

		/// <summary> Restrict Z to be multiples of iProd
		/// </summary>
		private bool RestrictMultiples(int iProd, out CspSolverTerm conflict)
		{
			if (-1 == iProd || 1 == iProd)
			{
				conflict = null;
				return false;
			}
			int[] array = new int[base.Count];
			int num = 0;
			if (base.Count <= 1)
			{
				if (First % iProd == 0)
				{
					array[num++] = First;
				}
			}
			else
			{
				int num2 = First - 1;
				int num3 = Math.Abs(iProd) - 1;
				while (num2 < Last)
				{
					num2 = base.FiniteValue.Succ(num2);
					if (num2 % iProd == 0)
					{
						array[num++] = num2;
						num2 += num3;
					}
				}
			}
			if (base.Count == num)
			{
				conflict = null;
				return false;
			}
			return Intersect(out conflict, CspSolverTerm.SubArray(array, num));
		}

		/// <summary> Add the positive factors of n to the factors. The generated factors will not contain duplicates.
		///           But factors list may have contained other factors, so we don't guarantee that the whole factors list is duplicate-free.
		/// </summary>
		private static void AddFactors(List<int> factors, int n)
		{
			if (-1 <= n && n <= 1)
			{
				factors.Add(n);
				return;
			}
			int num = (int)Math.Sqrt(Math.Abs(n));
			int num2 = 1;
			int num3 = 2;
			if ((1 & n) != 0)
			{
				num2 = 2;
				num3 = 3;
			}
			factors.Add(1);
			factors.Add(n);
			for (int i = num3; i <= num; i += num2)
			{
				if (n % i == 0)
				{
					factors.Add(i);
					factors.Add(n / i);
				}
			}
			if (n == num * num)
			{
				factors.RemoveAt(factors.Count - 1);
			}
		}

		/// <summary> Restrict v and w to being possible factors of z/iProd.
		/// </summary>
		private bool RestrictFactors(CspSolverTerm v, CspSolverTerm w, int iProd, out CspSolverTerm conflict)
		{
			conflict = null;
			if (Contains(0))
			{
				conflict = null;
				return false;
			}
			List<int> list = new List<int>();
			list.Add(1);
			int num = First - 1;
			int num2 = Math.Abs(iProd) - 1;
			while (num < Last)
			{
				num = base.FiniteValue.Succ(num);
				if (num % iProd == 0)
				{
					int num3 = num / iProd;
					if (num3 != 0)
					{
						AddFactors(list, Math.Abs(num3));
					}
					num += num2;
				}
			}
			list.Sort();
			int num4 = 0;
			for (int i = 1; i < list.Count; i++)
			{
				if (list[num4] < list[i])
				{
					num4++;
					if (num4 < i)
					{
						list[num4] = list[i];
					}
				}
			}
			int[] array = new int[2 * (num4 + 1)];
			int j = 0;
			int num5 = num4;
			int num6 = num5 + 1;
			for (; j <= num4; j++)
			{
				array[num6++] = list[j];
				array[num5--] = -list[j];
			}
			bool flag = v.Intersect(out conflict, array);
			if (conflict == null)
			{
				flag |= w.Intersect(out conflict, array);
			}
			return flag;
		}

		/// <summary> Recompute the value of the term from the value of  its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			double num = 1.0;
			bool flag = false;
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm expr in inputs)
			{
				int integerValue = ls.GetIntegerValue(expr);
				if (integerValue == 0)
				{
					num = 0.0;
					flag = false;
					break;
				}
				num *= (double)integerValue;
				if (!CspSolverTerm.IsSafe(num))
				{
					flag = true;
				}
			}
			if (flag)
			{
				ls.SignalOverflow(this);
			}
			else
			{
				ls[this] = (int)num;
			}
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			ValueWithGradients valueWithGradients = 1;
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm term in inputs)
			{
				ValueWithGradients integerGradients = ls.GetIntegerGradients(term);
				valueWithGradients = Gradients.Product(valueWithGradients, integerGradients);
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
			int num = ls[cspSolverTerm];
			int num2 = ((num == 0) ? 1 : (ls.GetIntegerValue(this) / num));
			int val = ((num2 != 0) ? (target / num2) : 0);
			return CspSolverTerm.CreateFlipSuggestion(cspSolverTerm, val, randomSource);
		}
	}
}
