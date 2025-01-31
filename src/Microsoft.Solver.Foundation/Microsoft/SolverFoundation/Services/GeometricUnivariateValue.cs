using System.Globalization;

namespace Microsoft.SolverFoundation.Services
{
	internal sealed class GeometricUnivariateValue : DistributedValue
	{
		public GeometricUnivariateValue(double successProbability)
		{
			Distribution = new GeometricUnivariateDistribution(successProbability);
		}

		/// <summary> Returns a string representation of the distribution.
		/// </summary>
		public override string ToString()
		{
			GeometricUnivariateDistribution geometricUnivariateDistribution = Distribution as GeometricUnivariateDistribution;
			return string.Format(CultureInfo.CurrentCulture, "SuccessProbability = {0}", new object[1] { geometricUnivariateDistribution.SuccessProbability });
		}
	}
}
