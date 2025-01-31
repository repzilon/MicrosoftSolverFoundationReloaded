using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A boolean which is equal to the mutual inequality of all the input expressions.
	/// </summary>
	internal sealed class Equal : Comparison
	{
		private int _unresolved;

		private CspSolverDomain _shared;

		/// <summary> Represent this class
		/// </summary>
		internal override string Name => "Equal";

		/// <summary> A boolean which is equal to the mutual inequality of all the input expressions.
		/// </summary>
		internal Equal(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, inputs)
		{
			Reset();
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		internal override void Reset()
		{
			_unresolved = Width;
			_shared = ConstraintSystem.DFinite;
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			if (base.Count == 0)
			{
				conflict = this;
				return false;
			}
			conflict = null;
			if (_unresolved == 0)
			{
				return false;
			}
			bool flag = true;
			bool flag2 = false;
			if (1 == base.Count)
			{
				flag = false;
				flag2 = !IsTrue;
			}
			bool flag3 = false;
			bool flag4;
			do
			{
				flag4 = false;
				int num = 0;
				while (num < _unresolved)
				{
					CspSolverTerm cspSolverTerm = _inputs[num];
					if (cspSolverTerm.Count == 0)
					{
						conflict = cspSolverTerm;
						return false;
					}
					if (!flag2 && !flag && cspSolverTerm.Intersect(ScaleToInput(_shared, num), out conflict))
					{
						flag3 = true;
						if (conflict != null)
						{
							return flag3;
						}
					}
					if (_shared.Intersect(ScaleToOutput(cspSolverTerm.FiniteValue, num), out _shared))
					{
						flag3 = true;
						if (_shared.Count == 0)
						{
							_unresolved = 0;
							if (flag2)
							{
								conflict = null;
								return false;
							}
							return Force(choice: false, out conflict);
						}
						flag4 = !flag2 && !flag;
					}
					if (1 == cspSolverTerm.Count)
					{
						_unresolved--;
						if (num < _unresolved)
						{
							_inputs[num] = _inputs[_unresolved];
							_inputs[_unresolved] = cspSolverTerm;
							Statics.Swap(ref _scales[num], ref _scales[_unresolved]);
						}
						if (_unresolved == 0)
						{
							return Force(choice: true, out conflict);
						}
					}
					else
					{
						num++;
					}
				}
			}
			while (flag4);
			if (conflict == null && !flag)
			{
				if (flag2)
				{
					if (_inputs.Length == 2 && _unresolved == 1)
					{
						flag3 |= _inputs[0].Exclude(ScaleToInput(ScaleToOutput(_inputs[1].FiniteValue, 1), 0), out conflict);
					}
				}
				else if (_shared.Count == 1)
				{
					int num2 = 0;
					while (conflict == null && num2 < _unresolved)
					{
						flag3 |= _inputs[num2].Intersect(ScaleToInput(_shared, num2), out conflict);
						num2++;
					}
				}
			}
			return flag3;
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.Equal(inputs);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.Equal(inputs);
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			int num = _inputs.Length;
			int num2;
			if (num < 2)
			{
				num2 = BooleanFunction.Satisfied;
			}
			else
			{
				num2 = int.MinValue;
				for (int i = 0; i < num; i++)
				{
					int integerValue = ls.GetIntegerValue(_inputs[i]);
					for (int j = i + 1; j < num; j++)
					{
						int integerValue2 = ls.GetIntegerValue(_inputs[j]);
						num2 = BooleanFunction.And(num2, BooleanFunction.Equal(integerValue, integerValue2));
					}
				}
			}
			ls[this] = num2;
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			int num = _inputs.Length;
			if (num < 2)
			{
				ls.CancelGradients(this);
				return;
			}
			ValueWithGradients valueWithGradients = new ValueWithGradients(int.MinValue);
			for (int i = 0; i < num; i++)
			{
				ValueWithGradients integerGradients = ls.GetIntegerGradients(_inputs[i]);
				for (int j = i + 1; j < num; j++)
				{
					ValueWithGradients integerGradients2 = ls.GetIntegerGradients(_inputs[j]);
					valueWithGradients = Gradients.And(valueWithGradients, Gradients.Equal(integerGradients, integerGradients2));
				}
			}
			ls.SetGradients(this, valueWithGradients);
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping,
		///           together with a suggestion of value for this subterm
		/// </summary>
		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			if (target == 1)
			{
				return SelectSubtermToFlipTowardsEquality(ls);
			}
			return SelectSubtermToFlipTowardsDifference(ls);
		}
	}
}
