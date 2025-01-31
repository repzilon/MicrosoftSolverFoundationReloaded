using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal static class SOCPUtilities
	{
		public static void Arw2e(Vector v, Vector VVe)
		{
			VVe.CopyFrom(v);
			VVe.ScaleBy(2.0 * v[0]);
			VVe[0] = v.InnerProduct(v);
		}

		public static void ArwArwe(Vector x, Vector y, Vector XYe)
		{
			XYe.CopyFrom(x);
			XYe.ScaleBy(y[0]);
			Vector.Daxpy(x[0], y, XYe);
			XYe[0] = x.InnerProduct(y);
		}

		public static void MultiplyByInvArw(Vector z, Vector r, Vector invZr)
		{
			double num = 2.0 * z[0] * z[0] - z.InnerProduct(z);
			double num2 = 2.0 * z[0] * r[0] - z.InnerProduct(r);
			double num3 = num2 / num;
			invZr.CopyFrom(r);
			Vector.Daxpy(0.0 - num3, z, invZr);
			invZr.ScaleBy(1.0 / z[0]);
			invZr[0] = num3;
		}

		public static void IncrementFirstElement(Vector v, double c)
		{
			v[0] += c;
		}
	}
}
