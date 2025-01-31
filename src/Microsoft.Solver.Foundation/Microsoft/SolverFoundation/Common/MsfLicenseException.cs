using System;
using System.Runtime.Serialization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Indicates that the model is too large for this edition of Microsoft Solver Foundation.
	/// </summary>
	[Serializable]
	public class MsfLicenseException : MsfException
	{
		/// <summary>
		/// Create the default MsfLicenseException instance.
		/// </summary>
		internal MsfLicenseException()
			: base(Resources.ModelSizeLimitHasBeenExceed + Environment.NewLine + License.LimitsToString())
		{
		}

		/// <summary>
		/// Create the MsfLicenseException instance with the given message string.
		/// </summary>
		internal MsfLicenseException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Create the MsfLicenseException instance with the given message string and inner exception.
		/// </summary>
		internal MsfLicenseException(string message, Exception ex)
			: base(message, ex)
		{
		}

		/// <summary> Construct exception during serialization.
		/// </summary>
		protected MsfLicenseException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
