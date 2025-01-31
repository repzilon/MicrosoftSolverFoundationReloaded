using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class RewriteSystem
	{
		internal class RuleApplier
		{
			protected sealed class CondInfo
			{
				public Expression _exprCond;

				public Expression _exprPatt;

				public CondInfo _ciNext;
			}

			protected sealed class ArgMatch
			{
				public Invocation _invPatt;

				public Invocation _invSrc;

				public CondInfo _ciFirst;

				public int _iargMin;

				public bool[] _rgfSeq;

				public int[] _rgiargMin;

				public int _markSub;

				public int _cam;

				[Conditional("DEBUG")]
				public void AssertValid()
				{
					int num = _rgfSeq.Length;
					while (--num >= 0)
					{
					}
				}

				public void Reset()
				{
					int num = _iargMin;
					for (int i = 0; i < _rgfSeq.Length; i++)
					{
						_rgiargMin[i] = num;
						if (!_rgfSeq[i])
						{
							num++;
						}
					}
				}

				private int LastPosSeq()
				{
					int num = _rgfSeq.Length;
					while (--num >= 0)
					{
						if (_rgfSeq[num] && _rgiargMin[num + 1] - _rgiargMin[num] > 0)
						{
							return num;
						}
					}
					return -1;
				}

				public bool Advance()
				{
					int num = LastPosSeq();
					int num2 = num;
					do
					{
						if (--num2 < 0)
						{
							return false;
						}
					}
					while (!_rgfSeq[num2]);
					int num3 = _rgiargMin[num2 + 1] + 1;
					for (int i = num2 + 1; i < _rgfSeq.Length; i++)
					{
						_rgiargMin[i] = num3;
						if (!_rgfSeq[i])
						{
							num3++;
						}
					}
					return true;
				}
			}

			private RewriteSystem _rs;

			private Substitution _sub;

			protected List<ArgMatch> _rgam;

			public RuleApplier(RewriteSystem rs)
			{
				_rs = rs;
				_sub = new Substitution();
				_rgam = new List<ArgMatch>();
			}

			public bool Match(Expression exprPatt, Expression exprSrc)
			{
				return MatchCore(exprPatt, exprSrc);
			}

			public bool ApplyRule(Expression exprSrc, Invocation rule, out Expression exprRes)
			{
				exprRes = null;
				Expression rulePattern = _rs.GetRulePattern(rule);
				if (rulePattern == null)
				{
					return false;
				}
				if (!MatchCore(rulePattern, exprSrc))
				{
					return false;
				}
				exprRes = rule[1] * _sub;
				return true;
			}

			protected bool MatchCore(Expression exprPatt, Expression exprSrc)
			{
				_sub.Clear();
				_rgam.Clear();
				if (!AddMatch(exprPatt, exprSrc))
				{
					return false;
				}
				for (int i = 0; i != _rgam.Count; i++)
				{
					ArgMatch argMatch = _rgam[i];
					argMatch._cam = _rgam.Count;
					argMatch._markSub = _sub.PushMark();
					while (!AddEntries(argMatch))
					{
						Statics.TrimList(_rgam, argMatch._cam);
						_sub.PopToMark(argMatch._markSub);
						while (!argMatch.Advance())
						{
							if (i == 0)
							{
								return false;
							}
							argMatch.Reset();
							argMatch = _rgam[--i];
							Statics.TrimList(_rgam, argMatch._cam);
							_sub.PopToMark(argMatch._markSub);
						}
					}
				}
				return true;
			}

			protected bool AddMatch(Expression exprPatt, Expression exprSrc)
			{
				Symbol patternSymbol = GetPatternSymbol(ref exprPatt);
				if (patternSymbol != null && !AddBinding(patternSymbol, exprSrc))
				{
					return false;
				}
				if (IsHole(exprPatt) || IsHoleSplice(exprPatt) || exprPatt == exprSrc)
				{
					return true;
				}
				if (exprPatt is Constant)
				{
					return exprPatt.Equivalent(exprSrc);
				}
				if (exprPatt is Invocation invPatt)
				{
					if (exprPatt.Head == _rs.Builtin.Condition && exprPatt.Arity == 2)
					{
						int count = _rgam.Count;
						if (!AddMatch(exprPatt[0], exprSrc))
						{
							return false;
						}
						return AddCondition(exprPatt[0], exprPatt[1], count);
					}
					if (exprSrc is Invocation invSrc && AddMatch(exprPatt.Head, exprSrc.Head))
					{
						return AddArgMatch(invPatt, invSrc);
					}
					return false;
				}
				return false;
			}

			protected bool AddCondition(Expression exprPatt, Expression exprCond, int cam)
			{
				if (cam == _rgam.Count)
				{
					return CheckCondition(exprCond, exprPatt);
				}
				CondInfo condInfo = new CondInfo();
				condInfo._exprCond = exprCond;
				condInfo._exprPatt = exprPatt;
				ArgMatch argMatch = _rgam[_rgam.Count - 1];
				condInfo._ciNext = argMatch._ciFirst;
				argMatch._ciFirst = condInfo;
				return true;
			}

			protected static bool SetsPatternVar(Expression exprPatt, Symbol sym)
			{
				return PatternVarVisitor.HasPatternVar(exprPatt, sym);
			}

			protected bool CheckCondition(Expression exprCond, Expression exprPatt)
			{
				exprCond = _sub.Apply(exprCond, (Symbol symCur) => SetsPatternVar(exprPatt, symCur));
				exprCond = exprCond.Evaluate();
				if (exprCond.GetValue(out bool val))
				{
					return val;
				}
				return false;
			}

			protected bool AddMatchList(Expression exprPatt, Invocation invSrc, int iargMin, int iargLim)
			{
				if (iargLim == iargMin + 1)
				{
					return AddMatch(exprPatt, invSrc[iargMin]);
				}
				if (exprPatt.Head == exprPatt.Rewrite.Builtin.Condition)
				{
					int count = _rgam.Count;
					if (!AddMatchList(exprPatt[0], invSrc, iargMin, iargLim))
					{
						return false;
					}
					return AddCondition(exprPatt[0], exprPatt[1], count);
				}
				Symbol patternSymbol = GetPatternSymbol(ref exprPatt);
				if (patternSymbol != null)
				{
					return AddBindingList(patternSymbol, invSrc, iargMin, iargLim);
				}
				return true;
			}

			protected bool AddEntries(ArgMatch am)
			{
				int count = _rgam.Count;
				for (int i = 0; i < am._rgfSeq.Length; i++)
				{
					if (!AddMatchList(am._invPatt[i], am._invSrc, am._rgiargMin[i], am._rgiargMin[i + 1]))
					{
						return false;
					}
				}
				if (am._ciFirst == null)
				{
					return true;
				}
				if (count == _rgam.Count)
				{
					for (CondInfo condInfo = am._ciFirst; condInfo != null; condInfo = condInfo._ciNext)
					{
						if (!CheckCondition(condInfo._exprCond, condInfo._exprPatt))
						{
							return false;
						}
					}
				}
				else
				{
					ArgMatch argMatch = _rgam[_rgam.Count - 1];
					if (argMatch._ciFirst == null)
					{
						argMatch._ciFirst = am._ciFirst;
					}
					else
					{
						CondInfo condInfo2 = argMatch._ciFirst;
						while (condInfo2._ciNext != null)
						{
							condInfo2 = condInfo2._ciNext;
						}
						condInfo2._ciNext = am._ciFirst;
					}
				}
				return true;
			}

			protected bool AddArgMatch(Invocation invPatt, Invocation invSrc)
			{
				int num = 0;
				while (true)
				{
					if (num == invPatt.Arity)
					{
						return num == invSrc.Arity;
					}
					if (IsSplicePattern(invPatt[num]))
					{
						break;
					}
					if (num >= invSrc.Arity)
					{
						return false;
					}
					if (!AddMatch(invPatt[num], invSrc[num]))
					{
						return false;
					}
					num++;
				}
				int num2 = invPatt.Arity;
				int num3 = invSrc.Arity;
				while (true)
				{
					if (num3 < num)
					{
						return false;
					}
					if (IsSplicePattern(invPatt[num2 - 1]))
					{
						break;
					}
					if (num3 <= num || !AddMatch(invPatt[num2 - 1], invSrc[num3 - 1]))
					{
						return false;
					}
					num2--;
					num3--;
				}
				if (num == num3)
				{
					for (int i = num; i < num2; i++)
					{
						if (!IsSplicePattern(invPatt[i]) || !AddMatchList(invPatt[i], invSrc, num, num))
						{
							return false;
						}
					}
					return true;
				}
				if (num == num2 - 1)
				{
					return AddMatchList(invPatt[num], invSrc, num, num3);
				}
				bool[] array = new bool[num2 - num];
				int[] array2 = new int[array.Length + 1];
				array2[array.Length] = num3;
				int num4 = num;
				for (int j = 0; j < array.Length; j++)
				{
					array2[j] = num4;
					array[j] = IsSplicePattern(invPatt[j + num]);
					if (!array[j] && ++num4 > num3)
					{
						return false;
					}
				}
				ArgMatch argMatch = new ArgMatch();
				argMatch._invPatt = invPatt;
				argMatch._invSrc = invSrc;
				argMatch._iargMin = num;
				argMatch._rgfSeq = array;
				argMatch._rgiargMin = array2;
				_rgam.Add(argMatch);
				return true;
			}

			protected static Symbol GetPatternSymbol(ref Expression exprPatt)
			{
				if (exprPatt.Head != exprPatt.Rewrite.Builtin.Pattern || exprPatt.Arity != 2 || !(exprPatt[0] is Symbol result))
				{
					return null;
				}
				exprPatt = exprPatt[1];
				return result;
			}

			protected static bool IsHole(Expression expr)
			{
				if (expr.Head == expr.Rewrite.Builtin.Hole)
				{
					return expr.Arity == 0;
				}
				return false;
			}

			protected static bool IsHoleSplice(Expression expr)
			{
				if (expr.Head == expr.Rewrite.Builtin.HoleSplice)
				{
					return expr.Arity == 0;
				}
				return false;
			}

			protected static bool IsSplicePattern(Expression expr)
			{
				while (expr.Head == expr.Rewrite.Builtin.Condition && expr.Arity == 2)
				{
					expr = expr[0];
				}
				GetPatternSymbol(ref expr);
				return IsHoleSplice(expr);
			}

			protected bool AddBinding(Symbol sym, Expression expr)
			{
				_sub.PushMark();
				return _sub.Add(sym, expr);
			}

			protected bool AddBindingList(Symbol sym, Invocation inv, int iargMin, int iargLim)
			{
				_sub.PushMark();
				return _sub.AddList(sym, inv, iargMin, iargLim);
			}
		}

		public readonly BuiltinAttributes Attributes;

		internal readonly BuiltinSymbols Builtin;

		private readonly SymbolScope _scopeRoot;

		private bool _fAbort;

		private int _depthCur;

		private int _depthLim;

		protected Action<ValueTableAdapter, string, string[]> _bindParamDelegate;

		internal Action<ValueTableAdapter, string, string[]> BindParamDelegate => _bindParamDelegate;

		public bool ShouldAbort
		{
			get
			{
				return _fAbort;
			}
			set
			{
				_fAbort = value;
			}
		}

		public int NestedLimit => _depthLim;

		public int NestedLevel
		{
			get
			{
				return _depthCur;
			}
			set
			{
				_depthCur = value;
			}
		}

		public SymbolScope Scope => _scopeRoot;

		public event Action<string> MessageLog;

		public RewriteSystem(Action<ValueTableAdapter, string, string[]> bindParamDel)
			: this()
		{
			_bindParamDelegate = bindParamDel;
		}

		public RewriteSystem()
		{
			_depthLim = 1000;
			_scopeRoot = new SymbolScope(null);
			Attributes = new BuiltinAttributes(this);
			Builtin = new BuiltinSymbols(this);
			foreach (Symbol item in _scopeRoot)
			{
				item.AddAttributes(Attributes.ValuesLocked);
			}
			Builtin.Root.AddAttributes(Attributes.AttributesLocked);
		}

		public void Log(string str)
		{
			this.MessageLog?.Invoke(str);
		}

		public void Log(string strFmt, params object[] args)
		{
			Action<string> messageLog = this.MessageLog;
			if (messageLog != null)
			{
				string obj = string.Format(CultureInfo.InvariantCulture, strFmt, args);
				messageLog(obj);
			}
		}

		public Expression Fail(string str)
		{
			return Builtin.Failed.Invoke(new StringConstant(this, str));
		}

		public Expression Fail(string str, params object[] args)
		{
			return Builtin.Failed.Invoke(new StringConstant(this, string.Format(CultureInfo.InvariantCulture, str, args)));
		}

		public Expression FailOnValuesLocked(Symbol sym)
		{
			if (sym.HasAttribute(Attributes.ValuesLocked))
			{
				return Fail(Resources.ValuesForSymbolAreLocked, sym);
			}
			return null;
		}

		public Expression FailOnAttributesLocked(Symbol sym)
		{
			if (sym.HasAttribute(Attributes.AttributesLocked))
			{
				return Fail(Resources.AttributesForSymbolAreLocked, sym);
			}
			return null;
		}

		/// <summary> Check for null expression
		/// </summary>
		/// <param name="exp"></param>
		/// <returns></returns>
		public bool IsNull(Expression exp)
		{
			return exp == Builtin.Null;
		}

		public void CheckAbort()
		{
			if (ShouldAbort)
			{
				Abort();
			}
			if (_depthCur > _depthLim)
			{
				throw new RewriteAbortException(string.Format(CultureInfo.InvariantCulture, Resources.RecursionLimitExceeded, new object[1] { _depthLim }));
			}
		}

		public static void Abort()
		{
			throw new RewriteAbortException();
		}

		protected internal Expression Evaluate(Symbol symSrc)
		{
			int depthCur = _depthCur++;
			try
			{
				RuleApplier ruleApplier = null;
				bool flag;
				do
				{
					flag = false;
					foreach (Invocation rule in symSrc.GetRules(symSrc))
					{
						CheckAbort();
						if (ruleApplier == null)
						{
							ruleApplier = new RuleApplier(this);
						}
						if (ruleApplier.ApplyRule(symSrc, rule, out var exprRes))
						{
							if (exprRes != symSrc)
							{
								symSrc = exprRes as Symbol;
								if (symSrc == null)
								{
									return exprRes.Evaluate();
								}
								flag = true;
								break;
							}
							break;
						}
					}
				}
				while (flag);
				return symSrc;
			}
			finally
			{
				_depthCur = depthCur;
			}
		}

		protected internal Expression Evaluate(Invocation invSrc)
		{
			int depthCur = _depthCur++;
			try
			{
				PlacementInfo placementInformation = invSrc.PlacementInformation;
				using (InvocationBuilder invocationBuilder = InvocationBuilder.GetBuilder(invSrc, fKeepAll: false))
				{
					Expression expression;
					while (true)
					{
						CheckAbort();
						invocationBuilder.HeadNew = invocationBuilder.HeadOld.Evaluate();
						EvaluateArgsCore(invocationBuilder);
						expression = EvaluateTailCall(invocationBuilder, ref invSrc);
						if (expression != null)
						{
							break;
						}
						invocationBuilder.Reset(invSrc, fKeepAll: false);
					}
					expression.PlacementInformation = placementInformation;
					return expression;
				}
			}
			finally
			{
				_depthCur = depthCur;
			}
		}

		private Expression EvaluateTailCall(InvocationBuilder ib, ref Invocation invSrc)
		{
			RuleApplier ruleApplier = null;
			if ((ib.HeadNew.HasAttribute(Attributes.Listable) && ThreadOverLists(ib, out var exprRes)) || (exprRes = ib.HeadNew.EvaluateInvocationArgs(ib)) != null)
			{
				invSrc = exprRes as Invocation;
				if (invSrc == null)
				{
					return exprRes.Evaluate();
				}
				return null;
			}
			if (ib.HeadNew.HasAttribute(Attributes.Orderless))
			{
				ib.SortArgs();
				if ((exprRes = ib.HeadNew.PostSort(ib)) != null)
				{
					invSrc = exprRes as Invocation;
					if (invSrc == null)
					{
						return exprRes.Evaluate();
					}
					return null;
				}
			}
			if (ib.Count == 1 && ib.HeadNew.HasAttribute(Attributes.UnaryIdentity) && ib[0].IsSingle)
			{
				return ib[0];
			}
			if (ib.Diff)
			{
				exprRes = ib.GetNew();
				invSrc = exprRes as Invocation;
				if (invSrc == null)
				{
					return exprRes;
				}
			}
			bool flag = false;
			foreach (Invocation rule in invSrc.FirstSymbolHead.GetRules(invSrc))
			{
				CheckAbort();
				if (ruleApplier == null)
				{
					ruleApplier = new RuleApplier(this);
				}
				if (ruleApplier.ApplyRule(invSrc, rule, out exprRes))
				{
					if (!(exprRes is Invocation invocation))
					{
						invSrc = null;
						return exprRes.Evaluate();
					}
					flag = !invSrc.Equivalent(invocation);
					invSrc = invocation;
					break;
				}
			}
			if (!flag)
			{
				return invSrc;
			}
			return null;
		}

		protected void EvaluateArgsCore(InvocationBuilder ib)
		{
			bool flag = ib.HeadNew.HasAttribute(Attributes.HoldAll);
			bool flag2 = flag || ib.HeadNew.HasAttribute(Attributes.HoldRest);
			bool flag3 = flag || ib.HeadNew.HasAttribute(Attributes.HoldFirst);
			while (ib.StartNextArg())
			{
				CheckAbort();
				Expression argCur = ib.ArgCur;
				Expression expression = (((ib.IargCur == 0 && !flag3) || (ib.IargCur > 0 && !flag2)) ? argCur.Evaluate() : ((argCur.Head != Builtin.Evaluate || argCur.Arity != 1) ? argCur : argCur[0].Evaluate()));
				if (ib.HeadNew.FlattenHead(expression.Head))
				{
					for (int i = 0; i < expression.Arity; i++)
					{
						ib.AddNewArg(expression[i]);
					}
				}
				else
				{
					ib.AddNewArg(expression);
				}
			}
		}

		public bool ThreadOverLists(InvocationBuilder ib, out Expression exprRes)
		{
			int num = -1;
			int num2 = 0;
			int num3 = 0;
			for (int i = 0; i < ib.Count; i++)
			{
				if (!(ib[i] is Invocation invocation) || invocation.Head != Builtin.List)
				{
					num3++;
					continue;
				}
				if (num2 == 0)
				{
					num = invocation.Arity;
				}
				else if (num != invocation.Arity)
				{
					exprRes = null;
					return false;
				}
				num2++;
			}
			if (num2 == 0)
			{
				exprRes = null;
				return false;
			}
			int num4 = num2 + num3;
			Expression[] array = new Expression[num];
			Expression[] array2 = new Expression[num4];
			for (int j = 0; j < num; j++)
			{
				for (int k = 0; k < num4; k++)
				{
					if (!(ib[k] is Invocation invocation2) || invocation2.Head != Builtin.List)
					{
						array2[k] = ib[k];
					}
					else
					{
						array2[k] = ib[k][j];
					}
				}
				array[j] = ib.HeadNew.Invoke(j == num - 1, array2);
			}
			exprRes = Builtin.List.Invoke(array);
			return true;
		}

		internal void AddRule(Invocation rule, List<Invocation> rgrule)
		{
			RuleApplier ra = null;
			Expression exprCond;
			Expression rulePattern = GetRulePattern(rule, out exprCond);
			for (int i = 0; i < rgrule.Count; i++)
			{
				int ruleOrder = GetRuleOrder(rulePattern, exprCond, rgrule[i], ref ra);
				if (ruleOrder <= 0)
				{
					if (ruleOrder < 0)
					{
						rgrule.Insert(i, rule);
					}
					else
					{
						rgrule[i] = rule;
					}
					return;
				}
			}
			rgrule.Add(rule);
		}

		internal void RemoveRule(Expression exprPatt, Expression exprCond, List<Invocation> rgrule)
		{
			RuleApplier ra = null;
			for (int i = 0; i < rgrule.Count; i++)
			{
				int ruleOrder = GetRuleOrder(exprPatt, exprCond, rgrule[i], ref ra);
				if (ruleOrder <= 0)
				{
					if (ruleOrder == 0)
					{
						rgrule.RemoveAt(i);
					}
					break;
				}
			}
		}

		protected virtual Expression GetRulePattern(Invocation rule)
		{
			if ((rule.Head != Builtin.Rule && rule.Head != Builtin.RuleDelayed) || rule.Arity != 2)
			{
				return null;
			}
			Expression expression = rule[0];
			while (expression.Head == Builtin.HoldPattern && expression.Arity == 1)
			{
				expression = expression[0];
			}
			return expression;
		}

		protected virtual Expression GetRulePattern(Invocation rule, out Expression exprCond)
		{
			exprCond = null;
			Expression expression = GetRulePattern(rule);
			if (expression == null)
			{
				return null;
			}
			while (expression.Head == Builtin.Condition && expression.Arity == 2)
			{
				if (exprCond == null)
				{
					exprCond = expression[1];
				}
				else
				{
					exprCond = Builtin.And.Invoke(expression[1], exprCond);
				}
				expression = expression[0];
			}
			return expression;
		}

		protected int GetRuleOrder(Expression exprPatt1, Expression exprCond1, Invocation rule2, ref RuleApplier ra)
		{
			Expression exprCond2;
			Expression rulePattern = GetRulePattern(rule2, out exprCond2);
			Dictionary<Symbol, Symbol> mpvarvar = null;
			Dictionary<Symbol, Symbol> mpvarvarInv = null;
			if (MatchPatternsModVars(exprPatt1, rulePattern, ref mpvarvar, ref mpvarvarInv))
			{
				if (exprCond1 == null)
				{
					if (exprCond2 == null)
					{
						return 0;
					}
					return 1;
				}
				if (exprCond2 == null)
				{
					return -1;
				}
				if (MatchModVars(exprCond1, exprCond2, mpvarvar))
				{
					return 0;
				}
				return 1;
			}
			if (ra == null)
			{
				ra = new RuleApplier(this);
			}
			if (ra.Match(rulePattern, exprPatt1) && !ra.Match(exprPatt1, rulePattern))
			{
				return -1;
			}
			return 1;
		}

		internal bool MatchPatternsModVars(Expression expr1, Expression expr2, ref Dictionary<Symbol, Symbol> mpvarvar, ref Dictionary<Symbol, Symbol> mpvarvarInv)
		{
			if (expr1 is Symbol symbol)
			{
				if (!(expr2 is Symbol symbol2))
				{
					return false;
				}
				if (mpvarvar == null)
				{
					return symbol == symbol2;
				}
				if (mpvarvar.TryGetValue(symbol, out var value))
				{
					return symbol2 == value;
				}
				if (symbol == symbol2)
				{
					return !mpvarvarInv.ContainsKey(symbol);
				}
				return false;
			}
			if (expr1 is Invocation invocation)
			{
				if (!(expr2 is Invocation invocation2))
				{
					return false;
				}
				if (invocation.Arity != invocation2.Arity || !MatchPatternsModVars(invocation.Head, invocation2.Head, ref mpvarvar, ref mpvarvarInv))
				{
					return false;
				}
				if (invocation.Head == Builtin.Pattern && invocation.Arity == 2 && invocation[0] is Symbol symbol3)
				{
					if (!(invocation2[0] is Symbol symbol4))
					{
						return false;
					}
					Symbol value2;
					if (mpvarvar == null)
					{
						mpvarvar = new Dictionary<Symbol, Symbol>();
						mpvarvarInv = new Dictionary<Symbol, Symbol>();
						mpvarvar.Add(symbol3, symbol4);
						mpvarvarInv.Add(symbol4, symbol3);
					}
					else if (!mpvarvar.TryGetValue(symbol3, out value2))
					{
						if (mpvarvarInv.ContainsKey(symbol4))
						{
							return false;
						}
						mpvarvar.Add(symbol3, symbol4);
						mpvarvarInv.Add(symbol4, symbol3);
					}
					else if (symbol4 != value2)
					{
						return false;
					}
					return MatchPatternsModVars(invocation[1], invocation2[1], ref mpvarvar, ref mpvarvarInv);
				}
				for (int i = 0; i < invocation.Arity; i++)
				{
					if (!MatchPatternsModVars(invocation[i], invocation2[i], ref mpvarvar, ref mpvarvarInv))
					{
						return false;
					}
				}
				return true;
			}
			return expr1.Equivalent(expr2);
		}

		internal bool MatchModVars(Expression expr1, Expression expr2, Dictionary<Symbol, Symbol> mpvarvar)
		{
			if (expr1 == expr2)
			{
				return true;
			}
			if (expr1 is Symbol key)
			{
				if (expr2 is Symbol && mpvarvar != null && mpvarvar.TryGetValue(key, out var value))
				{
					return expr2 == value;
				}
				return false;
			}
			if (expr1 is Invocation invocation)
			{
				if (!(expr2 is Invocation invocation2))
				{
					return false;
				}
				if (invocation.Arity != invocation2.Arity || !MatchModVars(invocation.Head, invocation2.Head, mpvarvar))
				{
					return false;
				}
				for (int i = 0; i < invocation.Arity; i++)
				{
					if (!MatchModVars(invocation[i], invocation2[i], mpvarvar))
					{
						return false;
					}
				}
				return true;
			}
			return expr1.Equivalent(expr2);
		}

		protected internal Invocation EvaluateHeadAndArgs(Invocation invSrc)
		{
			using (InvocationBuilder invocationBuilder = InvocationBuilder.GetBuilder(invSrc, fKeepAll: false))
			{
				invocationBuilder.HeadNew = invocationBuilder.HeadOld.Evaluate();
				EvaluateArgsCore(invocationBuilder);
				return invocationBuilder.GetNewRaw();
			}
		}

		public bool ApplyRule(Expression exprSrc, Invocation rule, out Expression exprRes)
		{
			RuleApplier ruleApplier = new RuleApplier(this);
			return ruleApplier.ApplyRule(exprSrc, rule, out exprRes);
		}

		public bool IsValidRuleSet(Expression rules)
		{
			if (rules.Head == Builtin.Rule || rules.Head == Builtin.RuleDelayed)
			{
				return true;
			}
			if (rules.Head != Builtin.List)
			{
				return false;
			}
			int num = rules.Arity;
			while (--num >= 0)
			{
				if (!(rules[num] is Invocation invocation) || (invocation.Head != Builtin.Rule && invocation.Head != Builtin.RuleDelayed))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// If no rule applied, this returns exprSrc and sets irule to -1. If rules is a single rule
		/// that applies, irule is set to 0. This asserts that the rule set is valid.
		/// </summary>
		public Expression ApplyRuleSet(Expression exprSrc, Invocation rules, out int irule)
		{
			irule = -1;
			Expression exprRes;
			if (rules.Head == Builtin.Rule || rules.Head == Builtin.RuleDelayed)
			{
				if (new RuleApplier(this).ApplyRule(exprSrc, rules, out exprRes))
				{
					irule = 0;
					return exprRes;
				}
				return exprSrc;
			}
			if (rules.Arity == 0)
			{
				return exprSrc;
			}
			RuleApplier ruleApplier = new RuleApplier(this);
			for (int i = 0; i < rules.Arity; i++)
			{
				if (ruleApplier.ApplyRule(exprSrc, (Invocation)rules[i], out exprRes))
				{
					irule = i;
					return exprRes;
				}
			}
			return exprSrc;
		}

		public bool Match(Expression exprPatt, Expression exprSrc)
		{
			RuleApplier ruleApplier = new RuleApplier(this);
			return ruleApplier.Match(exprPatt, exprSrc);
		}
	}
}
