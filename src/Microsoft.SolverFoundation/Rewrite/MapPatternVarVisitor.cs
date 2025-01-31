using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class MapPatternVarVisitor : RewriteVisitor
	{
		private Func<Symbol, Expression> _fn;

		public static Expression VisitPatternVars(Expression expr, Func<Symbol, Expression> fn)
		{
			if (!PatternVarVisitor.HasPatternVars(expr))
			{
				return expr;
			}
			MapPatternVarVisitor mapPatternVarVisitor = new MapPatternVarVisitor(fn);
			return mapPatternVarVisitor.Visit(expr);
		}

		protected MapPatternVarVisitor(Func<Symbol, Expression> fn)
		{
			_fn = fn;
		}

		public override Expression VisitInvocation(Invocation inv)
		{
			if (inv.Head != inv.Rewrite.Builtin.Pattern || inv.Arity != 2 || !(inv[0] is Symbol symbol))
			{
				return base.VisitInvocation(inv);
			}
			Expression expression = _fn(symbol);
			if (expression == symbol)
			{
				return inv;
			}
			return inv.Invoke(expression, Visit(inv[1]));
		}
	}
}
