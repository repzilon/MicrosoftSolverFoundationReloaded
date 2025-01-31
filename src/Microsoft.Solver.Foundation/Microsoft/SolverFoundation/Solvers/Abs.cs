using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A constraint of the form result == Abs(arg)
	/// </summary>
	internal sealed class Abs : CspFunction
	{
		internal override string Name => "Abs";

		/// <summary> A constraint of the form result == Abs(arg)
		/// </summary>
		internal Abs(ConstraintSystem solver, CspSolverTerm input)
			: base(solver, input)
		{
			ExcludeSymbols(input);
			InitMaximalScales();
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.Abs(inputs[0]);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.Abs(inputs[0]);
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			int count = base.Count;
			if (count == 0)
			{
				conflict = this;
				return false;
			}
			conflict = null;
			CspSolverTerm cspSolverTerm = _inputs[0];
			int count2 = cspSolverTerm.Count;
			if (count2 == 0)
			{
				conflict = cspSolverTerm;
				return false;
			}
			if (1 == count2)
			{
				int num = Math.Abs(cspSolverTerm.First);
				return Intersect(num, num, out conflict);
			}
			if (1 == count)
			{
				int num2 = Math.Abs(First);
				if (num2 == 0)
				{
					return cspSolverTerm.Intersect(0, 0, out conflict);
				}
				return cspSolverTerm.Intersect(out conflict, -num2, num2);
			}
			bool flag = false;
			if (0 <= cspSolverTerm.First)
			{
				flag = cspSolverTerm.Intersect(base.FiniteValue, out conflict) || flag;
				if (conflict != null)
				{
					return flag;
				}
				return Intersect(cspSolverTerm.FiniteValue, out conflict) || flag;
			}
			if (First < 0)
			{
				flag = Intersect(0, ConstraintSystem.MaxFinite, out conflict);
				if (conflict != null)
				{
					return flag;
				}
				count = base.Count;
			}
			CspSolverDomain finiteValue;
			if (count2 < 1000 || count < 500)
			{
				if (count <= count2 / 2)
				{
					int[] array = new int[count * 2];
					int num3 = 0;
					finiteValue = cspSolverTerm.FiniteValue;
					foreach (int item in Forward())
					{
						if (0 < item && finiteValue.Contains(-item))
						{
							array[num3++] = -item;
						}
						if (finiteValue.Contains(item))
						{
							array[num3++] = item;
						}
					}
					Array.Sort(array, 0, num3);
					flag |= cspSolverTerm.Intersect(out conflict, CspSolverTerm.SubArray(array, num3));
					if (conflict != null)
					{
						return flag;
					}
					count2 = cspSolverTerm.Count;
				}
				int[] array2 = new int[count2];
				int num4 = 0;
				foreach (int item2 in cspSolverTerm.Forward())
				{
					array2[num4++] = Math.Abs(item2);
				}
				Array.Sort(array2, 0, count2);
				num4 = 0;
				for (int i = 1; i < count2; i++)
				{
					if (array2[num4] != array2[i])
					{
						array2[++num4] = array2[i];
					}
				}
				return Intersect(out conflict, CspSolverTerm.SubArray(array2, 1 + num4)) || flag;
			}
			finiteValue = cspSolverTerm.FiniteValue;
			int j = Math.Max(First, finiteValue.First);
			int num5 = Math.Max(-finiteValue.First, finiteValue.Last);
			if (Last < num5)
			{
				num5 = Last;
			}
			if (num5 < j)
			{
				return Intersect(j, num5, out conflict);
			}
			for (; !finiteValue.Contains(j) || !finiteValue.Contains(-j); j++)
			{
			}
			while (!finiteValue.Contains(num5) || !finiteValue.Contains(-num5))
			{
				num5--;
			}
			flag = Intersect(j, num5, out conflict);
			if (conflict == null)
			{
				flag |= cspSolverTerm.Intersect(-num5, num5, out conflict);
				if (conflict != null)
				{
					return flag;
				}
			}
			if (0 < j && Math.Max(-cspSolverTerm.First, cspSolverTerm.Last) - j < 1000)
			{
				return flag | cspSolverTerm.Exclude(1 - j, j - 1, out conflict);
			}
			return flag;
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			int integerValue = ls.GetIntegerValue(_inputs[0]);
			if (integerValue == int.MinValue)
			{
				ls.SignalOverflow(this);
				return;
			}
			int value = Math.Abs(integerValue);
			ls[this] = value;
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			ValueWithGradients integerGradients = ls.GetIntegerGradients(_inputs[0]);
			ValueWithGradients v = Gradients.Abs(integerGradients);
			ls.SetGradients(this, v);
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping,
		///           together with a suggestion of value for this subterm
		/// </summary>
		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			Random randomSource = ls.RandomSource;
			CspSolverTerm t = _inputs[0];
			int val = ((randomSource.Next(2) == 0) ? target : (-target));
			return CspSolverTerm.CreateFlipSuggestion(t, val, randomSource);
		}
	}
}
