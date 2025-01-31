using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ExprSequenceConcat : ExprSequence
	{
		private const int khashBase = 1518443314;

		private ExprSequence[] _rgseq;

		private int _hash;

		public override bool IsCached => false;

		internal ExprSequenceConcat(RewriteSystem rs, ExprSequence[] rgseq)
			: base(rs)
		{
			_rgseq = rgseq;
		}

		public override IEnumerator<Expression> GetEnumerator()
		{
			try
			{
				ExprSequence[] rgseq = _rgseq;
				foreach (ExprSequence seq in rgseq)
				{
					foreach (Expression item in seq)
					{
						yield return item;
					}
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
				_hash = 1518443314;
				ExprSequence[] rgseq = _rgseq;
				foreach (ExprSequence exprSequence in rgseq)
				{
					_hash = Statics.CombineHash(_hash, exprSequence.GetEquivalenceHash());
				}
				if (_hash == 0)
				{
					_hash = 1518443314;
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
			if (!(expr is ExprSequenceConcat exprSequenceConcat))
			{
				return false;
			}
			if (exprSequenceConcat._rgseq == _rgseq)
			{
				return true;
			}
			if (exprSequenceConcat._rgseq.Length != _rgseq.Length)
			{
				return false;
			}
			int num = _rgseq.Length;
			while (--num >= 0)
			{
				if (!_rgseq[num].Equivalent(exprSequenceConcat._rgseq[num]))
				{
					return false;
				}
			}
			return true;
		}
	}
}
