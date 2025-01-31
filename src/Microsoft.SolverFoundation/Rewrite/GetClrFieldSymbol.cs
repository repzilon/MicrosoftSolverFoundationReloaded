using System.Reflection;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class GetClrFieldSymbol : GetClrValueSymbol
	{
		internal GetClrFieldSymbol(RewriteSystem rs)
			: base(rs, BindingFlags.Public | BindingFlags.GetField, "GetClrField")
		{
		}
	}
}
