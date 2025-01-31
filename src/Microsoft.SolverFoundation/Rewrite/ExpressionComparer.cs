using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ExpressionComparer : IEqualityComparer<Expression>, IEqualityComparer<object>
	{
		public static readonly ExpressionComparer Instance = new ExpressionComparer();

		private ExpressionComparer()
		{
		}

		public bool Equals(Expression expr1, Expression expr2)
		{
			if (expr1 == null)
			{
				return expr2 == null;
			}
			if (expr2 == null)
			{
				return false;
			}
			return expr1.Equivalent(expr2);
		}

		bool IEqualityComparer<object>.Equals(object obj1, object obj2)
		{
			if (obj1 == null)
			{
				return obj2 == null;
			}
			if (obj2 == null)
			{
				return false;
			}
			Expression expression = obj1 as Expression;
			Expression expression2 = obj2 as Expression;
			if (expression == null)
			{
				if (expression2 != null)
				{
					return false;
				}
				return obj1.Equals(obj2);
			}
			if (expression2 == null)
			{
				return false;
			}
			return expression.Equivalent(expression2);
		}

		public int GetHashCode(Expression expr)
		{
			return expr?.GetEquivalenceHash() ?? 0;
		}

		int IEqualityComparer<object>.GetHashCode(object obj)
		{
			if (obj == null)
			{
				return 0;
			}
			if (!(obj is Expression expression))
			{
				return obj.GetHashCode();
			}
			return expression.GetEquivalenceHash();
		}
	}
}
