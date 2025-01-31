using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal static class RotatedQuadraticCone
	{
		public static double FormWvector(Vector s, Vector z, Vector w)
		{
			double num = Math.Sqrt(2.0 * s[0] * s[1] + s[0] * s[0] + s[1] * s[1] - s.InnerProduct(s));
			double num2 = Math.Sqrt(2.0 * z[0] * z[1] + z[0] * z[0] + z[1] * z[1] - z.InnerProduct(z));
			double num3 = Math.Sqrt(num / num2);
			w.CopyFrom(z);
			w.ScaleBy(0.0 - num3);
			w[0] = 0.0 - w[1];
			w[1] = 0.0 - w[0];
			Vector.Daxpy(1.0 / num3, s, w);
			w.ScaleBy(1.0 / Math.Sqrt(2.0 * (s.InnerProduct(z) + num * num2)));
			return num3;
		}

		public static void MultiplyByTW(double theta, Vector w, Vector z, Vector TWz)
		{
			double num = 1.0 / Math.Sqrt(2.0);
			double num2 = (w.InnerProduct(z) + num * (z[0] + z[1])) / (1.0 + num * (w[0] + w[1]));
			TWz.CopyFrom(z);
			TWz[0] = 0.0 - z[1] + num2 * num;
			TWz[1] = 0.0 - z[0] + num2 * num;
			Vector.Daxpy(num2, w, TWz);
			TWz.ScaleBy(theta);
			double num3 = TWz[0];
			double num4 = TWz[1];
			TWz[0] = num * (num3 + num4);
			TWz[1] = num * (num3 - num4);
		}

		public static void MultiplyByWT(double theta, Vector w, Vector v, Vector WTv)
		{
			double num = 1.0 / Math.Sqrt(2.0);
			double num2 = num * (v[0] + v[1]);
			double num3 = num * (v[0] - v[1]);
			double num4 = w.InnerProduct(v) + (v[0] + w[0] * (num2 - v[0]) + w[1] * (num3 - v[1]));
			WTv.CopyFrom(v);
			WTv[0] = 0.0 - num3 + num4 * num;
			WTv[1] = 0.0 - num2 + num4 * num;
			Vector.Daxpy(num4, w, WTv);
			WTv.ScaleBy(theta);
		}

		public static void MultiplyByTWinverse(double theta, Vector w, Vector s, Vector TWis)
		{
			double num = 1.0 / Math.Sqrt(2.0);
			double num2 = ((num + w[0] + w[1]) * (s[0] + s[1]) - w.InnerProduct(s)) / (1.0 + num * (w[0] + w[1]));
			TWis.CopyFrom(s);
			Vector.Daxpy(0.0 - num2, w, TWis);
			double num3 = TWis[0];
			double num4 = TWis[1];
			TWis[0] = (0.0 - num) * (num3 + num4) + num2;
			TWis[1] = num * (num3 - num4);
			TWis.ScaleBy(1.0 / theta);
		}

		public static void MultiplyByW2(double theta, Vector w, Vector r, Vector W2r)
		{
			double alpha = 2.0 * w.InnerProduct(r);
			W2r.CopyFrom(r);
			W2r[0] = 0.0 - W2r[1];
			W2r[1] = 0.0 - W2r[0];
			Vector.Daxpy(alpha, w, W2r);
			W2r.ScaleBy(theta * theta);
		}

		public static double MaxStepsize(Vector s, Vector ds)
		{
			double num = (ds[0] + ds[1]) * (ds[0] + ds[1]) - ds.InnerProduct(ds);
			double num2 = (s[0] + s[1]) * (ds[0] + ds[1]) - s.InnerProduct(ds);
			double num3 = (s[0] + s[1]) * (s[0] + s[1]) - s.InnerProduct(s);
			double num4 = num2 * num2 - num * num3;
			double num5 = 1.0;
			num5 = ((num > 0.0) ? ((!(num4 >= 0.0) || !(num2 < 0.0)) ? 1.0 : Math.Min(1.0, (0.0 - num2 - Math.Sqrt(num4)) / num)) : ((num < 0.0) ? Math.Min(1.0, (0.0 - num2 - Math.Sqrt(num4)) / num) : ((!(num2 < 0.0)) ? 1.0 : Math.Min(1.0, (0.0 - num3) / (2.0 * num2)))));
			if (ds[0] < 0.0)
			{
				num5 = Math.Min(num5, (0.0 - s[0]) / ds[0]);
			}
			if (ds[1] < 0.0)
			{
				num5 = Math.Min(num5, (0.0 - s[1]) / ds[1]);
			}
			return num5;
		}

		public static double NeighborhoodTest(Vector s, Vector z, double mu)
		{
			double num = (s[0] + s[1]) * (s[0] + s[1]) - s.InnerProduct(s);
			double num2 = (z[0] + z[1]) * (z[0] + z[1]) - z.InnerProduct(z);
			double num3 = s.InnerProduct(z);
			return num * num2 / (num3 * mu);
		}

		public static void SimpleInitialization(Vector v)
		{
			v.ScaleBy(0.0);
			v[0] = 1.0 / Math.Sqrt(2.0);
			v[1] = v[0];
		}
	}
}
