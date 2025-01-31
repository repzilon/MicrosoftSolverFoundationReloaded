using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Index in a matrix
	/// </summary>
	internal class DisolverMatrixIndex : DisolverIntegerTerm
	{
		public readonly DisolverTerm[][] Matrix;

		public readonly DisolverIntegerTerm Index1;

		public readonly DisolverIntegerTerm Index2;

		public DisolverMatrixIndex(IntegerSolver s, long lbound, long rbound, DisolverTerm[][] matrix, DisolverIntegerTerm idx1, DisolverIntegerTerm idx2)
			: base(s, lbound, rbound, InitializeSubtems(matrix, idx1, idx2))
		{
			Matrix = matrix;
			Index1 = idx1;
			Index2 = idx2;
		}

		private static DisolverTerm[] InitializeSubtems(DisolverTerm[][] tab, DisolverIntegerTerm idx1, DisolverIntegerTerm idx2)
		{
			List<DisolverTerm> list = new List<DisolverTerm>();
			foreach (DisolverTerm[] array in tab)
			{
				DisolverTerm[] array2 = array;
				foreach (DisolverTerm item in array2)
				{
					list.Add(item);
				}
			}
			list.Add(idx1);
			list.Add(idx2);
			return list.ToArray();
		}
	}
}
