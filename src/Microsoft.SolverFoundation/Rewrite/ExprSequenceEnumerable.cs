using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class ExprSequenceEnumerable : ExprSequence
	{
		private const int khashBase = 728958117;

		private IEnumerable<Expression> _rgexpr;

		private int _hash;

		public override bool IsCached => false;

		public ExprSequenceEnumerable(RewriteSystem rs, IEnumerable<Expression> rgexpr)
			: base(rs)
		{
			_rgexpr = rgexpr;
		}

		public override IEnumerator<Expression> GetEnumerator()
		{
			return _rgexpr.GetEnumerator();
		}

		public override int GetEquivalenceHash()
		{
			if (_hash == 0)
			{
				_hash = Statics.CombineHash(728958117, _rgexpr.GetHashCode());
				if (_hash == 0)
				{
					_hash = 728958117;
				}
			}
			return _hash;
		}

		public override bool Equivalent(Expression expr)
		{
			if (expr == this)
			{
				return true;
			}
			if (!(expr is ExprSequenceEnumerable exprSequenceEnumerable))
			{
				return false;
			}
			return exprSequenceEnumerable._rgexpr == _rgexpr;
		}
	}
}
