namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   adapts events of signature (IntegerVariable -&gt; void)
	///   into events of signature (int -&gt; void) where the int is
	///   the index of the variable in the global constraint.
	///   This index is specified at construction time
	/// </summary>
	internal class DelegateAdaptor
	{
		private readonly Procedure<int> _procedure;

		private readonly int _index;

		public DelegateAdaptor(int idx, Procedure<int> p)
		{
			_procedure = p;
			_index = idx;
		}

		public void Run(IntegerVariable x)
		{
			_procedure(_index);
		}
	}
}
