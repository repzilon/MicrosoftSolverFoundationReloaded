using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Static utilities on gradients
	/// </summary>
	internal static class Gradients
	{
		private static Func<int, int, int> _lessEqualDelegate = BooleanFunction.LessEqual;

		private static Func<int, int, int> _lessStrictDelegate = BooleanFunction.LessStrict;

		private static Func<int, int, int> _equalDelegate = BooleanFunction.Equal;

		/// <summary>
		/// computes the gradient of -x
		/// </summary>
		public static ValueWithGradients Minus(ValueWithGradients grad)
		{
			return new ValueWithGradients(-grad.Value, -grad.IncGradient, grad.IncVariable, -grad.DecGradient, grad.DecVariable);
		}

		/// <summary>
		/// computes the gradient of x+y
		/// </summary>
		public static ValueWithGradients Sum(ValueWithGradients x, ValueWithGradients y)
		{
			bool flag = x.DecGradient <= y.DecGradient;
			bool flag2 = x.IncGradient >= y.IncGradient;
			return new ValueWithGradients(x.Value + y.Value, flag ? x.DecGradient : y.DecGradient, flag ? x.DecVariable : y.DecVariable, flag2 ? x.IncGradient : y.IncGradient, flag2 ? x.IncVariable : y.IncVariable);
		}

		/// <summary>
		/// Computes the gradient of min (x, y)
		/// </summary>
		public static ValueWithGradients Min(ValueWithGradients x, ValueWithGradients y)
		{
			ValueWithGradients result = new ValueWithGradients(Math.Min(x.Value, y.Value));
			result.Expand(x.DecVariable, Math.Min(x.Value + x.DecGradient, y.Value));
			result.Expand(x.IncVariable, Math.Min(x.Value + x.IncGradient, y.Value));
			result.Expand(y.DecVariable, Math.Min(x.Value, y.Value + y.DecGradient));
			result.Expand(y.IncVariable, Math.Min(x.Value, y.Value + y.IncGradient));
			return result;
		}

		/// <summary>
		/// Computes the gradient of max (x, y)
		/// </summary>
		public static ValueWithGradients Max(ValueWithGradients x, ValueWithGradients y)
		{
			ValueWithGradients result = new ValueWithGradients(Math.Max(x.Value, y.Value));
			result.Expand(x.DecVariable, Math.Max(x.Value + x.DecGradient, y.Value));
			result.Expand(x.IncVariable, Math.Max(x.Value + x.IncGradient, y.Value));
			result.Expand(y.DecVariable, Math.Max(x.Value, y.Value + y.DecGradient));
			result.Expand(y.IncVariable, Math.Max(x.Value, y.Value + y.IncGradient));
			return result;
		}

		/// <summary>
		/// Computes the gradient of x * y
		/// </summary>
		public static ValueWithGradients Product(ValueWithGradients x, ValueWithGradients y)
		{
			ValueWithGradients result = new ValueWithGradients(x.Value * y.Value);
			result.Expand(x.DecVariable, (x.Value + x.DecGradient) * y.Value);
			result.Expand(x.IncVariable, (x.Value + x.IncGradient) * y.Value);
			result.Expand(y.DecVariable, x.Value * (y.Value + y.DecGradient));
			result.Expand(y.IncVariable, x.Value * (y.Value + y.IncGradient));
			return result;
		}

		/// <summary>
		/// Computes the value of x ^ exponent together with its gradients
		/// </summary>
		public static ValueWithGradients Power(ValueWithGradients x, int exponent)
		{
			ValueWithGradients result = new ValueWithGradients(Power(x.Value, exponent));
			int num = x.Value + x.DecGradient;
			int num2 = x.Value + x.IncGradient;
			result.Expand(x.DecVariable, Power(num, exponent));
			result.Expand(x.IncVariable, Power(num2, exponent));
			if (exponent % 2 == 0)
			{
				if (num < 0 && 0 < x.Value)
				{
					result.ExpandNegative(x.DecVariable, 0);
				}
				else if (x.Value < 0 && 0 < num2)
				{
					result.ExpandNegative(x.IncVariable, 0);
				}
			}
			return result;
		}

		private static int Power(int x, int exponent)
		{
			int num = 1;
			for (int i = 0; i < exponent; i++)
			{
				num *= x;
			}
			return num;
		}

		/// <summary>
		/// Computes the value of Abs(x) together with its gradients
		/// </summary>
		public static ValueWithGradients Abs(ValueWithGradients x)
		{
			ValueWithGradients result = new ValueWithGradients(Math.Abs(x.Value));
			int num = x.Value + x.DecGradient;
			int num2 = x.Value + x.IncGradient;
			result.Expand(x.DecVariable, Math.Abs(num));
			result.Expand(x.IncVariable, Math.Abs(num2));
			if (num < 0 && 0 < x.Value)
			{
				result.ExpandNegative(x.DecVariable, 0);
			}
			else if (x.Value < 0 && 0 < num2)
			{
				result.ExpandNegative(x.IncVariable, 0);
			}
			return result;
		}

		/// <summary>
		/// computes the gradient of not x
		/// where x is a (non-zero) violation score
		/// </summary>
		public static ValueWithGradients Not(ValueWithGradients grad)
		{
			return new ValueWithGradients(BooleanFunction.Not(grad.Value), -grad.IncGradient, grad.IncVariable, -grad.DecGradient, grad.DecVariable);
		}

		/// <summary>
		/// Computes the gradient of x and y
		/// where x and y are both (non-zero) violation scores
		/// </summary>
		public static ValueWithGradients And(ValueWithGradients x, ValueWithGradients y)
		{
			ValueWithGradients result = new ValueWithGradients(BooleanFunction.And(x.Value, y.Value));
			result.Expand(x.DecVariable, BooleanFunction.And(x.Value + x.DecGradient, y.Value));
			result.Expand(x.IncVariable, BooleanFunction.And(x.Value + x.IncGradient, y.Value));
			result.Expand(y.DecVariable, BooleanFunction.And(x.Value, y.Value + y.DecGradient));
			result.Expand(y.IncVariable, BooleanFunction.And(x.Value, y.Value + y.IncGradient));
			return result;
		}

		/// <summary>
		/// Computes the gradient of x or y
		/// where x and y are both (non-zero) violation scores
		/// </summary>
		public static ValueWithGradients Or(ValueWithGradients x, ValueWithGradients y)
		{
			ValueWithGradients result = new ValueWithGradients(BooleanFunction.Or(x.Value, y.Value));
			result.Expand(x.DecVariable, BooleanFunction.Or(x.Value + x.DecGradient, y.Value));
			result.Expand(x.IncVariable, BooleanFunction.Or(x.Value + x.IncGradient, y.Value));
			result.Expand(y.DecVariable, BooleanFunction.Or(x.Value, y.Value + y.DecGradient));
			result.Expand(y.IncVariable, BooleanFunction.Or(x.Value, y.Value + y.IncGradient));
			return result;
		}

		/// <summary>
		/// Computes the gradient of x implies y
		/// where x and y are both (non-zero) violation scores
		/// </summary>
		public static ValueWithGradients Implies(ValueWithGradients x, ValueWithGradients y)
		{
			return Or(Not(x), y);
		}

		/// <summary>
		/// Computes the gradient of x iff y
		/// where x and y are both (non-zero) violation scores
		/// </summary>
		public static ValueWithGradients Equivalent(ValueWithGradients x, ValueWithGradients y)
		{
			return And(Implies(x, y), Implies(y, x));
		}

		/// <summary>
		/// Computes the violation of x lessEqual to y
		/// together with its gradient info
		/// </summary>
		public static ValueWithGradients LessEqual(ValueWithGradients x, ValueWithGradients y)
		{
			return Evaluate(x, y, _lessEqualDelegate);
		}

		/// <summary>
		/// Computes the violation of x lessStrict to y
		/// together with its gradient info
		/// </summary>
		public static ValueWithGradients LessStrict(ValueWithGradients x, ValueWithGradients y)
		{
			return Evaluate(x, y, _lessStrictDelegate);
		}

		/// <summary>
		/// Computes the violation of x != y
		/// together with its gradient info
		/// </summary>
		public static ValueWithGradients Unequal(ValueWithGradients x, ValueWithGradients y)
		{
			return Not(Equal(x, y));
		}

		/// <summary>
		/// Computes the violation of x = y
		/// together with its gradient info
		/// </summary>
		public static ValueWithGradients Equal(ValueWithGradients x, ValueWithGradients y)
		{
			ValueWithGradients result = Evaluate(x, y, _equalDelegate);
			int num = x.Value - y.Value;
			if (num > 0)
			{
				if (y.IncGradient >= num)
				{
					result.ExpandNegative(y.IncVariable, -1);
				}
				if (-x.DecGradient >= num)
				{
					result.ExpandNegative(x.DecVariable, -1);
				}
			}
			else if (num < 0)
			{
				if (-x.IncGradient <= num)
				{
					result.ExpandNegative(x.IncVariable, -1);
				}
				if (y.DecGradient <= num)
				{
					result.ExpandNegative(y.DecVariable, -1);
				}
			}
			return result;
		}

		/// <summary>
		/// Given a binary function and two points with gradients,
		/// computes the result and its gradients. 
		/// <remarks>
		/// The function has to be monotonic in the sense that 
		/// evaluating the inputs with maximal gradients should
		/// suffice to evaluate the gradients (i.e. extremal
		/// points should not be in the middle of a variable's range).
		///             </remarks>
		/// </summary>
		/// <param name="x">A point with its gradients</param>
		/// <param name="y">A point with its gradients</param>
		/// <param name="f">A monotone function</param>
		private static ValueWithGradients Evaluate(ValueWithGradients x, ValueWithGradients y, Func<int, int, int> f)
		{
			ValueWithGradients result = new ValueWithGradients(f(x.Value, y.Value));
			if (x.DecGradient != 0)
			{
				result.Expand(x.DecVariable, f(x.Value + x.DecGradient, y.Value));
			}
			if (x.IncGradient != 0)
			{
				result.Expand(x.IncVariable, f(x.Value + x.IncGradient, y.Value));
			}
			if (y.DecGradient != 0)
			{
				result.Expand(y.DecVariable, f(x.Value, y.Value + y.DecGradient));
			}
			if (y.IncGradient != 0)
			{
				result.Expand(y.IncVariable, f(x.Value, y.Value + y.IncGradient));
			}
			return result;
		}
	}
}
