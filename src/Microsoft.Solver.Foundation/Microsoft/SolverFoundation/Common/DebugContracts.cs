using System;
using System.Diagnostics;

namespace Microsoft.SolverFoundation.Common
{
	internal static class DebugContracts
	{
		[Conditional("DEBUG")]
		public static void Check(bool f)
		{
		}

		[Conditional("DEBUG")]
		public static void Check(bool f, string message)
		{
		}

		[Conditional("DEBUG")]
		public static void Fail(string str)
		{
		}

		public static void NonNull<T>(T t) where T : class
		{
			if (t != null)
			{
				return;
			}
			throw new NullReferenceException();
		}
	}
}
