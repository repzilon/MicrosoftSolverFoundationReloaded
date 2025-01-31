using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Trailing datastructure: BacktrackableIntsets
	///   can be registered to it. Then we can save the
	///   Trail whenever needed, modify the contents, and a restore operation
	///   will bring them back to their previous state.
	/// </summary>
	internal class TrailOfIntSets
	{
		private enum Change
		{
			Insertion,
			Removal
		}

		private struct Archive
		{
			internal BacktrackableIntSet _target;

			internal Change _action;

			internal int _element;

			public Archive(BacktrackableIntSet t, Change a, int e)
			{
				_target = t;
				_action = a;
				_element = e;
			}
		}

		private StackOfLists<Archive> _history;

		private List<BacktrackableIntSet> _allSets;

		/// <summary>
		///   sum of the numbers of elements stored at each level
		/// </summary>
		public int TotalLength => _history.TotalLength;

		/// <summary>
		///   Construction
		/// </summary>
		public TrailOfIntSets()
		{
			_history = new StackOfLists<Archive>(clean: false);
			_history.PushList();
			_allSets = new List<BacktrackableIntSet>();
		}

		/// <summary>
		///   Saves the state of all Backtrackable objects registered to
		///   the trail. We can save as many times as we wish, and states
		///   will be restored in a LIFO fashion.
		/// </summary>
		public void Save()
		{
			_history.PushList();
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
				FiniteIntSet set = archive._target._set;
				int element = archive._element;
				if (archive._action == Change.Insertion)
				{
					set.Remove(element);
				}
				else
				{
					set.Add(element);
				}
			}
			_history.PopList();
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
		}

		internal void RecordInsertion(BacktrackableIntSet s, int elt)
		{
			_history.AddToTopList(new Archive(s, Change.Insertion, elt));
		}

		internal void RecordRemoval(BacktrackableIntSet s, int elt)
		{
			_history.AddToTopList(new Archive(s, Change.Removal, elt));
		}

		internal void Register(BacktrackableIntSet s)
		{
			_allSets.Add(s);
		}
	}
}
