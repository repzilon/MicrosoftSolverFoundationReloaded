using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// Sets the coefficient of a variable in a row.
	/// Arguments are: a simplex solver, followed by any number of rules: { row, var } -&gt; coef. 
	/// </summary>
	internal class SetCoefsSymbol : BaseSolveSymbol
	{
		internal SetCoefsSymbol(SolveRewriteSystem rs)
			: base(rs, "SetCoefs")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count == 0 || !(ib[0] is SimplexSolverWrapper simplexSolverWrapper))
			{
				base.Rewrite.Log(Resources.NeedsASimplexSolverFollowedByAnyNumberOfRulesMappingRowVarToValue);
				return null;
			}
			SimplexSolver solver = simplexSolverWrapper.Solver;
			int num = 1;
			for (int i = 1; i < ib.Count; i++)
			{
				if (!(ib[i] is Invocation invocation) || invocation.Head != base.Rewrite.Builtin.Rule || invocation.Arity != 2 || invocation[0].Head != base.Rewrite.Builtin.List || invocation[0].Arity != 2 || !invocation[1].GetNumericValue(out var val) || !solver.TryGetIndexFromKey(invocation[0][0], out var vid) || !solver.TryGetIndexFromKey(invocation[0][1], out var vid2) || !SetCoef(solver, vid, vid2, val))
				{
					if (num < i)
					{
						ib[num] = ib[i];
					}
					num++;
				}
			}
			if (num > 1)
			{
				ib.RemoveRange(num, ib.Count);
				return null;
			}
			return base.Rewrite.Builtin.Null;
		}

		protected static bool SetCoef(SimplexSolver solver, int vidRow, int vidVar, Rational num)
		{
			if (!solver.IsRow(vidRow))
			{
				return false;
			}
			if (solver.IsRow(vidVar))
			{
				if (vidVar != vidRow)
				{
					return false;
				}
				if (num.IsZero)
				{
					return false;
				}
			}
			solver.SetCoefficient(vidRow, vidVar, num);
			return true;
		}
	}
}
