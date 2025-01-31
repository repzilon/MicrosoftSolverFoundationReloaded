using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A constraint of the form result == arg^power
	/// </summary>
	internal abstract class ProductOrPower : CspFunction
	{
		/// <summary> A constraint of the form result == arg^power
		/// </summary>
		internal ProductOrPower(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, inputs)
		{
			ExcludeSymbols(inputs);
		}

		private static int IntRoot(int x, double inversePower)
		{
			return (int)Math.Round((x < 0) ? (0.0 - Math.Pow(-x, inversePower)) : Math.Pow(x, inversePower));
		}

		internal bool PropagatePower(CspSolverTerm arg, int power, int iProd, out CspSolverTerm conflict)
		{
			int count = arg.Count;
			double inversePower = 1.0 / (double)power;
			bool flag = 0 == (1 & power);
			conflict = null;
			bool flag2 = false;
			if (flag)
			{
				if (0 < iProd && First < 0)
				{
					flag2 = Intersect(0, ConstraintSystem.MaxFinite, out conflict);
				}
				else if (iProd < 0 && 0 < Last)
				{
					flag2 = Intersect(ConstraintSystem.MinFinite, 0, out conflict);
				}
				if (conflict != null)
				{
					return flag2;
				}
			}
			int num = First / iProd;
			int num2 = Last / iProd;
			if (num2 < num)
			{
				int num3 = num;
				num = num2;
				num2 = num3;
			}
			int num4 = IntRoot(num, inversePower);
			int num5 = IntRoot(num2, inversePower);
			if (flag)
			{
				flag2 |= arg.Intersect(-num5, num5, out conflict);
				if (conflict != null)
				{
					return flag2;
				}
				if (0 < num4 && arg.Count < 4 * (num4 + 50))
				{
					flag2 |= arg.Exclude(1 - num4, num4 - 1, out conflict);
					if (conflict != null)
					{
						return flag2;
					}
				}
				num2 = (int)Math.Max(Math.Pow(arg.First, power), Math.Pow(arg.Last, power));
				num = ((0 <= (arg.First ^ arg.Last)) ? ((int)Math.Min(Math.Pow(arg.First, power), Math.Pow(arg.Last, power))) : 0);
			}
			else
			{
				flag2 |= arg.Intersect(num4, num5, out conflict);
				if (conflict != null)
				{
					return flag2;
				}
				num = (int)Math.Pow(arg.First, power);
				num2 = (int)Math.Pow(arg.Last, power);
			}
			if (iProd < 0)
			{
				int num6 = num;
				num = num2;
				num2 = num6;
			}
			flag2 = Intersect(num * iProd, num2 * iProd, out conflict) || flag2;
			if (conflict != null)
			{
				return flag2;
			}
			count = arg.Count;
			if (count < 1000)
			{
				int[] array = new int[count];
				int num7 = 0;
				int[] array2 = new int[count];
				int num8 = 0;
				foreach (int item in arg.Forward())
				{
					int num9 = (int)Math.Pow(item, power) * iProd;
					if (Contains(num9))
					{
						array[num7++] = num9;
						array2[num8++] = item;
					}
				}
				if (num8 == 0)
				{
					return arg.Intersect(ConstraintSystem.DEmpty, out conflict) || flag2;
				}
				flag2 |= arg.Intersect(out conflict, CspSolverTerm.SubArray(array2, num8));
				if (conflict != null)
				{
					return flag2;
				}
				Array.Sort(array, 0, num7);
				int num10 = 0;
				for (int i = 1; i < num7; i++)
				{
					if (array[num10] != array[i])
					{
						array[++num10] = array[i];
					}
				}
				flag2 |= Intersect(out conflict, CspSolverTerm.SubArray(array, 1 + num10)) || flag2;
			}
			return flag2;
		}
	}
}
