using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// RowVariableModel is the implementation of a basic optimization model 
	/// consisting of decision variables and rows.
	/// </summary>
	public abstract class RowVariableModel : IRowVariableModel
	{
		internal struct VarInfo
		{
			private object _key;

			private readonly int _vid;

			private readonly int _rid;

			public bool IsRow => _rid >= 0;

			public object Key => _key;

			public int Vid => _vid;

			public int Rid => _rid;

			public VarInfo(object key, int vid)
			{
				_key = key;
				_vid = vid;
				_rid = -1;
			}

			public VarInfo(object key, int vid, int rid)
			{
				_key = key;
				_vid = vid;
				_rid = rid;
			}

			internal void UpdateKey(object key)
			{
				_key = key;
			}
		}

		/// <summary>
		/// variable type flags 
		/// </summary>
		[Flags]
		internal enum VidFlags : byte
		{
			/// <summary>
			/// integer variable type 
			/// </summary>
			Integer = 1,
			/// <summary>
			/// Basic variable type 
			/// </summary>
			Basic = 2,
			/// <summary>
			/// variable without bounds ignored
			/// </summary>
			IgnoreBounds = 4,
			/// <summary>
			/// Conic variable type
			/// </summary>
			Conic = 8
		}

		/// <summary> Number of rows
		/// </summary>
		internal int _ridLim;

		/// <summary> Number of variables + rows
		/// </summary>
		internal int _vidLim;

		/// <summary> Number of integer variables
		/// </summary>
		protected int m_cvidInt;

		/// <summary> Map from key to variable index
		/// </summary>
		protected Dictionary<object, int> m_mpkeyvid;

		/// <summary> Map from variable index to VarInfo
		/// </summary>
		internal VarInfo[] _mpvidvi;

		/// <summary> Map from variable index to lower bound
		/// </summary>
		internal Rational[] _mpvidnumLo;

		/// <summary> Map from variable index to upper bound
		/// </summary>
		internal Rational[] _mpvidnumHi;

		/// <summary> Map from variable index to current value
		/// </summary>
		internal Rational[] _mpvidnum;

		/// <summary> Map from variable index to flags
		/// </summary>
		internal VidFlags[] _mpvidflags;

		/// <summary> Map from row index to variable index
		/// </summary>
		internal int[] _mpridvid;

		internal int _modelReadCount;

		/// <summary>
		/// Used for row or variable key comparison 
		/// </summary>
		public virtual IEqualityComparer<object> KeyComparer => m_mpkeyvid.Comparer;

		/// <summary>
		/// The number of keys in the model.
		/// </summary>
		public virtual int KeyCount => _vidLim;

		/// <summary>
		/// Return the variable and row key collection.
		/// </summary>
		public virtual IEnumerable<object> Keys
		{
			get
			{
				try
				{
					_modelReadCount++;
					for (int vid = 0; vid < _vidLim; vid++)
					{
						yield return _mpvidvi[vid].Key;
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary>
		/// Return the variable index collection.
		/// </summary>
		public virtual IEnumerable<int> Indices
		{
			get
			{
				try
				{
					_modelReadCount++;
					for (int vid = 0; vid < _vidLim; vid++)
					{
						yield return vid;
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary>
		/// Return the row key collection. 
		/// </summary>
		public virtual IEnumerable<object> RowKeys
		{
			get
			{
				try
				{
					_modelReadCount++;
					for (int rid = 0; rid < _ridLim; rid++)
					{
						yield return _mpvidvi[_mpridvid[rid]].Key;
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary>
		/// Return the row index collection. 
		/// </summary>
		public virtual IEnumerable<int> RowIndices
		{
			get
			{
				try
				{
					_modelReadCount++;
					for (int rid = 0; rid < _ridLim; rid++)
					{
						yield return _mpridvid[rid];
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary>
		/// The number of rows in the model.
		/// </summary>
		public virtual int RowCount => _ridLim;

		/// <summary>
		/// Return the variable count .
		/// </summary>
		public virtual int VariableCount => _vidLim - _ridLim;

		/// <summary>
		/// Return the variable key collection. 
		/// </summary>
		public virtual IEnumerable<object> VariableKeys
		{
			get
			{
				try
				{
					_modelReadCount++;
					for (int vid = 0; vid < _vidLim; vid++)
					{
						if (!_mpvidvi[vid].IsRow)
						{
							yield return _mpvidvi[vid].Key;
						}
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary>
		/// Return the variable index collection.
		/// </summary>
		public virtual IEnumerable<int> VariableIndices
		{
			get
			{
				try
				{
					_modelReadCount++;
					for (int vid = 0; vid < _vidLim; vid++)
					{
						if (!_mpvidvi[vid].IsRow)
						{
							yield return vid;
						}
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary>
		/// The number of integer variables.
		/// </summary>
		public virtual int IntegerIndexCount => m_cvidInt;

		/// <summary> Return true if the model is mip, otherwise false.
		/// </summary>
		public bool IsMipModel => m_cvidInt > 0;

		/// <summary>
		/// The IEqualityComparer's are for the internal dictionaries from key to row/variable.
		/// </summary>
		protected RowVariableModel(IEqualityComparer<object> comparer)
		{
			InitCore(comparer, 0, 0, 0);
		}

		private void InitCore(IEqualityComparer<object> cmp, int cvid, int crid, int cent)
		{
			if (cvid < 20)
			{
				cvid = 20;
			}
			if (crid < 20)
			{
				crid = 20;
			}
			if (cent < 100)
			{
				cent = 100;
			}
			_ridLim = 0;
			_vidLim = 0;
			m_mpkeyvid = new Dictionary<object, int>(cmp);
			_mpvidvi = new VarInfo[cvid];
			_mpvidnumLo = new Rational[cvid];
			_mpvidnumHi = new Rational[cvid];
			_mpvidnum = new Rational[cvid];
			_mpvidflags = new VidFlags[cvid];
			_mpridvid = new int[crid];
			_modelReadCount = 0;
		}

		internal virtual void EnsureArraySize<T>(ref T[] rgv, int cv)
		{
			Statics.EnsureArraySize(ref rgv, cv);
		}

		internal virtual int AllocVid()
		{
			int num = _vidLim++;
			EnsureArraySize(ref _mpvidvi, _vidLim);
			EnsureArraySize(ref _mpvidnumLo, _vidLim);
			EnsureArraySize(ref _mpvidnumHi, _vidLim);
			EnsureArraySize(ref _mpvidnum, _vidLim);
			EnsureArraySize(ref _mpvidflags, _vidLim);
			ref Rational reference = ref _mpvidnumHi[num];
			reference = Rational.PositiveInfinity;
			ref Rational reference2 = ref _mpvidnumLo[num];
			reference2 = Rational.NegativeInfinity;
			ref Rational reference3 = ref _mpvidnum[num];
			reference3 = Rational.Indeterminate;
			return num;
		}

		internal virtual int AllocRid()
		{
			int result = _ridLim++;
			EnsureArraySize(ref _mpridvid, _ridLim);
			return result;
		}

		internal virtual void ValidateRowVid(int vid)
		{
			if (0 > vid || vid >= _vidLim || !_mpvidvi[vid].IsRow)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownRowVariableIndex, new object[1] { vid }));
			}
		}

		internal virtual void ValidateVid(int vid)
		{
			if (0 > vid || vid >= _vidLim)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { vid }));
			}
		}

		internal virtual void ValidateCoefficient(Rational num)
		{
			if (num.IsIndeterminate || num.IsInfinite)
			{
				throw new ArgumentException(Resources.InvalidNumber, "num");
			}
		}

		internal virtual void ValidateCoefficient(double num)
		{
			if (double.IsNaN(num))
			{
				throw new ArgumentException(Resources.InvalidNumber, "num");
			}
		}

		internal virtual void ValidateBounds(Rational numLo, Rational numHi)
		{
			if (numLo.IsIndeterminate || numHi.IsIndeterminate || numHi < numLo)
			{
				throw new ArgumentException(Resources.InvalidBounds);
			}
		}

		internal virtual void PreChange()
		{
			if (_modelReadCount > 0)
			{
				throw new InvalidOperationException(Resources.ModelShouldNotBeEditedWhileEnumeratingRowsOrVariables);
			}
		}

		/// <summary>
		/// M4 internal
		/// </summary>
		/// <param name="vid"></param>
		/// <param name="flag"></param>
		/// <returns></returns>
		internal bool HasFlag(int vid, VidFlags flag)
		{
			return (_mpvidflags[vid] & flag) != 0;
		}

		/// <summary>
		/// M4 internal
		/// </summary>
		/// <param name="vid"></param>
		/// <param name="flag"></param>
		internal void SetFlag(int vid, VidFlags flag)
		{
			_mpvidflags[vid] |= flag;
		}

		/// <summary>
		/// M4 internal
		/// </summary>
		/// <param name="vid"></param>
		/// <param name="flag"></param>
		internal void ClearFlag(int vid, VidFlags flag)
		{
			_mpvidflags[vid] &= (VidFlags)(byte)(~(int)flag);
		}

		/// <summary>
		/// M4 internal
		/// </summary>
		/// <param name="vid"></param>
		/// <param name="flag"></param>
		/// <param name="fSet"></param>
		/// <param name="cvid"></param>
		internal void AssignVidFlag(int vid, VidFlags flag, bool fSet, ref int cvid)
		{
			if (HasFlag(vid, flag))
			{
				if (!fSet)
				{
					cvid--;
					ClearFlag(vid, flag);
				}
			}
			else if (fSet)
			{
				cvid++;
				SetFlag(vid, flag);
			}
		}

		/// <summary>
		/// Try to get the variable index based on the key.
		/// </summary>
		/// <param name="key">the key value </param>
		/// <param name="vid">the variable index </param>
		/// <returns>true if the variable exists, otherwise false</returns>
		public virtual bool TryGetIndexFromKey(object key, out int vid)
		{
			if (m_mpkeyvid.TryGetValue(key, out vid))
			{
				return true;
			}
			vid = -1;
			return false;
		}

		/// <summary>
		/// Maps the variable index from the key. If not found, KeyNotFoundException will be thrown.
		/// </summary>
		/// <param name="key"></param>
		/// <returns>variable index </returns>
		public virtual int GetIndexFromKey(object key)
		{
			return m_mpkeyvid[key];
		}

		/// <summary>
		/// Map from the variable index to the key. If not found, ArgumentException will be thrown.
		/// </summary>
		/// <param name="vid"></param>
		/// <returns></returns>
		public virtual object GetKeyFromIndex(int vid)
		{
			ValidateVid(vid);
			return _mpvidvi[vid].Key;
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
		public virtual bool AddRow(object key, out int vid)
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
			vid = AllocVid();
			int num = AllocRid();
			ref VarInfo reference = ref _mpvidvi[vid];
			reference = new VarInfo(key, vid, num);
			if (key != null)
			{
				m_mpkeyvid.Add(key, vid);
			}
			_mpridvid[num] = vid;
			return true;
		}

		/// <summary>
		/// Validate if the index is a row index.
		/// </summary>
		/// <param name="vid">row index</param>
		/// <returns>true if a row otherwise false</returns>
		public virtual bool IsRow(int vid)
		{
			ValidateVid(vid);
			return _mpvidvi[vid].IsRow;
		}

		/// <summary>
		/// Adjusts whether the bounds of a vid should be respected or ignored during solving. 
		/// By default, bounds are respected.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <param name="ignore">whether to ignore the bounds</param>
		public virtual void SetIgnoreBounds(int vid, bool ignore)
		{
			PreChange();
			ValidateVid(vid);
			if (ignore)
			{
				SetFlag(vid, VidFlags.IgnoreBounds);
			}
			else
			{
				ClearFlag(vid, VidFlags.IgnoreBounds);
			}
		}

		/// <summary>
		/// Get the flag whether is bound is ignored.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>true if bounds are ignored, otherwise false</returns>
		public virtual bool GetIgnoreBounds(int vid)
		{
			ValidateVid(vid);
			return HasFlag(vid, VidFlags.IgnoreBounds);
		}

		/// <summary>Set the bounds for a row.
		/// Logically, a vid may have an upper bound of Infinity and/or a lower bound of -Infinity. 
		/// Specifying any other non-finite value for bounds should be avoided. 
		/// If a vid has a lower bound that is greater than its upper bound, the model is automatically infeasible, 
		/// now the ArgumentException is thrown for this case.  
		/// </summary>
		/// <param name="vid">the variable index </param>
		/// <param name="lower">lower bound</param>
		/// <param name="upper">upper bound</param>
		public virtual void SetBounds(int vid, Rational lower, Rational upper)
		{
			PreChange();
			ValidateVid(vid);
			ValidateBounds(lower, upper);
			_mpvidnumLo[vid] = lower;
			_mpvidnumHi[vid] = upper;
		}

		/// <summary>
		/// Set or adjust the lower bound of the variable. 
		/// </summary>
		/// <param name="vid">the variable index </param>
		/// <param name="lower">lower bound</param>
		public virtual void SetLowerBound(int vid, Rational lower)
		{
			PreChange();
			ValidateVid(vid);
			ValidateBounds(lower, _mpvidnumHi[vid]);
			_mpvidnumLo[vid] = lower;
		}

		/// <summary>
		/// Set or adjust the upper bound of the variable. 
		/// </summary>
		/// <param name="vid">the variable index</param>
		/// <param name="upper">the upper bound</param>
		public virtual void SetUpperBound(int vid, Rational upper)
		{
			PreChange();
			ValidateVid(vid);
			ValidateBounds(_mpvidnumLo[vid], upper);
			_mpvidnumHi[vid] = upper;
		}

		/// <summary>
		/// Return the bounds for the variable. 
		/// </summary>
		/// <param name="vid">the variable index</param>
		/// <param name="lower">the lower bound returned</param>
		/// <param name="upper">the upper bound returned</param>
		public virtual void GetBounds(int vid, out Rational lower, out Rational upper)
		{
			ValidateVid(vid);
			lower = _mpvidnumLo[vid];
			upper = _mpvidnumHi[vid];
		}

		/// <summary>
		/// The AddVariable method ensures that a user variable with the given key is in the model.
		/// If the model already includes a user variable referenced by key, this sets vid to the variable’s index 
		/// and returns false. Otherwise, if the model already includes a row referenced by key, this sets vid to -1 and returns false. 
		/// Otherwise, this adds a new user variable associated with key to the model, assigns the next available index to the new variable, 
		/// sets vid to this index, and returns true.
		/// </summary>
		/// <param name="key"> Variable key </param>
		/// <param name="vid">variable index </param>
		/// <returns>true if added successfully, otherwise false</returns>
		public virtual bool AddVariable(object key, out int vid)
		{
			PreChange();
			if (key != null && m_mpkeyvid.TryGetValue(key, out vid))
			{
				if (IsRow(vid))
				{
					vid = -1;
				}
				return false;
			}
			vid = AllocVid();
			ref VarInfo reference = ref _mpvidvi[vid];
			reference = new VarInfo(key, vid);
			if (key != null)
			{
				m_mpkeyvid.Add(key, vid);
			}
			return true;
		}

		/// <summary>Set the value for the specified vid.
		/// The default value for a vid is Indeterminate. An ILinearModel can be used to represent not just a linear model, 
		/// but also a current state for the model’s (user and row) variables. 
		/// The state associates with each vid a current value, represented as a Rational, and a basis status, represented as a boolean. 
		/// This state may be used as a starting point when solving, and may be updated by a solve attempt. 
		/// In particular, invoking the Solve method of the SimplexSolver class updates the values and basis status appropriately.
		/// Some other solvers may ignore this initial state for rows and even for variables.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <param name="value">current value</param>
		public virtual void SetValue(int vid, Rational value)
		{
			PreChange();
			ValidateVid(vid);
			_mpvidnum[vid] = value;
		}

		/// <summary>
		/// Get the value associated with the variable index. This is typically used to fetch solver results.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>the variable value</returns>
		public virtual Rational GetValue(int vid)
		{
			ValidateVid(vid);
			if (!_mpvidnumLo[vid].IsFinite && !_mpvidnumHi[vid].IsFinite && _mpvidnumLo[vid] == _mpvidnumHi[vid])
			{
				return _mpvidnumHi[vid];
			}
			return _mpvidnum[vid];
		}

		/// <summary>
		/// Mark a variable as an integer variable. 
		/// </summary>
		/// <param name="vid">a variable index </param>
		/// <param name="integer">whether to be an integer variable</param>
		public virtual void SetIntegrality(int vid, bool integer)
		{
			PreChange();
			ValidateVid(vid);
			AssignVidFlag(vid, VidFlags.Integer, integer, ref m_cvidInt);
		}

		/// <summary>
		/// Check if a variable is an integer variable
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>true if this variable is an integer variable. Otherwise false.</returns>
		public virtual bool GetIntegrality(int vid)
		{
			ValidateVid(vid);
			return HasFlag(vid, VidFlags.Integer);
		}

		/// <summary>Set a property for the specified index.
		/// </summary>
		/// <param name="propertyName">The name of the property to set, see SolverProperties.</param>
		/// <param name="vid">The variable index.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="T:System.ArgumentNullException">The property name is null.</exception>
		/// <exception cref="T:System.ArgumentException">The variable index is invalid.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidSolverPropertyException">The property is not supported. The Reason property indicates why the property is not supported.</exception>
		/// <remarks> This method is typically called by Solver Foundation Services in response to event handler code.
		/// </remarks>
		public virtual void SetProperty(string propertyName, int vid, object value)
		{
			if (propertyName == null)
			{
				throw new ArgumentNullException("propertyName");
			}
			Rational rational = Rational.ConvertToRational(value);
			if (propertyName == SolverProperties.VariableLowerBound)
			{
				if (IsRow(vid))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { vid }));
				}
				SetLowerBound(vid, rational);
				return;
			}
			if (propertyName == SolverProperties.VariableUpperBound)
			{
				if (IsRow(vid))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { vid }));
				}
				SetUpperBound(vid, rational);
				return;
			}
			if (propertyName == SolverProperties.VariableStartValue)
			{
				if (IsRow(vid))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { vid }));
				}
				SetValue(vid, rational);
				return;
			}
			throw new InvalidSolverPropertyException(string.Format(CultureInfo.InvariantCulture, Resources.PropertyNameIsNotSupported0, new object[1] { propertyName }), InvalidSolverPropertyReason.InvalidPropertyName);
		}

		/// <summary>Get a property for the specified index.
		/// </summary>
		/// <param name="propertyName">The name of the property to get, see SolverProperties.</param>
		/// <param name="vid">The variable index.</param>
		/// <returns>The value.</returns>
		/// <exception cref="T:System.ArgumentNullException">The property name is null.</exception>
		/// <exception cref="T:System.ArgumentException">The variable index is invalid.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidSolverPropertyException">The property is not supported. The Reason property indicates why the property is not supported.</exception>
		/// <remarks> This method is typically called by Solver Foundation Services in response to event handler code.
		/// </remarks>
		public virtual object GetProperty(string propertyName, int vid)
		{
			if (propertyName == null)
			{
				throw new ArgumentNullException("propertyName");
			}
			Rational lower;
			Rational upper;
			if (propertyName == SolverProperties.VariableLowerBound)
			{
				if (IsRow(vid))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { vid }));
				}
				GetBounds(vid, out lower, out upper);
				return lower;
			}
			if (propertyName == SolverProperties.VariableUpperBound)
			{
				if (IsRow(vid))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { vid }));
				}
				GetBounds(vid, out lower, out upper);
				return upper;
			}
			if (propertyName == SolverProperties.VariableStartValue)
			{
				if (IsRow(vid))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { vid }));
				}
				return GetValue(vid);
			}
			throw new InvalidSolverPropertyException(string.Format(CultureInfo.InvariantCulture, Resources.PropertyNameIsNotSupported0, new object[1] { propertyName }), InvalidSolverPropertyReason.InvalidPropertyName);
		}
	}
}
