namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class BinomialDistributionSymbol : DistributionSymbol
	{
		public override int RequiredArgumentCount => 2;

		internal BinomialDistributionSymbol(RewriteSystem rs)
			: base(rs, "BinomialDistribution")
		{
		}
	}
}
