using System.Collections.Generic;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class CspWrapperSequenceEnumerable : ExprSequenceEnumerable
	{
		private ConstraintSystem _fs;

		private ConstraintSolverSolution _sol;

		public ConstraintSystem Solver => _fs;

		public ConstraintSolverSolution Solution => _sol;

		public CspWrapperSequenceEnumerable(RewriteSystem rs, ConstraintSystem fs, ConstraintSolverSolution sol, IEnumerable<Expression> rgexpr)
			: base(rs, rgexpr)
		{
			_fs = fs;
			_sol = sol;
		}
	}
}
