using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>HSD solver parameters and stopping criteria.
	/// </summary>
	internal class HsdSolverOptions
	{
		/// <summary>Termination criteria for relative primal feasibility
		/// </summary>
		public double EpsPrimalInfeasible { get; set; }

		/// <summary>Termination criteria for relative dual feasibility
		/// </summary>
		public double EpsDualInfeasible { get; set; }

		/// <summary>Termination criteria for relative accuracy
		/// </summary>
		public double EpsAccuracy { get; set; }

		/// <summary>Termination criteria for duality gap change ratio 
		/// </summary>
		public double EpsMuRatio { get; set; }

		/// <summary>Termination criteria for tau/kappa (infeasibility)
		/// </summary>
		public double EpsTauKappaRatio { get; set; }

		/// <summary>maximum number of iterations allowed
		/// </summary>
		public int MaxIterations { get; set; }

		/// <summary>whether or not to use advanced start point
		/// </summary>
		public bool AdvancedStartingPoint { get; set; }

		/// <summary>max centering component in predictor-corrector-centering
		/// </summary>
		public double BetaMaxCentering { get; set; }

		/// <summary>ratio test for uniformity of complementarity 
		/// </summary>
		public double BetaComplementUnif { get; set; }

		/// <summary>back off ratio due to nonuniform complementarity
		/// </summary>
		public double BetaBackOffRatio { get; set; }

		/// <summary>maximum number of backoff attempts per iteration
		/// </summary>
		public int MaxBackoffAttempts { get; set; }

		/// <summary>whether or not to use Gondzio's multiple corrections
		/// </summary>
		public bool MultipleCorrections { get; set; }

		/// <summary>threshold for stepsize (if alpha is smaller) to do mc
		/// </summary>
		public double MCStepThreshold { get; set; }

		/// <summary>complementarity uniform bound (0.1 ~ 1/0.1) 
		/// </summary>
		public double MCComplementBound { get; set; }

		/// <summary>number of maximum corrections allowed
		/// </summary>
		public int MaxCorrections { get; set; }

		/// <summary>max number of IRs before changing cond for perturbation   
		/// </summary>
		public int MaxIR { get; set; }

		/// <summary>max number of IRs considered as fast convergence
		/// </summary>
		public int MaxFastIR { get; set; }

		/// <summary>relative residue norm for iterative refinements
		/// </summary>
		public double EpsIR { get; set; }

		/// <summary> Minimum size for quadratic cone to do sparse+low-rank update
		/// </summary>
		public int MinConeSizeLowRank { get; set; }

		/// <summary>Stopping criteria for relative duality gap.
		/// </summary>
		public double EpsDualGapInfeasible { get; set; }

		/// <summary>One-sided infinity norm of neighborhood size. (1 is central path) 
		/// </summary>
		public double BetaNeighborhood { get; set; }

		/// <summary>The threshhold for "insufficient step size".  Consecutive tiny steps
		/// indicate we are stuck and should consider returning with reduced accuracy.
		/// </summary>
		public double EpsInsufficientStep { get; set; }

		/// <summary>Symbolic factorization parameters.
		/// </summary>
		public FactorizationParameters FactorizationParameters { get; set; }

		/// <summary>Set primal, dual, gap tolerance.
		/// </summary>
		public void SetTolerance(double tolerance, bool setAll)
		{
			tolerance = Math.Max(Math.Min(0.01, tolerance), 1E-10);
			EpsPrimalInfeasible = tolerance;
			EpsDualInfeasible = tolerance;
			double epsAccuracy = (EpsDualGapInfeasible = tolerance * 0.1);
			EpsAccuracy = epsAccuracy;
			if (setAll)
			{
				EpsTauKappaRatio = tolerance;
			}
		}

		/// <summary>Create a new instance with default values.
		/// </summary>
		public HsdSolverOptions()
		{
			double num = 1E-08;
			SetTolerance(num, setAll: true);
			EpsMuRatio = num;
			MaxIterations = 99;
			AdvancedStartingPoint = false;
			BetaMaxCentering = 0.1;
			BetaComplementUnif = 0.0001;
			BetaBackOffRatio = 0.9;
			MaxBackoffAttempts = 10;
			MultipleCorrections = true;
			MCStepThreshold = 0.5;
			MCComplementBound = 0.1;
			MaxCorrections = 5;
			MaxIR = 6;
			MaxFastIR = 3;
			EpsIR = 1E-05;
			MinConeSizeLowRank = 10;
			EpsDualGapInfeasible = 1E-06;
			BetaNeighborhood = 1E-06;
			EpsInsufficientStep = 1E-08;
			FactorizationParameters = new FactorizationParameters();
		}
	}
}
