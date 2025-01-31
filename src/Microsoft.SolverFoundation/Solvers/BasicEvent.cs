using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Events that are scheduled when something happens to a variable.
	///   Listeners can subscribe to this event so that when the event
	///   is activated by the scheduler, the listener is called.
	///   By "Basic we mean that no extra information is dispatched to the
	///   listeners, just the event.
	/// </summary>
	/// <remarks>
	///   No information attached. This way we decouple the event from any
	///   particular detail (i.e. useable for events related to bools, ints
	///   reals, whatever).
	/// </remarks>
	internal class BasicEvent : AbstractEvent
	{
		/// <summary>
		///   delegates that can subcribe to the event
		/// </summary>
		public delegate bool Listener();

		private List<Listener> _subscribedListeners;

		/// <summary>
		///   Construction
		/// </summary>
		/// <param name="s">the scheduler to which the event is connected</param>
		public BasicEvent(Scheduler s)
			: base(s)
		{
			_subscribedListeners = new List<Listener>();
		}

		/// <summary>
		///   Subscribes a listener to the event.
		/// </summary>
		public void Subscribe(Listener l)
		{
			_subscribedListeners.Add(l);
		}

		/// <summary>
		///   Method called when scheduler activates the event.
		///   What it does is to dispatch the call to all subscribed listeners.
		/// </summary>
		protected internal override bool Activate()
		{
			int count = _subscribedListeners.Count;
			for (int i = 0; i < count; i++)
			{
				Listener listener = _subscribedListeners[i];
				if (!listener())
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		///   Goes through all listeners subscribed to the event
		/// </summary>
		internal IEnumerable<Listener> EnumerateListeners()
		{
			foreach (Listener subscribedListener in _subscribedListeners)
			{
				yield return subscribedListener;
			}
		}
	}
}
