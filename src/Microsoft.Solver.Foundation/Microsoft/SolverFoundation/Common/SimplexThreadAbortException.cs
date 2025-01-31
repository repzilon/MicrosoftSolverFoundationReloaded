using System.Runtime.Serialization;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> A worker thread has had to terminate.
	/// </summary>
	internal class SimplexThreadAbortException : MsfException
	{
		public SimplexThreadAbortException()
		{
		}

		protected SimplexThreadAbortException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
