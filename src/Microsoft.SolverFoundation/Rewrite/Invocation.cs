using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class Invocation : Expression
	{
		/// <summary> thread continuation  
		/// </summary>
		private class Context : IDisposable
		{
			public Invocation _inv;

			public Expression _expr;

			public AutoResetEvent _waitHandle;

			public string _exceptionMessage;

			public bool _exceptionCatched;

			public Context(Invocation inv)
			{
				_inv = inv;
				_waitHandle = new AutoResetEvent(initialState: false);
				_exceptionCatched = false;
			}

			public void Dispose()
			{
				if (_waitHandle != null)
				{
					_waitHandle.Close();
					_waitHandle = null;
				}
				_inv = null;
				_expr = null;
				_exceptionMessage = null;
			}
		}

		private Expression _head;

		private Expression[] _args;

		private int _hash;

		private static readonly Expression[] _argsEmpty = new Expression[0];

		[ThreadStatic]
		private static int _NestedLevel = 0;

		public static Expression[] EmptyArgs => _argsEmpty;

		public override int Arity => _args.Length;

		public override bool IsSingle
		{
			get
			{
				if (Head == base.Rewrite.Builtin.ArgumentSplice || Head == base.Rewrite.Builtin.HoleSplice)
				{
					return false;
				}
				if (Head == base.Rewrite.Builtin.Pattern && Arity == 2 && this[0] is Symbol)
				{
					return this[1].IsSingle;
				}
				return true;
			}
		}

		public override Expression Head
		{
			[DebuggerStepThrough]
			get
			{
				return _head;
			}
		}

		public override Expression this[int iv]
		{
			[DebuggerStepThrough]
			get
			{
				return _args[iv];
			}
		}

		internal Expression[] ArgsArray => _args;

		public IEnumerable<Expression> Args
		{
			get
			{
				try
				{
					Expression[] args = _args;
					for (int i = 0; i < args.Length; i++)
					{
						yield return args[i];
					}
				}
				finally
				{
				}
			}
		}

		internal Invocation(Expression exprHead, bool fCanOwnArray, params Expression[] args)
			: base(exprHead.Rewrite)
		{
			_head = exprHead;
			if (args.Length == 0)
			{
				_args = _argsEmpty;
				return;
			}
			foreach (Expression expression in args)
			{
				if (expression.Rewrite != base.Rewrite)
				{
					throw new InvalidOperationException(Resources.CanTMixExpressionFromDifferentRewriteSystems);
				}
			}
			if (fCanOwnArray)
			{
				_args = args;
				return;
			}
			_args = new Expression[args.Length];
			for (int j = 0; j < args.Length; j++)
			{
				_args[j] = args[j];
			}
		}

		public Invocation Apply(Expression exprHead)
		{
			return new Invocation(exprHead, fCanOwnArray: true, _args);
		}

		public override Expression Evaluate()
		{
			try
			{
				if (_NestedLevel >= base.Rewrite.NestedLimit - 2)
				{
					_NestedLevel++;
					using (Context context = new Context(this))
					{
						base.Rewrite.NestedLevel -= base.Rewrite.NestedLimit;
						ThreadPool.QueueUserWorkItem(Eval, context);
						context._waitHandle.WaitOne();
						if (context._exceptionCatched)
						{
							throw new RewriteAbortException(context._exceptionMessage);
						}
						return context._expr;
					}
				}
				_NestedLevel++;
				return base.Rewrite.Evaluate(this);
			}
			finally
			{
				_NestedLevel--;
			}
		}

		private void Eval(object state)
		{
			Context context = (Context)state;
			try
			{
				context._expr = base.Rewrite.Evaluate(context._inv);
			}
			catch (RewriteAbortException ex)
			{
				context._exceptionMessage = ex.Message;
				context._exceptionCatched = true;
			}
			finally
			{
				context._waitHandle.Set();
			}
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			return FirstSymbolHead.EvaluateInvocationArgsNested(this, ib);
		}

		public override void Format(StringBuilder sb, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			_head.FormatInvocation(sb, this, out precLeft, out precRight, formatter);
		}

		public override void FormatFull(StringBuilder sb, IExpressionFormatter formatter)
		{
			_head.FormatInvocationFull(sb, this, formatter);
		}

		public override string ToString()
		{
			return ToString(new DefaultExpressionFormatter());
		}

		public override string ToString(IExpressionFormatter formatter)
		{
			StringBuilder stringBuilder = new StringBuilder();
			_head.FormatInvocation(stringBuilder, this, out var _, out var _, formatter);
			return stringBuilder.ToString();
		}

		public override bool Equivalent(Expression expr)
		{
			if (expr == this)
			{
				return true;
			}
			if (!(expr is Invocation invocation))
			{
				return false;
			}
			if (GetEquivalenceHash() != invocation.GetEquivalenceHash())
			{
				return false;
			}
			if (_args.Length != invocation._args.Length || !_head.Equivalent(invocation._head))
			{
				return false;
			}
			for (int i = 0; i < invocation._args.Length; i++)
			{
				if (!_args[i].Equivalent(invocation._args[i]))
				{
					return false;
				}
			}
			return true;
		}

		public override int GetEquivalenceHash()
		{
			if (_hash != 0)
			{
				return _hash;
			}
			int num = _head.GetEquivalenceHash();
			Expression[] args = _args;
			foreach (Expression expression in args)
			{
				num = Statics.CombineHash(num, expression.GetEquivalenceHash());
			}
			if (num == 0)
			{
				num = 2;
			}
			return _hash = num;
		}
	}
}
