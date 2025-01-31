using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Stores stochastic solution information.
	/// </summary>
	public class StochasticSolution
	{
		/// <summary>Number of independent scenarios in the problem.
		/// (Int32.MaxValue means there are an infinite number.)
		/// </summary>
		public int ScenarioCount { get; internal set; }

		/// <summary>Gets the sampling method (or NoSampling).
		/// </summary>
		public SamplingMethod SamplingMethod { get; internal set; }

		/// <summary>Number of samples.
		/// </summary>
		public int SampleCount { get; internal set; }

		/// <summary>Seed for random number generator
		/// </summary>
		internal int RandomSeed { get; set; }

		/// <summary>The number of decomposition iterations (0 if decomposition was not used).
		/// </summary>
		[Obsolete]
		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		public int DecompositionIterations => 0;

		/// <summary>Average gap of the last iteration of the decomposition
		/// </summary>
		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		[Obsolete]
		public double DecompositionGap => double.NaN;

		/// <summary>Objective of expected value problem.
		/// </summary>
		[Obsolete]
		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		public Rational ExpectedValue
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>Expected value of all wait-and-see problems.
		/// </summary>
		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		[Obsolete]
		public Rational WaitAndSee
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>Expected result of using the ExpectedValue solution
		/// </summary>
		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		[Obsolete]
		public Rational ExpectedResultOfExpectedValue
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>Creates a new instance.
		/// </summary>
		internal StochasticSolution(ScenarioGenerator scenariosGenerator)
		{
			if (scenariosGenerator == null)
			{
				throw new ArgumentNullException("scenariosGenerator");
			}
			ScenarioCount = scenariosGenerator.ScenarioCount;
			SampleCount = scenariosGenerator.SampleCount;
			SamplingMethod = scenariosGenerator.SamplingMethod;
			RandomSeed = scenariosGenerator.RandomSeed;
		}
	}
}
