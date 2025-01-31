using System.Collections.Generic;
using System.Text;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Triples are an exchange format for moving sparse arrays.
	///           They are also a convenient buffer format for non-ordered
	///           accumulation of array elements.
	/// </summary>
	internal class TripleList<Number> : List<Triple<Number>>
	{
		/// <summary> What to do when duplicate Triples are encountered.
		/// </summary>
		public delegate Number DuplicatePolicy(Number first, Number second);

		/// <summary> A list of {row, column, value} triples specify array contents
		/// </summary>
		public TripleList()
		{
		}

		/// <summary> Convenient form of Add which constructs the Triple
		/// </summary>
		public void Add(int row, int col, Number value)
		{
			Add(new Triple<Number>(row, col, value));
		}

		/// <summary> Sort the list and handle duplicates
		/// </summary>
		/// <param name="duplicate">Policy for handling duplicates - null means use the first.</param>
		/// <returns> the count of remaining unique values </returns>
		public virtual int SortUnique(DuplicatePolicy duplicate)
		{
			int count = base.Count;
			if (0 < count)
			{
				Sort();
				int num = 0;
				for (int i = 1; i < count; i++)
				{
					if (base[num].CompareTo(base[i]) != 0)
					{
						num++;
						if (num < i)
						{
							base[num] = base[i];
						}
					}
					else if (duplicate != null)
					{
						Triple<Number> value = base[num];
						value.Value = duplicate(value.Value, base[i].Value);
						base[num] = value;
					}
				}
				count = num + 1;
				RemoveRange(count, base.Count - count);
			}
			return base.Count;
		}

		/// <summary> Represent a Triple&lt;&gt; as a string
		/// </summary>
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			using (Enumerator enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Triple<Number> current = enumerator.Current;
					stringBuilder.Append(current.Row).Append(", ").Append(current.Column)
						.Append(", ")
						.Append(current.Value)
						.Append("\n");
				}
			}
			return stringBuilder.ToString();
		}
	}
}
