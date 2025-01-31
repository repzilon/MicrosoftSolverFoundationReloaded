using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A variable ordering heuristic in which we branch on user-defined 
	///   variables in the order in which they are declared. 
	/// </summary>
	internal class LexVariableSelector : VariableSelector
	{
		private readonly List<DiscreteVariable> _vars;

		private Backtrackable<int> _current;

		public LexVariableSelector(TreeSearchAlgorithm algo)
			: base(algo)
		{
			Problem problem = algo.Problem;
			_current = new Backtrackable<int>(problem.IntTrail, 0);
			_vars = new List<DiscreteVariable>(problem.UserDefinedVariables);
		}

		public override DiscreteVariable DecideNextVariable()
		{
			int count = _vars.Count;
			DiscreteVariable discreteVariable;
			while (true)
			{
				int value = _current.Value;
				if (value == count)
				{
					return null;
				}
				discreteVariable = _vars[value];
				if (!discreteVariable.CheckIfInstantiated())
				{
					break;
				}
				_current.Value = value + 1;
			}
			return discreteVariable;
		}
	}
}
