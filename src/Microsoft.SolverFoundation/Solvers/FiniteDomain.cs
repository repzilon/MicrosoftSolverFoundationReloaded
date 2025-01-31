using System.Collections;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Finite Domain in sparse representation, i.e. we have a bit vector
	///   saying whether each particular value is present or has been removed.
	/// </summary>
	internal class FiniteDomain
	{
		private readonly long _firstIndex;

		private TrailOfFiniteDomains _trail;

		private BitArray _bit;

		private long _cardinality;

		/// <summary>
		///   Number of values currently allowed in the Finite domain
		/// </summary>
		public long Cardinality => _cardinality;

		/// <summary>
		///   Construction; the domain will contain exactly the values
		///   specified in the initial Range.
		/// </summary>
		/// <param name="t">trail to which the set is connected</param>
		/// <param name="initialRange">set of initial values</param>
		public FiniteDomain(TrailOfFiniteDomains t, CspDomain initialRange)
		{
			DisolverDiscreteDomain disolverDiscreteDomain = DisolverDiscreteDomain.SubCast(initialRange);
			long num = disolverDiscreteDomain.First;
			long num2 = disolverDiscreteDomain.Last;
			int length = (int)(num2 - num + 1);
			_trail = t;
			_firstIndex = num;
			if (initialRange is SparseDomain)
			{
				_bit = new BitArray(length, defaultValue: false);
				{
					foreach (int item in disolverDiscreteDomain.Forward())
					{
						RestoreValue(item, item);
					}
					return;
				}
			}
			_bit = new BitArray(length, defaultValue: true);
			_cardinality = initialRange.Count;
		}

		/// <summary>
		///   Removes an integer from the set.
		///   Will have no effect if the value is not actually contained but 
		///   the value has to be within the bounds effectively representable
		///   in the set.
		/// </summary>
		public void Remove(long elt)
		{
			int index = (int)(elt - _firstIndex);
			if (_bit[index])
			{
				_bit[index] = false;
				_trail.RecordRemoval(this, elt, elt);
				_cardinality--;
			}
		}

		/// <summary>
		///   Returns true iff elt included in the set.
		/// </summary>
		public bool Contains(long elt)
		{
			return _bit[(int)(elt - _firstIndex)];
		}

		/// <summary>
		///   Returns the smallest value that is within the specified
		///   interval and that is also contained in the domain.
		///   Returns MaxValue if no such value is found.
		/// </summary>
		public long LowestValue(long left, long right)
		{
			int num = (int)(left - _firstIndex);
			long num2 = left;
			while (true)
			{
				if (_bit[num])
				{
					return num2;
				}
				num2++;
				if (num2 > right)
				{
					break;
				}
				num++;
			}
			return long.MaxValue;
		}

		/// <summary>
		///   Returns the largest value that is within the specified 
		///   interval and that is also contained in the domain.
		///   Returns MinValue if no such value is found.
		/// </summary>
		public long HighestValue(long left, long right)
		{
			int num = (int)(right - _firstIndex);
			long num2 = right;
			while (true)
			{
				if (_bit[num])
				{
					return num2;
				}
				num2--;
				if (num2 < left)
				{
					break;
				}
				num--;
			}
			return long.MinValue;
		}

		internal void RestoreValue(long left, long right)
		{
			int num = (int)(left - _firstIndex);
			for (long num2 = left; num2 <= right; num2++)
			{
				_bit[num] = true;
				num++;
				_cardinality++;
			}
		}

		public override string ToString()
		{
			string text = "";
			for (int i = 0; i < _bit.Length; i++)
			{
				if (_bit[i])
				{
					text = text + (_firstIndex + i) + " ";
				}
			}
			return text;
		}
	}
}
