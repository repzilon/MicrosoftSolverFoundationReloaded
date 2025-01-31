using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Public abstract class that contains user defined heuristics
	/// </summary>
	internal abstract class ConstraintSolverHeuristics
	{
		/// <summary> Decide the next decision variable. Will be called when we want to branch.
		/// </summary>
		/// <remarks> When this method is called, we guarantee that the current partial assignment is
		///           consistent</remarks>
		/// <remarks> Query callback </remarks>
		public abstract CspTerm GetNextDecisionVariable();

		/// <summary> Undo the most recent decision variable selection. Will be called when we backtrack.
		/// </summary>
		/// <remarks> Query callback </remarks>
		public abstract void UndoDecisionVariable(CspTerm decisionVariable);

		/// <summary> Decide the next value to try out given that chosenDecisionVariable is the branching 
		///           variable. Will be called after branching variable is chosen.
		/// </summary>
		/// <remarks> Query callback </remarks>
		public abstract object TryNextValue(CspTerm chosenDecisionVariable);

		/// <summary> Notify that a conflict has been found. Will be called when propagation finds a conflict.
		/// </summary>
		/// <remarks> Notification callback </remarks>
		public abstract void ConflictFound(CspTerm conflict);

		/// <summary> Initialization callback. Will be called before search phase starts 
		/// </summary> 
		public abstract void Init(ConstraintSystem model);

		/// <summary> Generate an initial assignment </summary> 
		/// <remarks> Query callback</remarks>
		public abstract Dictionary<CspTerm, object> GenerateInitialAssignment();

		/// <summary> Select a move according to the current assignment </summary> 
		/// <remarks> Query callback </remarks> 
		public abstract Dictionary<CspTerm, object> SelectMove();

		/// <summary> Notify user that we have set the initial assignment. 
		///           This is the time for user to  initialize any data structure that
		///           will be used during local search. </summary> 
		/// <remarks> Notification callback </remarks> 
		public abstract void InitialAssignmentInPlace(Dictionary<CspTerm, object> assignment);

		/// <summary> Notify user that we have made a flip </summary> 
		/// <remarks> Notification callback </remarks>
		public abstract void ReportFlip(CspTerm variable, object previousValue, object newValue);
	}
}
