using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ModuleSymbol : Symbol
	{
		internal ModuleSymbol(RewriteSystem rs)
			: base(rs, "Module")
		{
			AddAttributes(rs.Attributes.HoldAll);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count >= 2 && ib[0].Head == base.Rewrite.Builtin.List)
			{
				Expression result = null;
				if (ib[0].Arity == 0)
				{
					for (int i = 1; i < ib.Count; i++)
					{
						result = ib[i].Evaluate();
					}
				}
				else
				{
					Symbol[] rgsym = new Symbol[ib[0].Arity];
					int num = rgsym.Length;
					while (--num >= 0)
					{
						if (ib[0][num] is Symbol)
						{
							rgsym[num] = new Symbol(base.Rewrite, null, ((Symbol)ib[0][num]).Name);
							continue;
						}
						goto IL_0160;
					}
					Func<Symbol, Expression> fn = delegate(Symbol sym)
					{
						int num2 = rgsym.Length;
						while (--num2 >= 0)
						{
							if (sym == ib[0][num2])
							{
								return rgsym[num2];
							}
						}
						return sym;
					};
					for (int j = 1; j < ib.Count; j++)
					{
						result = MapSymbolVisitor.VisitSymbols(ib[j], fn).Evaluate();
					}
				}
				return result;
			}
			goto IL_0160;
			IL_0160:
			return base.EvaluateInvocationArgs(ib);
		}
	}
}
