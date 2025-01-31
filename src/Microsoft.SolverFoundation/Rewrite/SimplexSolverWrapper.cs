using System;
using System.Diagnostics;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class SimplexSolverWrapper : LinearSolverWrapper
	{
		private SimplexSolver _solver;

		public SimplexSolver Solver
		{
			[DebuggerStepThrough]
			get
			{
				return _solver;
			}
		}

		public override LinearModel Model => _solver;

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

		public SimplexSolverWrapper(SolveRewriteSystem rs)
			: base(rs)
		{
			_solver = new SimplexSolver(ExpressionComparer.Instance);
		}

		public override string ToString()
		{
			return "SimplexSolver[...]";
		}
	}
}
