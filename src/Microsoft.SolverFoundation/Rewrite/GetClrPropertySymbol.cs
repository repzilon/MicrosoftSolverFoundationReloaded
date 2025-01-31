using System.Reflection;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class GetClrPropertySymbol : GetClrValueSymbol
	{
		internal GetClrPropertySymbol(RewriteSystem rs)
			: base(rs, BindingFlags.Public | BindingFlags.GetProperty, "GetClrProperty")
		{
		}
	}
}
