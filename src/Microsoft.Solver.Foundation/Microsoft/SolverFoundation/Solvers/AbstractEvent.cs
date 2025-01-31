namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Event that is registered to a scheduler so that it can be rescheduled,
	///   which means that the scheduler will activate it later.
	/// </summary>
	internal abstract class AbstractEvent
	{
		protected const bool Success = true;

		protected const bool Failure = false;

		protected internal bool _isScheduled;

		protected readonly Scheduler _scheduler;

		/// <summary>
		///   Construction. All Schedulable events must be attached to
		///   one scheduler. Initially the event is not scheduled.
		/// </summary>
		/// <param name="scheduler">
		///   Scheduler to which event will be associated
		/// </param>
		public AbstractEvent(Scheduler scheduler)
		{
			_scheduler = scheduler;
			scheduler.RegisterSchedulableEvent(this);
		}

		/// <summary>
		///   Code to execute when the event is activated;
		///   Main method defined by derived classes  
		/// </summary>
		/// <returns>false iff failure</returns>
		protected internal abstract bool Activate();

		/// <summary>
		///   Reschedule the event; i.e. it will be activated later by the
		///   scheduler. This operation has no effect if the event is already
		///   scheduled.
		/// </summary>
		protected internal void RescheduleIfNeeded()
		{
			if (!_isScheduled)
			{
				_scheduler.Reschedule(this);
				_isScheduled = true;
			}
		}

		/// <summary>
		///   Reschedule the event; i.e. it will be activated later by the
		///   scheduler. This operation has no effect if the event is already
		///   scheduled.
		/// </summary>
		/// <remarks>HACK</remarks>
		protected internal bool RescheduleIfNeededBool()
		{
			if (!_isScheduled)
			{
				_scheduler.Reschedule(this);
				_isScheduled = true;
			}
			return true;
		}

		/// <summary>
		///   Called by the scheduler when a failure arises while the 
		///   event is still in the queue. This may require to clean-up some
		///   stuff in the state of the event
		/// </summary>
		internal virtual void UnscheduleDueToFailure()
		{
			_isScheduled = false;
		}

		/// <summary>
		///   True iff the event is already currently scheduled.
		/// </summary>
		public bool IsScheduled()
		{
			return _isScheduled;
		}
	}
}
