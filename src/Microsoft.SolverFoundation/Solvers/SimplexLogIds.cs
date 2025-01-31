using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Identify the various kinds of Logger messages
	/// </summary>
	internal static class SimplexLogIds
	{
		/// <summary> Verify computation of basic variable values
		/// </summary>
		public const int VerifyBasicValues = 0;

		/// <summary> Verify solving against current basis
		/// </summary>
		public const int VerifyBasisSolve = 1;

		/// <summary> Verify computation of exact reduced costs
		/// </summary>
		public const int VerifyReducedCostsExact = 2;

		/// <summary> Verify computation of double reduced costs
		/// </summary>
		public const int VerifyReducedCostsDouble = 3;

		/// <summary> The basis is singular
		/// </summary>
		public const int ErrorBasis = 4;

		/// <summary> Excessive error caused us to refactor the basis matrix
		/// </summary>
		public const int ErrorRefactor = 5;

		/// <summary> A solve was restarted due to error, either a singular basis or wandering to infeasible
		/// </summary>
		public const int ErrorRestart = 6;

		/// <summary> Excessive error caused us to patch some numeric values
		/// </summary>
		public const int ErrorPatch = 7;

		/// <summary> Log each pivot
		/// </summary>
		public const int Pivots = 8;

		/// <summary> Check for cycles
		/// </summary>
		public const int Cycles = 9;

		/// <summary> Log solution registration
		/// </summary>
		public const int Solution = 10;

		/// <summary> Log branching decisions when solving MIPs
		/// </summary>
		public const int Branching = 11;

		/// <summary> Log integer solution improvements when solving MIPs
		/// </summary>
		public const int SolutionImprovement = 12;

		/// <summary> Log information about presolve
		/// </summary>
		public const int Presolve = 13;

		/// <summary> Log information about cutting planes
		/// </summary>
		public const int Cuts = 14;

		/// <summary> Interior Point Method general
		/// </summary>
		public const int Ipm = 15;

		/// <summary> Predictor step in a predictor-corrector algorithm
		/// </summary>
		public const int IpmPredictor = 16;

		/// <summary> Corrector step in a predictor-corrector algorithm
		/// </summary>
		public const int IpmCorrector = 17;

		public static readonly LogIdSet DefaultDebugSet = VerifySet | (ErrorSet + 9 + 10);

		public static readonly LogIdSet VerifySet = new LogIdSet(0, 1, 2, 3);

		public static readonly LogIdSet ErrorSet = new LogIdSet(4, 5, 6, 7);
	}
}
