using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A user variable that implements INotifyPropertiesChanged
	/// </summary>
	internal sealed class CspUserVariable : CspVariable, IDomainNarrowing, INotifyPropertyChanged
	{
		private bool _fSearchComplete;

		private bool _fSingleton;

		private bool _fFixed;

		private bool _fEnableEvent;

		private int _NewValue;

		private List<int> _rgValuesTested;

		private Dictionary<int, bool> _mpfValuesTested;

		/// <summary>
		/// Return the current computed array of feasible values of this user var.
		/// </summary>
		public object[] FeasibleValues
		{
			get
			{
				int[] array;
				lock (this)
				{
					array = _rgValuesTested.ToArray();
				}
				object[] array2 = new object[array.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array2[i] = GetValue(array[i]);
				}
				return array2;
			}
			set
			{
				OnPropertyChanged("FeasibleValues");
			}
		}

		internal List<int> TestedValues => _rgValuesTested;

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string info)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		internal CspUserVariable(ConstraintSystem solver, CspSolverDomain domain)
			: this(solver, domain, null)
		{
		}

		internal CspUserVariable(ConstraintSystem solver, CspSolverDomain domain, object key)
			: base(solver, domain, TermKinds.DecisionVariable, key)
		{
			_fSearchComplete = false;
			_fSingleton = false;
			_fFixed = false;
			_fEnableEvent = true;
			_rgValuesTested = new List<int>();
			_mpfValuesTested = new Dictionary<int, bool>();
		}

		/// <summary>
		/// Fix the value of this user var.
		/// </summary>
		/// <param name="val"></param>
		public void Fix(object val)
		{
			_fFixed = true;
			_NewValue = GetInteger(val);
			base.InnerSolver.FixUserVariable(this);
		}

		/// <summary>
		/// Unfix this user var.
		/// </summary>
		public void Unfix()
		{
			if (_fFixed)
			{
				_fFixed = false;
				base.InnerSolver.FixUserVariable(this);
			}
		}

		/// <summary>
		/// Test if domain narrowing is finished for this user var.
		/// </summary>
		/// <returns></returns>
		public bool IsFinished()
		{
			return _fSearchComplete;
		}

		/// <summary>
		/// Test if the domain of feasible values of this user var is a singleton.
		/// </summary>
		/// <returns></returns>
		public bool IsSingleton()
		{
			return _fSingleton;
		}

		/// <summary>
		/// Is the user variable just been fixed
		/// </summary>
		/// <returns></returns>
		public bool IsFixing()
		{
			return _fFixed;
		}

		/// <summary>
		/// Enable the call to the PropertyChanged event handler on Update
		/// </summary>
		internal void EnableEvent()
		{
			_fEnableEvent = true;
		}

		/// <summary>
		/// Get the value to which this user var needs to be fixed
		/// </summary>
		internal int GetFixedValue()
		{
			return _NewValue;
		}

		/// <summary>
		/// Set the flags that indicate we have finished domain narrowing for this user var.
		/// </summary>
		internal void SetFinished()
		{
			_fSearchComplete = true;
			_fSingleton = _rgValuesTested.Count == 1;
		}

		/// <summary>
		/// Clear the flags that indicate we have finished domain narrowing for this user var.
		/// </summary>
		internal void ClearFinished()
		{
			_fSearchComplete = false;
			_fSingleton = false;
		}

		/// <summary>
		/// Tell the user var that its feasible value list has been updated.
		/// </summary>
		internal void Update()
		{
			if (_fEnableEvent)
			{
				OnPropertyChanged("FeasibleValues");
				if (_fSearchComplete)
				{
					_fEnableEvent = false;
				}
			}
		}

		/// <summary>
		/// Add the input val to the feasible value list of this user var. If the val exists already,
		/// no duplicate will be added then.
		/// </summary>
		/// <param name="val">A feasible value</param>
		internal void DecideValue(int val)
		{
			if (!_mpfValuesTested.ContainsKey(val))
			{
				_mpfValuesTested.Add(val, value: true);
				_rgValuesTested.Add(val);
			}
		}

		/// <summary>
		/// Clear the feasible value list
		/// </summary>
		internal void ClearFeasibleValues()
		{
			lock (this)
			{
				_rgValuesTested = new List<int>();
				_fSearchComplete = false;
				_fSingleton = false;
				_mpfValuesTested.Clear();
			}
		}

		/// <summary>
		/// Remove feasible values and infeasible values from the domain as they have been tested.
		/// </summary>
		/// <returns>The cardinality of the new working domain</returns>
		internal int SetWorkingDomain()
		{
			if (_rgValuesTested.Count < 1)
			{
				return base.FiniteValue.Count;
			}
			int[] array = _rgValuesTested.ToArray();
			Statics.QuickSort(array, 0, array.Length - 1);
			base.FiniteValue.Exclude(out var newD, array);
			if (newD.Count == base.Count)
			{
				return base.Count;
			}
			return Restrain(newD);
		}
	}
}
