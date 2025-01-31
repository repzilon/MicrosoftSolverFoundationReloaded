using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class OmlParser : RewriteParser
	{
		private readonly SolverContext _context;

		private readonly OmlLexer _omlLex;

		internal Dictionary<Expression, string> _expressionStrings = new Dictionary<Expression, string>();

		protected bool _fVerbose;

		public OmlParser(RewriteSystem rs, SolverContext context, OmlLexer lexer)
			: base(rs, lexer)
		{
			_context = context;
			_omlLex = lexer;
		}

		protected override void ErrorOutput(Token tok, string errorMessage, object errorData)
		{
			_map.MapSpanToPos(tok.Span, out var spos);
			errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.Error012345, spos.pathMin, spos.lineMin, spos.colMin, spos.lineLim, spos.colLim, errorMessage);
			OmlParseExceptionReason reason = GetReason(errorData);
			throw new OmlParseException(errorMessage, null, spos, reason);
		}

		private static OmlParseExceptionReason GetReason(object errorData)
		{
			if (errorData == null)
			{
				return OmlParseExceptionReason.NotSpecified;
			}
			if (errorData is OmlParseExceptionReason)
			{
				return (OmlParseExceptionReason)errorData;
			}
			if (errorData is RewriteParserErrorReason)
			{
				switch ((RewriteParserErrorReason)errorData)
				{
				case RewriteParserErrorReason.IdentifierExpected:
					return OmlParseExceptionReason.InvalidName;
				case RewriteParserErrorReason.UnexpectedTerm:
					return OmlParseExceptionReason.UnexpectedTerm;
				default:
					return OmlParseExceptionReason.NotSpecified;
				}
			}
			return OmlParseExceptionReason.NotSpecified;
		}

		public Term ProcessExpression(Model sfsModel, string context, string source)
		{
			return ProcessExpression(sfsModel, new StaticStringText(context, source));
		}

		public Term ProcessExpression(Model sfsModel, IText text)
		{
			BeginProcessing(text, fStrict: false);
			Expression expression = ParseExpression();
			if (TidCur != TokKind.Eof)
			{
				ErrorNoData(Resources.UnExpectedNeedToBe01, TokCur, TokKind.Eof);
			}
			try
			{
				ConcreteModel mod = new ConcreteModel(null, base.Rewrite as SolveRewriteSystem, null);
				ConcreteModel.SfsModelExtractor sfsModelExtractor = new ConcreteModel.SfsModelExtractor(mod, _context);
				return sfsModelExtractor.TranslateExpression(sfsModel, expression);
			}
			catch (ModelClauseException mce)
			{
				throw OmlParseException.Create(mce);
			}
		}

		protected override void ProcessExpr(Expression expr)
		{
			if (expr.Arity == 0 || !(expr.Head is ModelSymbol))
			{
				throw new MsfException(Resources.ModelShouldStartWithModelAndEndWithSymbol);
			}
			base.Stopwatch.Start();
			Expression expression = expr.Evaluate();
			base.Stopwatch.Stop();
			if (!base.Rewrite.IsNull(expression))
			{
				Model sfsModel = _context.CreateModel();
				try
				{
					ConcreteModel.ModelParser modelParser = new ConcreteModel.ModelParser();
					ConcreteModel mod = modelParser.ParseModel(expression as Invocation);
					ConcreteModel.SfsModelExtractor sfsModelExtractor = new ConcreteModel.SfsModelExtractor(mod, _context);
					sfsModelExtractor.TryGetSfsModel(modelParser, sfsModel, _expressionStrings, out var _);
				}
				catch (ModelClauseException mce)
				{
					throw OmlParseException.Create(mce);
				}
			}
		}

		public override void ProcessFile(IText text, bool fStrict)
		{
			BeginProcessing(text, fStrict);
			Expression expr = ParseModel();
			ProcessExpr(expr);
			if (TidCur != TokKind.Eof)
			{
				ErrorNoData(Resources.UnExpectedNeedToBe01, TokCur, TokKind.Eof);
			}
		}

		internal override Expression ParseExpression(Precedence precMax)
		{
			if (TidCur == TokKind.Ident)
			{
				IdentToken identToken = TokCur.As<IdentToken>();
				if (identToken.Val.ToString() == "Model")
				{
					return ParseModel();
				}
			}
			return base.ParseExpression(precMax);
		}

		private void BeginSavingText()
		{
			_omlLex.BeginSavingText();
		}

		private void EndSavingText()
		{
			_omlLex.EndSavingText();
		}

		private string TextfromSpan(TextSpan span)
		{
			string savedText = _omlLex.SavedText;
			int exprBufferStart = _omlLex._exprBufferStart;
			if (span.Min < exprBufferStart)
			{
				return null;
			}
			if (span.Lim - exprBufferStart >= savedText.Length)
			{
				return null;
			}
			return savedText.Substring(span.Min - exprBufferStart, span.Lim - span.Min);
		}

		private Expression ParseModel()
		{
			TextSpan span = TokCur.Span;
			if (TidCur == TokKind.Ident)
			{
				string text = TokCur.As<IdentToken>().Val.ToString();
				if (text != "Model")
				{
					Error("Model expected");
					return base.Rewrite.Fail("Model expected");
				}
			}
			EatToken(TokKind.Ident);
			EatToken(TokKind.SquareOpen);
			Expression[] arguments = ParseModelSectionList();
			EatToken(TokKind.SquareClose);
			Expression model = base.Rewrite.Builtin.Model;
			return BuildInvocation(span, model, arguments);
		}

		private Expression BuildInvocation(TextSpan exprSpan, Expression head, params Expression[] arguments)
		{
			Expression expression = head.Invoke(arguments);
			EndExpressionParsing(expression, exprSpan + TokPeek(-1).Span);
			return expression;
		}

		private Expression BuildInvocation(TextSpan exprSpan, Expression head, IEnumerable<Expression> arguments)
		{
			return BuildInvocation(exprSpan, head, arguments.ToArray());
		}

		private Expression[] ParseModelSymbolList()
		{
			return ParseCommaSeparatedList(ParseModelSymbol);
		}

		private Expression ParseModelSymbol()
		{
			if (TidCur != TokKind.Ident)
			{
				Error("Identifier expected", OmlParseExceptionReason.InvalidName);
				return base.Rewrite.Fail("Identifier expected");
			}
			Expression expression = ParseIdent();
			if (!(expression is Symbol) || expression.HasAttribute(base.Rewrite.Attributes.ValuesLocked))
			{
				Error("Identifier expected but keyword found instead", OmlParseExceptionReason.InvalidName);
			}
			return expression;
		}

		private Expression[] ParseModelSectionList()
		{
			return ParseCommaSeparatedList(ParseModelSection);
		}

		private Expression[] ParseCommaSeparatedList(Func<Expression> parseFunc)
		{
			List<Expression> list = new List<Expression>();
			list.Add(parseFunc());
			while (TidCur == TokKind.Comma)
			{
				EatToken(TokKind.Comma);
				list.Add(parseFunc());
			}
			return list.ToArray();
		}

		private Expression ParseModelSection()
		{
			if (TidCur == TokKind.Ident)
			{
				switch (TokCur.As<IdentToken>().Val.ToString())
				{
				case "Decisions":
					return ParseModelDecisionsSection();
				case "Parameters":
					return ParseModelParametersSection();
				case "Constraints":
					return ParseModelConstraintsSection();
				case "Goals":
					return ParseModelGoalsSection();
				case "Domains":
					return ParseModelDomainsSection();
				}
			}
			if (TidPeek(1) == TokKind.SubGrt)
			{
				return ParseModelSubmodelSection();
			}
			Error("Expected section name");
			return base.Rewrite.Fail("Expected section name");
		}

		private Expression ParseModelSubmodelSection()
		{
			TextSpan span = TokCur.Span;
			Expression expression = ParseModelSymbol();
			EatToken(TokKind.SubGrt);
			Expression expression2 = ParseModel();
			return BuildInvocation(span, base.Rewrite.Builtin.Rule, expression, expression2);
		}

		private Expression ParseModelDomainsSection()
		{
			TextSpan span = TokCur.Span;
			EatToken(TokKind.Ident);
			EatToken(TokKind.SquareOpen);
			Expression expression = ParseModelDomain();
			EatToken(TokKind.Comma);
			Expression[] second = ParseModelSymbolList();
			EatToken(TokKind.SquareClose);
			return BuildInvocation(span, base.Rewrite.Builtin.Domains, new Expression[1] { expression }.Concat(second));
		}

		private Expression ParseModelDecisionsSection()
		{
			TextSpan span = TokCur.Span;
			EatToken(TokKind.Ident);
			EatToken(TokKind.SquareOpen);
			Expression expression = ParseModelDomain();
			EatToken(TokKind.Comma);
			Expression[] second = ParseModelDecisionList();
			EatToken(TokKind.SquareClose);
			return BuildInvocation(span, base.Rewrite.Builtin.Decisions, new Expression[1] { expression }.Concat(second));
		}

		private Expression[] ParseModelDecisionList()
		{
			return ParseCommaSeparatedList(ParseModelDecision);
		}

		private Expression ParseModelDecision()
		{
			TextSpan span = TokCur.Span;
			if (TidCur == TokKind.Ident)
			{
				switch (TokCur.As<IdentToken>().Val.ToString())
				{
				case "Foreach":
				{
					EatToken(TokKind.Ident);
					EatToken(TokKind.SquareOpen);
					Expression[] first = ParseModelIterators();
					Expression expression2 = ParseModelDecision();
					EatToken(TokKind.SquareClose);
					return BuildInvocation(span, base.Rewrite.Builtin.Foreach, first.Concat(new Expression[1] { expression2 }));
				}
				case "Recourse":
				{
					EatToken(TokKind.Ident);
					EatToken(TokKind.SquareOpen);
					Expression expression2 = ParseModelDecision();
					EatToken(TokKind.SquareClose);
					return BuildInvocation(span, base.Rewrite.Builtin.Recourse, expression2);
				}
				case "Annotation":
					return ParseModelAnnotation(span, ParseModelDecision);
				default:
				{
					Expression expression = ParseModelSymbol();
					if (TidCur == TokKind.SquareOpen)
					{
						EatToken(TokKind.SquareOpen);
						if (TidCur == TokKind.SquareClose)
						{
							EatToken(TokKind.SquareClose);
							return BuildInvocation(span, expression);
						}
						Expression[] arguments = ParseModelIndexList();
						EatToken(TokKind.SquareClose);
						return BuildInvocation(span, expression, arguments);
					}
					return expression;
				}
				}
			}
			Error("Expected identifier or Foreach");
			return base.Rewrite.Fail("Expected identifier or Foreach");
		}

		private Expression ParseModelAnnotation(TextSpan exprSpan, Func<Expression> parseInner)
		{
			EatToken(TokKind.Ident);
			EatToken(TokKind.SquareOpen);
			Expression expression = parseInner();
			EatToken(TokKind.Comma);
			Expression expression2 = base.ParseAtom();
			EatToken(TokKind.Comma);
			Expression expression3 = base.ParseAtom();
			EatToken(TokKind.SquareClose);
			return BuildInvocation(exprSpan, base.Rewrite.Builtin.Annotation, expression, expression2, expression3);
		}

		private Expression ParseModelParametersSection()
		{
			TextSpan span = TokCur.Span;
			EatToken(TokKind.Ident);
			EatToken(TokKind.SquareOpen);
			if (TidCur == TokKind.SquareClose)
			{
				EatToken(TokKind.SquareClose);
				return BuildInvocation(span, base.Rewrite.Builtin.Parameters);
			}
			if (TidCur == TokKind.Ident)
			{
				Expression expression;
				switch (TokCur.As<IdentToken>().Val.ToString())
				{
				case "Sets":
					expression = ParseModelSetDomain();
					break;
				case "UniformDistribution":
				case "NormalDistribution":
				case "DiscreteUniformDistribution":
				case "GeometricDistribution":
				case "ExponentialDistribution":
				case "BinomialDistribution":
				case "LogNormalDistribution":
					expression = ParseModelDistributionDomain();
					break;
				default:
					expression = ParseModelDomain();
					break;
				}
				Expression[] second;
				if (TidCur == TokKind.Comma)
				{
					EatToken(TokKind.Comma);
					second = ParseModelParameterList();
				}
				else
				{
					second = new Expression[0];
				}
				EatToken(TokKind.SquareClose);
				return BuildInvocation(span, base.Rewrite.Builtin.Parameters, new Expression[1] { expression }.Concat(second));
			}
			Error("Expected domain", OmlParseExceptionReason.InvalidDomain);
			return base.Rewrite.Fail("Expected domain");
		}

		private Expression[] ParseModelParameterList()
		{
			return ParseCommaSeparatedList(ParseModelParameter);
		}

		private Expression ParseModelParameter()
		{
			TextSpan span = TokCur.Span;
			if (TidCur == TokKind.Ident)
			{
				string text = TokCur.As<IdentToken>().Val.ToString();
				if (text == "Foreach")
				{
					EatToken(TokKind.Ident);
					EatToken(TokKind.SquareOpen);
					Expression[] first = ParseModelIterators();
					Expression expression = ParseModelParameter();
					EatToken(TokKind.SquareClose);
					return BuildInvocation(span, base.Rewrite.Builtin.Foreach, first.Concat(new Expression[1] { expression }));
				}
				if (text == "Annotation")
				{
					return ParseModelAnnotation(span, ParseModelParameter);
				}
				Expression expression2 = ParseModelSymbol();
				Expression result = expression2;
				if (TidCur == TokKind.SquareOpen)
				{
					EatToken(TokKind.SquareOpen);
					Expression[] arguments = ((TidCur == TokKind.SquareClose) ? new Expression[0] : ParseModelIndexList());
					EatToken(TokKind.SquareClose);
					result = BuildInvocation(span, expression2, arguments);
				}
				if (TidCur == TokKind.Equ)
				{
					result = ParseParameterAssignment(span, result);
				}
				return result;
			}
			Error("Expected identifier or Foreach");
			return base.Rewrite.Fail("Expected identifier or Foreach");
		}

		private Expression ParseParameterAssignment(TextSpan exprSpan, Expression result)
		{
			EatToken(TokKind.Equ);
			Expression expression;
			if (TidCur != TokKind.CurlOpen)
			{
				expression = ParseModelExpression();
			}
			else
			{
				TextSpan span = TokCur.Span;
				EatToken(TokKind.CurlOpen);
				Expression[] array = null;
				array = ((TidCur == TokKind.CurlOpen) ? ParseModelNestedExprList() : ParseModelExpressionList());
				EatToken(TokKind.CurlClose);
				expression = BuildInvocation(span, base.Rewrite.Builtin.List, array);
			}
			result = BuildInvocation(exprSpan, base.Rewrite.Builtin.Set, result, expression);
			return result;
		}

		private Expression[] ParseModelNestedExprList()
		{
			return ParseCommaSeparatedList(ParseModelNestedExprListItem);
		}

		private Expression ParseModelNestedExprListItem()
		{
			TextSpan span = TokCur.Span;
			EatToken(TokKind.CurlOpen);
			Expression[] arguments = ParseModelExpressionList();
			EatToken(TokKind.CurlClose);
			return BuildInvocation(span, base.Rewrite.Builtin.List, arguments);
		}

		private Expression ParseModelConstraintsSection()
		{
			TextSpan span = TokCur.Span;
			BeginSavingText();
			EatToken(TokKind.Ident);
			EatToken(TokKind.SquareOpen);
			Expression[] arguments = ((TidCur == TokKind.SquareClose) ? new Expression[0] : ParseModelLabeledExprList());
			EatToken(TokKind.SquareClose);
			EndSavingText();
			return BuildInvocation(span, base.Rewrite.Builtin.Constraints, arguments);
		}

		private Expression ParseModelGoalsSection()
		{
			TextSpan span = TokCur.Span;
			BeginSavingText();
			EatToken(TokKind.Ident);
			EatToken(TokKind.SquareOpen);
			if (TidCur == TokKind.SquareClose)
			{
				EatToken(TokKind.SquareClose);
				EndSavingText();
				return BuildInvocation(span, base.Rewrite.Builtin.Goals);
			}
			Expression[] arguments = ParseModelGoalList();
			EatToken(TokKind.SquareClose);
			EndSavingText();
			return BuildInvocation(span, base.Rewrite.Builtin.Goals, arguments);
		}

		private Expression[] ParseModelGoalList()
		{
			return ParseCommaSeparatedList(ParseModelGoal);
		}

		private Expression ParseModelGoal()
		{
			TextSpan span = TokCur.Span;
			if (TidCur == TokKind.Ident)
			{
				switch (TokCur.As<IdentToken>().Val.ToString())
				{
				case "Minimize":
				{
					EatToken(TokKind.Ident);
					EatToken(TokKind.SquareOpen);
					Expression[] arguments = ((TidCur == TokKind.SquareClose) ? new Expression[0] : ParseModelLabeledExprList());
					EatToken(TokKind.SquareClose);
					return BuildInvocation(span, base.Rewrite.Builtin.Minimize, arguments);
				}
				case "Maximize":
				{
					EatToken(TokKind.Ident);
					EatToken(TokKind.SquareOpen);
					Expression[] arguments = ((TidCur == TokKind.SquareClose) ? new Expression[0] : ParseModelLabeledExprList());
					EatToken(TokKind.SquareClose);
					return BuildInvocation(span, base.Rewrite.Builtin.Maximize, arguments);
				}
				}
			}
			Error("Expected Minimize or Maximize");
			return base.Rewrite.Fail("Expected Minimize or Maximize");
		}

		private Expression[] ParseModelLabeledExprList()
		{
			return ParseCommaSeparatedList(ParseModelLabeledExpr);
		}

		private Expression ParseModelLabeledExpr()
		{
			TextSpan span = TokCur.Span;
			if (TidPeek(1) != TokKind.SubGrt)
			{
				return ParseModelLabeledExprCore();
			}
			Expression expression;
			if (TidCur == TokKind.Ident)
			{
				expression = ParseModelSymbol();
			}
			else if (TidCur == TokKind.StrLit)
			{
				expression = base.ParseAtom();
			}
			else
			{
				Error("Label must be identifier or string", OmlParseExceptionReason.InvalidLabel);
				expression = base.Rewrite.Fail("Label must be identifier or string");
			}
			EatToken(TokKind.SubGrt);
			Expression expression2 = ParseModelLabeledExprCore();
			return BuildInvocation(span, base.Rewrite.Builtin.Rule, expression, expression2);
		}

		private Expression ParseModelLabeledExprCore()
		{
			Expression expression = ParseModelExpression();
			Expression expression2 = expression;
			while (expression2.Head == base.Rewrite.Builtin.Annotation)
			{
				expression2 = expression2[0];
			}
			string text = TextfromSpan(expression2.PlacementInformation.Span);
			if (text != null)
			{
				_expressionStrings[expression] = text;
			}
			return expression;
		}

		private Expression ParseModelSetDomain()
		{
			TextSpan span = TokCur.Span;
			EatToken(TokKind.Ident);
			if (TidCur == TokKind.SquareOpen)
			{
				EatToken(TokKind.SquareOpen);
				Expression expression = ParseModelDomain();
				EatToken(TokKind.SquareClose);
				return BuildInvocation(span, base.Rewrite.Builtin.Sets, expression);
			}
			return base.Rewrite.Builtin.Sets;
		}

		private Expression ParseModelDistributionDomain()
		{
			TextSpan span = TokCur.Span;
			Expression expression = ParseIdent();
			if (TidCur != TokKind.SquareOpen)
			{
				return expression;
			}
			EatToken(TokKind.SquareOpen);
			Expression expression2 = ParseModelExpression();
			if (TidCur == TokKind.SquareClose)
			{
				EatToken(TokKind.SquareClose);
				return BuildInvocation(span, expression, expression2);
			}
			EatToken(TokKind.Comma);
			Expression expression3 = ParseModelExpression();
			EatToken(TokKind.SquareClose);
			return BuildInvocation(span, expression, expression2, expression3);
		}

		private Expression ParseModelDomain()
		{
			TextSpan span = TokCur.Span;
			if (TidCur == TokKind.Ident)
			{
				switch (TokCur.As<IdentToken>().Val.ToString())
				{
				case "Booleans":
					EatToken(TokKind.Ident);
					return base.Rewrite.Builtin.Booleans;
				case "Any":
					EatToken(TokKind.Ident);
					return base.Rewrite.Builtin.Any;
				case "Enum":
				{
					EatToken(TokKind.Ident);
					EatToken(TokKind.SquareOpen);
					TextSpan span2 = TokCur.Span;
					EatToken(TokKind.CurlOpen);
					Expression[] arguments3 = ParseModelExpressionList();
					EatToken(TokKind.CurlClose);
					Expression expression3 = BuildInvocation(span2, base.Rewrite.Builtin.List, arguments3);
					EatToken(TokKind.SquareClose);
					return BuildInvocation(span, base.Rewrite.Builtin.Enum, expression3);
				}
				case "Reals":
				case "Integers":
				{
					Expression expression2 = ParseModelDomainType();
					if (TidCur == TokKind.SquareOpen)
					{
						EatToken(TokKind.SquareOpen);
						Expression[] arguments2 = ParseModelDomainRestriction();
						EatToken(TokKind.SquareClose);
						return BuildInvocation(span, expression2, arguments2);
					}
					return expression2;
				}
				case "Tuples":
				{
					EatToken(TokKind.Ident);
					EatToken(TokKind.SquareOpen);
					Expression[] arguments = ParseModelDomainList();
					EatToken(TokKind.SquareClose);
					return BuildInvocation(span, base.Rewrite.Builtin.Tuples, arguments);
				}
				case "Scenarios":
				{
					EatToken(TokKind.Ident);
					EatToken(TokKind.SquareOpen);
					Expression expression = ParseModelDomain();
					EatToken(TokKind.SquareClose);
					return BuildInvocation(span, base.Rewrite.Builtin.Scenarios, expression);
				}
				default:
					return ParseModelSymbol();
				}
			}
			Error("Expected domain", OmlParseExceptionReason.InvalidDomain);
			return base.Rewrite.Fail("Expected domain");
		}

		private Expression[] ParseModelDomainList()
		{
			return ParseCommaSeparatedList(ParseModelDomain);
		}

		private Expression ParseModelDomainType()
		{
			return ParseIdent();
		}

		private Expression[] ParseModelDomainRestriction()
		{
			TextSpan span = TokCur.Span;
			if (TidCur == TokKind.CurlOpen)
			{
				EatToken(TokKind.CurlOpen);
				Expression[] arguments = ParseModelExpressionList();
				EatToken(TokKind.CurlClose);
				return new Expression[1] { BuildInvocation(span, base.Rewrite.Builtin.List, arguments) };
			}
			Expression expression = ParseModelExpression();
			EatToken(TokKind.Comma);
			Expression expression2 = ParseModelExpression();
			return new Expression[2] { expression, expression2 };
		}

		private Expression[] ParseModelIndexList()
		{
			return ParseCommaSeparatedList(ParseModelIndex);
		}

		private Expression ParseModelIndex()
		{
			return ParseModelExpression();
		}

		private Expression[] ParseModelExpressionList()
		{
			return ParseCommaSeparatedList(ParseModelExpression);
		}

		private Expression ParseModelExpression()
		{
			TextSpan span = TokCur.Span;
			if (TidCur == TokKind.Ident)
			{
				string text = TokCur.As<IdentToken>().Val.ToString();
				string text2;
				if ((text2 = text) != null && text2 == "Annotation")
				{
					return ParseModelAnnotation(span, ParseModelExpression);
				}
				return ParseExpression();
			}
			return ParseExpression();
		}

		private Expression[] ParseModelIterators()
		{
			List<Expression> list = new List<Expression>();
			while (TidCur == TokKind.CurlOpen)
			{
				list.Add(ParseModelIterator());
				EatToken(TokKind.Comma);
			}
			return list.ToArray();
		}

		private Expression ParseModelIterator()
		{
			TextSpan span = TokCur.Span;
			EatToken(TokKind.CurlOpen);
			Expression expression = ParseModelSymbol();
			EatToken(TokKind.Comma);
			Expression[] second = ParseModelIteratorSet();
			EatToken(TokKind.CurlClose);
			return BuildInvocation(span, base.Rewrite.Builtin.List, new Expression[1] { expression }.Concat(second));
		}

		private Expression[] ParseModelIteratorSet()
		{
			TextSpan span = TokCur.Span;
			if (TidCur == TokKind.CurlOpen)
			{
				EatToken(TokKind.CurlOpen);
				Expression[] arguments = ParseModelExpressionList();
				EatToken(TokKind.CurlClose);
				return new Expression[1] { BuildInvocation(span, base.Rewrite.Builtin.List, arguments) };
			}
			List<Expression> list = new List<Expression>();
			list.Add(ParseModelExpression());
			if (TidCur == TokKind.Comma)
			{
				EatToken(TokKind.Comma);
				list.Add(ParseModelExpression());
				if (TidCur == TokKind.Comma)
				{
					EatToken(TokKind.Comma);
					list.Add(ParseModelExpression());
				}
			}
			return list.ToArray();
		}

		internal override bool IsAllowedOperator(Symbol sym)
		{
			switch (sym.ParseInfo.OperatorText)
			{
			case "+":
			case "-":
			case "*":
			case "/":
			case "^":
			case "|":
			case "&":
			case "-:":
			case "==":
			case "!=":
			case "<":
			case "<=":
			case ">":
			case ">=":
			case "!":
				return true;
			default:
				switch (sym.Name)
				{
				case "Abs":
				case "AsInt":
				case "Max":
				case "Min":
				case "Quotient":
				case "Mod":
				case "ModTrunc":
				case "Sos1":
				case "Sos2":
				case "Cos":
				case "Sin":
				case "Tan":
				case "ArcCos":
				case "ArcSin":
				case "ArcTan":
				case "Cosh":
				case "Sinh":
				case "Tanh":
				case "Exp":
				case "Log":
				case "Log10":
				case "Sqrt":
				case "If":
				case "Floor":
				case "Ceiling":
					return true;
				default:
					return false;
				}
			}
		}

		internal override Expression ParseAtom()
		{
			switch (TidCur.Tke)
			{
			default:
			{
				Token token = TokPeek(-1);
				string text = ((token == TokCur) ? string.Format(CultureInfo.InvariantCulture, Resources.UnexpectedTerm0, new object[1] { TidCur }) : string.Format(CultureInfo.InvariantCulture, Resources.UnexpectedTermAfterTerm01, new object[2] { TidCur, token.Kind }));
				Error(text, OmlParseExceptionReason.UnexpectedTerm);
				TidNext();
				return base.Rewrite.Fail(text);
			}
			case (TokKindEnum)72:
			{
				TidNext();
				Expression result = ParseExpression();
				EatToken(TokKind.ParenClose);
				return result;
			}
			case (TokKindEnum)3:
				return new IntegerConstant(base.Rewrite, TokMove().As<IntLitToken>().Val);
			case (TokKindEnum)5:
			{
				DecimalLitToken decimalLitToken = TokMove().As<DecimalLitToken>();
				Rational rat;
				if (decimalLitToken.FormatChar == 'f')
				{
					if (decimalLitToken.GetDouble(out var dbl))
					{
						return new FloatConstant(base.Rewrite, dbl);
					}
				}
				else if (decimalLitToken.GetRational(out rat))
				{
					return RationalConstant.Create(base.Rewrite, rat);
				}
				return base.Rewrite.Fail(Resources.Overflow, decimalLitToken);
			}
			case (TokKindEnum)2003:
				TidNext();
				return base.Rewrite.Builtin.Boolean.True;
			case (TokKindEnum)2004:
				TidNext();
				return base.Rewrite.Builtin.Boolean.False;
			case (TokKindEnum)2005:
				TidNext();
				return base.Rewrite.Builtin.Rational.Infinity;
			case (TokKindEnum)2006:
				TidNext();
				return base.Rewrite.Builtin.Rational.UnsignedInfinity;
			case (TokKindEnum)2007:
				TidNext();
				return base.Rewrite.Builtin.Rational.Indeterminate;
			case (TokKindEnum)8:
				return new StringConstant(base.Rewrite, TokMove().As<StrLitToken>().Val);
			case (TokKindEnum)1:
			{
				IdentToken id = TokCur.As<IdentToken>();
				TidNext();
				Symbol symbol = EnsureSymbol(id, fCanCreate: true);
				Expression expression = symbol;
				if (expression == null)
				{
					Error("Identifier expected but keyword found instead", OmlParseExceptionReason.InvalidName);
					return base.Rewrite.Fail("Identifier expected but keyword found instead");
				}
				if (expression.HasAttribute(base.Rewrite.Attributes.ValuesLocked))
				{
					TextSpan span = TokPeek(-1).Span;
					EatToken(TokKind.SquareOpen);
					Expression[] arguments2;
					switch (symbol.Name)
					{
					default:
						if (!IsAllowedOperator(symbol))
						{
							Error("Identifier expected but reserved word found instead", OmlParseExceptionReason.InvalidName);
						}
						arguments2 = ((TidCur == TokKind.SquareClose) ? new Expression[0] : ParseModelExpressionList());
						break;
					case "FilteredForeach":
					case "FilteredSum":
					{
						Expression[] first = ParseModelIterators();
						Expression expression5 = ParseModelExpression();
						EatToken(TokKind.Comma);
						Expression expression4 = ParseModelExpression();
						arguments2 = first.Concat(new Expression[2] { expression5, expression4 }).ToArray();
						break;
					}
					case "Foreach":
					case "Sum":
					{
						Expression[] first = ParseModelIterators();
						Expression expression4 = ParseModelExpression();
						arguments2 = first.Concat(new Expression[1] { expression4 }).ToArray();
						break;
					}
					case "ElementOf":
					{
						TextSpan span2 = TokCur.Span;
						EatToken(TokKind.CurlOpen);
						Expression[] arguments = ParseModelExpressionList();
						EatToken(TokKind.CurlClose);
						Expression expression2 = BuildInvocation(span2, base.Rewrite.Builtin.List, arguments);
						EatToken(TokKind.Comma);
						Expression expression3 = ParseModelExpression();
						arguments2 = new Expression[2] { expression2, expression3 };
						break;
					}
					}
					EatToken(TokKind.SquareClose);
					return BuildInvocation(span, expression, arguments2);
				}
				return expression;
			}
			}
		}
	}
}
