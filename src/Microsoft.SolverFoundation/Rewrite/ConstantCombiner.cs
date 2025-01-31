namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class ConstantCombiner<T>
	{
		protected abstract T Identity { get; }

		protected abstract Expression IdentityExpr { get; }

		protected abstract bool IsIdentity(T val);

		protected abstract bool IsSink(T val);

		protected abstract bool IsFinalSink(T val);

		protected abstract bool CombineConsts(ref T valTot, Expression expr);

		protected abstract Expression ExprFromConst(T val);

		public virtual Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			T valTot = Identity;
			int num = -1;
			int num2 = 0;
			for (int i = 0; i < ib.Count; i++)
			{
				Expression expression = ib[i];
				if (!CombineConsts(ref valTot, expression))
				{
					ib[num2++] = expression;
					continue;
				}
				if (IsSink(valTot))
				{
					return expression;
				}
				if (num < 0)
				{
					num = num2++;
				}
			}
			if (IsFinalSink(valTot))
			{
				return ExprFromConst(valTot);
			}
			if (!IsIdentity(valTot))
			{
				Expression expression2 = ExprFromConst(valTot);
				if (num2 == 1)
				{
					return expression2;
				}
				ib[num] = expression2;
			}
			else
			{
				if (num2 == 0)
				{
					return IdentityExpr;
				}
				if (num >= 0)
				{
					switch (num2)
					{
					case 1:
						return IdentityExpr;
					case 2:
						return ib[1 - num];
					default:
						ib.RemoveRange(num2, ib.Count);
						ib.RemoveRange(num, num + 1);
						return null;
					}
				}
			}
			ib.RemoveRange(num2, ib.Count);
			return null;
		}
	}
}
