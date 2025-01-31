using System.Globalization;

namespace Microsoft.SolverFoundation.Services
{
	internal sealed class NormalUnivariateValue : DistributedValue
	{
		public NormalUnivariateValue(double mean, double standardDeviation)
		{
			Distribution = new NormalUnivariateDistribution(mean, standardDeviation);
		}

		/// <summary> Returns a string representation of the distribution.
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.CurrentCulture, "Mean = {0}, Variance = {1}", new object[2] { Distribution.Mean, Distribution.Variance });
		}
	}
}
