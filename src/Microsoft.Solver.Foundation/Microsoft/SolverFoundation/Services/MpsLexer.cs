using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Tokenizer for MPS 
	/// </summary>
	internal class MpsLexer : Lexer
	{
		internal class MpsLexerImpl : LexerImplBase
		{
			internal Dictionary<NormStr, bool> _mpnsfSpecialSectionRow;

			public MpsLexerImpl(MpsLexer lex, IText tv)
				: base(lex, tv, 0, fLineStart: true)
			{
				_mpnsfSpecialSectionRow = new Dictionary<NormStr, bool>();
				_mpnsfSpecialSectionRow[Pool.Add(MpsTokKind.QSection.ToString())] = true;
				_mpnsfSpecialSectionRow[Pool.Add(MpsTokKind.CSection.ToString())] = true;
			}
		}

		internal class MpsFixedLexerImpl : MpsLexerImpl
		{
			private bool _fSectionRow;

			private int ColCur => _crdr.IchCur - _ichMinLine;

			public MpsFixedLexerImpl(MpsLexer lex, IText tv)
				: base(lex, tv)
			{
				_fSectionRow = false;
			}

			/// <summary>
			/// M4 internal
			/// </summary>
			/// <returns></returns>
			protected override Token Dispatch()
			{
				while (true)
				{
					if (_lex.IsLineTerm(base.ChCur))
					{
						LexLineTerm();
						_fSectionRow = false;
						continue;
					}
					if (base.Eof)
					{
						return null;
					}
					if (ColCur == 0 && !_lex.IsSpace(base.ChCur))
					{
						return LexSectionHead();
					}
					if (!_fSectionRow)
					{
						if (ColCur <= 1)
						{
							return LexKey();
						}
						if (ColCur <= 4)
						{
							return LexTextToken(4, 12);
						}
						if (ColCur <= 14)
						{
							return LexTextToken(14, 22);
						}
						if (ColCur <= 23)
						{
							return LexNumber(23, 38);
						}
						if (ColCur <= 39)
						{
							return LexTextToken(39, 47);
						}
						if (ColCur <= 48)
						{
							return LexNumber(48, 63);
						}
					}
					else if (ColCur <= 7)
					{
						break;
					}
					while (!base.Eof && !_lex.IsLineTerm(base.ChCur))
					{
						ChNext();
					}
				}
				return LexTextToken(7, 63);
			}

			private void SkipWhiteSpace(int colMin, int colLim)
			{
				while (ColCur < colMin)
				{
					char chCur = base.ChCur;
					if (base.Eof || _lex.IsLineTerm(chCur))
					{
						return;
					}
					ChNext();
					if (!_lex.IsSpace(chCur))
					{
						ReportError(Lexer.BadChar, chCur);
					}
				}
				while (ColCur < colLim && !base.Eof && _lex.IsSpace(base.ChCur))
				{
					ChNext();
				}
			}

			private Token LexSectionHead()
			{
				if (base.ChCur == '*')
				{
					while (!base.Eof && !_lex.IsLineTerm(base.ChCur))
					{
						ChNext();
					}
					return null;
				}
				StartTok();
				_sb.Length = 0;
				while (ColCur <= 7 && !base.Eof && !_lex.IsSpace(base.ChCur) && !_lex.IsLineTerm(base.ChCur))
				{
					_sb.Append(base.ChCur);
					ChNext();
				}
				NormStr normStr = Pool.Add(_sb);
				if (!_lex.IsKeyWord(normStr, out var tid) || tid.Tke < (TokKindEnum)3000 || tid.Tke >= (TokKindEnum)3017)
				{
					return new ErrorToken(Unexpected, normStr);
				}
				_fSectionRow = !_mpnsfSpecialSectionRow.ContainsKey(normStr);
				return new KeyToken(tid);
			}

			private Token LexKey()
			{
				SkipWhiteSpace(1, 3);
				if (ColCur >= 3 || base.Eof || _lex.IsLineTerm(base.ChCur))
				{
					return null;
				}
				StartTok();
				_sb.Length = 0;
				while (ColCur <= 8 && !base.Eof && !_lex.IsSpace(base.ChCur) && !_lex.IsLineTerm(base.ChCur))
				{
					_sb.Append(base.ChCur);
					ChNext();
				}
				NormStr normStr = Pool.Add(_sb);
				if (!_lex.IsKeyWord(normStr, out var tid) || tid.Tke < (TokKindEnum)4000 || tid.Tke >= (TokKindEnum)4033)
				{
					return new ErrorToken(Unexpected, normStr);
				}
				return new KeyToken(tid);
			}

			private Token LexTextToken(int colMin, int colLim)
			{
				SkipWhiteSpace(colMin, colLim);
				if (ColCur >= colLim)
				{
					return new IdentToken(Pool.Add(""));
				}
				if (base.Eof || _lex.IsLineTerm(base.ChCur))
				{
					return null;
				}
				StartTok();
				_sb.Length = 0;
				int length = 0;
				while (ColCur < colLim && !base.Eof && !_lex.IsLineTerm(base.ChCur))
				{
					_sb.Append(base.ChCur);
					if (!_lex.IsSpace(base.ChCur))
					{
						length = _sb.Length;
					}
					ChNext();
				}
				_sb.Length = length;
				return new IdentToken(Pool.Add(_sb));
			}

			private Token LexNumber(int colMin, int colLim)
			{
				SkipWhiteSpace(colMin, colLim);
				if (ColCur >= colLim || base.Eof || _lex.IsLineTerm(base.ChCur))
				{
					return null;
				}
				StartTok();
				bool fNegate = false;
				if (base.ChCur == '-')
				{
					fNegate = true;
					ChNext();
				}
				if (!_lex.IsDigit(base.ChCur) && (base.ChCur != '.' || !_lex.IsDigit(ChPeek(1))))
				{
					ReportError(Lexer.BadChar, base.ChCur);
					ChNext();
					return null;
				}
				bool flag = false;
				int num = 0;
				_sb.Length = 0;
				if (base.ChCur == '.')
				{
					flag = true;
				}
				else
				{
					_sb.Append(base.ChCur);
				}
				while (true)
				{
					ChNext();
					if (ColCur > colLim)
					{
						break;
					}
					if (_lex.IsDigit(base.ChCur))
					{
						if (_sb.Length > 0 || base.ChCur != '0')
						{
							_sb.Append(base.ChCur);
						}
						if (flag)
						{
							num--;
						}
					}
					else
					{
						if (base.ChCur != '.' || flag)
						{
							break;
						}
						flag = true;
					}
				}
				BigInteger bigInteger = 0;
				char c;
				if (ColCur <= colLim && (base.ChCur == 'e' || base.ChCur == 'E') && (_lex.IsDigit(c = ChPeek(1)) || ((c == '+' || c == '-') && _lex.IsDigit(ChPeek(2)))))
				{
					bool flag2 = false;
					ChNext();
					switch (c)
					{
					case '+':
						ChNext();
						break;
					case '-':
						flag2 = true;
						ChNext();
						break;
					}
					do
					{
						bigInteger = bigInteger * 10 + _lex.GetDecVal(base.ChCur);
					}
					while (_lex.IsDigit(ChNext()) && ColCur <= colLim);
					if (flag2)
					{
						bigInteger = -bigInteger;
					}
				}
				bigInteger += (BigInteger)num;
				if (ColCur > colLim)
				{
					ReportError(NumberTooLong, colLim);
				}
				return LexFloatNum(bigInteger, '\0', fNegate);
			}
		}

		private class MpsFreeLexerImpl : MpsLexerImpl
		{
			private int _fldNext;

			private bool _fSectionRow;

			private bool _fSpecialRow;

			private int ColCur => _crdr.IchCur - _ichMinLine;

			public MpsFreeLexerImpl(MpsLexer lex, IText tv)
				: base(lex, tv)
			{
				_fSectionRow = false;
			}

			protected override Token Dispatch()
			{
				while (true)
				{
					SkipWhiteSpace();
					while (_lex.IsLineTerm(base.ChCur))
					{
						LexLineTerm();
						SkipWhiteSpace();
						_fSectionRow = false;
						_fSpecialRow = false;
						_fldNext = 0;
					}
					if (base.Eof)
					{
						return null;
					}
					if (ColCur == 0)
					{
						return LexSectionHead();
					}
					if (!_fSectionRow || _fSpecialRow)
					{
						if (ColCur == 1 || ColCur == 2)
						{
							break;
						}
						switch (_fldNext++)
						{
						case 0:
							return LexTextToken();
						case 1:
						{
							Token token = LexTextToken();
							if (token.Kind == TokKind.Ident && (token.As<IdentToken>().Val.ToString() == "'MARKER'" || _fSpecialRow))
							{
								_fldNext = 3;
							}
							return token;
						}
						case 3:
							return LexTextToken();
						case 2:
						case 4:
							return LexNumber();
						}
					}
					while (!base.Eof && !_lex.IsLineTerm(base.ChCur))
					{
						ChNext();
					}
				}
				return LexKey();
			}

			private void SkipWhiteSpace()
			{
				while (!base.Eof && _lex.IsSpace(base.ChCur))
				{
					ChNext();
				}
			}

			private Token LexSectionHead()
			{
				if (base.ChCur == '*')
				{
					while (!base.Eof && !_lex.IsLineTerm(base.ChCur))
					{
						ChNext();
					}
					return null;
				}
				StartTok();
				_sb.Length = 0;
				while (ColCur <= 7 && !base.Eof && !_lex.IsSpace(base.ChCur) && !_lex.IsLineTerm(base.ChCur))
				{
					_sb.Append(base.ChCur);
					ChNext();
				}
				NormStr normStr = Pool.Add(_sb);
				if (!_lex.IsKeyWord(normStr, out var tid) || tid.Tke < (TokKindEnum)3000 || tid.Tke >= (TokKindEnum)3017)
				{
					return new ErrorToken(Unexpected, normStr);
				}
				_fSectionRow = false;
				_fSpecialRow = _mpnsfSpecialSectionRow.ContainsKey(normStr);
				return new KeyToken(tid);
			}

			private Token LexKey()
			{
				StartTok();
				_sb.Length = 0;
				while (ColCur <= 8 && !base.Eof && !_lex.IsSpace(base.ChCur) && !_lex.IsLineTerm(base.ChCur))
				{
					_sb.Append(base.ChCur);
					ChNext();
				}
				NormStr normStr = Pool.Add(_sb);
				if (!_lex.IsKeyWord(normStr, out var tid) || tid.Tke < (TokKindEnum)4000 || tid.Tke >= (TokKindEnum)4033)
				{
					return new ErrorToken(Unexpected, normStr);
				}
				return new KeyToken(tid);
			}

			private Token LexTextToken()
			{
				StartTok();
				_sb.Length = 0;
				while (!base.Eof && !_lex.IsSpace(base.ChCur) && !_lex.IsLineTerm(base.ChCur))
				{
					_sb.Append(base.ChCur);
					ChNext();
				}
				return new IdentToken(Pool.Add(_sb));
			}

			private Token LexNumber()
			{
				SkipWhiteSpace();
				if (base.Eof || _lex.IsLineTerm(base.ChCur))
				{
					return null;
				}
				StartTok();
				bool fNegate = false;
				if (base.ChCur == '-')
				{
					fNegate = true;
					ChNext();
				}
				if (!_lex.IsDigit(base.ChCur) && (base.ChCur != '.' || !_lex.IsDigit(ChPeek(1))))
				{
					char chCur = base.ChCur;
					ChNext();
					ReportError(Lexer.BadChar, chCur);
					return null;
				}
				bool flag = false;
				int num = 0;
				_sb.Length = 0;
				if (base.ChCur == '.')
				{
					flag = true;
				}
				else
				{
					_sb.Append(base.ChCur);
				}
				while (true)
				{
					ChNext();
					if (_lex.IsDigit(base.ChCur))
					{
						if (_sb.Length > 0 || base.ChCur != '0')
						{
							_sb.Append(base.ChCur);
						}
						if (flag)
						{
							num--;
						}
					}
					else
					{
						if (base.ChCur != '.' || flag)
						{
							break;
						}
						flag = true;
					}
				}
				BigInteger bigInteger = 0;
				char c;
				if ((base.ChCur == 'e' || base.ChCur == 'E') && (_lex.IsDigit(c = ChPeek(1)) || ((c == '+' || c == '-') && _lex.IsDigit(ChPeek(2)))))
				{
					bool flag2 = false;
					ChNext();
					switch (c)
					{
					case '+':
						ChNext();
						break;
					case '-':
						flag2 = true;
						ChNext();
						break;
					}
					do
					{
						bigInteger = bigInteger * 10 + _lex.GetDecVal(base.ChCur);
					}
					while (_lex.IsDigit(ChNext()));
					if (flag2)
					{
						bigInteger = -bigInteger;
					}
				}
				bigInteger += (BigInteger)num;
				return LexFloatNum(bigInteger, '\0', fNegate);
			}
		}

		internal static readonly ErrObj ExpectedChar = new ErrObj(200, Resources.ErrObjExpectedCharacter0);

		internal static readonly ErrObj NumberTooLong = new ErrObj(202, Resources.ErrObjNumberPastColumn0);

		internal static readonly ErrObj Unexpected = new ErrObj(201, Resources.ErrObjUnexpected0);

		private bool _fFixedFormat;

		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="pool"></param>
		public MpsLexer(NormStr.Pool pool)
			: base(pool)
		{
			AddSingleKeyWord(MpsTokKind.Name);
			AddSingleKeyWord(MpsTokKind.ObjSense);
			AddSingleKeyWord(MpsTokKind.ObjSenseMaximize);
			AddSingleKeyWord(MpsTokKind.ObjSenseMax);
			AddSingleKeyWord(MpsTokKind.ObjSenseMinimize);
			AddSingleKeyWord(MpsTokKind.ObjSenseMin);
			AddSingleKeyWord(MpsTokKind.Rows);
			AddSingleKeyWord(MpsTokKind.Columns);
			AddSingleKeyWord(MpsTokKind.SETS);
			AddSingleKeyWord(MpsTokKind.SOS);
			AddSingleKeyWord(MpsTokKind.SOSS1);
			AddSingleKeyWord(MpsTokKind.SOSS2);
			AddSingleKeyWord(MpsTokKind.Rhs);
			AddSingleKeyWord(MpsTokKind.Ranges);
			AddSingleKeyWord(MpsTokKind.Bounds);
			AddSingleKeyWord(MpsTokKind.Quadobj);
			AddSingleKeyWord(MpsTokKind.QSection);
			AddSingleKeyWord(MpsTokKind.CSection);
			AddSingleKeyWord(MpsTokKind.EndData);
			AddSingleKeyWord(MpsTokKind.Equal);
			AddSingleKeyWord(MpsTokKind.LessEqual);
			AddSingleKeyWord(MpsTokKind.GreaterEqual);
			AddSingleKeyWord(MpsTokKind.Objective);
			AddSingleKeyWord(MpsTokKind.LowerBound);
			AddSingleKeyWord(MpsTokKind.UpperBound);
			AddSingleKeyWord(MpsTokKind.FixedVariable);
			AddSingleKeyWord(MpsTokKind.FreeVariable);
			AddSingleKeyWord(MpsTokKind.NoLowerBound);
			AddSingleKeyWord(MpsTokKind.NoUpperBound);
			AddSingleKeyWord(MpsTokKind.BinaryBound);
			AddSingleKeyWord(MpsTokKind.IntegerLowerBound);
			AddSingleKeyWord(MpsTokKind.IntegerUpperBound);
		}

		internal IEnumerable<Token> LexSource(IText tv, bool fFixedFormat)
		{
			_fFixedFormat = fFixedFormat;
			return base.LexSource(tv, 0, fLineStart: true);
		}

		/// <summary>
		/// M4 internal
		/// </summary>
		/// <param name="tv"></param>
		/// <param name="ichInit"></param>
		/// <param name="fLineStart"></param>
		/// <returns></returns>
		internal override IEnumerable<Token> LexSource(IText tv, int ichInit, bool fLineStart)
		{
			_fFixedFormat = false;
			return base.LexSource(tv, 0, fLineStart: true);
		}

		/// <summary>
		/// M4 internal 
		/// </summary>
		/// <param name="lex"></param>
		/// <param name="tv"></param>
		/// <param name="ichInit"></param>
		/// <param name="fLineStart"></param>
		/// <returns></returns>
		internal override LexerImplBase CreateImpl(Lexer lex, IText tv, int ichInit, bool fLineStart)
		{
			if (_fFixedFormat)
			{
				return new MpsFixedLexerImpl(this, tv);
			}
			return new MpsFreeLexerImpl(this, tv);
		}
	}
}
