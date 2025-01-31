using System;
using System.IO;

namespace Microsoft.SolverFoundation.Common
{
	internal class CharReader
	{
		private readonly TextReader _rdr;

		private char[] _rgchBuf;

		private int _cchCur;

		private int _ichCur;

		private int _ichTot;

		private bool _fEof;

		public bool Eof => _fEof;

		public int IchCur => _ichTot;

		public char ChCur => _rgchBuf[_ichCur];

		public CharReader(TextReader rdr, int ichInit)
		{
			DebugContracts.NonNull(rdr);
			_rdr = rdr;
			_rgchBuf = new char[512];
			_cchCur = 0;
			_ichTot = ichInit;
			_fEof = false;
			ChNext();
			_ichTot = ichInit;
		}

		public char ChNext()
		{
			if (!_fEof)
			{
				_ichCur++;
				_ichTot++;
				if (_ichCur >= _cchCur)
				{
					_ichCur = 0;
					_cchCur = _rdr.ReadBlock(_rgchBuf, 0, _rgchBuf.Length);
					if (_cchCur == 0)
					{
						_fEof = true;
						_rgchBuf[0] = '\0';
					}
				}
			}
			return _rgchBuf[_ichCur];
		}

		public char ChPeek(int ich)
		{
			if (ich + _ichCur >= _cchCur)
			{
				if (_ichCur > 0)
				{
					Array.Copy(_rgchBuf, _ichCur, _rgchBuf, 0, _cchCur - _ichCur);
					_cchCur -= _ichCur;
					_ichCur = 0;
				}
				_cchCur += _rdr.ReadBlock(_rgchBuf, _cchCur, _rgchBuf.Length - _cchCur);
				if (ich >= _cchCur)
				{
					return '\0';
				}
			}
			return _rgchBuf[ich + _ichCur];
		}

		public void Close()
		{
			_fEof = true;
		}
	}
}
