using System;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A directive for the  Compact Quasi Newton (CQN) solver.
	/// </summary>
	/// <remarks>
	/// The CQN solver is suitable for unconstrainted, unbounded non-linear models with real decisions.
	/// </remarks>
	public class CompactQuasiNewtonDirective : Directive
	{
		private double _tolerance;

		private int _iterationsToRemember;

		private int _iterationLimit;

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
				if (value < CompactQuasiNewtonSolverParams.MinimumToleranceEps)
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

		/// <summary>Create a new instance.
		/// </summary>
		public CompactQuasiNewtonDirective()
		{
			_iterationsToRemember = CompactQuasiNewtonSolverParams.DefaultM;
			_tolerance = CompactQuasiNewtonSolverParams.DefaultTolerance;
			_iterationLimit = CompactQuasiNewtonSolverParams.DefaultIterationLimit;
		}

		/// <summary>
		/// Returns a representation of the directive as a string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "CQN(TimeLimit = " + base.TimeLimit + ", Tolerance = " + Tolerance + ", IterationsToRemember = " + IterationsToRemember + ", IterationLimit = " + IterationLimit + ")";
		}
	}
}
