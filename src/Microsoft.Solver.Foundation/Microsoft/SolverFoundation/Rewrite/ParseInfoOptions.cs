using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	[Flags]
	internal enum ParseInfoOptions
	{
		None = 0,
		CreateScope = 1,
		BorrowScope = 2,
		IteratorScope = 4,
		VaryadicInfix = 8,
		Comparison = 0x10,
		CreateVariable = 0x20
	}
}
