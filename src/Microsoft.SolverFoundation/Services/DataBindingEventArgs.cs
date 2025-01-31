using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Event args for the DataBinding event.
	/// </summary> 
	/// <remarks>
	/// This event is fired for decision objects only, and only
	/// once per model entity (even if the model entity corresponds to more than one
	/// solver level entity as in the case of a ForEach constraint).
	/// </remarks> 
	public class DataBindingEventArgs : EventArgs
	{
		internal string _name;

		internal Term _target;

		internal Task _task;

		internal Directive _directive;

		/// <summary>The name of the target entity.
		/// </summary> 
		/// <remarks>
		/// Also available through, e.g. ((Decision)Target).Name.  Provided
		/// for convenience.
		/// </remarks> 
		public string Name => _name;

		/// <summary>The target entity (a Decision).
		/// </summary> 
		public Term Target => _target;

		/// <summary> 
		/// An IEnumerable of indexes for all solver-level decisions
		/// that correspond to the Decision.
		/// </summary> 
		public IEnumerable<object[]> Indexes
		{
			get
			{
				if (_target is Decision decision)
				{
					return _task.GetIndexes(decision);
				}
				return new object[1][] { SolverContext._emptyArray };
			}
		}

		/// <summary>Change or retreive a property by property name.
		/// </summary> 
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidSolverPropertyException"></exception>
		/// <exception cref="T:System.ArgumentException"></exception>
		public object this[string property]
		{
			get
			{
				return _task.GetSolverProperty(property, _target as Decision, SolverContext._emptyArray);
			}
			set
			{
				_task.SetSolverProperty(property, _target as Decision, SolverContext._emptyArray, value);
			}
		}

		/// <summary>Change or retreive a property by property name.
		/// </summary> 
		/// <remarks>
		/// The indexes are used in the case of indexed decisions or constraints.
		/// </remarks> 
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidSolverPropertyException"></exception>
		/// <exception cref="T:System.ArgumentException"></exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		[SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional")]
		public object this[string property, object[] indexes]
		{
			get
			{
				if (indexes == null)
				{
					throw new ArgumentNullException("indexes");
				}
				return _task.GetSolverProperty(property, _target as Decision, indexes);
			}
			set
			{
				if (indexes == null)
				{
					throw new ArgumentNullException("indexes");
				}
				_task.SetSolverProperty(property, _target as Decision, indexes, value);
			}
		}

		/// <summary> Directive identifying the solver.
		/// </summary> 
		public Directive Directive => _directive;
	}
}
