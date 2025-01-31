using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class SOSNodeManager
	{
		private Stack<SOSRowNode> _branchingStack;

		public bool IsEmpty => _branchingStack.Count <= 0;

		public SOSNodeManager()
		{
			_branchingStack = new Stack<SOSRowNode>();
		}

		public void Push(SOSRowNode node)
		{
			_branchingStack.Push(node);
		}

		public SOSRowNode Pop()
		{
			return _branchingStack.Pop();
		}
	}
}
