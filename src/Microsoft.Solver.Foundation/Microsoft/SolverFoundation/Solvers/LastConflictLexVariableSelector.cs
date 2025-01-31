using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A variable ordering heuristic in which we branch on variables 
	///   that appear in the most recent conflict first, and if there
	///   is no conflict we branch according to LEX
	/// </summary>
	internal class LastConflictLexVariableSelector : VariableSelector
	{
		private readonly List<DiscreteVariable> _vars;

		private Backtrackable<int> _current;

		private Queue<DiscreteVariable> _lastConflictsQueue;

		private ImplicationGraph _graph;

		public LastConflictLexVariableSelector(TreeSearchAlgorithm algo)
			: base(algo)
		{
			Problem problem = algo.Problem;
			_current = new Backtrackable<int>(problem.IntTrail, 0);
			_vars = new List<DiscreteVariable>(problem.UserDefinedVariables);
			_lastConflictsQueue = new Queue<DiscreteVariable>();
			_graph = algo.Problem.GetImplicationGraph();
			_graph.SubscribeToExplanation(ProcessConflict);
		}

		public override DiscreteVariable DecideNextVariable()
		{
			int count = _vars.Count;
			while (_lastConflictsQueue.Count > 0)
			{
				DiscreteVariable discreteVariable = _lastConflictsQueue.Dequeue();
				if (!discreteVariable.CheckIfInstantiated())
				{
					return discreteVariable;
				}
			}
			DiscreteVariable discreteVariable2;
			while (true)
			{
				int value = _current.Value;
				if (value == count)
				{
					Unplug();
					_lastConflictsQueue.Clear();
					return null;
				}
				discreteVariable2 = _vars[value];
				if (!discreteVariable2.CheckIfInstantiated())
				{
					break;
				}
				_current.Value = value + 1;
			}
			return discreteVariable2;
		}

		/// <summary>
		///   Adds the variables that are contained in the conflict to the queue
		/// </summary>
		public void ProcessConflict(VariableGroup cause)
		{
			if (_lastConflictsQueue.Count > 20)
			{
				_lastConflictsQueue.Clear();
			}
			if (cause.Length < 13)
			{
				foreach (DiscreteVariable variable in cause.GetVariables())
				{
					if (!_lastConflictsQueue.Contains(variable))
					{
						_lastConflictsQueue.Enqueue(variable);
					}
				}
				return;
			}
			long num = 0L;
			bool flag = false;
			int num2 = 0;
			DiscreteVariable discreteVariable = null;
			foreach (DiscreteVariable variable2 in cause.GetVariables())
			{
				if (variable2.DomainSize > 10)
				{
					if (!_lastConflictsQueue.Contains(variable2) && num2 < 10)
					{
						_lastConflictsQueue.Enqueue(variable2);
						num2++;
						flag = true;
					}
				}
				else if (!_lastConflictsQueue.Contains(variable2) && variable2.DomainSize > num)
				{
					discreteVariable = variable2;
					num = discreteVariable.DomainSize;
				}
			}
			if (!flag && discreteVariable != null)
			{
				_lastConflictsQueue.Enqueue(discreteVariable);
			}
		}

		/// <summary>
		///   Called when a first solution is found.
		///   Disconnects the implication graph. This is because conflict
		///   analysis would require solution analysis after
		///   1st solution found
		/// </summary>
		private void Unplug()
		{
			if (_graph != null)
			{
				_problem.UnplugImplicationGraph();
				_graph = null;
			}
		}
	}
}
