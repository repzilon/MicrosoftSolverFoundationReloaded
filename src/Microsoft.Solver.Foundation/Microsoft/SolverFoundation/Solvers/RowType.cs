using System;

namespace Microsoft.SolverFoundation.Solvers
{
	[Flags]
	internal enum RowType : byte
	{
		/// <summary>
		///  = 
		/// </summary>
		Equal = 1,
		/// <summary>
		/// less than or equal
		/// </summary>
		LessEqual = 2,
		/// <summary>
		/// greater than or equal
		/// </summary>
		GreaterEqual = 4,
		/// <summary>
		/// place holder
		/// </summary>
		Unknown = 0x80
	}
}
