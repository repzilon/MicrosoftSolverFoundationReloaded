namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Product of a lazy expression by a constant. Maintained incrementally
	/// </summary>
	internal class LazyProductByConstant : LazyExpression
	{
		private LazyExpression _arg;

		private readonly double _cst;

		/// <summary>
		///   Construction 
		/// </summary>
		/// <param name="eval">evaluator to which expression is connected</param>
		/// <param name="arg">first sub epxression</param>
		/// <param name="cst">coefficient</param>
		public LazyProductByConstant(LazyEvaluator eval, LazyExpression arg, double cst)
			: base(eval, cst * arg.Value, Utils.Singleton(arg))
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
			base.Value = _cst * _arg.Value;
		}
	}
}
