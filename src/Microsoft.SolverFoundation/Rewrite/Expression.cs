using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class Expression
	{
		private readonly RewriteSystem _rs;

		private PlacementInfo _placementInfo;

		public abstract Expression Head { get; }

		public virtual Symbol FirstSymbolHead
		{
			get
			{
				Expression expression = this;
				Symbol result;
				while ((result = expression as Symbol) == null)
				{
					expression = expression.Head;
				}
				return result;
			}
		}

		public virtual PlacementInfo PlacementInformation
		{
			get
			{
				return _placementInfo;
			}
			set
			{
				_placementInfo = value;
			}
		}

		public RewriteSystem Rewrite => _rs;

		public virtual ParseInfo ParseInfo => ParseInfo.Default;

		public virtual int Arity => 0;

		public virtual Expression this[int i]
		{
			get
			{
				throw new IndexOutOfRangeException();
			}
		}

		public virtual bool IsSingle => true;

		public virtual bool IsNumericValue => false;

		protected Expression(RewriteSystem rs)
		{
			_rs = rs;
		}

		public virtual Expression Evaluate()
		{
			return this;
		}

		public Expression Invoke(params Expression[] args)
		{
			return Invoke(fCanOwnArray: true, args);
		}

		public virtual Expression Invoke(bool fCanOwnArray, params Expression[] args)
		{
			return new Invocation(this, fCanOwnArray, args);
		}

		public virtual Invocation InvokeRaw(bool fCanOwnArray, params Expression[] args)
		{
			return new Invocation(this, fCanOwnArray, args);
		}

		public virtual bool HasAttribute(Symbol sym)
		{
			return false;
		}

		public virtual IEnumerable<Invocation> GetRules(Expression expr)
		{
			yield break;
		}

		public virtual Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			return null;
		}

		public virtual Expression PostSort(InvocationBuilder ib)
		{
			return null;
		}

		public virtual void Format(StringBuilder sb, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			precLeft = Precedence.Atom;
			precRight = Precedence.Atom;
			sb.Append(this);
		}

		public virtual void FormatFull(StringBuilder sb, IExpressionFormatter formatter)
		{
			sb.Append(this);
		}

		public virtual void FormatInvocation(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			FormatInvocationPlain(sb, inv, out precLeft, out precRight, formatter);
		}

		public virtual void FormatInvocationPlain(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			int length = sb.Length;
			Format(sb, out precLeft, out precRight, formatter);
			if ((int)precRight >= 2)
			{
				sb.Insert(length, '(');
				sb.Append(')');
			}
			sb.Append('[');
			formatter.BeginInvocationArgs(sb, inv);
			for (int i = 0; i < inv.Arity; i++)
			{
				formatter.BeginOneArg(sb, inv);
				inv[i].Format(sb, out precLeft, out precRight, formatter);
				if (i < inv.Arity - 1)
				{
					sb.Append(',');
				}
				formatter.EndOneArg(sb, i >= inv.Arity - 1, inv);
			}
			formatter.EndInvocationArgs(sb, inv);
			sb.Append(']');
			precLeft = Precedence.Invocation;
			precRight = Precedence.Atom;
		}

		public virtual void FormatInvocationFull(StringBuilder sb, Invocation inv, IExpressionFormatter formatter)
		{
			FormatFull(sb, null);
			sb.Append('[');
			formatter.BeginInvocationArgs(sb, inv);
			for (int i = 0; i < inv.Arity; i++)
			{
				formatter.BeginOneArg(sb, inv);
				inv[i].FormatFull(sb, formatter);
				if (i < inv.Arity - 1)
				{
					sb.Append(',');
				}
				formatter.EndOneArg(sb, i >= inv.Arity - 1, inv);
			}
			formatter.EndInvocationArgs(sb, inv);
			sb.Append(']');
		}

		public virtual string ToString(IExpressionFormatter formatter)
		{
			return ToString();
		}

		public virtual bool FlattenHead(Expression exprHead)
		{
			if (exprHead == Rewrite.Builtin.ArgumentSplice)
			{
				return !HasAttribute(Rewrite.Attributes.HoldSplice);
			}
			if (exprHead == this)
			{
				return HasAttribute(Rewrite.Attributes.Flat);
			}
			return false;
		}

		public abstract bool Equivalent(Expression expr);

		public abstract int GetEquivalenceHash();

		public virtual bool GetValue(out double val)
		{
			val = 0.0;
			return false;
		}

		public virtual bool GetValue(out BigInteger val)
		{
			val = default(BigInteger);
			return false;
		}

		public virtual bool GetValue(out Rational val)
		{
			val = default(Rational);
			return false;
		}

		public virtual bool GetValue(out int val)
		{
			val = 0;
			return false;
		}

		public virtual bool GetValue(out bool val)
		{
			val = false;
			return false;
		}

		public virtual bool GetValue(out string val)
		{
			val = null;
			return false;
		}

		public virtual bool GetNumericValue(out Rational val)
		{
			val = Rational.Indeterminate;
			return false;
		}

		internal static Expression CombineExprs(Symbol symHead, Expression expr1, Expression expr2)
		{
			Expression[] array;
			if (expr1.Head == symHead)
			{
				if (expr2.Head == symHead)
				{
					int arity = expr1.Arity;
					int arity2 = expr2.Arity;
					if (arity == 0)
					{
						return expr2;
					}
					if (arity2 == 0)
					{
						return expr1;
					}
					array = new Expression[arity + arity2];
					for (int i = 0; i < arity; i++)
					{
						array[i] = expr1[i];
					}
					for (int j = 0; j < arity2; j++)
					{
						array[j + arity] = expr1[j];
					}
				}
				else
				{
					int arity3 = expr1.Arity;
					array = new Expression[arity3 + 1];
					for (int k = 0; k < arity3; k++)
					{
						array[k] = expr1[k];
					}
					array[arity3] = expr2;
				}
			}
			else if (expr2.Head != symHead)
			{
				array = new Expression[2] { expr1, expr2 };
			}
			else
			{
				int arity4 = expr2.Arity;
				array = new Expression[1 + arity4];
				array[0] = expr1;
				for (int l = 0; l < arity4; l++)
				{
					array[l + 1] = expr2[l];
				}
			}
			return new Invocation(symHead, fCanOwnArray: true, array);
		}

		public static Expression operator +(Expression expr1, Expression expr2)
		{
			return CombineExprs(expr1._rs.Builtin.Plus, expr1, expr2);
		}

		public static Expression operator +(Expression expr, Rational num)
		{
			return CombineExprs(expr._rs.Builtin.Plus, expr, RationalConstant.Create(expr._rs, num));
		}

		public static Expression operator +(Rational num, Expression expr)
		{
			return CombineExprs(expr._rs.Builtin.Plus, RationalConstant.Create(expr._rs, num), expr);
		}

		public static Expression operator +(Expression expr, BigInteger num)
		{
			return CombineExprs(expr._rs.Builtin.Plus, expr, new IntegerConstant(expr._rs, num));
		}

		public static Expression operator +(BigInteger num, Expression expr)
		{
			return CombineExprs(expr._rs.Builtin.Plus, new IntegerConstant(expr._rs, num), expr);
		}

		public static Expression operator -(Expression expr1, Expression expr2)
		{
			return CombineExprs(expr1._rs.Builtin.Plus, expr1, expr1._rs.Builtin.Minus.Invoke(expr2));
		}

		public static Expression operator -(Expression expr, Rational num)
		{
			return CombineExprs(expr._rs.Builtin.Plus, expr, RationalConstant.Create(expr._rs, -num));
		}

		public static Expression operator -(Rational num, Expression expr)
		{
			return CombineExprs(expr._rs.Builtin.Plus, RationalConstant.Create(expr._rs, num), expr._rs.Builtin.Minus.Invoke(expr));
		}

		public static Expression operator -(Expression expr, BigInteger num)
		{
			return CombineExprs(expr._rs.Builtin.Plus, expr, new IntegerConstant(expr._rs, -num));
		}

		public static Expression operator -(BigInteger num, Expression expr)
		{
			return CombineExprs(expr._rs.Builtin.Plus, new IntegerConstant(expr._rs, num), expr._rs.Builtin.Minus.Invoke(expr));
		}

		public static Expression operator *(Expression expr1, Expression expr2)
		{
			return CombineExprs(expr1._rs.Builtin.Times, expr1, expr2);
		}

		public static Expression operator *(Expression expr, Rational num)
		{
			return CombineExprs(expr._rs.Builtin.Times, expr, RationalConstant.Create(expr._rs, num));
		}

		public static Expression operator *(Rational num, Expression expr)
		{
			return CombineExprs(expr._rs.Builtin.Times, RationalConstant.Create(expr._rs, num), expr);
		}

		public static Expression operator *(Expression expr, BigInteger num)
		{
			return CombineExprs(expr._rs.Builtin.Times, expr, new IntegerConstant(expr._rs, num));
		}

		public static Expression operator *(BigInteger num, Expression expr)
		{
			return CombineExprs(expr._rs.Builtin.Times, new IntegerConstant(expr._rs, num), expr);
		}

		public static Expression operator |(Expression expr1, Expression expr2)
		{
			return CombineExprs(expr1._rs.Builtin.Or, expr1, expr2);
		}

		public static Expression operator &(Expression expr1, Expression expr2)
		{
			return CombineExprs(expr1._rs.Builtin.And, expr1, expr2);
		}

		public static Expression operator ^(Expression expr1, Expression expr2)
		{
			return CombineExprs(expr1._rs.Builtin.Xor, expr1, expr2);
		}

		public static Expression operator !(Expression expr)
		{
			return expr._rs.Builtin.Not.Invoke(expr);
		}

		public static Expression operator <=(Expression expr1, Expression expr2)
		{
			return CombineExprs(expr1._rs.Builtin.LessEqual, expr1, expr2);
		}

		public static Expression operator <=(Expression expr, Rational num)
		{
			return CombineExprs(expr._rs.Builtin.LessEqual, expr, RationalConstant.Create(expr._rs, num));
		}

		public static Expression operator <=(Rational num, Expression expr)
		{
			return CombineExprs(expr._rs.Builtin.LessEqual, RationalConstant.Create(expr._rs, num), expr);
		}

		public static Expression operator <=(Expression expr, BigInteger num)
		{
			return CombineExprs(expr._rs.Builtin.LessEqual, expr, new IntegerConstant(expr._rs, num));
		}

		public static Expression operator <=(BigInteger num, Expression expr)
		{
			return CombineExprs(expr._rs.Builtin.LessEqual, new IntegerConstant(expr._rs, num), expr);
		}

		public static Expression operator <(Expression expr1, Expression expr2)
		{
			return CombineExprs(expr1._rs.Builtin.Less, expr1, expr2);
		}

		public static Expression operator <(Expression expr, Rational num)
		{
			return CombineExprs(expr._rs.Builtin.Less, expr, RationalConstant.Create(expr._rs, num));
		}

		public static Expression operator <(Rational num, Expression expr)
		{
			return CombineExprs(expr._rs.Builtin.Less, RationalConstant.Create(expr._rs, num), expr);
		}

		public static Expression operator <(Expression expr, BigInteger num)
		{
			return CombineExprs(expr._rs.Builtin.Less, expr, new IntegerConstant(expr._rs, num));
		}

		public static Expression operator <(BigInteger num, Expression expr)
		{
			return CombineExprs(expr._rs.Builtin.Less, new IntegerConstant(expr._rs, num), expr);
		}

		public static Expression operator >=(Expression expr1, Expression expr2)
		{
			return CombineExprs(expr1._rs.Builtin.GreaterEqual, expr1, expr2);
		}

		public static Expression operator >=(Expression expr, Rational num)
		{
			return CombineExprs(expr._rs.Builtin.GreaterEqual, expr, RationalConstant.Create(expr._rs, num));
		}

		public static Expression operator >=(Rational num, Expression expr)
		{
			return CombineExprs(expr._rs.Builtin.GreaterEqual, RationalConstant.Create(expr._rs, num), expr);
		}

		public static Expression operator >=(Expression expr, BigInteger num)
		{
			return CombineExprs(expr._rs.Builtin.GreaterEqual, expr, new IntegerConstant(expr._rs, num));
		}

		public static Expression operator >=(BigInteger num, Expression expr)
		{
			return CombineExprs(expr._rs.Builtin.GreaterEqual, new IntegerConstant(expr._rs, num), expr);
		}

		public static Expression operator >(Expression expr1, Expression expr2)
		{
			return CombineExprs(expr1._rs.Builtin.Greater, expr1, expr2);
		}

		public static Expression operator >(Expression expr, Rational num)
		{
			return CombineExprs(expr._rs.Builtin.Greater, expr, RationalConstant.Create(expr._rs, num));
		}

		public static Expression operator >(Rational num, Expression expr)
		{
			return CombineExprs(expr._rs.Builtin.Greater, RationalConstant.Create(expr._rs, num), expr);
		}

		public static Expression operator >(Expression expr, BigInteger num)
		{
			return CombineExprs(expr._rs.Builtin.Greater, expr, new IntegerConstant(expr._rs, num));
		}

		public static Expression operator >(BigInteger num, Expression expr)
		{
			return CombineExprs(expr._rs.Builtin.Greater, new IntegerConstant(expr._rs, num), expr);
		}
	}
}
