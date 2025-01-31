using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Base class for all cutting plane generators
	/// </summary>
	internal abstract class CutBuilder
	{
		protected MipNode _node;

		protected bool _isNodeIntegerInfeasible;

		public MipNode Node => _node;

		public bool IsNodeIntegerInfeasible => _isNodeIntegerInfeasible;

		public CutBuilder(MipNode node)
		{
			_node = node;
		}

		public abstract IEnumerable<RowCut> Build();

		/// <summary> Compute the cut row value in the optimal relaxation solution.
		/// </summary>
		public Rational ComputeCutRowValue(VectorRational row)
		{
			Rational rational = 0;
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(row);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				rational += _node.GetReducedVarValue(rc) * iter.Value;
				iter.Advance();
			}
			return rational.ToDouble();
		}

		public double ComputeCutRowValue(VectorDouble row)
		{
			double num = 0.0;
			Vector<double>.Iter iter = new Vector<double>.Iter(row);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				num += _node.GetReducedVarValue(rc) * iter.Value;
				iter.Advance();
			}
			return num;
		}
	}
}
