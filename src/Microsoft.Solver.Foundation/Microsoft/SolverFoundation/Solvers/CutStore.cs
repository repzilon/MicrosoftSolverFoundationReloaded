using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> In this class, we maintain cutting planes generated, detect/remove duplicates, etc.
	/// </summary>
	/// <remarks>Each MipSolver maintains a single global cut store.</remarks>
	internal sealed class CutStore
	{
		private static readonly ReadOnlyCollection<RowCut> EMPTY_LIST = new ReadOnlyCollection<RowCut>(new List<RowCut>());

		private int _totalCutCount;

		private IDictionary<MipNode, IList<RowCut>> _cuts;

		/// <summary> The number of rounds of Gomory cut generation
		/// </summary>
		public int GomoryCutRound { get; set; }

		/// <summary> Get the total number of cuts from all nodes
		/// </summary>
		public int TotalCutCount => _totalCutCount;

		/// <summary> Construct a cut store
		/// </summary>
		public CutStore(MipSolver solver)
		{
			_cuts = new Dictionary<MipNode, IList<RowCut>>();
		}

		/// <summary> Get the number of cuts generated in the given node
		/// </summary>
		public int Count(MipNode node)
		{
			if (_cuts.TryGetValue(node, out var value))
			{
				return value.Count;
			}
			return 0;
		}

		/// <summary> Get the cuts in the store
		/// </summary>
		/// <remarks>Returned IList is guaranteed not to be null</remarks>
		public IList<RowCut> Cuts(MipNode node)
		{
			if (_cuts.TryGetValue(node, out var value))
			{
				return value;
			}
			return EMPTY_LIST;
		}

		/// <summary> Add a cut to the store
		/// </summary>
		/// <returns>false if and only if the node is integer infeasible</returns>
		public bool AddCuts(CutBuilder cutBuilder)
		{
			foreach (RowCut item in cutBuilder.Build())
			{
				if (cutBuilder.IsNodeIntegerInfeasible)
				{
					return false;
				}
				if (ShouldAccept(cutBuilder, item))
				{
					if (_cuts.TryGetValue(cutBuilder.Node, out var value))
					{
						value.Add(item);
					}
					else
					{
						value = new List<RowCut>();
						value.Add(item);
						_cuts.Add(cutBuilder.Node, value);
					}
					_totalCutCount++;
				}
			}
			return true;
		}

		/// <summary> Age the cuts in the store based on the relaxation solve result of the given node.
		/// </summary>
		public void Age(MipNode node)
		{
		}

		private static bool ShouldAccept(CutBuilder cutBuilder, RowCut cut)
		{
			if (cut == null)
			{
				return false;
			}
			if (!IsCutStrong(cutBuilder, cut))
			{
				return false;
			}
			if (!IsCutStable(cutBuilder, cut))
			{
				return false;
			}
			if (!IsCutSparse(cut))
			{
				return false;
			}
			return true;
		}

		private static bool IsCutSparse(RowCut cut)
		{
			if (cut.Row.RcCount / cut.Row.EntryCount <= 10)
			{
				return cut.Row.EntryCount < 30;
			}
			return true;
		}

		private static bool IsCutStable(CutBuilder cutBuilder, RowCut cut)
		{
			double num = -1.0;
			double num2 = double.MaxValue;
			Vector<double>.Iter iter = new Vector<double>.Iter(cut.Row);
			while (iter.IsValid)
			{
				double num3 = Math.Abs(iter.Value);
				if (num2 > num3)
				{
					num2 = num3;
				}
				if (num < num3)
				{
					num = num3;
				}
				iter.Advance();
			}
			return num2 / num > cutBuilder.Node.Task.AlgorithmDouble.VarZeroRatio;
		}

		private static bool IsCutStrong(CutBuilder cutBuilder, RowCut cut)
		{
			double num = cutBuilder.ComputeCutRowValue(cut.Row);
			if (Math.Abs(num - cut.LowerRowBound) < 0.001)
			{
				return false;
			}
			if (Math.Abs(num - cut.UpperRowBound) < 0.001)
			{
				return false;
			}
			if (num >= cut.LowerRowBound && num <= cut.UpperRowBound)
			{
				return false;
			}
			return true;
		}
	}
}
