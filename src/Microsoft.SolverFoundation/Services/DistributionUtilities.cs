using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Utility methods for calculating PDFs etc.
	/// </summary>
	/// <remarks>
	/// These routines are good enough for the distributions but the need some tuning if
	/// very accurate results are needed.
	/// </remarks>
	internal static class DistributionUtilities
	{
		private const double one_12 = 1.0 / 12.0;

		private const double one_360 = 1.0 / 360.0;

		private const double one_1260 = 0.0007936507936507937;

		private static readonly double _sqrtTwoPi = Math.Sqrt(Math.PI * 2.0);

		private static readonly double _sqrtTwo = Math.Sqrt(2.0);

		private static readonly double _logSqrtTwoPi = Math.Log(Math.Sqrt(Math.PI * 2.0));

		private static readonly double[] _stirlingCorrectionTermsTable = new double[10] { 0.08106146679532726, 0.04134069595540929, 0.02767792568499834, 0.02079067210376509, 0.01664469118982119, 0.01387612882307075, 0.01189670994589177, 0.01041126526197209, 0.009255462182712733, 0.00833056343336287 };

		/// <summary>Determines if a value is a valid probability.
		/// </summary>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		public static void ValidateProbability(double probability)
		{
			if (!IsValidProbability(probability))
			{
				throw new ArgumentOutOfRangeException("probability");
			}
		}

		public static bool IsValidProbability(double probability)
		{
			if (probability >= 0.0)
			{
				return probability <= 1.0;
			}
			return false;
		}

		/// <summary>Determines if a value is a valid input to CumulativeDensity.
		/// </summary>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		public static void ValidateCumulativeDensityValue(double x)
		{
			if (double.IsNaN(x))
			{
				throw new ArgumentOutOfRangeException("x");
			}
		}

		/// <summary>Determines if a value is within the range of valid probability values.
		/// </summary>
		public static bool IsNonzeroProbability(Rational probability)
		{
			if (probability > Rational.Zero)
			{
				return probability <= Rational.One;
			}
			return false;
		}

		/// <summary>Determines if a value exceeds one by more than a tolerance.
		/// </summary>
		public static bool GreaterThanOne(Rational probability, double tolerance)
		{
			if (!probability.IsIndeterminate)
			{
				return probability - tolerance > 1.0;
			}
			return false;
		}

		/// <summary>Determines if a value is less than one by more than a tolerance.
		/// </summary>
		public static bool LessThanOne(Rational probability, double tolerance)
		{
			if (!probability.IsIndeterminate)
			{
				return probability + tolerance < 1.0;
			}
			return false;
		}

		/// <summary>Determines if a value is equal to one within a specified tolerance.
		/// </summary>
		public static bool EqualsOne(Rational probability, double tolerance)
		{
			if (probability.IsFinite)
			{
				if (!LessThanOne(probability, tolerance))
				{
					return !GreaterThanOne(probability, tolerance);
				}
				return false;
			}
			return false;
		}

		/// <summary>Correction terms for strinling (Fc)
		/// </summary>
		/// <param name="k"></param>
		/// <returns></returns>
		public static double StirlingCorrectionTerm(int k)
		{
			if (k < _stirlingCorrectionTermsTable.Length)
			{
				return _stirlingCorrectionTermsTable[k];
			}
			double num = k + 1;
			double num2 = num * num;
			return (1.0 / 12.0 - (1.0 / 360.0 - 0.0007936507936507937 / num2) / num2) / num;
		}

		/// <summary>
		/// Return the gamma function for x. Consider the more slowly growing LogGamma function instead.
		/// </summary>
		/// <remarks>
		/// Based on Lanczos, C. 1964, SIAM Journal on Numerical Analysis, ser. B, vol. 1, pp. 86–96.
		/// </remarks>
		/// <param name="x"></param>
		/// <returns></returns>
		public static double Gamma(double x)
		{
			double num = x;
			double num2 = 1.000000000190015;
			num2 += 76.18009172947146 / (num += 1.0);
			num2 += -86.50532032941678 / (num += 1.0);
			num2 += 24.01409824083091 / (num += 1.0);
			num2 += -1.231739572450155 / (num += 1.0);
			num2 += 0.001208650973866179 / (num += 1.0);
			num2 += -5.395239384953E-06 / (num += 1.0);
			return Math.Pow(x + 5.5, x + 0.5) * Math.Exp(0.0 - (x + 5.5)) * (_sqrtTwoPi * num2 / x);
		}

		/// <summary>
		/// Return the log of the gamma function for x. Because the gamma function grows so rapidly this is often
		/// a more useful function than the gamma function.
		/// </summary>
		/// <remarks>
		/// Based on Lanczos, C. 1964, SIAM Journal on Numerical Analysis, ser. B, vol. 1, pp. 86–96.
		/// </remarks>
		/// <param name="x"></param>
		/// <returns></returns>
		public static double LogGamma(double x)
		{
			double num = x;
			double num2 = 1.000000000190015;
			num2 += 76.18009172947146 / (num += 1.0);
			num2 += -86.50532032941678 / (num += 1.0);
			num2 += 24.01409824083091 / (num += 1.0);
			num2 += -1.231739572450155 / (num += 1.0);
			num2 += 0.001208650973866179 / (num += 1.0);
			num2 += -5.395239384953E-06 / (num += 1.0);
			return Math.Log(_sqrtTwoPi * num2 / x) - (x + 5.5 - (x + 0.5) * Math.Log(x + 5.5));
		}

		/// <summary>
		/// The error function erf.
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		public static double ErrorFunction(double x)
		{
			if (x < 0.0)
			{
				return 0.0 - IncompleteGamma(0.5, x * x);
			}
			return IncompleteGamma(0.5, x * x);
		}

		/// <summary>
		/// The incomplete gamma function (regularized gamma functions, P(a, x)).
		/// </summary>
		/// <param name="a"></param>
		/// <param name="x"></param>
		/// <returns></returns>
		/// <remarks>P(a, x) = LowerIncompleteGammaFunction(a, x)/ Gamma(a) </remarks>
		public static double IncompleteGamma(double a, double x)
		{
			if (double.IsPositiveInfinity(x))
			{
				return 1.0;
			}
			if (x < a + 1.0)
			{
				return IncompleteGammaSeriesApproximation(a, x);
			}
			return 1.0 - IncompleteGammaContinuedFractionApproximation(a, x);
		}

		private static double IncompleteGammaSeriesApproximation(double a, double x)
		{
			if (x != 0.0)
			{
				double num = 1.0 / a;
				double num2 = num;
				for (uint num3 = 1u; num3 <= 100; num3++)
				{
					num *= x / (a + (double)num3);
					num2 += num;
					if (Math.Abs(num) < Math.Abs(num2) * 3E-07)
					{
						return Math.Exp(0.0 - x + a * Math.Log(x) - LogGamma(a)) * num2;
					}
				}
				return double.NaN;
			}
			return 0.0;
		}

		private static double IncompleteGammaContinuedFractionApproximation(double a, double x)
		{
			double num = x + 1.0 - a;
			double num2 = 7.1362384635298E+44;
			double num3 = 1.0 / num;
			double num4 = num3;
			uint num5 = 1u;
			double num7;
			do
			{
				double num6 = (double)(0L - (long)num5) * ((double)num5 - a);
				num += 2.0;
				num3 = num6 * num3 + num;
				if (Math.Abs(num3) < 1.401298464324817E-45)
				{
					num3 = 1.401298464324817E-45;
				}
				num2 = num + num6 / num2;
				if (Math.Abs(num2) < 1.401298464324817E-45)
				{
					num2 = 1.401298464324817E-45;
				}
				num3 = 1.0 / num3;
				num7 = num3 * num2;
				num4 *= num7;
				num5++;
			}
			while (Math.Abs(num7 - 1.0) >= 3E-07 && num5 <= 100);
			return Math.Exp(0.0 - x + a * Math.Log(x) - LogGamma(a)) * num4;
		}

		/// <summary>
		/// The probit function or inverse cummulative of standard (mean 0, variance 1)
		/// normal density function.
		/// </summary>
		/// <param name="probability"></param>
		/// <returns></returns>
		public static double InverseCumulativeStandardNormal(double probability)
		{
			double num2;
			if (probability < 0.02425)
			{
				double num = Math.Sqrt(-2.0 * Math.Log(probability));
				num2 = (((((-0.007784894002430293 * num + -0.3223964580411365) * num + -2.400758277161838) * num + -2.549732539343734) * num + 4.374664141464968) * num + 2.938163982698783) / ((((0.007784695709041462 * num + 0.3224671290700398) * num + 2.445134137142996) * num + 3.754408661907416) * num + 1.0);
			}
			else if (probability <= 0.97575)
			{
				double num3 = probability - 0.5;
				double num4 = num3 * num3;
				num2 = (((((-39.69683028665376 * num4 + 220.9460984245205) * num4 + -275.9285104469687) * num4 + 138.357751867269) * num4 + -30.66479806614716) * num4 + 2.506628277459239) * num3 / (((((-54.47609879822406 * num4 + 161.5858368580409) * num4 + -155.6989798598866) * num4 + 66.80131188771972) * num4 + -13.28068155288572) * num4 + 1.0);
			}
			else
			{
				double num5 = Math.Sqrt(-2.0 * Math.Log(1.0 - probability));
				num2 = (0.0 - (((((-0.007784894002430293 * num5 + -0.3223964580411365) * num5 + -2.400758277161838) * num5 + -2.549732539343734) * num5 + 4.374664141464968) * num5 + 2.938163982698783)) / ((((0.007784695709041462 * num5 + 0.3224671290700398) * num5 + 2.445134137142996) * num5 + 3.754408661907416) * num5 + 1.0);
			}
			return num2 + (probability - CumulativeDensityStandardNormal(num2)) * Math.Exp(0.5 * Math.Pow(num2, 2.0) + _logSqrtTwoPi);
		}

		/// <summary>Cummulative density of standard (mean 0, variance 1)
		/// normal distribution.
		/// </summary>
		/// <param name="x">real number</param>
		/// <returns>commulative probability</returns>
		public static double CumulativeDensityStandardNormal(double x)
		{
			if (double.IsNegativeInfinity(x))
			{
				return 0.0;
			}
			if (double.IsPositiveInfinity(x))
			{
				return 1.0;
			}
			return 0.5 * (1.0 + ErrorFunction(x / _sqrtTwo));
		}
	}
}
