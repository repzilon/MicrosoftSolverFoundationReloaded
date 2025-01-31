namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///  terms for boolean variables 
	/// </summary>
	internal class DisolverBooleanVariable : DisolverBooleanTerm
	{
		private readonly bool _userDefined;

		public DisolverBooleanVariable(IntegerSolver s, object key, bool userDefined)
			: base(s, null)
		{
			_userDefined = userDefined;
			_key = key;
			if (key != null)
			{
				s.RecordKey(key, this);
			}
		}

		public override bool IsUserDefined()
		{
			return _userDefined;
		}
	}
}
