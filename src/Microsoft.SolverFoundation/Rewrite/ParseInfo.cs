namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ParseInfo
	{
		public readonly string OperatorText;

		public readonly Precedence LeftPrecedence;

		public readonly Precedence RightPrecedence;

		private readonly ParseInfoOptions _flags;

		public static readonly ParseInfo Default = new ParseInfo(ParseInfoOptions.None);

		public bool CreateScope => (_flags & ParseInfoOptions.CreateScope) != 0;

		public bool BorrowScope => (_flags & ParseInfoOptions.BorrowScope) != 0;

		public bool IteratorScope => (_flags & ParseInfoOptions.IteratorScope) != 0;

		public bool VaryadicInfix => (_flags & ParseInfoOptions.VaryadicInfix) != 0;

		public bool Comparison => (_flags & ParseInfoOptions.Comparison) != 0;

		public bool CreateVariable => (_flags & ParseInfoOptions.CreateVariable) != 0;

		public bool HasInfixForm => OperatorText != null;

		public bool IsUnaryPrefix => LeftPrecedence == Precedence.None;

		public bool IsUnaryPostfix => RightPrecedence == Precedence.None;

		public static ParseInfo GetUnaryPrefix(string strOper)
		{
			return new ParseInfo(strOper, Precedence.None, Precedence.Unary, ParseInfoOptions.None);
		}

		public ParseInfo(string strOper, Precedence precLeft)
			: this(strOper, precLeft, precLeft - 1, ParseInfoOptions.None)
		{
		}

		public ParseInfo(string strOper, Precedence precLeft, Precedence precRight)
			: this(strOper, precLeft, precRight, ParseInfoOptions.None)
		{
		}

		public ParseInfo(ParseInfoOptions flags)
			: this(null, Precedence.Invocation, Precedence.Atom, flags)
		{
		}

		public ParseInfo(string strOper, Precedence precLeft, Precedence precRight, ParseInfoOptions flags)
		{
			OperatorText = strOper;
			LeftPrecedence = precLeft;
			RightPrecedence = precRight;
			_flags = flags;
		}
	}
}
