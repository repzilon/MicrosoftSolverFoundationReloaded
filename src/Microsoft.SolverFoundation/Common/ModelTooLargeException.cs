using System;
using System.Runtime.Serialization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Time limit has reached or user has asked to Abort
	/// </summary>
	[Serializable]
	public class ModelTooLargeException : MsfException
	{
		/// <summary>
		/// Create the default ModelTooLargeException instance with message defined in Resources.ModelTooLarge
		/// </summary>
		internal ModelTooLargeException()
			: base(Resources.ModelTooLarge)
		{
		}

		/// <summary>
		/// Create the ModelTooLargeException instance with the given message string
		/// </summary>
		internal ModelTooLargeException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Create the ModelTooLargeException instance with the given message string and inner exception
		/// </summary>
		internal ModelTooLargeException(string message, Exception ex)
			: base(message, ex)
		{
		}

		/// <summary> Construct exception during serialization 
		/// </summary>
		protected ModelTooLargeException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
