using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>Exposes the methods and a properties of a Local Search
	///          solver that are used by local search strategies
	/// </summary>
	/// <remarks>A Local Search solver is a stateful object which is
	///          always in a certain configuration. This configuration is
	///          essentially defined by the value currently associated to 
	///          each term of a given problem. This can be queried through
	///          this interface.
	/// </remarks>
	internal interface ILocalSearchProcess
	{
		/// <summary>Gets the constraint violation, an indication of how far
		///          the current configuration is from satisfying the constraints
		/// </summary>
		/// <returns>Zero if all constraints of the Model are satisfied. Otherwise
		///          a strictly positive value: the higher the value the
		///          further we estimate to be from a satisfying configuration.
		/// </returns>
		int CurrentViolation { get; }

		/// <summary>Gets the constraint system solved by this Local Search solver
		/// </summary>
		ConstraintSystem Model { get; }

		/// <summary>Gets the current value of an integer term</summary>
		/// <param name="expr">A Term, which should belong to the Model 
		///          of the LocalSearch solver
		/// </param> 
		int GetCurrentIntegerValue(CspTerm expr);

		/// <summary>Estimates the effect of changing a variable to a candidate value.
		///          The flip is not effectively performed
		/// </summary>
		/// <param name="x">A variable of the problem</param>
		/// <param name="newValue">A new value, from the domain of the variable</param>
		/// <returns>A numerical indicator whose value is low if the flip is expected
		///          to reduce the penalty or of a minimization goal. 
		///          This value is 0 if the flip is not expected to change the quality,
		///          strictly positive if the new configuration is expected to be worse,
		///          strictly negative if the new configuration is expected to be better.
		/// </returns>
		/// <remarks>The comparison between configurations takes into account both the 
		///          violation and all minimization goals. A configuration B is better
		///          than a configuration A if either:
		///          (1) A violates the constraints and B has a lower violation
		///          (2) A and B satisfy the constraints and B gives a lower value to 
		///          the minimization goal. If several minimization goals are defined their
		///          priority is considered: B has a lower evaluation on one objective
		///          and does not increase the value over any higher priority objective
		///
		///          CODE REVIEW (lucasb): for consistency we probably want a more uniform
		///          way of evaluating arbitrary *moves*
		/// </remarks>
		int EvaluateFlip(CspTerm x, int newValue);

		/// <summary>Subscribes a delegate that will be called by the local search
		///          every time a move is attempted, even if the move was ultimately rejected
		/// </summary>
		/// <param name="listener">A listener for the Move event</param>
		void SubscribeToMove(LocalSearch.MoveListener listener);

		/// <summary>Subscribes a delegate that will be called by the local search
		///          every time a restart takes place
		/// </summary>
		void SubscribeToRestarts(Action listener);

		/// <summary>Gives a variable hint, i.e. a variable whose re-assignment is
		///          likely to increase the overall quality. The variable is preferably
		///          selected among those that are not filtered-out.
		/// </summary>
		CspTerm SelectBestVariable();

		/// <summary>Excludes x from the set of variables that are likely to
		///          be returned by the SelectBestVariable method
		/// </summary>
		/// <param name="x">A variable</param>
		void Filter(CspTerm x);

		/// <summary>Cancels the effect of filtering x, i.e. allows x to 
		///          be returned by the SelectBestVariable method
		/// </summary>
		/// <param name="x">A variable</param>
		void Unfilter(CspTerm x);

		/// <summary>True iff the variable is likely to
		///          be returned by the SelectBestVariable method
		/// </summary>
		/// <param name="x">A variable</param>
		bool IsFiltered(CspTerm x);
	}
}
