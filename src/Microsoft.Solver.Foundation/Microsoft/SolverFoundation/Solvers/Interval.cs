using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A basic interval class.
	/// </summary>
	internal struct Interval
	{
		public long Lower;

		public long Upper;

		private static Interval _empty = new Interval(4611686018427387903L, -4611686018427387903L);

		public Interval(long l, long u)
		{
			Lower = l;
			Upper = u;
		}

		public static Interval Empty()
		{
			return _empty;
		}

		/// <summary>
		///   Safe interval enclosure. SLOW
		///   meant to be used for initializations.
		///   The approach is to systematically be within ranges where
		///   we can multiply values without overflowing.
		/// </summary>
		public static Interval operator *(Interval l, Interval r)
		{
			try
			{
				long[] list = checked(new long[4]
				{
					l.Lower * r.Lower,
					l.Lower * r.Upper,
					l.Upper * r.Lower,
					l.Upper * r.Upper
				});
				return new Interval(Math.Max(-4611686018427387903L, Utils.Min(list)), Math.Min(4611686018427387903L, Utils.Max(list)));
			}
			catch (OverflowException)
			{
				throw new ModelException("Multiplications over integer terms may exceed numerical precision.");
			}
		}
	}
}
