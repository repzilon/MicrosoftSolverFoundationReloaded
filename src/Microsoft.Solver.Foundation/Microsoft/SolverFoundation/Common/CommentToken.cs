namespace Microsoft.SolverFoundation.Common
{
	internal class CommentToken : Token
	{
		private string _val;

		public string Text => _val;

		public CommentToken(string val)
			: base(TokKind.Comment)
		{
			_val = val;
		}

		public override string ToString()
		{
			return _val;
		}
	}
}
