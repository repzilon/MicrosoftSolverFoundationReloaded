using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Sends information to the Listeners.
	/// </summary>
	internal class LogSource : ILogSource
	{
		protected sealed class ListenerWrapper
		{
			private readonly TraceListener _listener;

			private LogIdSet _ids;

			private readonly bool _fThreadSafe;

			public TraceListener Listener => _listener;

			public LogIdSet IdSet
			{
				get
				{
					return _ids;
				}
				set
				{
					_ids = value;
				}
			}

			public bool IsThreadSafe => _fThreadSafe;

			public ListenerWrapper(TraceListener listener, LogIdSet ids)
			{
				_listener = listener;
				_ids = ids;
				_fThreadSafe = _listener.IsThreadSafe;
			}
		}

		protected const TraceEventType kevtDefault = TraceEventType.Information;

		private readonly string _name;

		public static TraceSource SimplexTracer = new TraceSource("SimplexSourceTrace");

		private LogIdSet _idsUnion;

		private List<ListenerWrapper> _rgwrap;

		private List<ListenerWrapper> _rgwrapWork;

		private object _sync = new object();

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public LogSource(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException("name");
			}
			_name = name;
			_rgwrap = new List<ListenerWrapper>();
			_rgwrapWork = new List<ListenerWrapper>();
		}

		/// <summary>
		/// Adds a listener. If the listener is already registered, its LogIdSet is updated.
		/// If the LogIdSet is empty, the listener is removed.
		/// Returns true iff the listener was newly added (not removed or updated).
		/// </summary>
		public bool AddListener(TraceListener listener, LogIdSet ids)
		{
			if (listener == null)
			{
				throw new ArgumentNullException("listener");
			}
			if (ids.IsEmpty)
			{
				RemoveListener(listener);
				return false;
			}
			lock (_sync)
			{
				for (int i = 0; i < _rgwrap.Count; i++)
				{
					if (_rgwrap[i].Listener == listener)
					{
						_rgwrap[i].IdSet = ids;
						_idsUnion |= ids;
						return false;
					}
				}
				ListenerWrapper item = new ListenerWrapper(listener, ids);
				_rgwrap.Add(item);
				_idsUnion |= ids;
				return true;
			}
		}

		/// <summary>
		/// Removes a listener. If the listener is not currently registed,
		/// has no affect.
		/// </summary>
		public void RemoveListener(TraceListener listener)
		{
			lock (_sync)
			{
				for (int i = 0; i < _rgwrap.Count; i++)
				{
					if (_rgwrap[i].Listener == listener)
					{
						_rgwrap.RemoveAt(i);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Checks whether an event should be logged.
		/// </summary>
		public bool ShouldLog(int id)
		{
			return _idsUnion.Contains(id);
		}

		/// <summary>
		/// Logs an event.
		/// </summary>
		public void LogEvent(int id)
		{
			if (_idsUnion.Contains(id))
			{
				LogEventCore(id, string.Empty);
			}
		}

		/// <summary>
		/// Logs an event.
		/// </summary>
		public void LogEvent(int id, string message)
		{
			if (_idsUnion.Contains(id))
			{
				LogEventCore(id, message);
			}
		}

		/// <summary>
		/// Logs an event.
		/// </summary>
		public void LogEvent(int id, string format, object arg)
		{
			if (_idsUnion.Contains(id))
			{
				LogEventCore(id, format, arg);
			}
		}

		/// <summary>
		/// Logs an event.
		/// </summary>
		public void LogEvent(int id, string format, object arg1, object arg2)
		{
			if (_idsUnion.Contains(id))
			{
				LogEventCore(id, format, arg1, arg2);
			}
		}

		/// <summary>
		/// Logs an event.
		/// </summary>
		public void LogEvent(int id, string format, object arg1, object arg2, object arg3)
		{
			if (_idsUnion.Contains(id))
			{
				LogEventCore(id, format, arg1, arg2, arg3);
			}
		}

		/// <summary>
		/// Logs an event.
		/// </summary>
		public void LogEvent(int id, string format, params object[] args)
		{
			if (_idsUnion.Contains(id))
			{
				LogEventCore(id, format, args);
			}
		}

		protected virtual void LogEventCore(int id, string format, params object[] args)
		{
			List<ListenerWrapper> list = null;
			list = Interlocked.Exchange(ref _rgwrapWork, null);
			if (list == null)
			{
				list = new List<ListenerWrapper>(_rgwrap.Count);
			}
			lock (_sync)
			{
				for (int i = 0; i < _rgwrap.Count; i++)
				{
					if (_rgwrap[i].IdSet.Contains(id))
					{
						list.Add(_rgwrap[i]);
					}
				}
				if (list.Count == 0)
				{
					_idsUnion -= id;
					_rgwrapWork = list;
					return;
				}
			}
			TraceEventCache eventCache = new TraceEventCache();
			for (int j = 0; j < list.Count; j++)
			{
				ListenerWrapper listenerWrapper = list[j];
				if (!listenerWrapper.IsThreadSafe)
				{
					lock (listenerWrapper)
					{
						listenerWrapper.Listener.TraceEvent(eventCache, _name, TraceEventType.Information, id, format, args);
					}
				}
				else
				{
					listenerWrapper.Listener.TraceEvent(eventCache, _name, TraceEventType.Information, id, format, args);
				}
			}
			list.Clear();
			_rgwrapWork = list;
		}
	}
}
