using System;
using System.Diagnostics;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	internal class TokenCursor
	{
		private IBufList<Token> _bltok;

		private int _itokCur;

		private Token _tokCur;

		private TokKind _tidCur;

		/// <summary> current token
		/// </summary>
		public virtual int ItokCur => _itokCur;

		/// <summary> current token
		/// </summary>
		public virtual Token TokCur => _tokCur;

		/// <summary> current token ID
		/// </summary>
		public virtual TokKind TidCur => _tidCur;

		public TokenCursor(IBufList<Token> bltok, int itokMin)
		{
			_bltok = bltok;
			MoveTo(itokMin);
		}

		[Conditional("LOG_TOK")]
		private void LogTok()
		{
			Console.WriteLine("{0}: ({1}, {2}) {3}", _itokCur, _tokCur.Span.Min, _tokCur.Span.Lim, _tokCur.DumpString());
		}

		internal virtual void HandleError(ErrorToken err)
		{
			throw new ArgumentException(Resources.ErrorToken);
		}

		protected virtual bool IsNoise(Token tok)
		{
			return false;
		}

		/// <summary> Move to
		/// </summary>
		/// <param name="itok"></param>
		public virtual void MoveTo(int itok)
		{
			while (true)
			{
				if (!_bltok.TryGet(itok, out var e))
				{
					throw new ArgumentOutOfRangeException(Resources.IndexOutOfRange);
				}
				_itokCur = itok;
				_tokCur = e;
				_tidCur = _tokCur.Kind;
				if (!IsNoise(_tokCur))
				{
					break;
				}
				if (_tidCur == TokKind.Error)
				{
					HandleError(_tokCur.As<ErrorToken>());
				}
				itok++;
			}
		}

		/// <summary> next token's ID
		/// </summary>
		/// <returns></returns>
		public virtual TokKind TidNext()
		{
			if (_tidCur != TokKind.Eof)
			{
				MoveTo(_itokCur + 1);
			}
			return _tidCur;
		}

		/// <summary> Token move
		/// </summary>
		/// <returns></returns>
		public virtual Token TokMove()
		{
			Token tokCur = _tokCur;
			TidNext();
			return tokCur;
		}

		/// <summary> Token move
		/// </summary>
		/// <param name="ctok"></param>
		/// <returns></returns>
		public virtual Token TokMove(int ctok)
		{
			Token tokCur = _tokCur;
			while (--ctok >= 0)
			{
				TidNext();
			}
			return tokCur;
		}

		/// <summary> Peek without taking
		/// </summary>
		/// <param name="ditok"></param>
		/// <returns></returns>
		public virtual Token TokPeek(int ditok)
		{
			Token e = _tokCur;
			if (ditok >= 0)
			{
				int num = _itokCur;
				while (ditok > 0 && _bltok.TryGet(++num, out e))
				{
					if (!IsNoise(e))
					{
						ditok--;
					}
				}
			}
			else
			{
				int num2 = _itokCur;
				while (ditok < 0 && --num2 >= 0)
				{
					Token token = _bltok[num2];
					if (!IsNoise(token))
					{
						e = token;
						ditok++;
					}
				}
			}
			return e;
		}

		/// <summary> Peek without taking
		/// </summary>
		/// <param name="ditok"></param>
		/// <returns></returns>
		public virtual TokKind TidPeek(int ditok)
		{
			Token token = TokPeek(ditok);
			if (token != null)
			{
				return token.Kind;
			}
			return TokKind.Eof;
		}
	}
}
