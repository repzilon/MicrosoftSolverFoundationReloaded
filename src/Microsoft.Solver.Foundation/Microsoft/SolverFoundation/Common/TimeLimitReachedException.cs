using System;
using System.Runtime.Serialization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Time limit has reached or user has asked to Abort
	/// </summary>
	[Serializable]
	public class TimeLimitReachedException : MsfException
	{
		/// <summary>
		/// Create the default TimeLimitReachedModelException instance with message defined in Resources.Aborted
		/// </summary>
		internal TimeLimitReachedException()
			: base(Resources.Timeout)
		{
		}

		/// <summary>
		/// Create the TimeLimitReachedException instance with the given message string
		/// </summary>
		internal TimeLimitReachedException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Create the TimeLimitReachedException instance with the given message string and inner exception
		/// </summary>
		internal TimeLimitReachedException(string message, Exception ex)
			: base(message, ex)
		{
		}

		/// <summary> Construct exception during serialization 
		/// </summary>
		protected TimeLimitReachedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
