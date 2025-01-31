using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A CspSolverDomain represents the allowed values of a Term.
	/// </summary>
	internal abstract class CspSolverDomain : CspDomain
	{
		private readonly ValueKind _valueKind;

		private int _scale;

		/// <summary> The first value in the restriction otherSet
		/// </summary>
		public sealed override object FirstValue => GetValue(First);

		/// <summary> The last value in the restriction otherSet
		/// </summary>
		public sealed override object LastValue => GetValue(Last);

		public override ValueKind Kind => _valueKind;

		internal int Scale => _scale;

		/// <summary> Get the i'th value of the Domain.
		/// </summary>
		internal abstract int this[int i] { get; }

		/// <summary> A CspSolverDomain represents the allowed values of a Term.
		/// </summary>
		internal CspSolverDomain()
		{
			_valueKind = ValueKind.Integer;
			_scale = 1;
		}

		internal CspSolverDomain(ValueKind kind, int scale)
		{
			_valueKind = kind;
			_scale = scale;
		}

		/// <summary>
		/// Enumerate all values in this domain
		/// </summary>
		public sealed override IEnumerable<object> Values()
		{
			foreach (int ival in Forward())
			{
				yield return GetValue(ival);
			}
		}

		/// <summary> Check if the given value is an element of the domain.
		/// </summary>
		public sealed override bool ContainsValue(object val)
		{
			return Contains(GetInteger(val));
		}

		/// <summary> Enumerate all allowed choices from least to greatest.
		/// </summary>
		internal override IEnumerable<int> Forward()
		{
			return Forward(First, Last);
		}

		/// <summary> Enumerate all allowed choices from greatest to least.
		/// </summary>
		internal override IEnumerable<int> Backward()
		{
			return Backward(Last, First);
		}

		/// <summary> The predecessor within the domain, of the given value, or Int32.minVal if none.
		/// </summary>
		internal abstract int Pred(int x);

		/// <summary> The successor within the domain, of the given value, or Int32.maxVal if none.
		/// </summary>
		internal abstract int Succ(int x);

		/// <summary> Return the index for the exact match of value x if the Domain were enumerated.
		///           Returns an undefined negative number if x is not a member of the Domain.
		/// </summary>
		internal abstract int IndexOf(int x);

		/// <summary> Intersect with the given bounds.  Return false if no change.
		/// </summary>
		/// <param name="min">lowerbound</param>
		/// <param name="max">upperbound</param>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal abstract bool Intersect(int min, int max, out CspSolverDomain newD);

		/// <summary> Intersect with the given ordered distinct otherSet.  Return false if no change.
		/// </summary>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <param name="orderedUniqueSet">The set to intersect</param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal abstract bool Intersect(out CspSolverDomain newD, params int[] orderedUniqueSet);

		/// <summary> Intersect with the other CspSolverDomain.  Return false if no change to this.
		/// </summary>
		/// <param name="otherValueSet">The set to intersect</param>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal abstract bool Intersect(CspSolverDomain otherValueSet, out CspSolverDomain newD);

		/// <summary> Remove the range [min..max] from the interval.  Return false if no change.
		/// </summary>
		/// <param name="min">the lowerbound</param>
		/// <param name="max">the upperbound</param>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal abstract bool Exclude(int min, int max, out CspSolverDomain newD);

		/// <summary> Remove the orderedUniqueSet from this CspSolverDomain.  Return false if no change.
		/// </summary>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <param name="orderedUniqueSet">The set to exclude</param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal abstract bool Exclude(out CspSolverDomain newD, params int[] orderedUniqueSet);

		/// <summary> Remove the other CspSolverDomain from this.  Return false if no change.
		/// </summary>
		/// <param name="otherValueSet">The set to exclue</param>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal abstract bool Exclude(CspSolverDomain otherValueSet, out CspSolverDomain newD);

		/// <summary> Clone this domain
		/// </summary>
		internal abstract CspSolverDomain Clone();

		internal abstract string AppendTo(string line, int itemLimit);

		internal abstract string AppendTo(string line, int itemLimit, CspVariable var);

		/// <summary>
		/// Return the actural value of in the domain that is mapped to the input integer. 
		/// Based on the value type, the returned value could be an integer, a double, or a string.
		/// </summary>
		/// <param name="intval">The internal integer value</param>
		/// <returns>The external value that is mapped to the input integer value.</returns>
		internal object GetValue(int intval)
		{
			switch (Kind)
			{
			case ValueKind.Integer:
				return intval;
			case ValueKind.Decimal:
				return (double)intval / (double)Scale;
			case ValueKind.Symbol:
				return (this as CspSymbolDomain).GetSymbol(intval);
			default:
				throw new InvalidCastException(Resources.InvalidValueType);
			}
		}

		/// <summary>
		/// Return the internal integer representation of the given value, based on its value type.
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		internal int GetInteger(object val)
		{
			switch (Kind)
			{
			case ValueKind.Integer:
				return (int)val;
			case ValueKind.Decimal:
				return (int)Math.Round((double)val * (double)Scale, 0);
			case ValueKind.Symbol:
				return (this as CspSymbolDomain).GetIntegerValue((string)val);
			default:
				throw new InvalidCastException(Resources.InvalidValueType);
			}
		}

		/// <summary> Pick a value at random from the domain
		/// </summary>
		/// <param name="prng">a Pseudo-Random Number Generator</param>
		internal int Pick(Random prng)
		{
			return this[prng.Next(Count)];
		}
	}
}
