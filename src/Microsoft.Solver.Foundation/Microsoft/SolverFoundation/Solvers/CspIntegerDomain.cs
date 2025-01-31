using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> An integer interval [min .. max].
	/// </summary>
	internal abstract class CspIntegerDomain : CspSolverDomain
	{
		/// <summary> An integer restraint.
		/// </summary>
		internal CspIntegerDomain()
		{
		}

		internal CspIntegerDomain(ValueKind kind, int scale)
			: base(kind, scale)
		{
		}

		/// <summary> Return the index of the initial value greater-or-equal to the given first value.
		/// </summary>
		/// <returns> a number in range [0 .. Length], where Length means beyond-end. </returns>
		internal static int FirstBound(int first, params int[] orderedUniqueSet)
		{
			int num = Array.BinarySearch(orderedUniqueSet, first);
			if (num >= 0)
			{
				return num;
			}
			return ~num;
		}

		/// <summary> Return the index of the final value less-or-equal to the given last value.
		/// </summary>
		/// <returns> a number in range [-1 .. Length-1], where -1 means before-start. </returns>
		internal static int LastBound(int last, params int[] orderedUniqueSet)
		{
			int num = Array.BinarySearch(orderedUniqueSet, last);
			if (num >= 0)
			{
				return num;
			}
			return ~num - 1;
		}

		/// <summary> Intersect with the given bounds.  Return false if no change.
		/// </summary>
		/// <param name="min">the lowerbound</param>
		/// <param name="max">the upperbound</param>
		/// <param name="otherValueSet"> The interval from which min,max are defined, null if none </param>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal abstract bool Intersect(int min, int max, CspIntervalDomain otherValueSet, out CspSolverDomain newD);

		/// <summary> Intersect with the given ordered distinct otherSet.  Return false if no change.
		/// </summary>
		/// <param name="orderedUniqueSet">The set to intersect</param>
		/// <param name="otherValueSet"> The otherSet from which the restriction otherSet is defined </param>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal abstract bool Intersect(int[] orderedUniqueSet, CspSetDomain otherValueSet, out CspSolverDomain newD);

		/// <summary> Intersect with the given bounds.  Return false if no change.
		/// </summary>
		/// <param name="min">the lowerbound</param>
		/// <param name="max">the upperbound</param>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Intersect(int min, int max, out CspSolverDomain newD)
		{
			return Intersect(min, max, null, out newD);
		}

		/// <summary> Intersect with the given ordered distinct otherSet.  Return false if no change.
		/// </summary>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <param name="orderedUniqueSet">The set to intersect</param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Intersect(out CspSolverDomain newD, params int[] orderedUniqueSet)
		{
			return Intersect(orderedUniqueSet, null, out newD);
		}

		/// <summary> Intersect with the other CspSolverDomain.  Return false if no change to this.
		/// </summary>
		/// <param name="otherValueSet">the set to intersect</param>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Intersect(CspSolverDomain otherValueSet, out CspSolverDomain newD)
		{
			if (otherValueSet == null || otherValueSet.Count == 0)
			{
				newD = ConstraintSystem.DEmpty;
				return true;
			}
			CspIntervalDomain cspIntervalDomain = otherValueSet as CspIntervalDomain;
			CspSetDomain cspSetDomain = otherValueSet as CspSetDomain;
			if (cspIntervalDomain != null)
			{
				return Intersect(otherValueSet.First, otherValueSet.Last, cspIntervalDomain, out newD);
			}
			if (cspSetDomain != null)
			{
				return Intersect(cspSetDomain.Set, cspSetDomain, out newD);
			}
			throw new InvalidOperationException(Resources.UnknownDomainType + otherValueSet.ToString());
		}

		/// <summary> Exclude the other CspSolverDomain.  Return false if no change to this.
		/// </summary>
		/// <param name="otherValueSet">The set to exclude</param>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Exclude(CspSolverDomain otherValueSet, out CspSolverDomain newD)
		{
			if (otherValueSet == null || otherValueSet.Count == 0)
			{
				newD = ConstraintSystem.DEmpty;
				return true;
			}
			if (otherValueSet is CspIntervalDomain)
			{
				return Exclude(otherValueSet.First, otherValueSet.Last, out newD);
			}
			if (otherValueSet is CspSetDomain cspSetDomain)
			{
				return Exclude(out newD, cspSetDomain.Set);
			}
			throw new InvalidOperationException(Resources.UnknownDomainType + otherValueSet.ToString());
		}

		/// <summary>
		/// Compare whether the two given decimals are equal according to the precision
		/// </summary>
		internal static bool AreDecimalsEqual(int precision, double dec1, double dec2)
		{
			return (int)Math.Round(dec1 * (double)precision, 0) == (int)Math.Round(dec2 * (double)precision, 0);
		}
	}
}
