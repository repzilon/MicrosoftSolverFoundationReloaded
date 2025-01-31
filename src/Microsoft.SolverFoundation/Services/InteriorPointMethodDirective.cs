using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A directive for the interior point (IPM) solver.
	/// </summary>
	/// <remarks>
	/// The IPM solver is suitable for linear, quadratic, and second order conic models with real decisions.
	/// </remarks>
	public class InteriorPointMethodDirective : Directive
	{
		private InteriorPointMethodAlgorithm _algorithm;

		private double _gapTolerance;

		private int _iterationLimit;

		private int _presolveLevel;

		private InteriorPointSymbolicOrdering _symbolicOrdering;

		/// <summary> The gap tolerance. If set to 0, the solver will select a default.
		/// </summary>
		public double GapTolerance
		{
			get
			{
				return _gapTolerance;
			}
			set
			{
				_gapTolerance = value;
			}
		}

		/// <summary> The algorithm.
		/// </summary>
		public InteriorPointMethodAlgorithm Algorithm
		{
			get
			{
				return _algorithm;
			}
			set
			{
				_algorithm = value;
			}
		}

		/// <summary> The type of matrix ordering to apply.
		/// </summary>
		public InteriorPointSymbolicOrdering SymbolicOrdering
		{
			get
			{
				return _symbolicOrdering;
			}
			set
			{
				_symbolicOrdering = value;
			}
		}

		/// <summary> The level of presolve the IPM solver will apply.
		/// -1 means default or automatic, 0 means no presolve, &gt;0 full.
		/// </summary>
		public int PresolveLevel
		{
			get
			{
				return _presolveLevel;
			}
			set
			{
				_presolveLevel = value;
			}
		}

		/// <summary> The maximum number of iterations. If negative, no limit.
		/// </summary>
		public int IterationLimit
		{
			get
			{
				return _iterationLimit;
			}
			set
			{
				_iterationLimit = value;
			}
		}

		/// <summary>
		/// Create a new IPM directive with default values.
		/// </summary>
		public InteriorPointMethodDirective()
		{
			_algorithm = InteriorPointMethodAlgorithm.Default;
			_gapTolerance = 0.0;
			_iterationLimit = InteriorPointSolverParams.DefaultIterationLimit;
			_presolveLevel = -1;
			_symbolicOrdering = InteriorPointSymbolicOrdering.Automatic;
		}

		/// <summary>
		/// Returns a representation of the directive as a string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Concat("IPM(TimeLimit = ", base.TimeLimit, ", MaximumGoalCount = ", base.MaximumGoalCount, ", Arithmetic = ", base.Arithmetic, ", GapTolerance = ", GapTolerance, ", Algorithm = ", Algorithm, ", IterationLimit = ", IterationLimit, ", PresolveLevel = ", PresolveLevel, ", SymbolicOrdering = ", SymbolicOrdering, ")");
		}
	}
}
