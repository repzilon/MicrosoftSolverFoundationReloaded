using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class SOSRowNode
	{
		public int _first;

		public int _split;

		public int _last;

		public int _vidRow;

		public int[] _vids;

		public double[] _weights;

		public SimplexBasis _bas;

		public bool _lower;

		public bool _sorted;

		public SpecialOrderedSetType _sosType;

		private SOSRowNode()
		{
		}

		public SOSRowNode(int vidRow, SpecialOrderedSetType sosType, SimplexTask task)
		{
			_split = _first;
			_vidRow = vidRow;
			_lower = true;
			_last = task.Solver.GetRowEntryCount(vidRow) - 2;
			_vids = new int[_last + 1];
			_weights = new double[_last + 1];
			int num = 0;
			foreach (LinearEntry rowEntry in task.Solver.GetRowEntries(vidRow))
			{
				_vids[num++] = rowEntry.Index;
			}
			_bas = task.Basis.Clone();
			_sosType = sosType;
		}

		public SOSRowNode Clone()
		{
			SOSRowNode sOSRowNode = new SOSRowNode();
			sOSRowNode._first = _first;
			sOSRowNode._split = _split;
			sOSRowNode._last = _last;
			sOSRowNode._vidRow = _vidRow;
			sOSRowNode._bas = _bas.Clone();
			sOSRowNode._bas.SetToSlacks();
			sOSRowNode._lower = _lower;
			sOSRowNode._vids = _vids;
			sOSRowNode._weights = _weights;
			sOSRowNode._sorted = _sorted;
			sOSRowNode._sosType = _sosType;
			return sOSRowNode;
		}

		public SOSRowNode RightClone()
		{
			SOSRowNode sOSRowNode = null;
			if (_split < _last)
			{
				sOSRowNode = Clone();
				sOSRowNode._split++;
			}
			return sOSRowNode;
		}
	}
}
