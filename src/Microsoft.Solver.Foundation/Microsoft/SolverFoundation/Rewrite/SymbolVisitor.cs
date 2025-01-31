using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class SymbolVisitor : Visitor
	{
		private readonly Func<Symbol, bool> _fn;

		public static bool VisitSymbols(Expression expr, Func<Symbol, bool> fn)
		{
			SymbolVisitor symbolVisitor = new SymbolVisitor(fn);
			return symbolVisitor.Visit(expr);
		}

		protected SymbolVisitor(Func<Symbol, bool> fn)
		{
			_fn = fn;
		}

		public override bool VisitSymbol(Symbol sym)
		{
			return _fn(sym);
		}
	}
}
