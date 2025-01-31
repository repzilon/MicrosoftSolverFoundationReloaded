using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	internal sealed class CspBooleanVariable : CspVariable
	{
		public override bool IsBoolean => true;

		internal override bool IsTrue
		{
			get
			{
				if (1 != base.Count)
				{
					throw new InvalidOperationException(Resources.InvalidIsTrueCall + ToString());
				}
				return 1 == First;
			}
		}

		internal CspBooleanVariable(ConstraintSystem solver, TermKinds kind)
			: this(solver, kind, null)
		{
		}

		internal CspBooleanVariable(ConstraintSystem solver, TermKinds kind, object key)
			: base(solver, ConstraintSystem.DBoolean, kind, key)
		{
		}

		/// <summary> Represent.
		/// </summary>
		public override string ToString()
		{
			if (1 != base.Count)
			{
				if (1 >= base.Count)
				{
					return "Boolean{}";
				}
				return "Boolean{?}";
			}
			if (!IsTrue)
			{
				return "Boolean{F}";
			}
			return "Boolean{T}";
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			if (!ls.IsFiltered(this))
			{
				int num = ls[this];
				if (num < 0)
				{
					if (BaseValueSet.Contains(0))
					{
						ls.SetGradients(this, 0, null, -2 * num, this);
						return;
					}
				}
				else if (BaseValueSet.Contains(1))
				{
					ls.SetGradients(this, -2 * num, this, 0, null);
					return;
				}
			}
			ls.CancelGradients(this);
		}
	}
}
