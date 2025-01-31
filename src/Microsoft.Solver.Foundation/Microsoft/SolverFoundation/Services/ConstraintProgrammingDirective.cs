using System.Collections.ObjectModel;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A directive for the constraint programming (CSP) solver.
	/// </summary>
	/// <remarks>
	/// The CSP solver is suitable for models that involve finite discrete domains and
	/// combinatorial constraints.
	/// In the case of a model with no goals, the constraint programming solver will report a Feasible solution.
	/// By contrast the simplex solver reports Optimal.
	/// </remarks>
	public class ConstraintProgrammingDirective : Directive
	{
		/// <summary>
		/// The algorithm to use
		/// </summary>
		public ConstraintProgrammingAlgorithm Algorithm { get; set; }

		/// <summary>
		/// Heuristic for selecting decisions to branch on
		/// </summary>
		public TreeSearchVariableSelection VariableSelection { get; set; }

		/// <summary>
		/// Heuristic for selecting decision value to test first
		/// </summary>
		public TreeSearchValueSelection ValueSelection { get; set; }

		/// <summary>
		/// Heuristic for selecting local search moves
		/// </summary>
		public LocalSearchMoveSelection MoveSelection { get; set; }

		/// <summary>
		/// Whether to enable the solver to restart from the beginning if it isn't making progress
		/// </summary>
		public bool RestartEnabled { get; set; }

		/// <summary>
		/// Number of decimal digits of precision (0 to 4)
		/// </summary>
		public int PrecisionDecimals { get; set; }

		/// <summary>
		/// A list of decisions to branch on first
		/// </summary>
		public ReadOnlyCollection<Decision> UserOrderVariables { get; set; }

		/// <summary>
		/// Create a new CSP directive with default values
		/// </summary>
		public ConstraintProgrammingDirective()
		{
			Algorithm = ConstraintProgrammingAlgorithm.Default;
			VariableSelection = TreeSearchVariableSelection.Default;
			ValueSelection = TreeSearchValueSelection.Default;
			MoveSelection = LocalSearchMoveSelection.Default;
			RestartEnabled = false;
			PrecisionDecimals = 2;
			UserOrderVariables = null;
		}

		/// <summary>
		/// Returns a representation of the directive as a string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Concat("CSP(TimeLimit = ", base.TimeLimit, ", MaximumGoalCount = ", base.MaximumGoalCount, ", Arithmetic = ", base.Arithmetic, ", Algorithm = ", Algorithm, ", VariableSelection = ", VariableSelection, ", ValueSelection = ", ValueSelection, ", MoveSelection = ", MoveSelection, ", RestartEnabled = ", RestartEnabled, ", PrecisionDecimals = ", PrecisionDecimals, ")");
		}
	}
}
