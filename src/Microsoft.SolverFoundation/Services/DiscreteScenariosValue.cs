using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Distribution given by a fixed number of scenarios.
	/// </summary>
	internal sealed class DiscreteScenariosValue : DistributedValue
	{
		public static readonly double RoundoffTolerance = 0.0001;

		private readonly ValueTable<Scenario> _table = ValueTable<Scenario>.Create(null, ValueSet.Create(Domain.IntegerNonnegative));

		private Rational _probabilitySum;

		private int _scenariosCount;

		/// <summary> The number of possible realizations.
		/// </summary>
		/// <remarks> Int32.MaxValue for continuous distributions.
		/// </remarks>
		public override int ScenariosCount => _scenariosCount;

		/// <summary> All possible scenarios.
		/// </summary>
		public override IEnumerable<Scenario> Scenarios
		{
			get
			{
				for (int i = 0; i < _scenariosCount; i++)
				{
					_table.TryGetValue(out var result, i);
					yield return result;
				}
			}
		}

		/// <summary> Add a scenario to the distributed value.
		/// </summary>
		/// <param name="scenario"></param>
		internal void AddScenario(Scenario scenario)
		{
			AddScenario(scenario.Probability, scenario.Value);
		}

		/// <summary> Add a scenario to the distributed value.
		/// </summary>
		internal void AddScenario(Rational probability, Rational value)
		{
			Scenario value2 = new Scenario(probability, value);
			_table.Add(value2, _scenariosCount++);
			_probabilitySum += probability;
			if (DistributionUtilities.GreaterThanOne(_probabilitySum, RoundoffTolerance))
			{
				throw new ModelException(Resources.SumOfAllProbabilitiesShouldNotExceedOne);
			}
		}

		/// <summary>Validates the sum of probabilities is unity.
		/// </summary>
		/// <remarks> Should be called after all scenarios are provided.
		/// shahark: Side effect - The underlying distribution will be initiated here
		/// </remarks>
		internal void ValidateScenarios()
		{
			if (!DistributionUtilities.EqualsOne(_probabilitySum, RoundoffTolerance))
			{
				throw new ModelException(Resources.ProbabilitiesShouldSumupToOne);
			}
			Distribution = new ScenariosDistribution(Scenarios);
		}

		/// <summary> Returns a string representation of the distribution.
		/// </summary>
		public override string ToString()
		{
			return ToString(5);
		}

		/// <summary> Returns a string representation of the first maxCount scenarios.
		/// </summary>
		public string ToString(int maxCount)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "Count = {0}, [", new object[1] { _scenariosCount }));
			int num = 1;
			foreach (Scenario scenario in Scenarios)
			{
				if (num > 1)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "[{0}]: {1}", new object[2] { num, scenario }));
				if (num >= maxCount)
				{
					stringBuilder.Append(", ...");
					break;
				}
				num++;
			}
			stringBuilder.AppendLine("]");
			return stringBuilder.ToString();
		}
	}
}
