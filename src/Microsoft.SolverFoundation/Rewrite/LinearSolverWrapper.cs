using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class LinearSolverWrapper : Constant
	{
		public abstract LinearModel Model { get; }

		public override object ObjectValue => Model;

		public LinearSolverWrapper(SolveRewriteSystem rs)
			: base(rs)
		{
		}

		public override bool Equivalent(Expression expr)
		{
			return this == expr;
		}

		public override int GetEquivalenceHash()
		{
			return Model.GetHashCode();
		}
	}
}
