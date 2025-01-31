using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ArraySymbol : Symbol
	{
		internal ArraySymbol(RewriteSystem rs)
			: base(rs, "Array")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count <= 1)
			{
				return null;
			}
			int[] array = new int[ib.Count - 1];
			BigInteger bigInteger = 1;
			int num = 0;
			for (int i = 1; i < ib.Count; i++)
			{
				if (ib[i].GetNumericValue(out var val))
				{
					if (val < 0 || val > int.MaxValue)
					{
						return null;
					}
					int num2 = (array[i - 1] = (int)val.GetCeiling());
					if (num2 > 0)
					{
						bigInteger *= (BigInteger)num2;
					}
					if (num < num2)
					{
						num = num2;
					}
					continue;
				}
				return null;
			}
			if (bigInteger > 10000)
			{
				return base.Rewrite.Fail(Resources.ExceededIterationLimit, 10000);
			}
			Expression[] array2 = new Expression[num];
			for (int j = 0; j < num; j++)
			{
				array2[j] = new IntegerConstant(base.Rewrite, j);
			}
			Expression[] rgexprArgs = new Expression[array.Length];
			return Generate(ib[0], array, 0, rgexprArgs, array2);
		}

		private Expression Generate(Expression exprHead, int[] rgcv, int icv, Expression[] rgexprArgs, Expression[] rgexprValues)
		{
			base.Rewrite.CheckAbort();
			if (icv >= rgcv.Length)
			{
				return exprHead.Invoke(fCanOwnArray: false, rgexprArgs).Evaluate();
			}
			Expression[] array = new Expression[rgcv[icv]];
			for (int i = 0; i < rgcv[icv]; i++)
			{
				rgexprArgs[icv] = rgexprValues[i];
				array[i] = Generate(exprHead, rgcv, icv + 1, rgexprArgs, rgexprValues);
			}
			return base.Rewrite.Builtin.List.Invoke(array);
		}
	}
}
