namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Quotient of a lazy expression by a constant. Maintained incrementally
	/// </summary>
	internal class LazyDivisionByConstant : LazyExpression
	{
		private LazyExpression _arg;

		private readonly double _cst;

		/// <summary>
		///   Construction 
		/// </summary>
		/// <param name="eval">evaluator to which expression is connected</param>
		/// <param name="arg">first sub epxression</param>
		/// <param name="cst">coefficient</param>
		public LazyDivisionByConstant(LazyEvaluator eval, LazyExpression arg, double cst)
			: base(eval, arg.Value / cst, Utils.Singleton(arg))
		{
			_arg = arg;
			_cst = cst;
			arg.Subscribe(WhenInputModified);
		}

		/// <summary>
		///   method called when a change occurs in one subterm.
		/// </summary>
		private void WhenInputModified(LazyExpression f, double oldValue)
		{
			base.Value = _arg.Value / _cst;
		}
	}
}
