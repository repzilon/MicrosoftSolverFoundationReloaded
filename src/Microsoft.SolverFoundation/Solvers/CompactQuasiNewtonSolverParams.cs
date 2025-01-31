using System;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>Parameters for the CompactQuasiNewtonSolver.
	/// </summary>
	/// <remarks>
	/// The solver terminates when the geometrically-weighted average improvement falls below the Tolerance.
	/// </remarks>
	public class CompactQuasiNewtonSolverParams : ISolverParameters, ISolverEvents
	{
		internal static double MinimumToleranceEps = 1E-10;

		internal static double DefaultTolerance = 1E-07;

		internal static int DefaultM = 17;

		internal static int DefaultIterationLimit = int.MaxValue;

		private double _tolerance;

		private int _iterationsToRemember;

		private int _iterationLimit;

		private Func<bool> _fnQueryAbort;

		private Action _fnSolving;

		private volatile bool _abort;

		/// <summary>The solver terminates when the geometrically-weighted average improvement 
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
				if (value < MinimumToleranceEps)
				{
					throw new ArgumentOutOfRangeException("value", value, Resources.ToleranceTooLow);
				}
				_tolerance = value;
			}
		}

		/// <summary>
		/// Number of previous iterations to remember for estimate of Hessian (m), (default is 17).
		/// </summary>
		/// <remarks>
		/// Higher values lead to better approximations to Newton's method, 
		/// but use more memory, and requires more time to compute direction.  
		/// The optimal setting of IterationsToRemember is problem specific,
		/// depending on such factors as how expensive is function evaluation
		/// compared to choosing the direction, how easily approximable is the 
		/// function's Hessian, etc.  A range of 15 to 20 is usually reasonable 
		/// but if necessary even a value of 2 is better than gradient descent.
		/// </remarks>    
		public virtual int IterationsToRemember
		{
			get
			{
				return _iterationsToRemember;
			}
			set
			{
				if (value < 1)
				{
					throw new ArgumentOutOfRangeException("value", value, Resources.NumberOfIterationsToRememberShouldBePositive);
				}
				_iterationsToRemember = value;
			}
		}

		/// <summary>The maximum number of solver iterations.
		/// </summary>
		/// <remarks>
		/// If the iteration limit is exceeded, the solver will return CompactQuasiNewtonSolutionQuality.Error.
		/// The default Int32.MaxValue.
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
		public CompactQuasiNewtonSolverParams(Func<bool> fnQueryAbort)
			: this(DefaultTolerance, DefaultM, DefaultIterationLimit, fnQueryAbort)
		{
		}

		/// <summary>Create a new instance with default arguments.
		/// </summary>
		public CompactQuasiNewtonSolverParams()
			: this((Func<bool>)null)
		{
		}

		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="tolerance">The solution tolerance.</param>
		/// <param name="iterationsToRemember"></param>
		/// <param name="maxIterations">The maximum number of iterations.</param>
		/// <param name="fnQueryAbort">An abort delegate that is called during the solution process.</param>
		public CompactQuasiNewtonSolverParams(double tolerance, int iterationsToRemember, int maxIterations, Func<bool> fnQueryAbort)
		{
			if (tolerance < MinimumToleranceEps)
			{
				throw new ArgumentOutOfRangeException("tolerance", tolerance, Resources.ToleranceTooLow);
			}
			_tolerance = tolerance;
			if (iterationsToRemember < 1)
			{
				throw new ArgumentOutOfRangeException("iterationsToRemember", iterationsToRemember, Resources.NumberOfIterationsToRememberShouldBePositive);
			}
			_iterationsToRemember = iterationsToRemember;
			if (maxIterations < 1)
			{
				throw new ArgumentOutOfRangeException("maxIterations", maxIterations, Resources.MaximumNumberOfIterationsShouldBePositive);
			}
			_iterationLimit = maxIterations;
			_fnQueryAbort = fnQueryAbort;
		}

		/// <summary>Copy constructor.
		/// </summary>
		public CompactQuasiNewtonSolverParams(CompactQuasiNewtonSolverParams parameters)
		{
			if (parameters == null)
			{
				throw new ArgumentNullException("parameters");
			}
			_tolerance = parameters._tolerance;
			_iterationsToRemember = parameters._iterationsToRemember;
			_iterationLimit = parameters._iterationLimit;
			_fnQueryAbort = parameters._fnQueryAbort;
			_fnSolving = parameters._fnSolving;
			_abort = parameters._abort;
		}

		/// <summary>Create a new instance from a Directive.
		/// </summary>
		public CompactQuasiNewtonSolverParams(Directive directive)
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
		/// Fill in CompactQuasiNewtonSolverParams based on the given directive.
		/// </summary>
		/// <param name="dir">The directive instance that contains all the parameter settings</param>
		private void FillInSolverParams(Directive dir)
		{
			if (dir != null && dir is CompactQuasiNewtonDirective compactQuasiNewtonDirective)
			{
				_tolerance = compactQuasiNewtonDirective.Tolerance;
				_iterationsToRemember = compactQuasiNewtonDirective.IterationsToRemember;
				_iterationLimit = compactQuasiNewtonDirective.IterationLimit;
			}
		}
	}
}
