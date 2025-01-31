namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class DiscreteUniformDistributionSymbol : DistributionSymbol
	{
		public override int RequiredArgumentCount => 2;

		internal DiscreteUniformDistributionSymbol(RewriteSystem rs)
			: base(rs, "DiscreteUniformDistribution")
		{
		}
	}
}
