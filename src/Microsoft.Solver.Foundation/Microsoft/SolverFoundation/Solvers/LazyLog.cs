using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Natural logarithm (base E) of a lazy expression,
	///   maintained incrementally.
	/// </summary>
	internal class LazyLog : LazyExpression
	{
		/// <summary>
		///   Construction
		/// </summary>
		/// <param name="eval">evaluator to which expression is connected</param>
		/// <param name="arg">sub expression</param>
		public LazyLog(LazyEvaluator eval, LazyExpression arg)
			: base(eval, Math.Log(arg.Value), Utils.Singleton(arg))
		{
			arg.Subscribe(WhenInputModified);
		}

		/// <summary>
		///   method called when a change occurs in the subterm.
		/// </summary>
		private void WhenInputModified(LazyExpression f, double oldValue)
		{
			base.Value = Math.Log(f.Value);
		}
	}
}
