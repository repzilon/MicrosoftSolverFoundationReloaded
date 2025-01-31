namespace Microsoft.SolverFoundation.Services
{
	internal class ProbabilityDomain : NumericRangeDomain
	{
		internal ProbabilityDomain()
			: base(0, 1, intRestricted: false)
		{
		}
	}
}
