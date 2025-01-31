namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///  A real value to which delegates can be registered in order to be
	///  called when the value is modified. The value is attached to a 
	///  lazy evaluator; running (method Recompute) the evaluator will cause any
	///  delegate subscribed to the value to be called.
	/// </summary>
	internal class LazyValue : LazyExpression
	{
		/// <summary>
		///   Get / set the value of the object. Setting the value
		///   will inform the lazy evaluator that it has to inform any 
		///   subscribed delegate of the change
		/// </summary>
		public new double Value
		{
			get
			{
				return base.Value;
			}
			set
			{
				base.Value = value;
			}
		}

		/// <summary>
		///   Construction of a lazy value
		/// </summary>
		/// <param name="s">evaluator to which the value is connected</param>
		/// <param name="initialValue">initial value</param>
		public LazyValue(LazyEvaluator s, double initialValue)
			: base(s, initialValue, LazyExpression._emptyList)
		{
		}
	}
}
