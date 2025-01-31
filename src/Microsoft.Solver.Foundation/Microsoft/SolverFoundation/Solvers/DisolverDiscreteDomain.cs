using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A domain, as manipulated by users when they state problems
	///   using Disolver
	/// </summary>
	internal abstract class DisolverDiscreteDomain : CspDomain
	{
		/// <summary> The first value in the restriction otherSet
		/// </summary>
		public sealed override object FirstValue => First;

		/// <summary> The last value in the restriction otherSet
		/// </summary>
		public sealed override object LastValue => Last;

		/// <summary>
		/// Enumerate all values in this domain
		/// </summary>
		public override IEnumerable<object> Values()
		{
			foreach (int ival in Forward())
			{
				yield return ival;
			}
		}

		/// <summary> Check if the given value is an element of the domain.
		/// </summary>
		public sealed override bool ContainsValue(object val)
		{
			return Contains((int)val);
		}

		internal override IEnumerable<int> Forward()
		{
			return Forward(First, Last);
		}

		internal override IEnumerable<int> Backward()
		{
			return Backward(Last, First);
		}

		/// <summary>
		///   Sometimes need to subcast from root class; use this:
		/// </summary>
		internal static DisolverDiscreteDomain SubCast(CspDomain d)
		{
			if (!(d is DisolverDiscreteDomain result))
			{
				throw new ArgumentException(Resources.InvalidDomain);
			}
			return result;
		}
	}
}
