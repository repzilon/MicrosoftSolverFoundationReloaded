using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A move strategy that uses a Tabu list, i.e. has a short-term
	///           memory of forbidden moves. At every step the strategy tries
	///           to find a good move that is not marked Tabu
	/// </summary>
	/// <remarks> Here the Tabu list contains variables
	///           that cannot be flipped. The algorithm tries to find a variable 
	///           flip that improves the quality and is not Tabu. 
	/// </remarks>
	internal class LS_TabuMoveStrategy : LS_Strategy
	{
		/// <summary> The tabu list. For each variable (indexed by their ordinals)
		///           we keep the time step up to which it remains Tabu
		/// </summary>
		private int[] _tabuList;

		/// <summary> Length (conceptually) of the Tabu queue; i.e. number of time
		///           steps a value remains Tabu
		/// </summary>
		private int _tabuLength;

		/// <summary> Counter of number of moves performed
		/// </summary>
		private int _currentTimeStep;

		public LS_TabuMoveStrategy(int randomSeed, int tabuLength)
			: base(randomSeed)
		{
			_tabuLength = tabuLength;
		}

		public LS_TabuMoveStrategy(int randomSeed)
			: base(randomSeed)
		{
			_tabuLength = -1;
		}

		protected override void Initialize(ILocalSearchProcess solver)
		{
			int num = base.Model.AllTerms.Count();
			_tabuList = new int[num];
			_currentTimeStep = 0;
			solver.SubscribeToRestarts(WhenRestart);
			if (_tabuLength < 0)
			{
				_tabuLength = BooleanFunction.ScaleDown(num);
			}
		}

		private void WhenRestart()
		{
			Array.Clear(_tabuList, 0, _tabuList.Length);
			_currentTimeStep = 0;
		}

		/// <summary>Computes the next move considered by the local search solver
		/// </summary>
		/// <remarks>The move selection is doing a exhaustive search through
		///          the variables and skips the ones that are Tabu; it samples
		///          a number of values for each of them and keeps the best move.
		///          The algorithm is essentially naive but we use the following
		///          refinements to avoid a too costly loop:
		///          (1) the search is interrupted as soon as we have found a
		///              non-Tabu variable which stricltly improves the penalty function
		///          (2) the enumeration is in order starting from a initial position.
		///              By default this initial position is random but when we can
		///              we try to find a slightly better-informed choice, which is
		///              likely to directly find a satisfying move
		/// </remarks>
		public LocalSearch.Move NextMove(ILocalSearchProcess solver)
		{
			CheckSolver(solver);
			_currentTimeStep++;
			List<CspTerm> allTerms = base.Model.AllTerms;
			int count = allTerms.Count;
			int num = StartPosition(count);
			CspVariable cspVariable = null;
			int num2 = int.MaxValue;
			int value = int.MinValue;
			for (int i = 0; i < count; i++)
			{
				int index = (num + i) % count;
				if (!(allTerms[index] is CspVariable cspVariable2) || IsTabu(cspVariable2))
				{
					continue;
				}
				int currentIntegerValue = solver.GetCurrentIntegerValue(cspVariable2);
				foreach (int item in LargeSample(cspVariable2))
				{
					if (solver.Model.CheckAbort())
					{
						return RandomFlip();
					}
					if (item != currentIntegerValue)
					{
						int num3 = solver.EvaluateFlip(cspVariable2, item);
						if (num3 < num2)
						{
							num2 = num3;
							value = item;
							cspVariable = cspVariable2;
						}
					}
				}
				if (num2 < 0)
				{
					break;
				}
			}
			if (cspVariable == null)
			{
				int index2 = num % count;
				cspVariable = allTerms[index2] as CspVariable;
				using (IEnumerator<int> enumerator2 = LargeSample(cspVariable).GetEnumerator())
				{
					if (enumerator2.MoveNext())
					{
						int current2 = enumerator2.Current;
						value = current2;
					}
				}
			}
			MarkTabu(cspVariable);
			return LocalSearch.CreateVariableFlip(cspVariable, value);
		}

		private int StartPosition(int size)
		{
			if (base.Solver.CurrentViolation > 0)
			{
				for (int i = 0; i < 5; i++)
				{
					CspTerm key = base.Solver.PickViolatedVariable(_prng).Key;
					CspVariable cspVariable = key as CspVariable;
					if (!IsTabu(cspVariable))
					{
						return cspVariable.Ordinal;
					}
				}
			}
			List<CspVariable> variablesExcludingConstants = base.Model._variablesExcludingConstants;
			return variablesExcludingConstants[_prng.Next(variablesExcludingConstants.Count)].Ordinal;
		}

		/// <summary> Mark the variable as Tabu - it should be be flipped
		///           for _tabuLength steps
		/// </summary>
		private void MarkTabu(CspVariable x)
		{
			_tabuList[x.Ordinal] = _currentTimeStep + _tabuLength;
		}

		/// <summary> True if the variable is currently Tabu, i.e. 
		///           it is currently forbidden to flip it
		/// </summary>
		private bool IsTabu(CspVariable x)
		{
			return _currentTimeStep <= _tabuList[x.Ordinal];
		}
	}
}
