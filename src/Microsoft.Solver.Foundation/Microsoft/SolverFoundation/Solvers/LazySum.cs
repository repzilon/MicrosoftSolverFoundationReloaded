using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Sum of several lazy expressions, maintained incrementally
	/// </summary>
	internal class LazySum : LazyListExpression
	{
		/// <summary>
		///   Construction
		/// </summary>
		/// <param name="eval">evaluator to which expression is connected</param>
		/// <param name="args">list of sub-expressions</param>
		public LazySum(LazyEvaluator eval, List<LazyExpression> args)
			: base(eval, ComputeSum(args), args)
		{
			Listener l = WhenInputModified;
			int count = args.Count;
			for (int i = 0; i < count; i++)
			{
				args[i].Subscribe(l);
			}
		}

		/// <summary>
		///   method called when a change occurs in one subterm.
		/// </summary>
		private void WhenInputModified(LazyExpression f, double difference)
		{
			base.Value += difference;
		}

		private static double ComputeSum(IEnumerable<LazyExpression> args)
		{
			double num = 0.0;
			foreach (LazyExpression arg in args)
			{
				num += arg.Value;
			}
			return num;
		}
	}
}
