using System;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Indicates that a term of the model could not be represented for a specific solver.
	/// </summary>
	[Serializable]
	public class InvalidTermException : ModelException
	{
		/// <summary>
		/// The term that caused the error.
		/// </summary>
		internal Term ErrorTerm { get; private set; }

		/// <summary>
		/// Create the default InvalidTermException instance
		/// </summary>
		internal InvalidTermException()
		{
		}

		/// <summary>
		/// Create the InvalidTermException instance with the given message string
		/// </summary>
		internal InvalidTermException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Create the InvalidTermException instance with the given message string and inner exception
		/// </summary>
		internal InvalidTermException(string message, Exception ex)
			: base(message, ex)
		{
		}

		/// <summary> Construct exception during serialization 
		/// </summary>
		protected InvalidTermException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		internal InvalidTermException(string error, Term errorTerm)
			: base(string.Format(CultureInfo.InvariantCulture, Resources.InvalidTermExceptionMessage, new object[2]
			{
				error,
				(errorTerm ?? ((Term)"")).ToString()
			}))
		{
			ErrorTerm = errorTerm;
		}

		internal InvalidTermException(string error, Term errorTerm, string format)
			: base(string.Format(CultureInfo.InvariantCulture, Resources.InvalidTermExceptionMessage, new object[2]
			{
				error,
				((object)errorTerm == null) ? "" : errorTerm.ToString(format, CultureInfo.InvariantCulture)
			}))
		{
			ErrorTerm = errorTerm;
		}
	}
}
