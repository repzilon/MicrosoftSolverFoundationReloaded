using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class ExpSymbol : PowerSymbolBase
	{
		private static Expression _e;

		internal ExpSymbol(RewriteSystem rs)
			: base(rs, "Exp")
		{
			AddAttributes(rs.Attributes.Listable);
			if (_e == null)
			{
				_e = RationalConstant.Create(rs, Math.E);
			}
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			ib.Insert(0, _e);
			return base.EvaluateInvocationArgs(ib);
		}
	}
}
