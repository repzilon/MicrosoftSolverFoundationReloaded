using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A Domain represents the allowed values of a Term.
	///           A Domain is immutable.  An implementation of a Solver may choose to
	///             pool equal value sets partially or completely.
	/// </summary>
	public abstract class CspDomain
	{
		/// <summary>
		/// Valid kinds allowed in domains.
		/// </summary>
		[Flags]
		public enum ValueKind
		{
			/// <summary>
			/// Integer values
			/// </summary>
			Integer = 1,
			/// <summary>
			/// Decimal values
			/// </summary>
			Decimal = 2,
			/// <summary>
			/// Symbolic values
			/// </summary>
			Symbol = 4
		}

		/// <summary>
		/// Return the kind of values in this Domain.
		/// </summary>
		public abstract ValueKind Kind { get; }

		/// <summary> How many distinct choices is this restriction allowing?
		/// </summary>
		public abstract int Count { get; }

		/// <summary> The first value in the restriction otherSet
		/// </summary>
		public abstract object FirstValue { get; }

		/// <summary> The last value in the restriction otherSet
		/// </summary>
		public abstract object LastValue { get; }

		/// <summary> The first value in the restriction otherSet
		/// </summary>
		internal abstract int First { get; }

		/// <summary> The last value in the restriction otherSet
		/// </summary>
		internal abstract int Last { get; }

		/// <summary> Check if this Domain and the other Domain have identical contents.
		/// </summary>
		public abstract bool SetEqual(CspDomain otherDomain);

		/// <summary>
		/// Enumerate all values in this domain
		/// </summary>
		public abstract IEnumerable<object> Values();

		/// <summary> Check if the given value is an element of the domain.
		/// </summary>
		public abstract bool ContainsValue(object val);

		/// <summary> Enumerate allowed choices from first to last inclusive.
		/// </summary>
		internal abstract IEnumerable<int> Forward(int first, int last);

		/// <summary> Enumerate all allowed choices from least to greatest.
		/// </summary>
		internal abstract IEnumerable<int> Forward();

		/// <summary> Enumerate allowed choices from first to last inclusive.
		/// </summary>
		internal abstract IEnumerable<int> Backward(int last, int first);

		/// <summary> Enumerate all allowed choices from greatest to least.
		/// </summary>
		internal abstract IEnumerable<int> Backward();

		/// <summary> Check if the given value is an element of the domain.
		/// </summary>
		internal abstract bool Contains(int val);
	}
}
