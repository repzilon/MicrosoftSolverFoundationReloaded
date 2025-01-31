using System.Diagnostics;
using System.Globalization;

namespace Microsoft.SolverFoundation.Services
{
	[DebuggerDisplay("Rate = {Distribution.Rate}")]
	internal sealed class ExponentialUnivariateValue : DistributedValue
	{
		public ExponentialUnivariateValue(double rate)
		{
			Distribution = new ExponentialUnivariateDistribution(rate);
		}

		/// <summary> Returns a string representation of the distribution.
		/// </summary>
		public override string ToString()
		{
			ExponentialUnivariateDistribution exponentialUnivariateDistribution = Distribution as ExponentialUnivariateDistribution;
			return string.Format(CultureInfo.CurrentCulture, "Rate = {0}", new object[1] { exponentialUnivariateDistribution.Rate });
		}
	}
}
