namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Lexer newline token
	/// </summary>
	internal class NewLineToken : Token
	{
		private bool _fNested;

		/// <summary> test or set Nested
		/// </summary>
		public override bool Nested
		{
			get
			{
				return _fNested;
			}
			internal set
			{
				_fNested = value;
			}
		}

		/// <summary> Constructor
		/// </summary>
		public NewLineToken()
			: base(TokKind.NewLine)
		{
		}
	}
}
