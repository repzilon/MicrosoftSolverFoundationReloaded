using System;
using System.Globalization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Lexical tokens
	/// </summary>
	internal abstract class Token
	{
		private TokKind _tid;

		private TextSpan _span;

		/// <summary> ID of token
		/// </summary>
		public TokKind Kind => _tid;

		/// <summary> test or set Nested
		/// </summary>
		public virtual bool Nested
		{
			get
			{
				return false;
			}
			internal set
			{
				throw new InvalidOperationException();
			}
		}

		/// <summary> get the span
		/// </summary>
		public TextSpan Span => _span;

		/// <summary> construct
		/// </summary>
		/// <param name="tid"></param>
		public Token(TokKind tid)
		{
			_tid = tid;
		}

		/// <summary> constructor
		/// </summary>
		/// <param name="tid"></param>
		/// <param name="span"></param>
		public Token(TokKind tid, TextSpan span)
		{
			_tid = tid;
			_span = span;
		}

		/// <summary> set extents
		/// </summary>
		/// <param name="span"></param>
		/// <returns></returns>
		public Token SetExtents(TextSpan span)
		{
			_span = span;
			return this;
		}

		/// <summary> Get Span
		/// </summary>
		/// <param name="tvr"></param>
		/// <param name="span"></param>
		/// <returns></returns>
		public bool GetSpan(ITextVersion tvr, out TextSpan span)
		{
			if (!tvr.MapSpan(ref _span))
			{
				span = default(TextSpan);
				return false;
			}
			span = _span;
			return true;
		}

		/// <summary> Test if before
		/// </summary>
		/// <param name="tokAfter"></param>
		/// <returns></returns>
		public bool ImmediatelyBefore(Token tokAfter)
		{
			if (!TextSpan.MapToSame(ref _span, ref tokAfter._span))
			{
				return false;
			}
			return _span.Lim == tokAfter._span.Min;
		}

		/// <summary> token comparison
		/// </summary>
		/// <param name="tok1"></param>
		/// <param name="tok2"></param>
		/// <returns></returns>
		public static bool operator <(Token tok1, Token tok2)
		{
			if (!TextSpan.MapToSame(ref tok1._span, ref tok2._span))
			{
				throw new InvalidOperationException(Resources.TokensNotInSameVersion);
			}
			return tok1._span.Lim < tok2._span.Lim;
		}

		/// <summary> token comparison
		/// </summary>
		/// <param name="tok1"></param>
		/// <param name="tok2"></param>
		/// <returns></returns>
		public static bool operator >(Token tok1, Token tok2)
		{
			if (!TextSpan.MapToSame(ref tok1._span, ref tok2._span))
			{
				throw new InvalidOperationException(Resources.TokensNotInSameVersion);
			}
			return tok1._span.Lim > tok2._span.Lim;
		}

		/// <summary> coerce to T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T As<T>() where T : Token
		{
			return (T)this;
		}

		/// <summary> Convert to string representation
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Kind.ToString();
		}

		/// <summary> formatted string representation
		/// </summary>
		/// <returns></returns>
		public virtual string DumpString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}: {1}", new object[2]
			{
				Kind,
				ToString()
			});
		}
	}
}
