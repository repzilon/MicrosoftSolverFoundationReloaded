using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>A local search acceptance strategy in which the probability
	///          to accept non-improving moves varies with time using a
	///          "temperature" (metropolis) policy
	/// </summary>
	internal class LS_SimulatedAnnealingAcceptanceStrategy : LS_Strategy
	{
		private const double _initialTemperature = 100.0;

		private double _temperature;

		public LS_SimulatedAnnealingAcceptanceStrategy(int randomSeed)
			: base(randomSeed)
		{
		}

		protected override void Initialize(ILocalSearchProcess solver)
		{
			_temperature = 100.0;
			solver.SubscribeToRestarts(OnRestart);
		}

		private void OnRestart()
		{
			_temperature = 100.0;
		}

		public bool Accept(ILocalSearchProcess solver, LocalSearch.Move change, int qualityChange, int flipCost)
		{
			CheckSolver(solver);
			bool flag = qualityChange <= 0;
			if (!flag)
			{
				double val = Math.Exp((0.0 - (double)qualityChange) / _temperature);
				val = Math.Min(0.01, Math.Max(0.5, val));
				flag = _prng.NextDouble() < val;
			}
			double num = _temperature * 0.99;
			if (1E-06 <= num && num < _temperature)
			{
				_temperature = num;
			}
			else
			{
				_temperature = 100.0;
			}
			return flag;
		}
	}
}
