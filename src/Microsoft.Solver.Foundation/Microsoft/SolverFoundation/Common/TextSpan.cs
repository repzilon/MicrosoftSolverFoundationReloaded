using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Represents a span of text in a text buffer.
	/// This needs to be very compact. Every token has one of these.
	/// </summary>
	internal struct TextSpan
	{
		private readonly ITextVersion _tvr;

		private readonly int _jchMin;

		private readonly int _jchLim;

		/// <summary> Check if valid
		/// </summary>
		public bool IsValid
		{
			get
			{
				if (_tvr != null && 0 < _jchMin)
				{
					return _jchMin <= _jchLim;
				}
				return false;
			}
		}

		/// <summary> get text version
		/// </summary>
		public ITextVersion Version => _tvr;

		/// <summary> get the minimum
		/// </summary>
		public int Min => _jchMin - 1;

		/// <summary> get the limit
		/// </summary>
		public int Lim => _jchLim - 1;

		/// <summary> constructor
		/// </summary>
		public TextSpan(ITextVersion tvr, int ichMin, int ichLim)
		{
			DebugContracts.NonNull(tvr);
			_tvr = tvr;
			_jchMin = ichMin + 1;
			_jchLim = ichLim + 1;
		}

		/// <summary> Do internal consistency check
		/// </summary>
		[Conditional("DEBUG")]
		public void AssertValid()
		{
		}

		/// <summary> Compute the union of the two spans.
		/// </summary>
		public static TextSpan operator +(TextSpan span1, TextSpan span2)
		{
			if (!MapToSame(ref span1, ref span2))
			{
				throw new InvalidOperationException(Resources.SpansNotCompatible);
			}
			return new TextSpan(span1.Version, Math.Min(span1.Min, span2.Min), Math.Max(span1.Lim, span2.Lim));
		}

		/// <summary> Represent as a formatted string
		/// </summary>
		/// <returns> the string representation </returns>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "({0},{1})", new object[2] { Min, Lim });
		}

		/// <summary> Map to same
		/// </summary>
		public static bool MapToSame(ref TextSpan span1, ref TextSpan span2)
		{
			if (span1._tvr == span2._tvr)
			{
				return true;
			}
			return span1._tvr.MapToSame(ref span1, ref span2);
		}
	}
}
