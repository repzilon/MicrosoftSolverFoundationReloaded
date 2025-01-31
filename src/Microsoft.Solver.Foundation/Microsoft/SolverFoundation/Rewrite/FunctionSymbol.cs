using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class FunctionSymbol : Symbol
	{
		private class MapSlotVisitor : RewriteVisitor
		{
			private InvocationBuilder _ibArgs;

			public static Expression VisitSlots(Expression expr, InvocationBuilder ibArgs)
			{
				if (ibArgs.Count == 0)
				{
					return expr;
				}
				MapSlotVisitor mapSlotVisitor = new MapSlotVisitor(ibArgs);
				return mapSlotVisitor.Visit(expr);
			}

			protected MapSlotVisitor(InvocationBuilder ibArgs)
			{
				_ibArgs = ibArgs;
			}

			public override Expression VisitInvocation(Invocation inv)
			{
				if (inv.Head == inv.Rewrite.Builtin.Function && inv.Arity <= 1)
				{
					return inv;
				}
				int val;
				if (inv.Head == inv.Rewrite.Builtin.Slot)
				{
					if (inv.Arity == 1 && inv[0].GetValue(out val) && 0 <= val && val < _ibArgs.Count)
					{
						return _ibArgs[val];
					}
					return inv;
				}
				if (inv.Head == inv.Rewrite.Builtin.SlotSplice)
				{
					if (inv.Arity == 1 && inv[0].GetValue(out val) && 0 <= val)
					{
						if (val + 1 == _ibArgs.Count)
						{
							return _ibArgs[val];
						}
						Expression[] array = new Expression[Math.Max(0, _ibArgs.Count - val)];
						for (int i = 0; i < array.Length; i++)
						{
							array[i] = _ibArgs[i + val];
						}
						return inv.Rewrite.Builtin.ArgumentSplice.Invoke(array);
					}
					return inv;
				}
				using (InvocationBuilder invocationBuilder = InvocationBuilder.GetBuilder(inv, fKeepAll: false))
				{
					invocationBuilder.HeadNew = Visit(invocationBuilder.HeadOld);
					while (invocationBuilder.StartNextArg())
					{
						Expression argCur = invocationBuilder.ArgCur;
						if (argCur.Head == inv.Rewrite.Builtin.SlotSplice)
						{
							if (argCur.Arity == 1 && argCur[0].GetValue(out val) && 0 <= val)
							{
								while (val < _ibArgs.Count)
								{
									invocationBuilder.AddNewArg(_ibArgs[val++]);
								}
							}
							else
							{
								invocationBuilder.AddNewArg(argCur);
							}
						}
						else
						{
							invocationBuilder.AddNewArg(Visit(argCur));
						}
					}
					return invocationBuilder.GetNew();
				}
			}
		}

		internal FunctionSymbol(RewriteSystem rs)
			: base(rs, "Function", new ParseInfo("!", Precedence.Function, Precedence.None))
		{
			AddAttributes(rs.Attributes.HoldAll);
		}

		public override Expression EvaluateInvocationArgsNested(Invocation invHead, InvocationBuilder ib)
		{
			if (invHead.Head == this)
			{
				if (invHead.Arity == 1)
				{
					return MapSlotVisitor.VisitSlots(invHead[0], ib);
				}
				if (invHead.Arity == 2 && invHead[0].Head == base.Rewrite.Builtin.List && invHead[0].Arity == ib.Count)
				{
					if (ib.Count == 0)
					{
						return invHead[1];
					}
					Invocation invocation = (Invocation)invHead[0];
					Substitution substitution = new Substitution();
					int num = 0;
					while (true)
					{
						if (num < invocation.Arity)
						{
							if (!(invocation[num] is Symbol sym2) || !substitution.Add(sym2, ib[num]))
							{
								break;
							}
							num++;
							continue;
						}
						return substitution.Apply(invHead[1]);
					}
				}
				else if (invHead.Arity == 2 && invHead[0] is Symbol && ib.Count == 1)
				{
					return MapSymbolVisitor.VisitSymbols(invHead[1], (Symbol sym) => (sym == invHead[0]) ? ib[0] : sym);
				}
			}
			return base.EvaluateInvocationArgsNested(invHead, ib);
		}
	}
}
