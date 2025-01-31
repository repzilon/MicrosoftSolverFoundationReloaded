using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ExprSequenceArray : ExprSequence
	{
		private const int khashBase = 728958117;

		private Expression[] _rgexpr;

		private int _hash;

		public override bool IsCached => true;

		internal override Expression[] Values => _rgexpr;

		internal ExprSequenceArray(RewriteSystem rs, Expression[] rgexpr)
			: base(rs)
		{
			_rgexpr = rgexpr;
		}

		public override IEnumerator<Expression> GetEnumerator()
		{
			try
			{
				Expression[] rgexpr = _rgexpr;
				for (int i = 0; i < rgexpr.Length; i++)
				{
					yield return rgexpr[i];
				}
			}
			finally
			{
			}
		}

		public override int GetEquivalenceHash()
		{
			if (_hash == 0)
			{
				_hash = 728958117;
				Expression[] rgexpr = _rgexpr;
				foreach (Expression expression in rgexpr)
				{
					_hash = Statics.CombineHash(_hash, expression.GetEquivalenceHash());
				}
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
			if (!(expr is ExprSequenceArray exprSequenceArray))
			{
				return false;
			}
			if (exprSequenceArray._rgexpr == _rgexpr)
			{
				return true;
			}
			if (exprSequenceArray._rgexpr.Length != _rgexpr.Length)
			{
				return false;
			}
			int num = _rgexpr.Length;
			while (--num >= 0)
			{
				if (!_rgexpr[num].Equivalent(exprSequenceArray._rgexpr[num]))
				{
					return false;
				}
			}
			return true;
		}
	}
}
