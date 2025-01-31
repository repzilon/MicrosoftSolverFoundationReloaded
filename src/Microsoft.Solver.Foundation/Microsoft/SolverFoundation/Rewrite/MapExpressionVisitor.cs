using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class MapExpressionVisitor : RewriteVisitor
	{
		private RefFunction<Expression, bool> _fn;

		public static Expression VisitExpressions(Expression expr, RefFunction<Expression, bool> fn)
		{
			MapExpressionVisitor mapExpressionVisitor = new MapExpressionVisitor(fn);
			return mapExpressionVisitor.Visit(expr);
		}

		protected MapExpressionVisitor(RefFunction<Expression, bool> fn)
		{
			_fn = fn;
		}

		public override Expression Visit(Expression expr)
		{
			if (_fn(ref expr))
			{
				return expr;
			}
			return base.Visit(expr);
		}
	}
}
