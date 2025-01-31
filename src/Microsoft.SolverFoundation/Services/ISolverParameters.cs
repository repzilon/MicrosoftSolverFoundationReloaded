using System;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Interface for defining solver parameter class.
	/// </summary>
	public interface ISolverParameters
	{
		/// <summary> Callback for ending the solve
		/// </summary>
		Func<bool> QueryAbort { get; set; }
	}
}
