using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// lexer which takes underscore('_') as a normal character
	/// </summary>
	internal class OmlLexer : RewriteLexer
	{
		internal class OmlLexerImpl : RewriteLexerImpl
		{
			private OmlLexer _omlLex;

			public OmlLexerImpl(Lexer lex, IText tv, int ichInit, bool fLineStart)
				: base(lex, tv, ichInit, fLineStart)
			{
				_omlLex = (OmlLexer)lex;
			}

			protected override char ChNext()
			{
				char c = base.ChNext();
				if (_omlLex._recordExpr)
				{
					if (_omlLex._exprBuffer.Length == 0)
					{
						_omlLex._exprBufferStart = _ichMinTok + 1;
					}
					_omlLex._exprBuffer.Append(c);
				}
				return c;
			}
		}

		internal StringBuilder _exprBuffer;

		internal bool _recordExpr;

		internal int _exprBufferStart;

		public string SavedText => _exprBuffer.ToString();

		internal OmlLexer()
			: base(new NormStr.Pool())
		{
		}

		/// <summary>
		/// Is the character valid in an identifier
		/// </summary>
		/// <param name="ch"></param>
		/// <returns></returns>
		internal override bool IsIdentCh(char ch)
		{
			if (ch == '_')
			{
				return true;
			}
			return base.IsIdentCh(ch);
		}

		/// <summary>
		/// Is the character valid starter for an identifier
		/// </summary>
		/// <param name="ch"></param>
		/// <returns></returns>
		internal override bool IsIdentStart(char ch)
		{
			if (ch == '_')
			{
				return true;
			}
			return base.IsIdentStart(ch);
		}

		public void BeginSavingText()
		{
			_exprBuffer = new StringBuilder();
			_recordExpr = true;
		}

		public void EndSavingText()
		{
			_exprBuffer = null;
			_recordExpr = false;
		}

		internal override LexerImplBase CreateImpl(Lexer lex, IText tv, int ichInit, bool fLineStart)
		{
			return new OmlLexerImpl(this, tv, ichInit, fLineStart);
		}
	}
}
