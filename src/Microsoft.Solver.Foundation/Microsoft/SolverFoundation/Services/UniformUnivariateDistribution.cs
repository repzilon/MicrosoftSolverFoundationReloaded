using System;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	internal abstract class UniformUnivariateDistribution<TotalOrder> : UnivariateDistribution<TotalOrder> where TotalOrder : IComparable<TotalOrder>
	{
		protected readonly TotalOrder _lowerBound;

		protected readonly TotalOrder _upperBound;

		protected readonly double _difference;

		/// <summary> The mean.
		/// </summary>
		public sealed override double Mean => Convert.ToDouble(_lowerBound, CultureInfo.InvariantCulture) + _difference / 2.0;

		/// <summary> The lopsidedness of the distribution as defined by the third moment.
		/// </summary>
		public sealed override double Skewness => 0.0;

		internal TotalOrder LowerBound => _lowerBound;

		internal TotalOrder UpperBound => _upperBound;

		public UniformUnivariateDistribution(TotalOrder lowerBound, TotalOrder upperBound)
		{
			if (typeof(TotalOrder) != typeof(int) && typeof(TotalOrder) != typeof(double))
			{
				throw new MsfException(Resources.InternalError);
			}
			_lowerBound = lowerBound;
			_upperBound = upperBound;
			_difference = Convert.ToDouble(_upperBound, CultureInfo.InvariantCulture) - Convert.ToDouble(_lowerBound, CultureInfo.InvariantCulture);
		}
	}
}
