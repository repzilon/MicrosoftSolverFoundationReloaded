using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class MapSymbolVisitor : RewriteVisitor
	{
		private Func<Symbol, Expression> _fn;

		public static Expression VisitSymbols(Expression expr, Func<Symbol, Expression> fn)
		{
			MapSymbolVisitor mapSymbolVisitor = new MapSymbolVisitor(fn);
			return mapSymbolVisitor.Visit(expr);
		}

		protected MapSymbolVisitor(Func<Symbol, Expression> fn)
		{
			_fn = fn;
		}

		public override Expression VisitSymbol(Symbol sym)
		{
			return _fn(sym);
		}
	}
}
