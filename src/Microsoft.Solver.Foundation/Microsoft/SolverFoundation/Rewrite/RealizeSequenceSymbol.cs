using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class RealizeSequenceSymbol : Symbol
	{
		private bool _fSplice;

		private Symbol ResultHead
		{
			get
			{
				if (!_fSplice)
				{
					return base.Rewrite.Builtin.List;
				}
				return base.Rewrite.Builtin.ArgumentSplice;
			}
		}

		internal RealizeSequenceSymbol(RewriteSystem rs, bool fSplice)
			: base(rs, fSplice ? "SpliceSequence" : "RealizeSequence")
		{
			_fSplice = fSplice;
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			int num = int.MaxValue;
			if (ib.Count != 1)
			{
				if (ib.Count != 2 || !ib[1].GetValue(out BigInteger val) || val < 0)
				{
					return null;
				}
				if (num > val)
				{
					num = (int)val;
				}
			}
			if (!(ib[0] is ExprSequence exprSequence))
			{
				return null;
			}
			if (num == 0)
			{
				return ResultHead.Invoke();
			}
			Expression[] array = exprSequence.Values;
			if (array == null)
			{
				base.Rewrite.CheckAbort();
				List<Expression> list = new List<Expression>();
				foreach (Expression item in exprSequence)
				{
					list.Add(item);
					if (list.Count < num)
					{
						base.Rewrite.CheckAbort();
						continue;
					}
					break;
				}
				array = list.ToArray();
			}
			else if (array.Length > num)
			{
				Array.Resize(ref array, num);
			}
			return ResultHead.Invoke(array);
		}
	}
}
