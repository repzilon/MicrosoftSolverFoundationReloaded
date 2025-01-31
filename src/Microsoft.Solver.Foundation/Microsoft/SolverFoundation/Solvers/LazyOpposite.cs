namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Opposite of a lazy expression; maintained incrementally
	/// </summary>
	internal class LazyOpposite : LazyExpression
	{
		/// <summary>
		///   Construction
		/// </summary>
		/// <param name="eval">evaluator to which expression is connected</param>
		/// <param name="arg">sub term</param>
		public LazyOpposite(LazyEvaluator eval, LazyExpression arg)
			: base(eval, 0.0 - arg.Value, Utils.Singleton(arg))
		{
			arg.Subscribe(WhenInputModified);
		}

		/// <summary>
		///   method called when a change occurs in the subterm.
		/// </summary>
		private void WhenInputModified(LazyExpression f, double oldValue)
		{
			base.Value = 0.0 - f.Value;
		}
	}
}
