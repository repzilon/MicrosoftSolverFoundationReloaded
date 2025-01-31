using System;
using System.Globalization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Random sampling parameters for the SolverContext.
	/// </summary>
	/// <remarks>Sampling parameters apply to all models created using the SolverContext.
	/// Stochastic options are set using the StochasticDirective class.
	/// </remarks>
	public sealed class SamplingParameters
	{
		private const int DefaultSampleCount = 0;

		private int _sampleCount;

		private SamplingMethod _samplingMethod;

		private int _randomSeed;

		/// <summary>How many samples should be taken.
		/// Use 0 for automatic mode (Default)
		/// </summary>
		public int SampleCount
		{
			get
			{
				return _sampleCount;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				_sampleCount = value;
			}
		}

		/// <summary>Sampling method: Monte Carlo or Latin Hypercube.
		/// </summary>
		public SamplingMethod SamplingMethod
		{
			get
			{
				return _samplingMethod;
			}
			set
			{
				if (value != SamplingMethod.LatinHypercube && value != SamplingMethod.MonteCarlo && value != 0)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidSamplingMethod0, new object[1] { value }));
				}
				_samplingMethod = value;
			}
		}

		/// <summary> Random seed for sampling engine. Use 0 for automatic mode (default).
		/// </summary>
		public int RandomSeed
		{
			get
			{
				return _randomSeed;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				_randomSeed = value;
			}
		}

		/// <summary> Create a new instance.
		/// </summary>
		public SamplingParameters()
		{
			_sampleCount = 0;
			_samplingMethod = SamplingMethod.Automatic;
		}

		/// <summary>A copy ctr
		/// </summary>
		/// <param name="samplingParameters"></param>
		internal SamplingParameters(SamplingParameters samplingParameters)
		{
			_sampleCount = samplingParameters.SampleCount;
			_samplingMethod = samplingParameters.SamplingMethod;
			_randomSeed = samplingParameters.RandomSeed;
		}
	}
}
