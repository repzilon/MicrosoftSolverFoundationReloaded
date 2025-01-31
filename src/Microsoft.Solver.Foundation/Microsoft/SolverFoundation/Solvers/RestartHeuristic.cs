using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Heuristic Adaptor that runs a heuristic but that will restart it
	///   every so often (often in the beginning, then increasingly rarely).
	///   As soon as a first solution is found we don't interrupt the heuristic
	///   anymore so that we guarantee an exact count of solutions.
	/// </summary>
	internal class RestartHeuristic : Heuristic
	{
		private long _nbfailsSinceLastRestart;

		private long _credit;

		private readonly Heuristic _heuristic;

		public RestartHeuristic(TreeSearchAlgorithm algo, Heuristic h)
			: base(algo)
		{
			_heuristic = h;
			_credit = 1L;
			_problem.SubscribeToConflicts(WhenConflict);
		}

		public override DisolverDecision NextDecision()
		{
			if (_nbfailsSinceLastRestart >= _credit)
			{
				_nbfailsSinceLastRestart = 0L;
				_credit = (long)Math.Ceiling(1.1 * (double)_credit);
				if (_treeSearch._searchStrategy.Variables == VariableEnumerationStrategy.Vsids)
				{
					_problem.ReplugImplicationGraph();
				}
				return DisolverDecision.Restart();
			}
			DisolverDecision result = _heuristic.NextDecision();
			if (result.Tag == DisolverDecision.Type.SolutionFound && !_problem.Source.HasMinimizationGoals)
			{
				_credit = long.MaxValue;
			}
			return result;
		}

		private void WhenConflict(Cause cstr)
		{
			_nbfailsSinceLastRestart++;
		}
	}
}
