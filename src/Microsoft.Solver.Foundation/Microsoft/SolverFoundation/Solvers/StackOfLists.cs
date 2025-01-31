using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A specialized class of stack of lists with low-level representation.
	///   Conceptually equivalent to Stack(List(content)), which would use the 
	///   generic classes Stack and List; but optimized.
	///   Key data-structure for storage of lists of modifications ("trailing").
	///   Implementation is basically one big resizeable array storing all contents,
	///   together with a stack storing the indexes at which each list starts.
	/// </summary>
	internal class StackOfLists<Content>
	{
		/// <summary>
		///   "Virtual" vector giving a view on the list at the top of
		///   the stack. Provides limited functionality, e.g. read-only access
		/// </summary>
		internal struct List
		{
			private readonly Content[] _directAccess;

			private readonly int _begin;

			private readonly int _end;

			public int Length => _end - _begin;

			public Content this[int position] => _directAccess[_begin + position];

			public List(StackOfLists<Content> s)
			{
				_directAccess = s._contents;
				_begin = s.IndexBeginTop();
				_end = s.IndexEndTop();
			}
		}

		/// <summary>
		///   Storage of all contents
		/// </summary>
		private Content[] _contents;

		/// <summary>
		///   Only contents at positions 0 to _length are considered;
		///   allows to have an extensible storage
		/// </summary>
		private int _length;

		/// <summary>
		///   Stack keeping the separations between the lists, e.g. the top list
		///   consists in the contents stored between positions 
		///   _levels.Peek() and _length-1
		/// </summary>
		private Stack<int> _levels;

		/// <summary>
		///   Flag checked when clean-up loop is needed;
		///   if false the loop is skipped
		/// </summary>
		private readonly bool _clean;

		/// <summary>
		///   Number of lists that are pushed in the stack
		/// </summary>
		public int NbLevels => _levels.Count;

		/// <summary>
		///   Returns the cumulated size of the lists at all levels
		/// </summary>
		public int TotalLength => _contents.Length;

		/// <summary>
		///   Default constructor; use unless you have a good reason
		/// </summary>
		public StackOfLists()
			: this(clean: true)
		{
		}

		/// <summary>
		///   Construction with parameters
		/// </summary>
		/// <param name="clean">
		///   Option specifying whether the StackOfList should perform all
		///   clean-ups when it is resized-down. In general it will set the
		///   contents to their default value but this can be skipped if the option
		///   is set to false. The clean-up loop is useless however it
		///   removes handles to any object that is not effectively stored anymore,
		///   allowing the GC to make a better job. The non-clean mode, which is
		///   perfectly ok, e.g., for value types that contain no reference, allows
		///   constant-time clearing and list-popping.
		/// </param>
		public StackOfLists(bool clean)
		{
			_contents = new Content[1024];
			_length = 0;
			_levels = new Stack<int>();
			_clean = clean;
		}

		/// <summary>
		///   Push a new (empty) list on top of the stack of lists
		/// </summary>
		public void PushList()
		{
			_levels.Push(_length);
		}

		/// <summary>
		///   Pops the stack of lists, removing the list on top of the stack
		/// </summary>
		public void PopList()
		{
			ReduceLength(_levels.Peek());
			_levels.Pop();
		}

		/// <summary>
		///   Adds a content into the list that is on top of the stack
		/// </summary>
		public void AddToTopList(Content c)
		{
			if (_length == _contents.Length)
			{
				ReallocateWithReservedSize(2 * _length);
			}
			_contents[_length] = c;
			_length++;
		}

		/// <summary>
		///   Remove all content
		/// </summary>
		public void Clear()
		{
			ReduceLength(0);
			_levels.Clear();
		}

		/// <summary>
		///   Memory reduction function: call to reduce any 
		///   space reserved in excess by the data-structure
		/// </summary>
		public void TrimExcess()
		{
			_levels.TrimExcess();
			ReallocateWithReservedSize(_length);
		}

		/// <summary>
		///   View on the vector that is on top of the stack.
		///   Use sparingly, e.g. for iteration
		/// </summary>
		internal List TopList()
		{
			return new List(this);
		}

		/// <summary>
		///   True if the stack is empty, i.e. contains no list
		/// </summary>
		public bool IsEmpty()
		{
			return _levels.Count == 0;
		}

		/// <summary>
		///   True if the list at the top of the stack is empty
		/// </summary>
		public bool TopIsEmpty()
		{
			return IndexBeginTop() == IndexEndTop();
		}

		private int IndexBeginTop()
		{
			return _levels.Peek();
		}

		private int IndexEndTop()
		{
			return _length;
		}

		/// <summary>
		///   Re-allocation; used for transparent array extension and,
		///   when explicitly required, for trimming
		/// </summary>
		private void ReallocateWithReservedSize(int newReservedSize)
		{
			Content[] array = new Content[newReservedSize];
			Array.Copy(_contents, array, _length);
			_contents = array;
		}

		/// <summary>
		///   Reduces the length of the content list
		/// </summary>
		private void ReduceLength(int len)
		{
			ResetRange(len, _length);
			_length = len;
		}

		/// <summary>
		///   Resets the contents stored between the "from" index
		///   (included) to the "to" index (non-included) to their
		///   default value.
		/// </summary>
		/// <remarks>
		///   The parameter "clean", specified at construction-time, 
		///   defines what we do in this method
		/// </remarks>
		private void ResetRange(int from, int to)
		{
			if (_clean)
			{
				for (int i = from; i < to; i++)
				{
					_contents[i] = default(Content);
				}
			}
		}
	}
}
