using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// The lexer. This is effectively a template. Call LexSource to get an Enumerable of tokens.
	/// </summary>
	internal class Lexer
	{
		internal class LexerImplBase : IDisposable
		{
			protected readonly Lexer _lex;

			protected TextReader _rdr;

			protected readonly CharReader _crdr;

			protected readonly IText _tv;

			protected bool _fLineStart;

			protected int _ichMinLine;

			protected StringBuilder _sb;

			protected int _ichMinTok;

			protected Queue<Token> _queue;

			protected virtual NormStr.Pool Pool => _lex._pool;

			/// <summary>
			/// Whether we've hit the end of input yet. If this returns true, ChCur will be zero.
			/// </summary>
			protected bool Eof => _crdr.Eof;

			/// <summary>
			/// The current character. Zero if we've hit the end of input.
			/// </summary>
			protected char ChCur => _crdr.ChCur;

			public LexerImplBase(Lexer lex, IText tv, int ichInit, bool fLineStart)
			{
				_lex = lex;
				_rdr = tv.GetReader(ichInit);
				_crdr = new CharReader(_rdr, ichInit);
				_tv = tv;
				_fLineStart = fLineStart;
				_ichMinLine = _crdr.IchCur;
				_sb = new StringBuilder();
				_queue = new Queue<Token>(4);
			}

			public virtual void Dispose()
			{
				if (_rdr != null)
				{
					_rdr.Dispose();
					_rdr = null;
				}
			}

			/// <summary>
			/// Advance to the next character and return it.
			/// </summary>
			protected virtual char ChNext()
			{
				return _crdr.ChNext();
			}

			/// <summary>
			/// Return the ich character without advancing the current position. Assumes (and asserts) that
			/// the buffer is large enough for everything from the current character to the peeked character.
			/// This model doesn't support unbounded look ahead.
			/// </summary>
			protected char ChPeek(int ich)
			{
				return _crdr.ChPeek(ich);
			}

			/// <summary>
			/// Marks the beginning of the current token.
			/// </summary>
			protected void StartTok()
			{
				_ichMinTok = _crdr.IchCur;
			}

			/// <summary>
			/// Called to embed an error token in the stream.
			/// </summary>
			protected void ReportError(ErrObj eid)
			{
				ReportError(_ichMinTok, _crdr.IchCur, eid, null);
			}

			protected void ReportError(ErrObj eid, params object[] args)
			{
				ReportError(_ichMinTok, _crdr.IchCur, eid, args);
			}

			protected void ReportError(int ichMin, int ichLim, ErrObj eid, params object[] args)
			{
				ErrorToken errorToken = new ErrorToken(eid, args);
				errorToken.SetExtents(GetTextSpan(ichMin, ichLim));
				_queue.Enqueue(errorToken);
			}

			protected TextSpan GetTextSpan(int ichMin, int ichLim)
			{
				return new TextSpan(_tv.Version, ichMin, ichLim);
			}

			/// <summary>
			/// Form and return the next token. Returns null to signal end of input.
			/// </summary>
			public Token GetNextToken()
			{
				while (_queue.Count == 0 && !Eof)
				{
					FetchTokens();
				}
				if (_queue.Count == 0)
				{
					return null;
				}
				Token token = _queue.Dequeue();
				if (_queue.Count > 0)
				{
					token.Nested = true;
				}
				return token;
			}

			/// <summary>
			/// Call once GetNextToken returns null if you need an Eof token.
			/// </summary>
			public EofToken GetEof()
			{
				EofToken eofToken = new EofToken();
				eofToken.SetExtents(GetTextSpan(_crdr.IchCur, _crdr.IchCur));
				return eofToken;
			}

			/// <summary>
			/// Form and enqueue the next token and associated tokens (errors and newlines).
			/// </summary>
			protected void FetchTokens()
			{
				Token token = FetchTokenCore();
				if (token != null)
				{
					_queue.Enqueue(token.SetExtents(GetTextSpan(_ichMinTok, _crdr.IchCur)));
				}
			}

			protected virtual Token FetchTokenCore()
			{
				StartTok();
				return Dispatch();
			}

			protected virtual Token Dispatch()
			{
				char chCur = ChCur;
				switch (chCur)
				{
				case '@':
					if (ChPeek(1) == '"')
					{
						return LexStrLit();
					}
					if (_lex.IsIdentStart(ChPeek(1)))
					{
						return LexIdent();
					}
					ChNext();
					return new ErrorToken(VerbatimLiteralExpected);
				case '"':
				case '\'':
					return LexStrLit();
				case '/':
					return LexComment();
				case '.':
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					return LexNumLit();
				default:
					if (_lex.IsIdentStart(chCur))
					{
						return LexIdent();
					}
					if (_lex.IsSpace(chCur))
					{
						return LexSpace();
					}
					if (_lex.IsLineTerm(chCur))
					{
						LexLineTerm();
						return null;
					}
					return LexPunc();
				}
			}

			/// <summary>
			/// Called to lex a punctuator.
			/// </summary>
			protected virtual Token LexPunc()
			{
				int num = 0;
				TokKind tid = TokKind.None;
				_sb.Length = 0;
				_sb.Append(ChCur);
				while (true)
				{
					NormStr nstr = _lex._pool.Add(_sb);
					if (!_lex.IsPunctuator(nstr, out var tid2))
					{
						break;
					}
					if (tid2 != TokKind.None)
					{
						tid = tid2;
						num = _sb.Length;
					}
					_sb.Append(ChPeek(_sb.Length));
				}
				if (num == 0)
				{
					return LexOther();
				}
				while (--num >= 0)
				{
					ChNext();
				}
				return new PuncToken(tid);
			}

			/// <summary>
			/// Called to lex a numeric literal or a Dot token.
			/// </summary>
			protected virtual Token LexNumLit()
			{
				if (ChCur == '.' && !_lex.IsDigit(ChPeek(1)))
				{
					return LexPunc();
				}
				if (ChCur == '0' && (ChPeek(1) == 'x' || ChPeek(1) == 'X') && _lex.IsHexDigit(ChPeek(2)))
				{
					ChNext();
					ChNext();
					return LexHexInt();
				}
				return LexDecLit();
			}

			protected virtual Token LexDecLit()
			{
				bool flag = false;
				bool flag2 = false;
				int num = 0;
				_sb.Length = 0;
				if (ChCur == '.')
				{
					flag2 = true;
				}
				else
				{
					_sb.Append(ChCur);
				}
				while (true)
				{
					if (ChNext() == '.')
					{
						if (flag2 || !_lex.IsDigit(ChPeek(1)))
						{
							break;
						}
						flag2 = true;
						continue;
					}
					if (!_lex.IsDigit(ChCur))
					{
						break;
					}
					if (_sb.Length > 0 || ChCur != '0')
					{
						_sb.Append(ChCur);
					}
					if (flag2)
					{
						num--;
					}
				}
				BigInteger bigInteger = 0;
				if (ChCur == 'e' || ChCur == 'E')
				{
					char c = ChPeek(1);
					if (_lex.IsDigit(c) || ((c == '+' || c == '-') && _lex.IsDigit(ChPeek(2))))
					{
						bool flag3 = false;
						flag = true;
						ChNext();
						switch (c)
						{
						case '+':
							ChNext();
							break;
						case '-':
							flag3 = true;
							ChNext();
							break;
						}
						do
						{
							bigInteger = bigInteger * 10 + _lex.GetDecVal(ChCur);
						}
						while (_lex.IsDigit(ChNext()));
						if (flag3)
						{
							bigInteger = -bigInteger;
						}
					}
				}
				bigInteger += (BigInteger)num;
				return LexDecLit(flag2 || flag, bigInteger);
			}

			protected virtual Token LexDecLit(bool fFloat, BigInteger bnExp)
			{
				char c = LexFloatSuffix();
				if (fFloat || c != 0)
				{
					return LexFloatNum(bnExp, c, fNegate: false);
				}
				return LexDecInt(LexIntSuffix());
			}

			/// <summary>
			/// Lex a hex literal optionally followed by an integer suffix. Asserts the current
			/// character is a hex digit.
			/// </summary>
			protected virtual Token LexHexInt()
			{
				BigInteger bigInteger = 0;
				uint num = 0u;
				uint num2 = 1u;
				do
				{
					if (num2 == 16777216)
					{
						bigInteger = num2 * bigInteger + num;
						num2 = 1u;
						num = 0u;
					}
					num2 <<= 4;
					num = (num << 4) + (uint)_lex.GetHexVal(ChCur);
				}
				while (_lex.IsHexDigit(ChNext()));
				if (num2 != 1)
				{
					bigInteger = num2 * bigInteger + num;
				}
				return new IntLitToken(bigInteger, LexIntSuffix() | IntLitKind.Hex);
			}

			/// <summary>
			/// Lex a decimal integer literal. The digits must be in _sb.
			/// </summary>
			protected virtual Token LexDecInt(IntLitKind ilk)
			{
				return new IntLitToken(LexBigInteger(), ilk);
			}

			protected virtual BigInteger LexBigInteger()
			{
				BigInteger bigInteger = 0;
				uint num = 0u;
				uint num2 = 1u;
				for (int i = 0; i < _sb.Length; i++)
				{
					if (num2 == 1000000000)
					{
						bigInteger = num2 * bigInteger + num;
						num2 = 1u;
						num = 0u;
					}
					num2 *= 10;
					num = 10 * num + (uint)_lex.GetDecVal(_sb[i]);
				}
				if (num2 != 1)
				{
					bigInteger = num2 * bigInteger + num;
				}
				return bigInteger;
			}

			/// <summary>
			/// Lex a real literal (eg, float, double or decimal). The characters should be in _sb.
			/// </summary>
			protected virtual Token LexFloatNum(BigInteger bnExp, char chSuf, bool fNegate)
			{
				int num = _sb.Length;
				while (num > 0 && _sb[num - 1] == '0')
				{
					num--;
				}
				if (num == 0)
				{
					return new DecimalLitToken(0, 0, chSuf);
				}
				if (num < _sb.Length)
				{
					bnExp += (BigInteger)(_sb.Length - num);
					_sb.Length = num;
				}
				BigInteger bn = LexBigInteger();
				if (fNegate)
				{
					BigInteger.Negate(ref bn);
				}
				return new DecimalLitToken(bn, bnExp, chSuf);
			}

			/// <summary>
			/// Lex an optional integer suffix (eg, U or L).
			/// </summary>
			protected virtual IntLitKind LexIntSuffix()
			{
				return IntLitKind.None;
			}

			/// <summary>
			/// Lex an optional real suffix (eg, F, D, M).
			/// </summary>
			protected virtual char LexFloatSuffix()
			{
				switch (ChCur)
				{
				case 'F':
				case 'f':
					ChNext();
					return 'f';
				case 'R':
				case 'r':
					ChNext();
					return 'r';
				default:
					return '\0';
				}
			}

			/// <summary>
			/// Lex a string or character literal.
			/// </summary>
			protected virtual Token LexStrLit()
			{
				_sb.Length = 0;
				char c;
				if (ChCur == '@')
				{
					c = '"';
					ChNext();
					ChNext();
					while (true)
					{
						char c2 = ChCur;
						if (c2 == '"')
						{
							ChNext();
							if (ChCur != '"')
							{
								break;
							}
							ChNext();
						}
						else if (_lex.IsLineTerm(c2))
						{
							c2 = LexLineTerm();
						}
						else
						{
							if (Eof)
							{
								ReportError(UnterminatedString);
								break;
							}
							ChNext();
						}
						_sb.Append(c2);
					}
				}
				else
				{
					c = ChCur;
					ChNext();
					while (true)
					{
						char ch = ChCur;
						if (ch == c || Eof || _lex.IsLineTerm(ch))
						{
							break;
						}
						if (ch == '\\')
						{
							if (!FLexEscChar(fUniOnly: false, out var u))
							{
								continue;
							}
							if (u < 65536)
							{
								ch = (char)u;
							}
							else
							{
								if (!ConvertToSurrogatePair(u, out var ch2, out ch))
								{
									continue;
								}
								_sb.Append(ch2);
							}
						}
						else
						{
							ChNext();
						}
						_sb.Append(ch);
					}
					if (ChCur != c)
					{
						ReportError(NewlineInConst);
					}
					else
					{
						ChNext();
					}
				}
				if (c == '"')
				{
					return new StrLitToken(_sb.ToString());
				}
				if (_sb.Length != 1)
				{
					ReportError((_sb.Length == 0) ? CharConstEmpty : CharConstTooLong);
				}
				return new CharLitToken((_sb.Length > 0) ? _sb[0] : '\0');
			}

			/// <summary>
			/// Lex a character escape. Returns true if successful (ch is valid).
			/// </summary>
			protected virtual bool FLexEscChar(bool fUniOnly, out uint u)
			{
				int ichCur = _crdr.IchCur;
				bool flag;
				int num;
				switch (ChNext())
				{
				case 'u':
					flag = true;
					num = 4;
					break;
				case 'U':
					flag = true;
					num = 8;
					break;
				default:
					{
						if (!fUniOnly)
						{
							switch (ChCur)
							{
							case 'X':
							case 'x':
								break;
							case '\'':
								u = 39u;
								goto IL_0108;
							case '"':
								u = 34u;
								goto IL_0108;
							case '\\':
								u = 92u;
								goto IL_0108;
							case '0':
								u = 0u;
								goto IL_0108;
							case 'a':
								u = 7u;
								goto IL_0108;
							case 'b':
								u = 8u;
								goto IL_0108;
							case 'f':
								u = 12u;
								goto IL_0108;
							case 'n':
								u = 10u;
								goto IL_0108;
							case 'r':
								u = 13u;
								goto IL_0108;
							case 't':
								u = 9u;
								goto IL_0108;
							case 'v':
								u = 11u;
								goto IL_0108;
							default:
								goto IL_0111;
								IL_0108:
								ChNext();
								return true;
							}
							flag = false;
							num = 4;
							break;
						}
						goto IL_0111;
					}
					IL_0111:
					ReportError(ichCur, _crdr.IchCur, BadEscape);
					u = 0u;
					return false;
				}
				bool flag2 = true;
				ChNext();
				u = 0u;
				for (int i = 0; i < num; i++)
				{
					if (!_lex.IsHexDigit(ChCur))
					{
						flag2 = i > 0;
						if (flag || !flag2)
						{
							ReportError(ichCur, _crdr.IchCur, BadEscape);
						}
						break;
					}
					u = (u << 4) + (uint)_lex.GetHexVal(ChCur);
					ChNext();
				}
				return flag2;
			}

			/// <summary>
			/// Convert the pair of characters to a surrogate pair.
			/// </summary>
			protected virtual bool ConvertToSurrogatePair(uint u, out char ch1, out char ch2)
			{
				if (u > 1114111)
				{
					ReportError(BadEscape);
					ch1 = (ch2 = '\0');
					return false;
				}
				ch1 = (char)((u - 65536) / 1024 + 55296);
				ch2 = (char)((u - 65536) % 1024 + 56320);
				return true;
			}

			/// <summary>
			/// Lex an identifier.
			/// </summary>
			protected virtual Token LexIdent()
			{
				bool fVerbatim = false;
				if (ChCur == '@')
				{
					fVerbatim = true;
					ChNext();
				}
				NormStr normStr = LexIdentCore(ref fVerbatim);
				if (normStr == null)
				{
					return null;
				}
				if (!fVerbatim && _lex.IsKeyWord(normStr, out var tid))
				{
					return new KeyToken(tid);
				}
				return new IdentToken(normStr);
			}

			protected virtual NormStr LexIdentCore(ref bool fVerbatim)
			{
				_sb.Length = 0;
				while (true)
				{
					char c;
					if (ChCur == '\\')
					{
						int ichCur = _crdr.IchCur;
						if (!FLexEscChar(fUniOnly: true, out var u))
						{
							break;
						}
						if (u > 65535 || !_lex.IsIdentCh(c = (char)u))
						{
							ReportError(ichCur, _crdr.IchCur, BadChar, _lex.GetUniEscape(u));
							break;
						}
						fVerbatim = true;
					}
					else
					{
						if (!_lex.IsIdentCh(ChCur))
						{
							break;
						}
						c = ChCur;
						ChNext();
					}
					if (!_lex.IsFormatCh(c))
					{
						_sb.Append(c);
					}
				}
				if (_sb.Length == 0)
				{
					return null;
				}
				return Pool.Add(_sb);
			}

			/// <summary>
			/// Lex a comment.
			/// </summary>
			protected virtual Token LexComment()
			{
				int ichCur = _crdr.IchCur;
				switch (ChPeek(1))
				{
				default:
					return LexPunc();
				case '/':
					ChNext();
					_sb.Length = 0;
					_sb.Append("//");
					while (!_lex.IsLineTerm(ChNext()) && !Eof)
					{
						_sb.Append(ChCur);
					}
					return new CommentToken(_sb.ToString());
				case '*':
					ChNext();
					_sb.Length = 0;
					_sb.Append("/*");
					ChNext();
					while (true)
					{
						if (Eof)
						{
							ReportError(ichCur, _crdr.IchCur, UnterminatedComment);
							break;
						}
						char c = ChCur;
						if (_lex.IsLineTerm(c))
						{
							c = LexLineTerm();
						}
						else
						{
							ChNext();
						}
						_sb.Append(c);
						if (c == '*' && ChCur == '/')
						{
							_sb.Append('/');
							ChNext();
							break;
						}
					}
					return new CommentToken(_sb.ToString());
				}
			}

			/// <summary>
			/// Lex a sequence of spacing characters.
			/// Always returns null.
			/// </summary>
			protected virtual Token LexSpace()
			{
				while (_lex.IsSpace(ChNext()))
				{
				}
				return null;
			}

			/// <summary>
			/// Lex a line termination character. Transforms CRLF into a single LF.
			/// Updates the line mapping.
			/// </summary>
			protected virtual char LexLineTerm()
			{
				int ichCur = _crdr.IchCur;
				if (ChCur == '\r' && ChPeek(1) == '\n')
				{
					ChNext();
				}
				char chCur = ChCur;
				ChNext();
				_queue.Enqueue(new NewLineToken().SetExtents(GetTextSpan(ichCur, _crdr.IchCur)));
				_fLineStart = true;
				_ichMinLine = _crdr.IchCur;
				return chCur;
			}

			/// <summary>
			/// Lex a pre-processing directive. The default implementation doesn't
			/// handle pre-processing.
			/// </summary>
			/// <returns></returns>
			protected virtual Token LexPreProc()
			{
				return LexOther();
			}

			/// <summary>
			/// Skip over an error character. Always returns null.
			/// REVIEW shonk: Should we skip over multiple?
			/// </summary>
			protected virtual Token LexOther()
			{
				_sb.Length = 0;
				_sb.AppendFormat("{0}({1})", ChCur, _lex.GetUniEscape(ChCur));
				ChNext();
				return new ErrorToken(BadChar, _sb.ToString());
			}
		}

		/// <summary>
		/// Bit masks of the UnicodeCategory enum. A couple extra values are defined
		/// for convenience for the C# lexical grammar.
		/// </summary>
		[Flags]
		internal enum UniCatFlags : uint
		{
			ConnectorPunctuation = 0x40000u,
			DecimalDigitNumber = 0x100u,
			Format = 0x8000u,
			LetterNumber = 0x200u,
			LowercaseLetter = 2u,
			ModifierLetter = 8u,
			NonSpacingMark = 0x20u,
			OtherLetter = 0x10u,
			SpaceSeparator = 0x800u,
			SpacingCombiningMark = 0x40u,
			TitlecaseLetter = 4u,
			UppercaseLetter = 1u,
			IdentStartChar = 0x21Fu,
			IdentPartChar = 0x4837Fu
		}

		/// <summary> VerbatimLiteralExpected
		/// </summary>
		internal static readonly ErrObj VerbatimLiteralExpected = new ErrObj(100, Resources.ErrObjKeywordIdentifierOrStringExpectedAfterVerbatimSpecifier);

		/// <summary> ErrObj IntOverflow
		/// </summary>
		internal static readonly ErrObj IntOverflow = new ErrObj(101, Resources.ErrObjIntegralConstantIsTooLarge);

		/// <summary> ErrObj FloatOverflow
		/// </summary>
		internal static readonly ErrObj FloatOverflow = new ErrObj(102, Resources.ErrObjFloatingPointConstantIsOutsideTheRangeOfType0);

		/// <summary> ErrObj UnterminatedString
		/// </summary>
		internal static readonly ErrObj UnterminatedString = new ErrObj(103, Resources.ErrObjUnterminatedStringLiteral);

		/// <summary> ErrObj NewlineInConst
		/// </summary>
		internal static readonly ErrObj NewlineInConst = new ErrObj(104, Resources.ErrObjNewlineInConstant);

		/// <summary> ErrObj CharConstEmpty
		/// </summary>
		internal static readonly ErrObj CharConstEmpty = new ErrObj(105, Resources.ErrObjEmptyCharacterLiteral);

		/// <summary> ErrObj CharConstTooLong
		/// </summary>
		internal static readonly ErrObj CharConstTooLong = new ErrObj(106, Resources.ErrObjTooManyCharactersInCharacterLiteral);

		/// <summary> ErrObj BadEscape
		/// </summary>
		internal static readonly ErrObj BadEscape = new ErrObj(107, Resources.ErrObjUnrecognizedEscapeSequence);

		/// <summary> ErrObj BadChar
		/// </summary>
		internal static readonly ErrObj BadChar = new ErrObj(108, Resources.ErrObjUnexpectedCharacter0);

		/// <summary> UnterminatedComment
		/// </summary>
		internal static readonly ErrObj UnterminatedComment = new ErrObj(109, Resources.ErrObjEndOfFileFoundExpected);

		private readonly NormStr.Pool _pool;

		private readonly Dictionary<NormStr, TokKind> _mpnstidKeyWord;

		private readonly Dictionary<NormStr, TokKind> _mpnstidPunc;

		internal NormStr.Pool Pool => _pool;

		/// <summary> The constructor. Caller must provide the name pool and key word table.
		/// </summary>
		public Lexer(NormStr.Pool pool)
		{
			DebugContracts.NonNull(pool);
			_pool = pool;
			_mpnstidKeyWord = new Dictionary<NormStr, TokKind>();
			_mpnstidPunc = new Dictionary<NormStr, TokKind>();
			AddKeyWords();
			AddPunctuators();
		}

		internal virtual IEnumerable<Token> LexSource(IText tv)
		{
			return LexSource(tv, 0, fLineStart: true);
		}

		internal virtual IEnumerable<Token> LexSource(IText tv, int ichInit, bool fLineStart)
		{
			LexerImplBase impl = CreateImpl(this, tv, ichInit, fLineStart);
			Token tok;
			try
			{
				while (true)
				{
					Token nextToken;
					tok = (nextToken = impl.GetNextToken());
					if (nextToken == null)
					{
						break;
					}
					yield return tok;
				}
				tok = impl.GetEof();
			}
			finally
			{
				impl.Dispose();
			}
			yield return tok;
		}

		internal virtual LexerImplBase CreateImpl(Lexer lex, IText tv, int ichInit, bool fLineStart)
		{
			return new LexerImplBase(this, tv, ichInit, fLineStart);
		}

		internal static UniCatFlags GetUniCatFlags(char ch)
		{
			return (UniCatFlags)(1 << (int)char.GetUnicodeCategory(ch));
		}

		internal virtual bool IsKeyWord(NormStr nstr, out TokKind tid)
		{
			return _mpnstidKeyWord.TryGetValue(nstr, out tid);
		}

		internal virtual void AddSingleKeyWord(string str, TokKind tid)
		{
			_mpnstidKeyWord.Add(_pool.Add(str), tid);
		}

		internal virtual void AddSingleKeyWord(TokKind tid)
		{
			_mpnstidKeyWord.Add(_pool.Add(tid.ToString()), tid);
		}

		internal virtual bool IsPunctuator(NormStr nstr, out TokKind tid)
		{
			return _mpnstidPunc.TryGetValue(nstr, out tid);
		}

		internal virtual void AddSinglePunctuator(TokKind tid)
		{
			if (!AddSinglePunctuator(tid.ToString(), tid))
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.PunctuatorAlreadyMapped, new object[1] { tid.ToString() }));
			}
		}

		internal virtual bool AddSinglePunctuator(string str, TokKind tid)
		{
			NormStr key = _pool.Add(str);
			if (_mpnstidPunc.TryGetValue(key, out var value))
			{
				if (value == tid)
				{
					return true;
				}
				if (value != TokKind.None)
				{
					return false;
				}
			}
			else if (str.Length > 1)
			{
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < str.Length - 1; i++)
				{
					stringBuilder.Append(str[i]);
					NormStr key2 = _pool.Add(stringBuilder);
					if (!_mpnstidPunc.TryGetValue(key2, out var _))
					{
						_mpnstidPunc.Add(key2, TokKind.None);
					}
				}
			}
			_mpnstidPunc[key] = tid;
			return true;
		}

		internal TokKind TidFromStr(string str)
		{
			NormStr normStr = _pool.Get(str);
			if (normStr == null)
			{
				return null;
			}
			if (_mpnstidKeyWord.TryGetValue(normStr, out var value))
			{
				return value;
			}
			if (_mpnstidPunc.TryGetValue(normStr, out value))
			{
				return value;
			}
			return null;
		}

		internal virtual bool IsIdentStart(char ch)
		{
			if (ch >= '\u0080')
			{
				return (GetUniCatFlags(ch) & UniCatFlags.IdentStartChar) != 0;
			}
			switch (ch)
			{
			case 'a':
			case 'b':
			case 'c':
			case 'd':
			case 'e':
			case 'f':
			case 'g':
			case 'h':
			case 'i':
			case 'j':
			case 'k':
			case 'l':
			case 'm':
			case 'n':
			case 'o':
			case 'p':
			case 'q':
			case 'r':
			case 's':
			case 't':
			case 'u':
			case 'v':
			case 'w':
			case 'x':
			case 'y':
			case 'z':
				return true;
			case 'A':
			case 'B':
			case 'C':
			case 'D':
			case 'E':
			case 'F':
			case 'G':
			case 'H':
			case 'I':
			case 'J':
			case 'K':
			case 'L':
			case 'M':
			case 'N':
			case 'O':
			case 'P':
			case 'Q':
			case 'R':
			case 'S':
			case 'T':
			case 'U':
			case 'V':
			case 'W':
			case 'X':
			case 'Y':
			case 'Z':
				return true;
			case '_':
				return true;
			default:
				return false;
			}
		}

		internal virtual bool IsIdentCh(char ch)
		{
			if (ch >= '\u0080')
			{
				return (GetUniCatFlags(ch) & UniCatFlags.IdentPartChar) != 0;
			}
			switch (ch)
			{
			case 'a':
			case 'b':
			case 'c':
			case 'd':
			case 'e':
			case 'f':
			case 'g':
			case 'h':
			case 'i':
			case 'j':
			case 'k':
			case 'l':
			case 'm':
			case 'n':
			case 'o':
			case 'p':
			case 'q':
			case 'r':
			case 's':
			case 't':
			case 'u':
			case 'v':
			case 'w':
			case 'x':
			case 'y':
			case 'z':
				return true;
			case 'A':
			case 'B':
			case 'C':
			case 'D':
			case 'E':
			case 'F':
			case 'G':
			case 'H':
			case 'I':
			case 'J':
			case 'K':
			case 'L':
			case 'M':
			case 'N':
			case 'O':
			case 'P':
			case 'Q':
			case 'R':
			case 'S':
			case 'T':
			case 'U':
			case 'V':
			case 'W':
			case 'X':
			case 'Y':
			case 'Z':
				return true;
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
				return true;
			case '_':
				return true;
			default:
				return false;
			}
		}

		internal virtual bool IsSpace(char ch)
		{
			if (ch >= '\u0080')
			{
				return (GetUniCatFlags(ch) & UniCatFlags.SpaceSeparator) != 0;
			}
			switch (ch)
			{
			case '\t':
			case '\v':
			case '\f':
			case ' ':
				return true;
			default:
				return false;
			}
		}

		internal virtual bool IsLineTerm(char ch)
		{
			switch (ch)
			{
			case '\n':
			case '\r':
			case '\u0085':
			case '\u2028':
			case '\u2029':
				return true;
			default:
				return false;
			}
		}

		internal virtual bool IsDigit(char ch)
		{
			return (uint)(ch - 48) <= 9u;
		}

		/// <summary>
		/// Get the value of a decimal digit.
		/// </summary>
		internal virtual int GetDecVal(char ch)
		{
			return ch - 48;
		}

		internal virtual bool IsHexDigit(char ch)
		{
			switch (ch)
			{
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
				return true;
			case 'A':
			case 'B':
			case 'C':
			case 'D':
			case 'E':
			case 'F':
				return true;
			default:
				return (uint)(ch - 97) <= 5u;
			}
		}

		/// <summary>
		/// Get the value of a hexadecimal digit.
		/// </summary>
		internal virtual int GetHexVal(char ch)
		{
			if (ch >= 'a')
			{
				return ch - 87;
			}
			if (ch >= 'A')
			{
				return ch - 55;
			}
			return ch - 48;
		}

		internal virtual bool IsFormatCh(char ch)
		{
			if (ch >= '\u0080')
			{
				return (GetUniCatFlags(ch) & UniCatFlags.Format) != 0;
			}
			return false;
		}

		/// <summary>
		/// Convert the given uint to a unicode escape.
		/// Note that the uint contains raw hex - not a surrogate pair.
		/// </summary>
		internal virtual string GetUniEscape(uint u)
		{
			if (u < 65536)
			{
				return string.Format(CultureInfo.InvariantCulture, "\\u{0:X4}", new object[1] { u });
			}
			return string.Format(CultureInfo.InvariantCulture, "\\U{0:X8}", new object[1] { u });
		}

		internal virtual void AddKeyWords()
		{
		}

		internal virtual void AddPunctuators()
		{
			AddSinglePunctuator(TokKind.Add);
			AddSinglePunctuator(TokKind.Sub);
			AddSinglePunctuator(TokKind.Grt);
			AddSinglePunctuator(TokKind.GrtEqu);
			AddSinglePunctuator(TokKind.Lss);
			AddSinglePunctuator(TokKind.LssEqu);
			AddSinglePunctuator(TokKind.And);
			AddSinglePunctuator(TokKind.Or);
			AddSinglePunctuator(TokKind.Mul);
			AddSinglePunctuator(TokKind.Div);
			AddSinglePunctuator(TokKind.Not);
			AddSinglePunctuator(TokKind.Equ);
			AddSinglePunctuator(TokKind.Mod);
			AddSinglePunctuator(TokKind.Xor);
			AddSinglePunctuator(TokKind.Quest);
			AddSinglePunctuator(TokKind.Colon);
			AddSinglePunctuator(TokKind.Tilde);
			AddSinglePunctuator(TokKind.Dot);
			AddSinglePunctuator(TokKind.Comma);
			AddSinglePunctuator(TokKind.Semi);
			AddSinglePunctuator(TokKind.Hash);
			AddSinglePunctuator(TokKind.Dollar);
			AddSinglePunctuator(TokKind.BackSlash);
			AddSinglePunctuator(TokKind.BackTick);
			AddSinglePunctuator(TokKind.CurlOpen);
			AddSinglePunctuator(TokKind.CurlClose);
			AddSinglePunctuator(TokKind.ParenOpen);
			AddSinglePunctuator(TokKind.ParenClose);
			AddSinglePunctuator(TokKind.SquareOpen);
			AddSinglePunctuator(TokKind.SquareClose);
		}
	}
}
