using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	internal class OperationHelpers
	{
		internal static Rational EvaluateUnaryOp(TermModelOperation operation, Rational arg1)
		{
			switch (operation)
			{
			case TermModelOperation.Identity:
				return arg1;
			case TermModelOperation.Minus:
				return -arg1;
			case TermModelOperation.Not:
				if (!arg1.IsZero)
				{
					return Rational.Zero;
				}
				return Rational.One;
			default:
				return EvaluateUnaryOp(operation, (double)arg1);
			}
		}

		internal static double EvaluateUnaryOp(TermModelOperation operation, double arg1)
		{
			switch (operation)
			{
			case TermModelOperation.Abs:
				return Math.Abs(arg1);
			case TermModelOperation.Cos:
				return Math.Cos(arg1);
			case TermModelOperation.Exp:
				return Math.Exp(arg1);
			case TermModelOperation.Log:
				return Math.Log(arg1);
			case TermModelOperation.Log10:
				return Math.Log10(arg1);
			case TermModelOperation.Sqrt:
				return Math.Sqrt(arg1);
			case TermModelOperation.Sin:
				return Math.Sin(arg1);
			case TermModelOperation.Tan:
				return Math.Tan(arg1);
			case TermModelOperation.ArcCos:
				return Math.Acos(arg1);
			case TermModelOperation.ArcTan:
				return Math.Atan(arg1);
			case TermModelOperation.ArcSin:
				return Math.Asin(arg1);
			case TermModelOperation.Sinh:
				return Math.Sinh(arg1);
			case TermModelOperation.Cosh:
				return Math.Cosh(arg1);
			case TermModelOperation.Tanh:
				return Math.Tanh(arg1);
			case TermModelOperation.Ceiling:
				return Math.Ceiling(arg1);
			case TermModelOperation.Floor:
				return Math.Floor(arg1);
			case TermModelOperation.Identity:
				return arg1;
			case TermModelOperation.Minus:
				return 0.0 - arg1;
			case TermModelOperation.Not:
				if (arg1 != 0.0)
				{
					return 0.0;
				}
				return 1.0;
			default:
				throw new NotSupportedException();
			}
		}

		internal static Rational EvaluateBinaryOp(TermModelOperation operation, Rational arg1, Rational arg2)
		{
			switch (operation)
			{
			case TermModelOperation.And:
				if (arg1.IsZero || arg2.IsZero)
				{
					return Rational.Zero;
				}
				return Rational.One;
			case TermModelOperation.Equal:
				if (!(arg1 == arg2))
				{
					return Rational.Zero;
				}
				return Rational.One;
			case TermModelOperation.Greater:
				if (!(arg1 > arg2))
				{
					return Rational.Zero;
				}
				return Rational.One;
			case TermModelOperation.GreaterEqual:
				if (!(arg1 >= arg2))
				{
					return Rational.Zero;
				}
				return Rational.One;
			case TermModelOperation.Less:
				if (!(arg1 < arg2))
				{
					return Rational.Zero;
				}
				return Rational.One;
			case TermModelOperation.LessEqual:
				if (!(arg1 <= arg2))
				{
					return Rational.Zero;
				}
				return Rational.One;
			case TermModelOperation.Or:
				if (arg1.IsZero && arg2.IsZero)
				{
					return Rational.Zero;
				}
				return Rational.One;
			case TermModelOperation.Plus:
				return arg1 + arg2;
			case TermModelOperation.Quotient:
				return arg1 / arg2;
			case TermModelOperation.Times:
				return arg1 * arg2;
			case TermModelOperation.Unequal:
				if (!(arg1 != arg2))
				{
					return Rational.Zero;
				}
				return Rational.One;
			default:
				return EvaluateBinaryOp(operation, (double)arg1, (double)arg2);
			}
		}

		internal static double EvaluateBinaryOp(TermModelOperation operation, double arg1, double arg2)
		{
			switch (operation)
			{
			case TermModelOperation.Max:
				return Math.Max(arg1, arg2);
			case TermModelOperation.Min:
				return Math.Min(arg1, arg2);
			case TermModelOperation.Power:
				return Math.Pow(arg1, arg2);
			case TermModelOperation.And:
				if (arg1 == 0.0 || arg2 == 0.0)
				{
					return 0.0;
				}
				return 1.0;
			case TermModelOperation.Equal:
				if (arg1 != arg2)
				{
					return 0.0;
				}
				return 1.0;
			case TermModelOperation.Greater:
				if (!(arg1 > arg2))
				{
					return 0.0;
				}
				return 1.0;
			case TermModelOperation.GreaterEqual:
				if (!(arg1 >= arg2))
				{
					return 0.0;
				}
				return 1.0;
			case TermModelOperation.Less:
				if (!(arg1 < arg2))
				{
					return 0.0;
				}
				return 1.0;
			case TermModelOperation.LessEqual:
				if (!(arg1 <= arg2))
				{
					return 0.0;
				}
				return 1.0;
			case TermModelOperation.Or:
				if (arg1 == 0.0 && arg2 == 0.0)
				{
					return 0.0;
				}
				return 1.0;
			case TermModelOperation.Plus:
				return arg1 + arg2;
			case TermModelOperation.Quotient:
				return arg1 / arg2;
			case TermModelOperation.Times:
				return arg1 * arg2;
			case TermModelOperation.Unequal:
				if (arg1 == arg2)
				{
					return 0.0;
				}
				return 1.0;
			default:
				throw new NotSupportedException();
			}
		}
	}
}
