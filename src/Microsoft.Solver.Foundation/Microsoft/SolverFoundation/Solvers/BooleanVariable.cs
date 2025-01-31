using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Class of Boolean variables.
	///   Can be involved both in purely Boolean constraints (propagated in a 
	///   special way) and in "classical" CP constraints (reified).
	/// </summary>
	internal class BooleanVariable : DiscreteVariable
	{
		private BasicEvent _instantiationToTrue;

		private BasicEvent _instantiationToFalse;

		private BooleanVariableState _state;

		public override long DomainSize
		{
			get
			{
				BooleanVariableState state = _state;
				if (state == BooleanVariableState.Unassigned)
				{
					return 2L;
				}
				return 1L;
			}
		}

		/// <summary>
		///   Access to the Status of the variable
		/// </summary>
		public BooleanVariableState Status => _state;

		/// <summary>
		///   Construction (for internal use) of a Boolean variable that 
		///   does not correspond to a term in the initial problem
		/// </summary>
		/// <param name="p">Problem in which the variable is created</param>
		/// <param name="s">initial state</param>
		internal BooleanVariable(Problem p, BooleanVariableState s)
			: base(p, (s != BooleanVariableState.Unassigned) ? 1 : 2)
		{
			_state = s;
		}

		/// <summary>
		///   Construction
		/// </summary>
		/// <param name="p">Problem in which the variable is created</param>
		/// <param name="src">Term from which the var originates</param>
		public BooleanVariable(Problem p, DisolverBooleanTerm src)
			: this(p, BooleanVariableState.Unassigned)
		{
		}

		/// <summary>
		///   Construction (for internal use) of a Boolean variable that 
		///   does not correspond to a term in the initial problem and which
		///   is initially unassigned
		/// </summary>
		/// <param name="p">Problem in which the variable is created</param>
		internal BooleanVariable(Problem p)
			: this(p, BooleanVariableState.Unassigned)
		{
		}

		/// <summary>
		///   Adds l to the list of listeners activated when
		///   the variable becomes false.
		/// </summary>
		internal void SubscribeToFalse(BasicEvent.Listener l)
		{
			if (_instantiationToFalse == null)
			{
				_instantiationToFalse = new BasicEvent(_problem.Scheduler);
			}
			_instantiationToFalse.Subscribe(l);
		}

		/// <summary>
		///   Adds l to the list of listeners activated when
		///   the variable becomes true.
		/// </summary>
		internal void SubscribeToTrue(BasicEvent.Listener l)
		{
			if (_instantiationToTrue == null)
			{
				_instantiationToTrue = new BasicEvent(_problem.Scheduler);
			}
			_instantiationToTrue.Subscribe(l);
		}

		/// <summary>
		///   Constrains the variable to take value false
		/// </summary>
		/// <returns>false if a contradiction was detected</returns>
		public bool ImposeValueFalse(Cause c)
		{
			switch (_state)
			{
			case BooleanVariableState.False:
				return true;
			case BooleanVariableState.True:
				return ImposeEmptyDomain(c);
			default:
				_state = BooleanVariableState.False;
				_problem.SignalBooleanVariableInstantiation(this, c);
				if (_instantiationToFalse != null)
				{
					_instantiationToFalse.RescheduleIfNeeded();
				}
				return true;
			}
		}

		/// <summary>
		///   Constrains the variable to take value true
		/// </summary>
		/// <returns>false if a contradiction was detected</returns>
		public bool ImposeValueTrue(Cause c)
		{
			switch (_state)
			{
			case BooleanVariableState.True:
				return true;
			case BooleanVariableState.False:
				return ImposeEmptyDomain(c);
			default:
				_state = BooleanVariableState.True;
				_problem.SignalBooleanVariableInstantiation(this, c);
				if (_instantiationToTrue != null)
				{
					_instantiationToTrue.RescheduleIfNeeded();
				}
				return true;
			}
		}

		/// <summary>
		///   Constrains the variable to take a particular Boolean value
		/// </summary>
		/// <returns>false if a contradiction was detected</returns>
		public bool ImposeValue(bool b, Cause c)
		{
			if (!b)
			{
				return ImposeValueFalse(c);
			}
			return ImposeValueTrue(c);
		}

		/// <summary>
		///   Called by the problem to notify the variable that 
		///   it return to an unassigned state
		/// </summary>
		internal void Uninstantiate()
		{
			_state = BooleanVariableState.Unassigned;
		}

		public override long GetLowerBound()
		{
			return (_state == BooleanVariableState.True) ? 1 : 0;
		}

		public override long GetUpperBound()
		{
			return (_state != BooleanVariableState.False) ? 1 : 0;
		}

		public override bool IsAllowed(long val)
		{
			switch (_state)
			{
			case BooleanVariableState.False:
				return val == 0;
			case BooleanVariableState.True:
				return val == 1;
			default:
				if (val != 0)
				{
					return val == 1;
				}
				return true;
			}
		}

		public override bool ImposeIntegerLowerBound(long lb, Cause c)
		{
			if (lb != 0)
			{
				return ImposeValueTrue(c);
			}
			return true;
		}

		public override bool ImposeIntegerUpperBound(long ub, Cause c)
		{
			if (ub != 1)
			{
				return ImposeValueFalse(c);
			}
			return true;
		}

		public override bool ImposeIntegerValue(long val, Cause c)
		{
			return ImposeValue(val == 1, c);
		}

		public override bool TryGetIntegerValue(out long value)
		{
			switch (_state)
			{
			case BooleanVariableState.False:
				value = 0L;
				return true;
			case BooleanVariableState.True:
				value = 1L;
				return true;
			default:
				value = -1234567890L;
				return false;
			}
		}

		public override void ScheduleInitialEvents()
		{
			if (_state == BooleanVariableState.False && _instantiationToFalse != null)
			{
				_instantiationToFalse.RescheduleIfNeeded();
			}
			if (_state == BooleanVariableState.True && _instantiationToTrue != null)
			{
				_instantiationToTrue.RescheduleIfNeeded();
			}
		}

		public override IEnumerable<DisolverConstraint> EnumerateConstraints()
		{
			BasicEvent[] array = new BasicEvent[2] { _instantiationToFalse, _instantiationToTrue };
			try
			{
				BasicEvent[] array2 = array;
				foreach (BasicEvent e in array2)
				{
					if (e == null)
					{
						continue;
					}
					foreach (BasicEvent.Listener l in e.EnumerateListeners())
					{
						if (l.Target is DisolverConstraint cstr)
						{
							yield return cstr;
						}
					}
				}
			}
			finally
			{
			}
		}

		/// <summary>
		///   Get the truth value 
		///   (precondition: the variable must be instantiated)
		/// </summary>
		public bool GetValue()
		{
			return Status == BooleanVariableState.True;
		}

		/// <summary>
		///   For debugging
		/// </summary>
		public override string ToString()
		{
			return _state.ToString();
		}
	}
}
