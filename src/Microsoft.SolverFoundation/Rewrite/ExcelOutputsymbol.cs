namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// Uses for two places
	/// 1. As first sun of model section, just with one arguments (string for the BindOut for all model)
	/// 2. As sum of Variables section, two arguments (var and string for the dataout)
	/// </summary>
	internal sealed class ExcelOutputsymbol : Symbol
	{
		internal ExcelOutputsymbol(RewriteSystem rs)
			: base(rs, "BindOut", new ParseInfo("->@", Precedence.Function, Precedence.Assign, ParseInfoOptions.CreateScope))
		{
		}
	}
}
