namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class DistributionSymbol : Symbol
	{
		public abstract int RequiredArgumentCount { get; }

		internal DistributionSymbol(RewriteSystem rs, string name)
			: base(rs, name)
		{
		}
	}
}
