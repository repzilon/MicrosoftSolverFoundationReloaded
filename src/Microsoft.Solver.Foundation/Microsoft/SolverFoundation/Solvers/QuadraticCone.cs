using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal static class QuadraticCone
	{
		public static double FormWvector(Vector s, Vector z, Vector w)
		{
			double num = Math.Sqrt(2.0 * s[0] * s[0] - s.InnerProduct(s));
			double num2 = Math.Sqrt(2.0 * z[0] * z[0] - z.InnerProduct(z));
			double num3 = Math.Sqrt(num / num2);
			w.CopyFrom(z);
			w.ScaleBy(0.0 - num3);
			w[0] = 0.0 - w[0];
			Vector.Daxpy(1.0 / num3, s, w);
			w.ScaleBy(1.0 / Math.Sqrt(2.0 * (s.InnerProduct(z) + num * num2)));
			return num3;
		}

		public static void MultiplyByTW(double theta, Vector w, Vector z, Vector TWz)
		{
			double num = (w.InnerProduct(z) + z[0]) / (1.0 + w[0]);
			TWz.CopyFrom(z);
			TWz[0] = 0.0 - z[0] + num;
			Vector.Daxpy(num, w, TWz);
			TWz.ScaleBy(theta);
		}

		public static void MultiplybyWT(double theta, Vector w, Vector v, Vector WTv)
		{
			MultiplyByTW(theta, w, v, WTv);
		}

		public static void MultiplyByTWinverse(double theta, Vector w, Vector s, Vector TWis)
		{
			double num = (s[0] - w.InnerProduct(s) + 2.0 * w[0] * s[0]) / (1.0 + w[0]);
			TWis.CopyFrom(s);
			Vector.Daxpy(0.0 - num, w, s);
			TWis[0] = 0.0 - TWis[0] + num;
			TWis.ScaleBy(1.0 / theta);
		}

		public static void MultiplyByW2(double theta, Vector w, Vector r, Vector W2r)
		{
			double alpha = 2.0 * w.InnerProduct(r);
			W2r.CopyFrom(r);
			W2r[0] = 0.0 - W2r[0];
			Vector.Daxpy(alpha, w, W2r);
			W2r.ScaleBy(theta * theta);
		}

		public static double MaxStepsize(Vector s, Vector ds)
		{
			double num = 2.0 * ds[0] * ds[0] - ds.InnerProduct(ds);
			double num2 = 2.0 * s[0] * ds[0] - s.InnerProduct(ds);
			double num3 = 2.0 * s[0] * s[0] - s.InnerProduct(s);
			double num4 = num2 * num2 - num * num3;
			double num5 = 1.0;
			num5 = ((num > 0.0) ? ((!(num4 >= 0.0) || !(num2 < 0.0)) ? 1.0 : Math.Min(1.0, (0.0 - num2 - Math.Sqrt(num4)) / num)) : ((num < 0.0) ? Math.Min(1.0, (0.0 - num2 - Math.Sqrt(num4)) / num) : ((!(num2 < 0.0)) ? 1.0 : Math.Min(1.0, (0.0 - num3) / (2.0 * num2)))));
			if (ds[0] < 0.0)
			{
				num5 = Math.Min(num5, (0.0 - s[0]) / ds[0]);
			}
			return num5;
		}

		public static double NeighborhoodTest(Vector s, Vector z, double mu)
		{
			double num = 2.0 * s[0] * s[0] - s.InnerProduct(s);
			double num2 = 2.0 * z[0] * z[0] - z.InnerProduct(z);
			double num3 = s.InnerProduct(z);
			return num * num2 / (num3 * mu);
		}

		public static void SimpleInitialization(Vector v)
		{
			v.ScaleBy(0.0);
			v[0] = 1.0;
		}
	}
}
