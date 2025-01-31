using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Priority queue implemented on top of .NET generic queue.
	///   This implementation should be fast when the number of priorities is 
	///   kept reasonably small.
	///
	///   Number of priorities is given at construction time, and priorities
	///   are from 0 to NumberOfPriorities-1.
	/// </summary>
	internal class PriorityQueue<Content>
	{
		private Queue<Content>[] _queues;

		private int _firstPosition;

		/// <summary>
		///   Number of priorities of the queue.
		///   This number is given at construction, it can be re-set but
		///   (for simplicity) only while the queue is empty
		/// </summary>
		public int NumberOfPriorities
		{
			get
			{
				return _queues.Length;
			}
			set
			{
				ConstructQueues(value);
			}
		}

		/// <summary>
		///   Construction
		/// </summary>
		///
		/// <param name="nbPriorities">(small) number of priorities</param>
		public PriorityQueue(int nbPriorities)
		{
			ConstructQueues(nbPriorities);
		}

		/// <summary>
		///   Remove all content in constant time 
		/// </summary>
		public void Clear()
		{
			int numberOfPriorities = NumberOfPriorities;
			for (int i = _firstPosition; i < numberOfPriorities; i++)
			{
				_queues[i].Clear();
			}
			_firstPosition = numberOfPriorities;
		}

		/// <summary>
		///   Removes and returns the element at beginning of the queue.
		/// </summary>
		public Content Dequeue()
		{
			return FirstNonEmptyQueue().Dequeue();
		}

		/// <summary>
		///   Insert an element at given priority, at end of queue
		/// </summary>
		public void Enqueue(Content elt, int priority)
		{
			_queues[priority].Enqueue(elt);
			if (priority < _firstPosition)
			{
				_firstPosition = priority;
			}
		}

		/// <summary>
		///   Gets the first element of the queue, without removing it
		/// </summary>
		public Content Peek()
		{
			return FirstNonEmptyQueue().Peek();
		}

		/// <summary>
		///   Checks whether queue empty
		/// </summary>
		public bool IsEmpty()
		{
			return FirstNonEmptyQueue() == null;
		}

		/// <returns>
		///   non-empty queue with lowest priority; or
		///   null if the priority queue is empty
		/// </returns>
		private Queue<Content> FirstNonEmptyQueue()
		{
			int numberOfPriorities = NumberOfPriorities;
			while (_firstPosition < numberOfPriorities)
			{
				Queue<Content> queue = _queues[_firstPosition];
				if (queue.Count > 0)
				{
					return queue;
				}
				_firstPosition++;
			}
			return null;
		}

		private void ConstructQueues(int nbPriorities)
		{
			_firstPosition = nbPriorities;
			_queues = new Queue<Content>[nbPriorities];
			for (int i = 0; i < nbPriorities; i++)
			{
				_queues[i] = new Queue<Content>();
			}
		}

		private bool InvariantIsVerified()
		{
			for (int i = 0; i < _firstPosition; i++)
			{
				if (_queues[i].Count > 0)
				{
					return false;
				}
			}
			return true;
		}
	}
}
