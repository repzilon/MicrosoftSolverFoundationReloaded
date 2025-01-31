using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   "enum", i.e. set of values that have a symbolic name.
	///   Internally these symbolic values are labelled from 0 to n-1
	/// </summary>
	/// <remarks>
	///   Naive implementation
	/// </remarks>
	internal class DisolverSymbolSet : DisolverDiscreteDomain
	{
		protected List<string> _elements;

		public override ValueKind Kind => ValueKind.Symbol;

		public override int Count => _elements.Count;

		internal override int First => 0;

		internal override int Last => _elements.Count - 1;

		public DisolverSymbolSet(params string[] symbolSet)
		{
			_elements = new List<string>(symbolSet.Length);
			foreach (string text in symbolSet)
			{
				if (Value(text) != -1)
				{
					throw new ArgumentException(Resources.MultipleOccurrenceOfASymbolInSymbolSet);
				}
				_elements.Add(text);
			}
		}

		public override bool SetEqual(CspDomain otherDomain)
		{
			if (!(otherDomain is DisolverSymbolSet disolverSymbolSet))
			{
				return false;
			}
			if (disolverSymbolSet._elements.Count != _elements.Count)
			{
				return false;
			}
			for (int num = _elements.Count - 1; num >= 0; num--)
			{
				if (disolverSymbolSet._elements[num] != _elements[num])
				{
					return false;
				}
			}
			return true;
		}

		public override IEnumerable<object> Values()
		{
			foreach (string element in _elements)
			{
				yield return element;
			}
		}

		internal override IEnumerable<int> Forward(int first, int last)
		{
			int beg = Math.Max(first, 0);
			int end = Math.Min(last, _elements.Count - 1);
			for (int i = beg; i <= end; i++)
			{
				yield return i;
			}
		}

		internal override IEnumerable<int> Backward(int last, int first)
		{
			int beg = Math.Max(first, 0);
			int end = Math.Min(last, _elements.Count - 1);
			for (int i = end; i >= beg; i--)
			{
				yield return i;
			}
		}

		internal override bool Contains(int val)
		{
			if (0 <= val)
			{
				return val < _elements.Count;
			}
			return false;
		}

		/// <summary>
		///   Conversion from int value to its symbol
		/// </summary>
		public string Symbol(int val)
		{
			return _elements[val];
		}

		/// <summary>
		///   Conversion from symbol to its int value
		/// </summary>
		public int Value(string symbol)
		{
			for (int num = _elements.Count - 1; num >= 0; num--)
			{
				if (_elements[num] == symbol)
				{
					return num;
				}
			}
			return -1;
		}
	}
}
