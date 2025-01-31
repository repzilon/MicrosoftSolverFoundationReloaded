using System;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class InteriorPointSolverWrapper : LinearSolverWrapper
	{
		private InteriorPointSolver _ipmSolver;

		public InteriorPointSolver IPMSolver => _ipmSolver;

		public override LinearModel Model => _ipmSolver;

		public override Expression Head
		{
			get
			{
				if (base.Rewrite.Scope.GetSymbolThis("SimplexSolver", out var sym))
				{
					return sym;
				}
				throw new InvalidOperationException(Resources.SimplexSolverSymbolIsMissing);
			}
		}

		public InteriorPointSolverWrapper(SolveRewriteSystem rs)
			: base(rs)
		{
			_ipmSolver = new InteriorPointSolver(ExpressionComparer.Instance);
		}

		public override string ToString()
		{
			return "InteriorPointSolver[...]";
		}
	}
}
