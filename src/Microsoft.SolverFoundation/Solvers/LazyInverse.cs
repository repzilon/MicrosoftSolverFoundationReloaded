namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Inverse (1/x) of a lazy expression x ; maintained incrementally
	/// </summary>
	internal class LazyInverse : LazyExpression
	{
		/// <summary>
		///   Construction
		/// </summary>
		/// <param name="eval">evaluator to which expression is connected</param>
		/// <param name="arg">sub term</param>
		public LazyInverse(LazyEvaluator eval, LazyExpression arg)
			: base(eval, 1.0 / arg.Value, Utils.Singleton(arg))
		{
			arg.Subscribe(WhenInputModified);
		}

		/// <summary>
		///   method called when a change occurs in the subterm.
		/// </summary>
		private void WhenInputModified(LazyExpression f, double oldValue)
		{
			base.Value = 1.0 / f.Value;
		}
	}
}
