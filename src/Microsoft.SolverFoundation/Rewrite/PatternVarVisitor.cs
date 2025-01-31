using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class PatternVarVisitor : Visitor
	{
		private Func<Symbol, bool> _fn;

		public static bool VisitPatternVars(Expression expr, Func<Symbol, bool> fn)
		{
			PatternVarVisitor patternVarVisitor = new PatternVarVisitor(fn);
			return patternVarVisitor.Visit(expr);
		}

		public static bool HasPatternVars(Expression expr)
		{
			return !VisitPatternVars(expr, (Symbol sym) => false);
		}

		public static bool HasPatternVar(Expression expr, Symbol sym)
		{
			return !VisitPatternVars(expr, (Symbol symCur) => symCur != sym);
		}

		protected PatternVarVisitor(Func<Symbol, bool> fn)
		{
			_fn = fn;
		}

		public override bool PreVisitInvocation(Invocation inv)
		{
			if (inv.Head == inv.Rewrite.Builtin.Pattern && inv.Arity == 2 && inv[0] is Symbol arg)
			{
				return _fn(arg);
			}
			return true;
		}
	}
}
