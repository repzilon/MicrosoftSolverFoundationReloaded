using System;
using System.Globalization;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Terminates when the geometrically-weighted average improvement falls below the tolerance
	/// </summary>
	internal class MeanImprovementCriterion : TerminationCriterion
	{
		private const double MinLambda = 0.1;

		private const double MaxLambda = 0.6;

		private readonly double _lambda;

		private double _unnormMeanImprovement;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.SolverFoundation.Solvers.MeanImprovementCriterion" /> class.
		/// </summary>
		/// <param name="tol">The tolerance parameter</param>
		/// <param name="lambda">The geometric weighting factor.  
		/// Higher means more heavily weighted toward older values.
		/// Currenty is not an input from the user
		/// </param>
		public MeanImprovementCriterion(double tol, double lambda)
			: base(tol)
		{
			_lambda = lambda;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.SolverFoundation.Solvers.MeanImprovementCriterion" /> class.
		/// </summary>
		/// <param name="tol">The tolerance parameter</param>
		public MeanImprovementCriterion(double tol)
			: this(tol, 0.1)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.SolverFoundation.Solvers.MeanImprovementCriterion" /> class.
		/// </summary>
		public MeanImprovementCriterion()
			: this(0.0001)
		{
		}

		/// <summary>
		/// Determines whether to stop optimization
		/// criterion(n) = z * unNorm(n)
		/// unNorm(n) = delta(n) + lambda*unNorm(n-1)
		/// </summary>
		/// <param name="state">the state of the optimizer</param>
		/// <returns>
		/// true iff criterion is met, i.e. optimization should halt
		/// </returns>
		public override bool CriterionMet(CompactQuasiNewtonSolverState state)
		{
			double num = state.LastValue - state.Value;
			num /= 1.0 + Math.Abs(state.Value);
			_unnormMeanImprovement = num + _lambda * _unnormMeanImprovement;
			if (state.Iter < 5)
			{
				return false;
			}
			double num2 = (Math.Pow(_lambda, state.Iter + 1) - 1.0) / (_lambda - 1.0);
			return (_currentTolerance = _unnormMeanImprovement / num2) < _tolerance;
		}

		public override string ToString()
		{
			if (_currentTolerance == 0.0)
			{
				return "********";
			}
			return _currentTolerance.ToString("0.0000e+00", CultureInfo.InvariantCulture);
		}
	}
}
