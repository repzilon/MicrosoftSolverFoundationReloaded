using System.Text;

namespace Microsoft.SolverFoundation.Common
{
	internal class StrLitToken : Token
	{
		private string _val;

		public string Val => _val;

		public StrLitToken(string val)
			: base(TokKind.StrLit)
		{
			_val = val;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(_val.Length + 2);
			stringBuilder.Append('"');
			for (int i = 0; i < _val.Length; i++)
			{
				char c = _val[i];
				if (c < ' ' || c >= '\u007f')
				{
					stringBuilder.AppendFormat("\\{0:X4}", (int)c);
					continue;
				}
				if (c == '\\' || c == '"')
				{
					stringBuilder.Append('\\');
				}
				stringBuilder.Append(c);
			}
			stringBuilder.Append('"');
			return stringBuilder.ToString();
		}
	}
}
