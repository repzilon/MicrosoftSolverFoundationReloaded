using System;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Parameters used by the interior point solver.
	/// </summary>
	public class InteriorPointSolverParams : ISolverParameters, ISolverEvents
	{
		private InteriorPointAlgorithmKind _ipmAlgorithm;

		private InteriorPointKktForm _ipmKKT;

		private bool _fAbort;

		private Func<bool> _fnQueryAbort;

		private Action _fnSolving;

		private SolverKind _solverKind;

		private int _threadCountLimit;

		private int _presolveLevel;

		private int _symbolicOrdering;

		private double _solveTolerance;

		internal static int DefaultIterationLimit = -1;

		/// <summary> Callback for ending the solve.
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

		/// <summary> Callback called during solve.
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

		/// <summary> Callback for the start of each iteration.
		/// </summary>
		public Func<bool> IterationStartedCallback { get; set; }

		/// <summary> Indicates that the solver should stop.  The value is sticky.
		/// </summary>
		public bool Abort
		{
			private get
			{
				return _fAbort;
			}
			set
			{
				_fAbort |= value;
			}
		}

		/// <summary> Choose an IPM algorithm. 
		/// </summary>
		public virtual InteriorPointAlgorithmKind IpmAlgorithm
		{
			get
			{
				return _ipmAlgorithm;
			}
			set
			{
				_ipmAlgorithm = value;
			}
		}

		/// <summary> Get or set the form of KKT matrix arithmetic to be used.
		/// </summary>
		public virtual InteriorPointKktForm IpmKKT
		{
			get
			{
				return _ipmKKT;
			}
			set
			{
				_ipmKKT = value;
			}
		}

		/// <summary> Choose a solver. 
		/// </summary>
		public virtual SolverKind KindOfSolver => _solverKind;

		/// <summary> The type of matrix ordering to apply.
		/// -1 means automatic, 0 is minimum fill, 1 is AMD.
		/// </summary>
		public int SymbolicOrdering
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

		/// <summary>
		/// If the matrix becomes dense we can switch algorithms in some places for more speed,
		///    a tradeoff against space.  This portion of the matrix is the "dense window".
		///    The start of the dense window is the first column that has at least this
		///    percentage of nonzeroes.
		/// </summary>
		public double MaxDenseColumnRatio { get; set; }

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
		public int MaxIterationCount { get; set; }

		/// <summary> Solve tolerance (gap, primal, dual).
		/// </summary>
		public double SolveTolerance
		{
			get
			{
				return _solveTolerance;
			}
			set
			{
				_solveTolerance = value;
			}
		}

		/// <summary>Set the maximum threads to use in algebra.
		/// </summary>
		internal virtual int ThreadCountLimit
		{
			get
			{
				return _threadCountLimit;
			}
			set
			{
				_threadCountLimit = value;
			}
		}

		/// <summary>Print debug information for a specific iteration.
		/// </summary>
		internal int DebugIteration { get; set; }

		/// <summary> Create a new instance.
		/// </summary>
		public InteriorPointSolverParams()
			: this((Func<bool>)null)
		{
		}

		/// <summary> Create a new instance from a Directive.
		/// </summary>
		public InteriorPointSolverParams(Directive directive)
			: this((Func<bool>)null)
		{
			FillInSolverParams(directive);
		}

		/// <summary> Create a new instance.
		/// </summary>
		/// <param name="fnQueryAbort">A callback delegate.</param>
		public InteriorPointSolverParams(Func<bool> fnQueryAbort)
		{
			_ipmAlgorithm = InteriorPointAlgorithmKind.HSD;
			_ipmKKT = InteriorPointKktForm.Blended;
			_fnQueryAbort = fnQueryAbort;
			_solverKind = SolverKind.InteriorPointCentral;
			_threadCountLimit = -1;
			_presolveLevel = -1;
			_symbolicOrdering = -1;
			_solveTolerance = 1E-08;
			MaxDenseColumnRatio = 0.8;
			MaxIterationCount = DefaultIterationLimit;
			DebugIteration = -1;
		}

		/// <summary> Copy constructor. 
		/// </summary>
		/// <param name="parameters">An InteriorPointSolverParams object.</param>
		public InteriorPointSolverParams(InteriorPointSolverParams parameters)
		{
			_ipmAlgorithm = parameters._ipmAlgorithm;
			_ipmKKT = parameters._ipmKKT;
			_fAbort = parameters._fAbort;
			_fnQueryAbort = parameters._fnQueryAbort;
			_symbolicOrdering = parameters._symbolicOrdering;
			_presolveLevel = parameters._presolveLevel;
			_solverKind = parameters._solverKind;
			_solveTolerance = parameters._solveTolerance;
			_threadCountLimit = parameters._threadCountLimit;
			DebugIteration = parameters.DebugIteration;
		}

		/// <summary> Callback on whether the solver should abort execution.
		/// </summary>
		/// <returns>True if the solver should stop, False otherwise.</returns>
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

		/// <summary> Callback before the solve starts.
		/// </summary>
		/// <param name="threadIndex">simplex thread id</param>
		/// <returns>Return code is not used.</returns>
		public virtual bool NotifyStartSolve(int threadIndex)
		{
			return !ShouldAbort();
		}

		/// <summary>
		/// Fill in InteriorPointSolverParams based on the InteriorPointMethodDirective instance passed in.
		/// </summary>
		private void FillInSolverParams(Directive dir)
		{
			if (dir != null && dir is InteriorPointMethodDirective interiorPointMethodDirective)
			{
				_presolveLevel = interiorPointMethodDirective.PresolveLevel;
				_symbolicOrdering = (int)interiorPointMethodDirective.SymbolicOrdering;
				MaxIterationCount = interiorPointMethodDirective.IterationLimit;
				switch (interiorPointMethodDirective.Algorithm)
				{
				case InteriorPointMethodAlgorithm.Default:
				case InteriorPointMethodAlgorithm.HomogeneousSelfDual:
					IpmAlgorithm = InteriorPointAlgorithmKind.HSD;
					IpmKKT = InteriorPointKktForm.Blended;
					break;
				default:
					IpmAlgorithm = InteriorPointAlgorithmKind.PredictorCorrector;
					IpmKKT = InteriorPointKktForm.Blended;
					break;
				}
				switch (interiorPointMethodDirective.Arithmetic)
				{
				}
			}
		}
	}
}
