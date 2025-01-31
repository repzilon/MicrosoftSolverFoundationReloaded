using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class IpmVectorUtilities
	{
		/// <summary>The IPM ratio test: (the input vector x should be elementwise positive) 
		/// </summary>
		/// <returns>Returns maximum alpha such that x + alpha*dx is nonegative vector </returns>
		public static double IPMRatioTest(Vector x, Vector dx)
		{
			double num = 1.0;
			int num2 = x.Length;
			while (0 <= --num2)
			{
				if (dx[num2] < 0.0)
				{
					num = Math.Min(num, (0.0 - x[num2]) / dx[num2]);
				}
			}
			return num;
		}

		/// <summary>The IPM uniform test for complementary pairs
		/// </summary>
		/// <param name="xz"></param>
		/// <param name="mu"></param>
		/// <returns>Returns minimum (over i) of xz[i]/mu</returns>
		public static double IPMUniformTest(Vector xz, double mu)
		{
			double num = 1.0;
			int num2 = xz.Length;
			while (0 <= --num2)
			{
				num = Math.Min(num, xz[num2] / mu);
			}
			return num;
		}

		/// <summary>Threshold truncating: this[i] = max( this[i], threshold )
		/// </summary>
		public static void TruncateBottom(Vector x, double threshold)
		{
			int num = x.Length;
			while (0 <= --num)
			{
				if (x[num] < threshold)
				{
					x[num] = threshold;
				}
			}
		}

		/// <summary>Threshold truncating: this[i] = min( this[i], threshold )
		/// </summary>
		public static void TruncateTop(Vector x, double threshold)
		{
			int num = x.Length;
			while (0 <= --num)
			{
				if (x[num] > threshold)
				{
					x[num] = threshold;
				}
			}
		}

		public static void BoxCorrection(Vector x, double bL, double bU, Vector c)
		{
			int num = x.Length;
			while (0 <= --num)
			{
				double num2 = bL - x[num];
				double num3 = bU - x[num];
				c[num] = ((num2 > 0.0) ? num2 : ((num3 < 0.0) ? num3 : 0.0));
			}
		}
	}
}
