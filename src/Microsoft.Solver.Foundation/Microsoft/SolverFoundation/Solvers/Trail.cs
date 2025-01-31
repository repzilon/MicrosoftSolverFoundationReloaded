using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Trailing datastructure: Backtrackable objects (of the corresponding
	///   Content type) can be registered to it. Then we can save the
	///   Trail whenever needed, modify the contents, and a restore operation
	///   will bring them back to their previous state.
	/// </summary>
	///
	/// <remarks>
	///   TODO with the design choice for stacks of vectors and trails it should
	///   be easy to allow returning cheaply several n steps back among previous
	///   states. Can be useful for non-chronological backtracking.
	/// </remarks>
	internal sealed class Trail<Content>
	{
		private struct Archive
		{
			public Backtrackable<Content> _bt;

			public Content _value;

			public int _previousDepth;

			public Archive(Backtrackable<Content> x)
			{
				_bt = x;
				_value = x.Value;
				_previousDepth = x._depthOfLastSave;
			}
		}

		private StackOfLists<Archive> _history;

		private int _depth;

		private List<Backtrackable<Content>> _allbacktrackables;

		/// <summary>
		///   sum of the numbers of elements stored at each level
		/// </summary>
		public int TotalLength => _history.TotalLength;

		internal int Depth => _depth;

		public Trail()
		{
			_depth = 0;
			_history = new StackOfLists<Archive>(clean: false);
			_history.PushList();
			_allbacktrackables = new List<Backtrackable<Content>>();
		}

		/// <summary>
		///   Saves the state of all Backtrackable objects registered to
		///   the trail. We can save as many times as we wish, and states
		///   will be restored in a LIFO fashion.
		/// </summary>
		public void Save()
		{
			_history.PushList();
			_depth++;
		}

		/// <summary>
		///   Restore the state of all Backtrackable objects registered to the
		///   trail, in a LIFO fashion, i.e. we go 1 step back to previous states.
		/// </summary>
		public void Restore()
		{
			StackOfLists<Archive>.List list = _history.TopList();
			int length = list.Length;
			for (int i = 0; i < length; i++)
			{
				Archive archive = list[i];
				Backtrackable<Content> bt = archive._bt;
				bt._depthOfLastSave = archive._previousDepth;
				bt._currentValue = archive._value;
			}
			_history.PopList();
			_depth--;
		}

		/// <summary> 
		///   Removes all content. This does not undo any modification on the
		///   backtrackable objects (used when memory consumption too high
		///   and re-computation policy must be activated)
		/// </summary>
		public void Clear()
		{
			_history.Clear();
			_history.PushList();
			_depth = 0;
			for (int num = _allbacktrackables.Count - 1; num >= 0; num--)
			{
				_allbacktrackables[num]._depthOfLastSave = -1;
			}
		}

		internal void RecordChange(Backtrackable<Content> x)
		{
			_history.AddToTopList(new Archive(x));
		}

		internal void Register(Backtrackable<Content> bt)
		{
			_allbacktrackables.Add(bt);
		}
	}
}
