using System;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>Parameters for the NelderMeadSolver.
	/// </summary>
	/// <remarks>
	/// The solver terminates when the size of the Nelder-Mead simplex falls below the Tolerance.
	/// </remarks>
	public class NelderMeadSolverParams : ISolverParameters, ISolverEvents
	{
		internal static double DefaultTolerance = 1E-06;

		internal static double DefaultUnboundedTolerance = 1E+200;

		internal static int DefaultIterationLimit = 50000;

		internal static NelderMeadStartMethod DefaultStartMethod = NelderMeadStartMethod.Default;

		internal static NelderMeadTerminationSensitivity DefaultTerminationSensitivity = NelderMeadTerminationSensitivity.Conservative;

		internal static int DefaultMaxSearchPoints = 1;

		private double _tolerance;

		private double _unboundedTolerance;

		private int _iterationLimit;

		private Func<bool> _fnQueryAbort;

		private Action _fnSolving;

		private volatile bool _abort;

		private NelderMeadStartMethod _startMethod;

		private NelderMeadTerminationSensitivity _termination;

		private int _maxSearchPoints;

		/// <summary>The solver terminates when the size of the simplex
		/// falls below the Tolerance.
		/// </summary>
		public virtual double Tolerance
		{
			get
			{
				return _tolerance;
			}
			set
			{
				if (!NelderMeadSolver.IsValidTolerance(value))
				{
					throw new ArgumentOutOfRangeException("value", value, Resources.ToleranceTooLow);
				}
				_tolerance = value;
			}
		}

		/// <summary>The solver terminates when the magnitude of the objective value at the centroid is beyond this value.
		/// </summary>
		public virtual double UnboundedTolerance
		{
			get
			{
				return _unboundedTolerance;
			}
			set
			{
				if (value < 0.0)
				{
					throw new ArgumentOutOfRangeException("value", value, Resources.ToleranceTooLow);
				}
				_unboundedTolerance = value;
			}
		}

		/// <summary>The maximum number of solver iterations.
		/// </summary>
		/// <remarks>
		/// If the iteration limit is exceeded, the solver will return an interrupted status.
		/// The default is Int32.MaxValue.
		/// </remarks>
		public virtual int IterationLimit
		{
			get
			{
				return _iterationLimit;
			}
			set
			{
				if (value < 1)
				{
					throw new ArgumentOutOfRangeException("value", value, Resources.MaximumNumberOfIterationsShouldBePositive);
				}
				_iterationLimit = value;
			}
		}

		/// <summary>The maximum number of search points per iteration.
		/// </summary>
		/// <remarks>
		/// This value controls the number of points in the simplex that are updated during each iteration.
		/// For some problems, increasing the number of search points results in faster convergence.
		/// The default value is 1.
		/// </remarks>
		public virtual int MaximumSearchPoints
		{
			get
			{
				return _maxSearchPoints;
			}
			set
			{
				if (value < 1)
				{
					throw new ArgumentOutOfRangeException("value", value, Resources.MaximumNumberOfIterationsShouldBePositive);
				}
				_maxSearchPoints = value;
			}
		}

		/// <summary>Setting this property to true will cause the solver to abort.
		/// </summary>
		public virtual bool Abort
		{
			get
			{
				return _abort;
			}
			set
			{
				_abort = value;
			}
		}

		/// <summary>Determines how to initialize the starting point.
		/// </summary>
		public virtual NelderMeadStartMethod StartMethod
		{
			get
			{
				return _startMethod;
			}
			set
			{
				_startMethod = value;
			}
		}

		/// <summary>The termination policy.
		/// </summary>
		public virtual NelderMeadTerminationSensitivity TerminationSensitivity
		{
			get
			{
				return _termination;
			}
			set
			{
				_termination = value;
			}
		}

		/// <summary>
		/// Get/set the callback function that decides when to abort the search
		/// </summary>
		public Func<bool> QueryAbort
		{
			get
			{
				return _fnQueryAbort;
			}
			set
			{
				_fnQueryAbort = value;
			}
		}

		/// <summary>
		/// Callback called during solve
		/// </summary>
		public Action Solving
		{
			get
			{
				return _fnSolving;
			}
			set
			{
				_fnSolving = value;
			}
		}

		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="fnQueryAbort">An abort delegate that is called during the solution process.</param>
		public NelderMeadSolverParams(Func<bool> fnQueryAbort)
			: this(DefaultTolerance, DefaultUnboundedTolerance, DefaultIterationLimit, fnQueryAbort)
		{
		}

		/// <summary>Create a new instance with default arguments.
		/// </summary>
		public NelderMeadSolverParams()
			: this((Func<bool>)null)
		{
		}

		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="tolerance">The solution tolerance.</param>
		/// <param name="unboundedTolerance">The unbounded solution tolerance.</param>
		/// <param name="maxIterations">The maximum number of iterations.</param>
		/// <param name="fnQueryAbort">An abort delegate that is called during the solution process.</param>
		public NelderMeadSolverParams(double tolerance, double unboundedTolerance, int maxIterations, Func<bool> fnQueryAbort)
		{
			if (!NelderMeadSolver.IsValidTolerance(tolerance))
			{
				throw new ArgumentOutOfRangeException("tolerance", tolerance, Resources.ToleranceTooLow);
			}
			_tolerance = tolerance;
			if (unboundedTolerance < 0.0)
			{
				throw new ArgumentOutOfRangeException("unboundedTolerance", unboundedTolerance, Resources.ToleranceTooLow);
			}
			_unboundedTolerance = unboundedTolerance;
			if (maxIterations < 1)
			{
				throw new ArgumentOutOfRangeException("maxIterations", maxIterations, Resources.MaximumNumberOfIterationsShouldBePositive);
			}
			_iterationLimit = maxIterations;
			_maxSearchPoints = DefaultMaxSearchPoints;
			_termination = DefaultTerminationSensitivity;
			_fnQueryAbort = fnQueryAbort;
		}

		/// <summary>Copy constructor.
		/// </summary>
		public NelderMeadSolverParams(NelderMeadSolverParams parameters)
		{
			if (parameters == null)
			{
				throw new ArgumentNullException("parameters");
			}
			_tolerance = parameters._tolerance;
			_unboundedTolerance = parameters._unboundedTolerance;
			_iterationLimit = parameters._iterationLimit;
			_fnQueryAbort = parameters._fnQueryAbort;
			_fnSolving = parameters._fnSolving;
			_abort = parameters._abort;
			_startMethod = parameters._startMethod;
			_termination = parameters._termination;
			_maxSearchPoints = parameters._maxSearchPoints;
		}

		/// <summary>Create a new instance from a Directive.
		/// </summary>
		public NelderMeadSolverParams(Directive directive)
			: this((Func<bool>)null)
		{
			FillInSolverParams(directive);
		}

		/// <summary>Checks whether the solver should abort by examining the Abort property and
		/// the abort delegate.
		/// </summary>
		public virtual bool ShouldAbort()
		{
			if (!Abort)
			{
				if (_fnQueryAbort != null)
				{
					return Abort = _fnQueryAbort();
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// Fill in NelderMeadSolverParams based on the given directive.
		/// </summary>
		/// <param name="dir">The directive instance that contains all the parameter settings</param>
		private void FillInSolverParams(Directive dir)
		{
			if (dir != null && dir is NelderMeadDirective nelderMeadDirective)
			{
				_tolerance = nelderMeadDirective.Tolerance;
				_iterationLimit = nelderMeadDirective.IterationLimit;
				_startMethod = nelderMeadDirective.StartMethod;
				_termination = nelderMeadDirective.TerminationSensitivity;
			}
		}
	}
}
