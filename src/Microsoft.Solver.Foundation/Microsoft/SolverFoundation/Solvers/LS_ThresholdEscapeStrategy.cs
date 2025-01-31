using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>A restart strategy in which we restart when the 
	///          percentage of recent moves that lead to improvements is
	///          below a certain threshold
	/// </summary>
	/// <remarks>In this version we keep a record of the N last attempted moves,
	///          where N is the number of variables. Information on these moves
	///          is simply kept in a queue of bounded size. From this we can
	///          determine the percentage of successful moves over the time window.
	///          We simply restart if this percentage is less than a threshold.
	///          Note that this strategy will never restart until the queue has
	///          reached the correct size. This makes sure that even if we restart
	///          requently we leave some time to each restart
	/// </remarks>
	internal class LS_ThresholdEscapeStrategy : LS_Strategy
	{
		private Queue<bool> _lastMoves;

		private int _horizon;

		private double _nbPositive;

		private double _nbNegative;

		private readonly double _threshold;

		private double PercentageSuccessfulMoves => _nbPositive / (double)_horizon;

		public LS_ThresholdEscapeStrategy(double percentage)
		{
			_threshold = percentage;
		}

		protected override void Initialize(ILocalSearchProcess solver)
		{
			_horizon = Math.Max(100, base.Model._variablesExcludingConstants.Count());
			_lastMoves = new Queue<bool>(_horizon);
			solver.SubscribeToMove(WhenMove);
			solver.SubscribeToRestarts(WhenRestart);
		}

		public bool Restart(ILocalSearchProcess solver)
		{
			CheckSolver(solver);
			if (_lastMoves.Count < _horizon)
			{
				return false;
			}
			return PercentageSuccessfulMoves < _threshold;
		}

		private void WhenMove(LocalSearch.Move move, bool improved, bool accept)
		{
			Enqueue(improved);
			if (_lastMoves.Count > _horizon)
			{
				Dequeue();
			}
		}

		private void WhenRestart()
		{
			_lastMoves.Clear();
			_nbPositive = 0.0;
			_nbNegative = 0.0;
		}

		private void Enqueue(bool res)
		{
			_lastMoves.Enqueue(res);
			if (res)
			{
				_nbPositive += 1.0;
			}
			else
			{
				_nbNegative += 1.0;
			}
		}

		private void Dequeue()
		{
			if (_lastMoves.Dequeue())
			{
				_nbPositive -= 1.0;
			}
			else
			{
				_nbNegative -= 1.0;
			}
		}

		[Conditional("DEBUG")]
		private void ChecInvariant()
		{
		}
	}
}
