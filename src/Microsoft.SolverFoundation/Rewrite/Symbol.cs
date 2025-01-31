using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary> Symbol is the class for a symbolic variable and is also the base type for
	///           all more specialized symbols.  Each has the characteristics:
	///   Name and Scope,
	///   ParseInfo - precedence and infix operator information,
	///   Attributes - a set of attributes,
	///   Rules - a list of rules like F[x_,y_],
	///   and Id - a unique integer useful for debugging.
	/// </summary>
	internal class Symbol : Expression
	{
		protected struct Key
		{
			public Expression expr;

			public int cvInv;
		}

		protected sealed class KeyComparer : IEqualityComparer<Key>
		{
			public static readonly KeyComparer Instance = new KeyComparer();

			private KeyComparer()
			{
			}

			public bool Equals(Key key1, Key key2)
			{
				if (key1.cvInv == key2.cvInv)
				{
					return key1.expr.Equivalent(key2.expr);
				}
				return false;
			}

			public int GetHashCode(Key key)
			{
				return Statics.CombineHash(key.cvInv, key.expr.GetEquivalenceHash());
			}
		}

		private SymbolScope _scope;

		private readonly string _strName;

		private readonly ParseInfo _pi;

		private Dictionary<Symbol, bool> _mpsymAttrs;

		private Dictionary<Key, List<Invocation>> _mpkeyrgrule;

		private readonly int _id;

		private static int _idNext;

		public virtual string Name => _strName;

		public int Id => _id;

		public virtual SymbolScope Scope
		{
			get
			{
				return _scope;
			}
			internal set
			{
				if (_scope != null && value != null)
				{
					throw new InvalidOperationException(Resources.SymbolAlreadyHasAScope);
				}
				_scope = value;
			}
		}

		public virtual Expression Attributes
		{
			get
			{
				if (_mpsymAttrs == null)
				{
					return base.Rewrite.Builtin.List.Invoke();
				}
				Symbol[] array = new Symbol[_mpsymAttrs.Count];
				_mpsymAttrs.Keys.CopyTo(array, 0);
				return base.Rewrite.Builtin.List.Invoke(array);
			}
		}

		public virtual Expression DownValues
		{
			get
			{
				if (_mpkeyrgrule == null)
				{
					return base.Rewrite.Builtin.List.Invoke();
				}
				Key[] array = new Key[_mpkeyrgrule.Count];
				_mpkeyrgrule.Keys.CopyTo(array, 0);
				List<Invocation> list;
				if (array.Length == 1)
				{
					list = _mpkeyrgrule[array[0]];
				}
				else
				{
					bool flag = true;
					while (flag)
					{
						flag = false;
						for (int i = 1; i < array.Length; i++)
						{
							if (array[i - 1].cvInv > array[i].cvInv)
							{
								Statics.Swap(ref array[i - 1], ref array[i]);
								flag = true;
							}
						}
					}
					list = new List<Invocation>();
					Key[] array2 = array;
					foreach (Key key in array2)
					{
						List<Invocation> collection = _mpkeyrgrule[key];
						list.AddRange(collection);
					}
				}
				return base.Rewrite.Builtin.List.Invoke(list.ToArray());
			}
		}

		public override Expression Head => base.Rewrite.Builtin.Root;

		public override ParseInfo ParseInfo => _pi;

		public Symbol(RewriteSystem rs, string strName)
			: this(rs, rs.Scope, strName, ParseInfo.Default)
		{
		}

		public Symbol(RewriteSystem rs, string strName, ParseInfo pi)
			: this(rs, rs.Scope, strName, pi)
		{
		}

		public Symbol(RewriteSystem rs, SymbolScope scope, string strName)
			: this(rs, scope, strName, ParseInfo.Default)
		{
		}

		public Symbol(RewriteSystem rs, SymbolScope scope, string strName, ParseInfo pi)
			: base(rs)
		{
			_strName = strName;
			_pi = pi;
			_id = Interlocked.Increment(ref _idNext);
			scope?.AddSymbol(this);
		}

		public override Expression Evaluate()
		{
			return base.Rewrite.Evaluate(this);
		}

		public virtual Expression EvaluateInvocationArgsNested(Invocation invHead, InvocationBuilder ib)
		{
			return null;
		}

		public virtual void AdjustAttributes(bool fAdd, params Symbol[] rgattr)
		{
			if (fAdd)
			{
				AddAttributes(rgattr);
			}
			else
			{
				RemoveAttributes(rgattr);
			}
		}

		public virtual void AddAttributes(params Symbol[] rgattr)
		{
			if (rgattr.Length == 0)
			{
				return;
			}
			if (_mpsymAttrs == null)
			{
				_mpsymAttrs = new Dictionary<Symbol, bool>();
			}
			foreach (Symbol key in rgattr)
			{
				if (!_mpsymAttrs.TryGetValue(key, out var _))
				{
					_mpsymAttrs.Add(key, value: true);
				}
			}
		}

		public virtual void RemoveAttributes(params Symbol[] rgattr)
		{
			if (rgattr != null && _mpsymAttrs != null)
			{
				foreach (Symbol key in rgattr)
				{
					_mpsymAttrs.Remove(key);
				}
			}
		}

		public virtual void ClearAttributes()
		{
			if (_mpsymAttrs != null)
			{
				_mpsymAttrs.Clear();
			}
		}

		public override bool HasAttribute(Symbol sym)
		{
			bool value;
			if (_mpsymAttrs != null)
			{
				return _mpsymAttrs.TryGetValue(sym, out value);
			}
			return false;
		}

		public virtual void ClearValues()
		{
			if (_mpkeyrgrule != null)
			{
				_mpkeyrgrule.Clear();
			}
		}

		public override IEnumerable<Invocation> GetRules(Expression expr)
		{
			if (_mpkeyrgrule == null)
			{
				yield break;
			}
			Key key = default(Key);
			key.expr = expr;
			key.cvInv = 0;
			while (true)
			{
				if (_mpkeyrgrule.TryGetValue(key, out var rgrule))
				{
					for (int iv = 0; iv < rgrule.Count; iv++)
					{
						yield return rgrule[iv];
					}
				}
				if (key.expr is Invocation)
				{
					key.expr = key.expr.Head;
					key.cvInv++;
					continue;
				}
				break;
			}
		}

		public virtual void AddRule(Symbol symRuleHead, Expression exprLeft, Expression exprRight, Expression exprCond)
		{
			Key key = default(Key);
			key.cvInv = 0;
			key.expr = exprLeft;
			while (!ExpressionVisitor.VisitExpressions(key.expr, (Expression expr) => expr.Head != base.Rewrite.Builtin.Hole && expr.Head != base.Rewrite.Builtin.HoleSplice))
			{
				key.cvInv++;
				key.expr = key.expr.Head;
			}
			if (key.expr.FirstSymbolHead != this)
			{
				throw new ModelClauseException(Resources.IllFormedRule, key.expr);
			}
			Expression expression = exprLeft;
			if (exprCond != null)
			{
				expression = base.Rewrite.Builtin.Condition.Invoke(expression, exprCond);
			}
			expression = base.Rewrite.Builtin.HoldPattern.Invoke(expression);
			Invocation rule = symRuleHead.InvokeRaw(true, expression, exprRight);
			List<Invocation> value;
			if (_mpkeyrgrule == null)
			{
				_mpkeyrgrule = new Dictionary<Key, List<Invocation>>(KeyComparer.Instance);
				value = new List<Invocation>();
				_mpkeyrgrule.Add(key, value);
			}
			else if (!_mpkeyrgrule.TryGetValue(key, out value))
			{
				value = new List<Invocation>();
				_mpkeyrgrule.Add(key, value);
			}
			base.Rewrite.AddRule(rule, value);
		}

		public virtual void RemoveRule(Expression exprLeft, Expression exprCond)
		{
			if (_mpkeyrgrule != null)
			{
				Key key = default(Key);
				key.cvInv = 0;
				key.expr = exprLeft;
				while (!ExpressionVisitor.VisitExpressions(key.expr, (Expression expr) => expr.Head != base.Rewrite.Builtin.Hole && expr.Head != base.Rewrite.Builtin.HoleSplice))
				{
					key.cvInv++;
					key.expr = key.expr.Head;
				}
				if (_mpkeyrgrule.TryGetValue(key, out var value))
				{
					base.Rewrite.RemoveRule(exprLeft, exprCond, value);
				}
			}
		}

		public override void FormatInvocation(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			if (ParseInfo.HasInfixForm)
			{
				if (ParseInfo.IsUnaryPrefix)
				{
					if (inv.Arity == 1 && !formatter.ShouldForceFullInvocationForm(inv))
					{
						FormatInvocationUnaryPre(sb, inv, out precLeft, out precRight, formatter);
						return;
					}
				}
				else if (ParseInfo.IsUnaryPostfix)
				{
					if (inv.Arity == 1 && !formatter.ShouldForceFullInvocationForm(inv))
					{
						FormatInvocationUnaryPost(sb, inv, out precLeft, out precRight, formatter);
						return;
					}
				}
				else if (!ParseInfo.IsUnaryPostfix && (inv.Arity == 2 || (inv.Arity >= 2 && ParseInfo.VaryadicInfix)) && !formatter.ShouldForceFullInvocationForm(inv))
				{
					FormatInvocationBinary(sb, inv, out precLeft, out precRight, formatter);
					return;
				}
			}
			FormatInvocationPlain(sb, inv, out precLeft, out precRight, formatter);
		}

		protected virtual void FormatInvocationBinary(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			int length = sb.Length;
			inv[0].Format(sb, out precLeft, out precRight, formatter);
			if ((int)precRight >= (int)ParseInfo.LeftPrecedence)
			{
				sb.Insert(length, '(');
				sb.Append(')');
			}
			for (int i = 1; i < inv.Arity; i++)
			{
				formatter.BeforeBinaryOperator(sb, inv);
				sb.Append(ParseInfo.OperatorText);
				formatter.AfterBinaryOperator(sb, inv);
				length = sb.Length;
				inv[i].Format(sb, out precLeft, out precRight, formatter);
				if ((int)precLeft > (int)ParseInfo.RightPrecedence || ((int)precRight >= (int)ParseInfo.LeftPrecedence && i + 1 < inv.Arity))
				{
					sb.Insert(length, '(');
					sb.Append(')');
				}
			}
			precLeft = ParseInfo.LeftPrecedence;
			precRight = ParseInfo.RightPrecedence;
		}

		protected virtual void FormatInvocationUnaryPre(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			sb.Append(ParseInfo.OperatorText);
			int length = sb.Length;
			inv[0].Format(sb, out precLeft, out precRight, formatter);
			if ((int)precLeft > (int)ParseInfo.RightPrecedence)
			{
				sb.Insert(length, '(');
				sb.Append(')');
			}
			precLeft = Precedence.Atom;
			precRight = ParseInfo.RightPrecedence;
		}

		protected virtual void FormatInvocationUnaryPost(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			int length = sb.Length;
			inv[0].Format(sb, out precLeft, out precRight, formatter);
			if ((int)precRight > (int)ParseInfo.LeftPrecedence)
			{
				sb.Insert(length, '(');
				sb.Append(')');
			}
			sb.Append(ParseInfo.OperatorText);
			precLeft = ParseInfo.LeftPrecedence;
			precRight = Precedence.Atom;
		}

		public override string ToString()
		{
			return Name;
		}

		public override bool Equivalent(Expression expr)
		{
			return this == expr;
		}

		public override int GetEquivalenceHash()
		{
			return GetHashCode();
		}
	}
}
