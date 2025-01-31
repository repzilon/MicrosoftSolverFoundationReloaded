using System;
using System.Runtime.Serialization;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> The model given has been found to contain a construction error.
	/// </summary>
	[Serializable]
	public class EmptyModelException : ModelException
	{
		/// <summary>
		/// Create the default EmptyModelException instance
		/// </summary>
		internal EmptyModelException()
		{
		}

		/// <summary>
		/// Create the EmptyModelException instance with the given message string
		/// </summary>
		internal EmptyModelException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Create the EmptyModelException instance with the given message string and inner exception
		/// </summary>
		internal EmptyModelException(string message, Exception ex)
			: base(message, ex)
		{
		}

		/// <summary> Construct exception during serialization 
		/// </summary>
		protected EmptyModelException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
