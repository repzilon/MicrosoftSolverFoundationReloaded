namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Terms representing integer variables.
	///   Can have a more complex domain and be user-defined
	/// </summary>
	internal class DisolverIntegerVariable : DisolverIntegerTerm
	{
		public readonly bool _isUserDefined;

		public readonly DisolverDiscreteDomain _domain;

		public CspDomain InitialDomain => _domain;

		public override double DomainSize => _domain.Count;

		public DisolverIntegerVariable(IntegerSolver s, DisolverDiscreteDomain dom, object key, bool userDefined)
			: base(s, dom.First, dom.Last, null)
		{
			_domain = dom;
			_isUserDefined = userDefined;
			_key = key;
			if (key != null)
			{
				s.RecordKey(key, this);
			}
		}

		public override bool IsUserDefined()
		{
			return _isUserDefined;
		}
	}
}
