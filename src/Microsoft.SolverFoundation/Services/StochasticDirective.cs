using System;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Controls stochastic solution settings.
	/// </summary>
	/// <remarks>
	/// The stochastic directive is suitable for linear models that contain recourse
	/// decisions and random parameters.
	/// Sampling options are set using the SolverContext.SamplingParameters property.
	/// </remarks>
	public class StochasticDirective : Directive
	{
		private int _maximumScenarioCountBeforeSampling;

		private DecompositionType _decompositionType;

		/// <summary>
		/// When there are more than MaximumScenarioCountBeforeSampling scenarios,
		/// sampling will be used instead of enumeration.
		/// Use -1 for automatic mode (Default)
		/// </summary>
		/// <remarks>
		/// </remarks>
		internal int MaximumScenarioCountBeforeSampling
		{
			get
			{
				return _maximumScenarioCountBeforeSampling;
			}
			set
			{
				if (value < -1)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				_maximumScenarioCountBeforeSampling = value;
			}
		}

		/// <summary>Whether to use decomposition or the deterministic equivalent.
		/// </summary>
		public DecompositionType DecompositionType
		{
			get
			{
				return _decompositionType;
			}
			set
			{
				_decompositionType = value;
			}
		}

		/// <summary>Create a new instance.
		/// </summary>
		public StochasticDirective()
		{
			_maximumScenarioCountBeforeSampling = -1;
		}

		/// <summary>
		/// Returns a representation of the directive as a string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Concat("Stochastic(DecompositionType = ", DecompositionType, ", MaximumScenarioCountBeforeSampling = ", MaximumScenarioCountBeforeSampling, ")");
		}
	}
}
