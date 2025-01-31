using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>SOCP solver using homogeneous and self-dual model.
	/// </summary>
	internal class SOCPSolverHSD : GeneralModel
	{
		private int _iterations;

		private int _displayProgress = 2;

		private double _gap;

		private Stopwatch _timer;

		private HsdSolverOptions _options;

		private SocpFactor _socpFactor;

		private Vector _sx;

		private Vector _yz;

		private double _tau;

		private double _kappa;

		private SubVector _sK;

		private SubVector _sP;

		private SubVector _sN;

		private SubVector _sG;

		private SubVector _sH;

		private SubVector _sV;

		private SubVector _xL;

		private SubVector _xU;

		private SubVector _xF;

		private SubVector _yG;

		private SubVector _zP;

		private SubVector _zN;

		private SubVector _zG;

		private SubVector _zH;

		private SubVector _zV;

		private SubVector _zL;

		private SubVector _zU;

		private SubVector _yGzK;

		private SubVector _xLxUxF;

		private SubVector _sxC;

		private SubVector _yzC;

		private double _eta;

		private double _gamma;

		private double[] thetaQ;

		private double[] thetaR;

		private List<Vector> wQ;

		private List<Vector> wR;

		private List<Vector> zQNT;

		private List<Vector> zRNT;

		private int _lowRankUpdateCount;

		private Matrix Wlr;

		private Matrix Ulr;

		private Matrix IplusUDU;

		private Vector UDvlr;

		private DenseLUFactor _denseFactor;

		private SubVector sKL;

		private SubVector sKQR;

		private SubVector sxPNGHVLU;

		private SubVector zKL;

		private SubVector zKQR;

		private SubVector zPNGHVLU;

		private Vector dsx;

		private Vector dyz;

		private double dtau;

		private double dkappa;

		private SubVector dsxC;

		private SubVector dyzC;

		private double dtauDenom;

		private double dtauNumer;

		private Vector dsxCdyzC;

		private SubVector tmpG;

		private SubVector tmpH;

		private SubVector tmpU;

		private Vector rp;

		private SubVector rpG;

		private SubVector rpK;

		private SubVector rpV;

		private SubVector rpH;

		private SubVector rpPN;

		private SubVector rpGrpK;

		private SubVector rpKL;

		private SubVector rpKQR;

		private Vector rd;

		private SubVector rdL;

		private SubVector rdU;

		private SubVector rdF;

		private SubVector rdG;

		private SubVector rdPN;

		private SubVector rdLrdUrdF;

		private double rg;

		private double cx;

		private double byuz;

		private double mu;

		private Vector rc;

		private double rcTK;

		private SubVector rcKL;

		private SubVector rcKQR;

		private SubVector rcP;

		private SubVector rcN;

		private SubVector rcG;

		private SubVector rcH;

		private SubVector rcV;

		private SubVector rcL;

		private SubVector rcU;

		private SubVector rcPNGHVLU;

		private Vector Dprml;

		private Vector Ddual;

		private Vector Drest;

		private SubVector DL;

		private SubVector DUV;

		private SubVector DPN;

		private SubVector DGH;

		private SubVector DK;

		private SubVector DU;

		private SubVector DV;

		private SubVector DG;

		private SubVector DH;

		private SubVector DP;

		private SubVector DN;

		private SubVector DKL;

		private SubVector DKQR;

		private SubVector DdualGKL;

		private Vector rr;

		private SubVector rrdL;

		private SubVector rrdU;

		private SubVector rrdF;

		private SubVector rrpG;

		private SubVector rrpKL;

		private SubVector rrpKQR;

		private Vector rrGH;

		private Vector rrPN;

		private Vector f;

		private Vector g;

		private SubVector fLfUfF;

		private SubVector fGfK;

		private SubVector gLgUgF;

		private SubVector gGgK;

		private SubVector fL;

		private SubVector fU;

		private SubVector fF;

		private SubVector fG;

		private SubVector fK;

		private SubVector gL;

		private SubVector gU;

		private SubVector gF;

		private SubVector gG;

		private SubVector gK;

		private SubVector fKL;

		private SubVector fKQR;

		private Vector DVuV;

		private Vector DGHDHuH;

		private Vector augSolutionVec;

		private SubVector DprmlPerturbed;

		private SubVector DdualPerturbed;

		private SubVector DdualPerturbedGKL;

		private SubVector tempLUF;

		private SubVector tempG;

		private SubVector tempKL;

		private SubVector tempKQR;

		private Vector augResidueVec;

		private double augResidueNorm;

		private bool isPertPhase;

		private List<int> prmlPositiveList;

		private double dmaxPrmlPosList;

		private double prmlCond = 1.0;

		private double prmlPertCond = 1.0;

		private List<int> dualPositiveList;

		private double dmaxDualPosList;

		private double dualCond = 1.0;

		private double dualPertCond = 1.0;

		private int nIterRefine;

		private int nPivotPert;

		private bool _needPrimalDual;

		/// <summary> This async callback returns true if a task needs to be stopped,
		///           for example by timeout, or if an exception in one thread needs
		///           to stop all the others.
		/// </summary>
		internal Func<bool> CheckAbort;

		/// <summary> Solving callback.
		/// </summary>
		private Action<InteriorPointSolveState> Solving;

		/// <summary>Called at the start of each iteration.
		/// </summary>
		internal Func<bool> IterationStartedCallback;

		protected override double SolveTolerance => _options.EpsAccuracy;

		/// <summary>Row count.
		/// </summary>
		public override int RowCount
		{
			get
			{
				if (bGbK != null)
				{
					return bGbK.Length;
				}
				return model.RowCount;
			}
		}

		/// <summary>Variable count.
		/// </summary>
		public override int VarCount
		{
			get
			{
				if (cLcUcF != null)
				{
					return cLcUcF.Length;
				}
				return model.VariableCount;
			}
		}

		/// <summary>Iteration count.
		/// </summary>
		public override int IterationCount => _iterations;

		/// <summary>Duality gap.
		/// </summary>
		public override double Gap => _gap;

		/// <summary>The algorithm kind.
		/// </summary>
		public override InteriorPointAlgorithmKind Algorithm => InteriorPointAlgorithmKind.SOCP;

		/// <summary>The KKT formulation used by the algorithm.
		/// </summary>
		public override InteriorPointKktForm KktForm => InteriorPointKktForm.Augmented;

		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="solver">The SecondOrderConicModel containing user data.</param>
		/// <param name="log">The LogSource.</param>
		/// <param name="prm">Solver parameters.</param>
		public SOCPSolverHSD(InteriorPointSolver solver, LogSource log, InteriorPointSolverParams prm)
			: base(solver, log, prm.PresolveLevel)
		{
			SOCPSolverHSD sOCPSolverHSD = this;
			CheckAbort = prm.ShouldAbort;
			Action<InteriorPointSolveState> solving = delegate(InteriorPointSolveState s)
			{
				sOCPSolverHSD.SolveState = s;
				if (prm.Solving != null)
				{
					prm.Solving();
				}
			};
			Solving = solving;
			IterationStartedCallback = prm.IterationStartedCallback;
			_timer = new Stopwatch();
			_options = new HsdSolverOptions();
			_options.SetTolerance(1E-06, setAll: true);
			_needPrimalDual = true;
			_primal = (_dual = (_gap = double.NaN));
		}

		public override LinearResult Solve(InteriorPointSolverParams prm)
		{
			if (base.Solution.status == LinearResult.Optimal || base.Solution.status == LinearResult.UnboundedPrimal)
			{
				return base.Solution.status;
			}
			if (base.Solution.status == LinearResult.InfeasiblePrimal)
			{
				_dual = (_primal = (_gap = double.NaN));
				base.Solution.cx = double.NaN;
				base.Solution.by = double.NaN;
				return base.Solution.status;
			}
			LinearResult result;
			try
			{
				result = SolveCore();
			}
			catch (TimeLimitReachedException)
			{
				result = LinearResult.Interrupted;
			}
			if (_needPrimalDual)
			{
				_primal = cLcUcF.BigInnerProduct(_xLxUxF).ToDouble() / _tau;
				_primal += cxShift;
				_dual = (bGbK.BigInnerProduct(_yGzK) - uV.BigInnerProduct(_zV) - uH.BigInnerProduct(_zH)).ToDouble() / _tau;
				_dual += cxShift;
				_gap = GetRelativeGap();
			}
			return result;
		}

		/// <summary> Indicates whether there are any decision variables.
		/// </summary>
		protected override bool EmptySolution()
		{
			return _xLxUxF == null;
		}

		protected override Rational MapRowVidToValue(ref VidToVarMap vvm, int vid)
		{
			switch (vvm.kind)
			{
			case VidToVarMapKind.RowConstant:
				return vvm.lower;
			case VidToVarMapKind.RowUnbounded:
			{
				Rational result = 0;
				{
					foreach (LinearEntry rowEntry in model.GetRowEntries(vid))
					{
						result += rowEntry.Value * MapColVidToValue(rowEntry.Index);
					}
					return result;
				}
			}
			case VidToVarMapKind.RowLower:
				return vvm.lower + _sK[vvm.iVar - mG];
			case VidToVarMapKind.RowBounded:
				if (!(vvm.lower == vvm.upper))
				{
					return vvm.lower + _sG[vvm.iVar];
				}
				return vvm.lower;
			case VidToVarMapKind.RowUpper:
				return vvm.upper - _sK[vvm.iVar - mG];
			default:
				return Rational.Indeterminate;
			}
		}

		protected override Rational MapColVidToValue(ref VidToVarMap vvm)
		{
			if (vvm.iVar < 0)
			{
				return vvm.lower;
			}
			return GeneralModel.MapToUserModel(ref vvm, _xLxUxF[vvm.iVar]);
		}

		/// <summary> Return the dual value for a row constraint.
		/// </summary>
		/// <param name="vidRow">Row vid.</param>
		/// <returns>The dual value.</returns>
		public override Rational GetDualValue(int vidRow)
		{
			return GetDualValue(_yGzK, vidRow);
		}

		private bool IterationStarted()
		{
			_primal = cx / _tau + cxShift;
			_dual = byuz / _tau + cxShift;
			_gap = GetRelativeGap();
			if (IterationStartedCallback != null && IterationStartedCallback())
			{
				return !CheckAbort();
			}
			return true;
		}

		private LinearResult SolveCore()
		{
			SocpResult socpResult = SocpResult.Invalid;
			base.Logger.LogEvent(15, Resources.LogComputationalProgressOfHSDSolver);
			_timer.Reset();
			_timer.Start();
			Solving(InteriorPointSolveState.Init);
			if (CheckAbort())
			{
				_needPrimalDual = false;
				return LinearResult.Interrupted;
			}
			AllocateMemory();
			if (AGKLUF == null)
			{
				SolveZeroConstraints(_xLxUxF);
				_gap = 0.0;
				_needPrimalDual = false;
				return base.Solution.status;
			}
			Solving(InteriorPointSolveState.SymbolicFactorization);
			InitializeLinearSolver();
			if (CheckAbort())
			{
				return LinearResult.Interrupted;
			}
			SimpleStartingPoint();
			ComputeResidues();
			double num = bGbK.Norm2();
			double num2 = uH.Norm2();
			double num3 = uV.Norm2();
			double num4 = Math.Sqrt(num * num + num2 * num2 + num3 * num3);
			double val = cLcUcF.Norm2();
			mu = (_sxC.InnerProduct(_yzC) + _tau * _kappa) / (double)(nC + 1);
			double num5 = mu;
			double num6 = 0.0;
			_gamma = 0.0;
			int nCorrections = 0;
			while (true)
			{
				double num7 = rp.Norm2() / Math.Max(1.0, num4);
				double num8 = rd.Norm2() / Math.Max(1.0, val);
				double num9 = Math.Abs(rg) / Math.Max(1.0, Math.Max(num4, val));
				double relativeGap = GetRelativeGap();
				DisplayProgress(num6, nCorrections, num7, num8, relativeGap);
				if (CheckAbort())
				{
					socpResult = SocpResult.Interrupted;
					break;
				}
				if (num7 < _options.EpsPrimalInfeasible && num8 < _options.EpsDualInfeasible && relativeGap < _options.EpsAccuracy)
				{
					socpResult = SocpResult.Optimal;
					break;
				}
				if (num7 < _options.EpsPrimalInfeasible && num8 < _options.EpsDualInfeasible && num9 < _options.EpsDualGapInfeasible && _tau < _options.EpsTauKappaRatio * Math.Max(1.0, _kappa))
				{
					socpResult = ((!(cx < 0.0)) ? ((byuz > 0.0) ? SocpResult.InfeasiblePrimal : SocpResult.InfeasiblePrimalOrDual) : ((!(byuz > 0.0)) ? SocpResult.InfeasibleDual : SocpResult.InfeasiblePrimalAndDual));
					break;
				}
				if (mu < _options.EpsMuRatio * num5 && _tau < _options.EpsTauKappaRatio * Math.Min(1.0, _kappa))
				{
					socpResult = SocpResult.IllPosed;
					break;
				}
				if (_iterations >= _options.MaxIterations || double.IsNaN(num7) || !IterationStarted())
				{
					socpResult = SocpResult.Interrupted;
					break;
				}
				ComputeDiagonals();
				prmlCond = PrmlConditionNumber();
				dualCond = DualConditionNumber();
				if (isPertPhase)
				{
					ComputeDiagPerturbation();
					_socpFactor.SetLinearSystemDiagonals(DprmlPerturbed, DdualPerturbedGKL, thetaQ, thetaR, wQ, wR, _options.MinConeSizeLowRank);
					nPivotPert = NumericalFactorization();
					_socpFactor.SetLinearSystemDiagonals(Dprml, DdualGKL, thetaQ, thetaR, wQ, wR, _options.MinConeSizeLowRank);
				}
				else
				{
					_socpFactor.SetLinearSystemDiagonals(Dprml, DdualGKL, thetaQ, thetaR, wQ, wR, _options.MinConeSizeLowRank);
					nPivotPert = NumericalFactorization();
				}
				if (_lowRankUpdateCount > 0)
				{
					PrepareLowRankUpdate();
				}
				nIterRefine = 0;
				augResidueNorm = 0.0;
				fLfUfF.CopyFrom(cLcUcF);
				Vector.Daxpy(-1.0, DVuV, fU);
				fGfK.CopyFrom(bGbK);
				Vector.Daxpy(1.0, DGHDHuH, fG);
				int val2 = SolveAugmentedSystemIR(f, ref g);
				nIterRefine = Math.Max(nIterRefine, val2);
				ComputeDeltaTauDenominator();
				_eta = 1.0;
				ComputeRightHandSide();
				val2 = SolveAugmentedSystemIR(rr, ref f);
				nIterRefine = Math.Max(nIterRefine, val2);
				AssembleSearchDirection(ref dsx, ref dyz, ref dtau, ref dkappa);
				FindVariablePartitions();
				num6 = FindMaxStepSize(dsxC, dyzC, dtau, dkappa);
				_gamma = 1.0 - num6;
				_gamma *= Math.Min(_gamma, _options.BetaMaxCentering);
				_eta = 1.0 - _gamma;
				AddCenteringAndCorrector();
				ComputeRightHandSide();
				val2 = SolveAugmentedSystemIR(rr, ref f);
				nIterRefine = Math.Max(nIterRefine, val2);
				AssembleSearchDirection(ref dsx, ref dyz, ref dtau, ref dkappa);
				num6 = FindMaxStepSize(dsxC, dyzC, dtau, dkappa);
				num6 *= 0.9;
				UpdatePrmlDualVariables(num6);
				Vector.ElementMultiply(_sxC, _yzC, dsxCdyzC);
				mu = (dsxCdyzC.Sum() + _tau * _kappa) / (double)(nC + 1);
				ComputeResidues();
				_iterations++;
			}
			switch (socpResult)
			{
			case SocpResult.Optimal:
			case SocpResult.NumericalDifficulty:
			case SocpResult.Interrupted:
				_sx.ScaleBy(1.0 / _tau);
				_yz.ScaleBy(1.0 / _tau);
				_kappa /= _tau;
				_tau = 1.0;
				break;
			}
			base.Logger.LogEvent(15, Resources.IpmCoreSolutionTime0, _timer.Elapsed.TotalSeconds);
			return ToLinearResult(socpResult);
		}

		private double GetRelativeGap()
		{
			double value = cx - byuz;
			return Math.Abs(value) / (_tau + Math.Abs(byuz));
		}

		[SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "cx")]
		[SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "nPP")]
		[SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "perCond")]
		private void DisplayProgress(double alpha, int nCorrections, double rel_rp, double rel_rd, double rel_ac)
		{
			Solving(InteriorPointSolveState.IterationStarted);
			if (_iterations % 50 == 0)
			{
				switch (_displayProgress)
				{
				case 1:
					base.Logger.LogEvent(15, "IPM computational progress:");
					base.Logger.LogEvent(15, " k     rp     rd     gap      k/t     mu   gamma C alpha       cx          ");
					break;
				case 2:
					base.Logger.LogEvent(15, "IPM computational progress:");
					base.Logger.LogEvent(15, " k    rp      rd     gap      k/t     mu   gamma C alpha nPP IR perCond residue");
					break;
				}
			}
			switch (_displayProgress)
			{
			case 1:
				base.Logger.LogEvent(15, "{0,2} {1:0.0E+00} {2:0.0E+00} {3:0.0E+00} {4:0.0E+00} {5:0.0E+00} {6:F3} {7:d1} {8:F3} {9,14:0.0000000E+00} {10:F2}", _iterations, rel_rp, rel_rd, rel_ac, _kappa / _tau, mu, _gamma, nCorrections, alpha, cx / _tau + cxShift, _timer.Elapsed.TotalSeconds);
				break;
			case 2:
				base.Logger.LogEvent(15, "{0,2} {1:0.0E+00} {2:0.0E+00} {3:0.0E+00} {4:0.0E+00} {5:0.0E+00} {6:F3} {7:d1} {8:F2} {9,4} {10,2} {11:0.0E+00} {12:0.0E+00}", _iterations, rel_rp, rel_rd, rel_ac, _kappa / _tau, mu, _gamma, nCorrections, alpha, nPivotPert, nIterRefine, prmlPertCond, augResidueNorm);
				break;
			}
		}

		private static LinearResult ToLinearResult(SocpResult result)
		{
			switch (result)
			{
			case SocpResult.InfeasibleDual:
				return LinearResult.UnboundedPrimal;
			case SocpResult.InfeasiblePrimal:
			case SocpResult.InfeasiblePrimalAndDual:
			case SocpResult.InfeasiblePrimalOrDual:
				return LinearResult.InfeasiblePrimal;
			case SocpResult.Interrupted:
				return LinearResult.Interrupted;
			case SocpResult.Optimal:
				return LinearResult.Optimal;
			default:
				return LinearResult.Invalid;
			}
		}

		private void AllocateMemory()
		{
			mQ = kone.mQ.Sum();
			mR = kone.mR.Sum();
			mK = kone.kL + mQ + mR;
			int num = kone.kL + kone.mQ.Count + kone.mR.Count;
			nC = num + nF + nF + mG + mG + nU + nL + nU;
			mC = mK + nF + nF + mG + mG + nU + nL + nU;
			thetaQ = new double[kone.mQ.Count];
			thetaR = new double[kone.mR.Count];
			wQ = new List<Vector>(kone.mQ.Count);
			wR = new List<Vector>(kone.mR.Count);
			zQNT = new List<Vector>(kone.mQ.Count);
			zRNT = new List<Vector>(kone.mR.Count);
			for (int i = 0; i < kone.mQ.Count; i++)
			{
				wQ.Add(new Vector(kone.mQ[i]));
				zQNT.Add(new Vector(kone.mQ[i]));
			}
			for (int j = 0; j < kone.mR.Count; j++)
			{
				wR.Add(new Vector(kone.mR[j]));
				zRNT.Add(new Vector(kone.mR[j]));
			}
			_lowRankUpdateCount = 0;
			for (int k = 0; k < kone.mQ.Count; k++)
			{
				if (kone.mQ[k] >= _options.MinConeSizeLowRank)
				{
					_lowRankUpdateCount++;
				}
			}
			for (int l = 0; l < kone.mR.Count; l++)
			{
				if (kone.mR[l] >= _options.MinConeSizeLowRank)
				{
					_lowRankUpdateCount++;
				}
			}
			if (_lowRankUpdateCount > 0)
			{
				Wlr = new Matrix(nL + nU + nF + mG + mK, _lowRankUpdateCount);
				Ulr = new Matrix(nL + nU + nF + mG + mK, _lowRankUpdateCount);
				IplusUDU = new Matrix(_lowRankUpdateCount, _lowRankUpdateCount);
				_denseFactor = new DenseLUFactor(IplusUDU);
				UDvlr = new Vector(_lowRankUpdateCount);
			}
			_sx = new Vector(mK + nF + nF + mG + mG + nU + nL + nU + nF);
			_sK = new SubVector(_sx, 0, mK);
			_sP = new SubVector(_sx, mK, nF);
			_sN = new SubVector(_sx, mK + nF, nF);
			_sG = new SubVector(_sx, mK + nF + nF, mG);
			_sH = new SubVector(_sx, mK + nF + nF + mG, mG);
			_sV = new SubVector(_sx, mK + nF + nF + mG + mG, nU);
			_xL = new SubVector(_sx, mK + nF + nF + mG + mG + nU, nL);
			_xU = new SubVector(_sx, mK + nF + nF + mG + mG + nU + nL, nU);
			_xF = new SubVector(_sx, mK + nF + nF + mG + mG + nU + nL + nU, nF);
			_xLxUxF = new SubVector(_sx, mK + nF + nF + mG + mG + nU, nL + nU + nF);
			_sxC = new SubVector(_sx, 0, mC);
			sKL = new SubVector(_sx, 0, kone.kL);
			sKQR = new SubVector(_sx, kone.kL, mQ + mR);
			sxPNGHVLU = new SubVector(_sx, mK, nF + nF + mG + mG + nU + nL + nU);
			_yz = new Vector(mG + mK + nF + nF + mG + mG + nU + nL + nU);
			_yG = new SubVector(_yz, 0, mG);
			_zP = new SubVector(_yz, mG + mK, nF);
			_zN = new SubVector(_yz, mG + mK + nF, nF);
			_zG = new SubVector(_yz, mG + mK + nF + nF, mG);
			_zH = new SubVector(_yz, mG + mK + nF + nF + mG, mG);
			_zV = new SubVector(_yz, mG + mK + nF + nF + mG + mG, nU);
			_zL = new SubVector(_yz, mG + mK + nF + nF + mG + mG + nU, nL);
			_zU = new SubVector(_yz, mG + mK + nF + nF + mG + mG + nU + nL, nU);
			_yGzK = new SubVector(_yz, 0, mG + mK);
			_yzC = new SubVector(_yz, mG, mC);
			zKL = new SubVector(_yz, mG, kone.kL);
			zKQR = new SubVector(_yz, mG + kone.kL, mQ + mR);
			zPNGHVLU = new SubVector(_yz, mG + mK, nF + nF + mG + mG + nU + nL + nU);
			dsx = new Vector(mK + nF + nF + mG + mG + nU + nL + nU + nF);
			dsxC = new SubVector(dsx, 0, mC);
			dyz = new Vector(mG + mK + nF + nF + mG + mG + nU + nL + nU);
			dyzC = new SubVector(dyz, mG, mC);
			dsxCdyzC = new Vector(mC);
			tmpG = new SubVector(dsxCdyzC, mK + nF + nF, mG);
			tmpH = new SubVector(dsxCdyzC, mK + nF + nF + mG, mG);
			tmpU = new SubVector(dsxCdyzC, mK + nF + nF + mG + mG, nU);
			rp = new Vector(mG + mK + nU + mG + nF);
			rpG = new SubVector(rp, 0, mG);
			rpK = new SubVector(rp, mG, mK);
			rpV = new SubVector(rp, mG + mK, nU);
			rpH = new SubVector(rp, mG + mK + nU, mG);
			rpPN = new SubVector(rp, mG + mK + nU + mG, nF);
			rpGrpK = new SubVector(rp, 0, mG + mK);
			rpKL = new SubVector(rpK, 0, kone.kL);
			rpKQR = new SubVector(rpK, kone.kL, mQ + mR);
			rd = new Vector(nL + nU + nF + mG + nF);
			rdL = new SubVector(rd, 0, nL);
			rdU = new SubVector(rd, nL, nU);
			rdF = new SubVector(rd, nL + nU, nF);
			rdG = new SubVector(rd, nL + nU + nF, mG);
			rdPN = new SubVector(rd, nL + nU + nF + mG, nF);
			rdLrdUrdF = new SubVector(rd, 0, nL + nU + nF);
			rc = new Vector(mK + nF + nF + mG + mG + nU + nL + nU);
			rcKL = new SubVector(rc, 0, kone.kL);
			rcKQR = new SubVector(rc, kone.kL, kone.mQ.Sum() + kone.mR.Sum());
			rcP = new SubVector(rc, mK, nF);
			rcN = new SubVector(rc, mK + nF, nF);
			rcG = new SubVector(rc, mK + nF + nF, mG);
			rcH = new SubVector(rc, mK + nF + nF + mG, mG);
			rcV = new SubVector(rc, mK + nF + nF + mG + mG, nU);
			rcL = new SubVector(rc, mK + nF + nF + mG + mG + nU, nL);
			rcU = new SubVector(rc, mK + nF + nF + mG + mG + nU + nL, nU);
			rcPNGHVLU = new SubVector(rc, mK, nF + nF + mG + mG + nU + nL + nU);
			Dprml = new Vector(nL + nU + nF);
			Ddual = new Vector(mG + mK);
			Drest = new Vector(nU + nU + mG + mG + nF + nF);
			DL = new SubVector(Dprml, 0, nL);
			DUV = new SubVector(Dprml, nL, nU);
			DPN = new SubVector(Dprml, nL + nU, nF);
			DGH = new SubVector(Ddual, 0, mG);
			DK = new SubVector(Ddual, mG, mK);
			DU = new SubVector(Drest, 0, nU);
			DV = new SubVector(Drest, nU, nU);
			DG = new SubVector(Drest, nU + nU, mG);
			DH = new SubVector(Drest, nU + nU + mG, mG);
			DP = new SubVector(Drest, nU + nU + mG + mG, nF);
			DN = new SubVector(Drest, nU + nU + mG + mG + nF, nF);
			DKL = new SubVector(DK, 0, kone.kL);
			DKQR = new SubVector(DK, kone.kL, mQ + mR);
			DdualGKL = new SubVector(Ddual, 0, mG + kone.kL);
			rr = new Vector(nL + nU + nF + mG + mK);
			rrdL = new SubVector(rr, 0, nL);
			rrdU = new SubVector(rr, nL, nU);
			rrdF = new SubVector(rr, nL + nU, nF);
			rrpG = new SubVector(rr, nL + nU + nF, mG);
			rrpKL = new SubVector(rr, nL + nU + nF + mG, kone.kL);
			rrpKQR = new SubVector(rr, nL + nU + nF + mG + kone.kL, mQ + mR);
			rrGH = new Vector(mG);
			rrPN = new Vector(nF);
			f = new Vector(nL + nU + nF + mG + mK);
			g = new Vector(nL + nU + nF + mG + mK);
			fLfUfF = new SubVector(f, 0, nL + nU + nF);
			fGfK = new SubVector(f, nL + nU + nF, mG + mK);
			gLgUgF = new SubVector(g, 0, nL + nU + nF);
			gGgK = new SubVector(g, nL + nU + nF, mG + mK);
			fL = new SubVector(f, 0, nL);
			fU = new SubVector(f, nL, nU);
			fF = new SubVector(f, nL + nU, nF);
			fG = new SubVector(f, nL + nU + nF, mG);
			fK = new SubVector(f, nL + nU + nF + mG, mK);
			fKL = new SubVector(fK, 0, kone.kL);
			fKQR = new SubVector(fK, kone.kL, mQ + mR);
			gL = new SubVector(g, 0, nL);
			gU = new SubVector(g, nL, nU);
			gF = new SubVector(g, nL + nU, nF);
			gG = new SubVector(g, nL + nU + nF, mG);
			gK = new SubVector(g, nL + nU + nF + mG, mK);
			DVuV = new Vector(nU);
			DGHDHuH = new Vector(mG);
			augResidueVec = new Vector(nL + nU + nF + mG + mK);
			augSolutionVec = new Vector(nL + nU + nF + mG + mK);
			DprmlPerturbed = new SubVector(augSolutionVec, 0, nL + nU + nF);
			DdualPerturbed = new SubVector(augSolutionVec, nL + nU + nF, mG + mK);
			DdualPerturbedGKL = new SubVector(DdualPerturbed, 0, mG + kone.kL);
			tempLUF = new SubVector(augSolutionVec, 0, nL + nU + nF);
			tempG = new SubVector(augSolutionVec, nL + nU + nF, mG);
			tempKL = new SubVector(augSolutionVec, nL + nU + nF + mG, kone.kL);
			tempKQR = new SubVector(augSolutionVec, nL + nU + nF + mG + kone.kL, mQ + mR);
			prmlPositiveList = new List<int>(nL + nU + nF);
			for (int m = 0; m < nF; m++)
			{
				prmlPositiveList.Add(nL + nU + m);
			}
			dualPositiveList = new List<int>(mG + mK);
		}

		private void InitializeLinearSolver()
		{
			TimeSpan elapsed = _timer.Elapsed;
			SparseMatrixDouble m = SocpLinearSystem.Create(AGKLUF, Dprml, DdualGKL, thetaQ, thetaR, wQ, wR, _options.MinConeSizeLowRank);
			_socpFactor = new SocpFactor(m, CheckAbort, new FactorizationParameters());
			base.Logger.LogEvent(15, Resources.SymbolicFactorizationTime0SecType1, (_timer.Elapsed - elapsed).TotalSeconds, _socpFactor.Parameters.FactorizationMethod);
			base.Logger.LogEvent(15, Resources.SymbolicFactorNonzeros0, AGKLUF.ColumnCount, AGKLUF.Count, 1.0 * (double)AGKLUF.Count / (1.0 * (double)AGKLUF.RowCount * (double)AGKLUF.ColumnCount), "A");
			base.Logger.LogEvent(15, Resources.SymbolicFactorNonzeros0, _socpFactor.ColumnCount, _socpFactor.Count, 1.0 * (double)_socpFactor.Count / (1.0 * (double)_socpFactor.ColumnCount * (double)_socpFactor.ColumnCount), "L");
		}

		private int NumericalFactorization()
		{
			_socpFactor.ZeroPivots = 0;
			_socpFactor.Cholesky();
			return _socpFactor.ZeroPivots;
		}

		private void SimpleStartingPoint()
		{
			_sxC.ConstantFill(1.0);
			_yzC.ConstantFill(1.0);
			_xF.ConstantFill(0.0);
			_yG.ConstantFill(0.0);
			_tau = 1.0;
			_kappa = 1.0;
			int num = 0;
			for (int i = 0; i < kone.mQ.Count; i++)
			{
				SubVector v = new SubVector(sKQR, num, kone.mQ[i]);
				QuadraticCone.SimpleInitialization(v);
				SubVector v2 = new SubVector(zKQR, num, kone.mQ[i]);
				QuadraticCone.SimpleInitialization(v2);
				num += kone.mQ[i];
			}
			for (int j = 0; j < kone.mR.Count; j++)
			{
				SubVector v3 = new SubVector(sKQR, num, kone.mR[j]);
				RotatedQuadraticCone.SimpleInitialization(v3);
				SubVector v4 = new SubVector(zKQR, num, kone.mR[j]);
				RotatedQuadraticCone.SimpleInitialization(v4);
				num += kone.mR[j];
			}
		}

		private void ComputeResidues()
		{
			rpG.CopyFrom(_sG);
			rpK.CopyFrom(_sK);
			Vector.Daxpy(_tau, bGbK, rpGrpK);
			AGKLUF.SumProductRight(-1.0, _xLxUxF, 1.0, rpGrpK);
			rpV.CopyFrom(_sV);
			Vector.Daxpy(0.0 - _tau, uV, rpV);
			Vector.Daxpy(1.0, _xU, rpV);
			rpH.CopyFrom(_sH);
			Vector.Daxpy(0.0 - _tau, uH, rpH);
			Vector.Daxpy(1.0, _sG, rpH);
			rpPN.CopyFrom(_sP);
			Vector.Daxpy(-1.0, _sN, rpPN);
			Vector.Daxpy(-1.0, _xF, rpPN);
			rdL.CopyFrom(_zL);
			rdU.CopyFrom(_zU);
			rdF.CopyFrom(_zP);
			AGKLUF.SumLeftProduct(-1.0, _yGzK, -1.0, rdLrdUrdF);
			Vector.Daxpy(1.0, _zV, rdU);
			Vector.Daxpy(_tau, cLcUcF, rdLrdUrdF);
			rdG.CopyFrom(_yG);
			Vector.Daxpy(1.0, _zH, rdG);
			Vector.Daxpy(-1.0, _zG, rdG);
			rdPN.CopyFrom(_zN);
			rdPN.ScaleBy(-1.0);
			Vector.Daxpy(-1.0, _zP, rdPN);
			cx = cLcUcF.InnerProduct(_xLxUxF);
			byuz = bGbK.InnerProduct(_yGzK) - uV.InnerProduct(_zV) - uH.InnerProduct(_zH);
			rg = cx - byuz + _kappa;
			Vector.ElementMultiply(sKL, zKL, rcKL);
			Vector.ElementMultiply(sxPNGHVLU, zPNGHVLU, rcPNGHVLU);
			int num = 0;
			for (int i = 0; i < kone.mQ.Count; i++)
			{
				SubVector s = new SubVector(sKQR, num, kone.mQ[i]);
				SubVector z = new SubVector(zKQR, num, kone.mQ[i]);
				SubVector vVe = new SubVector(rcKQR, num, kone.mQ[i]);
				thetaQ[i] = QuadraticCone.FormWvector(s, z, wQ[i]);
				QuadraticCone.MultiplyByTW(thetaQ[i], wQ[i], z, zQNT[i]);
				SOCPUtilities.Arw2e(zQNT[i], vVe);
				num += kone.mQ[i];
			}
			for (int j = 0; j < kone.mR.Count; j++)
			{
				SubVector s2 = new SubVector(sKQR, num, kone.mR[j]);
				SubVector z2 = new SubVector(zKQR, num, kone.mR[j]);
				SubVector vVe2 = new SubVector(rcKQR, num, kone.mR[j]);
				thetaR[j] = RotatedQuadraticCone.FormWvector(s2, z2, wR[j]);
				RotatedQuadraticCone.MultiplyByTW(thetaR[j], wR[j], z2, zRNT[j]);
				SOCPUtilities.Arw2e(zRNT[j], vVe2);
				num += kone.mR[j];
			}
			rc.ScaleBy(-1.0);
			rcTK = (0.0 - _tau) * _kappa;
		}

		private void AddCenteringAndCorrector()
		{
			SubVector x = new SubVector(dsxC, 0, kone.kL);
			SubVector vector = new SubVector(dsxC, kone.kL, mQ + mR);
			SubVector x2 = new SubVector(dsxC, mK, nF + nF + mG + mG + nU + nL + nU);
			SubVector y = new SubVector(dyzC, 0, kone.kL);
			SubVector vector2 = new SubVector(dyzC, kone.kL, mQ + mR);
			SubVector y2 = new SubVector(dyzC, mK, nF + nF + mG + mG + nU + nL + nU);
			SubVector subVector = new SubVector(dsxCdyzC, 0, kone.kL);
			SubVector vector3 = new SubVector(dsxCdyzC, kone.kL, mQ + mR);
			SubVector subVector2 = new SubVector(dsxCdyzC, mK, nF + nF + mG + mG + nU + nL + nU);
			Vector.ElementMultiply(x, y, subVector);
			rcKL.AddConstant(_gamma * mu);
			Vector.Daxpy(-1.0, subVector, rcKL);
			Vector.ElementMultiply(x2, y2, subVector2);
			rcPNGHVLU.AddConstant(_gamma * mu);
			Vector.Daxpy(-1.0, subVector2, rcPNGHVLU);
			int num = 0;
			for (int i = 0; i < kone.mQ.Count; i++)
			{
				SubVector subVector3 = new SubVector(rcKQR, num, kone.mQ[i]);
				SubVector s = new SubVector(vector, num, kone.mQ[i]);
				SubVector z = new SubVector(vector2, num, kone.mQ[i]);
				SubVector subVector4 = new SubVector(vector3, num, kone.mQ[i]);
				SubVector subVector5 = new SubVector(fKQR, num, kone.mQ[i]);
				SubVector subVector6 = new SubVector(rrpKQR, num, kone.mQ[i]);
				QuadraticCone.MultiplyByTW(thetaQ[i], wQ[i], z, subVector5);
				QuadraticCone.MultiplyByTWinverse(thetaQ[i], wQ[i], s, subVector4);
				SOCPUtilities.ArwArwe(subVector4, subVector5, subVector6);
				Vector.Daxpy(-1.0, subVector6, subVector3);
				SOCPUtilities.IncrementFirstElement(subVector3, _gamma * mu);
				num += kone.mQ[i];
			}
			for (int j = 0; j < kone.mR.Count; j++)
			{
				SubVector subVector7 = new SubVector(rcKQR, num, kone.mR[j]);
				SubVector s2 = new SubVector(vector, num, kone.mR[j]);
				SubVector z2 = new SubVector(vector2, num, kone.mR[j]);
				SubVector subVector8 = new SubVector(vector3, num, kone.mR[j]);
				SubVector subVector9 = new SubVector(fKQR, num, kone.mR[j]);
				SubVector subVector10 = new SubVector(rrpKQR, num, kone.mR[j]);
				RotatedQuadraticCone.MultiplyByTW(thetaR[j], wR[j], z2, subVector9);
				RotatedQuadraticCone.MultiplyByTWinverse(thetaR[j], wR[j], s2, subVector8);
				SOCPUtilities.ArwArwe(subVector8, subVector9, subVector10);
				Vector.Daxpy(-1.0, subVector10, subVector7);
				SOCPUtilities.IncrementFirstElement(subVector7, _gamma * mu);
				num += kone.mR[j];
			}
			rcTK = rcTK + _gamma * mu - dtau * dkappa;
		}

		private void ComputeDiagonals()
		{
			Vector.ElementDivide(_zL, _xL, DL);
			Vector.ElementDivide(_zU, _xU, DU);
			Vector.ElementDivide(_zV, _sV, DV);
			Vector.Add(DU, DV, DUV);
			Vector.ElementDivide(_sP, _zP, DP);
			Vector.ElementDivide(_sN, _zN, DN);
			Vector.Add(DP, DN, fF);
			fF.ElementInvert(DPN);
			Vector.ElementDivide(_zG, _sG, DG);
			Vector.ElementDivide(_zH, _sH, DH);
			Vector.Add(DG, DH, fG);
			fG.ElementInvert(DGH);
			Vector.ElementDivide(sKL, zKL, DKL);
			int num = 0;
			for (int i = 0; i < kone.mQ.Count; i++)
			{
				SubVector subVector = new SubVector(DKQR, num, kone.mQ[i]);
				subVector.ConstantFill(thetaQ[i] * thetaQ[i]);
				subVector[0] = 0.0 - subVector[0];
				num += kone.mQ[i];
			}
			for (int j = 0; j < kone.mR.Count; j++)
			{
				SubVector subVector2 = new SubVector(DKQR, num, kone.mR[j]);
				subVector2.ConstantFill(thetaR[j] * thetaR[j]);
				subVector2[0] = 0.0 - subVector2[0];
				subVector2[1] = 0.0 - subVector2[1];
				num += kone.mR[j];
			}
			Vector.ElementMultiply(DV, uV, DVuV);
			Vector.ElementMultiply(DH, uH, fG);
			Vector.ElementMultiply(DGH, fG, DGHDHuH);
		}

		private void ComputeRightHandSide()
		{
			Vector.ElementMultiply(DGH, uH, tmpG);
			Vector.ElementDivide(rcP, _zP, rrPN);
			Vector.Daxpy(_eta, rpPN, rrPN);
			Vector.ElementDivide(rcN, _zN, fF);
			Vector.Daxpy(-1.0, fF, rrPN);
			Vector.ElementMultiply(DN, rdPN, fF);
			Vector.Daxpy(_eta, fF, rrPN);
			Vector.ElementDivide(rcH, _sH, rrGH);
			Vector.ElementMultiply(DH, rpH, fG);
			Vector.Daxpy(_eta, fG, rrGH);
			Vector.ElementDivide(rcG, _sG, fG);
			Vector.Daxpy(0.0 - _eta, rdG, fG);
			Vector.Daxpy(-1.0, fG, rrGH);
			Vector.ElementMultiply(DPN, rrPN, rrdF);
			rrdF.ScaleBy(-1.0);
			Vector.Daxpy(_eta, rdF, rrdF);
			Vector.ElementDivide(rcL, _xL, rrdL);
			rrdL.ScaleBy(-1.0);
			Vector.Daxpy(_eta, rdL, rrdL);
			Vector.ElementDivide(rcV, _sV, rrdU);
			Vector.ElementMultiply(DV, rpV, fU);
			Vector.Daxpy(_eta, fU, rrdU);
			Vector.ElementDivide(rcU, _xU, fU);
			Vector.Daxpy(-1.0, fU, rrdU);
			Vector.Daxpy(_eta, rdU, rrdU);
			Vector.ElementMultiply(DGH, rrGH, rrpG);
			rrpG.ScaleBy(-1.0);
			Vector.Daxpy(_eta, rpG, rrpG);
			Vector.ElementDivide(rcKL, zKL, rrpKL);
			Vector.Daxpy(_eta, rpKL, rrpKL);
			int num = 0;
			for (int i = 0; i < kone.mQ.Count; i++)
			{
				SubVector wTv = new SubVector(rrpKQR, num, kone.mQ[i]);
				SubVector r = new SubVector(rcKQR, num, kone.mQ[i]);
				SubVector subVector = new SubVector(fKQR, num, kone.mQ[i]);
				SOCPUtilities.MultiplyByInvArw(zQNT[i], r, subVector);
				QuadraticCone.MultiplybyWT(thetaQ[i], wQ[i], subVector, wTv);
				num += kone.mQ[i];
			}
			for (int j = 0; j < kone.mR.Count; j++)
			{
				SubVector wTv2 = new SubVector(rrpKQR, num, kone.mR[j]);
				SubVector r2 = new SubVector(rcKQR, num, kone.mR[j]);
				SubVector subVector2 = new SubVector(fKQR, num, kone.mR[j]);
				SOCPUtilities.MultiplyByInvArw(zRNT[j], r2, subVector2);
				RotatedQuadraticCone.MultiplyByWT(thetaR[j], wR[j], subVector2, wTv2);
				num += kone.mR[j];
			}
			Vector.Daxpy(_eta, rpKQR, rrpKQR);
		}

		private void ComputeDeltaTauDenominator()
		{
			SubVector subVector = new SubVector(cLcUcF, 0, nL);
			SubVector subVector2 = new SubVector(cLcUcF, nL, nU);
			SubVector subVector3 = new SubVector(cLcUcF, nL + nU, nF);
			SubVector subVector4 = new SubVector(bGbK, 0, mG);
			SubVector subVector5 = new SubVector(bGbK, mG, mK);
			BigSum bigSum = default(BigSum);
			bigSum.Add(_kappa / _tau);
			bigSum.Add(uV.InnerProduct(DVuV));
			Vector.ElementMultiply(uH, DG, tmpG);
			bigSum.Add(tmpG.InnerProduct(DGHDHuH));
			bigSum.Add(0.0 - subVector.InnerProduct(gL));
			bigSum.Add(0.0 - subVector2.InnerProduct(gU));
			bigSum.Add(0.0 - subVector3.InnerProduct(gF));
			bigSum.Add(subVector4.InnerProduct(gG));
			bigSum.Add(subVector5.InnerProduct(gK));
			bigSum.Add(0.0 - DVuV.InnerProduct(gU));
			bigSum.Add(DGHDHuH.InnerProduct(gG));
			dtauDenom = bigSum.ToDouble();
		}

		private void ComputeDeltaTauNumerator()
		{
			SubVector subVector = new SubVector(cLcUcF, 0, nL);
			SubVector subVector2 = new SubVector(cLcUcF, nL, nU);
			SubVector subVector3 = new SubVector(cLcUcF, nL + nU, nF);
			SubVector subVector4 = new SubVector(bGbK, 0, mG);
			SubVector subVector5 = new SubVector(bGbK, mG, mK);
			BigSum bigSum = default(BigSum);
			bigSum.Add(_eta * rg);
			bigSum.Add(rcTK / _tau);
			Vector.ElementDivide(rcV, _sV, tmpU);
			bigSum.Add(uV.InnerProduct(tmpU));
			Vector.ElementMultiply(DV, rpV, tmpU);
			bigSum.Add(_eta * uV.InnerProduct(tmpU));
			Vector.ElementDivide(rcG, _sG, tmpG);
			bigSum.Add(DGHDHuH.InnerProduct(tmpG));
			bigSum.Add((0.0 - _eta) * DGHDHuH.InnerProduct(rdG));
			Vector.ElementMultiply(DG, rpH, tmpG);
			bigSum.Add(_eta * DGHDHuH.InnerProduct(tmpG));
			Vector.ElementDivide(rcH, _sH, tmpH);
			Vector.ElementMultiply(DG, tmpH, tmpG);
			Vector.ElementMultiply(uH, DGH, tmpH);
			bigSum.Add(tmpH.InnerProduct(tmpG));
			bigSum.Add(subVector.InnerProduct(fL));
			bigSum.Add(subVector2.InnerProduct(fU));
			bigSum.Add(subVector3.InnerProduct(fF));
			bigSum.Add(0.0 - subVector4.InnerProduct(fG));
			bigSum.Add(0.0 - subVector5.InnerProduct(fK));
			bigSum.Add(DVuV.InnerProduct(fU));
			bigSum.Add(0.0 - DGHDHuH.InnerProduct(fG));
			dtauNumer = bigSum.ToDouble();
		}

		private void AssembleSearchDirection(ref Vector dsx_, ref Vector dyz_, ref double dtau_, ref double dkappa_)
		{
			SubVector vector = new SubVector(dsx_, 0, mK);
			SubVector subVector = new SubVector(vector, 0, kone.kL);
			SubVector subVector2 = new SubVector(vector, kone.kL, mQ + mR);
			SubVector subVector3 = new SubVector(dsx_, mK, nF);
			SubVector subVector4 = new SubVector(dsx_, mK + nF, nF);
			SubVector subVector5 = new SubVector(dsx_, mK + nF + nF, mG);
			SubVector subVector6 = new SubVector(dsx_, mK + nF + nF + mG, mG);
			SubVector subVector7 = new SubVector(dsx_, mK + nF + nF + mG + mG, nU);
			SubVector y = new SubVector(dsx_, mK + nF + nF + mG + mG + nU, nL);
			SubVector y2 = new SubVector(dsx_, mK + nF + nF + mG + mG + nU + nL, nU);
			SubVector x = new SubVector(dsx_, mK + nF + nF + mG + mG + nU + nL + nU, nF);
			SubVector subVector8 = new SubVector(dsx_, mK + nF + nF + mG + mG + nU, nL + nU + nF);
			SubVector x2 = new SubVector(dyz_, 0, mG);
			SubVector vector2 = new SubVector(dyz_, mG, mK);
			SubVector y3 = new SubVector(vector2, 0, kone.kL);
			SubVector vector3 = new SubVector(vector2, kone.kL, mQ + mR);
			SubVector subVector9 = new SubVector(dyz_, mG + mK, nF);
			SubVector subVector10 = new SubVector(dyz_, mG + mK + nF, nF);
			SubVector subVector11 = new SubVector(dyz_, mG + mK + nF + nF, mG);
			SubVector subVector12 = new SubVector(dyz_, mG + mK + nF + nF + mG, mG);
			SubVector subVector13 = new SubVector(dyz_, mG + mK + nF + nF + mG + mG, nU);
			SubVector subVector14 = new SubVector(dyz_, mG + mK + nF + nF + mG + mG + nU, nL);
			SubVector subVector15 = new SubVector(dyz_, mG + mK + nF + nF + mG + mG + nU + nL, nU);
			SubVector subVector16 = new SubVector(dyz_, 0, mG + mK);
			ComputeDeltaTauNumerator();
			dtau_ = dtauNumer / dtauDenom;
			subVector8.CopyFrom(fLfUfF);
			Vector.Daxpy(dtau_, gLgUgF, subVector8);
			subVector16.CopyFrom(fGfK);
			Vector.Daxpy(dtau_, gGgK, subVector16);
			fG.CopyFrom(rrGH);
			Vector.ElementMultiply(DH, uH, tmpG);
			Vector.Daxpy(0.0 - dtau_, tmpG, fG);
			Vector.Daxpy(1.0, x2, fG);
			Vector.ElementMultiply(DGH, fG, subVector5);
			subVector5.ScaleBy(-1.0);
			fF.CopyFrom(rrPN);
			Vector.Daxpy(-1.0, x, fF);
			Vector.ElementMultiply(DPN, fF, subVector9);
			subVector10.CopyFrom(subVector9);
			subVector10.ScaleBy(-1.0);
			Vector.Daxpy(_eta, rdPN, subVector10);
			subVector6.CopyFrom(subVector5);
			subVector6.ScaleBy(-1.0);
			Vector.Daxpy(dtau_, uH, subVector6);
			Vector.Daxpy(0.0 - _eta, rpH, subVector6);
			subVector7.CopyFrom(y2);
			subVector7.ScaleBy(-1.0);
			Vector.Daxpy(dtau_, uV, subVector7);
			Vector.Daxpy(0.0 - _eta, rpV, subVector7);
			Vector.ElementDivide(rcKL, zKL, subVector);
			Vector.ElementMultiply(DKL, y3, fKL);
			Vector.Daxpy(-1.0, fKL, subVector);
			int num = 0;
			for (int i = 0; i < kone.mQ.Count; i++)
			{
				SubVector wTv = new SubVector(subVector2, num, kone.mQ[i]);
				SubVector r = new SubVector(vector3, num, kone.mQ[i]);
				SubVector r2 = new SubVector(rcKQR, num, kone.mQ[i]);
				SubVector subVector17 = new SubVector(fKQR, num, kone.mQ[i]);
				SOCPUtilities.MultiplyByInvArw(zQNT[i], r2, subVector17);
				QuadraticCone.MultiplybyWT(thetaQ[i], wQ[i], subVector17, wTv);
				QuadraticCone.MultiplyByW2(thetaQ[i], wQ[i], r, subVector17);
				num += kone.mQ[i];
			}
			for (int j = 0; j < kone.mR.Count; j++)
			{
				SubVector wTv2 = new SubVector(subVector2, num, kone.mR[j]);
				SubVector r3 = new SubVector(vector3, num, kone.mR[j]);
				SubVector r4 = new SubVector(rcKQR, num, kone.mR[j]);
				SubVector subVector18 = new SubVector(fKQR, num, kone.mR[j]);
				SOCPUtilities.MultiplyByInvArw(zRNT[j], r4, subVector18);
				RotatedQuadraticCone.MultiplyByWT(thetaR[j], wR[j], subVector18, wTv2);
				RotatedQuadraticCone.MultiplyByW2(thetaR[j], wR[j], r3, subVector18);
				num += kone.mR[j];
			}
			Vector.Daxpy(-1.0, fKQR, subVector2);
			Vector.ElementDivide(rcP, _zP, subVector3);
			Vector.ElementMultiply(DP, subVector9, fF);
			Vector.Daxpy(-1.0, fF, subVector3);
			Vector.ElementDivide(rcN, _zN, subVector4);
			Vector.ElementMultiply(DN, subVector10, fF);
			Vector.Daxpy(-1.0, fF, subVector4);
			Vector.ElementDivide(rcG, _sG, subVector11);
			Vector.ElementMultiply(DG, subVector5, fG);
			Vector.Daxpy(-1.0, fG, subVector11);
			Vector.ElementDivide(rcH, _sH, subVector12);
			Vector.ElementMultiply(DH, subVector6, fG);
			Vector.Daxpy(-1.0, fG, subVector12);
			Vector.ElementDivide(rcU, _xU, subVector15);
			Vector.ElementMultiply(DU, y2, fU);
			Vector.Daxpy(-1.0, fU, subVector15);
			Vector.ElementDivide(rcV, _sV, subVector13);
			Vector.ElementMultiply(DV, subVector7, fU);
			Vector.Daxpy(-1.0, fU, subVector13);
			Vector.ElementDivide(rcL, _xL, subVector14);
			Vector.ElementMultiply(DL, y, fL);
			Vector.Daxpy(-1.0, fL, subVector14);
			dkappa_ = (rcTK - _kappa * dtau_) / _tau;
		}

		private double FindMaxStepSize(Vector dsxC_, Vector dyzC_, double dtau_, double dkappa_)
		{
			SubVector dx = new SubVector(dsxC_, 0, kone.kL);
			SubVector vector = new SubVector(dsxC_, kone.kL, mQ + mR);
			SubVector dx2 = new SubVector(dsxC_, mK, nF + nF + mG + mG + nU + nL + nU);
			SubVector dx3 = new SubVector(dyzC_, 0, kone.kL);
			SubVector vector2 = new SubVector(dyzC_, kone.kL, mQ + mR);
			SubVector dx4 = new SubVector(dyzC_, mK, nF + nF + mG + mG + nU + nL + nU);
			double val = IpmVectorUtilities.IPMRatioTest(sxPNGHVLU, dx2);
			if (sKL.Length > 0)
			{
				val = Math.Min(val, IpmVectorUtilities.IPMRatioTest(sKL, dx));
			}
			double num = IpmVectorUtilities.IPMRatioTest(zPNGHVLU, dx4);
			if (zKL.Length > 0)
			{
				num = Math.Min(num, IpmVectorUtilities.IPMRatioTest(zKL, dx3));
			}
			int num2 = 0;
			for (int i = 0; i < kone.mQ.Count; i++)
			{
				SubVector s = new SubVector(sKQR, num2, kone.mQ[i]);
				SubVector ds = new SubVector(vector, num2, kone.mQ[i]);
				val = Math.Min(val, QuadraticCone.MaxStepsize(s, ds));
				SubVector s2 = new SubVector(zKQR, num2, kone.mQ[i]);
				SubVector ds2 = new SubVector(vector2, num2, kone.mQ[i]);
				num = Math.Min(num, QuadraticCone.MaxStepsize(s2, ds2));
				num2 += kone.mQ[i];
			}
			for (int j = 0; j < kone.mR.Count; j++)
			{
				SubVector s3 = new SubVector(sKQR, num2, kone.mR[j]);
				SubVector ds3 = new SubVector(vector, num2, kone.mR[j]);
				val = Math.Min(val, RotatedQuadraticCone.MaxStepsize(s3, ds3));
				SubVector s4 = new SubVector(zKQR, num2, kone.mR[j]);
				SubVector ds4 = new SubVector(vector2, num2, kone.mR[j]);
				num = Math.Min(num, RotatedQuadraticCone.MaxStepsize(s4, ds4));
				num2 += kone.mR[j];
			}
			if (dtau_ < 0.0)
			{
				val = Math.Min(val, (0.0 - _tau) / dtau_);
			}
			if (dkappa_ < 0.0)
			{
				num = Math.Min(num, (0.0 - _kappa) / dkappa_);
			}
			return Math.Min(val, num);
		}

		private void UpdatePrmlDualVariables(double stepsize)
		{
			Vector.Daxpy(stepsize, dsx, _sx);
			Vector.Daxpy(stepsize, dyz, _yz);
			_tau += stepsize * dtau;
			_kappa += stepsize * dkappa;
		}

		private double ComputeAugSysResidue(ref Vector rprd, Vector dpdd)
		{
			SubVector y = new SubVector(rprd, 0, nL + nU + nF);
			SubVector subVector = new SubVector(rprd, nL + nU + nF, mG + mK);
			SubVector y2 = new SubVector(rprd, nL + nU + nF, mG);
			SubVector y3 = new SubVector(subVector, mG, kone.kL);
			SubVector vector = new SubVector(subVector, mG + kone.kL, mQ + mR);
			SubVector subVector2 = new SubVector(dpdd, 0, nL + nU + nF);
			SubVector subVector3 = new SubVector(dpdd, nL + nU + nF, mG + mK);
			SubVector y4 = new SubVector(dpdd, nL + nU + nF, mG);
			SubVector y5 = new SubVector(subVector3, mG, kone.kL);
			SubVector vector2 = new SubVector(subVector3, mG + kone.kL, mQ + mR);
			AGKLUF.SumLeftProduct(-1.0, subVector3, 1.0, y);
			Vector.ElementMultiply(Dprml, subVector2, tempLUF);
			Vector.Daxpy(1.0, tempLUF, y);
			AGKLUF.SumProductRight(-1.0, subVector2, 1.0, subVector);
			Vector.ElementMultiply(DGH, y4, tempG);
			Vector.Daxpy(-1.0, tempG, y2);
			Vector.ElementMultiply(DKL, y5, tempKL);
			Vector.Daxpy(-1.0, tempKL, y3);
			int num = 0;
			for (int i = 0; i < kone.mQ.Count; i++)
			{
				SubVector y6 = new SubVector(vector, num, kone.mQ[i]);
				SubVector r = new SubVector(vector2, num, kone.mQ[i]);
				SubVector subVector4 = new SubVector(tempKQR, num, kone.mQ[i]);
				QuadraticCone.MultiplyByW2(thetaQ[i], wQ[i], r, subVector4);
				Vector.Daxpy(-1.0, subVector4, y6);
				num += kone.mQ[i];
			}
			for (int j = 0; j < kone.mR.Count; j++)
			{
				SubVector y7 = new SubVector(vector, num, kone.mR[j]);
				SubVector r2 = new SubVector(vector2, num, kone.mR[j]);
				SubVector subVector5 = new SubVector(tempKQR, num, kone.mR[j]);
				RotatedQuadraticCone.MultiplyByW2(thetaR[j], wR[j], r2, subVector5);
				Vector.Daxpy(-1.0, subVector5, y7);
				num += kone.mR[j];
			}
			return rprd.Norm2();
		}

		private void PrepareLowRankUpdate()
		{
			Wlr.ZeroFill();
			double num = Math.Sqrt(2.0);
			int num2 = 0;
			int num3 = nL + nU + nF + mG + kone.kL;
			for (int i = 0; i < kone.mQ.Count; i++)
			{
				if (kone.mQ[i] >= _options.MinConeSizeLowRank)
				{
					Wlr.FillColumn(num2, num3, num * thetaQ[i], wQ[i]);
					num2++;
				}
				num3 += kone.mQ[i];
			}
			for (int j = 0; j < kone.mR.Count; j++)
			{
				if (kone.mR[j] >= _options.MinConeSizeLowRank)
				{
					Wlr.FillColumn(num2, num3, num * thetaR[j], wR[j]);
					num2++;
				}
				num3 += kone.mR[j];
			}
			_socpFactor.ForwardSolve(Wlr, Ulr);
			_socpFactor.DiagonalSolve(Ulr, Wlr);
			IplusUDU.FillIdentity();
			Matrix.SumLeftProduct(1.0, Ulr, Wlr, 1.0, IplusUDU);
			_denseFactor.Factor();
		}

		private int SolveAugmentedSystem(Vector rprd, ref Vector dpdd, int nMaxIR)
		{
			int num = 0;
			if (_lowRankUpdateCount > 0)
			{
				num += _socpFactor.ForwardSolve(rprd, dpdd);
				Matrix.SumLeftProduct(1.0, Wlr, dpdd, 0.0, UDvlr);
				_denseFactor.Solve(UDvlr);
				Matrix.SumProductRight(-1.0, Ulr, UDvlr, 1.0, dpdd);
				num += _socpFactor.DiagonalSolve(dpdd, rprd);
				return num + _socpFactor.BackwardSolve(rprd, dpdd);
			}
			return _socpFactor.Solve(rprd, dpdd, nMaxIR);
		}

		private int SolveAugmentedSystemIR(Vector rprd, ref Vector dpdd)
		{
			double num = Math.Max(1.0, rprd.Norm2());
			double num2 = double.PositiveInfinity;
			int num3 = 0;
			int num4 = 0;
			int num5 = -1;
			double num6 = 1.0;
			augResidueVec.CopyFrom(rprd);
			dpdd.ScaleBy(0.0);
			double num7;
			while (true)
			{
				num3 = ((nPivotPert <= 0 && !isPertPhase) ? SolveAugmentedSystem(augResidueVec, ref augSolutionVec, 0) : SolveAugmentedSystem(augResidueVec, ref augSolutionVec, -1));
				Vector.Daxpy(1.0, augSolutionVec, dpdd);
				num5 += num3 + 1;
				augResidueVec.CopyFrom(rprd);
				num7 = ComputeAugSysResidue(ref augResidueVec, dpdd) / num;
				if (num7 <= _options.EpsIR)
				{
					if (!isPertPhase && num5 <= _options.MaxFastIR)
					{
						prmlPertCond = Math.Max(prmlPertCond, prmlCond);
						dualPertCond = Math.Max(dualPertCond, dualCond);
					}
					break;
				}
				if (num7 < num2 && num5 < _options.MaxIR)
				{
					num2 = num7;
					continue;
				}
				if (!isPertPhase)
				{
					isPertPhase = true;
				}
				else
				{
					if ((prmlPertCond >= prmlCond && dualPertCond >= dualCond) || num6 >= 100.0)
					{
						break;
					}
					prmlPertCond *= 10.0;
					dualPertCond *= 10.0;
					num6 *= 10.0;
				}
				ComputeDiagPerturbation();
				_socpFactor.SetLinearSystemDiagonals(DprmlPerturbed, DdualPerturbedGKL, thetaQ, thetaR, wQ, wR, _options.MinConeSizeLowRank);
				nPivotPert = NumericalFactorization();
				_socpFactor.SetLinearSystemDiagonals(Dprml, DdualGKL, thetaQ, thetaR, wQ, wR, _options.MinConeSizeLowRank);
				num2 = double.PositiveInfinity;
				num4 += num5;
				num5 = -1;
				augResidueVec.CopyFrom(rprd);
				dpdd.ScaleBy(0.0);
			}
			num4 += num5;
			augResidueNorm = Math.Max(augResidueNorm, num7);
			return num4;
		}

		private int FindVariablePartitions()
		{
			prmlPositiveList.RemoveRange(nF, prmlPositiveList.Count - nF);
			int num = mK + nF + nF + mG + mG + nU;
			for (int i = 0; i < nL; i++)
			{
				if (_sxC[num + i] >= _yzC[num + i])
				{
					prmlPositiveList.Add(i);
				}
			}
			num = mK + nF + nF + mG + mG;
			int num2 = num + nU + nL;
			for (int j = 0; j < nU; j++)
			{
				if (_sxC[num + j] >= _yzC[num + j] && _sxC[num2 + j] >= _yzC[num2 + j])
				{
					prmlPositiveList.Add(nL + j);
				}
			}
			dualPositiveList.Clear();
			num = mK + nF + nF;
			num2 = num + mG;
			for (int k = 0; k < mG; k++)
			{
				if (_sxC[num + k] <= _yzC[num + 1] || _sxC[num2 + 1] <= _yzC[num2 + k])
				{
					dualPositiveList.Add(k);
				}
			}
			for (int l = 0; l < kone.kL; l++)
			{
				if (_sxC[l] <= _yzC[l])
				{
					dualPositiveList.Add(mG + l);
				}
			}
			return prmlPositiveList.Count + dualPositiveList.Count;
		}

		private double PrmlConditionNumber()
		{
			if (prmlPositiveList.Count == 0)
			{
				return 1.0;
			}
			dmaxPrmlPosList = 0.0;
			double num = double.PositiveInfinity;
			foreach (int prmlPositive in prmlPositiveList)
			{
				dmaxPrmlPosList = Math.Max(Dprml[prmlPositive], dmaxPrmlPosList);
				num = Math.Min(Dprml[prmlPositive], num);
			}
			return dmaxPrmlPosList / num;
		}

		private double DualConditionNumber()
		{
			if (dualPositiveList.Count == 0)
			{
				return 1.0;
			}
			dmaxDualPosList = 0.0;
			double num = double.PositiveInfinity;
			foreach (int dualPositive in dualPositiveList)
			{
				dmaxDualPosList = Math.Max(Ddual[dualPositive], dmaxDualPosList);
				num = Math.Min(Ddual[dualPositive], num);
			}
			return dmaxDualPosList / num;
		}

		private void ComputeDiagPerturbation()
		{
			DprmlPerturbed.CopyFrom(Dprml);
			foreach (int prmlPositive in prmlPositiveList)
			{
				DprmlPerturbed[prmlPositive] = Math.Max(Dprml[prmlPositive], dmaxPrmlPosList / prmlPertCond);
			}
			DdualPerturbed.CopyFrom(Ddual);
			foreach (int dualPositive in dualPositiveList)
			{
				DdualPerturbed[dualPositive] = Math.Max(Ddual[dualPositive], dmaxDualPosList / dualPertCond);
			}
		}
	}
}
