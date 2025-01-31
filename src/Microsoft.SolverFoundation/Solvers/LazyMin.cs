using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Min of two lazy expressions; computed incrementally
	/// </summary>
	internal class LazyMin : LazyExpression
	{
		private LazyExpression _arg1;

		private LazyExpression _arg2;

		/// <summary>
		///   Construction 
		/// </summary>
		/// <param name="eval">evaluator to which expression is connected</param>
		/// <param name="arg1">first sub epxression</param>
		/// <param name="arg2">second sub expression</param>
		public LazyMin(LazyEvaluator eval, LazyExpression arg1, LazyExpression arg2)
			: base(eval, Math.Min(arg1.Value, arg2.Value), Utils.Pair(arg1, arg2))
		{
			_arg1 = arg1;
			_arg2 = arg2;
			Listener l = WhenInputModified;
			arg1.Subscribe(l);
			arg2.Subscribe(l);
		}

		/// <summary>
		///   method called when a change occurs in one subterm.
		/// </summary>
		private void WhenInputModified(LazyExpression f, double oldValue)
		{
			base.Value = Math.Min(_arg1.Value, _arg2.Value);
		}
	}
}
