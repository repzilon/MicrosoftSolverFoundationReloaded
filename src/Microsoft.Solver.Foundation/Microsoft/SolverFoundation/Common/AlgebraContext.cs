using System;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Various global parameters driving Algebra algorithms
	/// </summary>
	public sealed class AlgebraContext
	{
		/// <summary> Policy limit regardless of physical limit.
		/// </summary>
		private static int _threadCountLimit = -1;

		/// <summary> The ThreadCountLimit is the policy limit on threading.  It cannot
		///           be set outside the range (1 .. System.Environment.ProcessorCount).
		///           Values set will be silently bounded at those limits.
		/// </summary>
		public static int ThreadCountLimit
		{
			get
			{
				if (_threadCountLimit < 0)
				{
					_threadCountLimit = Math.Min(64, Environment.ProcessorCount);
				}
				return _threadCountLimit;
			}
			set
			{
				_threadCountLimit = Math.Max(1, Math.Min(value, Environment.ProcessorCount));
			}
		}

		/// <summary> This has been used as a debug feature to track the origin of
		///           test sets such as .MPS files currently using the AlgebraContext
		/// </summary>
		internal static string PathName { get; set; }

		private AlgebraContext()
		{
		}
	}
}
