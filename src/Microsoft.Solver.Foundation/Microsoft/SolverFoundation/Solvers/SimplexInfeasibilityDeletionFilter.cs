using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class SimplexInfeasibilityDeletionFilter
	{
		private SimplexTask _simplexTask;

		private bool[] _iisSet;

		private InfeasibilityReport _report;

		public SimplexInfeasibilityDeletionFilter(SimplexTask simplexTask)
		{
			if (simplexTask.AlgorithmUsed == SimplexAlgorithmKind.Primal)
			{
				_simplexTask = simplexTask;
			}
			else
			{
				_simplexTask = new SimplexTask(simplexTask.Solver, 0, new SimplexSolverParams
				{
					Algorithm = SimplexAlgorithmKind.Primal,
					UseExact = false
				}, fForceExact: false);
			}
			_iisSet = new bool[_simplexTask.Solver.RowCount];
			for (int i = 0; i < _iisSet.Length; i++)
			{
				_iisSet[i] = true;
			}
			_report = new InfeasibilityReport();
		}

		public ILinearSolverInfeasibilityReport Generate()
		{
			LinearResult linearResult = _simplexTask.RunSimplex(null, restart: false);
			if (linearResult == LinearResult.Optimal || linearResult == LinearResult.Feasible || linearResult == LinearResult.UnboundedPrimal)
			{
				return _report;
			}
			bool flag = true;
			int rowCount = _simplexTask.Solver.RowCount;
			_simplexTask.InfeasibleCount = 0;
			for (int i = 0; i < rowCount; i++)
			{
				int num = _simplexTask.Solver._mpridvid[i];
				if (!_simplexTask.Solver.IsGoal(num))
				{
					_iisSet[i] = false;
					ReloadModelWithoutRow(_iisSet);
					linearResult = _simplexTask.RunSimplex(null, restart: true);
					if (linearResult == LinearResult.Optimal || linearResult == LinearResult.Feasible || linearResult == LinearResult.UnboundedPrimal)
					{
						_iisSet[i] = true;
						_report.AppendRow(num);
						flag = false;
					}
				}
			}
			if (flag)
			{
				for (int j = 0; j < rowCount; j++)
				{
					int num2 = _simplexTask.Solver._mpridvid[j];
					if (!_simplexTask.Solver.IsGoal(num2))
					{
						_report.AppendRow(num2);
					}
				}
			}
			_simplexTask.InfeasibleCount = -1;
			return _report;
		}

		private void ReloadModelWithoutRow(bool[] rowFilter)
		{
			_simplexTask.Solver._mod = new SimplexReducedModel(_simplexTask.Solver, rowFilter);
			if (_simplexTask.Solver.IsSpecialOrderedSet)
			{
				_simplexTask.Solver._mod.BuildSOSModel();
			}
			if (_simplexTask.Params.UseDouble)
			{
				_simplexTask.Solver._mod.InitDbl(_simplexTask.Params);
			}
			_simplexTask.ReInit(SimplexBasisKind.Current);
		}
	}
}
