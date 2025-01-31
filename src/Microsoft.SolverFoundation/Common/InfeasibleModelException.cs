using System;
using System.Runtime.Serialization;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> The model given has been found to contain a construction error.
	/// </summary>
	[Serializable]
	public class InfeasibleModelException : MsfException
	{
		/// <summary>
		/// Create the default InfeasibleModelException instance
		/// </summary>
		internal InfeasibleModelException()
		{
		}

		/// <summary>
		/// Create the InfeasibleModelException instance with the given message string
		/// </summary>
		internal InfeasibleModelException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Create the InfeasibleModelException instance with the given message string and inner exception
		/// </summary>
		internal InfeasibleModelException(string message, Exception ex)
			: base(message, ex)
		{
		}

		/// <summary> Construct exception during serialization 
		/// </summary>
		protected InfeasibleModelException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
