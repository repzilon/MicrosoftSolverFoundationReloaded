using System.Runtime.Serialization;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Internal exception that is thrown whenever CQN terminates without an optimal result.
	/// </summary>
	internal class CompactQuasiNewtonException : MsfException
	{
		private readonly CompactQuasiNewtonErrorType _error;

		public CompactQuasiNewtonErrorType Error => _error;

		/// <summary>
		/// Initiate exception with enum for the type of error
		/// </summary>
		/// <param name="error">which error occured</param>
		public CompactQuasiNewtonException(CompactQuasiNewtonErrorType error)
		{
			_error = error;
		}

		protected CompactQuasiNewtonException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
