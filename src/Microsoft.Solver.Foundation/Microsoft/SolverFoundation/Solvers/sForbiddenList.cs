using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A combination of variables with particular choices which are forbidden.
	///           This is designed to be used for Learned Clauses.
	/// </summary>
	internal sealed class sForbiddenList : BooleanFunction
	{
		private int[] _rgForbidden;

		internal override string Name => "sForbiddenList";

		private sForbiddenList(ConstraintSystem solver, CspSolverTerm[] inputs, int[] forbiddens)
			: base(solver, inputs)
		{
			InitMaximalScales();
			_rgForbidden = forbiddens;
			Restrain(ConstraintSystem.DTrue);
		}

		public override void Accept(IVisitor visitor)
		{
		}

		/// <summary> A list of variables and choices, which are a forbidden combination.
		///           This is designed to be used for Learned Clauses.
		/// </summary>
		internal static sForbiddenList Create(ConstraintSystem solver, CspSolverTerm[] inputs, int[] rgForbidden, int first, int N)
		{
			CspSolverTerm[] array = new CspSolverTerm[N];
			int[] array2 = new int[N];
			for (int i = 0; i < N; i++)
			{
				array[i] = inputs[first + i];
				array2[i] = rgForbidden[first + i];
			}
			return new sForbiddenList(solver, array, array2);
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			throw new InvalidOperationException(Resources.CompositeConstraintNotSupported + ToString());
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			throw new InvalidOperationException(Resources.CompositeConstraintNotSupported + ToString());
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			CspSolverTerm cspSolverTerm = null;
			int num = 0;
			int num2 = 0;
			conflict = null;
			for (int i = 0; i < Width; i++)
			{
				CspSolverTerm cspSolverTerm2 = _inputs[i];
				if (cspSolverTerm2.Count == 0)
				{
					conflict = cspSolverTerm2;
					return false;
				}
				if (1 == cspSolverTerm2.Count)
				{
					if (cspSolverTerm2.First != _rgForbidden[i])
					{
						return false;
					}
					continue;
				}
				if (!cspSolverTerm2.Contains(_rgForbidden[i]))
				{
					return false;
				}
				num2++;
				if (cspSolverTerm == null)
				{
					cspSolverTerm = cspSolverTerm2;
					num = _rgForbidden[i];
				}
			}
			if (num2 == 0)
			{
				return Force(choice: false, out conflict);
			}
			if (1 == num2)
			{
				if (cspSolverTerm.IsBoolean)
				{
					return cspSolverTerm.Force(0 == num, out conflict);
				}
				return cspSolverTerm.Exclude(num, num, out conflict);
			}
			return false;
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
		}
	}
}
