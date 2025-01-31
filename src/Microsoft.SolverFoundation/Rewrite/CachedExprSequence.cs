using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class CachedExprSequence : ExprSequence
	{
		private const int khashBase = 2056504326;

		private ExprSequence _seqBase;

		private IEnumerable<Expression> _able;

		private IEnumerator<Expression> _ator;

		private List<Expression> _rgexpr;

		private int _hash;

		public override bool IsCached => true;

		public CachedExprSequence(ExprSequence seq)
			: base(seq.Rewrite)
		{
			_seqBase = seq;
			_able = seq;
		}

		public CachedExprSequence(RewriteSystem rs, IEnumerable<Expression> rgexpr)
			: base(rs)
		{
			_able = rgexpr;
			_seqBase = rgexpr as ExprSequence;
		}

		public override IEnumerator<Expression> GetEnumerator()
		{
			if (_rgexpr == null)
			{
				_rgexpr = new List<Expression>();
				_ator = _able.GetEnumerator();
			}
			int iv = 0;
			while (true)
			{
				if (iv < _rgexpr.Count)
				{
					yield return _rgexpr[iv++];
					continue;
				}
				if (_ator != null)
				{
					if (!_ator.MoveNext())
					{
						_ator.Dispose();
						_ator = null;
						break;
					}
					_rgexpr.Add(_ator.Current);
					continue;
				}
				break;
			}
		}

		public override bool Equivalent(Expression expr)
		{
			if (expr == this)
			{
				return true;
			}
			if (!(expr is CachedExprSequence cachedExprSequence))
			{
				return false;
			}
			if (_able == cachedExprSequence._able)
			{
				return true;
			}
			if (_seqBase == null || cachedExprSequence._seqBase == null)
			{
				return false;
			}
			return _seqBase.Equivalent(cachedExprSequence._seqBase);
		}

		public override int GetEquivalenceHash()
		{
			if (_hash == 0)
			{
				if (_seqBase != null)
				{
					_hash = Statics.CombineHash(2056504326, _seqBase.GetEquivalenceHash());
				}
				else
				{
					_hash = Statics.CombineHash(2056504326, _able.GetHashCode());
				}
				if (_hash == 0)
				{
					_hash = 2056504326;
				}
			}
			return _hash;
		}
	}
}
