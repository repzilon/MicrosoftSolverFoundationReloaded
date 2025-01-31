using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A discrete uniformly distributed random parameter.
	/// </summary>
	/// <remarks>
	/// A discrete uniform distribution is defined by an upper and lower
	/// bound.  The integer values within the (closed) interval occur with equal probability.  The upper
	/// and lower bounds must be finite.
	/// </remarks>
	public sealed class DiscreteUniformDistributionParameter : UniformDistributionParameter
	{
		/// <summary>Creates a new discrete uniform distribution.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		public DiscreteUniformDistributionParameter(string name)
			: base(name)
		{
		}

		/// <summary>Creates a new discrete uniform distribution.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="lowerBound">The lower bound (inclusive).</param>
		/// <param name="upperBound">The upper bound (inclusive).</param>
		public DiscreteUniformDistributionParameter(string name, double lowerBound, double upperBound)
			: this(name, (int)lowerBound, (int)upperBound)
		{
			if (!IsInt32(lowerBound) || !IsInt32(upperBound))
			{
				throw new InvalidModelDataException(Resources.BoundsForDiscreteUniformDistriutionNeedsToBeIntegers);
			}
		}

		/// <summary>Creates a new discrete uniform distribution.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="lowerBound">The lower bound (inclusive).</param>
		/// <param name="upperBound">The upper bound (inclusive).</param>
		public DiscreteUniformDistributionParameter(string name, int lowerBound, int upperBound)
			: base(name)
		{
			base.ValueTable = ValueTable<DistributedValue>.Create(_domain);
			DiscreteUniformUnivariateValue value;
			try
			{
				value = new DiscreteUniformUnivariateValue(lowerBound, upperBound);
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw new InvalidModelDataException(Resources.InvalidArgumentsForUniformDistribution, innerException);
			}
			base.ValueTable.Add(value);
		}

		/// <summary>Creates a new discrete uniform distribution.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="indexSets">The index sets to use. Omit to create a scalar parameter.</param>
		public DiscreteUniformDistributionParameter(string name, params Set[] indexSets)
			: base(name, indexSets)
		{
		}

		private DiscreteUniformDistributionParameter(string baseName, UniformDistributionParameter source)
			: base(baseName, source)
		{
		}

		internal override Term Clone(string baseName)
		{
			return new DiscreteUniformDistributionParameter(baseName, this);
		}

		/// <summary>Determine if a Double represents a valid Int32.
		/// </summary>
		private static bool IsInt32(double number)
		{
			if (number >= -2147483648.0 && Math.Floor(number) == number)
			{
				return number <= 2147483647.0;
			}
			return false;
		}

		internal override DistributedValue GetTableValue(double dLowerBound, double dUpperBound)
		{
			if (!IsInt32(dLowerBound) || !IsInt32(dUpperBound))
			{
				throw new InvalidModelDataException(Resources.BoundsForDiscreteUniformDistriutionNeedsToBeIntegers);
			}
			int lowerBound = (int)dLowerBound;
			int upperBound = (int)dUpperBound;
			try
			{
				return new DiscreteUniformUnivariateValue(lowerBound, upperBound);
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw new InvalidModelDataException(Resources.InvalidArgumentsForUniformDistribution, innerException);
			}
		}
	}
}
