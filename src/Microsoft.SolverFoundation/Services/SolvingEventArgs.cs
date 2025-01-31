using System;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Event args for the Solving event.
	/// </summary> 
	/// <remarks>
	/// This event is fired at the solver’s discretion.
	/// </remarks> 
	public sealed class SolvingEventArgs : EventArgs
	{
		internal Task _task;

		internal Directive _directive;

		/// <summary> Directive identifying the solver.
		/// Any changes to the directive may not be picked up before the next
		/// call to SolverContext.Solve().
		/// </summary> 
		public Directive Directive => _directive;

		/// <summary>Retreive a property by property name.
		/// </summary> 
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidSolverPropertyException"></exception>
		public object this[string property]
		{
			get
			{
				return _task.GetSolverProperty(property, null, SolverContext._emptyArray);
			}
			set
			{
				_task.SetSolverProperty(property, null, SolverContext._emptyArray, value);
			}
		}

		/// <summary>Terminates the solver that fired the event.
		/// </summary> 
		public void Cancel()
		{
			_task.AbortTask();
		}

		/// <summary>Terminates the entire Solve operation.
		/// </summary> 
		public void CancelAll()
		{
			_task.AbortAllTasks();
		}
	}
}
