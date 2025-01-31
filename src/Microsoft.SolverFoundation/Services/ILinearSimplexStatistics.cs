using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// This interface provides statistical information specific to an attempt by the SimplexSolver to solve a linear model.
	/// </summary>
	public interface ILinearSimplexStatistics
	{
		/// <summary>
		/// The InnerIndexCount property returns the number of user and row variables used 
		/// internally when solving the linear model. This may be less than ILinearModel.KeyCount since variables may be eliminated by presolve
		/// </summary>
		int InnerIndexCount { get; }

		/// <summary>
		/// The InnerIntegerIndexCount property returns the number of integer user 
		/// and row variables used internally when solving the linear model. 
		/// This may be less than ILinearModel.IntegerIndexCount since variables may be eliminated by presolve
		/// </summary>
		int InnerIntegerIndexCount { get; }

		/// <summary>
		/// The InnerSlackCount property returns the number of row variables used internally when 
		/// solving the linear model. This may be less than the ILinearModel.RowCount, since row variables may be eliminated by presolve.
		/// </summary>
		int InnerSlackCount { get; }

		/// <summary>
		/// The InnerRowCount property returns the number of rows used internally when solving the linear model. 
		/// This may be less than ILinearModel.RowCount since rows may be eliminated by presolve.
		/// </summary>
		int InnerRowCount { get; }

		/// <summary>
		/// The pivot count properties indicate the number of simplex pivots performed. Generally these include both major and minor pivots.
		/// </summary>
		int PivotCount { get; }

		/// <summary>
		/// The pivot count of degenerated pivots
		/// </summary>
		int PivotCountDegenerate { get; }

		/// <summary>
		/// the pviot count of exact arithmetic
		/// </summary>
		int PivotCountExact { get; }

		/// <summary>
		/// the phase I pviot count of exact arithmetic
		/// </summary>
		int PivotCountExactPhaseOne { get; }

		/// <summary>
		/// the phase II pviot count of exact arithmetic
		/// </summary>
		int PivotCountExactPhaseTwo { get; }

		/// <summary>
		/// the pviot count of double arithmetic
		/// </summary>
		int PivotCountDouble { get; }

		/// <summary>
		/// the phase I pviot count of double arithmetic
		/// </summary>
		int PivotCountDoublePhaseOne { get; }

		/// <summary>
		/// the phase II pviot count of double arithmetic
		/// </summary>
		int PivotCountDoublePhaseTwo { get; }

		/// <summary>
		/// The factor count properties indicate the number of basis matrix LU factorizations performed.
		/// </summary>
		int FactorCount { get; }

		/// <summary>
		/// The factor count of exact arithmetic 
		/// </summary>
		int FactorCountExact { get; }

		/// <summary>
		/// The factor count of double arithmetic 
		/// </summary>
		int FactorCountDouble { get; }

		/// <summary>
		/// The BranchCount property indicates the number of branches performed when applying the branch and bound algorithm to a MILP. 
		/// If the model has no integer variables, this will be zero.
		/// </summary>
		int BranchCount { get; }

		/// <summary>
		/// Used by MIP to indicate the difference between an integer solution to a relaxed solution
		/// </summary>
		Rational Gap { get; }

		/// <summary>
		/// indicate whether the solve attempt was instructed to use exact arithmetic
		/// </summary>
		bool UseExact { get; }

		/// <summary>
		/// indicate whether the solve attempt was instructed to use double arithmetic
		/// </summary>
		bool UseDouble { get; }

		/// <summary>
		/// indicates which algorithm was used to by the solver
		/// </summary>
		SimplexAlgorithmKind AlgorithmUsed { get; }

		/// <summary>
		/// Costing used for exact arithmetic 
		/// </summary>
		SimplexCosting CostingUsedExact { get; }

		/// <summary>
		/// costing used for double arithmetic 
		/// </summary>
		SimplexCosting CostingUsedDouble { get; }
	}
}
