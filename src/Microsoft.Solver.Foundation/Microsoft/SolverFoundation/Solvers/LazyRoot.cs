using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Squared root of a lazy expression; maintained incrementally
	/// </summary>
	internal class LazyRoot : LazyExpression
	{
		/// <summary>
		///   Construction
		/// </summary>
		/// <param name="eval">evaluator to which expression is connected</param>
		/// <param name="arg">sub term</param>
		public LazyRoot(LazyEvaluator eval, LazyExpression arg)
			: base(eval, Math.Sqrt(arg.Value), Utils.Singleton(arg))
		{
			arg.Subscribe(WhenInputModified);
		}

		/// <summary>
		///   method called when a change occurs in the subterm.
		/// </summary>
		private void WhenInputModified(LazyExpression f, double oldValue)
		{
			base.Value = Math.Sqrt(f.Value);
		}
	}
}
