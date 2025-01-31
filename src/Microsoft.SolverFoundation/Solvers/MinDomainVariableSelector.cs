using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A variable ordering heuristic in which we branch on one of the
	///   non-instantiated user-defined ariables whose domain has the minimal
	///   cardinality
	/// </summary>
	internal class MinDomainVariableSelector : HeapBasedVariableSelector
	{
		private Random _prng;

		/// <summary>
		///   Construction
		/// </summary>
		public MinDomainVariableSelector(TreeSearchAlgorithm prob, int randomSeed)
			: base(prob, onlyUserDefinedVars: true)
		{
			_prng = new Random(randomSeed);
			_problem.SubscribeToVariablePropagated(WhenDomainChanges);
			_problem.SubscribeToVariableRestored(WhenDomainChanges);
			foreach (DiscreteVariable userDefinedVariable in _problem.UserDefinedVariables)
			{
				ChangeScore(userDefinedVariable, userDefinedVariable.DomainSize);
			}
		}

		private void WhenDomainChanges(DiscreteVariable x)
		{
			if (_problem.IsUserDefined(x))
			{
				ChangeScore(x, (double)x.DomainSize + RandomPerturbation());
			}
		}

		/// <summary>
		///   Creates a random pertubation between 0.0 and 1.0, excluded.
		///   This will add some randomness to the choice of variables
		///   with identical domain sizes.
		/// </summary>
		private double RandomPerturbation()
		{
			double num = _prng.Next(0, 90);
			return num / 100.0;
		}
	}
}
