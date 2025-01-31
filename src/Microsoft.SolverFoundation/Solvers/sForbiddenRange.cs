using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A combination of variables and choices, which are a forbidden combination.
	///           This is designed to be used for Learned Clauses.  All but the final
	///           variable have just one value to avoid, and the final one may be a otherSet
	///           of avoidances.
	/// </summary>
	internal sealed class sForbiddenRange : BooleanFunction
	{
		private int[] _forbiddens;

		private CspSolverDomain _finalSet;

		internal override string Name => "sForbiddenRange";

		public override void Accept(IVisitor visitor)
		{
		}

		private sForbiddenRange(ConstraintSystem solver, CspSolverTerm[] inputs, int[] forbiddens, CspSolverDomain finals)
			: base(solver, inputs)
		{
			InitMaximalScales();
			_forbiddens = forbiddens;
			_finalSet = finals;
			Restrain(ConstraintSystem.DTrue);
		}

		/// <summary> A combination of variables and choices, which are a forbidden combination.
		///           This is designed to be used for Learned Clauses.  All but the final
		///           variable have just one value to avoid, and the final one may be a otherSet
		///           of avoidances.
		/// </summary>
		internal static sForbiddenRange Create(ConstraintSystem solver, CspSolverTerm[] inputs, int[] rgForbidden, int first, int N, CspSolverDomain finals)
		{
			CspSolverTerm[] array = new CspSolverTerm[N];
			int[] array2 = new int[N];
			for (int i = 0; i < N; i++)
			{
				array[i] = inputs[first + i];
				array2[i] = rgForbidden[first + i];
			}
			return new sForbiddenRange(solver, array, array2, finals);
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			throw new InvalidOperationException(Resources.CompositeConstraintNotSupported);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			throw new InvalidOperationException(Resources.CompositeConstraintNotSupported);
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			CspSolverTerm cspSolverTerm = null;
			int num = 0;
			int num2 = 0;
			conflict = null;
			for (int i = 0; i < Width - 1; i++)
			{
				CspSolverTerm cspSolverTerm2 = _inputs[i];
				if (cspSolverTerm2.Count == 0)
				{
					conflict = cspSolverTerm2;
					return false;
				}
				if (1 == cspSolverTerm2.Count)
				{
					if (cspSolverTerm2.First != _forbiddens[i])
					{
						return false;
					}
					continue;
				}
				if (!cspSolverTerm2.Contains(_forbiddens[i]))
				{
					return false;
				}
				num2++;
				if (cspSolverTerm == null)
				{
					cspSolverTerm = cspSolverTerm2;
					num = _forbiddens[i];
				}
			}
			CspSolverTerm cspSolverTerm3 = _inputs[Width - 1];
			if (cspSolverTerm3.Count == 0)
			{
				conflict = cspSolverTerm3;
				return false;
			}
			if (num2 == 0)
			{
				return cspSolverTerm3.Exclude(_finalSet, out conflict);
			}
			if (1 == num2 && cspSolverTerm3.Count <= _finalSet.Count)
			{
				foreach (int item in cspSolverTerm3.Forward())
				{
					if (!_finalSet.Contains(item))
					{
						return false;
					}
				}
				if (cspSolverTerm.IsBoolean)
				{
					return cspSolverTerm.Force(0 == num, out conflict);
				}
				return cspSolverTerm.Exclude(num, num, out conflict);
			}
			return false;
		}

		/// <summary> Represent this class
		/// </summary>
		public override string ToString()
		{
			return "ForbiddenRange";
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
