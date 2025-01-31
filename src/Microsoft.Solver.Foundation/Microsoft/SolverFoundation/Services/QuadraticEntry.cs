using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Represent &lt;key1, index1, key2, index2, value&gt; Quintet 
	/// </summary>
	public struct QuadraticEntry
	{
		/// <summary> row/variable key 
		/// </summary>
		public object Key1;

		/// <summary> row/variable index 
		/// </summary>
		public int Index1;

		/// <summary> row/variable key 
		/// </summary>
		public object Key2;

		/// <summary> row/variable index 
		/// </summary>
		public int Index2;

		/// <summary> row/variable value 
		/// </summary>
		public Rational Value;

		/// <summary> Compare whether the values of two QuadraticEntry are equal
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj != null)
			{
				QuadraticEntry? quadraticEntry;
				QuadraticEntry? quadraticEntry2 = (quadraticEntry = obj as QuadraticEntry?);
				if (quadraticEntry2.HasValue)
				{
					if (Key1 == quadraticEntry.Value.Key1 && Index1 == quadraticEntry.Value.Index1 && Key2 == quadraticEntry.Value.Key2 && Index2 == quadraticEntry.Value.Index2)
					{
						return Value == quadraticEntry.Value.Value;
					}
					return false;
				}
			}
			return false;
		}

		/// <summary> Compare whether the values of two QuadraticEntry are equal
		/// </summary>
		public static bool operator ==(QuadraticEntry le1, QuadraticEntry le2)
		{
			return le1.Equals(le2);
		}

		/// <summary> Compare whether the values of two QuadraticEntry are not equal
		/// </summary>
		public static bool operator !=(QuadraticEntry le1, QuadraticEntry le2)
		{
			return !le1.Equals(le2);
		}

		/// <summary> Return the hashcode of this QuadraticEntry
		/// </summary>
		public override int GetHashCode()
		{
			int num = Statics.CombineHash(Statics.CombineHash(Index1.GetHashCode(), Index2.GetHashCode()), Value.GetHashCode());
			if (Key2 != null)
			{
				num = Statics.CombineHash(Key2.GetHashCode(), num);
			}
			if (Key1 != null)
			{
				num = Statics.CombineHash(Key1.GetHashCode(), num);
			}
			return num;
		}
	}
}
