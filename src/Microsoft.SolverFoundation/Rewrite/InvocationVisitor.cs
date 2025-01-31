using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class InvocationVisitor
	{
		private readonly Func<Invocation, bool> _fn;

		public static bool VisitInvocations(Expression expr, Func<Invocation, bool> fn)
		{
			InvocationVisitor invocationVisitor = new InvocationVisitor(fn);
			return invocationVisitor.Visit(expr);
		}

		protected InvocationVisitor(Func<Invocation, bool> fn)
		{
			_fn = fn;
		}

		public virtual bool Visit(Expression expr)
		{
			if (!(expr is Invocation invocation))
			{
				return true;
			}
			if (!_fn(invocation))
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
			return true;
		}
	}
}
