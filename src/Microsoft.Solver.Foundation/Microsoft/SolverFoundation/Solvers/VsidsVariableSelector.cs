namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A variable ordering heuristic in which we branch on one of the
	///   non-instantiated user-defined ariables whose domain has the minimal
	///   cardinality
	/// </summary>
	internal class VsidsVariableSelector : HeapBasedVariableSelector
	{
		private const double varDecay = 1.0526315789473684;

		private ImplicationGraph _graph;

		private double bump;

		private double nConflicts;

		/// <summary>
		///   Construction
		/// </summary>
		public VsidsVariableSelector(TreeSearchAlgorithm algo, int randomSeed)
			: base(algo, onlyUserDefinedVars: false)
		{
			_graph = algo.Problem.GetImplicationGraph();
			foreach (DiscreteVariable item in _problem.DiscreteVariables.Enumerate())
			{
				ChangeScore(item, 1.0);
			}
			bump = 1.0;
			nConflicts = 200.0;
			foreach (DiscreteVariable userDefinedVariable in _problem.UserDefinedVariables)
			{
				ChangeScore(userDefinedVariable, -1000.0 + bump);
				bump *= 1.0001;
			}
			bump = 2.0;
			_graph.SubscribeToExplanation(AnalyseConflict);
			_graph.SubscribeToVarIteratedOver(bumpVarScore);
		}

		/// <summary>
		/// Bump the score of a dicrete Variable
		/// </summary>
		/// <param name="x"></param>
		private void bumpVarScore(DiscreteVariable x)
		{
			ChangeScore(x, Score(x) * bump);
		}

		/// <summary>
		/// Decay all Discrete Variables by a constant (varDecay)
		/// </summary>
		private void decayVarScore()
		{
			foreach (DiscreteVariable item in _problem.DiscreteVariables.Enumerate())
			{
				ChangeScore(item, Score(item) / 1.0526315789473684);
			}
		}

		/// <summary>
		///   Undoes the latest changes when backtracking
		/// </summary>
		public void AnalyseConflict(VariableGroup cause)
		{
			foreach (DiscreteVariable variable in cause.GetVariables())
			{
				bumpVarScore(variable);
			}
			bump += 1.0;
			if (bump >= nConflicts || bump > 10000.0)
			{
				decayVarScore();
				bump = 1.0;
				nConflicts *= 1.5;
			}
		}

		/// <summary>
		///   Picks one of the non-instantiated variables
		///   whose domain has the smallest cardinality
		/// </summary>
		public override DiscreteVariable DecideNextVariable()
		{
			while (!_heap.Empty)
			{
				DiscreteVariable discreteVariable = _heap.Top();
				if (discreteVariable.CheckIfInstantiated())
				{
					_heap.Pop();
					continue;
				}
				return discreteVariable;
			}
			Unplug();
			return null;
		}

		/// <summary>
		///   Called when a first solution is found.
		///   Freezes the weights in the heap, and disconnects
		///   the implication graph. This is because conflict
		///   analysis would require solution analysis after
		///   1st solution found
		/// </summary>
		private void Unplug()
		{
			_problem.UnplugImplicationGraph();
		}
	}
}
