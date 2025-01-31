using System.Text;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class InequalitySymbol : CompareSymbol
	{
		internal override Direction Dir => Direction.Bad;

		internal InequalitySymbol(RewriteSystem rs)
			: base(rs, "Inequality", ParseInfo.Default)
		{
		}

		private static Direction GetDir(Expression expr)
		{
			if (!(expr is CompareSymbol compareSymbol))
			{
				return Direction.Bad;
			}
			return compareSymbol.Dir;
		}

		private static Direction NonStrict(Direction dir)
		{
			return dir & (Direction)(-2);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count < 2)
			{
				return base.Rewrite.Builtin.Boolean.True;
			}
			if ((ib.Count & 1) != 1)
			{
				return null;
			}
			bool fRes;
			if (ib.Count == 3)
			{
				Direction dir = GetDir(ib[1]);
				if (dir == Direction.Bad)
				{
					return null;
				}
				if (CompareSymbol.CanCompare(ib[0], ib[2], dir, out fRes))
				{
					return base.Rewrite.Builtin.Boolean.Get(fRes);
				}
				return ib[1].Invoke(ib[0], ib[2]);
			}
			int num;
			Direction dir2;
			Expression expression;
			if (CompareSymbol.IsNumeric(expression = ib[0]))
			{
				num = 1;
				while (true)
				{
					if (num >= ib.Count)
					{
						return base.Rewrite.Builtin.Boolean.True;
					}
					Expression expression2;
					if ((dir2 = GetDir(ib[num])) == Direction.Bad || !CompareSymbol.CanCompare(expression, expression2 = ib[num + 1], dir2, out fRes))
					{
						break;
					}
					if (!fRes)
					{
						return base.Rewrite.Builtin.Boolean.False;
					}
					expression = expression2;
					num += 2;
				}
				ib[0] = expression;
				ib[1] = ib[num++];
			}
			else
			{
				num = 2;
			}
			int num2 = ib.Count;
			if (CompareSymbol.IsNumeric(expression = ib[num2 - 1]))
			{
				Expression expression2;
				while ((dir2 = GetDir(ib[num2 - 2])) != Direction.Bad && CompareSymbol.CanCompare(expression2 = ib[num2 - 3], expression, dir2, out fRes))
				{
					if (!fRes)
					{
						return base.Rewrite.Builtin.Boolean.False;
					}
					expression = expression2;
					num2 -= 2;
				}
			}
			int num3;
			if (CompareSymbol.IsNumeric(expression = ib[0]) && (dir2 = GetDir(ib[1])) != Direction.Bad)
			{
				num3 = 0;
			}
			else
			{
				expression = null;
				dir2 = Direction.Bad;
				num3 = -1;
			}
			int num4 = 0;
			int num5 = 2;
			int num6 = num;
			while (true)
			{
				Expression expression3 = ib[num6++];
				Expression expression2 = (CompareSymbol.IsNumeric(expression3) ? expression3 : null);
				if (expression2 != null && expression != null)
				{
					if (!CompareSymbol.CompareNumbers(expression, expression2, dir2))
					{
						return base.Rewrite.Builtin.Boolean.False;
					}
					if (CompareSymbol.CompareNumbers(expression, expression2, Direction.Equal))
					{
						num5--;
						int num7 = num5;
						while ((num7 -= 2) > num3)
						{
							ib[num7] = base.Rewrite.Builtin.Equal;
						}
						dir2 = Direction.Equal;
					}
					else
					{
						if (num3 == num5 - 2)
						{
							num4++;
						}
						ib[num5++] = expression3;
					}
				}
				else
				{
					ib[num5++] = expression3;
				}
				if (num6 >= num2)
				{
					break;
				}
				expression3 = ib[num6++];
				Direction dir3 = GetDir(expression3);
				ib[num5++] = expression3;
				if (dir3 == Direction.Bad)
				{
					expression = null;
					dir2 = Direction.Bad;
					num3 = -1;
				}
				else if (expression2 != null)
				{
					expression = expression2;
					dir2 = dir3;
					num3 = num5 - 2;
				}
				else
				{
					if (expression == null)
					{
						continue;
					}
					switch (dir2)
					{
					case Direction.Equal:
						dir2 = dir3;
						continue;
					case Direction.Unequal:
						expression = null;
						dir2 = Direction.Bad;
						num3 = -1;
						continue;
					}
					if (NonStrict(dir3) == NonStrict(dir2))
					{
						dir2 |= dir3;
					}
					else if (dir3 != 0)
					{
						expression = null;
						dir2 = Direction.Bad;
						num3 = -1;
					}
				}
			}
			if (num5 < ib.Count)
			{
				ib.RemoveRange(num5, ib.Count);
			}
			if (num4 >= 2 && ib.Count >= 9)
			{
				num5 = 2;
				int num8 = 2;
				while (num8 <= ib.Count - 7)
				{
					Expression expression2;
					int num9;
					if (!CompareSymbol.IsNumeric(expression2 = ib[num8 + 4]) || GetDir(ib[num8 + 3]) == Direction.Bad)
					{
						num9 = num8 + 6;
					}
					else if (!CompareSymbol.IsNumeric(ib[num8 + 2]) || GetDir(ib[num8 + 3]) == Direction.Bad)
					{
						num9 = num8 + 4;
					}
					else
					{
						if (CompareSymbol.IsNumeric(expression = ib[num8]) && GetDir(ib[num8 + 1]) != Direction.Bad)
						{
							int i;
							for (i = num8 + 5; i < ib.Count - 4; i += 2)
							{
								Expression expression4;
								if (!CompareSymbol.IsNumeric(expression4 = ib[i + 1]))
								{
									break;
								}
								if (GetDir(ib[i]) == Direction.Bad)
								{
									break;
								}
								expression2 = expression4;
							}
							ib[num5++] = expression;
							if (CompareSymbol.CompareNumbers(expression, expression2, Direction.Unequal))
							{
								ib[num5++] = base.Rewrite.Builtin.Unequal;
								ib[num5++] = expression2;
							}
							num8 = i;
							ib[num5++] = ib[num8++];
							continue;
						}
						num9 = num8 + 2;
					}
					if (num8 == num5)
					{
						num8 = (num5 = num9);
						continue;
					}
					while (num8 < num9)
					{
						ib[num5++] = ib[num8++];
					}
				}
				if (num5 < num8)
				{
					while (num8 < ib.Count)
					{
						ib[num5++] = ib[num8++];
					}
					ib.RemoveRange(num5, ib.Count);
				}
			}
			return null;
		}

		public override void FormatInvocation(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			if (inv.Arity < 3 || (inv.Arity & 1) != 1)
			{
				base.FormatInvocation(sb, inv, out precLeft, out precRight, formatter);
				return;
			}
			for (int i = 1; i < inv.Arity; i += 2)
			{
				if (!(inv[i] is CompareSymbol))
				{
					base.FormatInvocation(sb, inv, out precLeft, out precRight, formatter);
					return;
				}
			}
			int length = sb.Length;
			inv[0].Format(sb, out precLeft, out precRight, formatter);
			if ((int)precRight >= 7)
			{
				sb.Insert(length, '(');
				sb.Append(')');
			}
			for (int j = 1; j < inv.Arity; j += 2)
			{
				CompareSymbol compareSymbol = (CompareSymbol)inv[j];
				sb.Append(compareSymbol.ParseInfo.OperatorText);
				length = sb.Length;
				inv[j + 1].Format(sb, out precLeft, out precRight, formatter);
				if ((int)precLeft >= 7 || (int)precRight >= 7)
				{
					sb.Insert(length, '(');
					sb.Append(')');
				}
			}
			precLeft = Precedence.Compare;
			precRight = Precedence.Compare;
		}
	}
}
