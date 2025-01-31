using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	[DebuggerDisplay("Lower = {_lowerBound}, Upper = {_upperBound}")]
	internal sealed class DiscreteUniformUnivariateValue : DistributedValue
	{
		private readonly int _lowerBound;

		private readonly int _upperBound;

		/// <summary> The number of possible realizations.
		/// </summary>
		/// <remarks> Int32.MaxValue for continuous distributions.
		/// </remarks>
		public override int ScenariosCount
		{
			get
			{
				if ((long)_upperBound - (long)_lowerBound + 1 > int.MaxValue)
				{
					return int.MaxValue;
				}
				return _upperBound - _lowerBound + 1;
			}
		}

		/// <summary> All possible scenarios.
		/// </summary>
		public override IEnumerable<Scenario> Scenarios
		{
			get
			{
				int scenarioCount = ScenariosCount;
				Rational probability = Rational.One / scenarioCount;
				for (int i = 0; i < scenarioCount; i++)
				{
					yield return new Scenario(probability, _lowerBound + i);
				}
			}
		}

		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="lowerBound">Lower bound (inclusive).</param>
		/// <param name="upperBound">Upper bound (inclusive).</param>
		public DiscreteUniformUnivariateValue(int lowerBound, int upperBound)
		{
			if (lowerBound > upperBound)
			{
				throw new ArgumentOutOfRangeException(Resources.LowerBoundCannotBeLargerThanUpperBound);
			}
			Distribution = new DiscreteUniformUnivariateDistribution(lowerBound, upperBound);
			_lowerBound = lowerBound;
			_upperBound = upperBound;
		}

		/// <summary> Returns a string representation of the distribution.
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.CurrentCulture, "Lower = {0}, Upper = {1}", new object[2] { _lowerBound, _upperBound });
		}
	}
}
