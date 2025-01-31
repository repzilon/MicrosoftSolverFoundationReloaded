using System.Diagnostics;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Specifies the methods that a class doing logging should implement.
	/// </summary>
	public interface ILogSource
	{
		/// <summary>
		/// Adds a listener to the list of listeners. The listener will receive log messages that have the correct mask.
		/// </summary>
		/// <remarks>
		/// If the listener is already in the list of listeners, the mask is updated.
		/// If the mask is empty, the listener is removed from the list of listeners. This is equivalent to calling RemoveListener.
		/// </remarks>
		/// <param name="listener"></param>
		/// <param name="ids"></param>
		/// <returns>True if the listener is added to the list of listeners; false if the listener is already in the list.</returns>
		bool AddListener(TraceListener listener, LogIdSet ids);

		/// <summary>
		/// Removes a listener from the list of listeners.
		/// </summary>
		/// <remarks>
		/// Removing a listener that has not been added has no effect.
		/// </remarks>
		/// <param name="listener"></param>
		void RemoveListener(TraceListener listener);
	}
}
