using System;
using System.Diagnostics;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Represent &lt; diffRowVid, varVid&gt; tuple 
	/// </summary>
	[DebuggerDisplay("var = {varVid}, deriv = {derivRowVid}")]
	internal struct GradientEntry : IComparable<GradientEntry>
	{
		/// <summary> vid of gradient entry within the extended ITermModel (always a VID)
		/// </summary>
		public int derivRowVid;

		/// <summary> variable of differentiation index (always a VID)
		/// </summary>
		public int varVid;

		internal GradientEntry(int variableVid, int derivativeVid)
		{
			derivRowVid = derivativeVid;
			varVid = variableVid;
		}

		public int CompareTo(GradientEntry other)
		{
			return varVid - other.varVid;
		}
	}
}
