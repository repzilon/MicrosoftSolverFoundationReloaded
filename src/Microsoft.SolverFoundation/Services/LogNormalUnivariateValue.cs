using System.Globalization;

namespace Microsoft.SolverFoundation.Services
{
	internal sealed class LogNormalUnivariateValue : DistributedValue
	{
		public LogNormalUnivariateValue(double meanLog, double standardDeviationLog)
		{
			Distribution = new LogNormalUnivariateDistribution(meanLog, standardDeviationLog);
		}

		/// <summary> Returns a string representation of the distribution.
		/// </summary>
		public override string ToString()
		{
			LogNormalUnivariateDistribution logNormalUnivariateDistribution = Distribution as LogNormalUnivariateDistribution;
			return string.Format(CultureInfo.CurrentCulture, "MeanLog (Mu) = {0}, StdLog (Sigma) = {1}", new object[2] { logNormalUnivariateDistribution.MeanLog, logNormalUnivariateDistribution.StdLog });
		}
	}
}
