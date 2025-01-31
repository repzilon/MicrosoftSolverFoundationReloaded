using System;
using System.Runtime.Serialization;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> The model given has been found to contain a construction error.
	/// </summary>
	[Serializable]
	public class ModelException : MsfException
	{
		/// <summary>
		/// Create the default ModelException instance
		/// </summary>
		internal ModelException()
		{
		}

		/// <summary>
		/// Create the ModelException instance with the given message string
		/// </summary>
		internal ModelException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Create the ModelException instance with the given message string and inner exception
		/// </summary>
		internal ModelException(string message, Exception ex)
			: base(message, ex)
		{
		}

		/// <summary> Construct exception during serialization 
		/// </summary>
		protected ModelException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
