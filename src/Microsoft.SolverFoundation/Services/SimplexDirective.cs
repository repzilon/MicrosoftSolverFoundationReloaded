namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Directive for the simplex solver.
	/// </summary>
	/// <remarks>
	/// The simplex solver is suitable for linear models with real decisions.
	/// In the case of a model with no goals, the simplex solver will report an Optimal solution.
	/// By contrast the constraint programming solver reports Feasible.
	/// </remarks>
	public class SimplexDirective : Directive
	{
		private SimplexBasis _basisType;

		private bool _getSensitivity;

		private bool _getInfeasibility;

		private int _pivotCountLimit;

		private double _pricingTolerance;

		private SimplexAlgorithm _simplexAlgorithm;

		private SimplexPricing _simplexPricing;

		private double _variableTolerance;

		/// <summary> The pricing strategy to use.
		/// </summary>
		/// <remarks> The requested pricing strategy may not match that actually used by the solver.
		/// For example SteepestEdge pricing may be used if sensitivity information has been requested.
		/// </remarks>
		public SimplexPricing Pricing
		{
			get
			{
				return _simplexPricing;
			}
			set
			{
				_simplexPricing = value;
			}
		}

		/// <summary> The limit on number of pivots. If negative, no limit. 
		/// </summary>
		public int IterationLimit
		{
			get
			{
				return _pivotCountLimit;
			}
			set
			{
				_pivotCountLimit = value;
			}
		}

		/// <summary> The algorithm to use.
		/// </summary>
		public SimplexAlgorithm Algorithm
		{
			get
			{
				return _simplexAlgorithm;
			}
			set
			{
				_simplexAlgorithm = value;
			}
		}

		/// <summary> The basis to use.
		/// </summary>
		public SimplexBasis Basis
		{
			get
			{
				return _basisType;
			}
			set
			{
				_basisType = value;
			}
		}

		/// <summary>
		/// Whether to generate sensitivity information.
		/// </summary>
		public bool GetSensitivity
		{
			get
			{
				return _getSensitivity;
			}
			set
			{
				_getSensitivity = value;
			}
		}

		/// <summary>
		/// Whether to generate infeasibility information
		/// </summary>
		public bool GetInfeasibility
		{
			get
			{
				return _getInfeasibility;
			}
			set
			{
				_getInfeasibility = value;
			}
		}

		/// <summary>
		/// Numerical tolerance for simplex pricing.
		/// </summary>
		public double PricingTolerance
		{
			get
			{
				return _pricingTolerance;
			}
			set
			{
				_pricingTolerance = value;
			}
		}

		/// <summary>
		/// Numerical tolerance for variables. 
		/// </summary>
		public double VariableTolerance
		{
			get
			{
				return _variableTolerance;
			}
			set
			{
				_variableTolerance = value;
			}
		}

		/// <summary>Default constructor with default values for the simplex solver.
		/// </summary>
		public SimplexDirective()
		{
			_pivotCountLimit = -1;
			_simplexPricing = SimplexPricing.Default;
			_simplexAlgorithm = SimplexAlgorithm.Default;
			_basisType = SimplexBasis.Default;
			_getSensitivity = false;
			_getInfeasibility = false;
			_pricingTolerance = 0.0;
			_variableTolerance = 0.0;
		}

		/// <summary>
		/// Returns a representation of the directive as a string.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Concat("Simplex(TimeLimit = ", base.TimeLimit, ", MaximumGoalCount = ", base.MaximumGoalCount, ", Arithmetic = ", base.Arithmetic, ", Pricing = ", Pricing, ", IterationLimit = ", IterationLimit, ", Algorithm = ", Algorithm, ", Basis = ", Basis, ", GetSensitivity = ", GetSensitivity, ")");
		}
	}
}
