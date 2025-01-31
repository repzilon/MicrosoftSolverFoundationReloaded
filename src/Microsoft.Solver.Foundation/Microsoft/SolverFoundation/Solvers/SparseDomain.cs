using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   class of sparse value sets containing values at arbitrary positions
	///   and used to specify the initial range of some variables.
	/// </summary>
	/// <remarks>
	///   Naive implementation
	/// </remarks>
	internal class SparseDomain : DisolverDiscreteDomain
	{
		private int[] _orderedContents;

		public override int Count => _orderedContents.Length;

		internal override int First => _orderedContents[0];

		internal override int Last => _orderedContents[_orderedContents.Length - 1];

		public override ValueKind Kind => ValueKind.Integer;

		public SparseDomain(IEnumerable<int> inputs)
		{
			_orderedContents = Utils.GetOrderedUnique(inputs).ToArray();
		}

		public SparseDomain(int[] inputs, int from, int count)
		{
			if (from < 0 || count < 0 || from + count >= inputs.Length)
			{
				throw new ArgumentException(Resources.SparseDomainWithIncorrectIndexes);
			}
			int[] array = new int[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = inputs[i + from];
			}
			Array.Sort(_orderedContents);
		}

		public override IEnumerable<object> Values()
		{
			int len = _orderedContents.Length;
			for (int i = 0; i < len; i++)
			{
				yield return _orderedContents[i];
			}
		}

		internal override IEnumerable<int> Forward(int first, int last)
		{
			int len = _orderedContents.Length;
			for (int i = 0; i < len; i++)
			{
				long next = _orderedContents[i];
				if (first <= next && next <= last)
				{
					yield return (int)next;
				}
			}
		}

		internal override IEnumerable<int> Backward(int last, int first)
		{
			int beg = _orderedContents.Length - 1;
			for (int i = beg; i >= 0; i--)
			{
				long next = _orderedContents[i];
				if (first <= next && next <= last)
				{
					yield return (int)next;
				}
			}
		}

		internal override bool Contains(int val)
		{
			int[] orderedContents = _orderedContents;
			foreach (int num in orderedContents)
			{
				if (num == val)
				{
					return true;
				}
			}
			return false;
		}

		public override bool SetEqual(CspDomain otherDomain)
		{
			if (otherDomain is DisolverSymbolSet)
			{
				return false;
			}
			DisolverDiscreteDomain disolverDiscreteDomain = DisolverDiscreteDomain.SubCast(otherDomain);
			IEnumerator<int> enumerator = Forward().GetEnumerator();
			IEnumerator<int> enumerator2 = disolverDiscreteDomain.Forward().GetEnumerator();
			do
			{
				bool flag = !enumerator.MoveNext();
				bool flag2 = !enumerator2.MoveNext();
				if (flag || flag2)
				{
					if (flag)
					{
						return flag2;
					}
					return false;
				}
			}
			while (enumerator.Current == enumerator2.Current);
			return false;
		}

		/// <summary>
		///   returns the set of values contained in the set, 
		///   stored in a sorted (ascending) unique way.
		/// </summary>
		internal long[] GetOrderedUniqueValueSet()
		{
			long[] array = new long[_orderedContents.Length];
			Array.Copy(_orderedContents, array, _orderedContents.Length);
			return array;
		}
	}
}
