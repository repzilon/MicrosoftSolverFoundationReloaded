using System.Collections.Generic;
using System.Text;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// This is used to pretty-print OML. It handles tabbing, putting a space after ',', and having certain
	/// forms expand over multiple lines.
	/// </summary>
	internal class OmlFormatter : IExpressionFormatter
	{
		private const string Tab = "  ";

		/// <summary>
		/// An invocation which has a head in this set is expanded on multiple lines, e.g.
		/// <code>
		///   Inv[
		///     arg,
		///     arg
		///   ]
		/// </code>
		/// instead of
		/// <code>
		///   Inv[arg, arg]
		/// </code>
		/// </summary>
		private readonly HashSet<Expression> _prettyPrintExprs = new HashSet<Expression>();

		private int tabDepth;

		private SolveRewriteSystem Rewrite { get; set; }

		public OmlFormatter(SolveRewriteSystem rs)
		{
			Rewrite = rs;
			_prettyPrintExprs.Add(Rewrite.Builtin.Model);
			_prettyPrintExprs.Add(Rewrite.Builtin.Parameters);
			_prettyPrintExprs.Add(Rewrite.Builtin.Decisions);
			_prettyPrintExprs.Add(Rewrite.Builtin.Constraints);
			_prettyPrintExprs.Add(Rewrite.Builtin.Goals);
			_prettyPrintExprs.Add(Rewrite.Builtin.Maximize);
			_prettyPrintExprs.Add(Rewrite.Builtin.Minimize);
			_prettyPrintExprs.Add(Rewrite.Builtin.Foreach);
			_prettyPrintExprs.Add(Rewrite.Builtin.FilteredForeach);
			_prettyPrintExprs.Add(Rewrite.Builtin.Sum);
			_prettyPrintExprs.Add(Rewrite.Builtin.FilteredSum);
		}

		/// <summary>
		/// Called immediately after printing '[' in an invocation.
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="inv"></param>
		public void BeginInvocationArgs(StringBuilder sb, Invocation inv)
		{
			if (DoPrettyPrint(inv))
			{
				tabDepth++;
				sb.AppendLine();
			}
		}

		/// <summary>
		/// Called immediately before expanding each argument of an invocation.
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="inv"></param>
		public void BeginOneArg(StringBuilder sb, Invocation inv)
		{
			if (DoPrettyPrint(inv))
			{
				for (int i = 0; i < tabDepth; i++)
				{
					sb.Append("  ");
				}
			}
		}

		/// <summary>
		/// Called immediately after expanding each argument of an invocation, including the ',' if present.
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="last"></param>
		/// <param name="inv"></param>
		public void EndOneArg(StringBuilder sb, bool last, Invocation inv)
		{
			if (DoPrettyPrint(inv))
			{
				sb.AppendLine();
			}
			else if (!last)
			{
				sb.Append(' ');
			}
		}

		/// <summary>
		/// Called immediately before printing ']' in an invocation.
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="inv"></param>
		public void EndInvocationArgs(StringBuilder sb, Invocation inv)
		{
			if (DoPrettyPrint(inv))
			{
				tabDepth--;
				for (int i = 0; i < tabDepth; i++)
				{
					sb.Append("  ");
				}
			}
		}

		/// <summary>
		/// Called before printing the operator in a binary operator.
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="inv"></param>
		public void BeforeBinaryOperator(StringBuilder sb, Invocation inv)
		{
			sb.Append(' ');
		}

		/// <summary>
		/// Called after printing the operator in a binary operator.
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="inv"></param>
		public void AfterBinaryOperator(StringBuilder sb, Invocation inv)
		{
			sb.Append(' ');
		}

		public bool ShouldForceFullInvocationForm(Invocation inv)
		{
			if (inv.Head == inv.Rewrite.Builtin.Rule)
			{
				return false;
			}
			foreach (Expression arg in inv.Args)
			{
				if (arg is Invocation invocation && (invocation.Head == Rewrite.Builtin.Foreach || invocation.Head == Rewrite.Builtin.FilteredForeach))
				{
					return true;
				}
			}
			return false;
		}

		private bool DoPrettyPrint(Invocation inv)
		{
			if (_prettyPrintExprs.Contains(inv.Head))
			{
				return true;
			}
			if (inv.Head == Rewrite.Builtin.List && inv.Arity > 0 && inv[0].Head == Rewrite.Builtin.List)
			{
				return true;
			}
			return false;
		}
	}
}
