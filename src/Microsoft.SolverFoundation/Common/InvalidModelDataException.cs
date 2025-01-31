using System;
using System.Runtime.Serialization;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> The data given has been found to be invalid for the model.
	/// </summary>
	[Serializable]
	public class InvalidModelDataException : MsfException
	{
		/// <summary>
		/// Create the default InvalidModelDataException instance
		/// </summary>
		internal InvalidModelDataException()
		{
		}

		/// <summary>
		/// Create the InvalidModelDataException instance with the given message string
		/// </summary>
		internal InvalidModelDataException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Create the InvalidModelDataException instance with given string message and an inner exception instance
		/// </summary>
		internal InvalidModelDataException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary> Construct exception during serialization 
		/// </summary>
		protected InvalidModelDataException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
