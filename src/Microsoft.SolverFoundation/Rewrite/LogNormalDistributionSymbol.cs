namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class LogNormalDistributionSymbol : DistributionSymbol
	{
		public override int RequiredArgumentCount => 2;

		internal LogNormalDistributionSymbol(RewriteSystem rs)
			: base(rs, "LogNormalDistribution")
		{
		}
	}
}
