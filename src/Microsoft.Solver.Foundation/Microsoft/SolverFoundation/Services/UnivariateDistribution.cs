using System;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> A probability distribution of one random variable.
	/// </summary>
	internal abstract class UnivariateDistribution
	{
		/// <summary> The mean.
		/// </summary>
		public abstract double Mean { get; }

		/// <summary> The variance (square of the standard deviation).
		/// </summary>
		public abstract double Variance { get; }

		/// <summary> The lopsidedness of the distribution as defined by the third moment.
		/// </summary>
		public abstract double Skewness { get; }

		/// <summary> The measure of the 'skinnyness' of the distribution,
		/// defined by the fourth moment.
		/// </summary>
		/// <remarks>The kurtosis coefficient is sometimes shifted by 3 to make the normal value 0.  This method
		/// returns the unshifted value.
		/// </remarks>
		public abstract double Kurtosis { get; }

		/// <summary>
		/// How many random numbers (in interval [0,1]) the distribution needs in order
		/// to generate a random number from the distribution.
		/// (Default is 1.)
		/// -1 is the value for a dynamic (not a priori known) number 
		/// of random numbers needed
		/// </summary>
		public virtual int RandomNumberNeeded => 1;

		/// <summary>Can the distribution be sampled with LHC 
		/// Default is that only distribution that needs exactly one uniform 
		/// random number can be sampled with LHC
		/// </summary>
		public virtual bool SupportsLatinHypercube => RandomNumberNeeded == 1;

		/// <summary> Sample the distribution.
		/// </summary>
		/// <param name="randomNumbers">uniform random numbers needed for sampling</param>
		/// <returns></returns>
		public abstract double SampleDouble(params double[] randomNumbers);

		/// <summary>Sample the distribution, using a delegate for retrieving unifom numbers
		/// </summary>
		/// <param name="generateUniformNumber">Delegate for getting uniform numbers</param>
		/// <returns></returns>
		public virtual double SampleDouble(Func<double> generateUniformNumber)
		{
			int randomNumberNeeded = RandomNumberNeeded;
			if (randomNumberNeeded < 0)
			{
				throw new NotSupportedException("Distribution does not support sampling");
			}
			double[] array = new double[randomNumberNeeded];
			for (int i = 0; i < randomNumberNeeded; i++)
			{
				array[i] = generateUniformNumber();
			}
			return SampleDouble(array);
		}
	}
	/// <summary> A univariate distribution whose sampled values are of a specified type.
	/// </summary>
	/// <typeparam name="TotalOrder">The type for sampled values.</typeparam>
	internal abstract class UnivariateDistribution<TotalOrder> : UnivariateDistribution where TotalOrder : IComparable<TotalOrder>
	{
		/// <summary>PDF (Probability density function)
		/// </summary>
		/// <param name="x">value (Real number)</param>
		/// <returns>Probability density</returns>
		public abstract double Density(TotalOrder x);

		/// <summary>Compute the cumulative distribution function for the specified value.
		/// </summary>
		/// <param name="x">value (Real number)</param>
		/// <returns>Probability</returns>
		public abstract double CumulativeDensity(TotalOrder x);

		/// <summary> The inverse cumulative distribution.
		/// </summary>
		/// <param name="probability">The probability.</param>
		/// <returns></returns>
		public abstract TotalOrder Quantile(double probability);

		/// <summary> A pseudorandom sample drawn, with replacement, from the population.
		/// </summary>
		/// <remarks>Default implementation is to use Quantile (inverse CDF for sampling) using the first
		/// random number. This is the inverse transform technique
		/// </remarks>
		/// <param name="randomNumbers">A list of values needed to generate the sample.</param>
		/// <returns>The sampled value.</returns>
		public virtual TotalOrder Sample(params double[] randomNumbers)
		{
			CheckSampleArgs(randomNumbers);
			return Quantile(randomNumbers[0]);
		}

		/// <summary> Samples from the distribution and returns the result as a double.
		/// </summary>
		/// <param name="randomNumbers">A list of values needed to generate the sample.</param>
		/// <returns>The sampled value.</returns>
		public sealed override double SampleDouble(params double[] randomNumbers)
		{
			try
			{
				return Convert.ToDouble(Sample(randomNumbers));
			}
			catch (InvalidCastException innerException)
			{
				throw new MsfException(Resources.InternalError, innerException);
			}
		}

		/// <summary>
		/// Checks arguments for Sample(params double[] randomNumbers) method.
		/// </summary>
		/// <param name="randomNumbers">A list of values needed to generate the sample.</param>
		protected void CheckSampleArgs(params double[] randomNumbers)
		{
			if (randomNumbers == null)
			{
				throw new ArgumentNullException("randomNumbers");
			}
			if (randomNumbers.Length != RandomNumberNeeded)
			{
				throw new ArgumentOutOfRangeException("randomNumbers", string.Format(CultureInfo.InvariantCulture, Resources.WrongNumberOfRandomNumbers01, new object[2] { RandomNumberNeeded, randomNumbers.Length }));
			}
			foreach (double num in randomNumbers)
			{
				if (double.IsNaN(num) || num < 0.0 || num > 1.0)
				{
					throw new ArgumentOutOfRangeException("randomNumbers");
				}
			}
		}
	}
}
