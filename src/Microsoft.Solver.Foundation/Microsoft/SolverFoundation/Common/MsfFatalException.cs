using System;
using System.Runtime.Serialization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Indicates that Microsoft Solver Foundation encountered a fatal error.
	/// </summary>
	[Serializable]
	internal sealed class MsfFatalException : MsfException
	{
		/// <summary>
		/// Create the default MsfFatalException instance with message defined in Resources.FatalError
		/// </summary>
		public MsfFatalException()
			: base(Resources.FatalError)
		{
		}

		/// <summary>
		/// Create the MsfFatalException instance with the given message string
		/// </summary>
		public MsfFatalException(string strMessage)
			: base(strMessage)
		{
		}

		/// <summary>
		/// Create the MsfFatalException instance with the given message string and inner exception
		/// </summary>
		public MsfFatalException(string strMessage, Exception ex)
			: base(strMessage, ex)
		{
		}

		/// <summary> Construct exception during serialization 
		/// </summary>
		private MsfFatalException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
