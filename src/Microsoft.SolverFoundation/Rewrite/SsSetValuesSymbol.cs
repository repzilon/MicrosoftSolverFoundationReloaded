using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// Sets the value/bounds of variables.
	/// Arguments are: a simplex solver, a series of rules mapping keys to values/bounds. 
	/// If a variable key is not correct, the entry is not reduced.
	/// </summary>
	internal class SsSetValuesSymbol : SsGetValuesSymbol
	{
		internal SsSetValuesSymbol(SolveRewriteSystem rs, ValueKind kind)
			: base(rs, GetName(kind), kind)
		{
		}

		private static string GetName(ValueKind kind)
		{
			switch (kind)
			{
			case ValueKind.Values:
				return "SetValues";
			case ValueKind.Bounds:
				return "SetBounds";
			case ValueKind.Basic:
				return "SetBasic";
			case ValueKind.Integer:
				return "SetInteger";
			case ValueKind.IgnoreBounds:
				return "SetIgnoreBounds";
			case ValueKind.IgnoreGoal:
				return "SetIgnoreGoal";
			default:
				throw new InvalidOperationException(Resources.BadValueKind);
			}
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count == 0 || !(ib[0] is SimplexSolverWrapper simplexSolverWrapper))
			{
				base.Rewrite.Log(Resources.NeedsASimplexSolverFollowedByAnyNumberOfRulesMappingVariablesLabelsToValues, Name);
				return null;
			}
			SimplexSolver solver = simplexSolverWrapper.Solver;
			int num = 1;
			for (int i = 1; i < ib.Count; i++)
			{
				if (!(ib[i] is Invocation invocation) || invocation.Head != base.Rewrite.Builtin.Rule || invocation.Arity != 2 || !solver.TryGetIndexFromKey(invocation[0], out var vid) || !SetValue(solver, vid, invocation[1]))
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

		protected bool SetValue(SimplexSolver solver, int id, Expression expr)
		{
			Rational val2;
			bool val;
			switch (_kind)
			{
			case ValueKind.Values:
				if (!expr.GetNumericValue(out val2))
				{
					return false;
				}
				solver.SetValue(id, val2);
				return true;
			case ValueKind.Bounds:
			{
				if (expr.Head != base.Rewrite.Builtin.List || expr.Arity != 2 || !expr[0].GetNumericValue(out val2) || !expr[1].GetNumericValue(out var val3))
				{
					return false;
				}
				solver.SetBounds(id, val2, val3);
				return true;
			}
			case ValueKind.Basic:
				if (!expr.GetValue(out val))
				{
					return false;
				}
				solver.SetBasic(id, val);
				return true;
			case ValueKind.Integer:
				if (!expr.GetValue(out val))
				{
					return false;
				}
				solver.SetIntegrality(id, val);
				return true;
			case ValueKind.IgnoreBounds:
				if (!expr.GetValue(out val))
				{
					return false;
				}
				solver.SetIgnoreBounds(id, val);
				return true;
			case ValueKind.IgnoreGoal:
			{
				if (!expr.GetValue(out val))
				{
					return false;
				}
				if (solver.IsGoal(id, out var goal))
				{
					goal.Enabled = !val;
				}
				return true;
			}
			default:
				throw new InvalidOperationException(Resources.BadValueKind);
			}
		}
	}
}
