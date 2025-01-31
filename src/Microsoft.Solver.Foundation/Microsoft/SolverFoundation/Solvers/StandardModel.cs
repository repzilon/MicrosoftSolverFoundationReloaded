using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> The standard model is
	///           Minimize &lt;c,x&gt; + x*Qx/2 + F(x)
	///           Ax = b
	///           x &gt;= 0
	/// </summary>
	internal abstract class StandardModel : InteriorPointReducedModel
	{
		/// <summary> Mapping types for rows and vars
		/// </summary>
		protected enum VidToVarMapKind
		{
			Goal,
			/// <summary> Goal is less than RowConstant
			/// </summary>
			RowConstant,
			RowLower,
			RowUpper,
			RowBounded,
			RowUnbounded,
			/// <summary> All row kinds are less than VarConstant
			/// </summary>
			VarConstant,
			VarLower,
			VarUpper,
			VarBounded,
			VarUnbounded,
			VarSlack
		}

		/// <summary> This is the key to understanding how user vids map to standard vars
		/// </summary>
		/// <remarks> Map from _solver (user-model) vid to standard model var, or reverse:
		///    Condition	      Forward map	          Reverse map
		///    0 ≤ v ≤ ∞	      v =&gt; x	              x =&gt; v
		///    l ≤ v ≤ ∞	      v - l =&gt; x	          x + l =&gt; v
		///    -∞ ≤ v ≤ u	      -v + u =&gt; x	          u - x =&gt; v
		///    l ≤ v ≤ u   	    v - l =&gt; xl + l,
		///                     xl - xu = 0	          xl + l =&gt; v
		///    ∞ ≤ v ≤ ∞	      Not yet implemented.	
		///    l = u	          v =&gt; l	              l =&gt; v
		/// </remarks>
		protected struct VidToVarMap
		{
			internal Rational lower;

			internal Rational upper;

			/// <summary> MinValue =&gt; eliminated, non-negative =&gt; standard var
			/// </summary>
			internal int iVar;

			/// <summary> MinValue =&gt; none, negative =&gt; row, otherwise mirror var
			/// </summary>
			internal int mirror;

			/// <summary> goal &lt; row kinds &lt; var kinds
			/// </summary>
			internal VidToVarMapKind kind;

			public override string ToString()
			{
				StringBuilder stringBuilder = new StringBuilder(iVar.ToString(CultureInfo.InvariantCulture));
				if (int.MinValue < mirror)
				{
					stringBuilder.Append(", mirror ").Append(mirror.ToString(CultureInfo.InvariantCulture));
				}
				stringBuilder.Append(": ");
				stringBuilder.Append(kind.ToString());
				stringBuilder.Append(" [");
				stringBuilder.Append(((double)lower).ToString(CultureInfo.InvariantCulture));
				stringBuilder.Append(" .. ");
				stringBuilder.Append(((double)upper).ToString(CultureInfo.InvariantCulture));
				stringBuilder.Append("] ");
				return stringBuilder.ToString();
			}
		}

		/// <summary> Threshold used for numerical stability.
		/// </summary>
		protected const double _epsilon = 1E-11;

		/// <summary> The user-model version of the Q matrix
		/// </summary>
		protected CoefMatrix _qpModel;

		/// <summary> A map from vid to an index for the _qpModel
		/// </summary>
		protected int[] _mpvidQid;

		/// <summary> The Q matrix may be compressed to just those variables
		///           with a quadratic component.  Map the indexes of the Q
		///           and qpModel to the Vids of the user model.
		/// </summary>
		protected int[] _mapQidToVid;

		/// <summary> The count of additional rows invented to support bound mirrors
		/// </summary>
		private int _mirrorRowCount;

		private int _rowCount;

		/// <summary> Fixed or unbounded slack variables which have been removed
		/// </summary>
		private int _ignoredUserVarCount;

		/// <summary> The count of slack variables invented to convert rows to equality
		///           and to mirror dual-bounded rows or variables
		/// </summary>
		private int _slackVarCount;

		private int _varCount;

		protected BigSum _primal;

		protected BigSum _dual;

		protected bool _dumpTriplets;

		/// <summary> Map from user-model vid to standard model var and row.
		/// </summary>
		private VidToVarMap[] _vidToVars;

		/// <summary> Map from standard model var to user-model vid
		/// </summary>
		private int[] _varToVids;

		/// <summary> Map from standard model row to user-model vid
		/// </summary>
		private int[] _rowToVids;

		/// <summary> Map from user-model vid to standard model row 
		/// </summary>
		private int[] _vidToRows;

		/// <summary> During initialization we keep a list of all dual-bounded
		///           vars and rows so we can append their mirror rows.
		/// </summary>
		internal List<int> _inventions;

		/// <summary> Keep track of the user variables which are unbounded
		/// </summary>
		public List<int> _unboundedVids;

		/// <summary> The Standard constraint coefficients in Ax = b, exact and double
		/// </summary>
		protected SparseMatrixDouble _A;

		/// <summary> The exact RHS values in Ax = b
		/// </summary>
		protected VectorRational _bExact;

		/// <summary> The double RHS values in Ax = b
		/// </summary>
		protected double[] _b;

		/// <summary> the exact linear portion of the goal Minimize&lt;c,x&gt;
		/// </summary>
		protected VectorRational _cExact;

		/// <summary> the double linear portion of the goal Minimize&lt;c,x&gt;
		/// </summary>
		protected double[] _c;

		/// <summary> The cost function may have a constant component.
		/// </summary>
		protected BigSum _constantCost;

		/// <summary> the quadratic portion of the minimization goal
		/// </summary>
		protected SymmetricSparseMatrix _Q;

		/// <summary> If Q is present and not diagonal then we cannot use
		///           Normal form to solve the Reduced KKT.
		/// </summary>
		protected bool _QnotDiagonal;

		/// <summary> The primal variables
		/// </summary>
		protected double[] _x;

		/// <summary> The dual variables in A*y + z = c
		/// </summary>
		protected double[] _y;

		/// <summary> The dual variables as a Vector.
		/// </summary>
		private Vector _yVec;

		/// <summary> The dual slacks
		/// </summary>
		protected double[] _z;

		/// <summary> standard model number of rows
		/// </summary>
		public override int RowCount => _rowCount;

		/// <summary> Count of goals //REVIEW: tanjb: IPM supports == 1
		/// </summary>
		public int GoalCount { get; private set; }

		/// <summary> True means that the model has a pre-solved solution.
		/// </summary>
		public bool DirectSolution { get; protected set; }

		/// <summary> Standard Model total number of variables, user and slack.
		/// </summary>
		public override int VarCount => _varCount;

		/// <summary> The primal version of the objective
		/// </summary>
		public override double Primal => (double)_goalDirection * _primal.ToDouble();

		/// <summary> The dual version of the objective
		/// </summary>
		public override double Dual => (double)_goalDirection * _dual.ToDouble();

		/// <summary> Did we detect an unbounded objective?
		/// </summary>
		public bool GoalIsUnbounded { get; protected set; }

		internal SparseMatrixDouble Matrix => _A;

		internal VectorRational GetRhs()
		{
			return _bExact;
		}

		internal int GetVid(int iVar)
		{
			return _varToVids[iVar];
		}

		internal int GetVar(int vid)
		{
			return _vidToVars[vid].iVar;
		}

		/// <summary> Characterize the standard =&gt; user map,  s·x + t =&gt; v
		/// </summary>
		private void ReverseMapParameters(int vid, out double s, out double t)
		{
			VidToVarMap vidToVarMap = _vidToVars[vid];
			switch (vidToVarMap.kind)
			{
			case VidToVarMapKind.VarConstant:
				s = 0.0;
				t = (double)vidToVarMap.lower;
				break;
			case VidToVarMapKind.VarLower:
			case VidToVarMapKind.VarBounded:
			case VidToVarMapKind.VarSlack:
				s = 1.0;
				t = (double)vidToVarMap.lower;
				break;
			case VidToVarMapKind.VarUpper:
				s = -1.0;
				t = (double)vidToVarMap.upper;
				break;
			case VidToVarMapKind.VarUnbounded:
				s = 1.0;
				t = 0.0;
				break;
			default:
				throw new ArgumentException(Resources.QpModelShouldNotReferenceNonVariables);
			}
		}

		private static double[] Clone(VectorRational vR)
		{
			double[] array = new double[vR.RcCount];
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(vR);
			while (iter.IsValid)
			{
				array[iter.Rc] = (double)iter.Value;
				iter.Advance();
			}
			return array;
		}

		/// <summary> Creates a new instance.
		/// Loads our internal representation from the user specified representation.
		/// This only eliminates disabled rows, nothing more.
		/// </summary>
		/// <param name="model">The object containing the user model.</param>
		/// <param name="logger">The LogSource.</param>
		/// <param name="qpModel">A compact matrix of quadratic coeffs indexed by Qid.</param>
		/// <param name="mpvidQid">vid to Qid, zero means not used in the quadratic.</param>
		/// <param name="presolveLevel">Presolve level.</param>
		internal StandardModel(LinearModel model, LogSource logger, CoefMatrix qpModel, int[] mpvidQid, int presolveLevel)
			: base(model, logger)
		{
			_qpModel = qpModel;
			_mpvidQid = mpvidQid;
			if (_qpModel != null && _qpModel.EntryCount == 0)
			{
				_qpModel = null;
				_mpvidQid = null;
			}
			_solution = new StandardSolution();
			base.Solution.status = LinearResult.Invalid;
			GoalCount = model.GoalCount;
			DetermineGoal();
			int colCount = model.ColCount;
			if (colCount == 0)
			{
				throw new ArgumentException(Resources.InteriorPointCannotLoadAModelWithNoVariables);
			}
			_vidToVars = new VidToVarMap[colCount];
			_unboundedVids = new List<int>();
			if (presolveLevel != 0)
			{
				Presolve(presolveLevel);
			}
			if (base.Solution.status != 0)
			{
				DirectSolution = true;
				return;
			}
			foreach (KeyValuePair<int, bool> item in removedRowsByVid)
			{
				_vidToVars[item.Key].kind = VidToVarMapKind.RowUnbounded;
				_vidToVars[item.Key].iVar = int.MinValue;
			}
			MakeReductions(mpvidQid);
			if (_varCount == 0 || GoalIsUnbounded)
			{
				base.Solution.status = (GoalIsUnbounded ? LinearResult.UnboundedPrimal : LinearResult.Optimal);
				DirectSolution = true;
				return;
			}
			_bExact = new VectorRational(RowCount, RowCount);
			_cExact = new VectorRational(VarCount);
			_varToVids = new int[VarCount];
			_rowToVids = new int[RowCount];
			_inventions = new List<int>(_mirrorRowCount);
			FillMaps();
			NullPermute();
			CopyInitialMatrix();
			_b = Clone(_bExact);
			_c = Clone(_cExact);
			if (_A == null && _Q == null)
			{
				base.Solution.status = LinearResult.Optimal;
				_x = new double[VarCount];
				using (IEnumerator<ILinearGoal> enumerator2 = model.Goals.GetEnumerator())
				{
					if (enumerator2.MoveNext())
					{
						_ = enumerator2.Current;
						Rational rational = 0;
						int rowIndexFromVid = model.GetRowIndexFromVid(base.GoalVid);
						foreach (LinearEntry rowValue in model.GetRowValues(rowIndexFromVid))
						{
							Rational value = rowValue.Value;
							int num = value.Sign * _goalDirection;
							model.GetBounds(rowValue.Index, out var lower, out var upper);
							if ((num < 0 && upper.IsPositiveInfinity) || (0 < num && lower.IsNegativeInfinity))
							{
								base.Solution.status = LinearResult.UnboundedPrimal;
								break;
							}
							Rational rational2 = ((num < 0) ? upper : lower).ToDouble();
							rational += rational2 * rowValue.Value;
						}
						model._mpvidnum[base.GoalVid] = rational;
						base.Solution.cx = (base.Solution.by = rational.ToDouble());
						_primal = (_dual = base.Solution.cx);
					}
				}
				DirectSolution = true;
			}
			else if (_Q != null && QisTriviallyNotConvex())
			{
				base.Logger.LogEvent(16, Resources.ModelNotConvex);
				throw new InvalidModelDataException(Resources.ModelNotConvex);
			}
		}

		/// <summary> Predict the initial size necessary for the SparseMatrix
		/// </summary>
		/// <param name="mpvidQid"> the map from vid to indexes on the compressed qpMatrix </param>
		private void MakeReductions(int[] mpvidQid)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int colCount = model.ColCount;
			if (colCount == 0)
			{
				throw new EmptyModelException(Resources.InteriorPointAfterReductionThisModelContainsNoVariables);
			}
			HashSet<int> hashSet = new HashSet<int>();
			for (int i = 0; i < colCount; i++)
			{
				GetUserBounds(i, out var lo, out var hi);
				if (model.IsRow(i))
				{
					num++;
					if (removedRowsByVid.ContainsKey(i))
					{
						continue;
					}
					if ((_vidToVars[i].kind != VidToVarMapKind.RowUnbounded && lo.IsFinite) || hi.IsFinite)
					{
						if (lo.IsFinite && hi.IsFinite)
						{
							if (lo != hi)
							{
								_mirrorRowCount++;
								_slackVarCount += 2;
							}
						}
						else
						{
							_slackVarCount++;
						}
					}
					else
					{
						num3++;
						hashSet.Add(i);
					}
					continue;
				}
				num2++;
				if ((_vidToVars[i].kind != VidToVarMapKind.VarSlack && lo.IsFinite) || hi.IsFinite)
				{
					if (lo.IsFinite && hi.IsFinite)
					{
						if (lo == hi)
						{
							_vidToVars[i].kind = VidToVarMapKind.VarConstant;
							_ignoredUserVarCount++;
						}
						else
						{
							_mirrorRowCount++;
							_slackVarCount++;
						}
					}
				}
				else if (_vidToVars[i].kind == VidToVarMapKind.VarSlack)
				{
					_ignoredUserVarCount++;
				}
				else
				{
					_unboundedVids.Add(i);
				}
			}
			RemoveIdleRows(ref _mirrorRowCount, ref _slackVarCount);
			if (0 < removedRowsByVid.Count)
			{
				foreach (KeyValuePair<int, bool> item in removedRowsByVid)
				{
					int key = item.Key;
					if (hashSet.Contains(key))
					{
						num3--;
					}
				}
			}
			num3 += removedRowsByVid.Count;
			_rowCount = num - num3 + _mirrorRowCount;
			_varCount = num2 - _ignoredUserVarCount + _slackVarCount;
			base.Logger.LogEvent(15, Resources.ModelReduction0UnboundedRowsRemoved1ConstantOrUnboundedSlackVariablesRemoved, num3, _ignoredUserVarCount);
		}

		/// <summary> Some models have constant "variables" which result in empty rows.
		/// </summary>
		private void RemoveIdleRows(ref int mirrorRows, ref int slackVars)
		{
			for (int i = 0; i < model.ColCount; i++)
			{
				VidToVarMapKind kind = _vidToVars[i].kind;
				if (!model.IsRow(i) || IsGoalVid(i) || kind == VidToVarMapKind.RowUnbounded)
				{
					continue;
				}
				bool flag = false;
				foreach (LinearEntry rowValue in model.GetRowValues(model.GetRowIndexFromVid(i)))
				{
					if (VidToVarMapKind.VarConstant != _vidToVars[rowValue.Index].kind)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					continue;
				}
				removedRowsByVid[i] = true;
				GetUserBounds(i, out var lo, out var hi);
				if (!lo.IsFinite && !hi.IsFinite)
				{
					continue;
				}
				if (lo.IsFinite && hi.IsFinite)
				{
					if (lo != hi)
					{
						mirrorRows--;
						slackVars -= 2;
					}
				}
				else
				{
					slackVars--;
				}
			}
		}

		/// <summary> Fill the vidToVar and varToVid maps, categorizing every vid
		/// </summary>
		protected void FillMaps()
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int colCount = model.ColCount;
			for (int i = 0; i < colCount; i++)
			{
				_vidToVars[i].iVar = int.MinValue;
				_vidToVars[i].mirror = int.MinValue;
				GetUserBounds(i, out var lo, out var hi);
				_vidToVars[i].lower = lo;
				_vidToVars[i].upper = hi;
				if (model.IsRow(i))
				{
					if (removedRowsByVid.ContainsKey(i))
					{
						_vidToVars[i].kind = VidToVarMapKind.RowUnbounded;
						num3++;
					}
					else if (lo.IsFinite || hi.IsFinite)
					{
						if (lo == hi)
						{
							_vidToVars[i].kind = VidToVarMapKind.RowConstant;
						}
						else
						{
							_inventions.Add(i);
							if (lo.IsFinite)
							{
								if (hi.IsFinite)
								{
									_vidToVars[i].kind = VidToVarMapKind.RowBounded;
									_inventions.Add(-1 - i);
									num++;
								}
								else
								{
									_vidToVars[i].kind = VidToVarMapKind.RowLower;
								}
							}
							else
							{
								_vidToVars[i].kind = VidToVarMapKind.RowUpper;
							}
						}
						_rowToVids[num5++] = i;
					}
					else if (IsGoalVid(i))
					{
						_vidToVars[i].kind = VidToVarMapKind.Goal;
						_vidToVars[i].lower = lo;
					}
					else
					{
						_vidToVars[i].kind = VidToVarMapKind.RowUnbounded;
					}
					continue;
				}
				if (lo.IsNegativeInfinity && hi.IsPositiveInfinity)
				{
					if (VidToVarMapKind.VarSlack == _vidToVars[i].kind)
					{
						continue;
					}
					_vidToVars[i].kind = VidToVarMapKind.VarUnbounded;
					_vidToVars[i].lower = lo;
					num2++;
				}
				if (lo == hi)
				{
					_vidToVars[i].kind = VidToVarMapKind.VarConstant;
					continue;
				}
				_varToVids[num4++] = i;
				if (lo.IsFinite && hi.IsFinite)
				{
					_vidToVars[i].kind = VidToVarMapKind.VarBounded;
					_inventions.Add(-1 - i);
					num++;
				}
				else if (lo.IsFinite)
				{
					_vidToVars[i].kind = VidToVarMapKind.VarLower;
				}
				else if (_vidToVars[i].kind != VidToVarMapKind.VarUnbounded)
				{
					_vidToVars[i].kind = VidToVarMapKind.VarUpper;
				}
			}
		}

		/// <summary> Choose a permutation of rows and vars which will minimize array fill
		///           during factorizations.
		/// </summary>
		protected void NullPermute()
		{
			int num = VarCount;
			while (_slackVarCount <= --num)
			{
				int num2 = _varToVids[num - _slackVarCount];
				_varToVids[num] = num2;
				_vidToVars[num2].iVar = num;
			}
			int num3 = _slackVarCount;
			while (0 <= --num3)
			{
				_varToVids[num3] = int.MinValue;
			}
			int num4 = RowCount;
			while (_mirrorRowCount <= --num4)
			{
				int num5 = _rowToVids[num4 - _mirrorRowCount];
				_rowToVids[num4] = num5;
				_vidToVars[num5].mirror = -1 - num4;
			}
			int num6 = _mirrorRowCount;
			while (0 <= --num6)
			{
				_rowToVids[num6] = int.MinValue;
			}
		}

		/// <summary> Copy initial values from user model into Matrix using prepared maps
		/// </summary>
		protected void CopyInitialMatrix()
		{
			int num = 0;
			TripleList<double> tripleList = new TripleList<double>();
			using (IEnumerator<ILinearGoal> enumerator = model.Goals.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					_ = enumerator.Current;
					num++;
					int rowIndexFromVid = model.GetRowIndexFromVid(base.GoalVid);
					foreach (LinearEntry rowValue in model.GetRowValues(rowIndexFromVid))
					{
						int index = rowValue.Index;
						Rational value = rowValue.Value;
						VidToVarMapKind kind = _vidToVars[index].kind;
						int iVar = _vidToVars[index].iVar;
						switch (kind)
						{
						case VidToVarMapKind.VarLower:
						case VidToVarMapKind.VarBounded:
							_constantCost.Add((double)(value * _vidToVars[index].lower));
							_cExact.SetCoefNonZero(iVar, _goalDirection * value);
							break;
						case VidToVarMapKind.VarUpper:
							_constantCost.Add((double)(value * _vidToVars[index].upper));
							_cExact.SetCoefNonZero(iVar, -1 * _goalDirection * value);
							break;
						case VidToVarMapKind.VarUnbounded:
							_cExact.SetCoefNonZero(iVar, _goalDirection * value);
							break;
						case VidToVarMapKind.VarConstant:
							_constantCost.Add((double)(value * _vidToVars[index].lower));
							break;
						default:
							throw new ArgumentException(Resources.OnlyVariablesNotRowsMayBeUsedWithinAGoalRow);
						}
					}
				}
			}
			for (int i = _mirrorRowCount; i < RowCount; i++)
			{
				int num2 = _rowToVids[i];
				VidToVarMapKind kind2 = _vidToVars[num2].kind;
				Rational rational = 0;
				foreach (LinearEntry rowValue2 in model.GetRowValues(model.GetRowIndexFromVid(num2)))
				{
					int index2 = rowValue2.Index;
					Rational value2 = rowValue2.Value;
					VidToVarMapKind kind3 = _vidToVars[index2].kind;
					int iVar2 = _vidToVars[index2].iVar;
					switch (kind3)
					{
					case VidToVarMapKind.VarLower:
					case VidToVarMapKind.VarBounded:
						if (!_vidToVars[index2].lower.IsZero)
						{
							rational = Rational.AddMul(rational, value2, _vidToVars[index2].lower);
						}
						tripleList.Add(i, iVar2, (double)value2);
						break;
					case VidToVarMapKind.VarUpper:
						if (!_vidToVars[index2].upper.IsZero)
						{
							rational = Rational.AddMul(rational, value2, _vidToVars[index2].upper);
						}
						value2 = -value2;
						tripleList.Add(i, iVar2, (double)value2);
						break;
					case VidToVarMapKind.VarUnbounded:
						tripleList.Add(i, iVar2, (double)value2);
						break;
					case VidToVarMapKind.VarConstant:
						rational = Rational.AddMul(rational, value2, _vidToVars[index2].lower);
						break;
					default:
						throw new ArgumentException(Resources.UnexpectedColumnKind);
					}
				}
				Rational num3 = ((kind2 != VidToVarMapKind.RowLower && kind2 != VidToVarMapKind.RowBounded && kind2 != VidToVarMapKind.RowConstant) ? _vidToVars[num2].upper : _vidToVars[num2].lower);
				num3 -= rational;
				if (!num3.IsZero)
				{
					_bExact.SetCoefNonZero(i, num3);
				}
			}
			int num4 = 0;
			for (int j = 0; j < _inventions.Count; j++)
			{
				int num5 = _inventions[j];
				if (num5 < 0)
				{
					int num6 = -1 - num5;
					_ = _vidToVars[num6].kind;
					tripleList.Add(num4, j, 1.0);
					tripleList.Add(num4, _vidToVars[num6].iVar, 1.0);
					Rational num7 = _vidToVars[num6].upper - _vidToVars[num6].lower;
					_bExact.SetCoefNonZero(num4, num7);
					_vidToVars[num6].mirror = j;
					num4++;
					continue;
				}
				int num8 = num5;
				int row = -1 - _vidToVars[num8].mirror;
				switch (_vidToVars[num8].kind)
				{
				case VidToVarMapKind.RowLower:
				case VidToVarMapKind.RowBounded:
					tripleList.Add(row, j, -1.0);
					break;
				case VidToVarMapKind.RowUpper:
					tripleList.Add(row, j, 1.0);
					break;
				default:
					throw new ArgumentException(Resources.BoundedVidWasNeitherRowNorVar);
				}
				_vidToVars[num8].iVar = j;
			}
			_inventions = null;
			int varCount = VarCount;
			if (_qpModel != null)
			{
				if (_goalDirection == 0)
				{
					throw new ArgumentException(Resources.PureQuadraticGoalButUnknownWhetherMinimizeOrMaximize);
				}
				TripleList<double> tripleList2 = new TripleList<double>();
				_mapQidToVid = new int[_qpModel.ColCount];
				int num9 = _qpModel.ColCount;
				while (0 <= --num9)
				{
					_mapQidToVid[num9] = -1;
				}
				int num10 = model.ColCount;
				while (0 <= --num10)
				{
					if (0 < _mpvidQid[num10])
					{
						_mapQidToVid[_mpvidQid[num10] - 1] = num10;
					}
				}
				int num11 = _qpModel.ColCount;
				while (0 <= --num11)
				{
				}
				for (int k = 0; k < _qpModel.RowCount; k++)
				{
					int num12 = _mapQidToVid[k];
					int var = GetVar(num12);
					if (var < 0)
					{
						CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_qpModel, k);
						while (rowIter.IsValid)
						{
							int num13 = _mapQidToVid[rowIter.Column];
							if (num13 != num12)
							{
								int var2 = GetVar(num13);
								if (0 <= var2)
								{
									_cExact[var2] += rowIter.Approx * _vidToVars[num12].lower * _goalDirection;
								}
							}
							rowIter.Advance();
						}
						continue;
					}
					ReverseMapParameters(num12, out var s, out var t);
					CoefMatrix.RowIter rowIter2 = new CoefMatrix.RowIter(_qpModel, k);
					while (rowIter2.IsValid)
					{
						int vid = _mapQidToVid[rowIter2.Column];
						double num14 = rowIter2.Approx * (double)_goalDirection;
						ReverseMapParameters(vid, out var s2, out var t2);
						double num15 = t * num14 * t2;
						double num16 = t * num14 * s2;
						t2 = t2 * num14 * s;
						num14 = s * num14 * s2;
						int var3 = GetVar(vid);
						if (0.0 != num14 && 0 <= var3 && 0 <= var && var3 <= var)
						{
							tripleList2.Add(var, var3, num14);
						}
						if (0 <= var3)
						{
							if (0.0 != t2)
							{
								_cExact[var] += t2 / 2.0;
							}
							if (0.0 != num16)
							{
								_cExact[var3] += num16 / 2.0;
							}
						}
						_constantCost.Add(num15 / 2.0);
						rowIter2.Advance();
					}
				}
				_Q = new SymmetricSparseMatrix(tripleList2, varCount);
				int num17 = 0;
				while (!_QnotDiagonal && num17 < _Q.ColumnCount)
				{
					if (1 < _Q.CountColumnSlots(num17))
					{
						_QnotDiagonal = true;
					}
					num17++;
				}
			}
			if (0 < tripleList.Count)
			{
				_A = new SparseMatrixDouble(tripleList, RowCount, VarCount);
			}
			else
			{
				_A = null;
			}
		}

		/// <summary> Return the dual value for a row constraint.
		/// </summary>
		/// <param name="y">Vector containing dual values.</param>
		/// <param name="vidRow">Row vid.</param>
		/// <returns>The dual value.</returns>
		protected Rational GetDualValue(double[] y, int vidRow)
		{
			EnsureVidToRowsMap();
			if (vidRow >= 0)
			{
				VidToVarMap vidToVarMap = _vidToVars[vidRow];
				if (vidToVarMap.kind == VidToVarMapKind.Goal)
				{
					return Rational.Zero;
				}
				if (IsSingletonRow(vidRow))
				{
					DebugContracts.NonNull(_yVec);
					return SingletonRowDual(_yVec, vidRow);
				}
				DebugContracts.NonNull(_yVec);
				return GetDualFromDualVector(_yVec, vidRow);
			}
			return Rational.Indeterminate;
		}

		/// <summary>Return the dual value corresponding to a vid.
		/// </summary>
		/// <param name="y">Vector containing dual values.</param>
		/// <param name="vid">A vid corresponding to a constraint.</param>_
		/// <returns>
		/// Returns the dual value.  If the constraint has both upper and lower bounds
		/// then there are actually two dual values.  In this case the dual for the active bound (if any) will be returned.
		/// </returns>
		protected override double GetDualFromDualVector(Vector y, int vid)
		{
			VidToVarMap vidToVarMap = _vidToVars[vid];
			int num = 1 - vidToVarMap.mirror;
			if (_vidToRows[vid] < 0)
			{
				vid = num;
			}
			if (vid >= 0)
			{
				double num2 = (IsGoalMinimize(0) ? 1 : (-1));
				return num2 * y[_vidToRows[vid]];
			}
			return 0.0;
		}

		private void EnsureVidToRowsMap()
		{
			if (_vidToRows == null)
			{
				_vidToRows = new int[_vidToVars.Length];
				for (int i = 0; i < _vidToRows.Length; i++)
				{
					_vidToRows[i] = -1;
				}
				if (_rowToVids != null)
				{
					for (int j = 0; j < _rowToVids.Length; j++)
					{
						int num = _rowToVids[j];
						if (num >= 0)
						{
							_vidToRows[num] = j;
						}
					}
				}
			}
			if (_yVec == null && _y != null)
			{
				_yVec = new Vector(_y);
			}
		}

		/// <summary> Do some quick tests of _Q and return true if it is not convex.
		/// </summary>
		private bool QisTriviallyNotConvex()
		{
			if (_Q == null)
			{
				return false;
			}
			for (int i = 0; i < _Q.ColumnCount; i++)
			{
				bool flag = false;
				bool flag2 = false;
				SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(_Q, i);
				while (colIter.IsValid)
				{
					int num = colIter.Row(_Q);
					if (colIter.Value(_Q) < 0.0)
					{
						if (i == num)
						{
							return true;
						}
						flag = true;
					}
					else
					{
						flag2 = flag2 || num == i;
					}
					colIter.Advance();
				}
				if (flag && !flag2)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary> Map from a var's value  TO a value for the user-model "vid".
		/// </summary>
		private Rational MapVidToValue(int vid)
		{
			VidToVarMap vidToVarMap = _vidToVars[vid];
			if (_x == null)
			{
				return Rational.Indeterminate;
			}
			GetUserBounds(vid, out var lo, out var hi);
			if (0 <= vidToVarMap.iVar)
			{
				Rational rational = _x[vidToVarMap.iVar];
				Rational rational2;
				switch (vidToVarMap.kind)
				{
				case VidToVarMapKind.VarConstant:
				case VidToVarMapKind.VarLower:
				case VidToVarMapKind.VarBounded:
					rational2 = rational + vidToVarMap.lower;
					if (rational2 < lo)
					{
						rational2 = lo;
					}
					else if (VidToVarMapKind.VarBounded == vidToVarMap.kind && hi < rational2)
					{
						rational2 = hi;
					}
					break;
				case VidToVarMapKind.VarUpper:
					rational2 = vidToVarMap.upper - rational;
					if (hi < rational2)
					{
						rational2 = hi;
					}
					break;
				case VidToVarMapKind.VarUnbounded:
					rational2 = rational;
					break;
				default:
					rational2 = Rational.Indeterminate;
					break;
				}
				return rational2;
			}
			return lo;
		}

		protected void FinalValues()
		{
			Rational[] array = new Rational[_vidToVars.Length];
			MapVarValues(array);
			Rational rational = 0;
			int rowIndexFromVid = model.GetRowIndexFromVid(base.GoalVid);
			foreach (LinearEntry rowValue in model.GetRowValues(rowIndexFromVid))
			{
				int index = rowValue.Index;
				rational += array[index] * rowValue.Value;
			}
			_primal = (double)rational;
			if (_qpModel != null)
			{
				double[] array2 = new double[array.Length];
				for (int i = 0; i < array2.Length; i++)
				{
					array2[i] = (double)array[i];
				}
				BigSum bigSum = 0.0;
				int num = _qpModel.ColCount;
				while (0 <= --num)
				{
					int num2 = _mapQidToVid[num];
					CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_qpModel, num);
					while (rowIter.IsValid)
					{
						int column = rowIter.Column;
						if (num <= column)
						{
							int num3 = _mapQidToVid[column];
							double num4 = array2[num2] * _qpModel.GetCoefDouble(num, column) * array2[num3];
							bigSum.Add((num < column) ? num4 : (num4 / 2.0));
						}
						rowIter.Advance();
					}
				}
				base.Solution.x = array2;
				_primal += (BigSum)bigSum.ToDouble();
			}
			_primal *= _goalDirection;
			_dual = _primal + Gap;
			base.Solution.cx = _primal.ToDouble();
			base.Solution.by = _dual.ToDouble();
			base.Solution.relGap = Gap / (1.0 + base.Solution.cx);
		}

		public override void MapVarValues(Rational[] vidValues)
		{
			if ((DirectSolution && base.Solution.status != LinearResult.Optimal) || _varCount == 0)
			{
				SolveGoalAlone(vidValues);
				return;
			}
			foreach (ILinearGoal goal in model.Goals)
			{
				_vidToVars[goal.Index].kind = VidToVarMapKind.Goal;
			}
			int i = 0;
			int num = Math.Min(vidValues.Length, _vidToVars.Length);
			if (_x == null)
			{
				for (int j = 0; j < num; j++)
				{
					ref Rational reference = ref vidValues[j];
					reference = Rational.Indeterminate;
				}
				return;
			}
			for (; i < num; i++)
			{
				VidToVarMap vidToVarMap = _vidToVars[i];
				if (VidToVarMapKind.VarConstant <= vidToVarMap.kind)
				{
					ref Rational reference2 = ref vidValues[i];
					reference2 = MapVidToValue(i);
					continue;
				}
				if (vidToVarMap.kind == VidToVarMapKind.Goal)
				{
					if (i == base.GoalVid)
					{
						ref Rational reference3 = ref vidValues[i];
						reference3 = (_goalDirection * (_primal + _dual) / 2).ToRational();
					}
					else
					{
						ref Rational reference4 = ref vidValues[i];
						reference4 = Rational.Indeterminate;
					}
					continue;
				}
				GetUserBounds(i, out var lo, out var hi);
				if (vidToVarMap.kind == VidToVarMapKind.RowConstant)
				{
					vidValues[i] = lo;
					continue;
				}
				if (vidToVarMap.kind == VidToVarMapKind.RowUnbounded)
				{
					Rational rational = 0;
					foreach (LinearEntry rowEntry in model.GetRowEntries(i))
					{
						rational += rowEntry.Value * MapVidToValue(rowEntry.Index);
					}
					vidValues[i] = rational;
					continue;
				}
				switch (vidToVarMap.kind)
				{
				case VidToVarMapKind.RowConstant:
				case VidToVarMapKind.RowLower:
				case VidToVarMapKind.RowBounded:
				{
					ref Rational reference7 = ref vidValues[i];
					reference7 = lo + _x[vidToVarMap.iVar];
					break;
				}
				case VidToVarMapKind.RowUpper:
				{
					ref Rational reference6 = ref vidValues[i];
					reference6 = hi - _x[vidToVarMap.iVar];
					break;
				}
				default:
				{
					ref Rational reference5 = ref vidValues[i];
					reference5 = Rational.Indeterminate;
					break;
				}
				}
			}
			for (; i < vidValues.Length; i++)
			{
				ref Rational reference8 = ref vidValues[i];
				reference8 = Rational.Indeterminate;
			}
		}

		private void SolveGoalAlone(Rational[] vidValues)
		{
			int num = Math.Min(vidValues.Length, _vidToVars.Length);
			for (int i = 0; i < num; i++)
			{
				VidToVarMap vidToVarMap = _vidToVars[i];
				if (VidToVarMapKind.VarConstant == vidToVarMap.kind)
				{
					GetUserBounds(i, out var lo, out var _);
					vidValues[i] = lo;
				}
				else
				{
					ref Rational reference = ref vidValues[i];
					reference = Rational.Indeterminate;
				}
			}
			Rational rational = 0;
			foreach (LinearEntry rowValue in model.GetRowValues(model.GetRowIndexFromVid(base.GoalVid)))
			{
				int index = rowValue.Index;
				rational += vidValues[index] * rowValue.Value;
			}
			foreach (QuadraticEntry rowQuadraticEntry in model.GetRowQuadraticEntries(base.GoalVid))
			{
				rational += vidValues[rowQuadraticEntry.Index1] * vidValues[rowQuadraticEntry.Index2] * rowQuadraticEntry.Value;
			}
			vidValues[base.GoalVid] = rational;
			_primal = (double)rational;
			_dual = _primal;
			base.Solution.cx = _primal.ToDouble();
			base.Solution.by = _dual.ToDouble();
			base.Solution.relGap = Gap / (1.0 + base.Solution.cx);
		}
	}
}
