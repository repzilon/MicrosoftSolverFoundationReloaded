using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class ExpressionVisitor : Visitor
	{
		private readonly Func<Expression, bool> _fn;

		public static bool VisitExpressions(Expression expr, Func<Expression, bool> fn)
		{
			ExpressionVisitor expressionVisitor = new ExpressionVisitor(fn);
			return expressionVisitor.Visit(expr);
		}

		public static bool Contains(Expression exprSrc, Expression exprFind)
		{
			return !VisitExpressions(exprSrc, (Expression exprCur) => !exprCur.Equivalent(exprFind));
		}

		public static bool ContainsMatch(Expression exprSrc, Expression exprFind)
		{
			return !VisitExpressions(exprSrc, (Expression exprCur) => !exprCur.Rewrite.Match(exprFind, exprCur));
		}

		protected ExpressionVisitor(Func<Expression, bool> fn)
		{
			_fn = fn;
		}

		public override bool Visit(Expression expr)
		{
			if (!_fn(expr))
			{
				return false;
			}
			return base.Visit(expr);
		}
	}
}
