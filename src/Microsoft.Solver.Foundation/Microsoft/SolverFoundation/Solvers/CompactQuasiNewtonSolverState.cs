using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Contains information about the state of the solver
	/// </summary>
	internal class CompactQuasiNewtonSolverState
	{
		/// <summary>
		/// data for interpolation
		/// </summary>
		private struct PointInterpolationData
		{
			public double alpha;

			public double value;

			public double deriv;

			public PointInterpolationData(double alpha, double value, double deriv)
			{
				if (double.IsInfinity(alpha) || double.IsInfinity(value) || double.IsInfinity(deriv) || double.IsNaN(alpha) || double.IsNaN(value) || double.IsNaN(deriv))
				{
					throw new CompactQuasiNewtonException(CompactQuasiNewtonErrorType.NumericLimitExceeded);
				}
				this.alpha = alpha;
				this.value = value;
				this.deriv = deriv;
			}
		}

		private const double UnitRoundoff = 1.1E-16;

		private const double GradVicinityToZero = 0.001;

		private double[] _point;

		private double[] _grad;

		private double[] _newPoint;

		private double[] _newGrad;

		private double[] _direction;

		private double _value;

		private double _newValue;

		private int _iter;

		private long _evaluationCount;

		/// <summary>
		/// The function being optimized
		/// </summary>
		private readonly Func<double[], double[], double> _function;

		private readonly bool _minimize;

		private readonly CompactQuasiNewtonSolverParams _solverParams;

		/// <summary>
		/// The dimensionality of the function
		/// </summary>
		private readonly int _dimensions;

		private readonly int _m;

		private List<double[]> _sList;

		private List<double[]> _yList;

		private List<double> _roList;

		/// <summary>
		/// The number of iterations so far
		/// </summary>
		public int Iter => _iter;

		/// <summary>
		/// The current function value
		/// </summary>
		public double Value => _newValue;

		/// <summary>
		/// The current function value modified, so if it is 
		/// maximize problem will return the negative of that
		/// </summary>
		public double ModifiedValue
		{
			get
			{
				if (_minimize)
				{
					return _newValue;
				}
				return 0.0 - _newValue;
			}
		}

		/// <summary>
		/// The function value at the last point
		/// </summary>
		internal double LastValue => _value;

		/// <summary>
		/// The current point being explored
		/// </summary>
		public double[] Point => _newPoint;

		/// <summary>
		/// Count of evaluation calls
		/// </summary>
		public long EvaluationCount => _evaluationCount;

		/// <summary>
		/// Creating a state
		/// </summary>
		/// <param name="function"></param>
		/// <param name="solverParams"></param>
		/// <param name="minimize"></param>
		/// <param name="dimensions"></param>
		/// <param name="startingPoint"></param>
		public CompactQuasiNewtonSolverState(Func<double[], double[], double> function, CompactQuasiNewtonSolverParams solverParams, bool minimize, int dimensions, double[] startingPoint)
		{
			DebugContracts.NonNull(startingPoint);
			DebugContracts.NonNull(function);
			DebugContracts.NonNull(solverParams);
			_m = solverParams.IterationsToRemember;
			_dimensions = dimensions;
			_point = new double[dimensions];
			startingPoint.CopyTo(_point, 0);
			_grad = new double[dimensions];
			_direction = new double[dimensions];
			_newPoint = new double[dimensions];
			_newGrad = new double[dimensions];
			_sList = new List<double[]>(_m);
			_yList = new List<double[]>(_m);
			_roList = new List<double>(_m);
			_solverParams = solverParams;
			_minimize = minimize;
			_function = function;
			_iter = 1;
			_evaluationCount = 0L;
			_newValue = (_value = CallFunction(_point, _grad));
		}

		/// <summary>
		/// called fron the solver for first iteration 
		/// and from state as part of shifting 
		/// </summary>
		public virtual void UpdateDir()
		{
			_direction.ScaleIntoMe(_grad, -1.0);
			MapDirByInverseHessian();
		}

		/// <summary>
		/// two loop recursion (7.4)
		/// ro is 1/ro
		/// alpha is -alpha
		/// dir start as q (first loop) and then become r (second loop)
		/// </summary>
		private void MapDirByInverseHessian()
		{
			int count = _sList.Count;
			if (count != 0)
			{
				double[] array = new double[count];
				for (int num = count - 1; num >= 0; num--)
				{
					array[num] = (0.0 - _sList[num].InnerProduct(_direction)) / _roList[num];
					_direction.AddScaledVector(_yList[num], array[num]);
				}
				double num2 = _yList[count - 1].InnerProduct(_yList[count - 1]);
				if (num2 == 0.0)
				{
					throw new CompactQuasiNewtonException(CompactQuasiNewtonErrorType.GradientDeltaIsZero);
				}
				_direction.ScaleBy(_roList[count - 1] / num2);
				for (int i = 0; i < count; i++)
				{
					double num3 = _yList[i].InnerProduct(_direction) / _roList[i];
					_direction.AddScaledVector(_sList[i], 0.0 - array[i] - num3);
				}
			}
		}

		/// <summary>
		/// update the state for next move (add s and y) and discard old vectors
		/// </summary>
		public virtual void Shift()
		{
			double[] array;
			double[] array2;
			if (_sList.Count == _m)
			{
				array = _sList[0];
				_sList.RemoveAt(0);
				array2 = _yList[0];
				_yList.RemoveAt(0);
				_roList.RemoveAt(0);
			}
			else
			{
				array = new double[_dimensions];
				array2 = new double[_dimensions];
			}
			array.SubtractInto(_newPoint, _point);
			array2.SubtractInto(_newGrad, _grad);
			double num = array.InnerProduct(array2);
			_sList.Add(array);
			_yList.Add(array2);
			_roList.Add(num);
			Swap(ref _value, ref _newValue);
			Swap(ref _point, ref _newPoint);
			Swap(ref _grad, ref _newGrad);
			_iter++;
			if (num == 0.0)
			{
				throw new CompactQuasiNewtonException(CompactQuasiNewtonErrorType.YIsOrthogonalToS);
			}
		}

		/// <summary>
		/// swaps a and b
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="a"></param>
		/// <param name="b"></param>
		private static void Swap<T>(ref T a, ref T b)
		{
			T val = a;
			a = b;
			b = val;
		}

		/// <summary>
		/// An implementation of the line search for the Wolfe conditions, from Nocedal and Wright
		/// </summary>
		public virtual void LineSearch()
		{
			double num = _direction.InnerProduct(_grad);
			if (num > 0.0)
			{
				_newGrad = _grad;
				_newPoint = _point;
				_newValue = _value;
				throw new CompactQuasiNewtonException(CompactQuasiNewtonErrorType.NonDescentDirection);
			}
			double num2 = _direction.Norm2();
			double num3 = 0.0001 * num;
			double num4 = 0.9 * num;
			if (num2 == 0.0)
			{
				_point.CopyOver(ref _newPoint);
				_newValue = _value;
				return;
			}
			double num5 = ((_iter == 1) ? Math.Min(1.0, 1.0 / num2) : 1.0);
			PointInterpolationData pointInterpolationData = new PointInterpolationData(0.0, _value, num);
			PointInterpolationData aLo = default(PointInterpolationData);
			PointInterpolationData aHi = default(PointInterpolationData);
			bool flag = false;
			while (true)
			{
				if (_solverParams.ShouldAbort())
				{
					_newPoint = _point;
					throw new CompactQuasiNewtonException(CompactQuasiNewtonErrorType.Interrupted);
				}
				_point.CopyOver(ref _newPoint);
				_newPoint.AddScaledVector(_direction, num5);
				_newValue = CallFunction(_newPoint, _newGrad);
				num = _direction.InnerProduct(_newGrad);
				if (double.IsInfinity(_newValue) || double.IsPositiveInfinity(num5))
				{
					if (VectorUtility.AreExactlyTheSame(_newGrad, _grad) && _newGrad.Norm2() != 0.0)
					{
						throw new CompactQuasiNewtonException(CompactQuasiNewtonErrorType.GradientDeltaIsZero);
					}
					throw new CompactQuasiNewtonException(CompactQuasiNewtonErrorType.NumericLimitExceeded);
				}
				PointInterpolationData pointInterpolationData2 = new PointInterpolationData(num5, _newValue, num);
				if (pointInterpolationData2.value > _value + num3 * num5 || (pointInterpolationData.alpha > 0.0 && pointInterpolationData2.value >= pointInterpolationData.value))
				{
					aLo = pointInterpolationData;
					aHi = pointInterpolationData2;
					break;
				}
				if (Math.Abs(pointInterpolationData2.deriv) <= 0.0 - num4)
				{
					flag = true;
					break;
				}
				if (pointInterpolationData2.deriv >= 0.0)
				{
					aLo = pointInterpolationData2;
					aHi = pointInterpolationData;
					break;
				}
				pointInterpolationData = pointInterpolationData2;
				num5 *= 2.0;
			}
			if (!flag)
			{
				Zoom(aLo, aHi, num, num3, num4);
			}
		}

		/// <summary>
		/// "zoom" procedure (algirithm 3.6)
		/// </summary>
		/// <param name="aLo"></param>
		/// <param name="aHi"></param>
		/// <param name="dirDeriv"></param>
		/// <param name="c1"></param>
		/// <param name="c2"></param>
		private void Zoom(PointInterpolationData aLo, PointInterpolationData aHi, double dirDeriv, double c1, double c2)
		{
			double num = 0.01;
			bool flag = false;
			while (!flag)
			{
				if (_solverParams.ShouldAbort())
				{
					_newPoint = _point;
					throw new CompactQuasiNewtonException(CompactQuasiNewtonErrorType.Interrupted);
				}
				PointInterpolationData pointInterpolationData = ((aLo.alpha < aHi.alpha) ? aLo : aHi);
				PointInterpolationData pointInterpolationData2 = ((aLo.alpha < aHi.alpha) ? aHi : aLo);
				double num2 = ((!(pointInterpolationData.deriv > 0.0) || !(pointInterpolationData2.deriv < 0.0)) ? CubicInterp(aLo, aHi) : ((aLo.value < aHi.value) ? aLo.alpha : aHi.alpha));
				double num3 = num * pointInterpolationData.alpha + (1.0 - num) * pointInterpolationData2.alpha;
				if (num2 > num3)
				{
					num2 = num3;
				}
				double num4 = num * pointInterpolationData2.alpha + (1.0 - num) * pointInterpolationData.alpha;
				if (num2 < num4)
				{
					num2 = num4;
				}
				_point.CopyOver(ref _newPoint);
				_newPoint.AddScaledVector(_direction, num2);
				_newValue = CallFunction(_newPoint, _newGrad);
				dirDeriv = _direction.InnerProduct(_newGrad);
				PointInterpolationData pointInterpolationData3 = new PointInterpolationData(num2, _newValue, dirDeriv);
				if (pointInterpolationData3.value > _value + c1 * num2 || pointInterpolationData3.value >= aLo.value)
				{
					aHi = pointInterpolationData3;
				}
				else if (Math.Abs(pointInterpolationData3.deriv) <= 0.0 - c2)
				{
					flag = true;
				}
				else
				{
					if (pointInterpolationData3.deriv * (aHi.alpha - aLo.alpha) >= 0.0)
					{
						aHi = aLo;
					}
					aLo = pointInterpolationData3;
				}
				if (Math.Abs(aLo.alpha - aHi.alpha) < 1.1E-16)
				{
					_newGrad = _grad;
					_newPoint = _point;
					_newValue = _value;
					throw new CompactQuasiNewtonException(CompactQuasiNewtonErrorType.InsufficientSteplength);
				}
			}
		}

		/// <summary>
		/// Cubic interpolation routine from Nocedal and Wright (3.59) (used for LineSearch).
		/// </summary>
		/// <param name="p0">first point, with alpha, value and derivative</param>
		/// <param name="p1">second point, with alpha, value and derivative</param>
		/// <returns>local minimum of interpolating cubic polynomial</returns>
		private static double CubicInterp(PointInterpolationData p0, PointInterpolationData p1)
		{
			double num = p0.deriv + p1.deriv - 3.0 * (p0.value - p1.value) / (p0.alpha - p1.alpha);
			double num2 = (double)Math.Sign(p1.alpha - p0.alpha) * Math.Sqrt(num * num - p0.deriv * p1.deriv);
			double num3 = p1.deriv + num2 - num;
			double num4 = p1.deriv - p0.deriv + 2.0 * num2;
			return p1.alpha - (p1.alpha - p0.alpha) * num3 / num4;
		}

		/// <summary>
		/// call the function and inverse the result and gradient if this is
		/// maximization problem. All calls should be through that pipe
		/// </summary>
		/// <param name="pointVector"></param>
		/// <param name="gradientVector"></param>
		/// <returns></returns>
		private double CallFunction(double[] pointVector, double[] gradientVector)
		{
			double num = _function(pointVector, gradientVector);
			if (!_minimize)
			{
				num *= -1.0;
				gradientVector.ScaleBy(-1.0);
			}
			_evaluationCount++;
			return num;
		}

		/// <summary>
		/// checks if the gradient is in the vicinity of zero
		/// </summary>
		/// <returns></returns>
		internal bool IsGradientAlmostZero()
		{
			return _newGrad.Norm2() / (double)_dimensions < 0.001;
		}
	}
}
