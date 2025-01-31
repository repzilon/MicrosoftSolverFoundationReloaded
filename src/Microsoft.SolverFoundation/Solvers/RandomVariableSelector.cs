using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A variable ordering heuristic in which we systematically pick
	///   variables uniformly at random among the non-instantiated
	///   user-defined ones.
	/// </summary>
	internal class RandomVariableSelector : VariableSelector
	{
		private SubSet<DiscreteVariable> _nonInstantiatedVars;

		private Random _prng;

		public RandomVariableSelector(TreeSearchAlgorithm algo, int randomSeed)
			: base(algo)
		{
			Problem problem = algo.Problem;
			_prng = new Random(randomSeed);
			_nonInstantiatedVars = new SubSet<DiscreteVariable>(problem.DiscreteVariables);
			problem.SubscribeToVariableUninstantiated(WhenUninstantiated);
			foreach (DiscreteVariable userDefinedVariable in problem.UserDefinedVariables)
			{
				_nonInstantiatedVars.Add(userDefinedVariable);
			}
		}

		public override DiscreteVariable DecideNextVariable()
		{
			DiscreteVariable discreteVariable;
			while (true)
			{
				int cardinal = _nonInstantiatedVars.Cardinal;
				if (cardinal == 0)
				{
					return null;
				}
				int i = _prng.Next(0, cardinal);
				discreteVariable = _nonInstantiatedVars[i];
				if (!discreteVariable.CheckIfInstantiated())
				{
					break;
				}
				_nonInstantiatedVars.Remove(discreteVariable);
			}
			return discreteVariable;
		}

		private void WhenUninstantiated(DiscreteVariable x)
		{
			if (_problem.IsUserDefined(x))
			{
				_nonInstantiatedVars.Add(x);
			}
		}
	}
}
