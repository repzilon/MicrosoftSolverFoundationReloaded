namespace Microsoft.SolverFoundation.Common
{
	internal class IdentToken : Token
	{
		private NormStr _val;

		public NormStr Val => _val;

		public IdentToken(NormStr val)
			: base(TokKind.Ident)
		{
			_val = val;
		}

		public override string ToString()
		{
			return _val;
		}
	}
}
