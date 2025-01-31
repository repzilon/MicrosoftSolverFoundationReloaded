using System;
using System.Globalization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>Information about the location of OML parse errors.
	/// </summary>
	[Serializable]
	public class OmlParseExceptionLocation
	{
		/// <summary>The line number where the parse error begins.
		/// </summary>
		public int LineStart { get; set; }

		/// <summary>The column number where the parse error begins.
		/// </summary>
		public int ColumnStart { get; set; }

		/// <summary>The line number where the parse error ends.
		/// </summary>
		public int LineEnd { get; set; }

		/// <summary>The column number where the parse error ends.
		/// </summary>
		public int ColumnEnd { get; set; }

		internal OmlParseExceptionLocation(SrcPos spos)
		{
			LineStart = spos.lineMin;
			ColumnStart = spos.colMin;
			LineEnd = spos.lineLim;
			ColumnEnd = spos.colLim;
		}

		/// <summary>Returns a string representation of the instance.
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, Resources.OmlParseExceptionLocationFormat0123, LineStart, ColumnStart, LineEnd, ColumnEnd);
		}
	}
}
