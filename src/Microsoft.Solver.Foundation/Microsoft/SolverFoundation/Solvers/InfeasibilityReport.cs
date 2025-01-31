using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class InfeasibilityReport : ILinearSolverInfeasibilityReport, ILinearSolverReport
	{
		private List<int> _iis;

		public IEnumerable<int> IrreducibleInfeasibleSet
		{
			get
			{
				foreach (int ii in _iis)
				{
					yield return ii;
				}
			}
		}

		public InfeasibilityReport()
		{
			_iis = new List<int>();
		}

		public void AppendRow(int vidRow)
		{
			_iis.Add(vidRow);
		}
	}
}
