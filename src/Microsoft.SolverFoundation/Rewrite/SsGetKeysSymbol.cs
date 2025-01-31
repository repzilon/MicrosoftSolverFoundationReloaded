using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// Gets the variable/row/goal keys.
	/// Arguments are: a simplex solver. 
	/// The result is a sequence of keys.
	/// </summary>
	internal class SsGetKeysSymbol : BaseSolveSymbol
	{
		public enum Kind
		{
			Vars,
			Rows,
			Goals
		}

		protected Kind _kind;

		internal SsGetKeysSymbol(SolveRewriteSystem rs, Kind kind)
			: base(rs, GetName(kind))
		{
			_kind = kind;
		}

		protected static string GetName(Kind kind)
		{
			switch (kind)
			{
			case Kind.Vars:
				return "GetVars";
			case Kind.Rows:
				return "GetRows";
			case Kind.Goals:
				return "GetGoals";
			default:
				throw new InvalidOperationException(Resources.BadKind);
			}
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1 || !(ib[0] is SimplexSolverWrapper simplexSolverWrapper))
			{
				base.Rewrite.Log(Resources.NeedsASimplexSolver, Name);
				return null;
			}
			IEnumerable<Expression> rgexpr;
			switch (_kind)
			{
			default:
				rgexpr = Statics.ExplicitCastIter<object, Expression>(simplexSolverWrapper.Solver.VariableKeys);
				break;
			case Kind.Rows:
				rgexpr = Statics.ExplicitCastIter<object, Expression>(simplexSolverWrapper.Solver.RowKeys);
				break;
			case Kind.Goals:
				rgexpr = GetGoalKeys(simplexSolverWrapper.Solver);
				break;
			}
			return new ExprSequenceEnumerable(base.Rewrite, rgexpr);
		}

		protected static IEnumerable<Expression> GetGoalKeys(ILinearModel mod)
		{
			foreach (ILinearGoal goal in mod.Goals)
			{
				yield return (Expression)goal.Key;
			}
		}
	}
}
