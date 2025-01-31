namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class Visitor
	{
		public virtual bool Visit(Expression expr)
		{
			if (expr is Constant c)
			{
				return VisitConstant(c);
			}
			if (expr is Symbol sym)
			{
				return VisitSymbol(sym);
			}
			if (!(expr is Invocation invocation))
			{
				return VisitOther(expr);
			}
			if (!PreVisitInvocation(invocation))
			{
				return false;
			}
			if (!Visit(invocation.Head))
			{
				return false;
			}
			for (int i = 0; i < invocation.Arity; i++)
			{
				if (!Visit(invocation[i]))
				{
					return false;
				}
			}
			return PostVisitInvocation(invocation);
		}

		public virtual bool VisitConstant(Constant c)
		{
			return true;
		}

		public virtual bool VisitSymbol(Symbol sym)
		{
			return true;
		}

		public virtual bool PreVisitInvocation(Invocation inv)
		{
			return true;
		}

		public virtual bool PostVisitInvocation(Invocation inv)
		{
			return true;
		}

		public virtual bool VisitOther(Expression expr)
		{
			return true;
		}
	}
}
