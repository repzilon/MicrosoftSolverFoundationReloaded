namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ExponentialDistributionSymbol : DistributionSymbol
	{
		public override int RequiredArgumentCount => 1;

		internal ExponentialDistributionSymbol(RewriteSystem rs)
			: base(rs, "ExponentialDistribution")
		{
		}
	}
}
