namespace Microsoft.SolverFoundation.Common
{
	internal class IntLitToken : Token
	{
		private readonly BigInteger _val;

		private readonly IntLitKind _ilkSrc;

		private readonly IntLitKind _ilk;

		public BigInteger Val => _val;

		public IntLitKind LitKind => _ilk;

		public IntLitKind LitKindSrc => _ilkSrc;

		public IntLitToken(BigInteger val, IntLitKind ilk)
			: base(TokKind.IntLit)
		{
			_val = val;
			_ilk = ilk;
			_ilkSrc = ilk;
			if (val <= int.MaxValue)
			{
				return;
			}
			if (val <= uint.MaxValue)
			{
				if ((_ilk & IntLitKind.UnsLng) == 0)
				{
					_ilk |= IntLitKind.Uns;
				}
			}
			else if (val <= long.MaxValue)
			{
				_ilk |= IntLitKind.Lng;
			}
			else if (val <= ulong.MaxValue)
			{
				_ilk |= IntLitKind.UnsLng;
			}
			else
			{
				_ilk |= IntLitKind.Big;
			}
		}

		public override string ToString()
		{
			string text = (((_ilk & IntLitKind.Hex) != 0) ? ("0x" + _val.ToHexString()) : _val.ToString());
			if ((_ilk & IntLitKind.Uns) != 0)
			{
				text += "u";
			}
			if ((_ilk & IntLitKind.Lng) != 0)
			{
				text += "L";
			}
			return text;
		}
	}
}
