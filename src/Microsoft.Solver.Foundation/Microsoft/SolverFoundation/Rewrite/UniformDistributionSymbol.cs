namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class UniformDistributionSymbol : DistributionSymbol
	{
		public override int RequiredArgumentCount => 2;

		internal UniformDistributionSymbol(RewriteSystem rs)
			: base(rs, "UniformDistribution")
		{
		}
	}
}
