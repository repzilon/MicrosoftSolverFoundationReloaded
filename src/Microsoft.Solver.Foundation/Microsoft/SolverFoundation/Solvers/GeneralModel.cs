using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>LP/QP/SOCP model used by solver (Solver Model)
	///
	///      minimize             cL'*xL +    cU'*xU + cF'*xF                |
	///      subject to  bG &lt;= AGL*xL +    AGU*xU + AGF*xF &lt;= bG + uH  |     mG
	///                  bK &lt;= AKL*xL +    AKU*xU + AKF*xF                |     mK
	///                       0 &lt;= xL, 0 &lt;= xU &lt;= uV                |      
	///      -------------------------------------------------------       
	///                            nL       nU       nF                   (sizes)
	/// </summary>
	internal abstract class GeneralModel : InteriorPointReducedModel
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
			RowCone,
			RowConicQuadratic,
			RowConicRotated,
			/// <summary> All row kinds are less than VarZero
			/// </summary>
			VarZero,
			VarConstant,
			VarLower,
			VarUpper,
			VarBounded,
			VarUnbounded
		}

		/// <summary> This is the key to understanding how user vids map to solver vars.
		/// </summary>
		protected struct VidToVarMap
		{
			internal Rational lower;

			internal Rational upper;

			/// <summary> MinValue =&gt; eliminated, non-negative =&gt; standard var
			/// </summary>
			internal int iVar;

			internal double scale;

			internal double shift;

			/// <summary> goal &lt; row kinds &lt; var kinds
			/// </summary>
			internal VidToVarMapKind kind;

			public override string ToString()
			{
				StringBuilder stringBuilder = new StringBuilder(iVar.ToString(CultureInfo.InvariantCulture));
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

		/// <summary>Categorizes constraints by type.
		/// </summary>
		private class ConstraintPartition
		{
			/// <summary>lower and upper bounded, including equality
			/// </summary>
			public List<int> G = new List<int>();

			/// <summary>lower bounded inequality
			/// </summary>
			public List<int> IG = new List<int>();

			/// <summary>upper bounded inequality, to be transformed to lower bounded
			/// </summary>
			public List<int> IL = new List<int>();

			/// <summary>Quadratic or rotated quadratic cones.
			/// </summary>
			public List<int> QR = new List<int>();

			/// <summary>free or zero row, these are to be ignored
			/// </summary>
			public List<int> F0 = new List<int>();

			/// <summary>Total number of constraints.
			/// </summary>
			public int Count => G.Count + IG.Count + IL.Count + QR.Count + F0.Count;

			/// <summary>String representation.
			/// </summary>
			public override string ToString()
			{
				return "Count = " + Count + ", Up/Lo = " + G.Count + ", Lo = " + IG.Count + ", Up = " + IL.Count + ", QR = " + QR.Count + ", F0 = " + F0.Count;
			}
		}

		/// <summary>Categorizes variables by type.
		/// </summary>
		private class VariablePartition
		{
			/// <summary>lower bounded variables (greater than lower bound)
			/// </summary>
			public List<int> LG = new List<int>();

			/// <summary>upper bounded variables, to be transformed into lower bounded
			/// </summary>
			public List<int> LL = new List<int>();

			/// <summary>upper and lower bounded variables
			/// </summary>
			public List<int> U = new List<int>();

			/// <summary>free variables
			/// </summary>
			public List<int> F = new List<int>();

			/// <summary>zero columns in A (zero coefficients in all rows)
			/// </summary>
			public List<int> Zero = new List<int>();

			/// <summary>constant (fixed) variables
			/// </summary>
			public List<int> Const = new List<int>();

			/// <summary>String representation.
			/// </summary>
			public override string ToString()
			{
				return "Lo = " + LG.Count + ", Up = " + LL.Count + ", Up/Lo = " + U.Count + ", F = " + F.Count + ", 0 = " + Zero.Count + ", C = " + Const.Count;
			}
		}

		private static readonly double sqrt2over2 = Math.Sqrt(2.0) / 2.0;

		private readonly double minMatrixNorm = 1E-16;

		private readonly double constantVarTol = 1E-16;

		private readonly double zeroRowTol = 1E-12;

		private readonly double zeroVarTol = 1E-12;

		private double _minScaling = 1000.0;

		private bool _computeRowColumnNorms;

		private int _presolveLevel;

		protected VidToVarMap[] vidToVars;

		protected int mG;

		protected int mK;

		protected int nL;

		protected int nU;

		protected int nF;

		protected SparseMatrixDouble AGKLUF;

		protected Vector bGbK;

		protected Vector cLcUcF;

		protected Vector uH;

		protected Vector uV;

		protected double cxShift;

		protected double _primal;

		protected double _dual;

		protected ConicStructure kone;

		protected int nC;

		protected int mC;

		protected int mQ;

		protected int mR;

		/// <summary> The primal version of the objective
		/// </summary>
		public override double Primal => (double)_goalDirection * _primal;

		/// <summary> The dual version of the objective
		/// </summary>
		public override double Dual => (double)_goalDirection * _dual;

		/// <summary>Create a new instance using the given LinearModel.
		/// </summary>
		/// <param name="model">The LinearModel.</param>
		/// <param name="log">The LogSource.</param>
		/// <param name="presolveLevel">Presolve level.</param>
		public GeneralModel(LinearModel model, LogSource log, int presolveLevel)
			: base(model, log)
		{
			_presolveLevel = presolveLevel;
			_solution = new StandardSolution();
			_solution.status = LinearResult.Invalid;
			kone = new ConicStructure(0, new List<int>(), new List<int>());
			DetermineGoal();
			ConvertUserModel();
		}

		private void ConvertUserModel()
		{
			int colCount = model.ColCount;
			if (colCount == 0)
			{
				throw new ArgumentException(Resources.InteriorPointCannotLoadAModelWithNoVariables);
			}
			if (model.IsQuadraticModel)
			{
				throw new MsfException(Resources.HSDDoesNotHandleQuadratic);
			}
			base.Logger.LogEvent(15, "Reduced model: rows {0}, columns {1}, nonzeros {2}.", model.RowCount, model.VariableCount, model.CoefficientCount);
			if (_socpModel != null)
			{
				base.Logger.LogEvent(15, "Reduced model: second order cones {0}.", _socpModel.ConeCount);
			}
			if (_presolveLevel != 0)
			{
				Presolve(_presolveLevel);
				if (base.Solution.status != 0)
				{
					return;
				}
			}
			vidToVars = new VidToVarMap[colCount];
			ConstraintPartition constraintPartition = PartitionConstraints();
			if (base.Solution.status != 0)
			{
				return;
			}
			GetRowColumnNorms(constraintPartition, out var rowMax, out var colMax, out var matMax);
			GetConstraintScaling(constraintPartition, rowMax, matMax);
			VariablePartition vars = PartitionVariables(colMax, matMax);
			if (base.Solution.status != 0)
			{
				return;
			}
			if (_presolveLevel != 0)
			{
				RemoveIdleRows(constraintPartition);
				if (base.Solution.status != 0)
				{
					return;
				}
			}
			double[] c = null;
			GetVariableScaling(vars, ref c, colMax);
			colMax = null;
			if (base.Solution.status == LinearResult.Invalid)
			{
				GetVariableShift(vars, c);
				vars = null;
				GetObjectiveShift(c);
				c = null;
				GetConstraintShift(constraintPartition);
				CreateSolverRhs(constraintPartition);
				mG = constraintPartition.G.Count;
				mK = constraintPartition.IG.Count + constraintPartition.IL.Count;
				int[] array = new int[constraintPartition.G.Count + constraintPartition.IG.Count + constraintPartition.IL.Count + constraintPartition.QR.Count];
				constraintPartition.G.CopyTo(array, 0);
				constraintPartition.IG.CopyTo(array, constraintPartition.G.Count);
				constraintPartition.IL.CopyTo(array, constraintPartition.G.Count + constraintPartition.IG.Count);
				constraintPartition.QR.CopyTo(array, constraintPartition.G.Count + constraintPartition.IG.Count + constraintPartition.IL.Count);
				constraintPartition = null;
				CreateSolverMatrix(array);
			}
		}

		private bool RemoveIdleRows(ConstraintPartition cons)
		{
			int num = 0;
			for (int i = 0; i < model.ColCount; i++)
			{
				VidToVarMapKind kind = vidToVars[i].kind;
				if (!model.IsRow(i) || IsGoalVid(i) || kind == VidToVarMapKind.RowUnbounded)
				{
					continue;
				}
				bool flag = IsConicRow(i);
				foreach (LinearEntry rowValue in model.GetRowValues(model.GetRowIndexFromVid(i)))
				{
					if (VidToVarMapKind.VarConstant != vidToVars[rowValue.Index].kind)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					num++;
					removedRowsByVid[i] = true;
					vidToVars[i].kind = VidToVarMapKind.RowUnbounded;
					switch (kind)
					{
					case VidToVarMapKind.RowBounded:
						cons.G.Remove(i);
						break;
					case VidToVarMapKind.RowLower:
						cons.IG.Remove(i);
						break;
					case VidToVarMapKind.RowUpper:
						cons.IL.Remove(i);
						break;
					case VidToVarMapKind.RowUnbounded:
						cons.F0.Remove(i);
						break;
					case VidToVarMapKind.RowConicQuadratic:
					case VidToVarMapKind.RowConicRotated:
						cons.QR.Remove(i);
						break;
					}
				}
			}
			return num > 0;
		}

		private ConstraintPartition PartitionConstraints()
		{
			ConstraintPartition constraintPartition = new ConstraintPartition();
			for (int i = 0; i < model.ColCount; i++)
			{
				GetUserBounds(i, out var lo, out var hi);
				vidToVars[i].lower = lo;
				vidToVars[i].upper = hi;
				vidToVars[i].scale = 1.0;
				vidToVars[i].iVar = -1;
				if (!model.IsRow(i))
				{
					continue;
				}
				if (model.IsGoal(i))
				{
					vidToVars[i].kind = VidToVarMapKind.Goal;
					if (lo.IsNegativeInfinity && hi.IsPositiveInfinity)
					{
						continue;
					}
				}
				if (_socpModel != null && (_socpModel.IsConicRow(i) || _socpModel.TryGetConeFromIndex(i, out var _)))
				{
					continue;
				}
				if (removedRowsByVid.ContainsKey(i))
				{
					vidToVars[i].kind = VidToVarMapKind.RowConstant;
					constraintPartition.F0.Add(i);
				}
				else if (lo.IsNegativeInfinity)
				{
					if (hi.IsPositiveInfinity)
					{
						vidToVars[i].kind = VidToVarMapKind.RowUnbounded;
						constraintPartition.F0.Add(i);
					}
					else if (hi.IsNegativeInfinity)
					{
						base.Solution.status = LinearResult.InfeasiblePrimal;
					}
					else
					{
						vidToVars[i].kind = VidToVarMapKind.RowUpper;
						constraintPartition.IL.Add(i);
					}
				}
				else if (lo.IsPositiveInfinity)
				{
					base.Solution.status = LinearResult.InfeasiblePrimal;
				}
				else if (hi.IsPositiveInfinity)
				{
					vidToVars[i].kind = VidToVarMapKind.RowLower;
					constraintPartition.IG.Add(i);
				}
				else
				{
					if (lo > hi)
					{
						base.Solution.status = LinearResult.InfeasiblePrimal;
					}
					constraintPartition.G.Add(i);
					vidToVars[i].kind = VidToVarMapKind.RowBounded;
				}
			}
			if (_socpModel != null)
			{
				PartitionCones(constraintPartition);
			}
			return constraintPartition;
		}

		private void PartitionCones(ConstraintPartition cons)
		{
			int mL = cons.IL.Count + cons.IG.Count;
			kone = new ConicStructure(mL, new List<int>(), new List<int>());
			for (int i = 0; i < model.ColCount; i++)
			{
				if (_socpModel.TryGetConeFromIndex(i, out var cone))
				{
					vidToVars[i].kind = VidToVarMapKind.RowCone;
					SecondOrderCone cone2 = cone as SecondOrderCone;
					int num = 0;
					foreach (int item in cone2.Vids.OrderBy((int v) => (v != cone2.PrimaryVid1 && v != cone2.PrimaryVid2) ? 1 : 0))
					{
						num++;
						if (cone2.ConeType == SecondOrderConeType.Quadratic)
						{
							vidToVars[item].kind = VidToVarMapKind.RowConicQuadratic;
						}
						else
						{
							if (cone2.ConeType != SecondOrderConeType.RotatedQuadratic)
							{
								throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidConeType0, new object[1] { cone2.ConeType }));
							}
							vidToVars[item].kind = VidToVarMapKind.RowConicRotated;
						}
						cons.QR.Add(item);
					}
					if (cone2.ConeType == SecondOrderConeType.Quadratic)
					{
						kone.mQ.Add(num);
						continue;
					}
					if (cone2.ConeType != SecondOrderConeType.RotatedQuadratic)
					{
						throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidConeType0, new object[1] { cone2.ConeType }));
					}
					kone.mR.Add(num);
				}
				else
				{
					_socpModel.IsConicRow(i);
				}
			}
		}

		private VariablePartition PartitionVariables(Vector colMax, double matMax)
		{
			VariablePartition variablePartition = new VariablePartition();
			for (int i = 0; i < model.ColCount; i++)
			{
				if (!IsCol(i))
				{
					continue;
				}
				Rational lo = vidToVars[i].lower;
				Rational hi = vidToVars[i].upper;
				GetUserBounds(i, out lo, out hi);
				if (colMax[i] < zeroVarTol * matMax)
				{
					if (lo <= hi)
					{
						vidToVars[i].kind = VidToVarMapKind.VarZero;
						vidToVars[i].lower = (vidToVars[i].upper = (lo.IsFinite ? lo : Rational.Zero));
						variablePartition.Zero.Add(i);
					}
					else
					{
						base.Solution.status = LinearResult.InfeasiblePrimal;
					}
				}
				else if (lo.IsNegativeInfinity)
				{
					if (hi.IsPositiveInfinity)
					{
						vidToVars[i].kind = VidToVarMapKind.VarUnbounded;
						variablePartition.F.Add(i);
					}
					else if (hi.IsNegativeInfinity)
					{
						base.Solution.status = LinearResult.UnboundedPrimal;
					}
					else
					{
						vidToVars[i].kind = VidToVarMapKind.VarUpper;
						variablePartition.LL.Add(i);
					}
				}
				else if (lo.IsPositiveInfinity)
				{
					base.Solution.status = LinearResult.InfeasiblePrimal;
				}
				else if (hi.IsPositiveInfinity)
				{
					vidToVars[i].kind = VidToVarMapKind.VarLower;
					variablePartition.LG.Add(i);
				}
				else if (lo > hi)
				{
					base.Solution.status = LinearResult.InfeasiblePrimal;
				}
				else if (hi - lo < constantVarTol * Math.Max(1.0, Math.Abs(hi.ToDouble())))
				{
					vidToVars[i].kind = VidToVarMapKind.VarConstant;
					vidToVars[i].lower = (vidToVars[i].upper = (lo + hi) / 2);
					variablePartition.Const.Add(i);
				}
				else
				{
					vidToVars[i].kind = VidToVarMapKind.VarBounded;
					variablePartition.U.Add(i);
				}
			}
			nL = variablePartition.LG.Count + variablePartition.LL.Count;
			nU = variablePartition.U.Count;
			nF = variablePartition.F.Count;
			return variablePartition;
		}

		private void GetConstraintScaling(ConstraintPartition cons, Vector rowMax, double matMax)
		{
			for (int i = 0; i < model.ColCount; i++)
			{
				if (!IsRow(i) || IsCone(i) || IsConicRow(i))
				{
					continue;
				}
				if (rowMax[i] < zeroRowTol * matMax)
				{
					if (vidToVars[i].lower > 0 || vidToVars[i].upper < 0)
					{
						base.Solution.status = LinearResult.InfeasiblePrimal;
					}
					switch (vidToVars[i].kind)
					{
					case VidToVarMapKind.RowBounded:
						cons.G.Remove(i);
						break;
					case VidToVarMapKind.RowLower:
						cons.IG.Remove(i);
						break;
					case VidToVarMapKind.RowUpper:
						cons.IL.Remove(i);
						break;
					case VidToVarMapKind.RowUnbounded:
						cons.F0.Remove(i);
						break;
					}
					vidToVars[i].kind = VidToVarMapKind.RowUnbounded;
					cons.F0.Add(i);
				}
				else if (!IsConicRow(i) && !IsCone(i))
				{
					double num = rowMax[i];
					if (num > _minScaling)
					{
						vidToVars[i].scale /= num;
					}
				}
			}
		}

		private void GetRowColumnNorms(ConstraintPartition cons, out Vector rowMax, out Vector colMax, out double matMax)
		{
			rowMax = new Vector(model.ColCount);
			colMax = new Vector(model.ColCount);
			if (!_computeRowColumnNorms)
			{
				matMax = 1.0;
				rowMax.ConstantFill(1.0);
				colMax.ConstantFill(1.0);
				return;
			}
			List<int>[] array = new List<int>[3] { cons.G, cons.IG, cons.IL };
			List<int>[] array2 = array;
			foreach (List<int> list in array2)
			{
				foreach (int item in list)
				{
					foreach (LinearEntry rowEntry in model.GetRowEntries(item))
					{
						Rational value = rowEntry.Value;
						double val = Math.Abs(value.ToDouble());
						rowMax[item] = Math.Max(rowMax[item], val);
						colMax[rowEntry.Index] = Math.Max(colMax[rowEntry.Index], val);
					}
					rowMax[item] = Math.Pow(2.0, Math.Floor(Math.Log(Math.Sqrt(rowMax[item]), 2.0)));
				}
			}
			foreach (int item2 in cons.F0)
			{
				rowMax[item2] = 1.0;
			}
			foreach (int item3 in cons.QR)
			{
				rowMax[item3] = 1.0;
				foreach (LinearEntry rowEntry2 in model.GetRowEntries(item3))
				{
					Rational value2 = rowEntry2.Value;
					double val2 = Math.Abs(value2.ToDouble());
					colMax[rowEntry2.Index] = Math.Max(colMax[rowEntry2.Index], val2);
				}
			}
			for (int j = 0; j < colMax.Length; j++)
			{
				colMax[j] = Math.Pow(2.0, Math.Floor(Math.Log(Math.Sqrt(colMax[j]), 2.0)));
			}
			matMax = colMax.NormInf();
			_ = matMax;
			_ = minMatrixNorm;
		}

		private void GetVariableScaling(VariablePartition vars, ref double[] c, Vector colMax)
		{
			c = new double[model.ColCount];
			foreach (LinearEntry rowEntry in model.GetRowEntries(base.GoalVid))
			{
				double[] obj = c;
				int index = rowEntry.Index;
				double num = _goalDirection;
				Rational value = rowEntry.Value;
				obj[index] = num * value.ToDouble();
			}
			List<int>[] array = new List<int>[4] { vars.LG, vars.LL, vars.U, vars.F };
			List<int>[] array2 = array;
			foreach (List<int> list in array2)
			{
				foreach (int item in list)
				{
					double num2 = Math.Sqrt(colMax[item]);
					if (num2 > _minScaling)
					{
						vidToVars[item].scale /= num2;
					}
				}
			}
			foreach (int item2 in vars.Zero)
			{
				Rational lower = vidToVars[item2].lower;
				Rational upper = vidToVars[item2].upper;
				if (c[item2] > 0.0)
				{
					if (lower.IsNegativeInfinity)
					{
						base.Solution.status = LinearResult.UnboundedPrimal;
					}
					else
					{
						vidToVars[item2].shift = lower.ToDouble();
					}
				}
				else if (c[item2] < 0.0)
				{
					if (upper.IsPositiveInfinity)
					{
						base.Solution.status = LinearResult.UnboundedPrimal;
					}
					else
					{
						vidToVars[item2].shift = upper.ToDouble();
					}
				}
				else
				{
					vidToVars[item2].shift = Math.Min(Math.Max(lower.ToDouble(), 0.0), upper.ToDouble());
				}
			}
			foreach (int item3 in vars.Const)
			{
				Rational lower2 = vidToVars[item3].lower;
				Rational upper2 = vidToVars[item3].upper;
				vidToVars[item3].shift = ((lower2 + upper2) / 2).ToDouble();
			}
		}

		private void CreateSolverMatrix(int[] consA)
		{
			TripleList<double> tripleList = new TripleList<double>();
			int num = 0;
			for (int i = 0; i < consA.Length; i++)
			{
				int num2 = consA[i];
				if (vidToVars[num2].kind == VidToVarMapKind.RowConicRotated)
				{
					int num3 = i + kone.mR[num];
					AddTripletsForPrimaryConicRows(tripleList, consA, i);
					double value = sqrt2over2 * (bGbK[i] + bGbK[i + 1]);
					double value2 = sqrt2over2 * (bGbK[i] - bGbK[i + 1]);
					bGbK[i] = value;
					bGbK[i + 1] = value2;
					for (i++; i < num3; i++)
					{
						AddTripletsForRow(tripleList, i, consA[i]);
					}
					num++;
				}
				else if (vidToVars[num2].kind != VidToVarMapKind.RowUnbounded)
				{
					AddTripletsForRow(tripleList, i, num2);
				}
			}
			if (tripleList.Count > 0)
			{
				TripleList<double>.DuplicatePolicy duplicate = ((kone.mR.Count > 0) ? new TripleList<double>.DuplicatePolicy(AddDuplicates) : null);
				AGKLUF = new SparseMatrixDouble(tripleList, consA.Length, cLcUcF.Length, duplicate);
			}
			kone.mQ.AddRange(kone.mR);
			kone.mR.Clear();
		}

		private void AddTripletsForPrimaryConicRows(TripleList<double> ts, int[] consA, int rowIndex)
		{
			int num = consA[rowIndex];
			int num2 = consA[rowIndex + 1];
			foreach (LinearEntry rowValue in model.GetRowValues(model.GetRowIndexFromVid(num)))
			{
				int index = rowValue.Index;
				int iVar = vidToVars[index].iVar;
				if (vidToVars[index].kind != VidToVarMapKind.VarZero && vidToVars[index].kind != VidToVarMapKind.VarConstant)
				{
					Rational value = rowValue.Value;
					if (!value.IsZero)
					{
						double num3 = (vidToVars[index].scale * rowValue.Value).ToDouble();
						ts.Add(rowIndex, iVar, sqrt2over2 * vidToVars[num].scale * num3);
						ts.Add(rowIndex + 1, iVar, sqrt2over2 * vidToVars[num2].scale * num3);
					}
				}
			}
			foreach (LinearEntry rowValue2 in model.GetRowValues(model.GetRowIndexFromVid(num2)))
			{
				int index2 = rowValue2.Index;
				int iVar2 = vidToVars[index2].iVar;
				if (vidToVars[index2].kind != VidToVarMapKind.VarZero && vidToVars[index2].kind != VidToVarMapKind.VarConstant)
				{
					Rational value2 = rowValue2.Value;
					if (!value2.IsZero)
					{
						double num4 = (vidToVars[index2].scale * rowValue2.Value).ToDouble();
						ts.Add(rowIndex, iVar2, sqrt2over2 * vidToVars[num].scale * num4);
						ts.Add(rowIndex + 1, iVar2, (0.0 - sqrt2over2) * vidToVars[num2].scale * num4);
					}
				}
			}
		}

		private double AddDuplicates(double first, double second)
		{
			return first + second;
		}

		private void AddTripletsForRow(TripleList<double> ts, int rowIndex, int vidRow)
		{
			foreach (LinearEntry rowValue in model.GetRowValues(model.GetRowIndexFromVid(vidRow)))
			{
				int index = rowValue.Index;
				int iVar = vidToVars[index].iVar;
				if (vidToVars[index].kind != VidToVarMapKind.VarZero && vidToVars[index].kind != VidToVarMapKind.VarConstant)
				{
					Rational value = rowValue.Value;
					if (!value.IsZero)
					{
						ts.Add(rowIndex, iVar, (vidToVars[vidRow].scale * vidToVars[index].scale * rowValue.Value).ToDouble());
					}
				}
			}
		}

		private void CreateSolverRhs(ConstraintPartition cons)
		{
			int i = 0;
			bGbK = new Vector(cons.G.Count + cons.IG.Count + cons.IL.Count + cons.QR.Count);
			uH = new Vector(cons.G.Count);
			foreach (int item in cons.G)
			{
				bGbK[i] = ((vidToVars[item].lower - vidToVars[item].shift) * vidToVars[item].scale).ToDouble();
				uH[i] = ((vidToVars[item].upper - vidToVars[item].lower) * vidToVars[item].scale).ToDouble();
				vidToVars[item].iVar = i++;
			}
			foreach (int item2 in cons.IG)
			{
				bGbK[i] = ((vidToVars[item2].lower - vidToVars[item2].shift) * vidToVars[item2].scale).ToDouble();
				vidToVars[item2].iVar = i++;
			}
			foreach (int item3 in cons.IL)
			{
				vidToVars[item3].scale *= -1.0;
				bGbK[i] = ((vidToVars[item3].upper - vidToVars[item3].shift) * vidToVars[item3].scale).ToDouble();
				vidToVars[item3].iVar = i++;
			}
			foreach (int item4 in cons.QR)
			{
				bGbK[i] = ((vidToVars[item4].lower - vidToVars[item4].shift) * vidToVars[item4].scale).ToDouble();
				vidToVars[item4].iVar = i++;
			}
		}

		private void GetVariableShift(VariablePartition vars, double[] c)
		{
			cLcUcF = new Vector(nL + nU + nF);
			uV = new Vector(nU);
			int i = 0;
			foreach (int item in vars.LG)
			{
				cLcUcF[i] = c[item] * vidToVars[item].scale;
				vidToVars[item].shift = vidToVars[item].lower.ToDouble();
				vidToVars[item].iVar = i++;
			}
			foreach (int item2 in vars.LL)
			{
				vidToVars[item2].scale *= -1.0;
				cLcUcF[i] = c[item2] * vidToVars[item2].scale;
				vidToVars[item2].shift = vidToVars[item2].upper.ToDouble();
				vidToVars[item2].iVar = i++;
			}
			int num = 0;
			foreach (int item3 in vars.U)
			{
				cLcUcF[i] = c[item3] * vidToVars[item3].scale;
				uV[num++] = ((vidToVars[item3].upper - vidToVars[item3].lower) / vidToVars[item3].scale).ToDouble();
				vidToVars[item3].shift = vidToVars[item3].lower.ToDouble();
				vidToVars[item3].iVar = i++;
			}
			foreach (int item4 in vars.F)
			{
				cLcUcF[i] = c[item4] * vidToVars[item4].scale;
				vidToVars[item4].iVar = i++;
			}
		}

		private void GetConstraintShift(ConstraintPartition cons)
		{
			List<int>[] array = new List<int>[4] { cons.G, cons.IG, cons.IL, cons.F0 };
			List<int>[] array2 = array;
			foreach (List<int> list in array2)
			{
				foreach (int item in list)
				{
					foreach (LinearEntry rowValue in model.GetRowValues(model.GetRowIndexFromVid(item)))
					{
						vidToVars[item].shift += (rowValue.Value * vidToVars[rowValue.Index].shift).ToDouble();
					}
				}
			}
		}

		private void GetObjectiveShift(double[] c)
		{
			cxShift = 0.0;
			for (int i = 0; i < c.Length; i++)
			{
				if (c[i] != 0.0)
				{
					cxShift += c[i] * vidToVars[i].shift;
				}
			}
		}

		/// <summary>Map vars to user model values.
		/// </summary>
		public override void MapVarValues(Rational[] vidValues)
		{
			if (EmptySolution() && base.Solution.status != LinearResult.Optimal)
			{
				SolveGoalAlone(vidValues);
				_primal = base.Solution.cx;
				_dual = base.Solution.by;
				return;
			}
			foreach (ILinearGoal goal in model.Goals)
			{
				vidToVars[goal.Index].kind = VidToVarMapKind.Goal;
			}
			int num = Math.Min(vidValues.Length, vidToVars.Length);
			for (int i = 0; i < num; i++)
			{
				VidToVarMap vvm = vidToVars[i];
				if (IsCol(i))
				{
					ref Rational reference = ref vidValues[i];
					reference = MapColVidToValue(ref vvm);
				}
				else if (vvm.kind == VidToVarMapKind.Goal)
				{
					if (i == base.GoalVid)
					{
						ref Rational reference2 = ref vidValues[i];
						reference2 = (Primal + Dual) / 2.0;
					}
					else
					{
						ref Rational reference3 = ref vidValues[i];
						reference3 = Rational.Indeterminate;
					}
				}
				else
				{
					ref Rational reference4 = ref vidValues[i];
					reference4 = MapRowVidToValue(ref vvm, i);
				}
			}
		}

		protected abstract bool EmptySolution();

		protected abstract Rational MapColVidToValue(ref VidToVarMap vvm);

		protected abstract Rational MapRowVidToValue(ref VidToVarMap vvm, int vid);

		protected Rational MapColVidToValue(int vid)
		{
			return MapColVidToValue(ref vidToVars[vid]);
		}

		/// <summary>Solve a zero-constraint (null A) problem.
		/// </summary>
		protected void SolveZeroConstraints(Vector xLxUxF)
		{
			base.Solution.status = LinearResult.Optimal;
			using (IEnumerator<ILinearGoal> enumerator = model.Goals.GetEnumerator())
			{
				if (!enumerator.MoveNext())
				{
					return;
				}
				_ = enumerator.Current;
				double num = 0.0;
				int rowIndexFromVid = model.GetRowIndexFromVid(base.GoalVid);
				foreach (LinearEntry rowValue in model.GetRowValues(rowIndexFromVid))
				{
					Rational value = rowValue.Value;
					int num2 = value.Sign * _goalDirection;
					VidToVarMap vvm = vidToVars[rowValue.Index];
					Rational lower = vvm.lower;
					Rational upper = vvm.upper;
					if ((num2 < 0 && upper.IsPositiveInfinity) || (0 < num2 && lower.IsNegativeInfinity))
					{
						base.Solution.status = LinearResult.UnboundedPrimal;
						break;
					}
					double num3 = ((num2 < 0) ? upper : lower).ToDouble();
					double num4 = num;
					Rational value2 = rowValue.Value;
					num = num4 + num3 * value2.ToDouble();
					if (vvm.iVar >= 0)
					{
						xLxUxF[vvm.iVar] = MapToSolverModel(ref vvm, num3);
					}
				}
				ref Rational reference = ref model._mpvidnum[base.GoalVid];
				reference = num;
				base.Solution.cx = (base.Solution.by = num);
				_primal = (_dual = (double)_goalDirection * base.Solution.cx);
			}
		}

		protected static double MapToSolverModel(ref VidToVarMap vvm, double num)
		{
			return num / vvm.scale - vvm.shift;
		}

		protected static Rational MapToUserModel(ref VidToVarMap vvm, double value)
		{
			return vvm.scale * (value + vvm.shift);
		}

		protected void SolveGoalAlone(Rational[] vidValues)
		{
			int num = Math.Min(vidValues.Length, (vidToVars != null) ? vidToVars.Length : 0);
			for (int i = 0; i < num; i++)
			{
				VidToVarMap vidToVarMap = vidToVars[i];
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
			for (int j = num; j < vidValues.Length; j++)
			{
				ref Rational reference2 = ref vidValues[j];
				reference2 = Rational.Indeterminate;
			}
			Rational rational = 0;
			foreach (LinearEntry rowValue in model.GetRowValues(model.GetRowIndexFromVid(base.GoalVid)))
			{
				int index = rowValue.Index;
				rational += vidValues[index] * rowValue.Value;
			}
			vidValues[base.GoalVid] = rational;
			_primal = (double)rational;
			_dual = _primal;
			base.Solution.cx = _primal;
			base.Solution.by = _dual;
			base.Solution.relGap = Gap / (1.0 + base.Solution.cx);
		}

		/// <summary> Return the dual value for a row constraint.
		/// </summary>
		/// <param name="y">Vector containing dual values.</param>
		/// <param name="vidRow">Row vid.</param>
		/// <returns>The dual value.</returns>
		protected Rational GetDualValue(Vector y, int vidRow)
		{
			model.ValidateRowVid(vidRow);
			VidToVarMap vidToVarMap = vidToVars[vidRow];
			if (vidToVarMap.iVar >= 0)
			{
				return GetDualFromDualVector(y, vidRow);
			}
			if (vidToVarMap.kind == VidToVarMapKind.Goal || vidToVarMap.kind == VidToVarMapKind.RowUnbounded)
			{
				return Rational.Zero;
			}
			if (IsSingletonRow(vidRow))
			{
				return SingletonRowDual(y, vidRow);
			}
			if (removedRowsByVid.ContainsKey(vidRow))
			{
				return Rational.Zero;
			}
			return Rational.Indeterminate;
		}

		protected override double GetDualFromDualVector(Vector y, int vid)
		{
			VidToVarMap vidToVarMap = vidToVars[vid];
			double num = (IsGoalMinimize(0) ? vidToVarMap.scale : (0.0 - vidToVarMap.scale));
			return num * y[vidToVarMap.iVar];
		}

		protected bool IsRow(int vid)
		{
			if (model.IsRow(vid))
			{
				return vidToVars[vid].kind != VidToVarMapKind.Goal;
			}
			return false;
		}

		protected bool IsCol(int vid)
		{
			if (!IsRow(vid))
			{
				return !model.IsGoal(vid);
			}
			return false;
		}

		protected bool IsCone(int vid)
		{
			return vidToVars[vid].kind == VidToVarMapKind.RowCone;
		}

		protected bool IsConicRow(int vid)
		{
			if (vidToVars[vid].kind != VidToVarMapKind.RowConicRotated)
			{
				return vidToVars[vid].kind == VidToVarMapKind.RowConicQuadratic;
			}
			return true;
		}
	}
}
