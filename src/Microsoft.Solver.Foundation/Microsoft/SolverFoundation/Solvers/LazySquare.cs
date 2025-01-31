namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Square of a lazy expression; maintained incrementally
	/// </summary>
	internal class LazySquare : LazyExpression
	{
		/// <summary>
		///   Construction
		/// </summary>
		/// <param name="eval">evaluator to which expression is connected</param>
		/// <param name="arg">sub term</param>
		public LazySquare(LazyEvaluator eval, LazyExpression arg)
			: base(eval, arg.Value * arg.Value, Utils.Singleton(arg))
		{
			arg.Subscribe(WhenInputModified);
		}

		/// <summary>
		///   method called when a change occurs in the subterm.
		/// </summary>
		private void WhenInputModified(LazyExpression f, double oldValue)
		{
			double value = f.Value;
			base.Value = value * value;
		}
	}
}
