using System.Diagnostics;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Represent &lt;key, index, value&gt; triplet 
	/// </summary>
	[DebuggerDisplay("{Index}, {Value}, key = {Key}")]
	public struct LinearEntry
	{
		/// <summary> row/variable key 
		/// </summary>
		public object Key;

		/// <summary> row/variable index (always a VID, whether for row or for variable)
		/// </summary>
		public int Index;

		/// <summary> row/variable value 
		/// </summary>
		public Rational Value;

		/// <summary> Compare whether the values of two LinearEntry are equal
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj != null)
			{
				LinearEntry? linearEntry;
				LinearEntry? linearEntry2 = (linearEntry = obj as LinearEntry?);
				if (linearEntry2.HasValue)
				{
					if (Key == linearEntry.Value.Key && Index == linearEntry.Value.Index)
					{
						return Value == linearEntry.Value.Value;
					}
					return false;
				}
			}
			return false;
		}

		/// <summary> Compare whether the values of two LinearEntry are equal
		/// </summary>
		public static bool operator ==(LinearEntry le1, LinearEntry le2)
		{
			return le1.Equals(le2);
		}

		/// <summary> Compare whether the values of two LinearEntry are not equal
		/// </summary>
		public static bool operator !=(LinearEntry le1, LinearEntry le2)
		{
			return !le1.Equals(le2);
		}

		/// <summary> Return the hashcode of this LinearEntry
		/// </summary>
		public override int GetHashCode()
		{
			int num = Statics.CombineHash(Index.GetHashCode(), Value.GetHashCode());
			if (Key == null)
			{
				return num;
			}
			return Statics.CombineHash(Key.GetHashCode(), num);
		}
	}
}
