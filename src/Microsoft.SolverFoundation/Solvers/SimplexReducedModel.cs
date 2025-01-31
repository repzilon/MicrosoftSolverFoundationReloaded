using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class SimplexReducedModel
	{
		protected struct GoalInfo
		{
			public int vid;

			public bool fMinimize;

			public GoalInfo(int vid, bool fMinimize)
			{
				this.vid = vid;
				this.fMinimize = fMinimize;
			}

			public override string ToString()
			{
				return string.Format(CultureInfo.InvariantCulture, "vid {0} {1}", new object[2]
				{
					vid,
					fMinimize ? "minimize" : "maximize"
				});
			}
		}

		private MIPVarFlags[] _mpvarflags;

		protected SimplexSolver _solver;

		protected int _ridLim;

		protected int _vidLim;

		internal int _rowLim;

		internal int _varLim;

		protected int _cvarSlack;

		protected int _cvarInt;

		protected CoefMatrix _mat;

		protected VectorRational _vecRhs;

		protected Rational[] _mpvarnumLower;

		protected Rational[] _mpvarnumUpper;

		protected Rational[] _mpvidnumScale;

		internal Rational[] _mpvidnumDelta;

		protected VectorDouble _vecRhsDbl;

		protected double[] _mpvarnumLowerDbl;

		protected double[] _mpvarnumUpperDbl;

		protected double[] _mpvarnumScaleDbl;

		internal bool _approximateSolve;

		internal int _presolveLevel;

		protected Rational[] _rowLowerShadowPrice;

		protected Rational[] _rowUpperShadowPrice;

		protected bool _isSOS1;

		protected bool _isSOS2;

		internal int[] _mpvarSOS1Row;

		internal int[] _mpvarSOS2Row;

		internal SOSNode[] _sosRows;

		protected int[] _mprowrid;

		protected int[] _mpridrow;

		protected int[] _mpvarvid;

		internal int[] _mpvidvar;

		protected int[] _mprowvidSlack;

		protected List<int> _rgvidElim;

		protected CoefMatrix _matElim;

		protected LUFactorizationRational _lufElim;

		protected GoalInfo[] _rggi;

		protected int _cgi;

		private VectorRational _TableauRowReduced;

		private VectorRational _vecE;

		internal bool IsSOS
		{
			get
			{
				if (!_isSOS1)
				{
					return _isSOS2;
				}
				return true;
			}
		}

		public SimplexSolver UserModel => _solver;

		public CoefMatrix Matrix => _mat;

		public int RowLim => _rowLim;

		public int VarLim => _varLim;

		public int CvarSlack => _cvarSlack;

		public int CvarInt => _cvarInt;

		public int GoalCount => _cgi;

		public SimplexReducedModel(SimplexReducedModel mod, IList<RowCut> cuts)
		{
			_approximateSolve = mod._approximateSolve;
			_solver = mod.UserModel;
			_presolveLevel = -1;
			_ridLim = mod._ridLim + cuts.Count;
			_vidLim = mod._vidLim + cuts.Count;
			_varLim = mod.VarLim + cuts.Count;
			_rowLim = mod.RowLim + cuts.Count;
			_mpvarvid = new int[_varLim];
			_mpvidvar = new int[_vidLim];
			Array.Copy(mod._mpvidvar, _mpvidvar, mod._mpvidvar.Length);
			Array.Copy(mod._mpvarvid, _mpvarvid, mod._varLim);
			_mpvarnumLower = new Rational[_varLim];
			_mpvarnumUpper = new Rational[_varLim];
			Array.Copy(mod._mpvarnumLower, _mpvarnumLower, mod._varLim);
			Array.Copy(mod._mpvarnumUpper, _mpvarnumUpper, mod._varLim);
			_mpvarnumLowerDbl = new double[_varLim];
			_mpvarnumUpperDbl = new double[_varLim];
			Array.Copy(mod._mpvarnumLowerDbl, _mpvarnumLowerDbl, mod._varLim);
			Array.Copy(mod._mpvarnumUpperDbl, _mpvarnumUpperDbl, mod._varLim);
			_mpvarnumScaleDbl = new double[_varLim];
			Array.Copy(mod._mpvarnumScaleDbl, _mpvarnumScaleDbl, mod._varLim);
			_mpvidnumScale = new Rational[_vidLim];
			_mpvidnumDelta = new Rational[_vidLim];
			Array.Copy(mod._mpvidnumScale, _mpvidnumScale, mod._vidLim);
			Array.Copy(mod._mpvidnumDelta, _mpvidnumDelta, mod._vidLim);
			_mprowrid = new int[_rowLim];
			_mpridrow = new int[_ridLim];
			_mprowvidSlack = new int[_rowLim];
			Array.Copy(mod._mprowrid, _mprowrid, mod.RowLim);
			Array.Copy(mod._mpridrow, _mpridrow, mod.RowLim);
			Array.Copy(mod._mprowvidSlack, _mprowvidSlack, mod.RowLim);
			_vecRhs = new VectorRational(_rowLim);
			_vecRhsDbl = new VectorDouble(_rowLim);
			_rggi = new GoalInfo[_solver.GoalCount];
			foreach (LinearModel.Goal goal in _solver.Goals)
			{
				if (goal.Enabled)
				{
					ref GoalInfo reference = ref _rggi[_cgi];
					reference = new GoalInfo(goal.Index, goal.Minimize);
					_cgi++;
				}
			}
			InitializeReducedModelWithCuts(mod, cuts);
		}

		/// <summary> Add cuts to the reduced model and initialize cut related data structures
		/// </summary>
		private void InitializeReducedModelWithCuts(SimplexReducedModel mod, IList<RowCut> cuts)
		{
			int num = mod.VarLim;
			for (int i = mod._vidLim; i < _vidLim; i++)
			{
				_mpvidvar[i] = num;
				_mpvarvid[num] = i;
				num++;
			}
			for (int j = mod._vidLim; j < _vidLim; j++)
			{
				ref Rational reference = ref _mpvidnumScale[j];
				reference = 1;
				ref Rational reference2 = ref _mpvidnumDelta[j];
				reference2 = 0;
			}
			for (int k = mod._varLim; k < _varLim; k++)
			{
				_mpvarnumScaleDbl[k] = 1.0;
			}
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(mod._vecRhs);
			while (iter.IsValid)
			{
				_vecRhs.SetCoefNonZero(iter.Rc, iter.Value);
				iter.Advance();
			}
			Vector<double>.Iter iter2 = new Vector<double>.Iter(mod._vecRhsDbl);
			while (iter2.IsValid)
			{
				_vecRhsDbl.SetCoefNonZero(iter2.Rc, iter2.Value);
				iter2.Advance();
			}
			int num2 = mod.Matrix.EntryCount;
			foreach (RowCut cut in cuts)
			{
				num2 += cut.Row.EntryCount + 1;
			}
			_mat = new CoefMatrix(_rowLim, _varLim, num2, fExact: true, fDouble: true);
			for (int l = 0; l < mod.RowLim; l++)
			{
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(mod.Matrix, l);
				while (rowIter.IsValid)
				{
					_mat.SetCoef(l, rowIter.Column, rowIter.Exact, rowIter.Approx);
					rowIter.Advance();
				}
			}
			int num3 = mod.VarLim;
			int num4 = 0;
			int num5 = mod._ridLim;
			for (int m = mod.RowLim; m < _rowLim; m++)
			{
				Vector<double>.Iter iter3 = new Vector<double>.Iter(cuts[num4].Row);
				while (iter3.IsValid)
				{
					_mat.SetCoef(m, iter3.Rc, iter3.Value * mod.GetScaleDbl(iter3.Rc), iter3.Value);
					iter3.Advance();
				}
				_mat.SetCoef(m, num3, -1, -1.0);
				ref Rational reference3 = ref _mpvarnumLower[num3];
				reference3 = cuts[num4].LowerRowBound;
				ref Rational reference4 = ref _mpvarnumUpper[num3];
				reference4 = cuts[num4].UpperRowBound;
				_mpvarnumLowerDbl[num3] = cuts[num4].LowerRowBound;
				_mpvarnumUpperDbl[num3] = cuts[num4].UpperRowBound;
				_mprowrid[m] = num5;
				_mpridrow[num5] = m;
				_mprowvidSlack[m] = _mpvarvid[num3];
				num4++;
				num5++;
				num3++;
			}
		}

		public void SetLowerBound(int var, Rational num)
		{
			if (!(_mpvarnumLower[var] >= num))
			{
				if (IsIntegerVar(var))
				{
					ref Rational reference = ref _mpvarnumLower[var];
					reference = num.GetCeiling();
				}
				else
				{
					_mpvarnumLower[var] = num;
				}
			}
		}

		public void SetUpperBound(int var, Rational num)
		{
			if (!(_mpvarnumUpper[var] <= num))
			{
				if (IsIntegerVar(var))
				{
					ref Rational reference = ref _mpvarnumUpper[var];
					reference = num.GetFloor();
				}
				else
				{
					_mpvarnumUpper[var] = num;
				}
			}
		}

		public bool IsBinaryVar(int var)
		{
			return _mpvarflags[var] == MIPVarFlags.Binary;
		}

		public bool IsIntegerVar(int var)
		{
			return (_mpvarflags[var] & (MIPVarFlags.Binary | MIPVarFlags.Integer)) != 0;
		}

		public bool IsContinuousVar(int var)
		{
			return _mpvarflags[var] == MIPVarFlags.Continuous;
		}

		public RowType GetRowType(int rowVar)
		{
			Rational upperBound = GetUpperBound(rowVar);
			Rational lowerBound = GetLowerBound(rowVar);
			if (upperBound == lowerBound)
			{
				return RowType.Equal;
			}
			if (upperBound.IsPositiveInfinity && lowerBound.IsFinite)
			{
				return RowType.GreaterEqual;
			}
			if (upperBound.IsFinite && lowerBound.IsNegativeInfinity)
			{
				return RowType.LessEqual;
			}
			return RowType.Unknown;
		}

		private void InitMIP()
		{
			_mpvarflags = new MIPVarFlags[_varLim];
			for (int i = 0; i < _varLim; i++)
			{
				int vid = _mpvarvid[i];
				if (_solver.HasFlag(vid, RowVariableModel.VidFlags.Integer))
				{
					Rational rational = _mpvarnumLower[i];
					Rational rational2 = _mpvarnumUpper[i];
					if ((rational == 0 && rational2 == 1) || (rational == 1 && rational2 == 1) || (rational == 0 && rational2 == 0))
					{
						_mpvarflags[i] = MIPVarFlags.Binary;
					}
					else
					{
						_mpvarflags[i] = MIPVarFlags.Integer;
					}
				}
				else
				{
					_mpvarflags[i] = MIPVarFlags.Continuous;
				}
			}
		}

		private void ComputeMIPRowBounds(int row, out Rational rowBoundsLower, out Rational rowBoundsUpper)
		{
			int slackVarForRow = GetSlackVarForRow(row);
			rowBoundsLower = Rational.Zero;
			rowBoundsUpper = Rational.Zero;
			CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_mat, row);
			while (rowIter.IsValid)
			{
				int column = rowIter.Column;
				if (column != slackVarForRow)
				{
					Rational exact = rowIter.Exact;
					if (IsBinaryVar(column))
					{
						if (exact > 0)
						{
							rowBoundsUpper += exact;
						}
						else
						{
							rowBoundsLower += exact;
						}
					}
					else if (exact > 0)
					{
						rowBoundsUpper += exact * GetUpperBound(column);
						rowBoundsLower += exact * GetLowerBound(column);
					}
					else
					{
						rowBoundsUpper += exact * GetLowerBound(column);
						rowBoundsLower += exact * GetUpperBound(column);
					}
				}
				rowIter.Advance();
			}
		}

		public bool MIPPreSolve(MipNode node)
		{
			InitMIP();
			bool flag = true;
			while (flag)
			{
				for (int i = 0; i < _rowLim; i++)
				{
					node.MipSolver.CheckDone(LinearResult.Feasible);
					if (IsGoal(i) || IsRowEliminated(i))
					{
						continue;
					}
					ComputeMIPRowBounds(i, out var rowBoundsLower, out var rowBoundsUpper);
					int slackVarForRow = GetSlackVarForRow(i);
					Rational upperBound = GetUpperBound(slackVarForRow);
					Rational lowerBound = GetLowerBound(slackVarForRow);
					Rational indeterminate = Rational.Indeterminate;
					RowType rowType = GetRowType(slackVarForRow);
					if (rowType == RowType.Equal)
					{
						if (rowBoundsLower > upperBound || rowBoundsUpper < lowerBound)
						{
							return false;
						}
						indeterminate = lowerBound;
					}
					else
					{
						if (rowType != RowType.LessEqual)
						{
							continue;
						}
						if (rowBoundsLower > upperBound)
						{
							return false;
						}
						indeterminate = upperBound;
					}
					if (rowType == RowType.LessEqual && rowBoundsUpper <= indeterminate)
					{
						int num = _mprowrid[i];
						_mpridrow[num] = -1;
						continue;
					}
					bool flag2 = false;
					CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_mat, i);
					while (rowIter.IsValid)
					{
						int column = rowIter.Column;
						if (slackVarForRow != column)
						{
							Rational exact = rowIter.Exact;
							if (IsBinaryVar(column))
							{
								if (flag2)
								{
									ComputeMIPRowBounds(i, out rowBoundsLower, out rowBoundsUpper);
									upperBound = GetUpperBound(slackVarForRow);
									lowerBound = GetLowerBound(slackVarForRow);
									flag2 = false;
								}
								Rational rational = rowBoundsUpper;
								Rational rational2 = exact;
								Rational rational3 = upperBound;
								if (rowType == RowType.GreaterEqual)
								{
									rational = -rowBoundsLower;
									rational2 = -exact;
									rational3 = -lowerBound;
								}
								if (!rational3.IsInfinite)
								{
									if (rational2 < 0)
									{
										Rational rational4 = rational3 - rational;
										if (rational2 < rational4)
										{
											if (rowType == RowType.GreaterEqual)
											{
												_mat.SetCoefExact(i, column, -rational4);
											}
											else
											{
												_mat.SetCoefExact(i, column, rational4);
											}
											flag2 = true;
										}
									}
									else
									{
										Rational rational5 = rational - exact;
										if (rational5 < rational3)
										{
											if (rowType == RowType.GreaterEqual)
											{
												_mat.SetCoefExact(i, column, rational3 - rational);
												if (-rational5 > upperBound)
												{
													return false;
												}
												SetLowerBound(slackVarForRow, -rational5);
											}
											else
											{
												_mat.SetCoefExact(i, column, rational - rational3);
												if (lowerBound > rational5)
												{
													return false;
												}
												SetUpperBound(slackVarForRow, rational5);
											}
											flag2 = true;
										}
									}
								}
							}
							else if (exact > 0)
							{
								Rational num2 = (indeterminate - rowBoundsLower) / exact + GetLowerBound(column);
								if (!num2.IsIndeterminate)
								{
									SetUpperBound(column, num2);
								}
								switch (rowType)
								{
								case RowType.Equal:
								{
									Rational num3 = (indeterminate - rowBoundsUpper) / exact + GetUpperBound(column);
									if (!num3.IsIndeterminate)
									{
										SetLowerBound(column, num3);
									}
									break;
								}
								case RowType.LessEqual:
								{
									Rational num3 = (rowBoundsLower - rowBoundsUpper) / exact + GetUpperBound(column);
									if (!num3.IsIndeterminate)
									{
										SetLowerBound(column, num3);
									}
									break;
								}
								}
							}
							else
							{
								Rational num3 = (indeterminate - rowBoundsLower) / exact + GetUpperBound(column);
								if (!num3.IsIndeterminate)
								{
									SetLowerBound(column, num3);
								}
								switch (rowType)
								{
								case RowType.Equal:
								{
									Rational num2 = (indeterminate - rowBoundsUpper) / exact + GetLowerBound(column);
									if (!num2.IsIndeterminate)
									{
										SetUpperBound(column, num2);
									}
									break;
								}
								case RowType.LessEqual:
								{
									Rational num2 = (rowBoundsLower - rowBoundsUpper) / exact + GetLowerBound(column);
									if (!num2.IsIndeterminate)
									{
										SetUpperBound(column, num2);
									}
									break;
								}
								}
							}
						}
						rowIter.Advance();
					}
				}
				if (EliminateFixedVariables() == 0 || EliminateSingletonRows() == 0)
				{
					break;
				}
				flag &= EliminateRowsAndColumns();
			}
			flag &= EliminateRowsAndColumns();
			return flag & IsEliminatedIntegerVidFeasible();
		}

		/// <summary> Check if all eliminated integer vids are fixed to integer values
		/// </summary>
		/// <returns>True if they all are fixed to integer values</returns>
		private bool IsEliminatedIntegerVidFeasible()
		{
			if (_varLim < _vidLim)
			{
				int num = _vidLim;
				while (--num >= 0)
				{
					if (_mpvidvar[num] < 0 && _solver.GetIntegrality(num) && !_mpvidnumDelta[num].IsInteger())
					{
						return false;
					}
				}
			}
			return true;
		}

		private bool EliminateRowsAndColumns()
		{
			int num = 0;
			for (int i = 0; i < _rowLim; i++)
			{
				int num2 = _mprowrid[i];
				if (_mat.RowEntryCount(i) == 0)
				{
					_mpridrow[num2] = -1;
					continue;
				}
				if (_mpridrow[num2] == -1)
				{
					EliminateVid(_mprowvidSlack[i], _mat, i, fMap: true);
					_mat.ClearRow(i);
					_mprowvidSlack[i] = -1;
					continue;
				}
				if (num < i)
				{
					_mprowrid[num] = num2;
					_mpridrow[num2] = num;
					_mprowvidSlack[num] = _mprowvidSlack[i];
				}
				num++;
			}
			if (num < _rowLim)
			{
				_mat.RemoveEmptyRows();
				_rowLim = num;
			}
			_cvarSlack = 0;
			_cvarInt = 0;
			bool flag = false;
			int num3 = 0;
			for (int j = 0; j < _varLim; j++)
			{
				if (_mpvarnumLower[j] > _mpvarnumUpper[j])
				{
					flag = true;
					ref Rational reference = ref _mpvarnumLower[j];
					reference = _mpvarnumUpper[j];
				}
				int num4 = _mpvarvid[j];
				if (_mat.ColEntryCount(j) == 0 && !_solver.IsActiveGoal(num4))
				{
					_mpvidvar[num4] = -1;
					if (!(_mpvidnumScale[num4] != 0))
					{
						continue;
					}
					Rational rational;
					Rational rational2 = (rational = _mpvarnumLower[j]);
					if (!rational2.IsFinite)
					{
						Rational rational3 = (rational = _mpvarnumUpper[j]);
						if (!rational3.IsFinite)
						{
							rational = default(Rational);
						}
					}
					_mpvidnumDelta[num4] += rational * _mpvidnumScale[num4];
					ref Rational reference2 = ref _mpvidnumScale[num4];
					reference2 = 0;
					continue;
				}
				if (_solver._mpvidvi[num4].IsRow && GetRow(num4) != -1)
				{
					_cvarSlack++;
				}
				if (_solver.HasFlag(num4, RowVariableModel.VidFlags.Integer))
				{
					_cvarInt++;
				}
				if (num3 < j)
				{
					ref Rational reference3 = ref _mpvarnumLower[num3];
					reference3 = _mpvarnumLower[j];
					ref Rational reference4 = ref _mpvarnumUpper[num3];
					reference4 = _mpvarnumUpper[j];
					_mpvarvid[num3] = num4;
					_mpvidvar[num4] = num3;
				}
				num3++;
			}
			if (num3 < _varLim)
			{
				_mat.RemoveEmptyColumns(null);
				_varLim = num3;
			}
			return !flag;
		}

		public void RecalculateSlacksAndGoals(CoefMatrix userModelCoefM, Rational[] mpvidnum, OptimalGoalValues optGoalVals)
		{
			int num = mpvidnum.Length;
			int num2 = _rowLim;
			while (--num2 >= 0)
			{
				int slackVarForRow = GetSlackVarForRow(num2);
				int num3 = _mpvarvid[slackVarForRow];
				if (num3 < num && !_solver.IsGoal(num3) && !(MapValueFromVarToVid(slackVarForRow, _mpvarnumUpper[slackVarForRow]) == _solver._mpvidnumHi[num3]))
				{
					int rid = _mprowrid[num2];
					ref Rational reference = ref mpvidnum[num3];
					reference = ComputeUserRowValue(userModelCoefM, mpvidnum, rid, num3);
				}
			}
			if (optGoalVals != null)
			{
				int num4 = _cgi;
				while (--num4 >= 0)
				{
					GoalInfo goalInfo = _rggi[num4];
					int vid = goalInfo.vid;
					ref Rational reference2 = ref mpvidnum[vid];
					reference2 = ComputeUserRowValue(userModelCoefM, mpvidnum, _solver._mpvidvi[vid].Rid, vid);
					optGoalVals[num4] = mpvidnum[vid];
				}
			}
		}

		private static Rational ComputeUserRowValue(CoefMatrix userModelCoefM, Rational[] mpvidnum, int rid, int slackVid)
		{
			Rational rational = 0;
			CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(userModelCoefM, rid);
			while (rowIter.IsValid)
			{
				if (rowIter.Column != slackVid)
				{
					rational = Rational.AddMul(rational, mpvidnum[rowIter.Column], rowIter.Exact);
				}
				rowIter.Advance();
			}
			return rational;
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="solver">The solver containing the user model.</param>
		public SimplexReducedModel(SimplexSolver solver)
		{
			_solver = solver;
			_approximateSolve = false;
			_presolveLevel = -1;
			Load();
		}

		/// <summary>
		/// Creates a new instance without the specific row
		/// </summary>
		/// <param name="solver">The solver containing the user
		/// model.</param>
		/// <param name="rowFilter">row filter</param>    
		public SimplexReducedModel(SimplexSolver solver, bool[] rowFilter)
		{
			_solver = solver;
			_approximateSolve = false;
			_presolveLevel = -1;
			LoadWithoutRow(rowFilter);
			_vecRhs = new VectorRational(_rowLim);
		}

		/// <summary>
		/// Loads our internal representation from the user specified representation.
		/// This only eliminates disabled rows, nothing more.
		/// </summary>
		protected virtual void Load()
		{
			InitVidToVarMapping();
			_mprowrid = new int[_ridLim];
			_mpridrow = new int[_ridLim];
			_mprowvidSlack = new int[_ridLim];
			_mat = new CoefMatrix(_ridLim, _varLim, _solver._matModel.EntryCount, fExact: true, fDouble: true);
			_rowLim = 0;
			for (int i = 0; i < _ridLim; i++)
			{
				int num = _solver._mpridvid[i];
				int num2 = _mpvidvar[num];
				if (_solver.HasFlag(num, RowVariableModel.VidFlags.IgnoreBounds) || (_mpvarnumLower[num2].IsNegativeInfinity && _mpvarnumUpper[num2].IsPositiveInfinity))
				{
					if (!_solver.IsActiveGoal(num) && _solver._matModel.ColEntryCount(num) == 1)
					{
						EliminateVid(num, _solver._matModel, i, fMap: false);
						continue;
					}
					ref Rational reference = ref _mpvarnumLower[num2];
					reference = Rational.NegativeInfinity;
					ref Rational reference2 = ref _mpvarnumUpper[num2];
					reference2 = Rational.PositiveInfinity;
				}
				AppendRow(i);
			}
			_rggi = new GoalInfo[_solver.GoalCount];
			_cgi = 0;
			foreach (LinearModel.Goal goal in _solver.Goals)
			{
				if (goal.Enabled)
				{
					ref GoalInfo reference3 = ref _rggi[_cgi];
					reference3 = new GoalInfo(goal.Index, goal.Minimize);
					_cgi++;
				}
			}
		}

		public void BuildSOSModel()
		{
			_isSOS2 = _solver._sos2Rows != null;
			_isSOS1 = _solver._sos1Rows != null;
			if (!_isSOS1 && !_isSOS2)
			{
				return;
			}
			_sosRows = new SOSNode[_vidLim];
			if (_isSOS2)
			{
				_mpvarSOS2Row = new int[_varLim];
				for (int i = 0; i < _varLim; i++)
				{
					_mpvarSOS2Row[i] = -1;
				}
				foreach (int sos2Row in _solver._sos2Rows)
				{
					BuildSOSRow(sos2Row, isSos2: true);
				}
			}
			if (!_isSOS1)
			{
				return;
			}
			_mpvarSOS1Row = new int[_varLim];
			for (int j = 0; j < _varLim; j++)
			{
				_mpvarSOS1Row[j] = -1;
			}
			foreach (int sos1Row in _solver._sos1Rows)
			{
				BuildSOSRow(sos1Row, isSos2: false);
			}
		}

		private void BuildSOSRow(int vidRow, bool isSos2)
		{
			int num = _solver.GetRowEntryCount(vidRow) - 1;
			if (num < 2)
			{
				return;
			}
			int[] array = new int[num];
			double[] array2 = new double[num];
			int num2 = 0;
			foreach (LinearEntry rowEntry in _solver.GetRowEntries(vidRow))
			{
				int index = rowEntry.Index;
				int num3 = _mpvidvar[index];
				double num4 = (double)_solver.GetCoefficient(vidRow, index);
				array[num2] = num3;
				array2[num2++] = num4;
				if (isSos2)
				{
					_mpvarSOS2Row[num3] = vidRow;
				}
				else
				{
					_mpvarSOS1Row[num3] = vidRow;
				}
			}
			Array.Sort(array2, array);
			SOSNode sOSNode = default(SOSNode);
			sOSNode.Vars = array;
			_sosRows[vidRow] = sOSNode;
		}

		/// <summary>
		/// Loads our internal representation from the user specified representation.
		/// This only eliminates disabled rows, nothing more.
		/// </summary>
		/// <param name="rowFilter">row filter</param>    
		protected virtual void LoadWithoutRow(bool[] rowFilter)
		{
			InitVidToVarMapping();
			_mprowrid = new int[_ridLim];
			_mpridrow = new int[_ridLim];
			_mprowvidSlack = new int[_ridLim];
			_mat = new CoefMatrix(_ridLim, _varLim, _solver._matModel.EntryCount, fExact: true, fDouble: true);
			_rowLim = 0;
			for (int i = 0; i < _ridLim; i++)
			{
				if (!rowFilter[i])
				{
					continue;
				}
				int num = _solver._mpridvid[i];
				int num2 = _mpvidvar[num];
				if (_solver.HasFlag(num, RowVariableModel.VidFlags.IgnoreBounds) || (_mpvarnumLower[num2].IsNegativeInfinity && _mpvarnumUpper[num2].IsPositiveInfinity))
				{
					if (!_solver.IsActiveGoal(num) && _solver._matModel.ColEntryCount(num) == 1)
					{
						EliminateVid(num, _solver._matModel, i, fMap: false);
						continue;
					}
					ref Rational reference = ref _mpvarnumLower[num2];
					reference = Rational.NegativeInfinity;
					ref Rational reference2 = ref _mpvarnumUpper[num2];
					reference2 = Rational.PositiveInfinity;
				}
				AppendRow(i);
			}
			_rggi = new GoalInfo[_solver.GoalCount];
			_cgi = 0;
			foreach (LinearModel.Goal goal in _solver.Goals)
			{
				if (goal.Enabled)
				{
					ref GoalInfo reference3 = ref _rggi[_cgi];
					reference3 = new GoalInfo(goal.Index, goal.Minimize);
					_cgi++;
				}
			}
		}

		internal void InitDbl(SimplexSolverParams prm)
		{
			_mat.CopyExactToApprox();
			_vecRhsDbl = new VectorDouble(_rowLim);
			_mpvarnumLowerDbl = new double[_varLim];
			_mpvarnumUpperDbl = new double[_varLim];
			_mpvarnumScaleDbl = new double[_varLim];
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_vecRhs);
			while (iter.IsValid)
			{
				_vecRhsDbl.SetCoefNonZero(iter.Rc, (double)iter.Value);
				iter.Advance();
			}
			int num = _varLim;
			while (--num >= 0)
			{
				_mpvarnumLowerDbl[num] = (double)_mpvarnumLower[num];
				_mpvarnumUpperDbl[num] = (double)_mpvarnumUpper[num];
				_mpvarnumScaleDbl[num] = 1.0;
			}
			ScaleDbl(prm.MaxGeometricScalingIterations, prm.GeometricScalingThreshold);
		}

		public int GetGoalVar(int igi)
		{
			return _mpvidvar[_rggi[igi].vid];
		}

		public int GetGoalVid(int igi)
		{
			return _rggi[igi].vid;
		}

		public int GetGoalRow(int igi)
		{
			return _mpridrow[_solver._mpvidvi[_rggi[igi].vid].Rid];
		}

		/// <summary>
		/// Gets the index of a row (i.e., its position in the reduced model matrix) given a row id in the user model.
		/// </summary>
		/// <param name="vidRow">The id of the row.</param>
		/// <returns>The index of the row or -1 if the row has been eliminated.</returns>
		public int GetRow(int vidRow)
		{
			return _mpridrow[_solver._mpvidvi[vidRow].Rid];
		}

		public bool IsGoalMinimize(int igi)
		{
			return _rggi[igi].fMinimize;
		}

		public int GetSlackVarForRow(int row)
		{
			return _mpvidvar[_mprowvidSlack[row]];
		}

		public Rational GetLowerBound(int var)
		{
			return _mpvarnumLower[var];
		}

		public Rational GetUpperBound(int var)
		{
			return _mpvarnumUpper[var];
		}

		public void GetLowerBounds(Rational[] rgnum)
		{
			Array.Copy(_mpvarnumLower, rgnum, _varLim);
		}

		public void GetUpperBounds(Rational[] rgnum)
		{
			Array.Copy(_mpvarnumUpper, rgnum, _varLim);
		}

		public VectorRational GetRhs()
		{
			return _vecRhs;
		}

		public bool IsVarInteger(int var)
		{
			if (_mpvarvid[var] >= _solver._vidLim)
			{
				return false;
			}
			return _solver.HasFlag(_mpvarvid[var], RowVariableModel.VidFlags.Integer);
		}

		public bool IsSlackVar(int var)
		{
			if (_mpvarvid[var] >= _solver._vidLim)
			{
				return true;
			}
			return _solver.IsRow(_mpvarvid[var]);
		}

		/// <summary>
		/// Checks whether a variable is a binary variable.
		/// </summary>
		/// <param name="variable">The variable to test.</param>
		/// <returns>True if the variable is binary; false otherwise.</returns>
		public bool IsBinary(int variable)
		{
			int vid = _mpvarvid[variable];
			if (_solver.HasFlag(vid, RowVariableModel.VidFlags.Integer))
			{
				Rational ceiling = MapValueFromVarToVid(variable, _mpvarnumLower[variable]).GetCeiling();
				Rational floor = MapValueFromVarToVid(variable, _mpvarnumUpper[variable]).GetFloor();
				if ((!(ceiling == 0) || !(floor == 1)) && (!(ceiling == 1) || !(floor == 1)))
				{
					if (ceiling == 0)
					{
						return floor == 0;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Checks whether a variable is fixed.
		/// </summary>
		/// <param name="variable">The variable to test.</param>
		/// <returns>True if the variable is fixed; false otherwise.</returns>
		public bool IsFixed(int variable)
		{
			int vid = _mpvarvid[variable];
			if (_solver.HasFlag(vid, RowVariableModel.VidFlags.Integer))
			{
				Rational ceiling = MapValueFromVarToVid(variable, _mpvarnumLower[variable]).GetCeiling();
				Rational floor = MapValueFromVarToVid(variable, _mpvarnumUpper[variable]).GetFloor();
				return ceiling == floor;
			}
			return _mpvarnumLower[variable] == _mpvarnumUpper[variable];
		}

		/// <summary>
		/// Checks whether a row is a goal row (true) or a constraint row (false).
		/// </summary>
		/// <param name="row">The row to test.</param>
		/// <returns>True for a goal row; false otherwise.</returns>
		public bool IsGoal(int row)
		{
			return _solver.IsGoal(_solver._mpridvid[_mprowrid[row]]);
		}

		/// <summary>
		/// Checks whether a row has been eliminated.
		/// </summary>
		/// <param name="row">The row to test.</param>
		/// <returns>True if the row has been eliminated; false otherwise.</returns>
		public bool IsRowEliminated(int row)
		{
			return _mprowvidSlack[row] == -1;
		}

		public bool HasBasicFlag(int var)
		{
			return _solver.HasFlag(_mpvarvid[var], RowVariableModel.VidFlags.Basic);
		}

		public bool FindGoal(int var, out int igi)
		{
			int num = _mpvarvid[var];
			for (igi = 0; igi < _rggi.Length; igi++)
			{
				if (_rggi[igi].vid == num)
				{
					return true;
				}
			}
			return false;
		}

		public int GetVid(int var)
		{
			return _mpvarvid[var];
		}

		public int GetVar(int vid)
		{
			return _mpvidvar[vid];
		}

		public double GetLowerBoundDbl(int var)
		{
			return _mpvarnumLowerDbl[var];
		}

		public void SetLowerBoundDbl(int var, double num)
		{
			if (IsIntegerVar(var))
			{
				_mpvarnumLowerDbl[var] = Math.Ceiling(num);
			}
			else
			{
				_mpvarnumLowerDbl[var] = num;
			}
		}

		public double GetUpperBoundDbl(int var)
		{
			return _mpvarnumUpperDbl[var];
		}

		public void SetUpperBoundDbl(int var, double num)
		{
			if (IsIntegerVar(var))
			{
				_mpvarnumUpperDbl[var] = Math.Floor(num);
			}
			else
			{
				_mpvarnumUpperDbl[var] = num;
			}
		}

		/// <summary> Copy the model's lower bounds into the rgnum array.
		/// </summary>
		public void GetLowerBoundsDbl(double[] rgnum)
		{
			Array.Copy(_mpvarnumLowerDbl, rgnum, _varLim);
		}

		/// <summary> Copy the model's upper bounds into the rgnum array.
		/// </summary>
		public void GetUpperBoundsDbl(double[] rgnum)
		{
			Array.Copy(_mpvarnumUpperDbl, rgnum, _varLim);
		}

		public VectorDouble GetRhsDbl()
		{
			return _vecRhsDbl;
		}

		private void ScaleDbl(int cvMaxGeo, double dblGeoThresh)
		{
			if (dblGeoThresh < 1.0)
			{
				dblGeoThresh = 1.0;
			}
			for (int i = 0; i < cvMaxGeo; i++)
			{
				double dblRatioMax;
				int num = ScaleRowsGeometricDbl(out dblRatioMax);
				if (dblRatioMax <= dblGeoThresh)
				{
					break;
				}
				int num2 = ScaleColsGeometricDbl();
				if (num == 0 && num2 == 0)
				{
					break;
				}
			}
			ScaleRowsDbl();
			ScaleColsDbl();
		}

		private int ScaleRowsGeometricDbl(out double dblRatioMax)
		{
			int num = 0;
			dblRatioMax = 1.0;
			for (int i = 0; i < _rowLim; i++)
			{
				int num2 = 0;
				int exp = 0;
				double dbl = 1.0;
				double num3 = 0.0;
				double num4 = double.PositiveInfinity;
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_mat, i);
				while (rowIter.IsValid)
				{
					if (_mat.ColEntryCount(rowIter.Column) > 1)
					{
						double num5 = Math.Abs(rowIter.Approx);
						if (num5 != 0.0)
						{
							if (num4 > num5)
							{
								num4 = num5;
							}
							if (num3 < num5)
							{
								num3 = num5;
							}
							dbl *= num5;
							NumberUtils.NormalizeExponent(ref dbl, ref exp);
							num2++;
						}
					}
					rowIter.Advance();
				}
				if (num2 == 0)
				{
					continue;
				}
				int num6 = (int)Math.Round((double)exp / (double)num2);
				if (num6 != 0)
				{
					double doubleFromParts = NumberUtils.GetDoubleFromParts(1, -num6, 1uL);
					_mat.ScaleRowApprox(i, doubleFromParts);
					double coef = _vecRhsDbl.GetCoef(i);
					if (coef != 0.0)
					{
						_vecRhsDbl.SetCoefNonZero(i, coef * doubleFromParts);
					}
					num++;
				}
				double num7 = num3 / num4;
				if (dblRatioMax < num7)
				{
					dblRatioMax = num7;
				}
			}
			return num;
		}

		private int ScaleColsGeometricDbl()
		{
			int num = 0;
			for (int i = 0; i < _varLim; i++)
			{
				int num2 = 0;
				int exp = 0;
				double dbl = 1.0;
				CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(_mat, i);
				while (colIter.IsValid)
				{
					if (_mat.RowEntryCount(colIter.Row) > 1)
					{
						double num3 = Math.Abs(colIter.Approx);
						if (num3 != 0.0)
						{
							dbl *= num3;
							NumberUtils.NormalizeExponent(ref dbl, ref exp);
							num2++;
						}
					}
					colIter.Advance();
				}
				int exp2;
				if (num2 > 0 && (exp2 = (int)Math.Round((double)exp / (double)num2)) != 0)
				{
					double doubleFromParts = NumberUtils.GetDoubleFromParts(1, exp2, 1uL);
					_mat.ScaleColApprox(i, 1.0 / doubleFromParts);
					_mpvarnumLowerDbl[i] *= doubleFromParts;
					_mpvarnumUpperDbl[i] *= doubleFromParts;
					_mpvarnumScaleDbl[i] *= doubleFromParts;
					num++;
				}
			}
			return num;
		}

		/// <summary>
		/// Scale the rows so each has a maximum absolute value of 1.
		/// </summary>
		private void ScaleRowsDbl()
		{
			for (int i = 0; i < _rowLim; i++)
			{
				double num = 0.0;
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_mat, i);
				while (rowIter.IsValid)
				{
					if (_mat.ColEntryCount(rowIter.Column) > 1)
					{
						double num2 = Math.Abs(rowIter.Approx);
						if (num < num2)
						{
							num = num2;
						}
					}
					rowIter.Advance();
				}
				double num3 = num;
				if (num3 != 1.0 && num3 > 0.0)
				{
					num3 = 1.0 / num3;
					_mat.ScaleRowApprox(i, num3);
					double coef = _vecRhsDbl.GetCoef(i);
					if (coef != 0.0)
					{
						_vecRhsDbl.SetCoefNonZero(i, coef * num3);
					}
				}
			}
		}

		/// <summary>
		/// Scale the columns so each has a maximum absolute value of 1.
		/// </summary>
		private void ScaleColsDbl()
		{
			for (int i = 0; i < _varLim; i++)
			{
				double num = 0.0;
				int num2 = 0;
				CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(_mat, i);
				while (colIter.IsValid)
				{
					num2++;
					double num3 = Math.Abs(colIter.Approx);
					if (num < num3)
					{
						num = num3;
					}
					colIter.Advance();
				}
				double num4 = num;
				if (num4 != 1.0 && num4 > 0.0)
				{
					_mat.ScaleColApprox(i, 1.0 / num4);
					_mpvarnumLowerDbl[i] *= num4;
					_mpvarnumUpperDbl[i] *= num4;
					_mpvarnumScaleDbl[i] *= num4;
				}
			}
		}

		/// <summary>
		/// Return the scale factor of var
		/// </summary>
		public Rational GetScale(int var)
		{
			return _mpvidnumScale[_mpvarvid[var]];
		}

		/// <summary>
		/// Return the extra double scale factor of var
		/// </summary>
		public double GetScaleDbl(int var)
		{
			return _mpvarnumScaleDbl[var];
		}

		/// <summary>
		/// Return the shifting delta of var
		/// </summary>
		public Rational GetDelta(int var)
		{
			return _mpvidnumDelta[_mpvarvid[var]];
		}

		/// <summary>
		/// This maps FROM a value for "var" TO a value for the
		/// corresponding "vid".
		/// </summary>
		public Rational MapValueFromVarToVid(int var, Rational num)
		{
			return num * _mpvidnumScale[_mpvarvid[var]] + _mpvidnumDelta[_mpvarvid[var]];
		}

		/// <summary>
		/// This maps FROM a value for "var" TO a value for the
		/// corresponding "vid".
		/// </summary>
		public double MapValueFromVarToVid(int var, double num)
		{
			return (double)(num / _mpvarnumScaleDbl[var] * _mpvidnumScale[_mpvarvid[var]] + _mpvidnumDelta[_mpvarvid[var]]);
		}

		/// <summary>
		/// This maps TO a value for "var" FROM a value for the
		/// corresponding "vid".
		/// </summary>
		public Rational MapValueFromVidToVar(int var, Rational num)
		{
			return (num - _mpvidnumDelta[_mpvarvid[var]]) / _mpvidnumScale[_mpvarvid[var]];
		}

		/// <summary>
		/// The double side is scaled differently than the exact side.
		/// This maps a value from the exact side to the double side.
		/// </summary>
		public double MapValueFromExactToDouble(int var, Rational num)
		{
			return (double)num * _mpvarnumScaleDbl[var];
		}

		/// <summary>
		/// The double side is scaled differently than the exact side.
		/// This maps a value from the double side to the exact side.
		/// </summary>
		public Rational MapValueFromDoubleToExact(int var, double num)
		{
			return num / _mpvarnumScaleDbl[var];
		}

		public virtual void MapVarValues(SimplexTask thd, AlgorithmRational agr, Rational[] mpvidnum)
		{
			SimplexBasis basis = agr.Basis;
			Array.Clear(mpvidnum, 0, _solver._vidLim);
			for (int i = 0; i < _rowLim; i++)
			{
				int basicVar = basis.GetBasicVar(i);
				int num = _mpvarvid[basicVar];
				ref Rational reference = ref mpvidnum[num];
				reference = MapValueFromVarToVid(basicVar, agr.GetBasicValue(i));
			}
			int num2 = _varLim;
			while (--num2 >= 0)
			{
				int num3 = _mpvarvid[num2];
				SimplexVarValKind vvk = basis.GetVvk(num2);
				if (vvk != 0)
				{
					ref Rational reference2 = ref mpvidnum[num3];
					reference2 = MapValueFromVarToVid(num2, agr.GetVarBound(num2, vvk));
				}
			}
			if (_varLim >= _vidLim)
			{
				return;
			}
			int num4 = _vidLim;
			while (--num4 >= 0)
			{
				if (_mpvidvar[num4] < 0)
				{
					ref Rational reference3 = ref mpvidnum[num4];
					reference3 = _mpvidnumDelta[num4];
				}
			}
		}

		public virtual void MapVarValues(SimplexTask thd, AlgorithmDouble agd, Rational[] mpvidnum)
		{
			SimplexBasis basis = agd.Basis;
			Array.Clear(mpvidnum, 0, _solver._vidLim);
			for (int i = 0; i < thd.Model.RowLim; i++)
			{
				int basicVar = basis.GetBasicVar(i);
				if (basicVar < _varLim)
				{
					int num = _mpvarvid[basicVar];
					Rational num2 = MapValueFromDoubleToExact(basicVar, agd.GetBasicValue(i));
					ref Rational reference = ref mpvidnum[num];
					reference = MapValueFromVarToVid(basicVar, num2);
				}
			}
			int num3 = _varLim;
			while (--num3 >= 0)
			{
				int num4 = _mpvarvid[num3];
				SimplexVarValKind vvk = basis.GetVvk(num3);
				if (vvk != 0)
				{
					Rational num5 = thd.BoundManager.GetVarBound(num3, vvk);
					if (!num5.IsFinite)
					{
						num5 = MapValueFromDoubleToExact(num3, agd.GetVarBound(num3, vvk));
					}
					ref Rational reference2 = ref mpvidnum[num4];
					reference2 = MapValueFromVarToVid(num3, num5);
				}
			}
			if (_varLim >= _vidLim)
			{
				return;
			}
			int num6 = _vidLim;
			while (--num6 >= 0)
			{
				if (_mpvidvar[num6] < 0)
				{
					ref Rational reference3 = ref mpvidnum[num6];
					reference3 = _mpvidnumDelta[num6];
				}
			}
		}

		protected void AppendRow(int rid)
		{
			CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_solver._matModel, rid);
			while (rowIter.IsValid)
			{
				_mat.SetCoefExact(_rowLim, rowIter.Column, rowIter.Exact);
				rowIter.Advance();
			}
			if (_mat.RowEntryCount(_rowLim) > 0)
			{
				_mprowrid[_rowLim] = rid;
				_mpridrow[rid] = _rowLim;
				_mprowvidSlack[_rowLim] = _solver._mpridvid[rid];
				_rowLim++;
			}
		}

		/// <summary>  setup the initial shadow price table
		/// </summary>
		/// <param name="rid">a row id</param>
		private void SetupRowShadowPrice(int rid)
		{
			int num = _solver._mpridvid[rid];
			if (!_solver.IsGoal(num))
			{
				if (_mpvarnumLower[num].IsNegativeInfinity && _mpvarnumUpper[num].IsPositiveInfinity)
				{
					ref Rational reference = ref _rowLowerShadowPrice[rid];
					reference = 0;
					ref Rational reference2 = ref _rowUpperShadowPrice[rid];
					reference2 = 0;
				}
				else if (_mpvarnumLower[num] == _mpvarnumUpper[num])
				{
					ref Rational reference3 = ref _rowLowerShadowPrice[rid];
					reference3 = Rational.NegativeInfinity;
					ref Rational reference4 = ref _rowUpperShadowPrice[rid];
					reference4 = Rational.PositiveInfinity;
				}
				else if (_mpvarnumUpper[num].IsPositiveInfinity)
				{
					ref Rational reference5 = ref _rowLowerShadowPrice[rid];
					reference5 = Rational.NegativeInfinity;
					ref Rational reference6 = ref _rowUpperShadowPrice[rid];
					reference6 = 0;
				}
				else if (_mpvarnumLower[num].IsNegativeInfinity)
				{
					ref Rational reference7 = ref _rowLowerShadowPrice[rid];
					reference7 = 0;
					ref Rational reference8 = ref _rowUpperShadowPrice[rid];
					reference8 = Rational.PositiveInfinity;
				}
			}
		}

		private void FixingVariablesToBounds(int var)
		{
			Rational rational = 0;
			Rational rational2 = 0;
			int goalRow = GetGoalRow(0);
			CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(_mat, var);
			while (colIter.IsValid)
			{
				if (!IsGoal(colIter.Row))
				{
					if (colIter.Exact.Sign == 1)
					{
						rational2 += _rowUpperShadowPrice[colIter.Row] * colIter.Exact;
						rational += _rowLowerShadowPrice[colIter.Row] * colIter.Exact;
					}
					else
					{
						rational2 += _rowLowerShadowPrice[colIter.Row] * colIter.Exact;
						rational += _rowUpperShadowPrice[colIter.Row] * colIter.Exact;
					}
				}
				colIter.Advance();
			}
			Rational rational3 = MapValueFromVarToVid(var, _mat.GetCoefExact(goalRow, var));
			if (rational3 > rational2)
			{
				ref Rational reference = ref _mpvarnumLower[var];
				reference = _mpvarnumUpper[var];
			}
			else if (rational3 < rational)
			{
				ref Rational reference2 = ref _mpvarnumUpper[var];
				reference2 = _mpvarnumLower[var];
			}
		}

		private void CalculateShadowPrice(int var, CoefMatrix.ColIter cit)
		{
			int goalRow = GetGoalRow(0);
			Rational rational = MapValueFromVarToVid(var, _mat.GetCoefExact(goalRow, var));
			Rational rational2 = rational / cit.Exact;
			int row = cit.Row;
			if (cit.Exact.Sign == 1)
			{
				if (rational2 > _rowLowerShadowPrice[row])
				{
					_rowLowerShadowPrice[row] = rational2;
				}
				if (rational2 > _rowUpperShadowPrice[row])
				{
					ref Rational reference = ref _mpvarnumLower[var];
					reference = _mpvarnumUpper[var];
				}
			}
			else
			{
				if (rational2 < _rowUpperShadowPrice[row])
				{
					_rowUpperShadowPrice[row] = rational2;
				}
				if (rational2 < _rowLowerShadowPrice[row])
				{
					ref Rational reference2 = ref _mpvarnumUpper[var];
					reference2 = _mpvarnumLower[var];
				}
			}
		}

		/// <summary>
		/// Check redundant row or dominant constraint
		/// Paper ref: Analysis of math. programming problems prior to applying the Simplex algorithm
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		private bool CheckRedundantRowConstraint(int row)
		{
			bool result = false;
			Rational rational = 0;
			Rational rational2 = 0;
			int slackVarForRow = GetSlackVarForRow(row);
			CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_solver._matModel, row);
			while (rowIter.IsValid)
			{
				if (slackVarForRow != rowIter.Column)
				{
					if (rowIter.Exact.Sign == 1)
					{
						rational2 += _mpvarnumUpper[rowIter.Column] * rowIter.Exact;
						rational += _mpvarnumLower[rowIter.Column] * rowIter.Exact;
					}
					else
					{
						rational2 += _mpvarnumLower[rowIter.Column] * rowIter.Exact;
						rational += _mpvarnumUpper[rowIter.Column] * rowIter.Exact;
					}
				}
				rowIter.Advance();
			}
			if (_mpvarnumUpper[slackVarForRow] == _mpvarnumLower[slackVarForRow])
			{
				if (rational > _mpvarnumUpper[slackVarForRow] || rational2 < _mpvarnumLower[slackVarForRow])
				{
					return result;
				}
				if (rational == _mpvarnumUpper[slackVarForRow])
				{
					FixupRowVarsToLowerBound(row);
					result = true;
				}
				else if (rational2 == _mpvarnumLower[slackVarForRow])
				{
					FixupRowVarsToUpperBound(row);
					result = true;
				}
			}
			else if (_mpvarnumLower[slackVarForRow].IsNegativeInfinity)
			{
				if (rational2 <= _mpvarnumUpper[slackVarForRow])
				{
					result = true;
				}
				if (rational == _mpvarnumUpper[slackVarForRow])
				{
					FixupRowVarsToLowerBound(row);
				}
			}
			else if (_mpvarnumUpper[slackVarForRow].IsPositiveInfinity)
			{
				if (rational >= _mpvarnumLower[slackVarForRow])
				{
					result = true;
				}
				if (rational2 == _mpvarnumLower[slackVarForRow])
				{
					FixupRowVarsToUpperBound(row);
				}
			}
			return result;
		}

		private void FixupRowVarsToLowerBound(int row)
		{
			int num = _mprowvidSlack[row];
			CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_solver._matModel, row);
			while (rowIter.IsValid)
			{
				if (num != rowIter.Column)
				{
					if (rowIter.Exact.Sign == 1)
					{
						ref Rational reference = ref _mpvarnumUpper[rowIter.Column];
						reference = _mpvarnumLower[rowIter.Column];
					}
					else
					{
						ref Rational reference2 = ref _mpvarnumLower[rowIter.Column];
						reference2 = _mpvarnumUpper[rowIter.Column];
					}
				}
				rowIter.Advance();
			}
		}

		private void FixupRowVarsToUpperBound(int row)
		{
			int num = _mprowvidSlack[row];
			CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_solver._matModel, row);
			while (rowIter.IsValid)
			{
				if (num != rowIter.Column)
				{
					if (rowIter.Exact.Sign == 1)
					{
						ref Rational reference = ref _mpvarnumLower[rowIter.Column];
						reference = _mpvarnumUpper[rowIter.Column];
					}
					else
					{
						ref Rational reference2 = ref _mpvarnumUpper[rowIter.Column];
						reference2 = _mpvarnumLower[rowIter.Column];
					}
				}
				rowIter.Advance();
			}
		}

		private void InitVidToVarMapping()
		{
			_ridLim = _solver._ridLim;
			_vidLim = _solver._vidLim;
			_varLim = _vidLim;
			_mpvarvid = new int[_varLim];
			_mpvidvar = new int[_vidLim];
			_rgvidElim = new List<int>();
			_matElim = new CoefMatrix(0, _vidLim, 100, fExact: true, fDouble: false);
			_mpvarnumLower = new Rational[_varLim];
			_mpvarnumUpper = new Rational[_varLim];
			Array.Copy(_solver._mpvidnumLo, _mpvarnumLower, _vidLim);
			Array.Copy(_solver._mpvidnumHi, _mpvarnumUpper, _vidLim);
			_mpvidnumScale = new Rational[_vidLim];
			_mpvidnumDelta = new Rational[_vidLim];
			for (int i = 0; i < _vidLim; i++)
			{
				_mpvarvid[i] = i;
				_mpvidvar[i] = i;
				ref Rational reference = ref _mpvidnumScale[i];
				reference = Rational.One;
				if (_solver.HasFlag(i, RowVariableModel.VidFlags.IgnoreBounds))
				{
					ref Rational reference2 = ref _mpvarnumLower[i];
					reference2 = Rational.NegativeInfinity;
					ref Rational reference3 = ref _mpvarnumUpper[i];
					reference3 = Rational.PositiveInfinity;
				}
				else if (!_solver._fRelax && _solver.HasFlag(i, RowVariableModel.VidFlags.Integer))
				{
					ref Rational reference4 = ref _mpvarnumLower[i];
					reference4 = _mpvarnumLower[i].GetCeiling();
					ref Rational reference5 = ref _mpvarnumUpper[i];
					reference5 = _mpvarnumUpper[i].GetFloor();
				}
			}
		}

		public virtual bool PreSolve(SimplexSolverParams[] parameters)
		{
			foreach (SimplexSolverParams simplexSolverParams in parameters)
			{
				if (simplexSolverParams.PresolveLevel > _presolveLevel)
				{
					_presolveLevel = simplexSolverParams.PresolveLevel;
				}
				if (!simplexSolverParams.UseExact)
				{
					_approximateSolve = true;
				}
			}
			if (_presolveLevel == 0)
			{
				return true;
			}
			EliminateSingletonRows();
			PresolveStage1();
			if (_presolveLevel >= 1 && _approximateSolve)
			{
				PresolveStage2();
			}
			return EliminateEmptyRowsAndColumns((int vid) => _mpvidvar[vid] < 0);
		}

		private void CleanupWorkspace()
		{
			_rowLowerShadowPrice = null;
			_rowUpperShadowPrice = null;
		}

		private void PresolveStage1()
		{
			while (EliminateFixedVariables() != 0 && EliminateSingletonRows() != 0)
			{
			}
		}

		private void InitWorkspace()
		{
			_rowLowerShadowPrice = new Rational[_ridLim];
			_rowUpperShadowPrice = new Rational[_ridLim];
			for (int i = 0; i < _ridLim; i++)
			{
				SetupRowShadowPrice(i);
			}
		}

		private void PresolveStage2()
		{
			InitWorkspace();
			bool flag = false;
			while (!flag)
			{
				flag = true;
				for (int i = 0; i < _rowLim; i++)
				{
					if (!IsRowEliminated(i) && !IsGoal(i))
					{
						if (CheckRedundantRowConstraint(i))
						{
							EliminateRow(i);
						}
						else if (!TightenVariableBound(i))
						{
							flag = false;
						}
					}
				}
				CheckDominatedColumns();
				EliminateFixedVariables();
			}
			CleanupWorkspace();
		}

		private bool TightenVariableBound(int row)
		{
			bool result = true;
			int slackVarForRow = GetSlackVarForRow(row);
			Rational rowLower = 0;
			Rational rowUpper = 0;
			ComputeRowBounds(row, slackVarForRow, ref rowLower, ref rowUpper);
			if (!rowLower.IsFinite && !rowUpper.IsFinite)
			{
				return result;
			}
			if (_mpvarnumUpper[slackVarForRow].IsPositiveInfinity)
			{
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_mat, row);
				while (rowIter.IsValid)
				{
					if (rowIter.Column != slackVarForRow && !(_mpvarnumUpper[rowIter.Column] == _mpvarnumLower[rowIter.Column]))
					{
						Rational exact = rowIter.Exact;
						if (rowUpper.IsFinite)
						{
							if (exact.Sign == 1)
							{
								Rational rational = _mpvarnumUpper[rowIter.Column] + (_mpvarnumLower[slackVarForRow] - rowUpper) / exact;
								if (rational > _mpvarnumLower[rowIter.Column])
								{
									DumpBound("c", row, rowIter.Column, _mpvarnumLower[rowIter.Column], rational, isUpper: false);
									UpdateBound(rowIter.Column, isUpper: false, rational);
									result = false;
								}
							}
							else if (exact.Sign == -1)
							{
								Rational rational2 = _mpvarnumUpper[rowIter.Column] + (_mpvarnumLower[slackVarForRow] - rowLower) / exact;
								if (rational2 < _mpvarnumUpper[rowIter.Column])
								{
									DumpBound("b", row, rowIter.Column, _mpvarnumUpper[rowIter.Column], rational2, isUpper: true);
									UpdateBound(rowIter.Column, isUpper: true, rational2);
									result = false;
								}
							}
						}
						else if (_mpvarnumUpper[rowIter.Column].IsPositiveInfinity && exact.Sign == 1)
						{
							Rational rational3 = (_mpvarnumLower[slackVarForRow] - ComputeRowBound(row, slackVarForRow, isUpper: true, rowIter.Column)) / exact;
							if (rational3 > _mpvarnumLower[rowIter.Column])
							{
								DumpBound("c-1", row, rowIter.Column, _mpvarnumLower[rowIter.Column], rational3, isUpper: false);
								UpdateBound(rowIter.Column, isUpper: false, rational3);
								result = false;
							}
						}
						else if (_mpvarnumLower[rowIter.Column].IsNegativeInfinity && exact.Sign == -1)
						{
							Rational rational4 = (_mpvarnumLower[slackVarForRow] - ComputeRowBound(row, slackVarForRow, isUpper: true, rowIter.Column)) / exact;
							if (rational4 < _mpvarnumUpper[rowIter.Column])
							{
								DumpBound("b-1", row, rowIter.Column, _mpvarnumUpper[rowIter.Column], rational4, isUpper: true);
								UpdateBound(rowIter.Column, isUpper: true, rational4);
								result = false;
							}
						}
					}
					rowIter.Advance();
				}
			}
			else if (_mpvarnumLower[slackVarForRow].IsNegativeInfinity)
			{
				CoefMatrix.RowIter rowIter2 = new CoefMatrix.RowIter(_mat, row);
				while (rowIter2.IsValid)
				{
					if (rowIter2.Column != slackVarForRow && !(_mpvarnumUpper[rowIter2.Column] == _mpvarnumLower[rowIter2.Column]))
					{
						Rational exact2 = rowIter2.Exact;
						if (rowLower.IsFinite)
						{
							if (exact2.Sign == 1)
							{
								Rational rational5 = _mpvarnumLower[rowIter2.Column] + (_mpvarnumUpper[slackVarForRow] - rowLower) / exact2;
								if (rational5 < _mpvarnumUpper[rowIter2.Column])
								{
									DumpBound("a", row, rowIter2.Column, _mpvarnumUpper[rowIter2.Column], rational5, isUpper: true);
									UpdateBound(rowIter2.Column, isUpper: true, rational5);
									result = false;
								}
							}
							else if (exact2.Sign == -1)
							{
								Rational rational6 = _mpvarnumUpper[rowIter2.Column] + (_mpvarnumUpper[slackVarForRow] - rowLower) / exact2;
								if (rational6 > _mpvarnumLower[rowIter2.Column])
								{
									DumpBound("d", row, rowIter2.Column, _mpvarnumLower[rowIter2.Column], rational6, isUpper: false);
									UpdateBound(rowIter2.Column, isUpper: false, rational6);
									result = false;
								}
							}
						}
						else if (_mpvarnumLower[rowIter2.Column].IsNegativeInfinity && exact2.Sign == 1)
						{
							Rational rational7 = (_mpvarnumUpper[slackVarForRow] - ComputeRowBound(row, slackVarForRow, isUpper: false, rowIter2.Column)) / exact2;
							if (rational7 < _mpvarnumUpper[rowIter2.Column])
							{
								DumpBound("a-1", row, rowIter2.Column, _mpvarnumUpper[rowIter2.Column], rational7, isUpper: true);
								UpdateBound(rowIter2.Column, isUpper: true, rational7);
								result = false;
							}
						}
						else if (_mpvarnumUpper[rowIter2.Column].IsPositiveInfinity && exact2.Sign == -1)
						{
							Rational rational8 = (_mpvarnumUpper[slackVarForRow] - ComputeRowBound(row, slackVarForRow, isUpper: false, rowIter2.Column)) / exact2;
							if (rational8 > _mpvarnumLower[rowIter2.Column])
							{
								DumpBound("d-1", row, rowIter2.Column, _mpvarnumLower[rowIter2.Column], rational8, isUpper: false);
								UpdateBound(rowIter2.Column, isUpper: false, rational8);
								result = false;
							}
						}
					}
					rowIter2.Advance();
				}
			}
			return result;
		}

		private void CheckDominatedColumns()
		{
			int num = _varLim;
			while (--num >= 0)
			{
				if (_mat.ColEntryCount(num) == 0)
				{
					continue;
				}
				CoefMatrix.ColIter cit = new CoefMatrix.ColIter(_mat, num);
				if (_mpvidvar[_mprowvidSlack[cit.Row]] != num)
				{
					if (_mat.ColEntryCount(num) == 2)
					{
						CalculateShadowPrice(num, cit);
					}
					FixingVariablesToBounds(num);
				}
			}
		}

		private void UpdateBound(int var, bool isUpper, Rational value)
		{
			if (isUpper)
			{
				_mpvarnumUpper[var] = value;
			}
			else
			{
				_mpvarnumLower[var] = value;
			}
			if ((double)(_mpvarnumUpper[var] - _mpvarnumLower[var]) < 1E-12)
			{
				ref Rational reference = ref _mpvarnumUpper[var];
				reference = _mpvarnumLower[var];
			}
		}

		private void ComputeRowBounds(int row, int rowSlackVar, ref Rational rowLower, ref Rational rowUpper)
		{
			rowLower = 0;
			rowUpper = 0;
			CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_solver._matModel, row);
			while (rowIter.IsValid)
			{
				if (rowSlackVar != rowIter.Column)
				{
					if (rowIter.Exact.Sign == 1)
					{
						rowUpper += _mpvarnumUpper[rowIter.Column] * rowIter.Exact;
						rowLower += _mpvarnumLower[rowIter.Column] * rowIter.Exact;
					}
					else
					{
						rowUpper += _mpvarnumLower[rowIter.Column] * rowIter.Exact;
						rowLower += _mpvarnumUpper[rowIter.Column] * rowIter.Exact;
					}
				}
				rowIter.Advance();
			}
		}

		private Rational ComputeRowBound(int row, int rowSlackVar, bool isUpper, int skipCol)
		{
			Rational result = 0;
			CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_solver._matModel, row);
			while (rowIter.IsValid)
			{
				if (rowSlackVar != rowIter.Column && rowIter.Column != skipCol)
				{
					if (isUpper)
					{
						if (rowIter.Exact.Sign == 1)
						{
							result += _mpvarnumUpper[rowIter.Column] * rowIter.Exact;
						}
						else
						{
							result += _mpvarnumLower[rowIter.Column] * rowIter.Exact;
						}
					}
					else if (rowIter.Exact.Sign == 1)
					{
						result += _mpvarnumLower[rowIter.Column] * rowIter.Exact;
					}
					else
					{
						result += _mpvarnumUpper[rowIter.Column] * rowIter.Exact;
					}
				}
				rowIter.Advance();
			}
			return result;
		}

		internal void InitRhs()
		{
			_vecRhs = new VectorRational(_rowLim);
		}

		private void EliminateVid(int vid, CoefMatrix mat, int row, bool fMap)
		{
			_rgvidElim.Add(vid);
			_matElim.ResizeRows(_rgvidElim.Count);
			int row2 = _rgvidElim.Count - 1;
			if (fMap)
			{
				Rational rational = 0;
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(mat, row);
				while (rowIter.IsValid)
				{
					int num = _mpvarvid[rowIter.Column];
					Rational rational2 = rowIter.Exact / _mpvidnumScale[num];
					Rational rational3 = _mpvidnumDelta[num];
					if (!rational3.IsZero)
					{
						rational += rational3 * rational2;
					}
					_matElim.SetCoefExact(row2, num, rational2);
					rowIter.Advance();
				}
				ref Rational reference = ref _mpvidnumScale[vid];
				reference = 0;
				_mpvidnumDelta[vid] = rational;
			}
			else
			{
				CoefMatrix.RowIter rowIter2 = new CoefMatrix.RowIter(mat, row);
				while (rowIter2.IsValid)
				{
					_matElim.SetCoefExact(row2, rowIter2.Column, rowIter2.Exact);
					rowIter2.Advance();
				}
				ref Rational reference2 = ref _mpvidnumScale[vid];
				reference2 = 0;
				ref Rational reference3 = ref _mpvidnumDelta[vid];
				reference3 = 0;
			}
		}

		private int EliminateSingletonRows()
		{
			int num = 0;
			for (int i = 0; i < _rowLim; i++)
			{
				int num2 = _mat.RowEntryCount(i);
				if (num2 != 1 && num2 != 2)
				{
					continue;
				}
				switch (num2)
				{
				case 1:
				{
					int column3 = new CoefMatrix.RowIter(_mat, i).Column;
					TrimVariableRange(column3, 0, 0);
					int num4 = _mpvarvid[column3];
					_mat.ClearRow(i);
					_mprowvidSlack[i] = -1;
					num++;
					if (_mat.ColEntryCount(column3) > 0)
					{
						_mat.ClearColumn(column3);
					}
					if (!_solver.IsActiveGoal(num4))
					{
						ref Rational reference = ref _mpvidnumScale[num4];
						reference = 0;
					}
					break;
				}
				case 2:
				{
					CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_mat, i);
					int column = rowIter.Column;
					int vid = _mpvarvid[column];
					Rational exact = rowIter.Exact;
					rowIter.Advance();
					int column2 = rowIter.Column;
					int vid2 = _mpvarvid[column2];
					Rational exact2 = rowIter.Exact;
					if (_solver.HasFlag(vid2, RowVariableModel.VidFlags.Integer) || _solver.HasFlag(vid, RowVariableModel.VidFlags.Integer))
					{
						break;
					}
					int num3;
					int var;
					Rational rational;
					if (_mat.ColEntryCount(column2) > 1 || _solver.IsActiveGoal(vid2))
					{
						if (_mat.ColEntryCount(column) > 1 || _solver.IsActiveGoal(vid))
						{
							break;
						}
						num3 = column;
						var = column2;
						rational = -exact / exact2;
					}
					else
					{
						num3 = column2;
						var = column;
						rational = -exact2 / exact;
					}
					Rational a = rational * _mpvarnumLower[num3];
					Rational b = rational * _mpvarnumUpper[num3];
					if (rational.Sign < 0)
					{
						Statics.Swap(ref a, ref b);
					}
					TrimVariableRange(var, a, b);
					_mprowvidSlack[i] = _mpvarvid[num3];
					EliminateRow(i);
					num++;
					break;
				}
				}
			}
			return num;
		}

		internal void EliminateRow(int row)
		{
			EliminateVid(_mprowvidSlack[row], _mat, row, fMap: true);
			_mat.ClearRow(row);
			_mprowvidSlack[row] = -1;
		}

		private int EliminateFixedVariables()
		{
			int num = 0;
			int num2 = _varLim;
			while (--num2 >= 0)
			{
				if (_mat.ColEntryCount(num2) == 0)
				{
					continue;
				}
				Rational rational = _mpvarnumLower[num2];
				Rational rational2 = _mpvarnumUpper[num2];
				if (!(rational == rational2))
				{
					continue;
				}
				CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(_mat, num2);
				if (_mat.ColEntryCount(num2) == 1 && _mpvidvar[_mprowvidSlack[colIter.Row]] == num2)
				{
					CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_mat, colIter.Row);
					while (rowIter.Column == num2 || _mat.ColEntryCount(rowIter.Column) != 1)
					{
						rowIter.Advance();
						if (rowIter.IsValid)
						{
							continue;
						}
						goto IL_0234;
					}
					_mprowvidSlack[colIter.Row] = _mpvarvid[rowIter.Column];
				}
				do
				{
					int num3 = _mprowvidSlack[colIter.Row];
					int num4 = _mpvidvar[num3];
					if (!rational.IsZero)
					{
						Rational rational3 = colIter.Exact * rational / _mat.GetCoefExact(colIter.Row, num4);
						_mpvarnumLower[num4] += rational3;
						_mpvarnumUpper[num4] += rational3;
						_mpvidnumDelta[num3] -= rational3 * _mpvidnumScale[num3];
					}
					colIter.Advance();
				}
				while (colIter.IsValid);
				int num5 = _mpvarvid[num2];
				_mpvidnumDelta[num5] += _mpvidnumScale[num5] * rational;
				_mat.ClearColumn(num2);
				num++;
				if (!_solver.IsActiveGoal(num5))
				{
					ref Rational reference = ref _mpvidnumScale[num5];
					reference = 0;
				}
				IL_0234:;
			}
			return num;
		}

		private bool EliminateEmptyRowsAndColumns(Func<int, bool> fn)
		{
			int num = 0;
			for (int i = 0; i < _rowLim; i++)
			{
				int num2 = _mprowrid[i];
				if (_mat.RowEntryCount(i) == 0)
				{
					_mpridrow[num2] = -1;
					continue;
				}
				if (num < i)
				{
					_mprowrid[num] = num2;
					_mpridrow[num2] = num;
					_mprowvidSlack[num] = _mprowvidSlack[i];
				}
				num++;
			}
			if (num < _rowLim)
			{
				_mat.RemoveEmptyRows();
				_rowLim = num;
			}
			_cvarSlack = 0;
			_cvarInt = 0;
			bool flag = false;
			int num3 = 0;
			for (int j = 0; j < _varLim; j++)
			{
				if (_mpvarnumLower[j] > _mpvarnumUpper[j])
				{
					flag = true;
					ref Rational reference = ref _mpvarnumLower[j];
					reference = _mpvarnumUpper[j];
				}
				int num4 = _mpvarvid[j];
				if (_mat.ColEntryCount(j) == 0 && !_solver.IsActiveGoal(num4))
				{
					_mpvidvar[num4] = -1;
					if (!(_mpvidnumScale[num4] != 0))
					{
						continue;
					}
					Rational rational;
					Rational rational2 = (rational = _mpvarnumLower[j]);
					if (!rational2.IsFinite)
					{
						Rational rational3 = (rational = _mpvarnumUpper[j]);
						if (!rational3.IsFinite)
						{
							rational = default(Rational);
						}
					}
					_mpvidnumDelta[num4] += rational * _mpvidnumScale[num4];
					ref Rational reference2 = ref _mpvidnumScale[num4];
					reference2 = 0;
					continue;
				}
				if (_solver._mpvidvi[num4].IsRow && GetRow(num4) != -1)
				{
					_cvarSlack++;
				}
				if (_solver.HasFlag(num4, RowVariableModel.VidFlags.Integer))
				{
					_cvarInt++;
				}
				if (num3 < j)
				{
					ref Rational reference3 = ref _mpvarnumLower[num3];
					reference3 = _mpvarnumLower[j];
					ref Rational reference4 = ref _mpvarnumUpper[num3];
					reference4 = _mpvarnumUpper[j];
					_mpvarvid[num3] = num4;
					_mpvidvar[num4] = num3;
				}
				num3++;
			}
			if (num3 < _varLim)
			{
				_mat.RemoveEmptyColumns(fn);
				_varLim = num3;
			}
			return !flag;
		}

		internal bool TrimVariableRange(int var, Rational numLo, Rational numHi)
		{
			if (_mpvarnumUpper[var] > numHi)
			{
				_mpvarnumUpper[var] = numHi;
			}
			if (_mpvarnumLower[var] < numLo)
			{
				_mpvarnumLower[var] = numLo;
			}
			return _mpvarnumLower[var] <= _mpvarnumUpper[var];
		}

		/// <summary>
		/// Marks a row that has had coefficients changed during presolve.
		/// </summary>
		public void MarkAsModifiedRow(int row)
		{
		}

		/// <summary>
		/// Marks that a column has been merged with another.
		/// </summary>
		/// <param name="original">The column merged into.</param>
		/// <param name="merged">The merged column.</param>
		public void MarkAsMergedVid(int original, int merged)
		{
		}

		public virtual void ComputeEliminatedVidValues(Rational[] mpvidnum)
		{
			if (_rgvidElim.Count == 0)
			{
				return;
			}
			if (_lufElim == null)
			{
				_lufElim = new LUFactorizationRational();
				int[] array = _rgvidElim.ToArray();
				_lufElim.Factor(_matElim, array.Length, array);
			}
			Rational[] array2 = new Rational[_rgvidElim.Count];
			for (int i = 0; i < _rgvidElim.Count; i++)
			{
				Rational rational = Rational.Zero;
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_matElim, i);
				while (rowIter.IsValid)
				{
					Rational ratMul = mpvidnum[rowIter.Column];
					if (!ratMul.IsZero)
					{
						rational = Rational.AddMul(rational, ratMul, rowIter.Exact);
					}
					rowIter.Advance();
				}
				ref Rational reference = ref array2[i];
				reference = mpvidnum[_rgvidElim[i]] - rational;
			}
			_lufElim.SolveCol(array2);
			for (int j = 0; j < _rgvidElim.Count; j++)
			{
				mpvidnum[_rgvidElim[j]] += array2[j];
			}
		}

		public virtual void SetBasicFlagsOnSolver(SimplexBasis bas)
		{
			int num = _vidLim;
			while (--num >= 0)
			{
				int num2 = _mpvidvar[num];
				if (num2 >= 0 && bas.GetBasisSlot(num2) >= 0)
				{
					_solver.SetFlag(num, RowVariableModel.VidFlags.Basic);
				}
				else
				{
					_solver.ClearFlag(num, RowVariableModel.VidFlags.Basic);
				}
			}
			int num3 = _rgvidElim.Count;
			while (--num3 >= 0)
			{
				_solver.SetFlag(_rgvidElim[num3], RowVariableModel.VidFlags.Basic);
			}
		}

		/// <summary>
		/// Sets a variable upper bound.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="bound"></param>
		internal void SetVariableUpperBound(int variable, Rational bound)
		{
			_mpvarnumUpper[variable] = bound;
		}

		/// <summary>
		/// Sets a variable lower bound.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="bound"></param>
		internal void SetVariableLowerBound(int variable, Rational bound)
		{
			_mpvarnumLower[variable] = bound;
		}

		/// <summary>
		/// Gets the bounds of a row.
		/// </summary>
		/// <param name="row">The row whose bounds are sought.</param>
		/// <param name="lowerBound">The lower bound of the row.</param>
		/// <param name="upperBound">The upper bound of the row.</param>
		internal void GetRowBounds(int row, out Rational lowerBound, out Rational upperBound)
		{
			int slackVarForRow = GetSlackVarForRow(row);
			Rational rational = -_mat.GetCoefExact(row, slackVarForRow);
			if (rational.IsOne)
			{
				lowerBound = _mpvarnumLower[slackVarForRow];
				upperBound = _mpvarnumUpper[slackVarForRow];
				return;
			}
			if (rational.Sign < 0)
			{
				upperBound = _mpvarnumLower[slackVarForRow];
				lowerBound = _mpvarnumUpper[slackVarForRow];
			}
			else
			{
				lowerBound = _mpvarnumLower[slackVarForRow];
				upperBound = _mpvarnumUpper[slackVarForRow];
			}
			lowerBound *= rational;
			upperBound *= rational;
		}

		/// <summary>
		/// Sets the bounds of a row.
		/// </summary>
		/// <param name="row">The row whose bounds are set.</param>
		/// <param name="lowerBound">The lower bound of the row.</param>
		/// <param name="upperBound">The upper bound of the row.</param>
		internal void SetRowBounds(int row, Rational lowerBound, Rational upperBound)
		{
			int slackVarForRow = GetSlackVarForRow(row);
			Rational rational = -_mat.GetCoefExact(row, slackVarForRow);
			if (rational.IsOne)
			{
				_mpvarnumLower[slackVarForRow] = lowerBound;
				_mpvarnumUpper[slackVarForRow] = upperBound;
				return;
			}
			lowerBound /= rational;
			upperBound /= rational;
			if (rational.Sign < 0)
			{
				_mpvarnumLower[slackVarForRow] = upperBound;
				_mpvarnumUpper[slackVarForRow] = lowerBound;
			}
			else
			{
				_mpvarnumLower[slackVarForRow] = lowerBound;
				_mpvarnumUpper[slackVarForRow] = upperBound;
			}
		}

		/// <summary>
		/// Compute a row in the reduced model where vid is the user vid of the basic variable in that row
		/// </summary>
		/// <param name="vid"></param>
		/// <param name="bas"></param>
		/// <returns></returns>
		internal VectorRational ComputeTableauRow(int vid, SimplexFactoredBasis bas)
		{
			if (_TableauRowReduced == null || _TableauRowReduced.RcCount < VarLim)
			{
				_TableauRowReduced = new VectorRational(VarLim);
				_vecE = new VectorRational(RowLim);
			}
			else
			{
				_TableauRowReduced.Clear();
				_vecE.Clear();
			}
			int num = _mpvidvar[vid];
			int num2 = RowLim;
			while (--num2 >= 0)
			{
				if (num == bas.GetBasicVar(num2))
				{
					_vecE.SetCoefNonZero(num2, 1);
				}
			}
			bas.InplaceSolveRow(_vecE);
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_vecE);
			while (iter.IsValid)
			{
				Rational value = iter.Value;
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(Matrix, iter.Rc);
				while (rowIter.IsValid)
				{
					int column = rowIter.Column;
					Rational num3 = Rational.AddMul(_TableauRowReduced.GetCoef(column), value, rowIter.Exact);
					if (num3.IsZero)
					{
						_TableauRowReduced.RemoveCoef(column);
					}
					else
					{
						_TableauRowReduced.SetCoefNonZero(column, num3);
					}
					rowIter.Advance();
				}
				iter.Advance();
			}
			return _TableauRowReduced;
		}

		[Conditional("DEBUG")]
		internal static void DumpRow(SimplexReducedModel model, int row)
		{
		}

		private void DumpBound(string choice, int row, int var, Rational oldVal, Rational newVal, bool isUpper)
		{
		}
	}
}
