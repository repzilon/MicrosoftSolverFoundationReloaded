using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Manages the nodes in the branch and bound tree.
	/// </summary>
	internal struct NodeManager
	{
		private Heap<Node> _sortedNodes;

		private List<Node> _nodes;

		private SearchStrategy _searchStrategy;

		/// <summary>
		/// Gets whether there are nodes being managed.
		/// </summary>
		public int Count
		{
			get
			{
				if (_searchStrategy != SearchStrategy.DepthFirst)
				{
					return _sortedNodes.Count;
				}
				return _nodes.Count;
			}
		}

		/// <summary>
		/// Gets or sets in which order the nodes are popped from the manager.
		/// </summary>
		public SearchStrategy SearchStrategy => _searchStrategy;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public NodeManager(SearchStrategy searchStrategy)
		{
			_searchStrategy = searchStrategy;
			if (_searchStrategy == SearchStrategy.DepthFirst)
			{
				_sortedNodes = null;
				_nodes = new List<Node>();
			}
			else
			{
				_sortedNodes = new Heap<Node>(Reverse);
				_nodes = null;
			}
		}

		/// <summary>
		/// Adds a node.
		/// </summary>
		public void Add(Node node)
		{
			if (_searchStrategy == SearchStrategy.DepthFirst)
			{
				_nodes.Add(node);
			}
			else
			{
				_sortedNodes.Add(node);
			}
		}

		/// <summary>
		/// Removes all the nodes.
		/// </summary>
		public void Clear()
		{
			if (_nodes != null)
			{
				_nodes.Clear();
			}
			if (_sortedNodes != null)
			{
				_sortedNodes.Clear();
			}
		}

		/// <summary>
		/// Gets the next node to process.
		/// </summary>
		/// <returns>A node.</returns>
		public Node Pop()
		{
			if (_searchStrategy == SearchStrategy.DepthFirst)
			{
				if (_nodes.Count == 0)
				{
					throw new InvalidOperationException();
				}
				Node result = _nodes[_nodes.Count - 1];
				_nodes.RemoveAt(_nodes.Count - 1);
				return result;
			}
			return _sortedNodes.Pop();
		}

		/// <summary>
		/// Switches to best bound search.
		/// </summary>
		public void SwitchTo(SearchStrategy strategy)
		{
			if (strategy == _searchStrategy)
			{
				return;
			}
			if (_searchStrategy == SearchStrategy.DepthFirst)
			{
				_searchStrategy = strategy;
				if (_searchStrategy == SearchStrategy.BestBound)
				{
					_sortedNodes = new Heap<Node>(Reverse, _nodes.Count);
				}
				else
				{
					_sortedNodes = new Heap<Node>(EstimateReverse, _nodes.Count);
				}
				foreach (Node node in _nodes)
				{
					_sortedNodes.Add(node);
				}
				_nodes.Clear();
				_nodes = null;
			}
			else
			{
				if (_searchStrategy != SearchStrategy.BestEstimate || strategy != 0)
				{
					throw new NotSupportedException();
				}
				_searchStrategy = strategy;
				Heap<Node> heap = new Heap<Node>(Reverse, _sortedNodes.Count);
				while (_sortedNodes.Count > 0)
				{
					heap.Add(_sortedNodes.Pop());
				}
				_sortedNodes = heap;
			}
		}

		/// <summary>
		/// Compares two nodes.
		/// </summary>
		public static bool Reverse(Node x, Node y)
		{
			return y.LowerBoundGoalValue < x.LowerBoundGoalValue;
		}

		/// <summary>
		/// Compares two nodes.
		/// </summary>
		public static bool EstimateReverse(Node x, Node y)
		{
			return y.ExpectedGoalValue < x.ExpectedGoalValue;
		}
	}
}
