namespace Microsoft.SolverFoundation.Common
{
	/// <summary>Factorization parameters.
	/// </summary>
	internal class FactorizationParameters
	{
		/// <summary>
		/// If the matrix becomes dense we can switch algorithms in some places for more speed,
		///    a tradeoff against space.  This portion of the matrix is the "dense window".
		///    The start of the dense window is the first column that has at least this
		///    percentage of nonzeroes.
		/// </summary>
		public double DenseWindowThreshhold { get; set; }

		/// <summary>Factorization method.
		/// </summary>
		public SymbolicFactorizationMethod FactorizationMethod { get; set; }

		/// <summary>Factorization method.
		/// </summary>
		public bool AllowNormal { get; set; }

		public FactorizationParameters()
			: this(SymbolicFactorizationMethod.Automatic, 0.8)
		{
		}

		public FactorizationParameters(SymbolicFactorizationMethod method)
			: this(method, 0.8)
		{
		}

		public FactorizationParameters(SymbolicFactorizationMethod method, double dense)
		{
			DenseWindowThreshhold = dense;
			FactorizationMethod = method;
			AllowNormal = true;
		}
	}
}
