using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Integer (Finite-domain) variables.
	/// </summary>
	internal class IntegerVariable : DiscreteVariable
	{
		private int _depthOfLastSave;

		private long _lowerBound;

		private long _upperBound;

		private BasicEvent _anyBoundModification;

		public override long DomainSize => _upperBound - _lowerBound + 1;

		/// <summary>
		///   Access to current lower bound
		/// </summary>
		public long LowerBound => _lowerBound;

		/// <summary>
		///   Access to current upper bound
		/// </summary>
		public long UpperBound => _upperBound;

		/// <summary>
		///   Depth at which the variable was last saved
		/// </summary>
		internal int DepthOfLastSave
		{
			get
			{
				return _depthOfLastSave;
			}
			set
			{
				_depthOfLastSave = value;
			}
		}

		private event Procedure<IntegerVariable> _restored;

		/// <summary>
		///   Construction (for internal use) of an integer variable that 
		///   does not correspond to a term in the initial problem
		/// </summary>
		/// <param name="p">Problem in which variable is created</param>
		/// <param name="l">initial lower bound</param>
		/// <param name="r">initial upper bound</param>
		internal IntegerVariable(Problem p, long l, long r)
			: base(p, r - l + 1)
		{
			_depthOfLastSave = -1;
			_lowerBound = l;
			_upperBound = r;
		}

		/// <summary>
		///   Construction from an Integer Term
		/// </summary>
		/// <param name="p">Problem in which the variable is created</param>
		/// <param name="src">Term from which var originates</param>
		public IntegerVariable(Problem p, DisolverIntegerTerm src)
			: this(p, src.InitialLowerBound, src.InitialUpperBound)
		{
			if (src is DisolverIntegerVariable disolverIntegerVariable && disolverIntegerVariable.InitialDomain is SparseDomain)
			{
				p.AddConstraint(new Member(p, this, disolverIntegerVariable.InitialDomain as SparseDomain));
			}
		}

		/// <summary>
		///   Subscribes a delegate that will be called every time 
		///   the variable is modified 
		///   (except when the modification is a restoration, i.e. backtracking)
		/// </summary>
		internal void SubscribeToAnyModification(BasicEvent.Listener l)
		{
			CreateIfNull(ref _anyBoundModification).Subscribe(l);
		}

		/// <summary>
		///   Subscribes a delegate that will be called after any restoration
		///   of the state of the variable, i.e. on backtrack
		/// </summary>
		internal void SubscribeToRestored(Procedure<IntegerVariable> listener)
		{
			_restored += listener;
		}

		/// <summary>
		///   Constrains the variable to take a value greater or 
		///   equal to a new lower bound.  
		/// </summary>
		/// <param name="newlb">new lower bound</param>
		/// <param name="c">
		///   the cause of the call; can be null if not a consequence 
		/// </param>
		/// <returns>false if contradiction detected</returns>
		public bool ImposeLowerBound(long newlb, Cause c)
		{
			if (newlb <= _lowerBound)
			{
				return true;
			}
			if (newlb > _upperBound)
			{
				return ImposeEmptyDomain(c);
			}
			if (_anyBoundModification != null)
			{
				_anyBoundModification.RescheduleIfNeeded();
			}
			_problem.Save(this);
			_lowerBound = newlb;
			_problem.DispatchVariableModification(this, c);
			return true;
		}

		/// <summary>
		///   Constrains the variable to take a value less or equal 
		///   to a new upper bound
		/// </summary>
		/// <param name="newub">new upper bound</param>
		/// <param name="c">
		///   the cause of the call; can be null if not a consequence 
		/// </param>
		/// <returns>false if contradiction detected</returns>
		public bool ImposeUpperBound(long newub, Cause c)
		{
			if (newub >= _upperBound)
			{
				return true;
			}
			if (newub < _lowerBound)
			{
				return ImposeEmptyDomain(c);
			}
			if (_anyBoundModification != null)
			{
				_anyBoundModification.RescheduleIfNeeded();
			}
			_problem.Save(this);
			_upperBound = newub;
			_problem.DispatchVariableModification(this, c);
			return true;
		}

		/// <summary>
		///   Constrains the variable to take a particular value
		/// </summary>
		/// <param name="newlb">new lower bound</param>
		/// <param name="newub">new upper bound</param>
		/// <param name="c">
		///   the cause of the call; can be null if not a consequence 
		/// </param>
		/// <returns>false iff contradiction detected</returns>
		public bool ImposeRange(long newlb, long newub, Cause c)
		{
			if (newub < _lowerBound || newlb > _upperBound)
			{
				return ImposeEmptyDomain(c);
			}
			if (newlb <= _lowerBound && newub >= _upperBound)
			{
				return true;
			}
			if (_anyBoundModification != null)
			{
				_anyBoundModification.RescheduleIfNeeded();
			}
			_problem.Save(this);
			_lowerBound = Math.Max(newlb, _lowerBound);
			_upperBound = Math.Min(newub, _upperBound);
			_problem.DispatchVariableModification(this, c);
			return true;
		}

		/// <summary>
		///   Constrains the variable to take a particular value
		/// </summary>
		/// <param name="newval">the new value</param>
		/// <param name="c">
		///   the cause of the call; can be null if not a consequence 
		/// </param>
		/// <returns>false iff contradiction detected</returns>
		public bool ImposeValue(long newval, Cause c)
		{
			return ImposeRange(newval, newval, c);
		}

		/// <summary>
		///   If one bound of the variables happens to equal the removed Value
		///   the it will be tighten. Note that this method has no guarantee
		///   that the value will be permanently removed
		/// </summary>
		/// <param name="removedValue">the removed value</param>
		/// <param name="c">
		///   the cause of the call; can be null if not a consequence 
		/// </param>
		/// <returns>false iff contradiction detected</returns>
		public bool ImposeBoundsDifferentFrom(long removedValue, Cause c)
		{
			if (removedValue == _lowerBound)
			{
				return ImposeLowerBound(removedValue + 1, c);
			}
			if (removedValue == _upperBound)
			{
				return ImposeUpperBound(removedValue - 1, c);
			}
			return true;
		}

		public override long GetLowerBound()
		{
			return LowerBound;
		}

		public override long GetUpperBound()
		{
			return UpperBound;
		}

		public override bool IsAllowed(long value)
		{
			if (_lowerBound <= value)
			{
				return value <= _upperBound;
			}
			return false;
		}

		public override bool ImposeIntegerLowerBound(long val, Cause c)
		{
			return ImposeLowerBound(val, c);
		}

		public override bool ImposeIntegerUpperBound(long ub, Cause c)
		{
			return ImposeUpperBound(ub, c);
		}

		public override bool ImposeIntegerValue(long val, Cause c)
		{
			return ImposeValue(val, c);
		}

		public override bool TryGetIntegerValue(out long value)
		{
			if (_lowerBound == _upperBound)
			{
				value = _lowerBound;
				return true;
			}
			value = -1234567890L;
			return false;
		}

		public override IEnumerable<DisolverConstraint> EnumerateConstraints()
		{
			if (_anyBoundModification == null)
			{
				yield break;
			}
			foreach (BasicEvent.Listener l in _anyBoundModification.EnumerateListeners())
			{
				if (l.Target is DisolverConstraint cstr)
				{
					yield return cstr;
				}
			}
		}

		/// <summary>
		///   True if the two bounds of the variable are equal 
		/// </summary>
		public bool IsInstantiated()
		{
			return _lowerBound == _upperBound;
		}

		/// <summary>
		///   Gets the value of an instantiated variable
		/// </summary>
		public long GetValue()
		{
			return _lowerBound;
		}

		/// <summary>
		///   Called by the problem on backtrack, to notify the variable
		///   that it should return to a previous state. Note that the 
		///   (bit-vector) domain, if any, is maintained independently, as
		///   a BacktrackabkleFiniteSet
		/// </summary>
		internal void RestoreState(long lb, long ub, int depth)
		{
			_lowerBound = lb;
			_upperBound = ub;
			_depthOfLastSave = depth;
			if (this._restored != null)
			{
				this._restored(this);
			}
		}

		/// <summary>
		///   Implementation of the "create at first access" philosophy
		/// </summary>
		private BasicEvent CreateIfNull(ref BasicEvent e)
		{
			if (e == null)
			{
				e = new BasicEvent(_problem.Scheduler);
			}
			return e;
		}

		/// <summary>
		///   Schedules all events to which listeners should react
		///   at the beginning of the problem resolution
		/// </summary>
		public override void ScheduleInitialEvents()
		{
			if (_anyBoundModification != null)
			{
				_anyBoundModification.RescheduleIfNeeded();
			}
		}

		/// <summary>
		///   This is to help visualizing in debugger, nothing more
		/// </summary>
		public override string ToString()
		{
			return _lowerBound + ", " + _upperBound;
		}
	}
}
