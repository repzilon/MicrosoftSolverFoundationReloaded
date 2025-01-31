namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class GeometricDistributionSymbol : DistributionSymbol
	{
		public override int RequiredArgumentCount => 1;

		internal GeometricDistributionSymbol(RewriteSystem rs)
			: base(rs, "GeometricDistribution")
		{
		}
	}
}
