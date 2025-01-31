using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Simple struct for (probability, value) pairs.
	/// </summary>
	[DebuggerDisplay("p = {Probability}, v = {Value}")]
	public struct Scenario
	{
		/// <summary> Probability to get the specific value
		/// </summary>
		public readonly Rational Probability;

		/// <summary> The value for this scenario.
		/// </summary>
		public readonly Rational Value;

		/// <summary>Create a new instance given the scenario probability and value.
		/// </summary>
		/// <param name="probability">Probability</param>
		/// <param name="value">Value</param>
		public Scenario(Rational probability, Rational value)
		{
			if (!DistributionUtilities.IsNonzeroProbability(probability))
			{
				throw new ArgumentOutOfRangeException("probability", Resources.ProbabilityShouldBeMoreThanZeroAndLessOrEqualToOne);
			}
			if (!IsValidScenarioValue(value))
			{
				throw new ArgumentOutOfRangeException("value");
			}
			Probability = probability;
			Value = value;
		}

		/// <summary>Determine whether a value is valid for a scenario.
		/// </summary>
		/// <param name="value">The scenario value.</param>
		/// <returns>Returns true if it is valid.</returns>
		public static bool IsValidScenarioValue(Rational value)
		{
			if (!value.IsInfinite)
			{
				return !value.IsIndeterminate;
			}
			return false;
		}

		/// <summary> Returns a string representation of the distribution.
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.CurrentCulture, "p = {0}, v = {1}", new object[2] { Probability, Value });
		}

		/// <summary>Determine whether two scenarios are equal.
		/// Scenarios are equal if both their probability and value are equal.
		/// </summary>
		/// <param name="obj">object to compare to</param>
		/// <returns>True if equal, false otherwise</returns>
		public override bool Equals(object obj)
		{
			if (!(obj is Scenario scenario))
			{
				return false;
			}
			if (Probability.Equals(scenario.Probability))
			{
				return Value.Equals(scenario.Value);
			}
			return false;
		}

		/// <summary>Determine whether two scenarios are equal.
		/// Scenarios are equal if both their probability and value are equal.
		/// </summary>
		/// <param name="first">First scenario</param>
		/// <param name="second">Second scenario</param>
		/// <returns>True if equal, false otherwise</returns>
		public static bool operator ==(Scenario first, Scenario second)
		{
			return first.Equals(second);
		}

		/// <summary>Determine whether two scenarios are not equal.
		/// Scenarios are equal if both their probability and value are equal.
		/// </summary>
		/// <param name="first">First scenario</param>
		/// <param name="second">Second scenario</param>
		/// <returns>True if not equal, false otherwise</returns>
		public static bool operator !=(Scenario first, Scenario second)
		{
			return !first.Equals(second);
		}

		/// <summary>Return hash code for the scenario
		/// </summary>
		public override int GetHashCode()
		{
			return Statics.CombineHash(Probability.GetHashCode(), Value.GetHashCode());
		}
	}
}
