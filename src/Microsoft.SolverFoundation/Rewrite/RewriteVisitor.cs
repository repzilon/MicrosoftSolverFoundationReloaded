namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class RewriteVisitor
	{
		public virtual Expression Visit(Expression expr)
		{
			if (expr is Constant c)
			{
				return VisitConstant(c);
			}
			if (expr is Symbol sym)
			{
				return VisitSymbol(sym);
			}
			if (expr is Invocation inv)
			{
				return VisitInvocation(inv);
			}
			return VisitOther(expr);
		}

		public virtual Expression VisitConstant(Constant c)
		{
			return c;
		}

		public virtual Expression VisitSymbol(Symbol sym)
		{
			return sym;
		}

		public virtual Expression VisitOther(Expression expr)
		{
			return expr;
		}

		public virtual Expression VisitInvocation(Invocation inv)
		{
			using (InvocationBuilder invocationBuilder = InvocationBuilder.GetBuilder(inv, fKeepAll: false))
			{
				invocationBuilder.HeadNew = Visit(invocationBuilder.HeadOld);
				while (invocationBuilder.StartNextArg())
				{
					invocationBuilder.AddNewArg(Visit(invocationBuilder.ArgCur));
				}
				return invocationBuilder.GetNew();
			}
		}
	}
}
