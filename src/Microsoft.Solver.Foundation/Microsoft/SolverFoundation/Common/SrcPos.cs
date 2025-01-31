using System.Globalization;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> This is the verbose data structure. A TextPos can be resolved to one of these with the
	/// help of a line mapper / lexer.
	/// </summary>
	internal struct SrcPos
	{
		/// <summary> the span of text
		/// </summary>
		public TextSpan spanRaw;

		/// <summary> path of minimum
		/// </summary>
		public string pathMin;

		/// <summary> Path for limit
		/// </summary>
		public string pathLim;

		/// <summary> Line minimum
		/// </summary>
		public int lineMin;

		/// <summary> Column minimum
		/// </summary>
		public int colMin;

		/// <summary> Line limit
		/// </summary>
		public int lineLim;

		/// <summary> Column limit
		/// </summary>
		public int colLim;

		/// <summary> Convert the source position to a formatted string
		/// </summary>
		/// <returns> the formatted representation </returns>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}({1},{2})-({3},{4})", pathMin, lineMin, colMin, lineLim, colLim);
		}
	}
}
