using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// Sets the coefficient of a variable in a row.
	/// Arguments are: a simplex solver, followed by any number of rules: { row, var } -&gt; coef. 
	/// </summary>
	internal class GetCoefsSymbol : BaseSolveSymbol
	{
		internal GetCoefsSymbol(SolveRewriteSystem rs)
			: base(rs, "GetCoefs")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count == 0 || !(ib[0] is SimplexSolverWrapper simplexSolverWrapper))
			{
				base.Rewrite.Log(Resources.NeedsASimplexSolverOptionallyFollowedByLabelVarPairs, Name);
				return null;
			}
			List<Expression> list = new List<Expression>();
			SimplexSolver solver = simplexSolverWrapper.Solver;
			if (ib.Count == 1)
			{
				AppendCoefs(list, solver, base.Rewrite.Builtin.Null, base.Rewrite.Builtin.Null);
			}
			else
			{
				for (int i = 1; i < ib.Count; i++)
				{
					if (!(ib[i] is Invocation invocation) || invocation.Head != base.Rewrite.Builtin.List || invocation.Arity != 2)
					{
						return null;
					}
					if (!AppendCoefs(list, solver, invocation[0], invocation[1]))
					{
						return null;
					}
				}
			}
			return base.Rewrite.Builtin.List.Invoke(fCanOwnArray: true, list.ToArray());
		}

		protected bool AppendCoefs(List<Expression> rgexpr, SimplexSolver solver, Expression keyRow, Expression keyVar)
		{
			if (keyRow == base.Rewrite.Builtin.Null)
			{
				if (keyVar != base.Rewrite.Builtin.Null)
				{
					return AppendVarCoefs(rgexpr, solver, keyVar);
				}
				foreach (Expression rowKey in solver.RowKeys)
				{
					AppendRowCoefs(rgexpr, solver, rowKey);
				}
				return true;
			}
			if (keyVar == base.Rewrite.Builtin.Null)
			{
				return AppendRowCoefs(rgexpr, solver, keyRow);
			}
			if (!solver.TryGetIndexFromKey(keyRow, out var vid) || !solver.TryGetIndexFromKey(keyVar, out var vid2))
			{
				return false;
			}
			rgexpr.Add(base.Rewrite.Builtin.Rule.Invoke(base.Rewrite.Builtin.List.Invoke(keyRow, keyVar), RationalConstant.Create(base.Rewrite, solver.GetCoefficient(vid, vid2))));
			return true;
		}

		protected bool AppendRowCoefs(List<Expression> rgexpr, SimplexSolver solver, Expression key)
		{
			if (!solver.TryGetIndexFromKey(key, out var vid) || !solver.IsRow(vid))
			{
				return false;
			}
			foreach (LinearEntry rowEntry in solver.GetRowEntries(vid))
			{
				rgexpr.Add(base.Rewrite.Builtin.Rule.Invoke(base.Rewrite.Builtin.List.Invoke(key, (Expression)rowEntry.Key), RationalConstant.Create(base.Rewrite, rowEntry.Value)));
			}
			return true;
		}

		protected bool AppendVarCoefs(List<Expression> rgexpr, SimplexSolver solver, Expression key)
		{
			if (!solver.TryGetIndexFromKey(key, out var vid))
			{
				return false;
			}
			foreach (LinearEntry variableEntry in solver.GetVariableEntries(vid))
			{
				rgexpr.Add(base.Rewrite.Builtin.Rule.Invoke(base.Rewrite.Builtin.List.Invoke((Expression)variableEntry.Key, key), RationalConstant.Create(base.Rewrite, variableEntry.Value)));
			}
			return true;
		}
	}
}
