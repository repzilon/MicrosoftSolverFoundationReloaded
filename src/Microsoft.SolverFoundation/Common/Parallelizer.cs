using System;
using System.Threading;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Set up an object to manage a level of parallelization suited to an algorithm.
	/// </summary>
	internal class Parallelizer
	{
		/// <summary> Any thread may set Failed true.  All should stop if they see it has happened.
		/// </summary>
		public volatile bool Failed;

		public Exception innerException;

		private WaitHandle[] waitHandles;

		public int ThreadCount { get; private set; }

		/// <summary> Control parallel operation
		/// </summary>
		public Parallelizer()
		{
			ThreadCount = AlgebraContext.ThreadCountLimit;
		}

		public void FinishWorkItem(int threadIndex)
		{
			if (threadIndex < ThreadCount - 1)
			{
				(waitHandles[threadIndex] as AutoResetEvent).Set();
			}
		}

		/// <summary> Run a copy of the action on each thread and wait for all to finish
		/// </summary>
		/// <param name="action"> the Action to be run by each thread </param>
		/// <param name="maximumUsefulThreading"> Keep the investment in threading sensibly bounded </param>
		public bool Run(Action<object> action, int maximumUsefulThreading)
		{
			ThreadCount = Math.Min(AlgebraContext.ThreadCountLimit, Math.Max(1, maximumUsefulThreading));
			Failed = false;
			if (1 < ThreadCount)
			{
				try
				{
					waitHandles = new WaitHandle[ThreadCount - 1];
					int num = ThreadCount - 1;
					while (0 <= --num)
					{
						waitHandles[num] = new AutoResetEvent(initialState: false);
						ThreadPool.QueueUserWorkItem(action.Invoke, new ThreadState(this, num));
					}
					action(new ThreadState(this, ThreadCount - 1));
					for (int i = 0; i < waitHandles.Length; i++)
					{
						waitHandles[i].WaitOne();
					}
				}
				catch
				{
					Failed = true;
					throw;
				}
			}
			else
			{
				action(new ThreadState(this, 0));
			}
			return !Failed;
		}
	}
}
