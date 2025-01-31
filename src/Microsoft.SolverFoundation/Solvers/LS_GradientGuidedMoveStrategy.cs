using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>A Local Search Move Strategy in which we select the best
	///          move in a gradient-driven way
	/// </summary>
	/// <remarks> RENAME AS TABU????????????
	///           e.g. if old Tabu deprecated
	/// </remarks>
	internal class LS_GradientGuidedMoveStrategy : LS_Strategy
	{
		private Queue<CspTerm> _tabu;

		private int _maxTabuLength;

		public LS_GradientGuidedMoveStrategy(int randomSeed)
			: base(randomSeed)
		{
		}

		protected override void Initialize(ILocalSearchProcess solver)
		{
			int num = base.Model._variablesExcludingConstants.Count();
			_maxTabuLength = num / 15;
			_maxTabuLength = Math.Max(1, _maxTabuLength);
			_maxTabuLength = Math.Min(50, _maxTabuLength);
			_tabu = new Queue<CspTerm>(_maxTabuLength + 1);
			solver.SubscribeToRestarts(WhenRestart);
		}

		public LocalSearch.Move NextMove(ILocalSearchProcess S)
		{
			CheckSolver(S);
			CspTerm cspTerm = SelectVar();
			int currentIntegerValue = base.Solver.GetCurrentIntegerValue(cspTerm);
			int value = currentIntegerValue;
			int num = int.MaxValue;
			foreach (int item in LargeSample(cspTerm as CspSolverTerm))
			{
				if (S.Model.CheckAbort())
				{
					return RandomFlip();
				}
				if (item != currentIntegerValue)
				{
					int num2 = base.Solver.EvaluateFlip(cspTerm, item);
					if (num2 < num)
					{
						value = item;
						num = num2;
					}
				}
			}
			if (num <= 0)
			{
				return LocalSearch.CreateVariableFlip(cspTerm, value);
			}
			return RandomFlip();
		}

		private void WhenRestart()
		{
			while (_tabu.Count > 0)
			{
				CspTerm variable = _tabu.Dequeue();
				base.Solver.Unfilter(variable);
			}
		}

		private CspTerm SelectVar()
		{
			CspTerm cspTerm = base.Solver.SelectBestVariable();
			if (!base.Solver.IsFiltered(cspTerm))
			{
				base.Solver.Filter(cspTerm);
				_tabu.Enqueue(cspTerm);
			}
			if (_tabu.Count > _maxTabuLength)
			{
				CspTerm variable = _tabu.Dequeue();
				base.Solver.Unfilter(variable);
			}
			return cspTerm;
		}
	}
}
