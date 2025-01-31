using System;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>HybridLocalSearch parameters.
	/// </summary>
	public class HybridLocalSearchParameters : ISolverEvents, ISolverParameters
	{
		/// <summary>
		/// Callback called during solve when the solver changes states and,
		/// in particular, when it finds a new interesting solution
		/// </summary>
		public Action Solving { get; set; }

		/// <summary>
		/// Get/set the callback function that decides when to abort the search
		/// </summary>
		public Func<bool> QueryAbort { get; set; }

		/// <summary>
		/// Specifies that the Solve() method should never return. 
		/// Instead, the solver will run until interrupted (timeout or callback)
		/// and will regularly trigger events when improved solutions are found.
		/// </summary>
		public bool RunUntilTimeout { get; set; }

		/// <summary>
		/// Timelimit in milliseconds. If negative, no limit.
		/// </summary>
		public int TimeLimitMs { get; set; }

		/// <summary>Presolve level. (-1 is automatic, 0 is no presolve).
		/// </summary>
		public int PresolveLevel { get; set; }

		/// <summary>
		/// Tolerance for values to be still considered equal.
		/// </summary>
		public double EqualityTolerance { get; set; }

		/// <summary> Create a new instance.
		/// </summary>
		public HybridLocalSearchParameters()
			: this(new HybridLocalSearchDirective())
		{
		}

		/// <summary> Create a new instance from a Directive.
		/// </summary>
		public HybridLocalSearchParameters(Directive directive)
		{
			TimeLimitMs = directive.TimeLimit;
			if (directive is HybridLocalSearchDirective hybridLocalSearchDirective)
			{
				RunUntilTimeout = hybridLocalSearchDirective.RunUntilTimeout;
				PresolveLevel = hybridLocalSearchDirective.PresolveLevel;
				EqualityTolerance = ((hybridLocalSearchDirective.EqualityTolerance >= 0.0) ? hybridLocalSearchDirective.EqualityTolerance : 1E-08);
			}
			else
			{
				PresolveLevel = -1;
				EqualityTolerance = 1E-08;
			}
		}

		internal void CallSolvingEvent()
		{
			if (Solving != null)
			{
				Solving();
			}
		}

		internal bool CallQueryAbort()
		{
			if (QueryAbort != null)
			{
				return QueryAbort();
			}
			return false;
		}
	}
}
