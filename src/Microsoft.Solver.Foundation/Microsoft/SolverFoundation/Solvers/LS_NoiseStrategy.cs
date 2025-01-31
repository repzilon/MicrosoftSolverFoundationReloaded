namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>A strategy that takes a move and acceptance strategy 
	///          and makes then noisy
	/// </summary>
	/// <remarks>The call-backs are dispatched to the move and acceptance 
	///          strategies unless noise is made (with some probability),
	///          in which case a random flip is done and we keep state in
	///          order to accept this move
	/// </remarks>
	internal class LS_NoiseStrategy : LS_Strategy
	{
		private readonly LocalSearch.MoveStrategy _move;

		private readonly LocalSearch.AcceptanceStrategy _accept;

		private double _noiseLevel;

		private bool _noise;

		public LS_NoiseStrategy(LocalSearch.MoveStrategy move, LocalSearch.AcceptanceStrategy accept, int randomSeed, double noiseLevel)
			: base(randomSeed)
		{
			_move = move;
			_accept = accept;
			_noiseLevel = noiseLevel;
			_noise = false;
		}

		protected override void Initialize(ILocalSearchProcess solver)
		{
			solver.SubscribeToRestarts(WhenRestart);
		}

		private void WhenRestart()
		{
		}

		public LocalSearch.Move NextMove(ILocalSearchProcess solver)
		{
			CheckSolver(solver);
			if (_prng.NextDouble() < _noiseLevel)
			{
				_noise = true;
				return RandomFlip();
			}
			_noise = false;
			return _move(solver);
		}

		public bool Accept(ILocalSearchProcess solver, LocalSearch.Move move, int qualityChange, int moveCost)
		{
			CheckSolver(solver);
			if (_noise)
			{
				return true;
			}
			return _accept(solver, move, qualityChange, moveCost);
		}
	}
}
