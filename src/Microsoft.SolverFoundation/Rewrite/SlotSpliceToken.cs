using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class SlotSpliceToken : Token
	{
		private readonly int _iv;

		public int Index => _iv;

		public SlotSpliceToken(int iv)
			: base(RewriteTokKind.SlotSplice)
		{
			_iv = iv;
		}
	}
}
