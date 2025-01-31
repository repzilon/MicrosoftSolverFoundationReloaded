using System;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>Indicates that an invalid property name was specified in an event handler.
	/// </summary>
	[Serializable]
	public class InvalidSolverPropertyException : MsfException
	{
		/// <summary>The reason why the property is invalid.
		/// </summary>
		public InvalidSolverPropertyReason Reason { get; internal set; }

		/// <summary>
		/// Create the default instance
		/// </summary>
		public InvalidSolverPropertyException()
			: this(string.Format(CultureInfo.InvariantCulture, Resources.PropertyNameIsNotSupported0, new object[1] { string.Empty }), InvalidSolverPropertyReason.InvalidPropertyName)
		{
		}

		/// <summary>
		/// Create the default instance with the specified message.
		/// </summary>
		public InvalidSolverPropertyException(string message)
			: this(message, InvalidSolverPropertyReason.InvalidPropertyName)
		{
		}

		/// <summary>
		/// Create the InvalidSolverPropertyException instance with the given message string.
		/// </summary>
		public InvalidSolverPropertyException(string message, InvalidSolverPropertyReason reason)
			: base(message)
		{
			Reason = reason;
		}

		/// <summary>
		/// Create the InvalidSolverPropertyException instance with the given message string and inner exception.
		/// </summary>
		public InvalidSolverPropertyException(string message, Exception ex)
			: this(message, ex, InvalidSolverPropertyReason.InvalidPropertyName)
		{
		}

		/// <summary>
		/// Create the InvalidSolverPropertyException instance with the given message string and inner exception.
		/// </summary>
		public InvalidSolverPropertyException(string message, Exception ex, InvalidSolverPropertyReason reason)
			: base(message, ex)
		{
			Reason = reason;
		}

		/// <summary> Construct exception during serialization.
		/// </summary>
		protected InvalidSolverPropertyException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
