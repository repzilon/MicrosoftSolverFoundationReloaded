namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Useful way to pair objects of any type,
	///   for instance Pair between int and bool.
	/// </summary>
	internal struct Pair<A, B>
	{
		private A _a;

		private B _b;

		public A First
		{
			get
			{
				return _a;
			}
			set
			{
				_a = value;
			}
		}

		public B Second
		{
			get
			{
				return _b;
			}
			set
			{
				_b = value;
			}
		}

		public Pair(A a, B b)
		{
			_a = a;
			_b = b;
		}

		public override string ToString()
		{
			return "Pair(" + _a.ToString() + ", " + _b.ToString() + ")";
		}
	}
}
