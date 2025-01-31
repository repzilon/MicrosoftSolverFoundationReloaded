namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class AdjustAttributesSymbol : Symbol
	{
		private bool _fAdd;

		internal AdjustAttributesSymbol(RewriteSystem rs, string strName, bool fAdd)
			: base(rs, strName)
		{
			_fAdd = fAdd;
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2 || !(ib[0] is Symbol symbol))
			{
				return null;
			}
			Expression expression = base.Rewrite.FailOnAttributesLocked(symbol);
			if (expression != null)
			{
				return expression;
			}
			if (ib[1] is Symbol symbol2)
			{
				symbol.AdjustAttributes(_fAdd, symbol2);
				return base.Rewrite.Builtin.Null;
			}
			if (ib[1].Head != base.Rewrite.Builtin.List)
			{
				return null;
			}
			using (InvocationBuilder invocationBuilder = InvocationBuilder.GetBuilder((Invocation)ib[1], fKeepAll: false))
			{
				while (invocationBuilder.StartNextArg())
				{
					if (invocationBuilder.ArgCur is Symbol symbol3)
					{
						symbol.AdjustAttributes(_fAdd, symbol3);
					}
					else
					{
						invocationBuilder.AddNewArg(invocationBuilder.ArgCur);
					}
				}
				if (invocationBuilder.Count == 0)
				{
					return base.Rewrite.Builtin.Null;
				}
				if (invocationBuilder.Diff)
				{
					ib[1] = invocationBuilder.GetNew();
				}
			}
			return null;
		}
	}
}
