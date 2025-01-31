using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> CentralPathSolver is a partial class to provide a limited name scope.
	/// </summary>
	internal class CentralPathSolver : StandardModel
	{
		/// <summary> Factor and solve using KKT
		/// </summary>
		internal abstract class KKTsystem
		{
			protected CentralPathSolver _solver;

			/// <summary> primal column count
			/// </summary>
			protected int _n;

			/// <summary> primal row count
			/// </summary>
			protected int _m;

			/// <summary> The line-step for z (dual) slacks
			/// </summary>
			protected double[] _δz;

			/// <summary> Capture the exponents of the leading diagonal, used for
			///           tuning pivot order for stability.
			/// </summary>
			protected byte[] _diagonalExponents;

			/// <summary> A constrained pairing helps stabilize.
			/// </summary>
			protected static double _wide;

			/// <summary> The complementarity measure
			/// </summary>
			protected double _µ
			{
				get
				{
					return _solver._mu;
				}
				set
				{
					_solver._mu = value;
				}
			}

			/// <summary> The primal Variables
			/// </summary>
			protected double[] _xVars
			{
				get
				{
					return _solver._x;
				}
				set
				{
					_solver._x = value;
				}
			}

			/// <summary> The dual variables
			/// </summary>
			protected double[] _y
			{
				get
				{
					return _solver._y;
				}
				set
				{
					_solver._y = value;
				}
			}

			/// <summary> The primal complements
			/// </summary>
			protected double[] _zComps
			{
				get
				{
					return _solver._z;
				}
				set
				{
					_solver._z = value;
				}
			}

			/// <summary> The reduced Linear coefficients
			/// </summary>
			protected SparseMatrixDouble _A => _solver._A;

			/// <summary> The reduced Quadratic coefficients
			/// </summary>
			protected SymmetricSparseMatrix _Q => _solver._Q;

			/// <summary> The reduced primal row bounds
			/// </summary>
			protected double[] _b => _solver._b;

			/// <summary> The reduced primal costs
			/// </summary>
			protected double[] _c => _solver._c;

			/// <summary> An upper bound of the non-zeroes anticipated after fill-in
			/// </summary>
			internal abstract int NonZeroesCount { get; }

			/// <summary> If the model is decided during construction, we set this.
			/// </summary>
			internal bool Decided { get; set; }

			/// <summary> This async callback returns true if a task needs to be stopped,
			///           for example by timeout, or if an exception in one thread needs
			///           to stop all the others.
			/// </summary>
			internal abstract Func<bool> CheckAbort { set; }

			/// <summary> KKTsystem is context for factoring and solving
			/// </summary>
			protected KKTsystem(CentralPathSolver solver)
			{
				Decided = false;
				_solver = solver;
				_n = solver._c.Length;
				_m = solver._b.Length;
				_δz = new double[_n];
				_wide = Math.Pow(2.0, 20.0);
			}

			/// <summary>Form the matrix of the KKT and factor it.
			/// </summary>
			internal abstract void FillKKTandFactor(out double condition);

			/// <summary> Initialize x, y, z with a good guess
			/// </summary>
			protected virtual void InitializeValuesSimple()
			{
				int num = _xVars.Length;
				while (0 <= --num)
				{
					_zComps[num] = (_xVars[num] = 1.0 + Math.Abs(_c[num]));
				}
				int num2 = _y.Length;
				while (0 <= --num2)
				{
					_y[num2] = 0.0 - _b[num2];
				}
			}

			/// <summary> If the x, y, z values stray negative they are made
			///           positive by adding a uniform correction factor.
			/// </summary>
			protected virtual void ReviseInitialValues()
			{
				double num = _xVars.Min();
				if (num <= 0.0)
				{
					_xVars.Increment(Math.Max(1.0, num * -1.5));
				}
				double num2 = _zComps.Min();
				if (num2 <= 0.0)
				{
					_zComps.Increment(Math.Max(1.0, num2 * -1.5));
				}
				double num3 = 0.0;
				double num4 = 0.0;
				double num5 = 0.0;
				int num6 = _xVars.Length;
				while (0 <= --num6)
				{
					double num7 = _xVars[num6];
					double num8 = _zComps[num6];
					num3 += num7 * num8;
					num4 += num7;
					num5 += num8;
				}
				_xVars.Increment(Math.Sqrt(num3 / num5));
				_zComps.Increment(Math.Sqrt(num3 / num4));
				double num9 = MeasureDuality(_xVars, _zComps);
				if (num9 < 1000.0)
				{
					double a = Math.Sqrt(1000.0 / (1.0 + num9));
					_xVars.ScaleBy(a);
					_zComps.ScaleBy(a);
				}
			}

			/// <summary> Initialize x, y, z with a good guess
			/// </summary>
			internal abstract void InitializeValues();

			/// <summary> Calculate the RHS for the Affine step.  Sets
			///           _nVec = c + Qx - A*y, and _mVec = b - Ax, and
			///           also builds _rhs = {_nVec, _mVec}
			/// </summary>
			internal abstract void AffineStepRHS();

			/// <summary> Solve the KKT to derive ΔxΔy, Δz, and α prime and α dual.
			///           The corrector, (mu - dxdz), is conditional upon dxdz being present.
			/// </summary>
			internal abstract void SolveAndCalculateStep(double mu, double[] dxdz, bool secondaryCorrection, out double alphaPrime, out double alphaDual, out double muAff);

			/// <summary> Given Δx, Δy, Δz, α for prime and dual, calculate the new x, y, and z.
			/// </summary>
			internal abstract void FinishStep(double alphaPrime, double alphaDual, double mu);

			/// <summary> Given the step for x,y calculate the steps for z.
			///           Use steps aiming to improve centrality.
			/// </summary>
			/// <param name="refine"> should iterative refinement be applied to the matrix solution? </param>
			/// <param name="rate"> 0 &lt;= rate &lt; 0.995 controls rate of convergence </param>
			internal abstract void PredictorCorrectorStep(bool refine, double rate);

			protected static void DumpTriplets(SparseMatrixDouble M)
			{
				for (int i = 0; i < M.ColumnCount; i++)
				{
					SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(M, i);
					while (colIter.IsValid)
					{
						Console.WriteLine("{0}, {1}, {2}", colIter.Row(M), i, colIter.Value(M));
						colIter.Advance();
					}
				}
			}

			protected static void DumpVector(string vName, double[] vec)
			{
				StringBuilder stringBuilder = new StringBuilder(vName);
				stringBuilder.Append('\n');
				for (int i = 0; i < vec.Length; i++)
				{
					stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "{0,9:G6}, ", new object[1] { vec[i] }));
					if (7 == i % 8)
					{
						Console.WriteLine(stringBuilder);
						stringBuilder.Length = 0;
					}
				}
				Console.WriteLine(stringBuilder);
			}
		}

		/// <summary> Factor and solve using a blend of Augmented and Normal form.
		/// </summary>
		protected class BlendedSystem : KKTsystem
		{
			/// <summary> The LLt = ADAt factorization mechanism for Normal form.
			/// </summary>
			internal KktBlendedFactor _blendedKkt;

			internal Vector _x;

			internal Vector _s;

			internal Vector _z_x;

			internal Vector _z_s;

			internal new Vector _y;

			internal Vector _c_x;

			internal Vector _c_s;

			/// <summary> -r_cx + r_zx / x
			/// </summary>
			internal Vector _rhsX;

			/// <summary> -r_cs + r_zs / s
			/// </summary>
			internal Vector _rhsS;

			/// <summary> -rb = b - A (x:s)
			/// </summary>
			internal Vector _rhsB;

			/// <summary> step change in the augmented X
			/// </summary>
			internal Vector _δx;

			/// <summary> step change in the simple X
			/// </summary>
			internal Vector _δs;

			/// <summary> step change in the Y
			/// </summary>
			internal Vector _δy;

			/// <summary> step change in the augmented complements
			/// </summary>
			internal Vector _δz_x;

			/// <summary> step change in the simple complements
			/// </summary>
			internal Vector _δz_s;

			/// <summary> a value 0 &lt;= rate &lt; 0.995 controls the rate of convergence
			/// </summary>
			internal double _rate = 0.98;

			internal int _nRefinements = 5;

			internal SparseMatrixDouble _A_x => _blendedKkt._A_x;

			internal SparseMatrixDouble _A_s => _blendedKkt._A_s;

			internal SymmetricSparseMatrix _Q_x => _blendedKkt._Q_x;

			internal Vector _Q_s => _blendedKkt._Q_s;

			/// <summary> An upper bound of the non-zeroes anticipated after fill-in
			/// </summary>
			internal override int NonZeroesCount => (int)_blendedKkt.Count;

			/// <summary> This async callback returns true if a task needs to be stopped,
			///           for example by timeout, or if an exception in one thread needs
			///           to stop all the others.
			/// </summary>
			internal override Func<bool> CheckAbort
			{
				set
				{
					_blendedKkt.CheckAbort = value;
				}
			}

			/// <summary> only unbounded variables have zero complementarity
			/// </summary>
			private bool IsUnboundedX(int augCol)
			{
				return 0.0 == _z_x[augCol];
			}

			/// <summary> only unbounded variables have zero complementarity
			/// </summary>
			private bool IsUnboundedS(int augCol)
			{
				return 0.0 == _z_s[augCol];
			}

			internal BlendedSystem(CentralPathSolver solver, Func<bool> CheckAbort, SymbolicFactorizationMethod method)
				: base(solver)
			{
				VarSpecial[] array = new VarSpecial[base._c.Length];
				foreach (int unboundedVid in solver._unboundedVids)
				{
					array[solver.GetVar(unboundedVid)] = VarSpecial.Unbounded;
				}
				_blendedKkt = new KktBlendedFactor(base._A, base._Q, array, CheckAbort, new FactorizationParameters(method));
				int[] primalToBlend = _blendedKkt.PrimalToBlend;
				int augCount = _blendedKkt.AugCount;
				_c_x = new Vector(augCount);
				_c_s = new Vector(_blendedKkt.SimpleCount);
				for (int i = 0; i < base._c.Length; i++)
				{
					int num = primalToBlend[i];
					if (num < augCount)
					{
						_c_x[num] = base._c[i];
					}
					else
					{
						_c_s[num - augCount] = base._c[i];
					}
				}
				if (_blendedKkt.IsPurelyQuadraticBlendedSystem())
				{
					base.Decided = true;
				}
				else
				{
					base.Decided = _blendedKkt.Decided;
				}
			}

			/// <summary>Result _KKT is the upper triangular, _L will be the lower.
			/// </summary>
			internal override void FillKKTandFactor(out double condition)
			{
				double perturbation = Math.Max(Math.Pow(2.0, -50.0), Math.Pow(2.0, -20 - _solver.IterationCount));
				_blendedKkt.SetBlendedValues(_z_x, _x, _z_s, _s, perturbation);
				_diagonalExponents = _blendedKkt.DiagonalExponents();
				_blendedKkt.Cholesky();
				condition = 1.0;
			}

			protected void MapAllToPrimal()
			{
				_blendedKkt.MapToPrimal(_x, _s, ref _solver._x);
				_solver._y = _y.ToArray();
				_blendedKkt.MapToPrimal(_z_x, _z_s, ref _solver._z);
			}

			/// <summary> Initialize x, y, z with a good guess
			/// </summary>
			protected override void InitializeValuesSimple()
			{
				InitializeValuesZero();
				int num = _x.Length;
				while (0 <= --num)
				{
					Vector z_x = _z_x;
					int i = num;
					double value = (_x[num] = 1.0 + Math.Abs(_c_x[num]));
					z_x[i] = value;
				}
				int num3 = _s.Length;
				while (0 <= --num3)
				{
					Vector z_s = _z_s;
					int i2 = num3;
					double value2 = (_s[num3] = 1.0 + Math.Abs(_c_s[num3]));
					z_s[i2] = value2;
				}
				int num5 = _y.Length;
				while (0 <= --num5)
				{
					_y[num5] = 0.0 - base._b[num5];
				}
				MapAllToPrimal();
			}

			private void InitializeValuesZero()
			{
				_x = new Vector(_blendedKkt.AugCount);
				_z_x = new Vector(_blendedKkt.AugCount);
				_y = new Vector(_blendedKkt.Ycount);
				_s = new Vector(_blendedKkt.SimpleCount);
				_z_s = new Vector(_blendedKkt.SimpleCount);
			}

			/// <summary> If the x, y, z values stray negative they are made
			///           positive by adding a uniform correction factor.
			/// </summary>
			protected override void ReviseInitialValues()
			{
				double num = double.MaxValue;
				double num2 = num;
				if (!Vector.IsNullOrEmpty(_x))
				{
					num = _x.Min();
					num2 = _z_x.Min();
				}
				if (!Vector.IsNullOrEmpty(_s))
				{
					num = Math.Min(num, _s.Min());
					num2 = Math.Min(num2, _z_s.Min());
				}
				if (num <= 0.0)
				{
					double y = Math.Max(1.0, num * -1.5);
					_x.AddConstant(y);
					_s.AddConstant(y);
				}
				if (num2 <= 0.0)
				{
					double y2 = Math.Max(1.0, num2 * -1.5);
					_z_x.AddConstant(y2);
					_z_s.AddConstant(y2);
				}
				double num3 = 0.0;
				double num4 = 0.0;
				double num5 = 0.0;
				int num6 = _x.Length;
				while (0 <= --num6)
				{
					double num7 = _x[num6];
					double num8 = _z_x[num6];
					num3 += num7 * num8;
					num4 += num7;
					num5 += num8;
				}
				int num9 = _s.Length;
				while (0 <= --num9)
				{
					double num10 = _s[num9];
					double num11 = _z_s[num9];
					num3 += num10 * num11;
					num4 += num10;
					num5 += num11;
				}
				_x.AddConstant(Math.Sqrt(num3 / num5));
				_s.AddConstant(Math.Sqrt(num3 / num5));
				_z_x.AddConstant(Math.Sqrt(num3 / num4));
				_z_s.AddConstant(Math.Sqrt(num3 / num4));
				double num12 = 0.0;
				int num13 = _x.Length;
				while (0 <= --num13)
				{
					num12 += _x[num13] * _z_x[num13];
				}
				int num14 = _s.Length;
				while (0 <= --num14)
				{
					num12 += _s[num14] * _z_s[num14];
				}
				num12 /= (double)(_x.Length + _s.Length);
				if (num12 < 1000.0)
				{
					double y3 = Math.Sqrt(1000.0 / (1.0 + num12));
					_x.ScaleBy(y3);
					_z_x.ScaleBy(y3);
					_s.ScaleBy(y3);
					_z_s.ScaleBy(y3);
				}
			}

			/// <summary> Initialize x, y, z with a good guess
			/// </summary>
			internal override void InitializeValues()
			{
				_solver.Logger.LogEvent(16, "complex columns {0}, simple columns {1}", _blendedKkt.AugCount, _blendedKkt.SimpleCount);
				if (base._A == null)
				{
					InitializeValuesSimple();
					return;
				}
				try
				{
					Vector vector = new Vector(Math.Max(_blendedKkt.AugCount, _blendedKkt.SimpleCount));
					vector.ConstantFill(1.0);
					_blendedKkt.SetBlendedValues(vector, vector, vector, vector, Math.Pow(2.0, -15.0));
					_blendedKkt.Cholesky();
				}
				catch (ArithmeticException)
				{
					InitializeValuesSimple();
					return;
				}
				Vector vector2 = new Vector(_blendedKkt.AugCount + _blendedKkt.Ycount);
				Vector.Copy(new Vector(base._b), 0, vector2, _blendedKkt.AugCount, _blendedKkt.Ycount);
				Vector source = _blendedKkt.Solve(vector2);
				_x = new Vector(_blendedKkt.AugCount);
				Vector.Copy(source, 0, _x, 0, _blendedKkt.AugCount);
				_y = new Vector(_blendedKkt.Ycount);
				Vector.Copy(source, _blendedKkt.AugCount, _y, 0, _blendedKkt.Ycount);
				_s = new Vector(_blendedKkt.SimpleCount);
				if (_A_s != null)
				{
					_A_s.SumLeftProduct(1.0, _y, 0.0, _s);
				}
				if (_Q_s != null)
				{
					for (int i = 0; i < _s.Length; i++)
					{
						_s[i] /= 1.0 + _Q_s[i];
					}
				}
				Vector vector3 = new Vector(_blendedKkt.Ycount);
				if (_A_x != null)
				{
					_A_x.SumProductRight(1.0, _c_x, 0.0, vector3);
				}
				if (_A_s != null)
				{
					_A_s.SumProductRight(1.0, _c_s, 1.0, vector3);
				}
				Vector.Copy(vector3, 0, vector2, _blendedKkt.AugCount, vector3.Length);
				source = _blendedKkt.Solve(vector2);
				Vector.Copy(source, _blendedKkt.AugCount, _y, 0, _y.Length);
				_z_x = _c_x.Copy();
				if (_A_x != null)
				{
					_A_x.SumLeftProduct(-1.0, _y, 1.0, _z_x);
				}
				_z_s = _c_s.Copy();
				if (_A_s != null)
				{
					_A_s.SumLeftProduct(-1.0, _y, 1.0, _z_s);
				}
				ReviseInitialValues();
				for (int j = 0; j < _z_x.Length; j++)
				{
					if (_blendedKkt.IsUnbounded(_blendedKkt.AugToPrimal[j]))
					{
						_z_x[j] = 0.0;
					}
				}
				for (int k = 0; k < _z_s.Length; k++)
				{
					if (_blendedKkt.IsUnbounded(_blendedKkt.SimpleToPrimal[k]))
					{
						_z_s[k] = 0.0;
					}
				}
				MapAllToPrimal();
			}

			/// <summary> Solve the Normal form KKT to derive δx δs δy, δz, and α prime and α dual.
			///           The corrector, (mu - dxdz), is conditional upon dxdz being present.
			/// </summary>
			internal override void SolveAndCalculateStep(double mu, double[] dxdz, bool secondaryCorrection, out double alphaPrime, out double alphaDual, out double muAff)
			{
				throw new NotImplementedException(Resources.NotYetImplemented);
			}

			/// <summary> Calculate the RHS for the Affine step, rhsS is a side-effect.
			/// </summary>
			internal override void AffineStepRHS()
			{
				_rhsX = _c_x.Copy();
				if (_A_x != null)
				{
					_A_x.SumLeftProduct(-1.0, _y, 1.0, _rhsX);
				}
				if (_Q_x != null)
				{
					_Q_x.SumProductRight(1.0, _x, 1.0, _rhsX);
				}
				_rhsS = _c_s.Copy();
				if (_A_s != null)
				{
					_A_s.SumLeftProduct(-1.0, _y, 1.0, _rhsS);
				}
				if (_Q_s != null)
				{
					Vector vector = new Vector(_Q_s.Length);
					Vector.ElementMultiply(_Q_s, _s, vector);
					Vector.Add(_rhsS, vector, _rhsS);
				}
				_rhsB = new Vector(base._b.Clone() as double[]);
				if (_A_x != null)
				{
					_A_x.SumProductRight(-1.0, _x, 1.0, _rhsB);
				}
				if (_A_s != null)
				{
					_A_s.SumProductRight(-1.0, _s, 1.0, _rhsB);
				}
			}

			/// <summary> Calculate the RHS for the Corrector step, starting from Affine.
			/// </summary>
			internal void CorrectorStepRHS()
			{
				for (int i = 0; i < _rhsX.Length; i++)
				{
					if (!IsUnboundedX(i))
					{
						_rhsX[i] += (_δx[i] * _δz_x[i] - base._µ) / _x[i];
					}
				}
				for (int j = 0; j < _rhsS.Length; j++)
				{
					if (!IsUnboundedS(j))
					{
						_rhsS[j] += (_δs[j] * _δz_s[j] - base._µ) / _s[j];
					}
				}
			}

			/// <summary> Solve the Blended form KKT to derive δx δs δy.
			///           The outputs are additive to allow for iteration of residuals.
			/// </summary>
			private void SolveForXYS(Vector rhsXY, Vector inverseQplusZbyS, Vector d_norm, bool refine)
			{
				Vector vector = _blendedKkt.Solve(rhsXY);
				for (int i = 0; i < _blendedKkt.AugCount; i++)
				{
					_δx[i] += vector[i];
				}
				for (int j = 0; j < _δy.Length; j++)
				{
					_δy[j] += vector[j + _blendedKkt.AugCount];
				}
				Vector vector2;
				if (!refine)
				{
					vector2 = _δy;
				}
				else if (_blendedKkt.AugCount == 0)
				{
					vector2 = vector;
				}
				else
				{
					vector2 = new Vector(_δy.Length);
					Vector.Copy(vector, _blendedKkt.AugCount, vector2, 0, vector2.Length);
				}
				if (_A_s != null)
				{
					Vector vector3 = new Vector(_blendedKkt.SimpleCount);
					_A_s.SumLeftProduct(1.0, vector2, 1.0, vector3);
					vector3.ElementMultiply(inverseQplusZbyS);
					_δs.Add(vector3).Add(d_norm);
				}
			}

			/// <summary> Solve the Blended form KKT to derive δx δs δy.
			/// </summary>
			internal void SolveForXYS(Vector inverseQplusZbyS, int nRefinements)
			{
				_δx = new Vector(_blendedKkt.AugCount);
				_δs = new Vector(_blendedKkt.SimpleCount);
				_δy = new Vector(_blendedKkt.Ycount);
				Vector vector = _rhsX.Copy();
				Vector vector2 = _rhsS.Copy();
				Vector vector3 = _rhsB.Copy();
				bool refine = false;
				double num = 1E+100;
				int num2 = 0;
				double num6;
				do
				{
					Vector vector4 = new Vector(vector2.Length);
					Vector.ElementMultiply(vector2, inverseQplusZbyS, vector4);
					vector4.ScaleBy(-1.0);
					Vector vector5 = vector3.Copy();
					if (_A_s != null)
					{
						_A_s.SumProductRight(-1.0, vector4, 1.0, vector5);
					}
					Vector rhsXY = new Vector(vector, vector5);
					SolveForXYS(rhsXY, inverseQplusZbyS, vector4, refine);
					num2++;
					if (nRefinements < num2)
					{
						break;
					}
					refine = true;
					if (1 < num2)
					{
						vector3 = _rhsB.Copy();
					}
					if (0 < _blendedKkt.AugCount)
					{
						if (1 < num2)
						{
							vector = _rhsX.Copy();
						}
						for (int i = 0; i < vector.Length; i++)
						{
							if (!IsUnboundedX(i))
							{
								vector[i] += _z_x[i] * _δx[i] / _x[i];
							}
						}
						if (_Q_x != null)
						{
							_Q_x.SumProductRight(1.0, _δx, 1.0, vector);
						}
						if (_A_x != null)
						{
							_A_x.SumLeftProduct(-1.0, _δy, 1.0, vector);
							_A_x.SumProductRight(-1.0, _δx, 1.0, vector3);
						}
					}
					if (_A_s != null)
					{
						if (1 < num2)
						{
							vector2 = _rhsS.Copy();
						}
						for (int j = 0; j < vector2.Length; j++)
						{
							if (!IsUnboundedS(j))
							{
								vector2[j] += _z_s[j] * _δs[j] / _s[j];
							}
						}
						if (_Q_s != null)
						{
							Vector vector6 = new Vector(_Q_s.Length);
							Vector.ElementMultiply(_Q_s, _δs, vector6);
							Vector.Add(vector2, vector6, vector2);
						}
						_A_s.SumLeftProduct(-1.0, _δy, 1.0, vector2);
						_A_s.SumProductRight(-1.0, _δs, 1.0, vector3);
					}
					double num3 = vector.NormInf();
					double num4 = vector2.NormInf();
					double num5 = vector3.NormInf();
					num6 = num3 + num4 + num5;
					double num7 = num6 / num;
					if (1.0 <= num7)
					{
						if (4.0 < num7)
						{
							_nRefinements--;
						}
					}
					else
					{
						num = num6;
					}
				}
				while (0.0001 < num6 && num2 <= nRefinements);
			}

			/// <summary> The calculation for affine δz[i].
			/// </summary>
			private static double PredictorElementZ(double xi, double dxi, double zi, ref double alphaPrime, ref double alphaDual, ref double muAff)
			{
				double num = (0.0 - zi) * (1.0 + dxi / xi);
				alphaPrime = Math.Min(dxi / xi, alphaPrime);
				alphaDual = Math.Min(num / zi, alphaDual);
				muAff += (xi + dxi) * (zi + num);
				return num;
			}

			/// <summary> Solve the Normal form KKT to derive δx δs δy, δz, and α prime and α dual.
			/// </summary>
			internal void SolvePredictorZ(out double alphaPrime, out double alphaDual, out double muAff)
			{
				alphaPrime = -1.0;
				alphaDual = -1.0;
				muAff = 0.0;
				int num = _x.Length + _s.Length;
				_δz_x = new Vector(_blendedKkt.AugCount);
				for (int i = 0; i < _δz_x.Length; i++)
				{
					if (!IsUnboundedX(i))
					{
						_δz_x[i] = PredictorElementZ(_x[i], _δx[i], _z_x[i], ref alphaPrime, ref alphaDual, ref muAff);
					}
					else
					{
						num--;
					}
				}
				if (_δz_s == null)
				{
					_δz_s = new Vector(_blendedKkt.SimpleCount);
				}
				for (int j = 0; j < _δz_s.Length; j++)
				{
					if (!IsUnboundedS(j))
					{
						_δz_s[j] = PredictorElementZ(_s[j], _δs[j], _z_s[j], ref alphaPrime, ref alphaDual, ref muAff);
					}
					else
					{
						num--;
					}
				}
				if (0 < num)
				{
					muAff /= num;
				}
				alphaPrime = -1.0 / alphaPrime;
				alphaDual = -1.0 / alphaDual;
				alphaPrime *= (_rate + alphaPrime) / 2.0;
				alphaDual *= (_rate + alphaDual) / 2.0;
			}

			/// <summary> The calculation for predictor δz[i].
			/// </summary>
			private static double CorrectorElementZ(double muMinusDxdz, double xi, double dxi, double zi, ref double alphaPrime, ref double alphaDual, ref double muNext)
			{
				double num = (muMinusDxdz - zi * (xi + dxi)) / xi;
				alphaPrime = Math.Min(dxi / xi, alphaPrime);
				alphaDual = Math.Min(num / zi, alphaDual);
				muNext += (xi + dxi) * (zi + num);
				return num;
			}

			/// <summary> Solve the Normal form KKT to derive δx δs δy, δz, and α prime and α dual.
			///           The corrector, (mu - dxdz), is conditional upon dxdz being present.
			/// </summary>
			internal void SolveCorrectorZ(double mu, Vector dxdza, Vector dsdza, out double alphaPrime, out double alphaDual, out double muNext)
			{
				alphaPrime = -1.0;
				alphaDual = -1.0;
				muNext = 0.0;
				int num = _x.Length + _s.Length;
				for (int i = 0; i < _δz_x.Length; i++)
				{
					if (!IsUnboundedX(i))
					{
						_δz_x[i] = CorrectorElementZ(mu - dxdza[i], _x[i], _δx[i], _z_x[i], ref alphaPrime, ref alphaDual, ref muNext);
					}
					else
					{
						num--;
					}
				}
				for (int j = 0; j < _δz_s.Length; j++)
				{
					if (!IsUnboundedS(j))
					{
						_δz_s[j] = CorrectorElementZ(mu - dsdza[j], _s[j], _δs[j], _z_s[j], ref alphaPrime, ref alphaDual, ref muNext);
					}
					else
					{
						num--;
					}
				}
				if (0 < num)
				{
					muNext /= num;
				}
				alphaPrime = -1.0 / alphaPrime;
				alphaDual = -1.0 / alphaDual;
				alphaPrime *= (_rate + alphaPrime) / 2.0;
				alphaDual *= (_rate + alphaDual) / 2.0;
			}

			/// <summary> Limit the new x:s, and z.
			/// </summary>
			internal static void BoundComplement(Vector x, Vector z, int i, double mu)
			{
				double num = x[i];
				double num2 = z[i];
				if (num * KKTsystem._wide < num2)
				{
					if (num2 * num < mu / 2.0)
					{
						x[i] = mu / num2;
					}
				}
				else if (num2 * KKTsystem._wide < num && num * num2 < mu / 2.0)
				{
					z[i] = mu / num;
				}
			}

			/// <summary> Given δx, δs, δy, δz, α for prime and dual, calculate the new x, s, y, and z.
			/// </summary>
			internal override void FinishStep(double alphaPrime, double alphaDual, double mu)
			{
				for (int i = 0; i < _x.Length; i++)
				{
					_x[i] += alphaPrime * _δx[i];
					if (!IsUnboundedX(i))
					{
						_z_x[i] += alphaDual * _δz_x[i];
						BoundComplement(_x, _z_x, i, mu);
					}
				}
				for (int j = 0; j < _s.Length; j++)
				{
					_s[j] += alphaPrime * _δs[j];
					if (!IsUnboundedS(j))
					{
						_z_s[j] += alphaDual * _δz_s[j];
						BoundComplement(_s, _z_s, j, mu);
					}
				}
				KKTsystem._wide = Math.Min(Math.Pow(2.0, 40.0), 2.0 * KKTsystem._wide);
				for (int k = 0; k < _y.Length; k++)
				{
					_y[k] += alphaDual * _δy[k];
				}
			}

			/// <summary> Given the step for x,y calculate the steps for z.
			///           Use steps aiming to improve centrality.
			/// </summary>
			/// <param name="refine"> should iterative refinement be applied to the matrix solution? </param>
			/// <param name="rate"> 0 &lt;= rate &lt; 0.995 controls rate of convergence </param>
			internal override void PredictorCorrectorStep(bool refine, double rate)
			{
				_rate = rate;
				Vector vector = _s.Copy();
				if (_Q_s != null)
				{
					for (int i = 0; i < _s.Length; i++)
					{
						vector[i] /= _s[i] * _Q_s[i] + _z_s[i];
					}
				}
				else
				{
					vector.ElementDivide(_z_s);
				}
				AffineStepRHS();
				SolveForXYS(vector, refine ? Math.Min(1, _nRefinements) : 0);
				SolvePredictorZ(out var alphaPrime, out var alphaDual, out var muAff);
				base._µ *= Math.Pow(1.0 - Math.Min(alphaDual, alphaPrime), 3.0);
				Vector vector2 = new Vector(_δx.Length);
				Vector.ElementMultiply(_δx, _δz_x, vector2);
				Vector vector3 = new Vector(_δs.Length);
				Vector.ElementMultiply(_δs, _δz_s, vector3);
				CorrectorStepRHS();
				SolveForXYS(vector, refine ? _nRefinements : 0);
				SolveCorrectorZ(base._µ, vector2, vector3, out alphaPrime, out alphaDual, out muAff);
				FinishStep(alphaPrime, alphaDual, muAff);
				MapAllToPrimal();
			}

			/// <summary> Find a closed form solution for a quadratic problem.
			/// </summary>
			public bool SolveTrivialQuadratic()
			{
				if (_blendedKkt.IsPurelyQuadraticBlendedSystem())
				{
					return SolvePurelyQuadraticBlendedSystem();
				}
				if (!Vector.IsNullOrEmpty(_blendedKkt._Q_s))
				{
					return SolveDiagonalQuadraticNoConstraints();
				}
				return false;
			}

			/// <summary> Find the closed form solution when A is null Q involves unbounded variables only.
			/// In this case we can simply solve for x directly using Q.
			/// </summary>
			private bool SolvePurelyQuadraticBlendedSystem()
			{
				InitializeValuesZero();
				try
				{
					Vector vector = new Vector(Math.Max(_blendedKkt.AugCount, _blendedKkt.SimpleCount));
					_blendedKkt.SetBlendedValues(vector, vector, vector, vector, Math.Pow(2.0, -15.0));
					_blendedKkt.Cholesky();
				}
				catch (ArithmeticException)
				{
					return false;
				}
				Vector vector2 = new Vector(_blendedKkt.AugCount);
				Vector.Copy(new Vector(base._c), 0, vector2, 0, _blendedKkt.AugCount);
				Vector y = _blendedKkt.Solve(vector2);
				_x.CopyFrom(y);
				MapAllToPrimal();
				return true;
			}

			/// <summary> Find the closed form solution when A is null and there are only diagonal quadratic entries.
			/// </summary>
			private bool SolveDiagonalQuadraticNoConstraints()
			{
				InitializeValuesZero();
				for (int i = 0; i < _Q_s.Length; i++)
				{
					double num = (0.0 - _c_s[i]) / _Q_s[i];
					_s[i] = (_blendedKkt.IsUnbounded(i) ? num : Math.Max(num, 0.0));
				}
				MapAllToPrimal();
				for (int j = 0; j < _c_x.Length; j++)
				{
					if (Math.Abs(_c_x[j]) >= 1E-12)
					{
						return false;
					}
				}
				return true;
			}
		}

		private BlendedSystem _kktSys;

		/// <summary>Called at the start of each iteration.
		/// </summary>
		internal Func<bool> IterationStartedCallback;

		/// <summary> The mix ratio for the log barrier functions, also
		///           known as the complementarity measure
		/// </summary>
		private double _mu;

		/// <summary> Duality gap.
		/// </summary>
		private double _gap;

		/// <summary>Iteration count.
		/// </summary>
		private int _iterations;

		/// <summary>The KKT formulation used by the algorithm.
		/// </summary>
		private InteriorPointKktForm _ipmKktForm = InteriorPointKktForm.Augmented;

		private int _maxIterationCount = 100;

		private double _epsApprox = Math.Pow(2.0, -14.0);

		private double _epsFine = Math.Pow(2.0, -30.0);

		protected override double SolveTolerance => _epsApprox;

		/// <summary>The algorithm kind.
		/// </summary>
		public override InteriorPointAlgorithmKind Algorithm => InteriorPointAlgorithmKind.PredictorCorrector;

		/// <summary>The KKT formulation used by the algorithm.
		/// </summary>
		public override InteriorPointKktForm KktForm => _ipmKktForm;

		/// <summary> Duality gap.
		/// </summary>
		public override double Gap => _gap;

		/// <summary>Iteration count.
		/// </summary>
		public override int IterationCount => _iterations;

		/// <summary> Creates a new instance.
		/// </summary>
		/// <param name="solver">The object containing the user model.</param>
		/// <param name="logger">The LogSource.</param>
		/// <param name="qpModel">A compact matrix of quadratic coeffs indexed by Qid.</param>
		/// <param name="mpvidQid">vid to Qid, zero means not used in the quadratic.</param>
		/// <param name="presolveLevel">Presolve level.</param>
		internal CentralPathSolver(InteriorPointSolver solver, LogSource logger, CoefMatrix qpModel, int[] mpvidQid, int presolveLevel)
			: base(solver, logger, qpModel, mpvidQid, presolveLevel)
		{
			_primal = (_dual = (_gap = double.NaN));
		}

		/// <summary>Result compute xQx/2.
		/// </summary>
		private BigSum Half_xQx()
		{
			BigSum bigSum = 0.0;
			if (_Q != null)
			{
				for (int i = 0; i < _Q.ColumnCount; i++)
				{
					foreach (KeyValuePair<int, double> item in _Q.AllValuesInColumn(i))
					{
						bigSum.Add(_x[i] * item.Value * _x[item.Key]);
					}
				}
			}
			return bigSum / 2;
		}

		/// <summary> Calculate the duality measure (Nocedal and Wright 2nd ed, 14.6)
		/// </summary>
		/// <param name="x"> primal vars </param>
		/// <param name="z"> primal slacks </param>
		private static double MeasureDuality(double[] x, double[] z)
		{
			int num = x.Length;
			double num2 = 0.0;
			int num3 = x.Length;
			while (0 <= --num3)
			{
				if (0.0 != z[num3])
				{
					num2 += x[num3] * z[num3];
				}
				else
				{
					num--;
				}
			}
			if (0 < num)
			{
				num2 /= (double)num;
			}
			return num2;
		}

		/// <summary> z = x[]·y[] -- (extension method)
		/// </summary>
		/// <returns> inner (dot) product of x and y </returns>
		private static BigSum BigInnerProduct(double[] x, double[] y)
		{
			BigSum result = 0.0;
			int num = x.Length;
			while (0 <= --num)
			{
				result.Add(x[num] * y[num]);
			}
			return result;
		}

		/// <summary> Choose Augmented or Normal, and get an initial value
		/// </summary>
		private bool InitializeCentralPath(InteriorPointSolverParams prm)
		{
			IterationStartedCallback = prm.IterationStartedCallback;
			_ = _A;
			_ipmKktForm = InteriorPointKktForm.Blended;
			prm.NotifyStartSolve(0);
			_maxIterationCount = ((prm.MaxIterationCount < 0) ? int.MaxValue : prm.MaxIterationCount);
			base.Logger.LogEvent(16, Resources.InitialNonZerosA0Q1, (_A == null) ? 0 : _A.Count, (_Q == null) ? 0 : _Q.Count);
			bool flag = false;
			try
			{
				base.SolveState = InteriorPointSolveState.SymbolicFactorization;
				SymbolicFactorizationMethod method = ((prm.SymbolicOrdering >= 0) ? ((SymbolicFactorizationMethod)prm.SymbolicOrdering) : SymbolicFactorizationMethod.LocalFill);
				_kktSys = new BlendedSystem(this, delegate
				{
					Solving(prm, InteriorPointSolveState.Init);
					return prm.ShouldAbort();
				}, method);
				if (_kktSys.Decided)
				{
					SolveTrivialQuadratic();
				}
				else
				{
					base.Logger.LogEvent(16, Resources.AfterFillNonZeros0, _kktSys.NonZeroesCount);
					if (_kktSys._Q_x != null)
					{
						QuadraticFactorWorkspace quadraticFactorWorkspace = new QuadraticFactorWorkspace(_kktSys._Q_x, () => false);
						quadraticFactorWorkspace.SetWorkspaceValues();
						quadraticFactorWorkspace.Cholesky();
					}
					_kktSys.InitializeValues();
				}
			}
			catch (DivideByZeroException innerException)
			{
				base.Logger.LogEvent(16, Resources.ModelNotConvex);
				throw new InvalidModelDataException(Resources.ModelNotConvex, innerException);
			}
			catch (TimeLimitReachedException)
			{
				flag = true;
			}
			if (flag)
			{
				_kktSys = null;
				return false;
			}
			if (base.DirectSolution)
			{
				return false;
			}
			_mu = MeasureDuality(_x, _z);
			return true;
		}

		protected void SolveTrivialQuadratic()
		{
			if (_x == null)
			{
				_x = new double[_c.Length];
			}
			if (_kktSys.SolveTrivialQuadratic())
			{
				base.Solution.status = LinearResult.Optimal;
			}
			else
			{
				base.Solution.status = LinearResult.UnboundedPrimal;
			}
			_gap = 0.0;
			FinalValues();
			base.DirectSolution = true;
		}

		private bool IterationStarted()
		{
			_gap = Math.Abs((_dual - _primal).ToDouble());
			base.SolveState = InteriorPointSolveState.IterationStarted;
			if (IterationStartedCallback != null)
			{
				return !IterationStartedCallback();
			}
			return true;
		}

		/// <summary> Run Central Path iterations
		/// </summary>
		private LinearResult SolveCentralPath(InteriorPointSolverParams prm)
		{
			_primal = BigInnerProduct(_x, _c) + Half_xQx();
			double num = double.PositiveInfinity;
			_gap = 1E+100;
			double gap = Gap;
			double num2 = _mu;
			double num3 = 1E+20;
			_iterations = 1;
			double rate = 0.9;
			double[] y = null;
			double[] y2 = null;
			double[] y3 = null;
			int num4 = _maxIterationCount - 25;
			double gap2 = Gap;
			InitPrimalDual();
			while (gap >= Gap / num3 && num > _epsFine && _iterations < num4 + 20 && _iterations < _maxIterationCount && !prm.ShouldAbort())
			{
				Solving(prm, InteriorPointSolveState.IterationStarted);
				base.Logger.LogEvent(16, Resources.PredictorLoopMuGap, _iterations, _mu, Gap);
				double condition = 0.0;
				bool flag = false;
				try
				{
					if (!IterationStarted())
					{
						break;
					}
					_kktSys.FillKKTandFactor(out condition);
					bool refine = 10 <= _iterations || (gap < Gap && 5 <= _iterations);
					_kktSys.PredictorCorrectorStep(refine, rate);
					_mu = MeasureDuality(_x, _z);
					BigSum bigSum = Half_xQx();
					_dual = BigInnerProduct(_y, _b);
					_primal = BigInnerProduct(_x, _c);
					double num5 = Math.Min(Math.Abs(_dual.ToDouble()), Math.Abs(_primal.ToDouble()));
					_dual += _constantCost - bigSum;
					_primal += _constantCost + bigSum;
					_gap = Math.Abs((_dual - _primal).ToDouble());
					num = Gap / (1.0 + num5);
					goto IL_021c;
				}
				catch (DivideByZeroException)
				{
					flag = true;
					goto IL_021c;
				}
				catch (TimeLimitReachedException)
				{
					flag = true;
					goto IL_021c;
				}
				IL_021c:
				if (flag)
				{
					break;
				}
				rate = 0.97;
				num2 = Math.Min(_mu, num2);
				if (Gap < gap)
				{
					gap = Gap;
					if (Gap * 17.0 < gap2 * 16.0)
					{
						num4 = Math.Max(30, IterationCount);
						gap2 = Gap;
					}
					if (_mu < 1.0)
					{
						_x.CopyOver(ref y);
						_y.CopyOver(ref y2);
						_z.CopyOver(ref y3);
					}
				}
				num3 = (1000000.0 + num2) / 10.0;
				_iterations++;
			}
			if (gap < Gap && y != null)
			{
				_x = y;
				_y = y2;
				_z = y3;
				_gap = gap;
			}
			FinalValues();
			_kktSys = null;
			base.Solution.status = ((!(Gap < _epsApprox) && !(Gap < Math.Abs(_primal.ToDouble()) * _epsApprox)) ? LinearResult.Interrupted : LinearResult.Optimal);
			return base.Solution.status;
		}

		private void Solving(InteriorPointSolverParams prm, InteriorPointSolveState state)
		{
			base.SolveState = state;
			if (prm.Solving != null)
			{
				prm.Solving();
			}
		}

		private void InitPrimalDual()
		{
			BigSum bigSum = Half_xQx();
			_dual = BigInnerProduct(_y, _b);
			_primal = BigInnerProduct(_x, _c);
			_dual += _constantCost - bigSum;
			_primal += _constantCost + bigSum;
		}

		/// <summary>solve problems of form Ax &gt;= b, 0 &lt;= x, minimize &lt;c,x&gt;.
		/// </summary>
		/// <param name="prm"> solver parameters </param>
		public override LinearResult Solve(InteriorPointSolverParams prm)
		{
			if (base.DirectSolution || ((VarCount == 0 || base.GoalIsUnbounded) && base.Solution != null))
			{
				return base.Solution.status;
			}
			if (_A == null && _Q == null)
			{
				if (base.Solution == null)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.TwoAreNull01, new object[2] { "A", "Q" }));
				}
				return base.Solution.status;
			}
			_solution = new StandardSolution();
			if (0 < base.InfeasibleRowVarPairs.Count)
			{
				_dual = (_primal = (_gap = double.NaN));
				base.Solution.cx = double.NaN;
				base.Solution.by = double.NaN;
				base.Solution.status = LinearResult.InfeasiblePrimal;
				return base.Solution.status;
			}
			if (!InitializeCentralPath(prm))
			{
				if (!base.DirectSolution)
				{
					return LinearResult.Interrupted;
				}
				return base.Solution.status;
			}
			return SolveCentralPath(prm);
		}

		/// <summary> Return the dual value for a row constraint.
		/// </summary>
		/// <param name="vidRow">Row vid.</param>
		/// <returns>
		/// Returns the dual value.  If the constraint has both upper and lower bounds
		/// then there are actually two dual values.  In this case the dual for the active bound (if any) will be returned.
		/// </returns>
		public override Rational GetDualValue(int vidRow)
		{
			if (base.Solution.status != LinearResult.Optimal)
			{
				return Rational.Indeterminate;
			}
			return GetDualValue(_y, vidRow);
		}
	}
}
