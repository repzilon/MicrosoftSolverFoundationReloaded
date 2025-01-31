using System.Text;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class StringConstant : Constant<string>
	{
		public override Expression Head => base.Rewrite.Builtin.String;

		public StringConstant(RewriteSystem rs, string str)
			: base(rs, str)
		{
		}

		public override bool GetValue(out string val)
		{
			val = base.Value;
			return true;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("\"");
			string value = base.Value;
			foreach (char c in value)
			{
				if (c == '"')
				{
					stringBuilder.Append("\\\"");
					continue;
				}
				switch (c)
				{
				case '\n':
					stringBuilder.Append("\\n");
					break;
				case '\t':
					stringBuilder.Append("\t");
					break;
				case '\\':
					stringBuilder.Append("\\\\");
					break;
				case ' ':
				case '!':
				case '"':
				case '#':
				case '$':
				case '%':
				case '&':
				case '\'':
				case '(':
				case ')':
				case '*':
				case '+':
				case ',':
				case '-':
				case '.':
				case '/':
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				case ':':
				case ';':
				case '<':
				case '=':
				case '>':
				case '?':
				case '@':
				case 'A':
				case 'B':
				case 'C':
				case 'D':
				case 'E':
				case 'F':
				case 'G':
				case 'H':
				case 'I':
				case 'J':
				case 'K':
				case 'L':
				case 'M':
				case 'N':
				case 'O':
				case 'P':
				case 'Q':
				case 'R':
				case 'S':
				case 'T':
				case 'U':
				case 'V':
				case 'W':
				case 'X':
				case 'Y':
				case 'Z':
				case '[':
				case ']':
				case '^':
				case '_':
				case '`':
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
				case 'f':
				case 'g':
				case 'h':
				case 'i':
				case 'j':
				case 'k':
				case 'l':
				case 'm':
				case 'n':
				case 'o':
				case 'p':
				case 'q':
				case 'r':
				case 's':
				case 't':
				case 'u':
				case 'v':
				case 'w':
				case 'x':
				case 'y':
				case 'z':
				case '{':
				case '|':
				case '}':
				case '~':
					stringBuilder.Append(c);
					break;
				default:
					stringBuilder.AppendFormat("\\u{0:X4}", (int)c);
					break;
				}
			}
			stringBuilder.Append("\"");
			return stringBuilder.ToString();
		}
	}
}
