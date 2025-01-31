using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A variable ordering heuristic in which we returns non-instantiated
	///   variables in a cyclic way
	/// </summary>
	/// <remarks>
	///   In current version only user-defined variables are branched on
	/// </remarks>
	internal class RoundRobinVariableSelector : VariableSelector
	{
		private Queue<DiscreteVariable> _queue;

		private SubSet<DiscreteVariable> _enqueuedVars;

		public RoundRobinVariableSelector(TreeSearchAlgorithm algo)
			: base(algo)
		{
			Problem problem = algo.Problem;
			_queue = new Queue<DiscreteVariable>();
			_enqueuedVars = new SubSet<DiscreteVariable>(problem.DiscreteVariables);
			problem.SubscribeToVariableUninstantiated(WhenVariableUninstantiated);
			foreach (DiscreteVariable item in problem.DiscreteVariables.Enumerate())
			{
				_enqueuedVars.Add(item);
				if (problem.IsUserDefined(item))
				{
					_queue.Enqueue(item);
				}
			}
		}

		public override DiscreteVariable DecideNextVariable()
		{
			DiscreteVariable discreteVariable;
			while (true)
			{
				if (_queue.Count == 0)
				{
					return null;
				}
				discreteVariable = _queue.Peek();
				if (!discreteVariable.CheckIfInstantiated())
				{
					break;
				}
				_queue.Dequeue();
				_enqueuedVars.Remove(discreteVariable);
			}
			return discreteVariable;
		}

		public void WhenVariableUninstantiated(DiscreteVariable x)
		{
			if (!_enqueuedVars.Contains(x))
			{
				_queue.Enqueue(x);
				_enqueuedVars.Add(x);
			}
		}
	}
}
