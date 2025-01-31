using System;
using System.Globalization;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> BigSum is designed to add/subdract doubles with better cancellations.
	/// </summary>
	internal struct BigSum
	{
		private double whole;

		private double fraction;

		private static long HUGE = 1152921504606846976L;

		internal bool IsZero
		{
			get
			{
				if (0.0 == fraction)
				{
					return 0.0 == whole;
				}
				return false;
			}
		}

		internal BigSum(double num)
		{
			if ((double)HUGE < Math.Abs(num))
			{
				whole = num;
				fraction = 0.0;
			}
			else
			{
				whole = (long)num;
				fraction = num - whole;
			}
		}

		private BigSum(double w, double f)
		{
			whole = w;
			fraction = f;
		}

		internal double ToDouble()
		{
			return whole + fraction;
		}

		internal Rational ToRational()
		{
			return (Rational)whole + (Rational)fraction;
		}

		public static implicit operator BigSum(double num)
		{
			return new BigSum(num);
		}

		public static BigSum operator +(BigSum a, BigSum b)
		{
			return new BigSum(a.whole + b.whole, a.fraction + b.fraction);
		}

		public static BigSum operator -(BigSum a, BigSum b)
		{
			return new BigSum(a.whole - b.whole, a.fraction - b.fraction);
		}

		public static BigSum operator /(BigSum num, int div)
		{
			return new BigSum(num.whole / (double)div, num.fraction / (double)div);
		}

		public static BigSum operator *(BigSum num, int mul)
		{
			return new BigSum(num.whole * (double)mul, num.fraction * (double)mul);
		}

		public static BigSum operator *(int mul, BigSum num)
		{
			return new BigSum(num.whole * (double)mul, num.fraction * (double)mul);
		}

		internal void Add(double num)
		{
			BigSum bigSum = num;
			whole += bigSum.whole;
			fraction += bigSum.fraction;
		}

		internal void Sub(double num)
		{
			BigSum bigSum = num;
			whole -= bigSum.whole;
			fraction -= bigSum.fraction;
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}+{1}={2}", new object[3]
			{
				whole,
				fraction,
				ToDouble()
			});
		}
	}
}
