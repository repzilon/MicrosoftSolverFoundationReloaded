using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Lazy expressions that depend on arbitrarily many sub expressions.
	/// </summary>
	internal abstract class LazyListExpression : LazyExpression
	{
		/// <summary>
		///   Construction
		/// </summary>
		/// <param name="args">sub expressions</param>
		/// <param name="initialValue">initial value</param>
		/// <param name="eval">evaluator to which expression is connected</param>
		protected LazyListExpression(LazyEvaluator eval, double initialValue, List<LazyExpression> args)
			: base(eval, initialValue, args)
		{
		}
	}
}
