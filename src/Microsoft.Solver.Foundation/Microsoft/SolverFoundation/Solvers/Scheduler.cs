using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Scheduler, responsible for propagating Disolver events.
	/// </summary>
	internal class Scheduler
	{
		protected const bool Success = true;

		protected const bool Failure = false;

		private Queue<AbstractEvent> _queue;

		private List<AbstractEvent> _registeredEvents;

		private long _nbEventsActivated;

		private List<Interval> _intervalVector;

		private readonly ParameterlessProcedure _checkAbortion;

		/// <summary>
		///   Returns the number of events activated during the lifetime of 
		///   the scheduler.
		/// </summary>
		public long NbEventsActivated => _nbEventsActivated;

		/// <summary>
		///   New scheduler; 
		///   Schedulable events must then be registered to it
		/// </summary>
		public Scheduler(ParameterlessProcedure checkAbortion)
		{
			_queue = new Queue<AbstractEvent>();
			_registeredEvents = new List<AbstractEvent>();
			_intervalVector = new List<Interval>();
			_checkAbortion = checkAbortion;
		}

		/// <summary>
		///   New scheduler; 
		///   Schedulable events must then be registered to it
		/// </summary>
		public Scheduler()
			: this(delegate
			{
			})
		{
		}

		/// <summary>
		///   Activates the scheduler, i.e. activates all scheduled events.
		/// </summary>
		/// <remarks>
		///   Post-condition is that the queue be systematically emptied
		///   when we return.
		/// </remarks>
		/// <returns>false iff failure detected at any step</returns>
		public bool Activate()
		{
			int num = 0;
			while (!IsEmpty())
			{
				_nbEventsActivated++;
				if (num >= 1000)
				{
					num = 0;
					_checkAbortion();
				}
				AbstractEvent abstractEvent = _queue.Dequeue();
				Unschedule(abstractEvent);
				bool flag = abstractEvent.Activate();
				if (!flag)
				{
					UnScheduleAll();
					return flag;
				}
				num++;
			}
			return true;
		}

		/// <summary>
		///   Unschedule all events
		/// </summary>
		public void UnScheduleAll()
		{
			while (!IsEmpty())
			{
				AbstractEvent abstractEvent = _queue.Dequeue();
				abstractEvent.UnscheduleDueToFailure();
			}
		}

		/// <summary>
		///   True if queue empty in the sense nothing scheduled.
		/// </summary>
		public bool IsEmpty()
		{
			return _queue.Count == 0;
		}

		/// <summary>
		///   Called at creation time by abstract events to make sure the
		///   scheduler is aware of their creation
		/// </summary>
		internal void RegisterSchedulableEvent(AbstractEvent e)
		{
			_registeredEvents.Add(e);
		}

		/// <summary>
		///   Enqueue the event in the scheduler. The caller is 
		///   responsible for checking that the event is not already scheduled
		/// </summary>
		internal void Reschedule(AbstractEvent e)
		{
			_queue.Enqueue(e);
		}

		/// <summary>
		///   ValuesRemovedEvents need a lot of creation of temporary
		///   int vectors. To avoid newing (and garbage-collecting) them 
		///   repeatedly we share a pre-allocated one in scheduler. 
		///   The discipline should be to use it (non concurrently) and
		///   Clear it as soon as exiting when block is left.
		/// </summary>
		internal List<Interval> FastAllocatedIntervalList()
		{
			return _intervalVector;
		}

		/// <summary>
		///   Unschedules an event
		/// </summary>
		private static void Unschedule(AbstractEvent next)
		{
			next._isScheduled = false;
		}
	}
}
