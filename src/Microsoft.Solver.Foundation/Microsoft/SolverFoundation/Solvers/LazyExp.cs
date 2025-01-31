using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Exponent (in base E) of a lazy expression; maintained incrementally
	/// </summary>
	internal class LazyExp : LazyExpression
	{
		/// <summary>
		///   Construction
		/// </summary>
		/// <param name="eval">evaluator to which expression is connected</param>
		/// <param name="arg">sub term</param>
		public LazyExp(LazyEvaluator eval, LazyExpression arg)
			: base(eval, Math.Exp(arg.Value), Utils.Singleton(arg))
		{
			arg.Subscribe(WhenInputModified);
		}

		/// <summary>
		///   method called when a change occurs in the subterm.
		/// </summary>
		private void WhenInputModified(LazyExpression f, double oldValue)
		{
			base.Value = Math.Exp(f.Value);
		}
	}
}
