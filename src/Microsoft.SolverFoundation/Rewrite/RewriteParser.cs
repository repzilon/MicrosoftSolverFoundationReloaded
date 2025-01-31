using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class RewriteParser
	{
		internal enum RewriteParserErrorReason
		{
			UnexpectedTerm,
			IdentifierExpected
		}

		private readonly RewriteSystem _rs;

		private readonly RewriteLexer _lex;

		private readonly Dictionary<TokKind, Symbol> _mptidsymInfix;

		private readonly Dictionary<TokKind, Symbol> _mptidsymPrefix;

		private TokenCursor _curs;

		internal LineMapper _map;

		private List<Symbol> _rgsymLocals;

		private SymbolScope _scopeTop;

		private int _itokError = -1;

		internal bool _fStrict;

		internal readonly Stopwatch _sw;

		/// <summary> Construct a RewriteSystem
		/// </summary>
		protected RewriteSystem Rewrite => _rs;

		/// <summary> Read the Stopwatch
		/// </summary>
		protected Stopwatch Stopwatch => _sw;

		public TimeSpan EvaluationTime => _sw.Elapsed;

		internal virtual Token TokCur => _curs.TokCur;

		internal virtual TokKind TidCur => _curs.TidCur;

		protected RewriteParser(RewriteSystem rs, RewriteLexer lex)
		{
			_sw = new Stopwatch();
			_rs = rs;
			_lex = lex;
			_mptidsymInfix = new Dictionary<TokKind, Symbol>();
			_mptidsymPrefix = new Dictionary<TokKind, Symbol>();
			_scopeTop = _rs.Scope;
			foreach (Symbol item in _scopeTop)
			{
				if (!item.ParseInfo.HasInfixForm || !IsAllowedOperator(item))
				{
					continue;
				}
				TokKind tokKind = _lex.TidFromStr(item.ParseInfo.OperatorText);
				if (tokKind != null)
				{
					if (item.ParseInfo.IsUnaryPrefix)
					{
						_mptidsymPrefix.Add(tokKind, item);
					}
					else
					{
						_mptidsymInfix.Add(tokKind, item);
					}
				}
			}
			_rgsymLocals = new List<Symbol>();
		}

		protected RewriteParser(RewriteSystem rs)
			: this(rs, new RewriteLexer(new NormStr.Pool()))
		{
		}

		internal virtual bool IsAllowedOperator(Symbol sym)
		{
			return true;
		}

		/// <summary> output an error
		/// </summary>
		/// <param name="tok">Token that produced the error</param>
		/// <param name="errorMessage">Error text.</param>
		/// <param name="errorData">Error data (optional).</param>
		protected abstract void ErrorOutput(Token tok, string errorMessage, object errorData);

		/// <summary> Process an expression
		/// </summary>
		/// <param name="expr"></param>
		protected abstract void ProcessExpr(Expression expr);

		/// <summary> record an error
		/// </summary>
		/// <param name="errorMessage"></param>
		/// <param name="errorData">Error data (optional).</param>
		protected void Error(string errorMessage, object errorData = null)
		{
			ErrorCore(_curs.TokCur, errorMessage, errorData);
		}

		/// <summary> record an error with no data and with one or more args.
		/// </summary>
		/// <param name="errorMessage"></param>
		/// <param name="args"></param>
		protected void ErrorNoData(string errorMessage, params object[] args)
		{
			ErrorCore(_curs.TokCur, string.Format(CultureInfo.InvariantCulture, errorMessage, args), null);
		}

		/// <summary> record an error
		/// </summary>
		/// <param name="tok"></param>
		/// <param name="errorMessage"></param>
		/// <param name="args"></param>
		protected void Error(Token tok, string errorMessage, params object[] args)
		{
			ErrorCore(tok, string.Format(CultureInfo.InvariantCulture, errorMessage, args), null);
		}

		/// <summary> check if the error should be ignored as duplicate
		/// </summary>
		/// <returns></returns>
		protected bool IgnoreError()
		{
			if (_itokError == _curs.ItokCur)
			{
				return true;
			}
			_itokError = _curs.ItokCur;
			return false;
		}

		/// <summary> record an error
		/// </summary>
		/// <param name="tok"></param>
		/// <param name="errorMessage"></param>
		/// <param name="errorData">Error data (optional).</param>
		protected virtual void ErrorCore(Token tok, string errorMessage, object errorData)
		{
			if (!IgnoreError())
			{
				ErrorOutput(tok, errorMessage, errorData);
			}
		}

		/// <summary>
		/// Eats a literal token of the given kind. If the next token is not the right kind, reports an error.
		/// </summary>
		/// <param name="tid"></param>
		internal void EatToken(TokKind tid)
		{
			if (_curs.TidCur != tid)
			{
				if (tid == TokKind.SquareClose || tid == RewriteTokKind.SquareColonClose || tid == TokKind.CurlClose)
				{
					ErrorNoData(Resources.UnexpectedNeedToBe012, _curs.TokCur, tid, TokKind.Comma);
				}
				else if (tid == TokKind.Semi)
				{
					ErrorNoData(Resources.UnexpectedNeedToBe01orEndOfFile, _curs.TokCur, tid);
				}
				else
				{
					ErrorNoData(Resources.UnExpectedNeedToBe01, _curs.TokCur, tid);
				}
			}
			else
			{
				_curs.TidNext();
			}
		}

		public void ProcessFile(string context, string source, bool fStrict)
		{
			ProcessFile(new StaticStringText(context, source), fStrict);
		}

		public void ProcessFile(string path, bool fStrict)
		{
			ProcessFile(new StaticText(path), fStrict);
		}

		public void ProcessFile(string context, TextReader reader, bool fStrict)
		{
			ProcessFile(new FileText(context, reader), fStrict);
		}

		public virtual void ProcessFile(IText text, bool fStrict)
		{
			BeginProcessing(text, fStrict);
			while (_curs.TidCur != TokKind.Eof)
			{
				Expression expr = ParseExpression();
				if (_rgsymLocals.Count > 0)
				{
					MakeAndPushScope(expr, 0);
				}
				ProcessExpr(expr);
				_scopeTop = _rs.Scope;
				if (_curs.TidCur != TokKind.Eof)
				{
					EatToken(TokKind.Semi);
				}
			}
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		internal void BeginProcessing(IText text, bool fStrict)
		{
			_sw.Stop();
			_sw.Reset();
			_fStrict = fStrict;
			_map = new LineMapper(text.Version);
			IEnumerable<Token> rgtok = _lex.LexSource(text);
			rgtok = TokenFilter.Filter(rgtok, delegate(ref Token tok)
			{
				if (tok.Kind == TokKind.NewLine)
				{
					_map.AddNewLine(tok.As<NewLineToken>());
					return false;
				}
				return tok.Kind != TokKind.Comment;
			});
			_curs = new TokenCursor(new BufList<Token>(rgtok), 0);
			_scopeTop = _rs.Scope;
		}

		internal virtual int MarkScope()
		{
			return _rgsymLocals.Count;
		}

		internal virtual SymbolScope MakeAndPushScope(Expression expr, int mark)
		{
			if (_rgsymLocals.Count <= mark && !PatternVarVisitor.HasPatternVars(expr))
			{
				return null;
			}
			int isymLim = _rgsymLocals.Count;
			expr = MapPatternVarVisitor.VisitPatternVars(expr, delegate(Symbol sym)
			{
				if (sym.Scope != null)
				{
					return sym;
				}
				int num = isymLim;
				while (--num >= mark)
				{
					if (sym == _rgsymLocals[num])
					{
						return sym;
					}
				}
				int num2 = _rgsymLocals.Count;
				while (--num2 >= isymLim)
				{
					if (sym.Name == _rgsymLocals[num2].Name)
					{
						return _rgsymLocals[num2];
					}
				}
				_rgsymLocals.Add(sym = new Symbol(sym.Rewrite, null, sym.Name, sym.ParseInfo));
				return sym;
			});
			if (_rgsymLocals.Count <= mark)
			{
				return null;
			}
			SymbolScope symbolScope = new SymbolScope(_scopeTop);
			symbolScope.ParentExpression = expr;
			for (int i = mark; i < _rgsymLocals.Count; i++)
			{
				symbolScope.AddSymbol(_rgsymLocals[i]);
			}
			Statics.TrimList(_rgsymLocals, mark);
			_scopeTop = symbolScope;
			return symbolScope;
		}

		internal virtual SymbolScope MakeAndPushTempScope(Expression expr)
		{
			if (!PatternVarVisitor.HasPatternVars(expr))
			{
				return null;
			}
			SymbolScope scope = new SymbolScope(_scopeTop);
			scope.ParentExpression = expr;
			PatternVarVisitor.VisitPatternVars(expr, delegate(Symbol sym)
			{
				if (sym.Scope == null)
				{
					scope.AddSymbol(sym);
				}
				return true;
			});
			_scopeTop = scope;
			return scope;
		}

		internal static void RevertTempScope(SymbolScope scope)
		{
			scope?.RemoveAll();
		}

		internal virtual TokKind TidPeek(int ditok)
		{
			return _curs.TidPeek(ditok);
		}

		internal virtual Token TokPeek(int ditok)
		{
			return _curs.TokPeek(ditok);
		}

		internal virtual TokKind TidNext()
		{
			return _curs.TidNext();
		}

		internal virtual Token TokMove()
		{
			return _curs.TokMove();
		}

		internal virtual Expression ParseExpression()
		{
			return ParseExpression(Precedence.Lim);
		}

		internal virtual Expression ParseExpression(Precedence precMax)
		{
			int mark = MarkScope();
			List<Expression> rgexpr = null;
			TextSpan span = _curs.TokCur.Span;
			Expression expression;
			if (_mptidsymPrefix.TryGetValue(_curs.TidCur, out var value))
			{
				_curs.TidNext();
				expression = value.Invoke(ParseExpression(Precedence.Unary));
			}
			else if (_curs.TidCur.Tke == (TokKindEnum)20)
			{
				_curs.TidNext();
				expression = ParseExpression(Precedence.Unary);
			}
			else
			{
				expression = ParseAtom();
			}
			EndExpressionParsing(expression, span + _curs.TokPeek(-1).Span);
			while (true)
			{
				switch (_curs.TidCur.Tke)
				{
				case (TokKindEnum)74:
					if (2 > (int)precMax)
					{
						return expression;
					}
					_curs.TidNext();
					expression = expression.Invoke(ParseArgListOpt(expression.ParseInfo, TokKind.SquareClose));
					EndExpressionParsing(expression, span + _curs.TokPeek(-1).Span);
					continue;
				case (TokKindEnum)2020:
					if (2 > (int)precMax)
					{
						return expression;
					}
					_curs.TidNext();
					if (rgexpr == null)
					{
						rgexpr = new List<Expression>();
					}
					else
					{
						rgexpr.Clear();
					}
					rgexpr.Add(expression);
					ParseArgListOpt(_rs.Builtin.Part.ParseInfo, RewriteTokKind.SquareColonClose, ref rgexpr);
					expression = _rs.Builtin.Part.Invoke(rgexpr.ToArray());
					EndExpressionParsing(expression, span + _curs.TokPeek(-1).Span);
					continue;
				case (TokKindEnum)70:
					ErrorNoData(Resources.UnexpectedNeedToBe012, _curs.TokCur, TokKind.SquareOpen, TokKind.Comma);
					break;
				case (TokKindEnum)43:
				{
					if (5 > (int)precMax)
					{
						return expression;
					}
					TextSpan span2 = _curs.TokCur.Span;
					_curs.TidNext();
					Expression expression2 = ParseExpression(Precedence.Power);
					double val3;
					if (expression2.GetValue(out Rational val))
					{
						if (expression.GetValue(out Rational val2))
						{
							expression = RationalConstant.Create(_rs, val2 / val);
							EndExpressionParsing(expression, span + _curs.TokPeek(-1).Span);
							continue;
						}
						expression2 = RationalConstant.Create(_rs, val.Invert());
					}
					else if (expression2.GetValue(out val3))
					{
						if (expression.GetValue(out double val4))
						{
							expression = new FloatConstant(_rs, val4 / val3);
							EndExpressionParsing(expression, span + _curs.TokPeek(-1).Span);
							continue;
						}
						expression2 = new FloatConstant(_rs, 1.0 / val3);
					}
					else
					{
						expression2 = _rs.Builtin.Power.Invoke(expression2, _rs.Builtin.Integer.MinusOne);
					}
					EndExpressionParsing(expression2, span2 + _curs.TokPeek(-1).Span);
					expression = _rs.Builtin.Times.Invoke(expression, expression2);
					EndExpressionParsing(expression, span + _curs.TokPeek(-1).Span);
					continue;
				}
				case (TokKindEnum)23:
					value = _rs.Builtin.Plus;
					if ((int)value.ParseInfo.LeftPrecedence > (int)precMax)
					{
						return expression;
					}
					expression = ParseInfixOperator(mark, expression, value, TokKind.Add);
					EndExpressionParsing(expression, span + _curs.TokPeek(-1).Span);
					continue;
				}
				if (!_mptidsymInfix.TryGetValue(_curs.TidCur, out value) || (int)value.ParseInfo.LeftPrecedence > (int)precMax)
				{
					break;
				}
				expression = ((!value.ParseInfo.Comparison) ? ParseInfixOperator(mark, expression, value, _curs.TidCur) : ParseComparisonOperator(expression, value));
				EndExpressionParsing(expression, span + _curs.TokPeek(-1).Span);
			}
			return expression;
		}

		/// <summary>
		/// set the span info
		/// </summary>
		/// <param name="expr"></param>
		/// <param name="exprSpan"></param>
		/// <returns></returns>
		protected void EndExpressionParsing(Expression expr, TextSpan exprSpan)
		{
			if (expr.PlacementInformation == null)
			{
				PlacementInfo placementInformation = new PlacementInfo(_map, exprSpan);
				expr.PlacementInformation = placementInformation;
			}
		}

		internal virtual Expression ParseInfixOperator(int mark, Expression exprLeft, Symbol sym, TokKind tidOrig)
		{
			TokKind tidCur = _curs.TidCur;
			_curs.TidNext();
			SymbolScope scopeTop = _scopeTop;
			SymbolScope scope = null;
			ParseInfo parseInfo = sym.ParseInfo;
			if (parseInfo.CreateScope)
			{
				MakeAndPushScope(exprLeft, mark);
			}
			else if (parseInfo.BorrowScope)
			{
				scope = MakeAndPushTempScope(exprLeft);
			}
			Expression result;
			if (parseInfo.IsUnaryPostfix)
			{
				result = sym.Invoke(exprLeft);
			}
			else if (!parseInfo.VaryadicInfix || parseInfo.LeftPrecedence != parseInfo.RightPrecedence + 1)
			{
				result = sym.Invoke(exprLeft, ParseExpression(parseInfo.RightPrecedence));
			}
			else
			{
				Expression expression = ParseExpression(parseInfo.RightPrecedence);
				if (tidCur.Tke == (TokKindEnum)23)
				{
					expression = _rs.Builtin.Minus.Invoke(expression);
				}
				List<Expression> list = new List<Expression>();
				list.Add(exprLeft);
				list.Add(expression);
				while (_curs.TidCur == tidOrig || (tidOrig.Tke == (TokKindEnum)20 && _curs.TidCur.Tke == (TokKindEnum)23))
				{
					tidCur = _curs.TidCur;
					_curs.TidNext();
					expression = ParseExpression(parseInfo.RightPrecedence);
					if (tidCur.Tke == (TokKindEnum)23)
					{
						expression = _rs.Builtin.Minus.Invoke(expression);
					}
					list.Add(expression);
				}
				result = sym.Invoke(list.ToArray());
			}
			RevertTempScope(scope);
			_scopeTop = scopeTop;
			return result;
		}

		internal virtual Expression ParseComparisonOperator(Expression exprLeft, Symbol sym)
		{
			_curs.TidNext();
			Expression expression = ParseExpression(sym.ParseInfo.RightPrecedence);
			if (!_mptidsymInfix.TryGetValue(_curs.TidCur, out var value) || !value.ParseInfo.Comparison)
			{
				return sym.Invoke(exprLeft, expression);
			}
			bool flag = value == sym;
			List<Expression> list = new List<Expression>();
			list.Add(exprLeft);
			list.Add(sym);
			list.Add(expression);
			list.Add(value);
			while (true)
			{
				_curs.TidNext();
				list.Add(ParseExpression(sym.ParseInfo.RightPrecedence));
				if (!_mptidsymInfix.TryGetValue(_curs.TidCur, out value) || !value.ParseInfo.Comparison)
				{
					break;
				}
				list.Add(value);
				flag = flag && sym == value;
			}
			if (flag)
			{
				for (int num = list.Count - 2; num > 0; num -= 2)
				{
					list.RemoveAt(num);
				}
				return sym.Invoke(list.ToArray());
			}
			return _rs.Builtin.Inequality.Invoke(list.ToArray());
		}

		internal virtual Expression ParseAtom()
		{
			switch (_curs.TidCur.Tke)
			{
			default:
			{
				Token token = _curs.TokPeek(-1);
				string text = ((token == _curs.TokCur) ? string.Format(CultureInfo.InvariantCulture, Resources.UnexpectedTerm0, new object[1] { _curs.TidCur }) : string.Format(CultureInfo.InvariantCulture, Resources.UnexpectedTermAfterTerm01, new object[2] { _curs.TidCur, token.Kind }));
				Error(text, RewriteParserErrorReason.UnexpectedTerm);
				_curs.TidNext();
				return _rs.Fail(text);
			}
			case (TokKindEnum)72:
			{
				_curs.TidNext();
				Expression result = ParseExpression();
				EatToken(TokKind.ParenClose);
				return result;
			}
			case (TokKindEnum)70:
				_curs.TidNext();
				return _rs.Builtin.List.Invoke(ParseArgListOpt(_rs.Builtin.List.ParseInfo, TokKind.CurlClose));
			case (TokKindEnum)32:
				return ParseParamList();
			case (TokKindEnum)3:
				return new IntegerConstant(_rs, _curs.TokMove().As<IntLitToken>().Val);
			case (TokKindEnum)5:
			{
				DecimalLitToken decimalLitToken = _curs.TokMove().As<DecimalLitToken>();
				Rational rat;
				if (decimalLitToken.FormatChar == 'f')
				{
					if (decimalLitToken.GetDouble(out var dbl))
					{
						return new FloatConstant(_rs, dbl);
					}
				}
				else if (decimalLitToken.GetRational(out rat))
				{
					return RationalConstant.Create(_rs, rat);
				}
				return _rs.Fail(Resources.Overflow, decimalLitToken);
			}
			case (TokKindEnum)2003:
				_curs.TidNext();
				return _rs.Builtin.Boolean.True;
			case (TokKindEnum)2004:
				_curs.TidNext();
				return _rs.Builtin.Boolean.False;
			case (TokKindEnum)2005:
				_curs.TidNext();
				return _rs.Builtin.Rational.Infinity;
			case (TokKindEnum)2006:
				_curs.TidNext();
				return _rs.Builtin.Rational.UnsignedInfinity;
			case (TokKindEnum)2007:
				_curs.TidNext();
				return _rs.Builtin.Rational.Indeterminate;
			case (TokKindEnum)8:
				return new StringConstant(_rs, _curs.TokMove().As<StrLitToken>().Val);
			case (TokKindEnum)1:
				return ParseIdent();
			case (TokKindEnum)2010:
				_curs.TidNext();
				return _rs.Builtin.Hole.Invoke();
			case (TokKindEnum)2011:
				_curs.TidNext();
				return _rs.Builtin.HoleSplice.Invoke();
			case (TokKindEnum)2012:
				return _rs.Builtin.Slot.Invoke(new IntegerConstant(_rs, _curs.TokMove().As<SlotToken>().Index));
			case (TokKindEnum)2013:
				return _rs.Builtin.SlotSplice.Invoke(new IntegerConstant(_rs, _curs.TokMove().As<SlotSpliceToken>().Index));
			}
		}

		internal virtual Expression[] ParseArgListOpt(ParseInfo pi, TokKind tidClose)
		{
			List<Expression> rgexpr = null;
			ParseArgListOpt(pi, tidClose, ref rgexpr);
			if (rgexpr == null)
			{
				return new Expression[0];
			}
			return rgexpr.ToArray();
		}

		internal virtual void ParseArgListOpt(ParseInfo pi, TokKind tidClose, ref List<Expression> rgexpr)
		{
			if (_curs.TidCur == tidClose)
			{
				_curs.TidNext();
				return;
			}
			if (rgexpr == null)
			{
				rgexpr = new List<Expression>();
			}
			SymbolScope scopeTop = _scopeTop;
			if (pi.IteratorScope)
			{
				ParseIterList(rgexpr);
			}
			else
			{
				SymbolScope scope = null;
				int mark = MarkScope();
				Expression expression;
				if (pi.CreateVariable && _curs.TidCur == TokKind.Ident && _curs.TidPeek(1) == TokKind.Comma)
				{
					expression = EnsureVariable(_curs.TokCur.As<IdentToken>());
					_curs.TidNext();
				}
				else
				{
					expression = ParseExpression();
				}
				rgexpr.Add(expression);
				if (pi.CreateScope)
				{
					MakeAndPushScope(expression, mark);
				}
				else if (pi.BorrowScope)
				{
					scope = MakeAndPushTempScope(expression);
				}
				while (_curs.TidCur == TokKind.Comma)
				{
					_curs.TidNext();
					rgexpr.Add(ParseExpression());
				}
				RevertTempScope(scope);
			}
			EatToken(tidClose);
			_scopeTop = scopeTop;
		}

		internal virtual void ParseIterList(List<Expression> rgexpr)
		{
			List<Expression> list = null;
			SymbolScope symbolScope = (_scopeTop = new SymbolScope(_scopeTop));
			while (true)
			{
				IdentToken identToken;
				Expression expression2;
				if (_curs.TidCur == TokKind.CurlOpen && _curs.TidPeek(1) == TokKind.Ident && _curs.TidPeek(2) == TokKind.Comma && !symbolScope.GetSymbolThis((identToken = _curs.TokPeek(1).As<IdentToken>()).Val.ToString(), out var sym))
				{
					TextSpan span = _curs.TokCur.Span;
					_curs.TidNext();
					_curs.TidNext();
					if (list == null)
					{
						list = new List<Expression>();
					}
					else
					{
						list.Clear();
					}
					sym = new Symbol(_rs, null, identToken.Val.ToString());
					list.Add(sym);
					while (_curs.TidCur == TokKind.Comma)
					{
						_curs.TidNext();
						list.Add(ParseExpression());
					}
					TextSpan span2 = _curs.TokCur.Span;
					EatToken(TokKind.CurlClose);
					if (_curs.TidCur != TokKind.Comma)
					{
						sym = (Symbol)(list[0] = EnsureSymbol(identToken, !_fStrict));
					}
					else
					{
						symbolScope.AddSymbol(sym);
					}
					expression2 = _rs.Builtin.List.Invoke(list.ToArray());
					span += span2;
					EndExpressionParsing(expression2, span);
				}
				else
				{
					expression2 = ParseExpression();
				}
				rgexpr.Add(expression2);
				if (_curs.TidCur != TokKind.Comma)
				{
					break;
				}
				_curs.TidNext();
			}
		}

		internal virtual Expression ParseParamList()
		{
			if (_curs.TidNext() == TokKind.GrtGrt)
			{
				_curs.TidNext();
				return _rs.Builtin.List.Invoke();
			}
			SymbolScope symbolScope = null;
			List<Expression> list = new List<Expression>();
			while (true)
			{
				if (_curs.TidCur != TokKind.Ident)
				{
					Error(Resources.IdentifierExpected, RewriteParserErrorReason.IdentifierExpected);
					ParseExpression();
				}
				else
				{
					if (symbolScope == null)
					{
						symbolScope = (_scopeTop = new SymbolScope(_scopeTop));
					}
					list.Add(new Symbol(_rs, symbolScope, _curs.TokMove().As<IdentToken>().Val.ToString()));
				}
				if (_curs.TidCur != TokKind.Comma)
				{
					break;
				}
				_curs.TidNext();
			}
			EatToken(TokKind.GrtGrt);
			Expression expression = _rs.Builtin.List.Invoke(list.ToArray());
			if (symbolScope != null)
			{
				symbolScope.ParentExpression = expression;
			}
			return expression;
		}

		internal virtual Symbol EnsureVariable(IdentToken id)
		{
			string text = id.Val.ToString();
			int num = _rgsymLocals.Count;
			Symbol symbol;
			while (--num >= 0)
			{
				symbol = _rgsymLocals[num];
				if (symbol.Name == text)
				{
					return symbol;
				}
			}
			symbol = new Symbol(_rs, null, text);
			_rgsymLocals.Add(symbol);
			return symbol;
		}

		internal virtual Symbol EnsureSymbol(IdentToken id, bool fCanCreate)
		{
			if (_scopeTop.GetSymbolAll(id.Val.ToString(), out var sym))
			{
				return sym;
			}
			if (!fCanCreate)
			{
				Error(id, "Symbol '{0}' does not exist. Append the ` character to create a new symbol.", id.Val);
			}
			return new Symbol(_rs, id.Val.ToString());
		}

		protected Expression ParseIdent()
		{
			IdentToken id = _curs.TokCur.As<IdentToken>();
			_curs.TidNext();
			switch (_curs.TidCur.Tke)
			{
			case (TokKindEnum)2010:
				_curs.TidNext();
				return _rs.Builtin.Pattern.Invoke(EnsureVariable(id), _rs.Builtin.Hole.Invoke());
			case (TokKindEnum)2011:
				_curs.TidNext();
				return _rs.Builtin.Pattern.Invoke(EnsureVariable(id), _rs.Builtin.HoleSplice.Invoke());
			case (TokKindEnum)55:
				_curs.TidNext();
				return _rs.Builtin.Pattern.Invoke(EnsureVariable(id), ParseExpression());
			case (TokKindEnum)64:
				_curs.TidNext();
				return EnsureSymbol(id, fCanCreate: true);
			default:
				return EnsureSymbol(id, !_fStrict);
			}
		}
	}
}
