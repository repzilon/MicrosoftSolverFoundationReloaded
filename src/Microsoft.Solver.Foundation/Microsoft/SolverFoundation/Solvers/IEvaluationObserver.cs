namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Term that is notified of any change in a term of lower depth
	/// </summary>
	internal interface IEvaluationObserver
	{
		/// <summary>
		/// Signals to the observer that the value of the term has changed
		/// </summary>
		void ValueChange(EvaluableTerm arg, double oldValue, double newValue);
	}
}
