using System;

namespace Microsoft.SolverFoundation.Common
{
	[Flags]
	internal enum IntLitKind : byte
	{
		None = 0,
		Uns = 1,
		Lng = 2,
		Hex = 4,
		Big = 8,
		UnsLng = 3
	}
}
