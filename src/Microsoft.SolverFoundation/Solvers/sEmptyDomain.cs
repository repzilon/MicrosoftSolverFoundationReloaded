using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A Term with no value consistent with the model.
	/// </summary>
	internal class sEmptyDomain : CspSolverDomain, IVisitable
	{
		public override ValueKind Kind => ValueKind.Integer;

		/// <summary> How many distinct choices is this restriction allowing?
		/// </summary>
		public override int Count
		{
			[DebuggerStepThrough]
			get
			{
				return 0;
			}
		}

		/// <summary> The first value in the restriction otherSet
		/// </summary>
		internal override int First
		{
			get
			{
				throw new InvalidOperationException(Resources.EnumerateEmptyDomain);
			}
		}

		/// <summary> The last value in the restriction otherSet
		/// </summary>
		internal override int Last
		{
			get
			{
				throw new InvalidOperationException(Resources.EnumerateEmptyDomain);
			}
		}

		/// <summary> Get the i'th value of the Domain.
		/// </summary>
		internal override int this[int i]
		{
			get
			{
				throw new InvalidOperationException(Resources.EnumerateEmptyDomain);
			}
		}

		public void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		/// <summary> Please use immutable instance ConstraintSystem.DEmpty.
		/// </summary>
		internal sEmptyDomain()
		{
		}

		/// <summary> Enumerate allowed choices from first to last inclusive.
		/// </summary>
		internal override IEnumerable<int> Forward(int first, int last)
		{
			throw new InvalidOperationException(Resources.EnumerateEmptyDomain);
		}

		/// <summary> Enumerate allowed choices.
		/// </summary>
		internal override IEnumerable<int> Forward()
		{
			yield break;
		}

		/// <summary> Enumerate allowed choices from last to first inclusive.
		/// </summary>
		internal override IEnumerable<int> Backward(int last, int first)
		{
			throw new InvalidOperationException(Resources.EnumerateEmptyDomain);
		}

		/// <summary> Enumerate allowed choices in reverse order.
		/// </summary>
		internal override IEnumerable<int> Backward()
		{
			yield break;
		}

		/// <summary> Check if the given value is an element of the domain.
		/// </summary>
		internal override bool Contains(int val)
		{
			return false;
		}

		/// <summary> Check if this CspSolverDomain and the other CspSolverDomain have identical contents.
		/// </summary>
		public override bool SetEqual(CspDomain otherValueSet)
		{
			return 0 == otherValueSet.Count;
		}

		/// <summary> The predecessor within the domain, of the given value, or Int32.MinValue if none.
		/// </summary>
		internal override int Pred(int x)
		{
			return int.MinValue;
		}

		/// <summary> The successor within the domain, of the given value, or Int32.MaxValue if none.
		/// </summary>
		internal override int Succ(int x)
		{
			return int.MaxValue;
		}

		/// <summary> Return the index for the exact match of value x if the Domain were enumerated.
		///           Returns an undefined negative number if x is not a member of the Domain.
		/// </summary>
		internal override int IndexOf(int x)
		{
			return int.MinValue;
		}

		/// <summary> Operations on an Empty CspSolverDomain usually indicate a programming error.
		/// </summary>
		private bool EmptyOperation(out CspSolverDomain newD)
		{
			newD = this;
			return false;
		}

		/// <summary> Intersect with the given bounds.  Return false if no change.
		/// </summary>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Intersect(int min, int max, out CspSolverDomain newD)
		{
			return EmptyOperation(out newD);
		}

		/// <summary> Intersect with the given ordered distinct otherSet.  Return false if no change.
		/// </summary>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Intersect(out CspSolverDomain newD, params int[] orderedUniqueSet)
		{
			return EmptyOperation(out newD);
		}

		/// <summary> Intersect with the other CspSolverDomain.  Return false if no change to this.
		/// </summary>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Intersect(CspSolverDomain D, out CspSolverDomain newD)
		{
			return EmptyOperation(out newD);
		}

		/// <summary> Exclude the given bounds.  Return false if no change.
		/// </summary>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Exclude(int min, int max, out CspSolverDomain newD)
		{
			return EmptyOperation(out newD);
		}

		/// <summary> Exclude the given ordered distinct otherSet.  Return false if no change.
		/// </summary>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Exclude(out CspSolverDomain newD, params int[] orderedUniqueSet)
		{
			return EmptyOperation(out newD);
		}

		/// <summary> Exclude the other CspSolverDomain.  Return false if no change to this.
		/// </summary>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Exclude(CspSolverDomain D, out CspSolverDomain newD)
		{
			return EmptyOperation(out newD);
		}

		internal override CspSolverDomain Clone()
		{
			return this;
		}

		internal override string AppendTo(string line, int itemLimit)
		{
			return line;
		}

		internal override string AppendTo(string line, int itemLimit, CspVariable var)
		{
			return line;
		}

		/// <summary> Represent.
		/// </summary>
		public override string ToString()
		{
			return "EmptyDomain {}";
		}
	}
}
