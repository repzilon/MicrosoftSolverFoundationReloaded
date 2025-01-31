namespace Microsoft.SolverFoundation.Rewrite
{
	internal class BuiltinSolveSymbols
	{
		public readonly WriteMpsSymbol WriteMps;

		public readonly SolveSymbol Solve;

		public readonly BaseSolveSymbol SatSolve;

		public readonly BaseSolveSymbol FiniteSolve;

		public readonly BaseSolveSymbol SimplexSolver;

		public readonly BaseSolveSymbol Mps;

		public readonly BaseSolveSymbol GetVars;

		public readonly BaseSolveSymbol GetRows;

		public readonly BaseSolveSymbol GetGoals;

		public readonly BaseSolveSymbol GetValues;

		public readonly BaseSolveSymbol GetBounds;

		public readonly BaseSolveSymbol GetInteger;

		public readonly BaseSolveSymbol GetBasic;

		public readonly BaseSolveSymbol GetIgnoreBounds;

		public readonly BaseSolveSymbol GetIgnoreGoal;

		public readonly BaseSolveSymbol GetCoefs;

		public readonly BaseSolveSymbol GetDualValues;

		public readonly BaseSolveSymbol GetVariableRanges;

		public readonly BaseSolveSymbol GetObjectiveCoefficientRanges;

		public readonly BaseSolveSymbol SetValues;

		public readonly BaseSolveSymbol SetBounds;

		public readonly BaseSolveSymbol SetInteger;

		public readonly BaseSolveSymbol SetBasic;

		public readonly BaseSolveSymbol SetIgnoreBounds;

		public readonly BaseSolveSymbol SetIgnoreGoal;

		public readonly BaseSolveSymbol SetCoefs;

		public readonly BaseSolveSymbol GetStats;

		protected internal BuiltinSolveSymbols(SolveRewriteSystem rs)
		{
			Solve = new SolveSymbol(rs);
			SatSolve = new SatSolveSymbol(rs);
			SimplexSolver = new SimplexSolverSymbol(rs);
			WriteMps = new WriteMpsSymbol(rs);
			Mps = new MpsSymbol(rs);
			GetVars = new SsGetKeysSymbol(rs, SsGetKeysSymbol.Kind.Vars);
			GetRows = new SsGetKeysSymbol(rs, SsGetKeysSymbol.Kind.Rows);
			GetGoals = new SsGetKeysSymbol(rs, SsGetKeysSymbol.Kind.Goals);
			GetValues = new SsGetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.Values);
			GetBounds = new SsGetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.Bounds);
			GetInteger = new SsGetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.Integer);
			GetBasic = new SsGetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.Basic);
			GetIgnoreBounds = new SsGetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.IgnoreBounds);
			GetIgnoreGoal = new SsGetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.IgnoreGoal);
			GetCoefs = new GetCoefsSymbol(rs);
			GetDualValues = new SsGetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.DualValues);
			GetVariableRanges = new SsGetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.VariableRanges);
			GetObjectiveCoefficientRanges = new SsGetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.ObjectiveCoefficientRanges);
			SetValues = new SsSetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.Values);
			SetBounds = new SsSetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.Bounds);
			SetInteger = new SsSetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.Integer);
			SetBasic = new SsSetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.Basic);
			SetIgnoreBounds = new SsSetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.IgnoreBounds);
			SetIgnoreGoal = new SsSetValuesSymbol(rs, SsGetValuesSymbol.ValueKind.IgnoreGoal);
			SetCoefs = new SetCoefsSymbol(rs);
			GetStats = new GetStatsSymbol(rs);
		}
	}
}
