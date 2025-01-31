using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Wraps a Decision for SolverContext.Probe.
	/// </summary>
	public sealed class DecisionBinding : INotifyPropertyChanged
	{
		private Decision _decision;

		private Rational[] _feasibleValues = new Rational[0];

		internal Rational _fixedValue;

		internal bool _isFixed;

		/// <summary>
		/// Returns the feasible values of this decision after a call to SolverContext.Probe.
		/// </summary>
		/// <exception cref="T:System.InvalidCastException">Thrown if the domain of the decision is not integer.</exception>
		public IEnumerable<int> Int32FeasibleValues
		{
			get
			{
				if (_decision._domain.EnumeratedNames != null)
				{
					throw new InvalidCastException();
				}
				try
				{
					Rational[] feasibleValues = _feasibleValues;
					foreach (Rational r in feasibleValues)
					{
						yield return (int)r;
					}
				}
				finally
				{
				}
			}
		}

		/// <summary>
		/// Returns the feasible values of this decision after a call to SolverContext.Probe.
		/// </summary>
		/// <exception cref="T:System.InvalidCastException">Thrown if the domain of the decision is not enumerated.</exception>
		public IEnumerable<string> StringFeasibleValues
		{
			get
			{
				if (_decision._domain.EnumeratedNames == null)
				{
					throw new InvalidCastException();
				}
				try
				{
					Rational[] feasibleValues = _feasibleValues;
					foreach (Rational r in feasibleValues)
					{
						yield return _decision._domain.EnumeratedNames[(int)r];
					}
				}
				finally
				{
				}
			}
		}

		/// <summary>
		/// The underlying Decision wrapped by this DecisionBinding.
		/// </summary>
		public Decision Decision => _decision;

		/// <summary>
		/// Called when the feasible values of this decision change. May be called multiple times during a call to solve.
		/// Note that the list of feasible values may be incomplete when this is called. Code called from this event must
		/// not modify the model or fix/unfix a DecisionBinding.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Called when a specific value of this decision is determined to be definitely feasible or infeasible.
		/// This will be called exactly once for each possible value of the decision.
		/// </summary>
		public event EventHandler<ValueFeasibilityKnownEventArgs> ValueFeasibilityKnown;

		internal DecisionBinding(Decision decision)
		{
			if (decision._domain.ValueClass == TermValueClass.Any)
			{
				throw new ArgumentException(Resources.OnlyIntegerAndEnumDecisionsMayBeProbed, "decision");
			}
			if (!decision._domain.IntRestricted)
			{
				throw new ArgumentException(Resources.OnlyIntegerAndEnumDecisionsMayBeProbed, "decision");
			}
			if (decision._indexSets.Length > 0)
			{
				throw new ArgumentException(Resources.IndexedDecisionsMayNotBeProbed, "decision");
			}
			_decision = decision;
		}

		/// <summary>
		/// Fixes this decision to a specific value for purposes of probing. Only feasible solutions where this decision
		/// takes the given value will be considered during probing.
		/// </summary>
		/// <param name="value"></param>
		public void Fix(int value)
		{
			if (_decision._domain.EnumeratedNames != null)
			{
				throw new InvalidCastException();
			}
			_isFixed = true;
			_fixedValue = value;
		}

		/// <summary>
		/// Fixes this decision to a specific value for purposes of probing. Only feasible solutions where this decision
		/// takes the given value will be considered during probing.
		/// </summary>
		/// <param name="value"></param>
		public void Fix(string value)
		{
			if (_decision._domain.EnumeratedNames == null)
			{
				throw new InvalidCastException();
			}
			for (int i = 0; i < _decision._domain.EnumeratedNames.Length; i++)
			{
				if (value == _decision._domain.EnumeratedNames[i])
				{
					_isFixed = true;
					_fixedValue = i;
					return;
				}
			}
			throw new ArgumentOutOfRangeException("value");
		}

		/// <summary>
		/// Undoes the result of a previous call to Fix().
		/// </summary>
		public void Unfix()
		{
			_isFixed = false;
		}

		internal void SetFeasibleValues(Rational[] feasibleValues)
		{
			_feasibleValues = feasibleValues;
			if (this.PropertyChanged != null)
			{
				if (_decision._domain.EnumeratedNames != null)
				{
					this.PropertyChanged(this, new PropertyChangedEventArgs("StringFeasibleValues"));
				}
				else
				{
					this.PropertyChanged(this, new PropertyChangedEventArgs("Int32FeasibleValues"));
				}
			}
		}

		internal void SetFeasibility(Rational value, bool feasible)
		{
			if (this.ValueFeasibilityKnown != null)
			{
				if (_decision._domain.EnumeratedNames != null)
				{
					this.ValueFeasibilityKnown(_decision._domain.EnumeratedNames[(int)value], new ValueFeasibilityKnownEventArgs(feasible));
				}
				else
				{
					this.ValueFeasibilityKnown((int)value, new ValueFeasibilityKnownEventArgs(feasible));
				}
			}
		}
	}
}
