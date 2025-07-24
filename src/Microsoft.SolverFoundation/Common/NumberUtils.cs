using System;
using System.Runtime.InteropServices;

namespace Microsoft.SolverFoundation.Common
{
	internal static class NumberUtils
	{
		[StructLayout(LayoutKind.Explicit)]
		internal struct DoubleUlong
		{
			[FieldOffset(0)]
			public double dbl;

			[FieldOffset(0)]
			public ulong uu;
		}

		public static bool Is32Bit => IntPtr.Size == 4;

		public static bool IsFinite(double dbl)
		{
			DoubleUlong doubleUlong = default(DoubleUlong);
			doubleUlong.uu = 0uL;
			doubleUlong.dbl = dbl;
			return ((int)(doubleUlong.uu >> 52) & 0x7FF) < 2047;
		}

		public static void GetDoubleParts(double dbl, out int sign, out int exp, out ulong man, out bool fFinite)
		{
			DoubleUlong doubleUlong = default(DoubleUlong);
			doubleUlong.uu = 0uL;
			doubleUlong.dbl = dbl;
			sign = 1 - ((int)(doubleUlong.uu >> 62) & 2);
			man = doubleUlong.uu & 0xFFFFFFFFFFFFFL;
			exp = (int)(doubleUlong.uu >> 52) & 0x7FF;
			if (exp == 0) {
				fFinite = true;
				if (man != 0) {
					exp = -1074;
				}
			} else if (exp == 2047) {
				fFinite = false;
				exp = int.MaxValue;
			} else {
				fFinite = true;
				man |= 4503599627370496uL;
				exp -= 1075;
			}
		}

		public static double GetDoubleFromParts(int sign, int exp, ulong man)
		{
			DoubleUlong doubleUlong = default(DoubleUlong);
			doubleUlong.dbl = 0.0;
			if (man == 0) {
				doubleUlong.uu = 0uL;
			} else {
				int num = Statics.CbitHighZero(man) - 11;
				man = ((num >= 0) ? (man << num) : (man >> -num));
				exp -= num;
				exp += 1075;
				if (exp >= 2047) {
					doubleUlong.uu = 9218868437227405312uL;
				} else if (exp <= 0) {
					exp--;
					if (exp < -52) {
						doubleUlong.uu = 0uL;
					} else {
						doubleUlong.uu = man >> -exp;
					}
				} else {
					doubleUlong.uu = (man & 0xFFFFFFFFFFFFFL) | (ulong)((long)exp << 52);
				}
			}
			if (sign < 0) {
				doubleUlong.uu |= 9223372036854775808uL;
			}
			return doubleUlong.dbl;
		}

		public static void NormalizeExponent(ref double dbl, ref int exp)
		{
			DoubleUlong doubleUlong = default(DoubleUlong);
			doubleUlong.uu = 0uL;
			doubleUlong.dbl = dbl;
			int num = (int)(doubleUlong.uu >> 52) & 0x7FF;
			exp += num - 1023;
			doubleUlong.uu &= 9227875636482146303uL;
			doubleUlong.uu |= 4607182418800017408uL;
			dbl = doubleUlong.dbl;
		}

		public static double TruncateMantissaToSingleBit(double dbl)
		{
			DoubleUlong doubleUlong = default(DoubleUlong);
			doubleUlong.uu = 0uL;
			doubleUlong.dbl = dbl;
			if (doubleUlong.uu == 0) {
				return dbl;
			}
			switch ((int)(doubleUlong.uu >> 52) & 0x7FF) {
				case 2047:
					return dbl;
				default:
					doubleUlong.uu &= 18442240474082181120uL;
					break;
				case 0: {
						ulong num = doubleUlong.uu & 0xFFFFFFFFFFFFFL;
						ulong num2;
						while ((num2 = num & (num - 1)) != 0) {
							num = num2;
						}
						doubleUlong.uu = (doubleUlong.uu & 0x8000000000000000uL) | num;
						break;
					}
			}
			return doubleUlong.dbl;
		}
	}
}
