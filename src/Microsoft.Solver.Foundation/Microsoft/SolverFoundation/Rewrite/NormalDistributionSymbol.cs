namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class NormalDistributionSymbol : DistributionSymbol
	{
		public override int RequiredArgumentCount => 2;

		internal NormalDistributionSymbol(RewriteSystem rs)
			: base(rs, "NormalDistribution")
		{
		}
	}
}
