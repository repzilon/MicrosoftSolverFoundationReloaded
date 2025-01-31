using System;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A directive for the NelderMeadSolver.
	/// </summary>
	/// <remarks>
	/// The Nelder-Mead solver is suitable for unconstrainted, unbounded non-linear models with real decisions.
	/// </remarks>
	public class NelderMeadDirective : Directive
	{
		private double _tolerance;

		private int _iterationLimit;

		private NelderMeadStartMethod _startMethod;

		private NelderMeadTerminationSensitivity _termination;

		/// <summary>The solver terminates when the size of the Nelder-Mead simplex falls below the Tolerance.
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

		/// <summary>The maximum number of solver iterations.
		/// </summary>
		/// <remarks>
		/// If the iteration limit is exceeded, the solver will return an unknown status.
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

		/// <summary>How to initialize the starting point.
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

		/// <summary>Create a new instance.
		/// </summary>
		public NelderMeadDirective()
		{
			_tolerance = NelderMeadSolverParams.DefaultTolerance;
			_iterationLimit = NelderMeadSolverParams.DefaultIterationLimit;
			_startMethod = NelderMeadSolverParams.DefaultStartMethod;
			_termination = NelderMeadSolverParams.DefaultTerminationSensitivity;
		}

		/// <summary>
		/// Returns a representation of the directive as a string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Concat("NelderMead(TimeLimit = ", base.TimeLimit, ", Tolerance = ", Tolerance, ", IterationLimit = ", IterationLimit, ", TerminationSensitivity = ", TerminationSensitivity, ")");
		}
	}
}
