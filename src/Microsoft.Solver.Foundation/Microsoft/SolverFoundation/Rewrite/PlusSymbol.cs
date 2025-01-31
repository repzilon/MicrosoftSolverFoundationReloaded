using System.Text;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class PlusSymbol : Symbol
	{
		private NumberPlus _np;

		internal PlusSymbol(RewriteSystem rs)
			: base(rs, "Plus", new ParseInfo("+", Precedence.Plus, Precedence.Times, ParseInfoOptions.VaryadicInfix))
		{
			AddAttributes(rs.Attributes.Flat, rs.Attributes.Listable, rs.Attributes.UnaryIdentity, rs.Attributes.Orderless);
			_np = new NumberPlus(rs);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			return _np.EvaluateInvocationArgs(ib);
		}

		public override Expression PostSort(InvocationBuilder ib)
		{
			return _np.PostSort(ib);
		}

		protected override void FormatInvocationBinary(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			int length = sb.Length;
			inv[0].Format(sb, out precLeft, out precRight, formatter);
			if ((int)precRight >= (int)ParseInfo.LeftPrecedence)
			{
				sb.Insert(length, '(');
				sb.Append(')');
			}
			for (int i = 1; i < inv.Arity; i++)
			{
				Expression expression = inv[i];
				bool flag = false;
				while (expression.Head == base.Rewrite.Builtin.Minus && expression.Arity == 1)
				{
					expression = expression[0];
					flag = !flag;
				}
				formatter.BeforeBinaryOperator(sb, inv);
				int length2 = sb.Length;
				sb.Append(flag ? "-" : ParseInfo.OperatorText);
				formatter.AfterBinaryOperator(sb, inv);
				length = sb.Length;
				expression.Format(sb, out precLeft, out precRight, formatter);
				if ((int)precLeft > (int)ParseInfo.RightPrecedence || ((int)precRight >= (int)ParseInfo.LeftPrecedence && i + 1 < inv.Arity))
				{
					sb.Insert(length, '(');
					sb.Append(')');
				}
				else if (sb[length] == '-')
				{
					sb[length2] = (flag ? '+' : '-');
					sb.Remove(length, 1);
				}
			}
			precLeft = ParseInfo.LeftPrecedence;
			precRight = ParseInfo.RightPrecedence;
		}
	}
}
