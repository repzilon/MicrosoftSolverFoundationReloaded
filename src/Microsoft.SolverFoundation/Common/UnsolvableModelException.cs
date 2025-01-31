using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// This exception type indicates that no solver was found to solve a model.
	/// </summary>
	[Serializable]
	public class UnsolvableModelException : ModelException
	{
		/// <summary>
		/// An array of exceptions thrown from individual solvers.
		/// </summary>
		internal ReadOnlyCollection<Exception> InnerExceptions { get; private set; }

		/// <summary>
		/// Create the default UnsolvableModelException instance
		/// </summary>
		internal UnsolvableModelException()
		{
		}

		/// <summary>
		/// Create the UnsolvableModelException instance with the given message string
		/// </summary>
		internal UnsolvableModelException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Create the UnsolvableModelException instance with the given message string and inner exception
		/// </summary>
		internal UnsolvableModelException(string message, Exception ex)
			: base(message, ex)
		{
		}

		/// <summary> Construct exception during serialization 
		/// </summary>
		protected UnsolvableModelException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		internal UnsolvableModelException(Exception[] innerExceptions)
			: base(Resources.NoSolverFound, innerExceptions[0])
		{
			InnerExceptions = new ReadOnlyCollection<Exception>(innerExceptions);
		}

		internal UnsolvableModelException(string message, Exception[] innerExceptions)
			: base(message, innerExceptions[0])
		{
			InnerExceptions = new ReadOnlyCollection<Exception>(innerExceptions);
		}
	}
}
