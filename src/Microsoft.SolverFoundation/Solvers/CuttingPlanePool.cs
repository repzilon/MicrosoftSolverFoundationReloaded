using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A cutting plane pool that stores the cuts and merge/clear the cuts to the user model when necessary.
	/// This class is designed for each branch and bound node, which should have its own cutting plane pool.
	/// When the node is relocated to a worker thread, we need to set the simplex solver object in the worker
	/// thread to this cutting plane pool.
	/// </summary>
	internal class CuttingPlanePool : Constraint
	{
		/// <summary>
		/// Struct for cutting planes
		/// </summary>
		internal struct CuttingPlane
		{
			private CutKind _kind;

			private VectorRational _row;

			private Rational _lower;

			private Rational _upper;

			internal CutKind Kind => _kind;

			internal VectorRational Row => _row;

			internal Rational LowerBound => _lower;

			internal Rational UpperBound => _upper;

			/// <summary>
			/// Create a cutting plane
			/// </summary>
			/// <param name="kind"></param>
			/// <param name="row">the cut row</param>
			/// <param name="lower">the lower bound</param>
			/// <param name="upper">the upper bound</param>
			internal CuttingPlane(CutKind kind, VectorRational row, Rational lower, Rational upper)
			{
				_kind = kind;
				_row = row;
				_lower = lower;
				_upper = upper;
			}
		}

		private static int _pathCutLim = 5000;

		private static bool _fCoverCutGen = true;

		private WeakReference _model;

		private List<CuttingPlane> _cuts;

		private List<int> _cutRowVids;

		private bool _fReset;

		private int _cCutGomory;

		private int _cCutCover;

		private int _cCutMixedCover;

		private int _cCutFlowCover;

		private int _cPathCuts;

		private List<int> _listUsedRowCover;

		private List<int> _listUsedRowMixedCover;

		private List<int> _listUsedRowFlowCover;

		internal SimplexReducedModel Model
		{
			get
			{
				if (_model == null)
				{
					return null;
				}
				return (SimplexReducedModel)_model.Target;
			}
			set
			{
				if (_model == null)
				{
					_model = new WeakReference(value);
				}
				_model.Target = value;
			}
		}

		public static int PathCutLimit => _pathCutLim;

		public int GomoryFractionalCutCount => _cCutGomory;

		public int CoverCutCount => _cCutCover;

		public int MixedCoverCutCount => _cCutMixedCover;

		public int FlowCoverCutCount => _cCutFlowCover;

		public int PathCutCount => _cPathCuts;

		/// <summary>
		/// Return a list of current cuts.
		/// </summary>
		internal List<CuttingPlane> Cuts => _cuts;

		internal static void ResetCoverGenFlag()
		{
			_fCoverCutGen = true;
		}

		/// <summary>
		/// Return the ancestor cutting plane pool of the input constraint
		/// </summary>
		/// <returns></returns>
		internal static CuttingPlanePool GetAncestorCutPool(Constraint currentConstraint)
		{
			CuttingPlanePool result = null;
			while (currentConstraint != null && (result = currentConstraint as CuttingPlanePool) == null)
			{
				currentConstraint = currentConstraint.ParentConstraint;
			}
			return result;
		}

		internal void IncrementGomoryFractionalCutCount()
		{
			_cCutGomory++;
			_cPathCuts++;
		}

		internal void IncrementCoverCutCount()
		{
			_cCutCover++;
			_cPathCuts++;
		}

		internal void IncrementMixedCoverCutCount()
		{
			_cCutMixedCover++;
			_cPathCuts++;
		}

		internal void IncrementFlowCoverCutCount()
		{
			_cCutFlowCover++;
			_cPathCuts++;
		}

		/// <summary>
		/// Merge the cuts to the user model (must be called before solve)
		/// </summary>
		public override void ApplyConstraintCore(SimplexTask thread)
		{
			int num = ((!_fReset) ? _cutRowVids.Count : 0);
			for (int i = num; i < _cuts.Count; i++)
			{
				CuttingPlane cut = _cuts[i];
				int num2 = 0;
				string text2;
				bool flag;
				int vid;
				do
				{
					Guid guid = Guid.NewGuid();
					string text = ((cut.Kind == CutKind.Cover) ? "Cover" : ((cut.Kind == CutKind.MixedCover) ? "MixedCover" : ((cut.Kind == CutKind.FlowCover) ? "FlowCover" : ((cut.Kind != CutKind.GomoryFractional) ? "Unknown" : "GomoryFractional"))));
					text2 = "cut" + text + guid.ToString();
					flag = thread.Solver.AddRow(text2, out vid);
					num2++;
				}
				while (!flag && num2 <= 10);
				if (!flag)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.CannotAddCutXToModel0, new object[1] { text2 }));
				}
				_cutRowVids.Add(vid);
				SetCutRow(thread, vid, cut);
			}
			_fReset = false;
		}

		/// <summary>
		/// Disable all the cut rows in the user model (must be called after solve)
		/// </summary>
		public override void ResetConstraintCore(SimplexTask thread)
		{
			for (int i = 0; i < _cutRowVids.Count; i++)
			{
				thread.Solver.RemoveRow(_cutRowVids[i]);
			}
			_cutRowVids.Clear();
			_fReset = true;
		}

		private static void SetCutRow(SimplexTask thread, int vidCutRow, CuttingPlane cut)
		{
			SimplexSolver solver = thread.Solver;
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(cut.Row);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				Rational value = iter.Value;
				if (solver.IsRow(rc))
				{
					foreach (LinearEntry rowEntry in solver.GetRowEntries(rc))
					{
						if (!solver.IsRow(rowEntry.Index))
						{
							UpdateCoefInCutRow(solver, vidCutRow, rowEntry.Index, rowEntry.Value * value);
						}
					}
				}
				else
				{
					UpdateCoefInCutRow(solver, vidCutRow, rc, value);
				}
				iter.Advance();
			}
			solver.SetBounds(vidCutRow, cut.LowerBound, cut.UpperBound);
		}

		private static void UpdateCoefInCutRow(SimplexSolver solver, int vidCutRow, int vidCol, Rational val)
		{
			Rational coefficient = solver.GetCoefficient(vidCutRow, vidCol);
			solver.SetCoefficient(vidCutRow, vidCol, coefficient + val);
		}

		internal int AddCut(CuttingPlane cut)
		{
			int count = _cuts.Count;
			_cuts.Add(cut);
			return count;
		}

		/// <summary>
		/// Mark the vidRow to indicate it has been used to generate a cover cut.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="vidRow"></param>
		internal void AddUsedRowCover(SimplexTask thread, int vidRow)
		{
			_listUsedRowCover.Add(vidRow);
			thread.SetRowUsedFlag(vidRow, CutKind.Cover);
		}

		/// <summary>
		/// Mark the vidRow to indicate it has been used to generate a mixed cover cut.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="vidRow"></param>
		internal void AddUsedRowMixedCover(SimplexTask thread, int vidRow)
		{
			_listUsedRowMixedCover.Add(vidRow);
			thread.SetRowUsedFlag(vidRow, CutKind.MixedCover);
		}

		/// <summary>
		/// Mark the vidRow to indicate it has been used to generate a flow cover cut.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="vidRow"></param>
		internal void AddUsedRowFlowCover(SimplexTask thread, int vidRow)
		{
			_listUsedRowFlowCover.Add(vidRow);
			thread.SetRowUsedFlag(vidRow, CutKind.MixedCover);
		}

		/// <summary>
		/// Test if the vidRow has been used to generate a cover cut.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="vidRow"></param>
		internal static bool IsUsedRowCover(SimplexTask thread, int vidRow)
		{
			return thread.HasRowUsedFlag(vidRow, CutKind.Cover);
		}

		/// <summary>
		/// Test if the vidRow has been used to generate a mixed cover cut.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="vidRow"></param>
		internal static bool IsUsedRowMixedCover(SimplexTask thread, int vidRow)
		{
			return thread.HasRowUsedFlag(vidRow, CutKind.MixedCover);
		}

		/// <summary>
		/// Test if the vidRow has been used to generate a flow cover cut.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="vidRow"></param>
		internal static bool IsUsedRowFlowCover(SimplexTask thread, int vidRow)
		{
			return thread.HasRowUsedFlag(vidRow, CutKind.FlowCover);
		}

		/// <summary>
		/// Initialize the flags in the thread by walking through the constraint train and setting
		/// the flags based on all ancestor cut pools' lists of rows used to generate cuts.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="currentNode"></param>
		private static void InitUsedRowFlags(SimplexTask thread, Node currentNode)
		{
			thread.InitRowUsedFlags();
			CuttingPlanePool cuttingPlanePool = currentNode.GetAncestorCutPool();
			while (cuttingPlanePool != null)
			{
				foreach (int item in cuttingPlanePool._listUsedRowCover)
				{
					thread.SetRowUsedFlag(item, CutKind.Cover);
				}
				foreach (int item2 in cuttingPlanePool._listUsedRowMixedCover)
				{
					thread.SetRowUsedFlag(item2, CutKind.MixedCover);
				}
				foreach (int item3 in cuttingPlanePool._listUsedRowFlowCover)
				{
					thread.SetRowUsedFlag(item3, CutKind.FlowCover);
				}
				Constraint constraint = cuttingPlanePool;
				CuttingPlanePool cuttingPlanePool2 = null;
				do
				{
					constraint = constraint.ParentConstraint;
				}
				while (constraint != null && (cuttingPlanePool2 = constraint as CuttingPlanePool) == null);
				cuttingPlanePool = cuttingPlanePool2;
			}
		}

		/// <summary>
		/// Generate cutting planes from the given simplex thread. The simplex thread must be the one that finds the
		/// optimum of the LP relaxation. In that case, the basis in the thread is valid.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="nodeLimit">The maximum number of cuts to generate (for each kind) at this node.</param>
		/// <param name="currentNode"></param>
		/// <returns></returns>
		public static bool GenerateCuts(SimplexTask thread, int nodeLimit, ref Node currentNode)
		{
			return GenerateCuts(thread, nodeLimit, ref currentNode, CutKind.GomoryFractional, thread.Solver.LpResult);
		}

		/// <summary>
		/// Generate cutting planes from the given simplex thread. The simplex thread must be the one that finds the
		/// optimum of the LP relaxation. In that case, the basis in the thread is valid.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="nodeLimit">The maximum number of cuts to generate (for each kind) at this node.</param>
		/// <param name="currentNode"></param>
		/// <param name="kinds"></param>
		/// <returns></returns>
		public static bool GenerateCuts(SimplexTask thread, int nodeLimit, ref Node currentNode, CutKind kinds)
		{
			return GenerateCuts(thread, nodeLimit, ref currentNode, kinds, thread.Solver.LpResult);
		}

		/// <summary>
		/// Generate cutting planes from the given simplex thread. The simplex thread must be the one that finds the
		/// optimum of the LP relaxation. In that case, the basis in the thread is valid.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="nodeLimit">The maximum number of cuts to generate (for each kind) at this node.</param>
		/// <param name="currentNode"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static bool GenerateCuts(SimplexTask thread, int nodeLimit, ref Node currentNode, LinearResult result)
		{
			return GenerateCuts(thread, nodeLimit, ref currentNode, CutKind.GomoryFractional, result);
		}

		/// <summary>
		/// Generate cutting planes from the given simplex thread. The simplex thread must be the one that finds the
		/// optimum of the LP relaxation. In that case, the basis in the thread is valid.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="kinds">the kind of the cuts to generate.</param>
		/// <param name="currentNode">the current node.</param>
		/// <param name="nodeLimit">The maximum number of cuts to generate (for each kind) at this node.</param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static bool GenerateCuts(SimplexTask thread, int nodeLimit, ref Node currentNode, CutKind kinds, LinearResult result)
		{
			CuttingPlanePool cuttingPlanePool;
			int num;
			bool flag;
			if (currentNode.LatestConstraint is CuttingPlanePool)
			{
				cuttingPlanePool = currentNode.LatestConstraint as CuttingPlanePool;
				num = cuttingPlanePool.Cuts.Count;
				flag = false;
			}
			else
			{
				cuttingPlanePool = new CuttingPlanePool(currentNode.LatestConstraint);
				num = 0;
				flag = true;
			}
			if (_fCoverCutGen && (kinds & (CutKind.Cover | CutKind.MixedCover | CutKind.FlowCover)) != 0)
			{
				InitUsedRowFlags(thread, currentNode);
			}
			if (_fCoverCutGen && (kinds & CutKind.Cover) != 0)
			{
				CutGenerator.Cover(thread, nodeLimit, cuttingPlanePool);
			}
			if (_fCoverCutGen && (kinds & CutKind.MixedCover) != 0)
			{
				CutGenerator.MixedCover(thread, nodeLimit, cuttingPlanePool);
			}
			if (_fCoverCutGen && (kinds & CutKind.FlowCover) != 0)
			{
				CutGenerator.FlowCover(thread, nodeLimit, cuttingPlanePool);
			}
			if ((kinds & CutKind.GomoryFractional) != 0)
			{
				CutGenerator.GomoryFractional(thread, result, nodeLimit, cuttingPlanePool);
			}
			if (_fCoverCutGen)
			{
				_ = kinds & (CutKind.Cover | CutKind.MixedCover | CutKind.FlowCover);
			}
			if (flag && num < cuttingPlanePool.Cuts.Count)
			{
				currentNode.ExtendConstraint(cuttingPlanePool);
			}
			return num < cuttingPlanePool.Cuts.Count;
		}

		/// <summary>
		/// Create a cutting plane pool and set the simplex solver and the initial cut kinds, and set the path limit (this is
		/// supposed to be called only once).
		/// </summary>
		/// <param name="parent">The parent constraint.</param>
		private CuttingPlanePool(Constraint parent)
			: base(parent)
		{
			if (parent != null)
			{
				CuttingPlanePool ancestorCutPool = GetAncestorCutPool(parent);
				if (ancestorCutPool != null)
				{
					_cPathCuts = ancestorCutPool.PathCutCount;
				}
			}
			_listUsedRowCover = new List<int>();
			_listUsedRowMixedCover = new List<int>();
			_listUsedRowFlowCover = new List<int>();
			_cuts = new List<CuttingPlane>();
			_cutRowVids = new List<int>();
			_fReset = true;
		}
	}
}
