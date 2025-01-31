using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>The representation of an MILP problem in terms of rows and variables.
	/// </summary>
	/// <remarks>
	/// Rows and Variables are both identified by a key.
	/// A key can be any object. Rows and variables are also accessed via an index.
	/// Note that indices are not necessarily contiguous!
	/// </remarks>
	public class LinearModel : RowVariableModel, ILinearModel, ISolverProperties
	{
		internal sealed class Goal : ILinearGoal, IGoal, IComparable<Goal>
		{
			private bool _fEnabled;

			private bool _fMinimize;

			private LinearModel _mod;

			private int _oid;

			private int _pri;

			private int _vid;

			public int OrderIndex
			{
				get
				{
					return _oid;
				}
				set
				{
					_oid = value;
					_mod._rggoalSorted = null;
				}
			}

			public object Key => _mod.GetKeyFromIndex(_vid);

			public int Index => _vid;

			public int Priority
			{
				get
				{
					return _pri;
				}
				set
				{
					_pri = value;
					_mod._rggoalSorted = null;
				}
			}

			public bool Minimize
			{
				get
				{
					return _fMinimize;
				}
				set
				{
					_fMinimize = value;
				}
			}

			public bool Enabled
			{
				get
				{
					return _fEnabled;
				}
				set
				{
					_fEnabled = value;
				}
			}

			public Goal(LinearModel mod, int vid, int pri, bool fMinimize)
			{
				_mod = mod;
				_vid = vid;
				_pri = pri;
				_fMinimize = fMinimize;
				_fEnabled = true;
			}

			public int CompareTo(Goal goal)
			{
				if (_pri < goal._pri)
				{
					return -1;
				}
				if (_pri > goal._pri)
				{
					return 1;
				}
				if (_oid < goal._oid)
				{
					return -1;
				}
				if (_oid > goal._oid)
				{
					return 1;
				}
				return 0;
			}
		}

		/// <summary> Number of variables labeled as "basic"
		/// </summary>
		protected int m_cvidBasic;

		/// <summary> The complete coefficient matrix, including objective(s) and
		///           a variable for each row. Implicitly A.x = 0, where A is _matSrc,
		///           the dot indicates matrix product, and x is the vector of variables
		///           (user and slack).
		/// </summary>
		internal CoefMatrix _matModel;

		/// <summary> This is a CoefMatrix to record quadratic Coef
		/// </summary>
		internal CoefMatrix _qpModel;

		/// <summary> This remembers which goal variable has the QP form
		///           Right now, we only support a single goal count 
		/// </summary>
		protected int m_qpRowVar;

		/// <summary> This maps vid to Qid, if zero means not used in the quadratic.
		/// </summary>
		internal int[] _mpvidQid;

		/// <summary> This maps qid to vid, if zero means not used in the quadratic.
		/// </summary>
		internal int[] _mpqidVid;

		/// <summary> number of quadratic coefficient
		/// </summary>
		internal int _qidLim;

		/// <summary>  The mapping from vid to goal
		/// </summary>
		internal Dictionary<int, Goal> _mpvidgoal;

		internal int _oidNext;

		internal Goal[] _rggoalSorted;

		/// <summary> A list of SOS1 reference row
		/// </summary>
		internal HashSet<int> _sos1Rows;

		/// <summary> A list of SOS2 reference row
		/// </summary>
		internal HashSet<int> _sos2Rows;

		private Dictionary<int, bool> _fReusableVidRow;

		private Heap<int> _vidRowDeleted;

		internal int NonzeroCount => _matModel.EntryCount;

		/// <summary> Return true if the model is quadratic, otherwise false.
		/// </summary>
		public bool IsQuadraticModel => m_qpRowVar != -1;

		/// <summary> Is the linear model SOS?
		/// </summary>
		public virtual bool IsSpecialOrderedSet
		{
			get
			{
				if (_sos1Rows == null)
				{
					return _sos2Rows != null;
				}
				return true;
			}
		}

		/// <summary>
		/// Return the row index collection. 
		/// </summary>
		public override IEnumerable<int> RowIndices
		{
			get
			{
				try
				{
					_modelReadCount++;
					for (int rid = 0; rid < _ridLim; rid++)
					{
						if (!IsRowRemoved(_mpridvid[rid]))
						{
							yield return _mpridvid[rid];
						}
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		internal virtual int ColCount => _vidLim;

		internal virtual int EntryCount => _matModel.EntryCount;

		/// <summary>
		/// The number of non-zero coefficients. 
		/// </summary>
		public virtual int CoefficientCount => _matModel.EntryCount;

		/// <summary>
		/// The number of goals in this linear model.
		/// </summary>
		public virtual int GoalCount => _mpvidgoal.Count;

		/// <summary>
		/// Return the goal collection of this linear model. 
		/// </summary>
		public virtual IEnumerable<ILinearGoal> Goals
		{
			get
			{
				if (_rggoalSorted == null)
				{
					_rggoalSorted = new Goal[_mpvidgoal.Count];
					int num = 0;
					foreach (Goal value in _mpvidgoal.Values)
					{
						_rggoalSorted[num++] = value;
					}
					Array.Sort(_rggoalSorted);
				}
				return Statics.ImplicitCastIter<Goal, ILinearGoal>(_rggoalSorted);
			}
		}

		/// <summary>
		/// The IEqualityComparer's are for the internal dictionaries from key to row/variable.
		/// </summary>
		public LinearModel(IEqualityComparer<object> cmp)
			: base(cmp)
		{
			InitCore(0, 0, 0);
		}

		private void InitCore(int cvid, int crid, int cent)
		{
			_matModel = new CoefMatrix(0, 0, cent, fExact: true, fDouble: false);
			_mpvidgoal = new Dictionary<int, Goal>();
			_oidNext = 0;
			_rggoalSorted = null;
			m_qpRowVar = -1;
		}

		private void InitQuadraticModel()
		{
			if (_qpModel == null)
			{
				_qpModel = new CoefMatrix(0, 0, 0, fExact: false, fDouble: true);
				_mpvidQid = new int[_vidLim];
				_qidLim = 1;
				_mpqidVid = new int[_qidLim];
			}
		}

		/// <summary> Return true if the variable referenced by vid is part of a quadratic row, otherwise false
		/// </summary>
		/// <param name="vidVar"></param>
		/// <returns></returns>
		public bool IsQuadraticVariable(int vidVar)
		{
			if (_mpvidQid == null)
			{
				return false;
			}
			return _mpvidQid[vidVar] != 0;
		}

		/// <summary> Allocate clean data structures for a new model.
		/// </summary>
		protected virtual void InitModel(IEqualityComparer<object> comparer, int cvid, int crid, int cent)
		{
			InitCore(cvid, crid, cent);
		}

		/// <summary> Inject the given LinearModel into this model, removing all previous information.
		/// </summary>
		public virtual void LoadLinearModel(ILinearModel mod)
		{
			if (mod == null)
			{
				throw new ArgumentNullException("mod", Resources.ModelCouldNotBeNull);
			}
			if (mod == this)
			{
				throw new InvalidOperationException(Resources.LoadLinearModelPassedThis);
			}
			int keyCount = mod.KeyCount;
			InitModel(mod.KeyComparer, keyCount, mod.RowCount, mod.CoefficientCount);
			for (int i = 0; i < keyCount; i++)
			{
				object keyFromIndex = mod.GetKeyFromIndex(_vidLim);
				if (mod.IsRow(i) ? (!AddRow(keyFromIndex, out var vid)) : (!AddVariable(keyFromIndex, out vid)))
				{
					throw new InvalidDataException(Resources.SourceModelContainsDuplicateKeys);
				}
				mod.GetBounds(i, out var numLo, out var numHi);
				SetBounds(vid, numLo, numHi);
				SetValue(vid, mod.GetValue(vid));
				SetBasic(vid, mod.GetBasic(i));
				SetIgnoreBounds(vid, mod.GetIgnoreBounds(i));
				bool integrality = mod.GetIntegrality(i);
				if (integrality)
				{
					SetIntegrality(vid, integrality);
				}
			}
			if (mod.IsSpecialOrderedSet)
			{
				foreach (int specialOrderedSetTypeRowIndex in mod.GetSpecialOrderedSetTypeRowIndexes(SpecialOrderedSetType.SOS1))
				{
					if (_sos1Rows == null)
					{
						_sos1Rows = new HashSet<int>();
					}
					_sos1Rows.Add(specialOrderedSetTypeRowIndex);
				}
				foreach (int specialOrderedSetTypeRowIndex2 in mod.GetSpecialOrderedSetTypeRowIndexes(SpecialOrderedSetType.SOS2))
				{
					if (_sos2Rows == null)
					{
						_sos2Rows = new HashSet<int>();
					}
					_sos2Rows.Add(specialOrderedSetTypeRowIndex2);
				}
			}
			for (int j = 0; j < _ridLim; j++)
			{
				foreach (LinearEntry rowEntry in mod.GetRowEntries(_mpridvid[j]))
				{
					_matModel.SetCoefExact(j, rowEntry.Index, rowEntry.Value);
				}
			}
			foreach (ILinearGoal goal in mod.Goals)
			{
				ILinearGoal linearGoal = AddGoal(goal.Index, goal.Priority, goal.Minimize);
				linearGoal.Enabled = goal.Enabled;
			}
			if (!mod.IsQuadraticModel)
			{
				return;
			}
			LinearModel linearModel = (LinearModel)mod;
			_qpModel = new CoefMatrix(linearModel._qpModel.RowCount, linearModel._qpModel.ColCount, linearModel._qpModel.EntryCount, fExact: false, fDouble: true);
			_mpvidQid = new int[linearModel._vidLim];
			_qidLim = linearModel._qidLim;
			_mpqidVid = new int[linearModel._qidLim];
			Array.Copy(linearModel._mpvidQid, _mpvidQid, _vidLim);
			Array.Copy(linearModel._mpqidVid, _mpqidVid, _qidLim);
			for (int k = 0; k < _qpModel.RowCount; k++)
			{
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(linearModel._qpModel, k);
				while (rowIter.IsValid)
				{
					_qpModel.SetCoefDouble(k, rowIter.Column, rowIter.Approx);
					rowIter.Advance();
				}
			}
			m_qpRowVar = linearModel.m_qpRowVar;
		}

		internal override int AllocVid()
		{
			int result = base.AllocVid();
			_matModel.ResizeCols(_vidLim);
			if (_mpvidQid != null)
			{
				EnsureArraySize(ref _mpvidQid, _vidLim);
				EnsureArraySize(ref _mpqidVid, _qidLim);
			}
			return result;
		}

		internal override int AllocRid()
		{
			int result = base.AllocRid();
			_matModel.ResizeRows(_ridLim);
			return result;
		}

		internal virtual int AllocQid()
		{
			int result = _qidLim++;
			_qpModel.Resize(_qidLim - 1, _qidLim - 1);
			EnsureArraySize(ref _mpvidQid, _vidLim);
			EnsureArraySize(ref _mpqidVid, _qidLim);
			return result;
		}

		internal bool IsActiveGoal(int vid)
		{
			if (_mpvidgoal.TryGetValue(vid, out var value))
			{
				return value.Enabled;
			}
			return false;
		}

		/// <summary>
		/// Test if a row has been deleted.
		/// </summary>
		/// <param name="vidRow"></param>
		/// <returns></returns>
		internal bool IsRowRemoved(int vidRow)
		{
			if (_fReusableVidRow == null)
			{
				return false;
			}
			return _fReusableVidRow.ContainsKey(vidRow);
		}

		/// <summary>
		/// Delete the row represented by vidRow.
		/// </summary>
		/// <param name="vidRow"></param>
		/// <returns></returns>
		internal virtual bool RemoveRow(int vidRow)
		{
			PreChange();
			SetIgnoreBounds(vidRow, ignore: true);
			if (IsRowRemoved(vidRow))
			{
				return false;
			}
			m_mpkeyvid.Remove(_mpvidvi[vidRow].Key);
			if (_vidRowDeleted == null)
			{
				_vidRowDeleted = new Heap<int>((int int1, int int2) => int1 > int2);
				_fReusableVidRow = new Dictionary<int, bool>();
			}
			_fReusableVidRow.Add(vidRow, value: true);
			_vidRowDeleted.Add(vidRow);
			return true;
		}

		/// <summary>
		/// If the model already includes a row referenced by key, this sets vid to the row’s index and returns false. 
		/// Otherwise, if the model already includes a user variable referenced by key, this sets vid to -1 and returns false. 
		/// Otherwise, this adds a new row associated with key to the model, assigns the next available index to the new row, sets vid to this index, 
		/// and returns true.
		/// </summary>
		/// <param name="key">a key for the row</param>
		/// <param name="vid">a row variable index </param>
		/// <returns>true if added successfully, otherwise false</returns>
		public override bool AddRow(object key, out int vid)
		{
			PreChange();
			if (key != null && m_mpkeyvid.TryGetValue(key, out vid))
			{
				if (!_mpvidvi[vid].IsRow)
				{
					vid = -1;
				}
				return false;
			}
			if (_vidRowDeleted != null && _vidRowDeleted.Count > 0)
			{
				vid = _vidRowDeleted.Pop();
				_fReusableVidRow.Remove(vid);
				_matModel.ClearRow(_mpvidvi[vid].Rid);
				_matModel.SetCoefExact(_mpvidvi[vid].Rid, vid, -1);
				m_mpkeyvid.Add(key, vid);
				_mpvidvi[vid].UpdateKey(key);
				ref Rational reference = ref _mpvidnumHi[vid];
				reference = Rational.PositiveInfinity;
				ref Rational reference2 = ref _mpvidnumLo[vid];
				reference2 = Rational.NegativeInfinity;
				ref Rational reference3 = ref _mpvidnum[vid];
				reference3 = Rational.Indeterminate;
				SetBasic(vid, fBasic: false);
				SetIntegrality(vid, integer: false);
				SetIgnoreBounds(vid, ignore: false);
				return true;
			}
			vid = AllocVid();
			int num = AllocRid();
			ref VarInfo reference4 = ref _mpvidvi[vid];
			reference4 = new VarInfo(key, vid, num);
			if (key != null)
			{
				m_mpkeyvid.Add(key, vid);
			}
			_mpridvid[num] = vid;
			_matModel.SetCoefExact(num, vid, -1);
			return true;
		}

		/// <summary> Add a reference row for a SOS set. Each SOS set has one reference row.
		/// </summary>
		/// <param name="key">a SOS key</param>
		/// <param name="sos">type of SOS</param>
		/// <param name="vidRow">the vid of the reference row</param>
		/// <returns></returns>
		public virtual bool AddRow(object key, SpecialOrderedSetType sos, out int vidRow)
		{
			if (sos != 0 && sos != SpecialOrderedSetType.SOS2)
			{
				throw new NotImplementedException();
			}
			vidRow = -1;
			if (AddRow(key, out vidRow))
			{
				if (sos == SpecialOrderedSetType.SOS1)
				{
					if (_sos1Rows == null)
					{
						_sos1Rows = new HashSet<int>();
					}
					_sos1Rows.Add(vidRow);
				}
				else
				{
					if (_sos2Rows == null)
					{
						_sos2Rows = new HashSet<int>();
					}
					_sos2Rows.Add(vidRow);
				}
				return true;
			}
			return false;
		}

		/// <summary> Return a list of SOS1 or SOS2 row indexes.
		/// </summary>
		/// <param name="sosType"></param>
		/// <returns></returns>
		public virtual IEnumerable<int> GetSpecialOrderedSetTypeRowIndexes(SpecialOrderedSetType sosType)
		{
			switch (sosType)
			{
			case SpecialOrderedSetType.SOS1:
				if (_sos1Rows == null)
				{
					break;
				}
				{
					foreach (int sos1Row in _sos1Rows)
					{
						yield return sos1Row;
					}
					break;
				}
			case SpecialOrderedSetType.SOS2:
				if (_sos2Rows == null)
				{
					break;
				}
				{
					foreach (int sos2Row in _sos2Rows)
					{
						yield return sos2Row;
					}
					break;
				}
			default:
				throw new NotImplementedException();
			}
		}

		internal virtual int GetRowIndexFromVid(int vid)
		{
			ValidateVid(vid);
			return _mpvidvi[vid].Rid;
		}

		/// <summary>
		/// The SetBasic method sets the basis status for a variable. The default basis status for a variable is false. 
		/// The SimplexSolver class updates these flags after a solve attempt.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <param name="fBasic">whether set it to a basic variable</param>
		public virtual void SetBasic(int vid, bool fBasic)
		{
			PreChange();
			ValidateVid(vid);
			AssignVidFlag(vid, VidFlags.Basic, fBasic, ref m_cvidBasic);
		}

		/// <summary>
		/// Get the basis status for this variable.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>true if this variable is a basic variable. otherwise false</returns>
		public virtual bool GetBasic(int vid)
		{
			ValidateVid(vid);
			return HasFlag(vid, VidFlags.Basic);
		}

		internal bool GetBoolean(int vid)
		{
			ValidateVid(vid);
			if (HasFlag(vid, VidFlags.Integer) && _mpvidnumLo[vid] == 0)
			{
				return _mpvidnumHi[vid] == 1;
			}
			return false;
		}

		/// <summary>
		/// Get the value state of this variable. 
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>variable state</returns>
		public virtual LinearValueState GetValueState(int vid)
		{
			ValidateVid(vid);
			Rational rational = _mpvidnum[vid];
			if (!rational.IsFinite)
			{
				return LinearValueState.Invalid;
			}
			if (rational < _mpvidnumLo[vid])
			{
				return LinearValueState.Below;
			}
			if (rational > _mpvidnumHi[vid])
			{
				return LinearValueState.Above;
			}
			if (rational == _mpvidnumLo[vid])
			{
				return LinearValueState.AtLower;
			}
			if (rational == _mpvidnumHi[vid])
			{
				return LinearValueState.AtUpper;
			}
			return LinearValueState.Between;
		}

		/// <summary> check if the row is SOS row
		/// </summary>
		/// <param name="vidRow">assumed to be vid of a row</param>
		/// <param name="sosType"></param>
		/// <returns></returns>
		private bool IsSosRow(int vidRow, SpecialOrderedSetType sosType)
		{
			switch (sosType)
			{
			case SpecialOrderedSetType.SOS1:
				if (_sos1Rows != null)
				{
					return _sos1Rows.Contains(vidRow);
				}
				return false;
			case SpecialOrderedSetType.SOS2:
				if (_sos2Rows != null)
				{
					return _sos2Rows.Contains(vidRow);
				}
				return false;
			default:
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Set the coefficient of the A matrix in the linear model. If num is zero, the entry is removed. 
		/// </summary>
		/// <remarks>Coefficient cannot be zero for a row of special ordered set type 2</remarks>
		/// <param name="vidRow">a row id </param>
		/// <param name="vidVar">a column/variable id</param>
		/// <param name="num">a value</param>
		public virtual void SetCoefficient(int vidRow, int vidVar, Rational num)
		{
			PreChange();
			ValidateRowVid(vidRow);
			ValidateVid(vidVar);
			ValidateCoefficient(num);
			if (_mpvidvi[vidVar].IsRow && (vidRow != vidVar || num.IsZero))
			{
				throw new InvalidOperationException(Resources.CanTChangeCoefficientsForRowVariables);
			}
			int rid = _mpvidvi[vidRow].Rid;
			if (num.IsZero)
			{
				if (IsSosRow(vidRow, SpecialOrderedSetType.SOS2) || IsSosRow(vidRow, SpecialOrderedSetType.SOS1))
				{
					throw new InvalidOperationException(Resources.CoefficientForVariableInASOSRowCannotBeZero);
				}
				_matModel.RemoveCoef(rid, vidVar);
			}
			else
			{
				_matModel.SetCoefExact(rid, vidVar, num);
			}
		}

		/// <summary>
		/// Set the coefficient of the Q matrix on the objective row. If num is zero, the entry is removed. 
		/// This is used for quadratic terms on the objective row.
		/// </summary>
		/// <param name="vidRow">a goal row</param>
		/// <param name="num">a value </param>
		/// <param name="vidVar1">a column/variable id</param>
		/// <param name="vidVar2">another column/variable id</param>
		public virtual void SetCoefficient(int vidRow, Rational num, int vidVar1, int vidVar2)
		{
			InitQuadraticModel();
			PreChange();
			ValidateRowVid(vidRow);
			ValidateVid(vidVar1);
			ValidateVid(vidVar2);
			ValidateCoefficient(num);
			if (m_qpRowVar != -1 && m_qpRowVar != vidRow)
			{
				throw new NotSupportedException(Resources.QuadraticModelOnlySupportsASingleGoalRowCanTChangeToADifferentGoalRow);
			}
			m_qpRowVar = vidRow;
			if (_mpvidQid[vidVar1] == 0)
			{
				_mpvidQid[vidVar1] = AllocQid();
			}
			int num2 = _mpvidQid[vidVar1] - 1;
			_mpqidVid[num2] = vidVar1;
			if (_mpvidQid[vidVar2] == 0)
			{
				_mpvidQid[vidVar2] = AllocQid();
			}
			int num3 = _mpvidQid[vidVar2] - 1;
			_mpqidVid[num3] = vidVar2;
			SetCoefficient(_qpModel, num, num2, num3);
			if (num.IsZero && _qpModel.EntryCount == 0)
			{
				m_qpRowVar = -1;
			}
		}

		internal static void SetCoefficient(CoefMatrix coefMatrix, Rational num, int qid1, int qid2)
		{
			if (num == 0)
			{
				coefMatrix.RemoveCoef(qid1, qid2);
				coefMatrix.RemoveCoef(qid2, qid1);
				return;
			}
			double num2 = (double)num;
			if (qid1 == qid2)
			{
				num2 = 2.0 * num2;
			}
			coefMatrix.SetCoefDouble(qid1, qid2, num2);
			coefMatrix.SetCoefDouble(qid2, qid1, num2);
		}

		/// <summary>
		/// Return the coefficient of the A matrix in the linear model.
		/// </summary>
		/// <param name="vidRow">a row id</param>
		/// <param name="vidVar">a column/variable id</param>
		/// <returns>a coefficient value</returns>
		public virtual Rational GetCoefficient(int vidRow, int vidVar)
		{
			ValidateRowVid(vidRow);
			ValidateVid(vidVar);
			int rid = _mpvidvi[vidRow].Rid;
			return _matModel.GetCoefExact(rid, vidVar);
		}

		/// <summary>
		/// Return the coefficient of the Q matrix on the objective row.
		/// </summary>
		/// <param name="goalRow">a goal row</param>
		/// <param name="vidVar1">a column/variable id</param>
		/// <param name="vidVar2">another column/variable id</param>
		/// <returns>a coefficient value</returns>
		public virtual Rational GetCoefficient(int goalRow, int vidVar1, int vidVar2)
		{
			if (goalRow != m_qpRowVar)
			{
				return Rational.Zero;
			}
			ValidateRowVid(goalRow);
			ValidateVid(vidVar1);
			ValidateVid(vidVar2);
			if (_mpvidQid[vidVar1] == 0 || _mpvidQid[vidVar2] == 0)
			{
				return Rational.Zero;
			}
			double coefDouble = _qpModel.GetCoefDouble(_mpvidQid[vidVar1] - 1, _mpvidQid[vidVar2] - 1);
			if (vidVar1 == vidVar2)
			{
				return coefDouble / 2.0;
			}
			return coefDouble;
		}

		/// <summary>
		/// Return the number of non-zero coefficients for the given row index
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <returns>number of non-zero entries</returns>
		public virtual int GetRowEntryCount(int vid)
		{
			ValidateRowVid(vid);
			return _matModel.RowEntryCount(_mpvidvi[vid].Rid);
		}

		/// <summary>
		/// Return a collection of non-zero variable entries
		/// </summary>
		/// <param name="vidRow"></param>
		/// <returns>the variable collection</returns>
		public IEnumerable<LinearEntry> GetRowEntries(int vidRow)
		{
			ValidateRowVid(vidRow);
			return GetRowValues(_mpvidvi[vidRow].Rid);
		}

		/// <summary> Get the entries for this row, including vid, key, and coeff value.
		/// </summary>
		internal virtual IEnumerable<LinearEntry> GetRowValues(int rid)
		{
			try
			{
				_modelReadCount++;
				CoefMatrix.RowIter rit = new CoefMatrix.RowIter(_matModel, rid);
				LinearEntry ent = default(LinearEntry);
				while (rit.IsValid)
				{
					if (_mpvidvi[rit.Column].Rid == -1)
					{
						ent.Index = rit.Column;
						ent.Key = _mpvidvi[ent.Index].Key;
						ent.Value = rit.Exact;
						yield return ent;
					}
					rit.Advance();
				}
			}
			finally
			{
				_modelReadCount--;
			}
		}

		/// <summary>
		/// Return a collection of non-zero variable entries on the
		/// quadratic row.
		/// </summary>
		/// <param name="vidRow"></param>
		/// <returns>the variable collection</returns>
		public virtual IEnumerable<QuadraticEntry> GetRowQuadraticEntries(int vidRow)
		{
			ValidateRowVid(vidRow);
			if (_qpModel == null || vidRow != m_qpRowVar)
			{
				yield break;
			}
			try
			{
				_modelReadCount++;
				QuadraticEntry ent = default(QuadraticEntry);
				for (int qid1 = 0; qid1 < _qidLim - 1; qid1++)
				{
					CoefMatrix.RowIter rit = new CoefMatrix.RowIter(_qpModel, qid1);
					while (rit.IsValid)
					{
						if (rit.Column <= qid1)
						{
							ent.Index1 = _mpqidVid[rit.Column];
							ent.Key1 = _mpvidvi[ent.Index1].Key;
							ent.Index2 = _mpqidVid[qid1];
							ent.Key2 = _mpvidvi[ent.Index2].Key;
							if (rit.Column == qid1)
							{
								ent.Value = rit.Approx / 2.0;
							}
							else
							{
								ent.Value = rit.Approx;
							}
							yield return ent;
						}
						rit.Advance();
					}
				}
			}
			finally
			{
				_modelReadCount--;
			}
		}

		/// <summary> Get column entries not including entries in goal rows.
		/// </summary>
		internal virtual IEnumerable<LinearEntry> GetColValues(int cid)
		{
			try
			{
				_modelReadCount++;
				CoefMatrix.ColIter cit = new CoefMatrix.ColIter(_matModel, cid);
				LinearEntry ent = default(LinearEntry);
				while (cit.IsValid)
				{
					int vid = _mpridvid[cit.Row];
					if (!_mpvidgoal.ContainsKey(vid))
					{
						ent.Index = _mpridvid[cit.Row];
						ent.Key = _mpvidvi[cid].Key;
						ent.Value = cit.Exact;
						yield return ent;
					}
					cit.Advance();
				}
			}
			finally
			{
				_modelReadCount--;
			}
		}

		/// <summary> Return the number of non-zero coefficients for the given variable/column index.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>number of non-zero entries</returns>
		public virtual int GetVariableEntryCount(int vid)
		{
			ValidateVid(vid);
			return _matModel.ColEntryCount(vid);
		}

		/// <summary> Return a collection of non-zero column entries, including any goal-row entries.
		/// </summary>
		/// <param name="vidVar">a variable index</param>
		/// <returns>number of non-zero entries</returns>
		public virtual IEnumerable<LinearEntry> GetVariableEntries(int vidVar)
		{
			ValidateVid(vidVar);
			try
			{
				_modelReadCount++;
				CoefMatrix.ColIter cit = new CoefMatrix.ColIter(_matModel, vidVar);
				LinearEntry ent = default(LinearEntry);
				while (cit.IsValid)
				{
					ent.Index = _mpridvid[cit.Row];
					ent.Key = _mpvidvi[ent.Index].Key;
					ent.Value = cit.Exact;
					yield return ent;
					cit.Advance();
				}
			}
			finally
			{
				_modelReadCount--;
			}
		}

		/// <summary>
		/// Mark a row as a goal row.
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <param name="pri">the priority of a goal</param>
		/// <param name="fMinimize">whether to minimize the goal row</param>
		/// <returns></returns>
		public virtual ILinearGoal AddGoal(int vid, int pri, bool fMinimize)
		{
			PreChange();
			ValidateVid(vid);
			_mpvidgoal.Remove(vid);
			Goal goal = new Goal(this, vid, pri, fMinimize);
			_mpvidgoal.Add(vid, goal);
			goal.OrderIndex = _oidNext++;
			_rggoalSorted = null;
			return goal;
		}

		/// <summary>
		/// Clear all the goals. 
		/// </summary>
		public virtual void ClearGoals()
		{
			PreChange();
			_mpvidgoal.Clear();
			_rggoalSorted = null;
			m_qpRowVar = -1;
		}

		/// <summary>
		/// Remove a goal row.
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <returns>true if the goal is removed. otherwise false</returns>
		public virtual bool RemoveGoal(int vid)
		{
			PreChange();
			if (!_mpvidgoal.Remove(vid))
			{
				return false;
			}
			_rggoalSorted = null;
			if (m_qpRowVar == vid)
			{
				m_qpRowVar = -1;
				using (Dictionary<int, Goal>.Enumerator enumerator = _mpvidgoal.GetEnumerator())
				{
					if (enumerator.MoveNext())
					{
						m_qpRowVar = enumerator.Current.Key;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Check if a row id is a goal row. 
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <returns>true if this a goal row. otherwise false</returns>
		public virtual bool IsGoal(int vid)
		{
			ValidateVid(vid);
			return _mpvidgoal.ContainsKey(vid);
		}

		/// <summary>
		/// Check if a row id is a goal. If true, return the goal entry.
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <param name="goal">return the goal entry</param>
		/// <returns>true if a goal row. otherwise false</returns>
		public virtual bool IsGoal(int vid, out ILinearGoal goal)
		{
			ValidateVid(vid);
			if (_mpvidgoal.TryGetValue(vid, out var value))
			{
				goal = value;
				return true;
			}
			goal = null;
			return false;
		}

		/// <summary>
		/// Return a goal entry if the row id is a goal.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>A goal entry. Null if not a goal row</returns>
		public virtual ILinearGoal GetGoalFromIndex(int vid)
		{
			ValidateVid(vid);
			if (_mpvidgoal.TryGetValue(vid, out var value))
			{
				return value;
			}
			return null;
		}

		/// <summary>Preallocate storage for the model.
		/// </summary>
		/// <param name="cols"></param>
		/// <param name="rows"></param>
		/// <param name="nonzeroes"></param>
		[SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "nonzeroes")]
		[SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Preallocate")]
		[SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "nonzeros")]
		public virtual void Preallocate(int cols, int rows, int nonzeroes)
		{
			InitCore(cols, rows, nonzeroes);
		}
	}
}
