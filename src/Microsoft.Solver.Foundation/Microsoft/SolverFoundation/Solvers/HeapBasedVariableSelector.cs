using System.Diagnostics;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Root class for all variable ordering heuristics based on a
	///   numerical score. The returned variable is, at every decision, the
	///   one with the lowest numerical score. 
	/// </summary>
	internal abstract class HeapBasedVariableSelector : VariableSelector
	{
		protected BinaryHeap<DiscreteVariable> _heap;

		private LookupMap<DiscreteVariable, double> _score;

		private readonly bool _ignoreNonUserDefinedVars;

		/// <summary>
		///   Construction
		/// </summary>
		/// <param name="algo">The search algorithm</param>
		/// <param name="onlyUserDefinedVars">
		///   set to true if the heuristic should ignore 
		///   variables that are not user-defined
		/// </param>
		public HeapBasedVariableSelector(TreeSearchAlgorithm algo, bool onlyUserDefinedVars)
			: base(algo)
		{
			Problem problem = algo.Problem;
			IndexedCollection<DiscreteVariable> discreteVariables = problem.DiscreteVariables;
			_heap = new BinaryHeap<DiscreteVariable>(discreteVariables);
			_score = new LookupMap<DiscreteVariable, double>(discreteVariables);
			_ignoreNonUserDefinedVars = onlyUserDefinedVars;
			problem.SubscribeToVariableRestored(WhenVariableRestored);
			foreach (DiscreteVariable item in onlyUserDefinedVars ? problem.UserDefinedVariables : discreteVariables.Enumerate())
			{
				_heap.Insert(item, double.MaxValue);
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
			return null;
		}

		/// <summary>
		///   Should be called by concrete class whenever the score
		///   associated with a variable is dynamically changing
		/// </summary>
		protected void ChangeScore(DiscreteVariable x, double newscore)
		{
			_score[x] = newscore;
			if (_heap.Contains(x))
			{
				_heap.ChangeScore(x, newscore);
			}
		}

		/// <summary>
		///   Gets the current score of the variable
		/// </summary>
		protected double Score(DiscreteVariable x)
		{
			return _score[x];
		}

		/// <summary>
		///   Reacts to the restoration of any variable
		/// </summary>
		private void WhenVariableRestored(DiscreteVariable x)
		{
			if (!IgnoreVariable(x) && !_heap.Contains(x))
			{
				_heap.Insert(x, _score[x]);
			}
		}

		/// <summary>
		///   True if the variable should never be returned by
		///   the heuristic
		/// </summary>
		private bool IgnoreVariable(DiscreteVariable x)
		{
			if (_ignoreNonUserDefinedVars)
			{
				return !_problem.IsUserDefined(x);
			}
			return false;
		}

		/// <summary>
		///   Checks that the selected variable is one of the 
		///   non-instantiated vars with minimal score
		/// </summary>
		[Conditional("DEBUG")]
		protected void CheckMinimality(DiscreteVariable x)
		{
			foreach (DiscreteVariable item in _problem.DiscreteVariables.Enumerate())
			{
				IgnoreVariable(item);
			}
		}

		/// <summary>
		///   Checks that the heap contains all non-instantiated vars
		///   at the right position
		/// </summary>
		[Conditional("DEBUG")]
		protected void CheckHeapContent()
		{
			foreach (DiscreteVariable item in _problem.DiscreteVariables.Enumerate())
			{
				if (!IgnoreVariable(item))
				{
					item.CheckIfInstantiated();
				}
			}
		}
	}
}
