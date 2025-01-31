using System;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Computes the symbolic factor of a symmetric sparse matrix.
	/// </summary>
	/// <remarks> Subclasses implement different symbolic factorization algorithms, e.g. AMD, Minimum Fill.
	/// </remarks>
	internal abstract class SymbolicFactorWorkspace
	{
		/// <summary> The current set of coeffs in the workspace
		/// </summary>
		protected SparseMatrixDouble _M;

		protected double[] _colWeights;

		/// <summary> This async callback returns true if a task needs to be stopped,
		///           for example by timeout, or if an exception in one thread needs
		///           to stop all the others.
		/// </summary>
		protected Func<bool> CheckAbort;

		public FactorizationParameters Parameters { get; set; }

		/// <summary>Computes the symbolic factorization, modifying M.
		/// </summary>
		/// <returns>Symbolic factorization information, including mapping from original to permuted columns.</returns>
		public abstract SymbolicFactorResult Factorize();

		/// <summary> Create a new instance.
		/// </summary>
		/// <param name="M">The matrix.</param>
		/// <param name="colWeights">Column weights.</param>
		/// <param name="factorizationParameters">Factorization parameters</param>
		/// <param name="abort">Abort delegate.</param>
		protected SymbolicFactorWorkspace(SparseMatrixDouble M, double[] colWeights, FactorizationParameters factorizationParameters, Func<bool> abort)
		{
			_M = M;
			_colWeights = colWeights;
			CheckAbort = abort;
			Parameters = factorizationParameters;
		}
	}
}
