using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Random distribution.
	/// </summary>
	internal abstract class DistributedValue
	{
		/// <summary> The number of possible realizations.
		/// </summary>
		/// <remarks> Int32.MaxValue for non degenerate continuous distributions.
		/// </remarks>
		public virtual int ScenariosCount
		{
			get
			{
				if (Distribution.Variance == 0.0)
				{
					return 1;
				}
				return int.MaxValue;
			}
		}

		/// <summary> The most recently sampled value.
		/// </summary>
		public virtual double CurrentSample { get; set; }

		/// <summary> All possible scenarios (if there is a finite number of them).
		/// </summary>
		/// <remarks>This is the default impl for continuous distribution</remarks>
		public virtual IEnumerable<Scenario> Scenarios
		{
			get
			{
				if (ScenariosCount == int.MaxValue)
				{
					throw new MsfException(Resources.CannotEnumerateAnInfiniteNumberOfScenarios);
				}
				yield return new Scenario(Rational.One, Distribution.Mean);
			}
		}

		/// <summary> The underlying distribution.
		/// </summary>
		public virtual UnivariateDistribution Distribution { get; protected set; }
	}
}
