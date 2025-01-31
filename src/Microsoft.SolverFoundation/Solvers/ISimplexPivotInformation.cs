using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Simplex pivoting information
	/// </summary>
	public interface ISimplexPivotInformation
	{
		/// <summary> Whether this is a double based pivot (vs Rational based).
		/// </summary>
		bool IsDouble { get; }

		/// <summary> The entering variable index.
		/// </summary>
		int VarEnter { get; }

		/// <summary> The state of the variable before entering.
		/// </summary>
		SimplexVarValKind VvkEnter { get; }

		/// <summary> The leaving variable index.
		/// </summary>
		int VarLeave { get; }

		/// <summary> The state of the leaving variable after leaving.
		/// </summary>
		SimplexVarValKind VvkLeave { get; }

		/// <summary> How much the entering variable is changing by (absolute value).
		/// Zero (or close to it) indicates a degenerate pivot.
		/// </summary>
		Rational Scale { get; }

		/// <summary> Indicates whether the entering variable is increasing or decreasing.
		/// </summary>
		int Sign { get; }

		/// <summary> The determinant of this transformation. This is the "pivot" value, or diagonal
		/// entry of the eta matrix. The determinant of the new basis is this value times the
		/// determinant of the old basis.
		/// </summary>
		Rational Determinant { get; }

		/// <summary> An approximation of the reduced cost. There are no guarantees on accuracy.
		/// </summary>
		double ApproxCost { get; }
	}
}
