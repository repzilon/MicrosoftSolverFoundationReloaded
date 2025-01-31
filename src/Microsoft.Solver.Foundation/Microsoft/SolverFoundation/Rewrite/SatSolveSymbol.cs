using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class SatSolveSymbol : BaseSolveSymbol
	{
		public SatSolveSymbol(SolveRewriteSystem rs)
			: base(rs, "SatSolve")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2)
			{
				return null;
			}
			Dictionary<Expression, int> dictionary = new Dictionary<Expression, int>(ExpressionComparer.Instance);
			if (!AddVars(dictionary, ib[0]))
			{
				return null;
			}
			IEnumerable<SatSolution> rgsln;
			try
			{
				rgsln = SatSolver.Solve(new SatSolverParams(), dictionary.Count, GetClauses(dictionary, ib[1]));
			}
			catch (ModelClauseException)
			{
				return null;
			}
			return new ExprSequenceEnumerable(base.Rewrite, GetSolutionExprs(dictionary, rgsln));
		}

		private bool AddVars(Dictionary<Expression, int> mpvarvid, Expression exprRoot)
		{
			if (exprRoot is ExprSequence exprSequence)
			{
				foreach (Expression item in exprSequence)
				{
					if (!AddVars(mpvarvid, item))
					{
						return false;
					}
				}
			}
			else if (exprRoot.Head == base.Rewrite.Builtin.List)
			{
				for (int i = 0; i < exprRoot.Arity; i++)
				{
					if (!AddVars(mpvarvid, exprRoot[i]))
					{
						return false;
					}
				}
			}
			else
			{
				if (mpvarvid.TryGetValue(exprRoot, out var _))
				{
					return false;
				}
				Symbol firstSymbolHead = exprRoot.FirstSymbolHead;
				if (firstSymbolHead.HasAttribute(base.Rewrite.Attributes.ValuesLocked))
				{
					return false;
				}
				mpvarvid.Add(exprRoot, mpvarvid.Count);
			}
			return true;
		}

		private IEnumerable<Literal[]> GetClauses(Dictionary<Expression, int> mpvarvid, Expression exprRoot)
		{
			Literal[] rglit = null;
			foreach (Expression con in FlattenExprs(exprRoot))
			{
				if (GetLiteral(mpvarvid, con, out var lit))
				{
					if (rglit == null || rglit.Length != 1)
					{
						rglit = new Literal[1];
					}
					rglit[0] = lit;
				}
				else
				{
					if (con.Head != base.Rewrite.Builtin.Or)
					{
						throw new ModelClauseException(Resources.BadConstraint, con);
					}
					if (con.Arity == 0)
					{
						continue;
					}
					if (rglit == null || rglit.Length != con.Arity)
					{
						rglit = new Literal[con.Arity];
					}
					for (int i = 0; i < con.Arity; i++)
					{
						Expression expr = con[i];
						if (!GetLiteral(mpvarvid, expr, out rglit[i]))
						{
							throw new ModelClauseException(Resources.BadConstraint, expr);
						}
					}
				}
				yield return rglit;
			}
		}

		private bool GetLiteral(Dictionary<Expression, int> mpvarvid, Expression expr, out Literal lit)
		{
			if (mpvarvid.TryGetValue(expr, out var value))
			{
				lit = new Literal(value, fSense: true);
				return true;
			}
			if (expr.Head == base.Rewrite.Builtin.Not && expr.Arity == 1 && mpvarvid.TryGetValue(expr[0], out value))
			{
				lit = new Literal(value, fSense: false);
				return true;
			}
			lit = default(Literal);
			return false;
		}

		private IEnumerable<Expression> FlattenExprs(Expression exprRoot)
		{
			if (GetEnumerable(exprRoot, out var rgexprTmp))
			{
				foreach (Expression item in FlattenExprs(rgexprTmp))
				{
					yield return item;
				}
				yield break;
			}
			yield return exprRoot;
		}

		private IEnumerable<Expression> FlattenExprs(IEnumerable<Expression> rgexpr)
		{
			foreach (Expression expr in rgexpr)
			{
				if (GetEnumerable(expr, out var rgexprTmp))
				{
					foreach (Expression item in FlattenExprs(rgexprTmp))
					{
						yield return item;
					}
				}
				else
				{
					yield return expr;
				}
			}
		}

		private bool GetEnumerable(Expression expr, out IEnumerable<Expression> rgexpr)
		{
			rgexpr = expr as ExprSequence;
			if (rgexpr != null)
			{
				return true;
			}
			if (expr.Head != base.Rewrite.Builtin.List)
			{
				return false;
			}
			rgexpr = ((Invocation)expr).Args;
			return true;
		}

		private IEnumerable<Expression> GetSolutionExprs(Dictionary<Expression, int> mpvarvid, IEnumerable<SatSolution> rgsln)
		{
			Expression[] rgvar = new Expression[mpvarvid.Count];
			foreach (KeyValuePair<Expression, int> item in mpvarvid)
			{
				rgvar[item.Value] = item.Key;
			}
			foreach (SatSolution sln in rgsln)
			{
				Expression[] rgexpr = new Expression[mpvarvid.Count];
				foreach (Literal literal in sln.Literals)
				{
					int var = literal.Var;
					rgexpr[var] = base.Rewrite.Builtin.Rule.Invoke(rgvar[var], base.Rewrite.Builtin.Boolean.Get(literal.Sense));
				}
				for (int i = 0; i < rgexpr.Length; i++)
				{
					if (rgexpr[i] == null)
					{
						rgexpr[i] = base.Rewrite.Builtin.Rule.Invoke(rgvar[i], base.Rewrite.Builtin.Null);
					}
				}
				yield return base.Rewrite.Builtin.List.Invoke(rgexpr);
			}
		}
	}
}
