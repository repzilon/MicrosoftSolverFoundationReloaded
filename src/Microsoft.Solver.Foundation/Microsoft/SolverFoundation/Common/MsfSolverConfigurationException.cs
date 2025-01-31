using System;
using System.Runtime.Serialization;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Indicates that the plugin solvers cause errors
	/// </summary>
	[Serializable]
	public class MsfSolverConfigurationException : MsfException
	{
		/// <summary>
		/// Create the default MsfSolverConfigurationException instance
		/// </summary>
		internal MsfSolverConfigurationException()
		{
		}

		/// <summary>
		/// Create the MsfSolverConfigurationException instance with the given message string
		/// </summary>
		internal MsfSolverConfigurationException(string strMessage)
			: base(strMessage)
		{
		}

		/// <summary>
		/// Create the MsfSolverConfigurationException instance with the given message string and inner exception
		/// </summary>
		internal MsfSolverConfigurationException(string strMessage, Exception ex)
			: base(strMessage, ex)
		{
		}

		/// <summary> Construct exception during serialization 
		/// </summary>
		protected MsfSolverConfigurationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
