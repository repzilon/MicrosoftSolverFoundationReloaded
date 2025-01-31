using System.Globalization;

namespace Microsoft.SolverFoundation.Common
{
	internal class CharLitToken : Token
	{
		private char _val;

		public char Val => _val;

		public CharLitToken(char val)
			: base(TokKind.CharLit)
		{
			_val = val;
		}

		public override string ToString()
		{
			if (_val < ' ' || _val >= '\u007f')
			{
				return string.Format(CultureInfo.InvariantCulture, "'\\u{0:X4}'", new object[1] { (int)_val });
			}
			if (_val == '\'')
			{
				return "'\\''";
			}
			return string.Format(CultureInfo.InvariantCulture, "'{0}'", new object[1] { _val });
		}
	}
}
