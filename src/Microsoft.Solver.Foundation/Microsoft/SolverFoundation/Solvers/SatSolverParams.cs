using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Class for processing SatSolver parameters
	/// </summary>
	public class SatSolverParams
	{
		private bool _fAbort;

		private bool _fBiased;

		private bool _fInitialGuess;

		private double _dblRandVarProb;

		private double _dblRandSenseProb;

		private int _cvBackTrackMaxInit;

		private readonly Func<bool> _fnQueryAbort;

		/// <summary>
		/// Get/Set the abort flag to stop the solver
		/// </summary>
		public virtual bool Abort
		{
			get
			{
				if (!_fAbort)
				{
					if (_fnQueryAbort != null)
					{
						return _fnQueryAbort();
					}
					return false;
				}
				return true;
			}
			set
			{
				_fAbort = value;
			}
		}

		/// <summary>
		/// Whether the value choice is biased in the search
		/// </summary>
		public virtual bool Biased
		{
			get
			{
				return _fBiased;
			}
			set
			{
				_fBiased = value;
			}
		}

		/// <summary>
		/// If Biased is true, this indicates which direction will be tried first.
		/// </summary>
		public virtual bool InitialGuess
		{
			get
			{
				return _fInitialGuess;
			}
			set
			{
				_fInitialGuess = value;
			}
		}

		/// <summary>
		/// Probability that a variable is chosen at random.
		/// </summary>
		public virtual double RandVarProb
		{
			get
			{
				return _dblRandVarProb;
			}
			set
			{
				_dblRandVarProb = value;
			}
		}

		/// <summary>
		/// Probability that a sense is chosen at random.
		/// </summary>
		public virtual double RandSenseProb
		{
			get
			{
				return _dblRandSenseProb;
			}
			set
			{
				_dblRandSenseProb = value;
			}
		}

		/// <summary>
		/// Initial number of back tracks that triggers a restart.
		/// </summary>
		public virtual int BackTrackCountLimit
		{
			get
			{
				return _cvBackTrackMaxInit;
			}
			set
			{
				_cvBackTrackMaxInit = Math.Max(10, value);
			}
		}

		/// <summary>
		/// Create default parameter set
		/// </summary>
		public SatSolverParams()
			: this(null)
		{
		}

		/// <summary>
		/// Create default parameter set with a callback function to indicate when to stop
		/// </summary>
		/// <param name="fnQueryAbort"></param>
		public SatSolverParams(Func<bool> fnQueryAbort)
		{
			_dblRandVarProb = 0.02;
			_dblRandSenseProb = 0.1;
			_cvBackTrackMaxInit = 50;
			_fnQueryAbort = fnQueryAbort;
		}
	}
}
