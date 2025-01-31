namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Trailing datastructure: Finite Domains
	///   can be registered to it. Then we can save the
	///   Trail whenever needed, modify the contents, and a restore operation
	///   will bring them back to their previous state.
	/// </summary>
	internal class TrailOfFiniteDomains
	{
		private struct Archive
		{
			internal FiniteDomain _fd;

			internal long _left;

			internal long _right;
		}

		private StackOfLists<Archive> _removedValues;

		/// <summary>
		///   sum of the numbers of elements stored at each level
		/// </summary>
		public int TotalLength => _removedValues.TotalLength;

		public TrailOfFiniteDomains()
		{
			_removedValues = new StackOfLists<Archive>(clean: false);
			_removedValues.PushList();
		}

		/// <summary>
		///   Saves the state of all Backtrackable objects registered to
		///   the trail. We can save as many times as we wish, and states
		///   will be restored in a LIFO fashion.
		/// </summary>
		public void Save()
		{
			_removedValues.PushList();
		}

		/// <summary>
		///   Restore the state of all Backtrackable objects registered to the
		///   trail, in a LIFO fashion, i.e. we go 1 step back to previous states.
		/// </summary>
		public void Restore()
		{
			StackOfLists<Archive>.List list = _removedValues.TopList();
			int length = list.Length;
			for (int i = 0; i < length; i++)
			{
				Archive archive = list[i];
				archive._fd.RestoreValue(archive._left, archive._right);
			}
			_removedValues.PopList();
		}

		/// <summary> 
		///   Removes all content. This does not undo any modification on the
		///   backtrackable objects (used when memory consumption too high
		///   and re-computation policy must be activated)
		/// </summary>
		public void Clear()
		{
			_removedValues.Clear();
			_removedValues.PushList();
		}

		internal void RecordRemoval(FiniteDomain s, long l, long r)
		{
			Archive c = default(Archive);
			c._fd = s;
			c._left = l;
			c._right = r;
			_removedValues.AddToTopList(c);
		}
	}
}
