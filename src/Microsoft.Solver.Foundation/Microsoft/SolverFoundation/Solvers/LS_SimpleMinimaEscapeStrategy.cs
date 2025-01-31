using System.Linq;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>A restart strategy in which we restart when the
	///          number of moves without improvement hits a limit
	/// </summary>
	/// <remarks>
	/// Difficult tuning of the limit, we want to escape from minima
	/// after a while but we want to leave enough time
	/// for the exploration to have a chance to improve quality.
	/// Tuned as function of number of variables
	/// </remarks>
	internal class LS_SimpleMinimaEscapeStrategy : LS_Strategy
	{
		private long _plateauLength;

		private int _limit;

		protected override void Initialize(ILocalSearchProcess solver)
		{
			_limit = 5 * base.Model._variablesExcludingConstants.Count();
			_plateauLength = 0L;
			solver.SubscribeToMove(WhenMove);
			solver.SubscribeToRestarts(WhenRestart);
		}

		public bool Restart(ILocalSearchProcess solver)
		{
			CheckSolver(solver);
			return _plateauLength >= _limit;
		}

		private void WhenMove(LocalSearch.Move move, bool improved, bool accept)
		{
			if (improved)
			{
				_plateauLength = 0L;
			}
			else
			{
				_plateauLength++;
			}
		}

		private void WhenRestart()
		{
			_plateauLength = 0L;
		}
	}
}
