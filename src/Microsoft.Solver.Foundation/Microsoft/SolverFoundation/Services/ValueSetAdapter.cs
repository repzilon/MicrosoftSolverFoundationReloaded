using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class ValueSetAdapter : ExprSequence
	{
		internal ValueSet _set;

		public override Expression Head => base.Rewrite.Builtin.Root;

		public override bool IsCached => false;

		public ValueSetAdapter(RewriteSystem rs, Domain domain)
			: base(rs)
		{
			_set = ValueSet.Create(domain);
		}

		public ValueSetAdapter(RewriteSystem rs, ValueSet set)
			: base(rs)
		{
			_set = set;
		}

		public override bool Equivalent(Expression expr)
		{
			if (!(expr is ValueSetAdapter valueSetAdapter))
			{
				return false;
			}
			return valueSetAdapter._set == _set;
		}

		public override int GetEquivalenceHash()
		{
			return GetHashCode();
		}

		public override IEnumerator<Expression> GetEnumerator()
		{
			foreach (object expr in _set._set)
			{
				if (Domain.TryCastToDouble(expr, out var dblValue))
				{
					yield return RationalConstant.Create(base.Rewrite, dblValue);
					continue;
				}
				if (expr is string)
				{
					yield return new StringConstant(base.Rewrite, (string)expr);
					continue;
				}
				throw new MsfException(Resources.InternalErrorBadSetElement);
			}
		}
	}
}
