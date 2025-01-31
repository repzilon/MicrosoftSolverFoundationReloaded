using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// Gets the values/bounds of the variables/rows.
	/// Arguments are: a simplex solver, and optional keys.
	/// If no keys are specified, the values of all variables/rows are returned.
	/// Otherwise only the values of the specified variables/rows are returned.
	/// If a key is invalid, the expression is not reduced.
	/// The result is a list of rules with first argument the key and second argument the value.
	/// </summary>
	internal class SsGetValuesSymbol : BaseSolveSymbol
	{
		internal enum ValueKind
		{
			Values,
			Bounds,
			Basic,
			Integer,
			IgnoreBounds,
			IgnoreGoal,
			DualValues,
			VariableRanges,
			ObjectiveCoefficientRanges
		}

		protected ValueKind _kind;

		internal SsGetValuesSymbol(SolveRewriteSystem rs, ValueKind kind)
			: this(rs, GetName(kind), kind)
		{
		}

		protected SsGetValuesSymbol(SolveRewriteSystem rs, string name, ValueKind kind)
			: base(rs, name)
		{
			_kind = kind;
		}

		private static string GetName(ValueKind kind)
		{
			switch (kind)
			{
			case ValueKind.Values:
				return "GetValues";
			case ValueKind.Bounds:
				return "GetBounds";
			case ValueKind.Basic:
				return "GetBasic";
			case ValueKind.Integer:
				return "GetInteger";
			case ValueKind.IgnoreBounds:
				return "GetIgnoreBounds";
			case ValueKind.IgnoreGoal:
				return "GetIgnoreGoal";
			case ValueKind.DualValues:
				return "GetDualValues";
			case ValueKind.VariableRanges:
				return "GetVariableRanges";
			case ValueKind.ObjectiveCoefficientRanges:
				return "GetObjectiveCoefficientRanges";
			default:
				throw new InvalidOperationException(Resources.BadValueKind);
			}
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count == 0 || !(ib[0] is LinearSolverWrapper linearSolverWrapper))
			{
				base.Rewrite.Log(Resources.NeedsASimplexSolverOptionallyFollowedByVariablesLabels, Name);
				return null;
			}
			List<Expression> list = new List<Expression>();
			LinearModel model = linearSolverWrapper.Model;
			SimplexSensitivity simplexSensitivity = null;
			if (_kind == ValueKind.DualValues || _kind == ValueKind.ObjectiveCoefficientRanges || _kind == ValueKind.VariableRanges)
			{
				if (!(model is SimplexSolver simplexSolver))
				{
					base.Rewrite.Log(Resources.NeedsASolverWithSensitivityInformation, Name);
					return null;
				}
				simplexSensitivity = simplexSolver.GetReport(LinearSolverReportType.Sensitivity) as SimplexSensitivity;
				if (simplexSensitivity == null)
				{
					base.Rewrite.Log(Resources.NeedsASolverWithSensitivityInformation, Name);
					return null;
				}
			}
			if (ib.Count > 1)
			{
				for (int i = 1; i < ib.Count; i++)
				{
					if (!AddItem(ib[i], list, model, simplexSensitivity))
					{
						return null;
					}
				}
			}
			else if (_kind != ValueKind.IgnoreGoal)
			{
				foreach (Expression key in model.Keys)
				{
					int indexFromKey = model.GetIndexFromKey(key);
					Expression value = GetValue(model, simplexSensitivity, indexFromKey);
					if (value != null)
					{
						list.Add(base.Rewrite.Builtin.Rule.Invoke(key, value));
					}
				}
			}
			else
			{
				foreach (ILinearGoal goal in model.Goals)
				{
					list.Add(base.Rewrite.Builtin.Rule.Invoke((Expression)goal.Key, base.Rewrite.Builtin.Boolean.Get(!goal.Enabled)));
				}
			}
			return base.Rewrite.Builtin.List.Invoke(fCanOwnArray: true, list.ToArray());
		}

		protected bool AddItem(Expression expr, List<Expression> rgexpr, LinearModel solver, SimplexSensitivity sensitivity)
		{
			if (expr is ExprSequence exprSequence)
			{
				foreach (Expression item in exprSequence)
				{
					if (!AddItem(item, rgexpr, solver, sensitivity))
					{
						return false;
					}
				}
			}
			else
			{
				if (!solver.TryGetIndexFromKey(expr, out var vid))
				{
					return false;
				}
				Expression value = GetValue(solver, sensitivity, vid);
				if (value != null)
				{
					rgexpr.Add(base.Rewrite.Builtin.Rule.Invoke(expr, value));
				}
			}
			return true;
		}

		protected Expression GetValue(LinearModel solver, SimplexSensitivity sensitivity, int id)
		{
			switch (_kind)
			{
			case ValueKind.Values:
				return RationalConstant.Create(base.Rewrite, solver.GetValue(id));
			case ValueKind.Bounds:
			{
				solver.GetBounds(id, out var lower, out var upper);
				return base.Rewrite.Builtin.List.Invoke(RationalConstant.Create(base.Rewrite, lower), RationalConstant.Create(base.Rewrite, upper));
			}
			case ValueKind.Basic:
				return base.Rewrite.Builtin.Boolean.Get(solver.GetBasic(id));
			case ValueKind.Integer:
				return base.Rewrite.Builtin.Boolean.Get(solver.GetIntegrality(id));
			case ValueKind.IgnoreBounds:
				return base.Rewrite.Builtin.Boolean.Get(solver.GetIgnoreBounds(id));
			case ValueKind.IgnoreGoal:
			{
				ILinearGoal goal;
				return base.Rewrite.Builtin.Boolean.Get(solver.IsGoal(id, out goal) && !goal.Enabled);
			}
			case ValueKind.DualValues:
			{
				if (!solver.IsRow(id))
				{
					return null;
				}
				Rational dualValue = sensitivity.GetDualValue(id);
				base.Rewrite.CheckAbort();
				return RationalConstant.Create(base.Rewrite, dualValue);
			}
			case ValueKind.VariableRanges:
			{
				if (!solver.IsRow(id) || solver.IsGoal(id))
				{
					return null;
				}
				LinearSolverSensitivityRange objectiveCoefficientRange = sensitivity.GetVariableRange(id);
				base.Rewrite.CheckAbort();
				return base.Rewrite.Builtin.List.Invoke(RationalConstant.Create(base.Rewrite, objectiveCoefficientRange.Current), RationalConstant.Create(base.Rewrite, objectiveCoefficientRange.Lower), RationalConstant.Create(base.Rewrite, objectiveCoefficientRange.Upper));
			}
			case ValueKind.ObjectiveCoefficientRanges:
			{
				if (solver.IsRow(id))
				{
					return null;
				}
				LinearSolverSensitivityRange objectiveCoefficientRange = sensitivity.GetObjectiveCoefficientRange(id);
				base.Rewrite.CheckAbort();
				return base.Rewrite.Builtin.List.Invoke(RationalConstant.Create(base.Rewrite, objectiveCoefficientRange.Current), RationalConstant.Create(base.Rewrite, objectiveCoefficientRange.Lower), RationalConstant.Create(base.Rewrite, objectiveCoefficientRange.Upper));
			}
			default:
				throw new InvalidOperationException(Resources.BadValueKind);
			}
		}
	}
}
