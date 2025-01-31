using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class RewriteLexer : Lexer
	{
		internal class RewriteLexerImpl : LexerImplBase
		{
			public RewriteLexerImpl(Lexer lex, IText tv, int ichInit, bool fLineStart)
				: base(lex, tv, ichInit, fLineStart)
			{
			}

			protected override Token LexPunc()
			{
				char chCur = base.ChCur;
				if (chCur != '#')
				{
					return base.LexPunc();
				}
				long num = 0L;
				bool flag = false;
				if (ChNext() == '#')
				{
					flag = true;
					ChNext();
				}
				if (_lex.IsDigit(base.ChCur))
				{
					num = _lex.GetDecVal(base.ChCur);
					while (_lex.IsDigit(ChNext()))
					{
						num = num * 10 + _lex.GetDecVal(base.ChCur);
						if (num > int.MaxValue)
						{
							num = 2147483647L;
						}
					}
				}
				if (!flag)
				{
					return new SlotToken((int)num);
				}
				return new SlotSpliceToken((int)num);
			}

			protected override bool FLexEscChar(bool fUniOnly, out uint u)
			{
				if (fUniOnly)
				{
					u = 0u;
					return false;
				}
				return base.FLexEscChar(fUniOnly, out u);
			}
		}

		public RewriteLexer(NormStr.Pool pool)
			: base(pool)
		{
		}

		internal override void AddKeyWords()
		{
			AddSingleKeyWord(RewriteTokKind.True);
			AddSingleKeyWord(RewriteTokKind.False);
			AddSingleKeyWord(RewriteTokKind.Infinity);
			AddSingleKeyWord(RewriteTokKind.UnsignedInfinity);
			AddSingleKeyWord(RewriteTokKind.Indeterminate);
		}

		internal override void AddPunctuators()
		{
			base.AddPunctuators();
			AddSinglePunctuator(RewriteTokKind.Hole);
			AddSinglePunctuator(RewriteTokKind.TripleHole);
			AddSinglePunctuator(RewriteTokKind.AssignDelayed);
			AddSinglePunctuator(RewriteTokKind.RuleImmed);
			AddSinglePunctuator(RewriteTokKind.RuleDelayed);
			AddSinglePunctuator(RewriteTokKind.RuleApplyOnce);
			AddSinglePunctuator(RewriteTokKind.RuleApplyMany);
			AddSinglePunctuator(RewriteTokKind.Conditional);
			AddSinglePunctuator(RewriteTokKind.Unset);
			AddSinglePunctuator(RewriteTokKind.SquareColonOpen);
			AddSinglePunctuator(RewriteTokKind.SquareColonClose);
			AddSinglePunctuator(RewriteTokKind.EquEquEqu);
			AddSinglePunctuator(RewriteTokKind.NotEquEqu);
			AddSinglePunctuator(RewriteTokKind.CaretOr);
			AddSinglePunctuator(RewriteTokKind.MinusColon);
			AddSinglePunctuator(RewriteTokKind.ExcelInputBinding);
			AddSinglePunctuator(RewriteTokKind.ExcelOutputBinding);
			AddSinglePunctuator(TokKind.LssLss);
			AddSinglePunctuator(TokKind.GrtGrt);
			AddSinglePunctuator(TokKind.EquEqu);
			AddSinglePunctuator(TokKind.NotEqu);
			AddSinglePunctuator(TokKind.AndAnd);
			AddSinglePunctuator(TokKind.OrOr);
		}

		/// <summary> Is the character valid starter for an identifier
		/// </summary>
		/// <param name="ch"></param>
		/// <returns></returns>
		internal override bool IsIdentStart(char ch)
		{
			if (ch == '_')
			{
				return false;
			}
			return base.IsIdentStart(ch);
		}

		/// <summary> Is the character valid in an identifier
		/// </summary>
		/// <param name="ch"></param>
		/// <returns></returns>
		internal override bool IsIdentCh(char ch)
		{
			if (ch == '_')
			{
				return false;
			}
			return base.IsIdentCh(ch);
		}

		internal override LexerImplBase CreateImpl(Lexer lex, IText tv, int ichInit, bool fLineStart)
		{
			return new RewriteLexerImpl(this, tv, ichInit, fLineStart);
		}
	}
}
