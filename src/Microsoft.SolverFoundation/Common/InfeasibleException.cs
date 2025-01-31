using System.Runtime.Serialization;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> The problem seems to have no solution within bounds.
	/// </summary>
	internal class InfeasibleException : ModelException
	{
		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="msg"></param>
		public InfeasibleException(string msg)
			: base(msg)
		{
		}

		protected InfeasibleException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
