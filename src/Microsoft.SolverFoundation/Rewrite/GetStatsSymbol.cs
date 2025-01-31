using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class GetStatsSymbol : BaseSolveSymbol
	{
		private Dictionary<string, Func<SimplexSolver, Expression>> _mpstrfn;

		internal GetStatsSymbol(SolveRewriteSystem rs)
			: base(rs, "GetStats")
		{
			_mpstrfn = new Dictionary<string, Func<SimplexSolver, Expression>>();
			Dictionary<string, Func<SimplexSolver, Expression>> mpstrfn = _mpstrfn;
			Func<SimplexSolver, Expression> value = (SimplexSolver solver) => GetExpr(solver.Result.ToString());
			mpstrfn.Add("Result", value);
			_mpstrfn.Add("Pivots", (SimplexSolver solver) => GetExpr(solver.PivotCount));
			_mpstrfn.Add("DegeneratePivots", (SimplexSolver solver) => GetExpr(solver.PivotCountDegenerate));
			_mpstrfn.Add("RationalPivots", (SimplexSolver solver) => GetExpr(solver.PivotCountExact));
			_mpstrfn.Add("FloatPivots", (SimplexSolver solver) => GetExpr(solver.PivotCountDouble));
			_mpstrfn.Add("Branches", (SimplexSolver solver) => GetExpr(solver.BranchCount));
		}

		protected Expression GetExpr(int n)
		{
			return new IntegerConstant(base.Rewrite, n);
		}

		protected Expression GetExpr(string str)
		{
			return new StringConstant(base.Rewrite, str);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count == 0 || !(ib[0] is SimplexSolverWrapper simplexSolverWrapper))
			{
				base.Rewrite.Log(Resources.NeedsASimplexSolverOptionallyFollowedByStatisticNames, Name);
				return null;
			}
			List<Expression> list = new List<Expression>();
			SimplexSolver solver = simplexSolverWrapper.Solver;
			if (ib.Count == 1)
			{
				foreach (KeyValuePair<string, Func<SimplexSolver, Expression>> item in _mpstrfn)
				{
					list.Add(base.Rewrite.Builtin.Rule.Invoke(new StringConstant(base.Rewrite, item.Key), item.Value(solver)));
				}
			}
			else
			{
				for (int i = 1; i < ib.Count; i++)
				{
					if (!ib[i].GetValue(out string val) || !_mpstrfn.TryGetValue(val, out var value))
					{
						return null;
					}
					list.Add(base.Rewrite.Builtin.Rule.Invoke(ib[i], value(solver)));
				}
			}
			return base.Rewrite.Builtin.List.Invoke(fCanOwnArray: true, list.ToArray());
		}
	}
}
