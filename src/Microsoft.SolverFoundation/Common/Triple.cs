using System;
using System.Globalization;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Each triple represents the value at a row and column of the array.
	/// </summary>
	internal struct Triple<Number> : IComparable<Triple<Number>>
	{
		/// <summary> The row of the array for this value
		/// </summary>
		public int Row;

		/// <summary> The column of the array for this value
		/// </summary>
		public int Column;

		/// <summary> The value sited at this row, column position
		/// </summary>
		public Number Value;

		/// <summary> Each triple represents the value at a row and column of the array.
		/// </summary>
		public Triple(int row, int col, Number value)
		{
			if (row < 0)
			{
				throw new ArgumentOutOfRangeException("row");
			}
			if (col < 0)
			{
				throw new ArgumentOutOfRangeException("col");
			}
			Value = value;
			Row = row;
			Column = col;
		}

		/// <summary> The default comparison for IComparable&lt;&gt;.  Only the row,column are used.
		///           The value is not considered: the comparison is for the purpose of structure.
		/// </summary>
		/// <param name="b"> The other triple for which row and column are to be compared </param>
		/// <returns> negative, zero, or positive if this row,column is compared to b's row,column </returns>
		public int CompareTo(Triple<Number> b)
		{
			if (Column == b.Column)
			{
				return Row - b.Row;
			}
			return Column - b.Column;
		}

		/// <summary> Formatted comma separated row, column, value
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}, {1}, {2}", new object[3] { Row, Column, Value });
		}
	}
}
