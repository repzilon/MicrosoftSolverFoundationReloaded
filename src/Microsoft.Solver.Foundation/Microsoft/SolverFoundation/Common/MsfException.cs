using System;
using System.Runtime.Serialization;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Base exception class for Microsoft Solver Foundation exceptions
	/// </summary>
	[Serializable]
	public class MsfException : Exception
	{
		/// <summary>
		/// Create the default MSF exception instance
		/// </summary>
		public MsfException()
		{
		}

		/// <summary>
		/// Create the MSF exception instance with given string message
		/// </summary>
		public MsfException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Create the MSF exception instance with given string message and an inner exception instance
		/// </summary>
		public MsfException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary> Construct exception during serialization 
		/// </summary>
		protected MsfException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
