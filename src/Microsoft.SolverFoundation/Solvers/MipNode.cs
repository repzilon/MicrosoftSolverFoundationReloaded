using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal sealed class MipNode
	{
		private MipSolver _mipSolver;

		private OptimalGoalValues _relaxationOptimalGoalValues;

		private MipNode _parent;

		private int _branchVar;

		private double _numBound;

		private bool _isLowerBound;

		private VectorDouble _tableauRow;

		private VectorDouble _rowSpace;

		private SimplexTask _dualSolver;

		private int _nodeID;

		public MipNode Parent => _parent;

		public OptimalGoalValues RelaxationOptimalGoalValues => _relaxationOptimalGoalValues;

		public bool IsLowerBound => _isLowerBound;

		public int BranchingVar => _branchVar;

		public int Level { get; set; }

		public bool HasCuts => _mipSolver.CutStore.Count(this) > 0;

		public MipSolver MipSolver => _mipSolver;

		public SimplexTask Task => _dualSolver;

		public int ID => _nodeID;

		public MipNode(MipSolver mipSolver)
			: this(null, mipSolver)
		{
		}

		public MipNode Clone(int branchVar, double bound, bool isLowerBound)
		{
			MipNode mipNode = new MipNode(this, _mipSolver);
			mipNode._isLowerBound = isLowerBound;
			mipNode._numBound = bound;
			mipNode._branchVar = branchVar;
			return mipNode;
		}

		private MipNode(MipNode parent, MipSolver mipSolver)
		{
			_nodeID = mipSolver.GetCounter();
			if (parent != null)
			{
				Level = parent.Level + 1;
				_parent = parent;
				_mipSolver = parent._mipSolver;
				if (parent.HasCuts)
				{
					SimplexReducedModel mod = new SimplexReducedModel(parent.Task.Model, _mipSolver.CutStore.Cuts(parent));
					_dualSolver = new SimplexTask(parent, mod);
				}
				else
				{
					_dualSolver = new SimplexTask(parent);
				}
			}
			else
			{
				Level = 0;
				_mipSolver = mipSolver;
				_dualSolver = mipSolver.Root;
			}
		}

		public void ClearTask()
		{
			_dualSolver = null;
			_tableauRow = null;
			_rowSpace = null;
		}

		public bool PreSolve()
		{
			SimplexReducedModel model = _dualSolver.Model;
			bool flag = model.MIPPreSolve(this);
			_dualSolver.ReInit();
			if (flag)
			{
				model.InitDbl(_dualSolver.Params);
			}
			return flag;
		}

		public LinearResult Solve()
		{
			if (_parent != null)
			{
				if (_isLowerBound)
				{
					_dualSolver.BoundManager.SetLowerBoundDbl(_branchVar, _numBound);
				}
				else
				{
					_dualSolver.BoundManager.SetUpperBoundDbl(_branchVar, _numBound);
				}
			}
			LinearResult result = _dualSolver.RunSimplex(null, restart: false);
			_relaxationOptimalGoalValues = _dualSolver.OptimalGoalValues.Clone();
			return result;
		}

		/// <summary> Generate cutting planes
		/// </summary>
		/// <returns>false if and only if the node is integer infeasible</returns>
		public bool GenerateCuts(CutStore cutStore)
		{
			if ((_mipSolver.Root.Params.CutKinds & CutKind.GomoryFractional) != 0 && cutStore.GomoryCutRound < _mipSolver.Root.Params.MixedIntegerGomoryCutRoundLimit)
			{
				if (!cutStore.AddCuts(new GomoryCutBuilder(this)))
				{
					return false;
				}
				cutStore.GomoryCutRound++;
			}
			return true;
		}

		public VectorDouble ComputeSimplexTableauRow(int ivar)
		{
			if (_tableauRow == null)
			{
				_tableauRow = new VectorDouble(Task.Model.VarLim);
			}
			else
			{
				_tableauRow.Clear();
			}
			if (_rowSpace == null)
			{
				_rowSpace = new VectorDouble(Task.Model.RowLim);
			}
			else
			{
				_rowSpace.Clear();
			}
			_rowSpace.SetCoefNonZero(ivar, 1.0);
			SimplexFactoredBasis basis = Task.Basis;
			if (basis.IsDoubleFactorizationValid)
			{
				if (basis.IsDoubleBasisPermuted)
				{
					basis.RepairPermutedDoubleBasis();
				}
				basis.InplaceSolveRow(_rowSpace);
			}
			else
			{
				basis.InplaceSolveApproxRow(_rowSpace);
			}
			Vector<double>.Iter iter = new Vector<double>.Iter(_rowSpace);
			while (iter.IsValid)
			{
				double value = iter.Value;
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(Task.Model.Matrix, iter.Rc);
				while (rowIter.IsValid)
				{
					int column = rowIter.Column;
					if (Task.Basis.GetVvk(column) > SimplexVarValKind.Basic)
					{
						double coef = _tableauRow.GetCoef(column);
						if (SlamToZero(coef, coef += value * rowIter.Approx))
						{
							_tableauRow.RemoveCoef(column);
						}
						else
						{
							_tableauRow.SetCoefNonZero(column, coef);
						}
					}
					rowIter.Advance();
				}
				iter.Advance();
			}
			return _tableauRow;
		}

		private bool SlamToZero(double numSrc, double numDst)
		{
			double num = Math.Abs(numSrc);
			double num2 = Math.Abs(numDst);
			double varZeroRatio = _dualSolver.AlgorithmDouble.VarZeroRatio;
			if (!(num2 <= varZeroRatio * num))
			{
				return num2 / num < varZeroRatio;
			}
			return true;
		}

		[Conditional("DEBUG")]
		private void ValidateRow(int basisRow)
		{
		}

		[Conditional("DEBUG")]
		internal void DumpCutRowsInUserModel()
		{
		}

		public double GetUserVarValue(int var)
		{
			SimplexReducedModel model = Task.Model;
			if (Task.AlgorithmDouble == null)
			{
				return (double)model.MapValueFromVarToVid(var, Task.AlgorithmExact.GetVarValue(var));
			}
			return model.MapValueFromVarToVid(var, Task.AlgorithmDouble.GetVarValue(var));
		}

		public double GetReducedVarValue(int var)
		{
			if (Task.AlgorithmDouble == null)
			{
				return (double)Task.AlgorithmExact.GetVarValue(var);
			}
			return Task.AlgorithmDouble.GetVarValue(var);
		}

		/// <summary> format values as a string.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "node[{0}], parent Node[{1}] \r\nset var {2} to {3} at {4}, ", _nodeID, (_parent != null) ? _parent._nodeID : 0, _mipSolver.Root.Solver.GetKeyFromIndex(_branchVar), _isLowerBound ? "Lower" : "Upper", _numBound));
			int varLim = _dualSolver.Model.VarLim;
			for (int i = 0; i < varLim; i++)
			{
				stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "\r\nvar {0} lower = {1}, upper = {2}", new object[3]
				{
					_mipSolver.Root.Solver.GetKeyFromIndex(i),
					_dualSolver.BoundManager.GetLowerBoundDbl(i),
					_dualSolver.BoundManager.GetUpperBoundDbl(i)
				}));
			}
			return stringBuilder.ToString();
		}
	}
}
