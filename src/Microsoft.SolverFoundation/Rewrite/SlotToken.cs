using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class SlotToken : Token
	{
		private readonly int _iv;

		public int Index => _iv;

		public SlotToken(int iv)
			: base(RewriteTokKind.Slot)
		{
			_iv = iv;
		}
	}
}
