using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Index, or array indirection
	/// </summary>
	internal class DisolverIndex : DisolverIntegerTerm
	{
		public readonly DisolverTerm[] Array;

		public readonly DisolverIntegerTerm Index;

		public DisolverIndex(IntegerSolver s, long lbound, long rbound, DisolverTerm[] tab, DisolverIntegerTerm idx)
			: base(s, lbound, rbound, InitializeSubtems(tab, idx))
		{
			Array = tab;
			Index = idx;
		}

		private static DisolverTerm[] InitializeSubtems(DisolverTerm[] tab, DisolverTerm idx)
		{
			List<DisolverTerm> list = new List<DisolverTerm>(tab);
			list.Add(idx);
			return list.ToArray();
		}
	}
}
