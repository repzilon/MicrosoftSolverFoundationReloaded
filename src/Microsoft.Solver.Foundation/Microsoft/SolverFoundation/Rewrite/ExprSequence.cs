using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class ExprSequence : Expression, IEnumerable<Expression>, IEnumerable
	{
		public override Expression Head => base.Rewrite.Builtin.Sequence;

		public abstract bool IsCached { get; }

		internal virtual Expression[] Values => null;

		protected ExprSequence(RewriteSystem rs)
			: base(rs)
		{
		}

		public override string ToString()
		{
			return "Sequence[...]";
		}

		public abstract IEnumerator<Expression> GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
