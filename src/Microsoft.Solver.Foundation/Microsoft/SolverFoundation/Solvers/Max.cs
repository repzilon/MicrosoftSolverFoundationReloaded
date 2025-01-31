using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A constraint of the form result = max (arglist)
	/// </summary>
	internal sealed class Max : CspFunction
	{
		internal override string Name => "Max";

		/// <summary> An integer equals the maximum of a sequence of integers.
		/// </summary>
		internal Max(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, inputs)
		{
			ExcludeSymbols(inputs);
			InitMaximalScales();
		}

		public override void Accept(IVisitor visitor)
		{
			throw new NotImplementedException();
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.Max(inputs);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.Max(inputs);
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			bool flag = false;
			int first = First;
			int last = Last;
			int num = 0;
			int num2 = -1;
			int num3 = first;
			for (int i = 0; i < _inputs.Length; i++)
			{
				int first2 = _inputs[i].First;
				if (first2 > num3)
				{
					num3 = first2;
				}
			}
			int num4 = -1073741823;
			int num5 = -1073741823;
			for (int j = 0; j < _inputs.Length; j++)
			{
				CspSolverTerm cspSolverTerm = _inputs[j];
				flag |= cspSolverTerm.Intersect(-1073741823, last, out conflict);
				if (conflict != null)
				{
					return flag;
				}
				int first3 = cspSolverTerm.First;
				int last2 = cspSolverTerm.Last;
				if (last2 >= num3)
				{
					num++;
					num2 = j;
					if (first3 > num4)
					{
						num4 = first3;
					}
					if (last2 > num5)
					{
						num5 = last2;
					}
				}
			}
			switch (num)
			{
			case 0:
				conflict = this;
				return true;
			case 1:
			{
				CspSolverTerm cspSolverTerm2 = _inputs[num2];
				flag |= Intersect(cspSolverTerm2.FiniteValue, out conflict);
				if (conflict != null)
				{
					return flag;
				}
				flag |= cspSolverTerm2.Intersect(base.FiniteValue, out conflict);
				if (conflict != null)
				{
					return flag;
				}
				break;
			}
			default:
				flag |= Intersect(num4, num5, out conflict);
				if (conflict != null)
				{
					return flag;
				}
				break;
			}
			conflict = null;
			return flag;
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			ls[this] = RecomputeMaxValue(ls);
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
			int num = ls[this];
			if (newValue > num)
			{
				num = newValue;
			}
			else if (oldValue == num)
			{
				num = RecomputeMaxValue(ls);
			}
			ls[this] = num;
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			ValueWithGradients valueWithGradients = new ValueWithGradients(-1073741823);
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm term in inputs)
			{
				ValueWithGradients integerGradients = ls.GetIntegerGradients(term);
				valueWithGradients = Gradients.Max(valueWithGradients, integerGradients);
			}
			ls.SetGradients(this, valueWithGradients);
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping,
		///           together with a suggestion of value for this subterm
		/// </summary>
		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			int num = ls[this];
			Random randomSource = ls.RandomSource;
			if (target > num)
			{
				foreach (CspSolverTerm item in RandomlyEnumerateSubterms(randomSource))
				{
					if (item.Last > num)
					{
						return CspSolverTerm.CreateFlipSuggestion(item, target, randomSource);
					}
				}
			}
			else if (target < num)
			{
				foreach (CspSolverTerm item2 in RandomlyEnumerateSubterms(randomSource))
				{
					if (item2.BaseValueSet.Count > 1 && ls.GetIntegerValue(item2) == num)
					{
						return CspSolverTerm.CreateFlipSuggestion(item2, target, randomSource);
					}
				}
			}
			return CspSolverTerm.CreateFlipSuggestion(_inputs[randomSource.Next(_inputs.Length)], 0, randomSource);
		}

		private int RecomputeMaxValue(LocalSearchSolver ls)
		{
			int num = CspSolverTerm.MinSafeNegativeValue;
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm expr in inputs)
			{
				int integerValue = ls.GetIntegerValue(expr);
				if (integerValue > num)
				{
					num = integerValue;
				}
			}
			return num;
		}
	}
}
