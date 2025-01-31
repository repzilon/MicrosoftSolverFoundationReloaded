using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>Homogeneous self-dual predictor-corrector LP solver using the general model:
	///
	///      minimize             cL'*xL +    cU'*xU + cF'*xF                |
	///      subject to  bG &lt;= AGL*xL +    AGU*xU + AGF*xF &lt;= bG + uH  |     mG
	///                  bI &lt;= AIL*xL +    AIU*xU + AIF*xF                |     mI
	///                       0 &lt;= xL, 0 &lt;= xU &lt;= uV                |      
	///      -------------------------------------------------------       
	///                            nL       nU       nF                   (sizes)
	/// </summary>
	/// <remarks>Please see the implementation notes for details.
	/// </remarks>
	internal class HsdGeneralModelSolver : GeneralModel
	{
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

		/// <summary>Factorization.
		/// </summary>
		private GeneralModelBlendedFactor _hsdFactor;

		/// <summary>Solver options.
		/// </summary>
		private HsdSolverOptions _options;

		/// <summary>Iteration count.
		/// </summary>
		private int _iterations;

		/// <summary> Duality gap.
		/// </summary>
		private double _gap;

		private Stopwatch _timer;

		private Vector _sx;

		private Vector _yz;

		private double _tau;

		private double _kappa;

		private SubVector sI;

		private SubVector sP;

		private SubVector sN;

		private SubVector sG;

		private SubVector sH;

		private SubVector sV;

		private SubVector xL;

		private SubVector xU;

		private SubVector xF;

		private SubVector yG;

		private SubVector zI;

		private SubVector zP;

		private SubVector zN;

		private SubVector zG;

		private SubVector zH;

		private SubVector zV;

		private SubVector zL;

		private SubVector zU;

		private SubVector yGzI;

		private SubVector xLxUxF;

		private SubVector sxC;

		private SubVector yzC;

		private Vector _augSolutionVec;

		private SubVector _DprmlPerturbed;

		private SubVector _DdualPerturbed;

		private Vector _augResidueVec;

		private double _augResidueNorm;

		private bool _isPertPhase;

		private List<int> _prmlPositiveList;

		private double _dmaxPrmlPosList;

		private double _prmlCond = 1.0;

		private double _prmlPertCond = 1.0;

		private List<int> _dualPositiveList;

		private double _dmaxDualPosList;

		private double _dualCond = 1.0;

		private double _dualPertCond = 1.0;

		private int _iterRefine;

		private int _pivotPert;

		private double _eta;

		private double _gamma;

		private int _nC;

		private Vector _dsx;

		private Vector _dyz;

		private double _dtau;

		private double _dkappa;

		private SubVector _dsxC;

		private SubVector _dyzC;

		private Vector _dsxMC;

		private Vector _dyzMC;

		private double _dtauMC;

		private double _dkappaMC;

		private double _dtauDenom;

		private double _dtauNumer;

		private SubVector _dsxCMC;

		private SubVector _dyzCMC;

		private Vector _dsxCdyzC;

		private SubVector _tmpG;

		private SubVector _tmpH;

		private SubVector _tmpU;

		private Vector _rp;

		private SubVector _rpG;

		private SubVector _rpI;

		private SubVector _rpV;

		private SubVector _rpH;

		private SubVector _rpPN;

		private SubVector _rpGrpI;

		private Vector _rd;

		private SubVector _rdL;

		private SubVector _rdU;

		private SubVector _rdF;

		private SubVector _rdG;

		private SubVector _rdPN;

		private SubVector _rdLrdUrdF;

		private double _rg;

		private BigSum _cx;

		private BigSum _byuz;

		private double _mu;

		private Vector _rc;

		private double _rcTK;

		private SubVector _rcI;

		private SubVector _rcP;

		private SubVector _rcN;

		private SubVector _rcG;

		private SubVector _rcH;

		private SubVector _rcV;

		private SubVector _rcL;

		private SubVector _rcU;

		private Vector _Dprml;

		private Vector _Ddual;

		private Vector _Drest;

		private SubVector _DL;

		private SubVector _DUV;

		private SubVector _DPN;

		private SubVector _DGH;

		private SubVector _DI;

		private SubVector _DU;

		private SubVector _DV;

		private SubVector _DG;

		private SubVector _DH;

		private SubVector _DP;

		private SubVector _DN;

		private Vector _rr;

		private SubVector _rrdL;

		private SubVector _rrdU;

		private SubVector _rrdF;

		private SubVector _rrpG;

		private SubVector _rrpI;

		private Vector _rrGH;

		private Vector _rrPN;

		private Vector _f;

		private Vector _g;

		private SubVector _fLfUfF;

		private SubVector _fGfI;

		private SubVector _gLgUgF;

		private SubVector _gGgI;

		private SubVector _fL;

		private SubVector _fU;

		private SubVector _fF;

		private SubVector _fG;

		private SubVector _fI;

		private SubVector _gL;

		private SubVector _gU;

		private SubVector _gF;

		private SubVector _gG;

		private SubVector _gI;

		private Vector _DVuV;

		private Vector _DGHDHuH;

		internal int _printMIteration = 5;

		protected override double SolveTolerance => _options.EpsAccuracy;

		/// <summary> Duality gap.
		/// </summary>
		public override double Gap => _gap;

		/// <summary>Iteration count.
		/// </summary>
		public override int IterationCount => _iterations;

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

		/// <summary>The algorithm kind.
		/// </summary>
		public override InteriorPointAlgorithmKind Algorithm => InteriorPointAlgorithmKind.HSD;

		/// <summary>The KKT formulation used by the algorithm.
		/// </summary>
		public override InteriorPointKktForm KktForm => InteriorPointKktForm.Augmented;

		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="solver">The InteriorPointSolver containing user data.</param>
		/// <param name="log">The LogSource.</param>
		/// <param name="prm">Solver parameters.</param>
		public HsdGeneralModelSolver(InteriorPointSolver solver, LogSource log, InteriorPointSolverParams prm)
			: base(solver, log, prm.PresolveLevel)
		{
			HsdGeneralModelSolver hsdGeneralModelSolver = this;
			CheckAbort = prm.ShouldAbort;
			IterationStartedCallback = prm.IterationStartedCallback;
			Action<InteriorPointSolveState> solving = delegate(InteriorPointSolveState s)
			{
				hsdGeneralModelSolver.SolveState = s;
				if (prm.Solving != null)
				{
					prm.Solving();
				}
			};
			Solving = solving;
			_options = new HsdSolverOptions();
			_timer = new Stopwatch();
			_primal = (_dual = (_gap = double.NaN));
		}

		/// <summary>Solve an LP.
		/// </summary>
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
			SetSolverOptions(prm);
			try
			{
				Solve();
			}
			catch (TimeLimitReachedException)
			{
				base.Solution.status = LinearResult.Interrupted;
			}
			return base.Solution.status;
		}

		/// <summary> Return the dual value for a row constraint.
		/// </summary>
		/// <param name="vidRow">Row vid.</param>
		/// <returns></returns>
		/// <returns>The dual value.</returns>
		public override Rational GetDualValue(int vidRow)
		{
			if (yGzI == null)
			{
				return Rational.Indeterminate;
			}
			return GetDualValue(yGzI, vidRow);
		}

		private void SetSolverOptions(InteriorPointSolverParams param)
		{
			_printMIteration = param.DebugIteration;
			_options.FactorizationParameters.FactorizationMethod = (SymbolicFactorizationMethod)param.SymbolicOrdering;
			_options.FactorizationParameters.DenseWindowThreshhold = param.MaxDenseColumnRatio;
			_options.FactorizationParameters.AllowNormal = param.IpmKKT == InteriorPointKktForm.Blended || param.IpmKKT == InteriorPointKktForm.Normal;
			_options.SetTolerance(param.SolveTolerance, setAll: true);
			_options.MaxIterations = ((param.MaxIterationCount < 0) ? int.MaxValue : param.MaxIterationCount);
		}

		private bool IterationStarted()
		{
			_primal = _cx.ToDouble() / _tau + cxShift;
			_dual = _byuz.ToDouble() / _tau + cxShift;
			_gap = GetRelativeGap();
			if (IterationStartedCallback != null && IterationStartedCallback())
			{
				return !CheckAbort();
			}
			return true;
		}

		/// <summary>Core solver.
		/// </summary>
		private void Solve()
		{
			base.Logger.LogEvent(15, Resources.LogComputationalProgressOfHSDSolver);
			_timer.Reset();
			_timer.Start();
			Solving(InteriorPointSolveState.Init);
			if (CheckAbort())
			{
				base.Solution.status = LinearResult.Interrupted;
				return;
			}
			AllocateMemory();
			if (AGKLUF == null)
			{
				SolveZeroConstraints(xLxUxF);
				_gap = 0.0;
				return;
			}
			InitializeLinearSolver();
			Solving(InteriorPointSolveState.SymbolicFactorization);
			if (CheckAbort())
			{
				base.Solution.status = LinearResult.Interrupted;
				return;
			}
			SimpleStartingPoint();
			if (_options.AdvancedStartingPoint)
			{
				AdvancedStartingPoint();
			}
			ComputeResidues();
			double num = bGbK.Norm2();
			double num2 = uH.Norm2();
			double num3 = uV.Norm2();
			double bGbIuHuV_Norm = Math.Sqrt(num * num + num2 * num2 + num3 * num3);
			double num4 = cLcUcF.Norm2();
			_mu = (sxC.BigInnerProduct(yzC).ToDouble() + _tau * _kappa) / (double)(_nC + 1);
			double mu = _mu;
			double num5 = ((num4 < _options.EpsMuRatio) ? 1.0 : Math.Max(num / num4, 1.0));
			_iterations = 0;
			int i = 0;
			double alpha = 0.0;
			double alphaLast = 1.0;
			_gamma = 0.0;
			double num6 = Math.Min(_options.EpsTauKappaRatio, 1E-08);
			while (true)
			{
				double relativePrimalInfeasibility = GetRelativePrimalInfeasibility(bGbIuHuV_Norm);
				double relativeDualInfeasibility = GetRelativeDualInfeasibility(num4);
				double relativeGap = GetRelativeGap();
				DisplayProgress(_iterations % 50 == 0, relativePrimalInfeasibility, relativeDualInfeasibility, relativeGap, i, alpha);
				if (CheckAbort())
				{
					base.Solution.status = LinearResult.Interrupted;
					return;
				}
				if (CheckOptimality(alpha, alphaLast, relativePrimalInfeasibility, relativeDualInfeasibility, relativeGap))
				{
					base.Solution.status = LinearResult.Optimal;
					break;
				}
				double num7 = num5 * _mu / mu;
				if (num7 < _options.EpsMuRatio && _tau < num6 * Math.Min(1.0, _kappa))
				{
					if (_cx.ToDouble() < -1E-12)
					{
						if (_byuz.ToDouble() > 1E-10)
						{
							base.Solution.status = LinearResult.InfeasiblePrimal;
						}
						else
						{
							base.Solution.status = LinearResult.UnboundedPrimal;
						}
					}
					else if (_byuz.ToDouble() > 1E-10)
					{
						base.Solution.status = LinearResult.InfeasiblePrimal;
					}
					else
					{
						base.Solution.status = LinearResult.InfeasiblePrimal;
					}
					break;
				}
				if (10 < _iterations)
				{
					num6 = Math.Min(num6 * 2.0, 0.001);
					_options.EpsMuRatio = Math.Min(_options.EpsMuRatio * 2.0, 1E-06);
				}
				if (_iterations == _options.MaxIterations - 10)
				{
					_options.SetTolerance(_options.EpsPrimalInfeasible * 100.0, setAll: false);
				}
				if (_iterations >= _options.MaxIterations || !IterationStarted())
				{
					base.Solution.status = LinearResult.Interrupted;
					break;
				}
				ComputeDiagonals();
				_prmlCond = PrmlConditionNumber();
				_dualCond = DualConditionNumber();
				if (_isPertPhase)
				{
					ComputeDiagPerturbation();
					SetLinearSystem(_DprmlPerturbed, _DdualPerturbed);
					_pivotPert = NumericalFactorization();
					SetLinearSystemDiagonals(_Dprml, _Ddual);
				}
				else
				{
					SetLinearSystem(_Dprml, _Ddual);
					_pivotPert = NumericalFactorization();
				}
				_iterRefine = 0;
				_augResidueNorm = 0.0;
				_fLfUfF.CopyFrom(cLcUcF);
				Vector.Daxpy(-1.0, _DVuV, _fU);
				_fGfI.CopyFrom(bGbK);
				Vector.Daxpy(1.0, _DGHDHuH, _fG);
				int val = SolveAugmentedSystemIR(_f, ref _g);
				_iterRefine = Math.Max(_iterRefine, val);
				ComputeDeltaTauDenominator();
				_eta = 1.0;
				ComputeRightHandSide();
				val = SolveAugmentedSystemIR(_rr, ref _f);
				_iterRefine = Math.Max(_iterRefine, val);
				AssembleSearchDirection(ref _dsx, ref _dyz, ref _dtau, ref _dkappa);
				FindVariablePartitions();
				alpha = FindMaxStepSize(_dsxC, _dyzC, _dtau, _dkappa);
				_gamma = 1.0 - alpha;
				_gamma = _gamma * _gamma * Math.Min(_gamma, _options.BetaMaxCentering);
				_eta = 1.0 - _gamma;
				_rc.AddConstant(_gamma * _mu);
				_rcTK += _gamma * _mu;
				Vector.ElementMultiply(_dsxC, _dyzC, _dsxCdyzC);
				Vector.Daxpy(-1.0, _dsxCdyzC, _rc);
				_rcTK -= _dtau * _dkappa;
				ComputeRightHandSide();
				val = SolveAugmentedSystemIR(_rr, ref _f);
				_iterRefine = Math.Max(_iterRefine, val);
				AssembleSearchDirection(ref _dsx, ref _dyz, ref _dtau, ref _dkappa);
				alpha = FindMaxStepSize(_dsxC, _dyzC, _dtau, _dkappa);
				if (_options.MultipleCorrections)
				{
					for (i = 0; i < _options.MaxCorrections; i++)
					{
						if (!(alpha < _options.MCStepThreshold))
						{
							break;
						}
						double stepsize = Math.Min(1.0, 2.0 * alpha);
						ComputeCorrection(stepsize);
						stepsize = FindMaxStepSize(_dsxCMC, _dyzCMC, _dtauMC, _dkappaMC);
						if (!(stepsize > 1.1 * alpha))
						{
							break;
						}
						_dsx.CopyFrom(_dsxMC);
						_dyz.CopyFrom(_dyzMC);
						_dtau = _dtauMC;
						_dkappa = _dkappaMC;
						alpha = stepsize;
					}
				}
				alpha *= 0.99995;
				UpdatePrmlDualVariables(alpha);
				Vector.ElementMultiply(sxC, yzC, _dsxCdyzC);
				_mu = (_dsxCdyzC.Sum() + _tau * _kappa) / (double)(_nC + 1);
				BackoffFromBoundary(ref alpha);
				ComputeResidues();
				_iterations++;
				alphaLast = alpha;
			}
			_primal = cLcUcF.BigInnerProduct(xLxUxF).ToDouble() / _tau;
			_primal += cxShift;
			_dual = (bGbK.BigInnerProduct(yGzI) - uV.BigInnerProduct(zV) - uH.BigInnerProduct(zH)).ToDouble() / _tau;
			_dual += cxShift;
			base.Solution.x = xLxUxF.ToArray();
			base.Solution.y = yG.ToArray();
			base.Solution.z = zI.ToArray();
			base.Solution.cx = _primal;
			base.Solution.by = _dual;
			base.Solution.relGap = (_gap = GetRelativeGap());
			base.Solution.relPrmlFeas = GetRelativePrimalInfeasibility(bGbIuHuV_Norm);
			base.Solution.relDualFeas = GetRelativeDualInfeasibility(num4);
			_sx.Over(_tau);
			_yz.Over(_tau);
			_kappa /= _tau;
			_tau = 1.0;
			CleanUpLinearSolver();
			CleanUpMemory();
			base.Logger.LogEvent(15, Resources.IpmCoreSolutionTime0, _timer.Elapsed.TotalSeconds);
		}

		private bool CheckOptimality(double alpha, double alphaLast, double rel_rp, double rel_rd, double rel_ac)
		{
			if ((!(rel_rp < _options.EpsPrimalInfeasible) || !(rel_rd < _options.EpsDualInfeasible) || !(rel_ac < _options.EpsAccuracy)) && (!(_kappa / _tau < _options.EpsTauKappaRatio) || !(rel_ac < _options.EpsAccuracy) || !(rel_rp < 1000.0 * _options.EpsPrimalInfeasible) || !(rel_rd < 1000.0 * _options.EpsDualInfeasible)) && (!(_kappa / _tau < _options.EpsTauKappaRatio) || !(rel_ac < _options.EpsAccuracy * 100.0) || !(rel_rp < _options.EpsPrimalInfeasible) || !(rel_rd < _options.EpsDualInfeasible)))
			{
				if (Math.Max(alpha, alphaLast) < _options.EpsInsufficientStep)
				{
					if (rel_rp < _options.EpsPrimalInfeasible * 100.0 && rel_rd < _options.EpsDualInfeasible * 100.0)
					{
						return rel_ac < _options.EpsAccuracy * 100.0;
					}
					return false;
				}
				return false;
			}
			return true;
		}

		private double GetRelativeDualInfeasibility(double cLcUcF_Norm)
		{
			return _rd.Norm2() / (_tau * (1.0 + cLcUcF_Norm));
		}

		private double GetRelativePrimalInfeasibility(double bGbIuHuV_Norm)
		{
			return _rp.Norm2() / (_tau * (1.0 + bGbIuHuV_Norm));
		}

		private double GetRelativeGap()
		{
			double value = (_cx - _byuz).ToDouble();
			return Math.Abs(value) / (_tau + Math.Abs(_byuz.ToDouble()));
		}

		private void AllocateMemory()
		{
			_nC = mK + nF + nF + mG + mG + nU + nL + nU;
			_sx = new Vector(mK + nF + nF + mG + mG + nU + nL + nU + nF);
			sI = new SubVector(_sx, 0, mK);
			sP = new SubVector(_sx, mK, nF);
			sN = new SubVector(_sx, mK + nF, nF);
			sG = new SubVector(_sx, mK + nF + nF, mG);
			sH = new SubVector(_sx, mK + nF + nF + mG, mG);
			sV = new SubVector(_sx, mK + nF + nF + mG + mG, nU);
			xL = new SubVector(_sx, mK + nF + nF + mG + mG + nU, nL);
			xU = new SubVector(_sx, mK + nF + nF + mG + mG + nU + nL, nU);
			xF = new SubVector(_sx, mK + nF + nF + mG + mG + nU + nL + nU, nF);
			xLxUxF = new SubVector(_sx, mK + nF + nF + mG + mG + nU, nL + nU + nF);
			sxC = new SubVector(_sx, 0, _nC);
			_yz = new Vector(mG + mK + nF + nF + mG + mG + nU + nL + nU);
			yG = new SubVector(_yz, 0, mG);
			zI = new SubVector(_yz, mG, mK);
			zP = new SubVector(_yz, mG + mK, nF);
			zN = new SubVector(_yz, mG + mK + nF, nF);
			zG = new SubVector(_yz, mG + mK + nF + nF, mG);
			zH = new SubVector(_yz, mG + mK + nF + nF + mG, mG);
			zV = new SubVector(_yz, mG + mK + nF + nF + mG + mG, nU);
			zL = new SubVector(_yz, mG + mK + nF + nF + mG + mG + nU, nL);
			zU = new SubVector(_yz, mG + mK + nF + nF + mG + mG + nU + nL, nU);
			yGzI = new SubVector(_yz, 0, mG + mK);
			yzC = new SubVector(_yz, mG, _nC);
			_dsx = new Vector(mK + nF + nF + mG + mG + nU + nL + nU + nF);
			_dsxC = new SubVector(_dsx, 0, _nC);
			_dyz = new Vector(mG + mK + nF + nF + mG + mG + nU + nL + nU);
			_dyzC = new SubVector(_dyz, mG, _nC);
			_dsxMC = new Vector(mK + nF + nF + mG + mG + nU + nL + nU + nF);
			_dsxCMC = new SubVector(_dsxMC, 0, _nC);
			_dyzMC = new Vector(mG + mK + nF + nF + mG + mG + nU + nL + nU);
			_dyzCMC = new SubVector(_dyzMC, mG, _nC);
			_dsxCdyzC = new Vector(_nC);
			_tmpG = new SubVector(_dsxCdyzC, mK + nF + nF, mG);
			_tmpH = new SubVector(_dsxCdyzC, mK + nF + nF + mG, mG);
			_tmpU = new SubVector(_dsxCdyzC, mK + nF + nF + mG + mG, nU);
			_rp = new Vector(mG + mK + nU + mG + nF);
			_rpG = new SubVector(_rp, 0, mG);
			_rpI = new SubVector(_rp, mG, mK);
			_rpV = new SubVector(_rp, mG + mK, nU);
			_rpH = new SubVector(_rp, mG + mK + nU, mG);
			_rpPN = new SubVector(_rp, mG + mK + nU + mG, nF);
			_rpGrpI = new SubVector(_rp, 0, mG + mK);
			_rd = new Vector(nL + nU + nF + mG + nF);
			_rdL = new SubVector(_rd, 0, nL);
			_rdU = new SubVector(_rd, nL, nU);
			_rdF = new SubVector(_rd, nL + nU, nF);
			_rdG = new SubVector(_rd, nL + nU + nF, mG);
			_rdPN = new SubVector(_rd, nL + nU + nF + mG, nF);
			_rdLrdUrdF = new SubVector(_rd, 0, nL + nU + nF);
			_rc = new Vector(mK + nF + nF + mG + mG + nU + nL + nU);
			_rcI = new SubVector(_rc, 0, mK);
			_rcP = new SubVector(_rc, mK, nF);
			_rcN = new SubVector(_rc, mK + nF, nF);
			_rcG = new SubVector(_rc, mK + nF + nF, mG);
			_rcH = new SubVector(_rc, mK + nF + nF + mG, mG);
			_rcV = new SubVector(_rc, mK + nF + nF + mG + mG, nU);
			_rcL = new SubVector(_rc, mK + nF + nF + mG + mG + nU, nL);
			_rcU = new SubVector(_rc, mK + nF + nF + mG + mG + nU + nL, nU);
			_Dprml = new Vector(nL + nU + nF);
			_Ddual = new Vector(mG + mK);
			_Drest = new Vector(nU + nU + mG + mG + nF + nF);
			_DL = new SubVector(_Dprml, 0, nL);
			_DUV = new SubVector(_Dprml, nL, nU);
			_DPN = new SubVector(_Dprml, nL + nU, nF);
			_DGH = new SubVector(_Ddual, 0, mG);
			_DI = new SubVector(_Ddual, mG, mK);
			_DU = new SubVector(_Drest, 0, nU);
			_DV = new SubVector(_Drest, nU, nU);
			_DG = new SubVector(_Drest, nU + nU, mG);
			_DH = new SubVector(_Drest, nU + nU + mG, mG);
			_DP = new SubVector(_Drest, nU + nU + mG + mG, nF);
			_DN = new SubVector(_Drest, nU + nU + mG + mG + nF, nF);
			_augResidueVec = new Vector(nL + nU + nF + mG + mK);
			_augSolutionVec = new Vector(nL + nU + nF + mG + mK);
			_DprmlPerturbed = new SubVector(_augSolutionVec, 0, nL + nU + nF);
			_DdualPerturbed = new SubVector(_augSolutionVec, nL + nU + nF, mG + mK);
			_prmlPositiveList = new List<int>(nL + nU + nF);
			for (int i = 0; i < nF; i++)
			{
				_prmlPositiveList.Add(nL + nU + i);
			}
			_dualPositiveList = new List<int>(mG + mK);
			_rr = new Vector(nL + nU + nF + mG + mK);
			_rrdL = new SubVector(_rr, 0, nL);
			_rrdU = new SubVector(_rr, nL, nU);
			_rrdF = new SubVector(_rr, nL + nU, nF);
			_rrpG = new SubVector(_rr, nL + nU + nF, mG);
			_rrpI = new SubVector(_rr, nL + nU + nF + mG, mK);
			_rrGH = new Vector(mG);
			_rrPN = new Vector(nF);
			_f = new Vector(nL + nU + nF + mG + mK);
			_g = new Vector(nL + nU + nF + mG + mK);
			_fLfUfF = new SubVector(_f, 0, nL + nU + nF);
			_fGfI = new SubVector(_f, nL + nU + nF, mG + mK);
			_gLgUgF = new SubVector(_g, 0, nL + nU + nF);
			_gGgI = new SubVector(_g, nL + nU + nF, mG + mK);
			_fL = new SubVector(_f, 0, nL);
			_fU = new SubVector(_f, nL, nU);
			_fF = new SubVector(_f, nL + nU, nF);
			_fG = new SubVector(_f, nL + nU + nF, mG);
			_fI = new SubVector(_f, nL + nU + nF + mG, mK);
			_gL = new SubVector(_g, 0, nL);
			_gU = new SubVector(_g, nL, nU);
			_gF = new SubVector(_g, nL + nU, nF);
			_gG = new SubVector(_g, nL + nU + nF, mG);
			_gI = new SubVector(_g, nL + nU + nF + mG, mK);
			_DVuV = new Vector(nU);
			_DGHDHuH = new Vector(mG);
		}

		private void DisplayProgress(bool showHeader, double rel_rp, double rel_rd, double rel_ac, int nCorrections, double alpha)
		{
			Solving(InteriorPointSolveState.IterationStarted);
			if (showHeader)
			{
				base.Logger.LogEvent(15, Resources.KRpRdGapKtMuGammaCAlphaCxTime);
			}
			base.Logger.LogEvent(15, "{0,2} {1:0.0E+00} {2:0.0E+00} {3:0.0E+00} {4:0.0E+00} {5:0.0E+00} {6:F3} {7:d1} {8:F3} {9,14:0.0000000E+00} {10:F2}", _iterations, rel_rp, rel_rd, rel_ac, _kappa / _tau, _mu, _gamma, nCorrections, alpha, _cx.ToDouble() / _tau + cxShift, _timer.Elapsed.TotalSeconds);
		}

		protected virtual void InitializeLinearSolver()
		{
			TimeSpan elapsed = _timer.Elapsed;
			_hsdFactor = new GeneralModelBlendedFactor(AGKLUF, CheckAbort, _options.FactorizationParameters);
			base.Logger.LogEvent(15, Resources.SymbolicFactorizationTime0SecType1, (_timer.Elapsed - elapsed).TotalSeconds, _hsdFactor.Parameters.FactorizationMethod);
			base.Logger.LogEvent(15, Resources.SymbolicFactorNonzeros0, AGKLUF.ColumnCount, AGKLUF.Count, 1.0 * (double)AGKLUF.Count / (1.0 * (double)AGKLUF.RowCount * (double)AGKLUF.ColumnCount), "A");
			base.Logger.LogEvent(15, Resources.SymbolicFactorNonzeros0, _hsdFactor.ColumnCount, _hsdFactor.Count, 1.0 * (double)_hsdFactor.Count / (1.0 * (double)_hsdFactor.ColumnCount * (double)_hsdFactor.ColumnCount), "L");
		}

		/// <summary> Numerical factorization (define necessary state in derived class)
		/// <remarks>
		/// Derived classes should implement specific methods to solve the augmented system:
		///    | Dp  A' | |dx| - |rx|
		///    | A   Dd | |dy| - |ry| 
		/// </remarks>
		/// </summary>
		protected virtual int NumericalFactorization()
		{
			_hsdFactor.ZeroPivots = 0;
			_hsdFactor.Cholesky();
			return _hsdFactor.ZeroPivots;
		}

		/// <summary>Set the values of the augmented system.
		/// </summary>
		protected virtual void SetLinearSystem(Vector dPrml, Vector dDual)
		{
			if (_printMIteration == _iterations)
			{
				_hsdFactor._printM = true;
				_printMIteration = -1;
			}
			else
			{
				_hsdFactor._printM = false;
			}
			_hsdFactor.SetBlendedValues(-1.0, dPrml, 1.0, dDual, setA: true);
		}

		/// <summary>Set the diagonal values of the augmented system.
		/// </summary>
		protected virtual void SetLinearSystemDiagonals(Vector dPrml, Vector dDual)
		{
			_hsdFactor.SetBlendedValues(-1.0, dPrml, 1.0, dDual, setA: false);
		}

		/// <summary>Solve the augmented system.
		/// </summary>
		protected virtual int SolveSystem(Vector rprd, ref Vector dpdd, int nMaxIterRef)
		{
			dpdd = _hsdFactor.SolveSystem(-1.0, _Dprml, 1.0, _Ddual, rprd);
			return 1;
		}

		/// <summary>Compute the residual of the solution to the augmented system.
		/// </summary>
		protected virtual double ComputeResidue(ref Vector rprd, Vector dpdd)
		{
			return _hsdFactor.ComputeResidue(-1.0, _Dprml, 1.0, _Ddual, dpdd, rprd);
		}

		/// <summary>Cleanup the linear solver.
		/// </summary>
		protected virtual void CleanUpLinearSolver()
		{
			_hsdFactor = null;
		}

		/// <summary>Cleanup workspace.
		/// </summary>
		protected virtual void CleanUpMemory()
		{
			_dsx = null;
			_dyz = null;
			_dsxMC = null;
			_dyzMC = null;
			_dsxCdyzC = null;
			_rp = null;
			_rd = null;
			_rc = null;
			_Dprml = null;
			_Ddual = null;
			_Drest = null;
			_augResidueVec = null;
			_augSolutionVec = null;
			_prmlPositiveList.Clear();
			_dualPositiveList.Clear();
			_rr = null;
			_rrGH = null;
			_rrPN = null;
			_f = null;
			_g = null;
			_DVuV = null;
			_DGHDHuH = null;
		}

		private void SimpleStartingPoint()
		{
			sxC.ConstantFill(1.0);
			yzC.ConstantFill(1.0);
			xF.ConstantFill(0.0);
			yG.ConstantFill(0.0);
			_tau = 1.0;
			_kappa = 1.0;
		}

		private void AdvancedStartingPoint()
		{
			ComputeResidues();
			ComputeDiagonals();
			SetLinearSystemDiagonals(_Dprml, _Ddual);
			NumericalFactorization();
			_fLfUfF.CopyFrom(cLcUcF);
			Vector.Daxpy(-1.0, _DVuV, _fU);
			_fGfI.CopyFrom(bGbK);
			Vector.Daxpy(1.0, _DGHDHuH, _fG);
			_iterRefine = Math.Max(_iterRefine, SolveAugmentedSystemIR(_f, ref _g));
			ComputeDeltaTauDenominator();
			_eta = 1.0;
			_rc.AddConstant(_mu);
			_rcTK += _mu;
			ComputeRightHandSide();
			_iterRefine = Math.Max(_iterRefine, SolveAugmentedSystemIR(_rr, ref _f));
			AssembleSearchDirection(ref _dsx, ref _dyz, ref _dtau, ref _dkappa);
			double num = FindMaxStepSize(_dsxC, _dyzC, _dtau, _dkappa);
			_gamma = 10.0;
			_eta = 1.0;
			_rc.AddConstant((1.0 - num) * _gamma * _mu - _mu);
			_rcTK += (1.0 - num) * _gamma * _mu - _mu;
			Vector.ElementMultiply(_dsxC, _dyzC, _dsxCdyzC);
			Vector.Daxpy((0.0 - num) * num, _dsxCdyzC, _rc);
			_rcTK -= num * num * _dtau * _dkappa;
			ComputeRightHandSide();
			_iterRefine = Math.Max(_iterRefine, SolveAugmentedSystemIR(_rr, ref _f));
			AssembleSearchDirection(ref _dsx, ref _dyz, ref _dtau, ref _dkappa);
			UpdatePrmlDualVariables(1.0, ref _sx, ref _yz, ref _tau, ref _kappa);
			IpmVectorUtilities.TruncateBottom(sxC, 1.0);
			IpmVectorUtilities.TruncateBottom(yzC, 1.0);
			_tau = ((_tau > 1.0) ? _tau : 1.0);
			_kappa = ((_kappa > 1.0) ? _kappa : 1.0);
		}

		private void ComputeResidues()
		{
			_rpG.CopyFrom(sG);
			_rpI.CopyFrom(sI);
			Vector.Daxpy(_tau, bGbK, _rpGrpI);
			AGKLUF.SumProductRight(-1.0, xLxUxF, 1.0, _rpGrpI);
			_rpV.CopyFrom(sV);
			Vector.Daxpy(0.0 - _tau, uV, _rpV);
			Vector.Daxpy(1.0, xU, _rpV);
			_rpH.CopyFrom(sH);
			Vector.Daxpy(0.0 - _tau, uH, _rpH);
			Vector.Daxpy(1.0, sG, _rpH);
			_rpPN.CopyFrom(sP);
			Vector.Daxpy(-1.0, sN, _rpPN);
			Vector.Daxpy(-1.0, xF, _rpPN);
			_rdL.CopyFrom(zL);
			_rdU.CopyFrom(zU);
			_rdF.CopyFrom(zP);
			AGKLUF.SumLeftProduct(-1.0, yGzI, -1.0, _rdLrdUrdF);
			Vector.Daxpy(1.0, zV, _rdU);
			Vector.Daxpy(_tau, cLcUcF, _rdLrdUrdF);
			_rdG.CopyFrom(yG);
			Vector.Daxpy(1.0, zH, _rdG);
			Vector.Daxpy(-1.0, zG, _rdG);
			_rdPN.CopyFrom(zN);
			_rdPN.ScaleBy(-1.0);
			Vector.Daxpy(-1.0, zP, _rdPN);
			_cx = cLcUcF.BigInnerProduct(xLxUxF);
			_byuz = bGbK.BigInnerProduct(yGzI) - uV.BigInnerProduct(zV) - uH.BigInnerProduct(zH);
			_rg = (_cx - _byuz).ToDouble() + _kappa;
			Vector.ElementMultiply(sxC, yzC, _rc);
			_rc.ScaleBy(-1.0);
			_rcTK = (0.0 - _tau) * _kappa;
		}

		private void ComputeDiagonals()
		{
			Vector.ElementDivide(zL, xL, _DL);
			Vector.ElementDivide(zU, xU, _DU);
			Vector.ElementDivide(zV, sV, _DV);
			Vector.Add(_DU, _DV, _DUV);
			Vector.ElementDivide(sP, zP, _DP);
			Vector.ElementDivide(sN, zN, _DN);
			Vector.Add(_DP, _DN, _fF);
			_fF.ElementInvert(_DPN);
			Vector.ElementDivide(zG, sG, _DG);
			Vector.ElementDivide(zH, sH, _DH);
			Vector.Add(_DG, _DH, _fG);
			_fG.ElementInvert(_DGH);
			Vector.ElementDivide(sI, zI, _DI);
			Vector.ElementMultiply(_DV, uV, _DVuV);
			Vector.ElementMultiply(_DH, uH, _fG);
			Vector.ElementMultiply(_DGH, _fG, _DGHDHuH);
			Vector.ElementMultiply(_DG, uH, _fG);
		}

		private void ComputeRightHandSide()
		{
			Vector.ElementMultiply(_DGH, uH, _tmpG);
			Vector.ElementDivide(_rcP, zP, _rrPN);
			Vector.Daxpy(_eta, _rpPN, _rrPN);
			Vector.ElementDivide(_rcN, zN, _fF);
			Vector.Daxpy(-1.0, _fF, _rrPN);
			Vector.ElementMultiply(_DN, _rdPN, _fF);
			Vector.Daxpy(_eta, _fF, _rrPN);
			Vector.ElementDivide(_rcH, sH, _rrGH);
			Vector.ElementMultiply(_DH, _rpH, _fG);
			Vector.Daxpy(_eta, _fG, _rrGH);
			Vector.ElementDivide(_rcG, sG, _fG);
			Vector.Daxpy(0.0 - _eta, _rdG, _fG);
			Vector.Daxpy(-1.0, _fG, _rrGH);
			Vector.ElementMultiply(_DPN, _rrPN, _rrdF);
			_rrdF.ScaleBy(-1.0);
			Vector.Daxpy(_eta, _rdF, _rrdF);
			Vector.ElementDivide(_rcL, xL, _rrdL);
			_rrdL.ScaleBy(-1.0);
			Vector.Daxpy(_eta, _rdL, _rrdL);
			Vector.ElementDivide(_rcV, sV, _rrdU);
			Vector.ElementMultiply(_DV, _rpV, _fU);
			Vector.Daxpy(_eta, _fU, _rrdU);
			Vector.ElementDivide(_rcU, xU, _fU);
			Vector.Daxpy(-1.0, _fU, _rrdU);
			Vector.Daxpy(_eta, _rdU, _rrdU);
			Vector.ElementMultiply(_DGH, _rrGH, _rrpG);
			_rrpG.ScaleBy(-1.0);
			Vector.Daxpy(_eta, _rpG, _rrpG);
			Vector.ElementDivide(_rcI, zI, _rrpI);
			Vector.Daxpy(_eta, _rpI, _rrpI);
		}

		private double PrmlConditionNumber()
		{
			if (_prmlPositiveList.Count == 0)
			{
				return 1.0;
			}
			_dmaxPrmlPosList = 0.0;
			double num = double.PositiveInfinity;
			foreach (int prmlPositive in _prmlPositiveList)
			{
				_dmaxPrmlPosList = Math.Max(_Dprml[prmlPositive], _dmaxPrmlPosList);
				num = Math.Min(_Dprml[prmlPositive], num);
			}
			return _dmaxPrmlPosList / num;
		}

		private double DualConditionNumber()
		{
			if (_dualPositiveList.Count == 0)
			{
				return 1.0;
			}
			_dmaxDualPosList = 0.0;
			double num = double.PositiveInfinity;
			foreach (int dualPositive in _dualPositiveList)
			{
				_dmaxDualPosList = Math.Max(_Ddual[dualPositive], _dmaxDualPosList);
				num = Math.Min(_Ddual[dualPositive], num);
			}
			return _dmaxDualPosList / num;
		}

		private int FindVariablePartitions()
		{
			_prmlPositiveList.RemoveRange(nF, _prmlPositiveList.Count - nF);
			SubVector z = new SubVector(_dsxCMC, 0, _nC);
			Vector.ElementDivide(_dsxC, sxC, z);
			SubVector z2 = new SubVector(_dyzCMC, 0, _nC);
			Vector.ElementDivide(_dyzC, yzC, z2);
			int num = mK + nF + nF + mG + mG + nU;
			for (int i = 0; i < nL; i++)
			{
				if (sxC[num + i] >= yzC[num + i])
				{
					_prmlPositiveList.Add(i);
				}
			}
			for (int j = 0; j < nU; j++)
			{
				if (sV[j] >= zV[j] && xU[j] >= zU[j])
				{
					_prmlPositiveList.Add(nL + j);
				}
			}
			_dualPositiveList.Clear();
			num = mK + nF + nF;
			int num2 = num + mG;
			for (int k = 0; k < mG; k++)
			{
				if (sxC[num + k] <= yzC[num + k] || sxC[num2 + k] <= yzC[num2 + k])
				{
					_dualPositiveList.Add(k);
				}
			}
			for (int l = 0; l < mK; l++)
			{
				if (sxC[l] <= yzC[l])
				{
					_dualPositiveList.Add(mG + l);
				}
			}
			return _prmlPositiveList.Count + _dualPositiveList.Count;
		}

		private void ComputeDiagPerturbation()
		{
			_DprmlPerturbed.CopyFrom(_Dprml);
			foreach (int prmlPositive in _prmlPositiveList)
			{
				_DprmlPerturbed[prmlPositive] = Math.Max(_Dprml[prmlPositive], _dmaxPrmlPosList / _prmlPertCond);
			}
			_DdualPerturbed.CopyFrom(_Ddual);
			foreach (int dualPositive in _dualPositiveList)
			{
				_DdualPerturbed[dualPositive] = Math.Max(_Ddual[dualPositive], _dmaxDualPosList / _dualPertCond);
			}
		}

		private int SolveAugmentedSystemIR(Vector rprd, ref Vector dpdd)
		{
			double num = Math.Max(1.0, rprd.Norm2());
			double num2 = double.PositiveInfinity;
			double num3 = double.PositiveInfinity;
			int num4 = 0;
			int num5 = 0;
			int num6 = -1;
			double num7 = 1.0;
			_augResidueVec.CopyFrom(rprd);
			dpdd.ZeroFill();
			bool flag = true;
			double num8;
			while (true)
			{
				num4 = ((_pivotPert <= 0 && !_isPertPhase) ? SolveSystem(_augResidueVec, ref _augSolutionVec, 0) : SolveSystem(_augResidueVec, ref _augSolutionVec, -1));
				Vector.Daxpy(1.0, _augSolutionVec, dpdd);
				num6 += num4 + 1;
				_augResidueVec.CopyFrom(rprd);
				num8 = ComputeResidue(ref _augResidueVec, dpdd) / num;
				if (num8 <= _options.EpsIR)
				{
					if (!_isPertPhase && num6 <= _options.MaxFastIR)
					{
						_prmlPertCond = Math.Max(_prmlPertCond, _prmlCond);
						_dualPertCond = Math.Max(_dualPertCond, _dualCond);
					}
					break;
				}
				if (num8 <= num3)
				{
					num3 = num8;
					if (!flag)
					{
						continue;
					}
				}
				if (num8 < num2 && num6 < _options.MaxIR)
				{
					num2 = num8;
					continue;
				}
				if (!_isPertPhase)
				{
					if (!flag)
					{
						break;
					}
					_isPertPhase = true;
				}
				else if ((_prmlPertCond >= _prmlCond && _dualPertCond >= _dualCond) || num7 >= 100.0)
				{
					flag = false;
					_isPertPhase = false;
				}
				else
				{
					_prmlPertCond *= 10.0;
					_dualPertCond *= 10.0;
					num7 *= 10.0;
				}
				if (flag)
				{
					ComputeDiagPerturbation();
					SetLinearSystem(_DprmlPerturbed, _DdualPerturbed);
					_pivotPert = NumericalFactorization();
					SetLinearSystemDiagonals(_Dprml, _Ddual);
				}
				else
				{
					SetLinearSystem(_Dprml, _Ddual);
					_pivotPert = NumericalFactorization();
				}
				num2 = double.PositiveInfinity;
				num5 += num6;
				num6 = -1;
				_augResidueVec.CopyFrom(rprd);
				dpdd.ZeroFill();
			}
			num5 += num6;
			_augResidueNorm = Math.Max(_augResidueNorm, num8);
			return num5;
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
			bigSum.Add(uV.InnerProduct(_DVuV));
			Vector.ElementMultiply(uH, _DG, _tmpG);
			bigSum.Add(_tmpG.InnerProduct(_DGHDHuH));
			bigSum.Add(0.0 - subVector.InnerProduct(_gL));
			bigSum.Add(0.0 - subVector2.InnerProduct(_gU));
			bigSum.Add(0.0 - subVector3.InnerProduct(_gF));
			bigSum.Add(subVector4.InnerProduct(_gG));
			bigSum.Add(subVector5.InnerProduct(_gI));
			bigSum.Add(0.0 - _DVuV.InnerProduct(_gU));
			bigSum.Add(_DGHDHuH.InnerProduct(_gG));
			_dtauDenom = bigSum.ToDouble();
		}

		private void ComputeDeltaTauNumerator()
		{
			SubVector subVector = new SubVector(cLcUcF, 0, nL);
			SubVector subVector2 = new SubVector(cLcUcF, nL, nU);
			SubVector subVector3 = new SubVector(cLcUcF, nL + nU, nF);
			SubVector subVector4 = new SubVector(bGbK, 0, mG);
			SubVector subVector5 = new SubVector(bGbK, mG, mK);
			BigSum bigSum = default(BigSum);
			bigSum.Add(_eta * _rg);
			bigSum.Add(_rcTK / _tau);
			Vector.ElementDivide(_rcV, sV, _tmpU);
			bigSum.Add(uV.InnerProduct(_tmpU));
			Vector.ElementMultiply(_DV, _rpV, _tmpU);
			bigSum.Add(_eta * uV.InnerProduct(_tmpU));
			Vector.ElementDivide(_rcG, sG, _tmpG);
			bigSum.Add(_DGHDHuH.InnerProduct(_tmpG));
			bigSum.Add((0.0 - _eta) * _DGHDHuH.InnerProduct(_rdG));
			Vector.ElementMultiply(_DG, _rpH, _tmpG);
			bigSum.Add(_eta * _DGHDHuH.InnerProduct(_tmpG));
			Vector.ElementDivide(_rcH, sH, _tmpH);
			Vector.ElementMultiply(_DG, _tmpH, _tmpG);
			Vector.ElementMultiply(uH, _DGH, _tmpH);
			bigSum.Add(_tmpH.InnerProduct(_tmpG));
			bigSum.Add(subVector.InnerProduct(_fL));
			bigSum.Add(subVector2.InnerProduct(_fU));
			bigSum.Add(subVector3.InnerProduct(_fF));
			bigSum.Add(0.0 - subVector4.InnerProduct(_fG));
			bigSum.Add(0.0 - subVector5.InnerProduct(_fI));
			bigSum.Add(_DVuV.InnerProduct(_fU));
			bigSum.Add(0.0 - _DGHDHuH.InnerProduct(_fG));
			_dtauNumer = bigSum.ToDouble();
		}

		private void AssembleSearchDirection(ref Vector dsx_, ref Vector dyz_, ref double dtau_, ref double dkappa_)
		{
			SubVector subVector = new SubVector(dsx_, 0, mK);
			SubVector subVector2 = new SubVector(dsx_, mK, nF);
			SubVector subVector3 = new SubVector(dsx_, mK + nF, nF);
			SubVector subVector4 = new SubVector(dsx_, mK + nF + nF, mG);
			SubVector subVector5 = new SubVector(dsx_, mK + nF + nF + mG, mG);
			SubVector subVector6 = new SubVector(dsx_, mK + nF + nF + mG + mG, nU);
			SubVector y = new SubVector(dsx_, mK + nF + nF + mG + mG + nU, nL);
			SubVector y2 = new SubVector(dsx_, mK + nF + nF + mG + mG + nU + nL, nU);
			SubVector x = new SubVector(dsx_, mK + nF + nF + mG + mG + nU + nL + nU, nF);
			SubVector subVector7 = new SubVector(dsx_, mK + nF + nF + mG + mG + nU, nL + nU + nF);
			SubVector x2 = new SubVector(dyz_, 0, mG);
			SubVector y3 = new SubVector(dyz_, mG, mK);
			SubVector subVector8 = new SubVector(dyz_, mG + mK, nF);
			SubVector subVector9 = new SubVector(dyz_, mG + mK + nF, nF);
			SubVector subVector10 = new SubVector(dyz_, mG + mK + nF + nF, mG);
			SubVector subVector11 = new SubVector(dyz_, mG + mK + nF + nF + mG, mG);
			SubVector subVector12 = new SubVector(dyz_, mG + mK + nF + nF + mG + mG, nU);
			SubVector subVector13 = new SubVector(dyz_, mG + mK + nF + nF + mG + mG + nU, nL);
			SubVector subVector14 = new SubVector(dyz_, mG + mK + nF + nF + mG + mG + nU + nL, nU);
			SubVector subVector15 = new SubVector(dyz_, 0, mG + mK);
			ComputeDeltaTauNumerator();
			dtau_ = _dtauNumer / _dtauDenom;
			if (_kappa < _tau && Math.Abs(dtau_ / _tau) < 0.0001)
			{
				dtau_ = 0.0;
			}
			subVector7.CopyFrom(_fLfUfF);
			Vector.Daxpy(dtau_, _gLgUgF, subVector7);
			subVector15.CopyFrom(_fGfI);
			Vector.Daxpy(dtau_, _gGgI, subVector15);
			_fG.CopyFrom(_rrGH);
			Vector.ElementMultiply(_DH, uH, _tmpG);
			Vector.Daxpy(0.0 - dtau_, _tmpG, _fG);
			Vector.Daxpy(1.0, x2, _fG);
			Vector.ElementMultiply(_DGH, _fG, subVector4);
			subVector4.ScaleBy(-1.0);
			_fF.CopyFrom(_rrPN);
			Vector.Daxpy(-1.0, x, _fF);
			Vector.ElementMultiply(_DPN, _fF, subVector8);
			subVector9.CopyFrom(subVector8);
			subVector9.ScaleBy(-1.0);
			Vector.Daxpy(_eta, _rdPN, subVector9);
			subVector5.CopyFrom(subVector4);
			subVector5.ScaleBy(-1.0);
			Vector.Daxpy(dtau_, uH, subVector5);
			Vector.Daxpy(0.0 - _eta, _rpH, subVector5);
			subVector6.CopyFrom(y2);
			subVector6.ScaleBy(-1.0);
			Vector.Daxpy(dtau_, uV, subVector6);
			Vector.Daxpy(0.0 - _eta, _rpV, subVector6);
			Vector.ElementDivide(_rcI, zI, subVector);
			Vector.ElementMultiply(_DI, y3, _fI);
			Vector.Daxpy(-1.0, _fI, subVector);
			Vector.ElementDivide(_rcP, zP, subVector2);
			Vector.ElementMultiply(_DP, subVector8, _fF);
			Vector.Daxpy(-1.0, _fF, subVector2);
			Vector.ElementDivide(_rcN, zN, subVector3);
			Vector.ElementMultiply(_DN, subVector9, _fF);
			Vector.Daxpy(-1.0, _fF, subVector3);
			Vector.ElementDivide(_rcG, sG, subVector10);
			Vector.ElementMultiply(_DG, subVector4, _fG);
			Vector.Daxpy(-1.0, _fG, subVector10);
			Vector.ElementDivide(_rcH, sH, subVector11);
			Vector.ElementMultiply(_DH, subVector5, _fG);
			Vector.Daxpy(-1.0, _fG, subVector11);
			Vector.ElementDivide(_rcU, xU, subVector14);
			Vector.ElementMultiply(_DU, y2, _fU);
			Vector.Daxpy(-1.0, _fU, subVector14);
			Vector.ElementDivide(_rcV, sV, subVector12);
			Vector.ElementMultiply(_DV, subVector6, _fU);
			Vector.Daxpy(-1.0, _fU, subVector12);
			Vector.ElementDivide(_rcL, xL, subVector13);
			Vector.ElementMultiply(_DL, y, _fL);
			Vector.Daxpy(-1.0, _fL, subVector13);
			dkappa_ = (_rcTK - _kappa * dtau_) / _tau;
		}

		private double FindMaxStepSize(Vector dsxC_, Vector dyzC_, double dtau_, double dkappa_)
		{
			double val = IpmVectorUtilities.IPMRatioTest(sxC, dsxC_);
			if (dtau_ < 0.0)
			{
				val = Math.Min(val, (0.0 - _tau) / dtau_);
			}
			double num = IpmVectorUtilities.IPMRatioTest(yzC, dyzC_);
			if (dkappa_ < 0.0)
			{
				num = Math.Min(num, (0.0 - _kappa) / dkappa_);
			}
			return Math.Min(val, num);
		}

		private void UpdatePrmlDualVariables(double stepsize, ref Vector sx_, ref Vector yz_, ref double tau_, ref double kappa_)
		{
			Vector.Daxpy(stepsize, _dsx, sx_);
			Vector.Daxpy(stepsize, _dyz, yz_);
			tau_ += stepsize * _dtau;
			kappa_ += stepsize * _dkappa;
		}

		private void UpdatePrmlDualVariables(double stepsize)
		{
			Vector.Daxpy(stepsize, _dsx, _sx);
			Vector.Daxpy(stepsize, _dyz, _yz);
			_tau += stepsize * _dtau;
			_kappa += stepsize * _dkappa;
		}

		private void ComputeCorrection(double stepsize)
		{
			_dsxMC.CopyFrom(_sx);
			_dyzMC.CopyFrom(_yz);
			Vector.Daxpy(stepsize, _dsx, _dsxMC);
			Vector.Daxpy(stepsize, _dyz, _dyzMC);
			_dtauMC = _tau + stepsize * _dtau;
			_dkappaMC = _kappa + stepsize * _dkappa;
			Vector.ElementMultiply(_dsxCMC, _dyzCMC, _dsxCdyzC);
			double val = _dtauMC * _dkappaMC;
			double num = _gamma * _mu * _options.MCComplementBound;
			double num2 = _gamma * _mu / _options.MCComplementBound;
			IpmVectorUtilities.BoxCorrection(_dsxCdyzC, num, num2, _dsxCMC);
			_dtauMC = num - Math.Min(val, num) + (num2 - Math.Max(val, num2));
			Vector.Daxpy(1.0, _dsxCMC, _rc);
			_rcTK += _dtauMC;
			ComputeRightHandSide();
			_iterRefine = Math.Max(_iterRefine, SolveAugmentedSystemIR(_rr, ref _f));
			AssembleSearchDirection(ref _dsxMC, ref _dyzMC, ref _dtauMC, ref _dkappaMC);
		}

		private void BackoffFromBoundary(ref double alpha)
		{
			double num = (1.0 - _options.BetaBackOffRatio) * alpha / _options.BetaBackOffRatio;
			int num2 = 0;
			while (num2++ < _options.MaxBackoffAttempts && Math.Min(_tau * _kappa / _mu, IpmVectorUtilities.IPMUniformTest(_dsxCdyzC, _mu)) < _options.BetaComplementUnif)
			{
				num *= _options.BetaBackOffRatio;
				UpdatePrmlDualVariables(0.0 - num, ref _sx, ref _yz, ref _tau, ref _kappa);
				alpha *= _options.BetaBackOffRatio;
				Vector.ElementMultiply(sxC, yzC, _dsxCdyzC);
				_mu = (_dsxCdyzC.Sum() + _tau * _kappa) / (double)(_nC + 1);
			}
		}

		protected override bool EmptySolution()
		{
			return xLxUxF == null;
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
				return vvm.lower + sI[vvm.iVar - mG];
			case VidToVarMapKind.RowBounded:
				if (!(vvm.lower == vvm.upper))
				{
					return vvm.lower + sG[vvm.iVar];
				}
				return vvm.lower;
			case VidToVarMapKind.RowUpper:
				return vvm.upper - sI[vvm.iVar - mG];
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
			return GeneralModel.MapToUserModel(ref vvm, xLxUxF[vvm.iVar]);
		}
	}
}
