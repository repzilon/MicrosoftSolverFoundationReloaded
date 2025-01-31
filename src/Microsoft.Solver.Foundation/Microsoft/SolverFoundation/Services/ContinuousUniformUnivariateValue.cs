using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	[DebuggerDisplay("Lower = {_lowerBound}, Upper = {_upperBound}")]
	internal sealed class ContinuousUniformUnivariateValue : DistributedValue
	{
		private readonly double _lowerBound;

		private readonly double _upperBound;

		public ContinuousUniformUnivariateValue(double lowerBound, double upperBound)
		{
			if (double.IsInfinity(lowerBound) || double.IsNaN(lowerBound))
			{
				throw new ArgumentOutOfRangeException("lowerBound");
			}
			if (double.IsInfinity(upperBound) || double.IsNaN(upperBound))
			{
				throw new ArgumentOutOfRangeException("upperBound");
			}
			if (upperBound < lowerBound)
			{
				throw new ArgumentOutOfRangeException("lowerBound", Resources.LowerBoundCannotBeLargerThanUpperBound);
			}
			Distribution = new ContinuousUniformUnivariateDistribution(lowerBound, upperBound);
			_lowerBound = lowerBound;
			_upperBound = upperBound;
		}

		/// <summary> Returns a string representation of the distribution.
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.CurrentCulture, "LowerBound is {0} UpperBound is {1}", new object[2] { _lowerBound, _upperBound });
		}
	}
}
