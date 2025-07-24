using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.SolverFoundation.Common
{
	internal static class Statics
	{
		private const int kcbitUint = 32;

		public static bool IsInteger(double num)
		{
			double num2 = num - Math.Floor(num);
			return Math.Min(1.0 - num2, num2) < 1E-06;
		}

		public static double GetFraction(double num)
		{
			double num2 = num - Math.Floor(num);
			return Math.Min(1.0 - num2, num2);
		}

		public static Rational GetFraction(Rational num)
		{
			Rational rational = num - num.GetFloor();
			Rational rational2 = 1 - rational;
			if (!(rational2 < rational)) {
				return rational;
			}
			return rational2;
		}

		public static double GetDoubleRoundUp(Rational num)
		{
			double num2 = num.ToDouble();
			Rational rational = num - num2;
			if (rational > 0) {
				Rational rational2;
				for (rational2 = 10; num - (num + rational * rational2).ToDouble() > 0; rational2 *= (Rational)10) {
				}
				num2 = (num + rational * rational2).ToDouble();
			}
			return num2;
		}

		public static double GetDoubleRoundDown(Rational num)
		{
			double num2 = num.ToDouble();
			Rational rational = num - num2;
			if (rational < 0) {
				Rational rational2;
				for (rational2 = 10; num - (num + rational * rational2).ToDouble() < 0; rational2 *= (Rational)10) {
				}
				num2 = (num + rational * rational2).ToDouble();
			}
			return num2;
		}

		public static void Swap<T>(ref T a, ref T b)
		{
			T val = a;
			a = b;
			b = val;
		}

		public static uint Gcd(uint u1, uint u2)
		{
			if (u2 <= u1) {
				if (u2 == 0) {
					return u1;
				}
				u1 %= u2;
			}
			while (true) {
				if (u1 == 0) {
					return u2;
				}
				u2 %= u1;
				if (u2 == 0) {
					break;
				}
				u1 %= u2;
			}
			return u1;
		}

		public static ulong Gcd(ulong uu1, ulong uu2)
		{
			if (uu1 >= uu2) {
				goto IL_0004;
			}
			goto IL_0028;
		IL_0004:
			if (uu1 > uint.MaxValue) {
				if (uu2 == 0) {
					return uu1;
				}
				int num = 32;
				while (true) {
					uu1 -= uu2;
					if (uu1 < uu2) {
						break;
					}
					if (--num == 0) {
						uu1 %= uu2;
						break;
					}
				}
				goto IL_0028;
			}
			goto IL_004e;
		IL_004e:
			uint num2 = (uint)uu1;
			uint num3 = (uint)uu2;
			if (num2 >= num3) {
				goto IL_0058;
			}
			goto IL_0077;
		IL_0077:
			if (num2 == 0) {
				return num3;
			}
			int num4 = 32;
			while (true) {
				num3 -= num2;
				if (num3 < num2) {
					break;
				}
				if (--num4 == 0) {
					num3 %= num2;
					break;
				}
			}
			goto IL_0058;
		IL_0028:
			if (uu2 > uint.MaxValue) {
				if (uu1 == 0) {
					return uu2;
				}
				int num5 = 32;
				while (true) {
					uu2 -= uu1;
					if (uu2 < uu1) {
						break;
					}
					if (--num5 == 0) {
						uu2 %= uu1;
						break;
					}
				}
				goto IL_0004;
			}
			goto IL_004e;
		IL_0058:
			if (num3 == 0) {
				return num2;
			}
			int num6 = 32;
			while (true) {
				num2 -= num3;
				if (num2 < num3) {
					break;
				}
				if (--num6 == 0) {
					num2 %= num3;
					break;
				}
			}
			goto IL_0077;
		}

		public static ulong MakeUlong(uint uHi, uint uLo)
		{
			return ((ulong)uHi << 32) | uLo;
		}

		public static uint GetLo(ulong uu)
		{
			return (uint)uu;
		}

		public static uint GetHi(ulong uu)
		{
			return (uint)(uu >> 32);
		}

		public static uint Abs(int a)
		{
			uint num = (uint)(a >> 31);
			return ((uint)a ^ num) - num;
		}

		public static ulong Abs(long a)
		{
			ulong num = (ulong)(a >> 63);
			return ((ulong)a ^ num) - num;
		}

		public static int Size<T>(T[] rgv)
		{
			if (rgv == null) {
				return 0;
			}
			return rgv.Length;
		}

		public static int Size<T>(IList<T> list)
		{
			return list?.Count ?? 0;
		}

		public static void TrimList<T>(List<T> list, int cv)
		{
			list.RemoveRange(cv, list.Count - cv);
		}

		public static T PopList<T>(List<T> list)
		{
			T result = list[list.Count - 1];
			list.RemoveAt(list.Count - 1);
			return result;
		}

		public static void MoveItem<T>(T[] rgv, int ivSrc, int ivDst)
		{
			if (ivSrc != ivDst) {
				T val = rgv[ivSrc];
				if (ivSrc < ivDst) {
					Array.Copy(rgv, ivSrc + 1, rgv, ivSrc, ivDst - ivSrc);
				} else {
					Array.Copy(rgv, ivDst, rgv, ivDst + 1, ivSrc - ivDst);
				}
				rgv[ivDst] = val;
			}
		}

		public static void EnsureArraySize<T>(ref T[] rgv, int cv)
		{
			if (rgv.Length < cv) {
				Array.Resize(ref rgv, Math.Max(cv, rgv.Length + rgv.Length / 2));
			}
		}

		public static void QuickSort<T>(T[] rgv, int ivFirst, int ivLast, Comparison<T> cmp)
		{
			while (ivFirst < ivLast) {
				int num = ivFirst;
				int num2 = ivLast;
				int num3 = num + num2 >> 1;
				T x = rgv[num3];
				while (true) {
					if (cmp(x, rgv[num]) > 0) {
						num++;
						continue;
					}
					while (cmp(x, rgv[num2]) < 0) {
						num2--;
					}
					if (num > num2) {
						break;
					}
					if (num < num2) {
						Swap(ref rgv[num], ref rgv[num2]);
					}
					num++;
					num2--;
					if (num > num2) {
						break;
					}
				}
				if (num2 - ivFirst <= ivLast - num) {
					if (ivFirst < num2) {
						QuickSort(rgv, ivFirst, num2, cmp);
					}
					ivFirst = num;
				} else {
					if (num < ivLast) {
						QuickSort(rgv, num, ivLast, cmp);
					}
					ivLast = num2;
				}
			}
		}

		public static void QuickSort(int[] rgv, int ivFirst, int ivLast)
		{
			if (ivLast - ivFirst < 20) {
				QuadSort(rgv, ivFirst, ivLast);
				return;
			}
			while (ivFirst < ivLast) {
				int num = ivFirst;
				int num2 = ivLast;
				int num3 = num + num2 >> 1;
				int num4 = rgv[num3];
				while (true) {
					if (num4 > rgv[num]) {
						num++;
						continue;
					}
					while (num4 < rgv[num2]) {
						num2--;
					}
					if (num > num2) {
						break;
					}
					if (num < num2) {
						Swap(ref rgv[num], ref rgv[num2]);
					}
					num++;
					num2--;
					if (num > num2) {
						break;
					}
				}
				if (num2 - ivFirst <= ivLast - num) {
					if (ivFirst < num2) {
						QuickSort(rgv, ivFirst, num2);
					}
					ivFirst = num;
				} else {
					if (num < ivLast) {
						QuickSort(rgv, num, ivLast);
					}
					ivLast = num2;
				}
			}
		}

		public static void QuadSort(int[] rgv, int ivFirst, int ivLast)
		{
			for (int num = ivLast; num > ivFirst; num--) {
				int num2 = num;
				int num3 = rgv[num2];
				int num4 = num;
				while (--num4 >= ivFirst) {
					if (num3 < rgv[num4]) {
						num2 = num4;
						num3 = rgv[num2];
					}
				}
				if (num2 != num) {
					Swap(ref rgv[num2], ref rgv[num]);
				}
			}
		}

		/// <summary>Selection sort.
		/// </summary>
		/// <typeparam name="T">Item type.</typeparam>
		/// <param name="rgv">Keys.</param>
		/// <param name="items">Items.</param>
		/// <param name="ivFirst">First index to sort.</param>
		/// <param name="ivLast">Last index to sort (inclusive).</param>
		public static void QuadSort<T>(double[] rgv, T[] items, int ivFirst, int ivLast)
		{
			for (int num = ivLast; num > ivFirst; num--) {
				int num2 = num;
				double num3 = rgv[num2];
				int num4 = num;
				while (--num4 >= ivFirst) {
					if (num3 < rgv[num4]) {
						num2 = num4;
						num3 = rgv[num2];
					}
				}
				if (num2 != num) {
					Swap(ref rgv[num2], ref rgv[num]);
					Swap(ref items[num2], ref items[num]);
				}
			}
		}

		public static void QuickSortIndirect(int[] rgiv, int[] rgv, int iivFirst, int iivLast)
		{
			if (iivLast - iivFirst < 20) {
				QuadSortIndirect(rgiv, rgv, iivFirst, iivLast);
				return;
			}
			while (iivFirst < iivLast) {
				int num = iivFirst;
				int num2 = iivLast;
				int num3 = num + num2 >> 1;
				int num4 = rgv[rgiv[num3]];
				while (true) {
					if (num4 > rgv[rgiv[num]]) {
						num++;
						continue;
					}
					while (num4 < rgv[rgiv[num2]]) {
						num2--;
					}
					if (num > num2) {
						break;
					}
					if (num < num2) {
						Swap(ref rgiv[num], ref rgiv[num2]);
					}
					num++;
					num2--;
					if (num > num2) {
						break;
					}
				}
				if (num2 - iivFirst <= iivLast - num) {
					if (iivFirst < num2) {
						QuickSortIndirect(rgiv, rgv, iivFirst, num2);
					}
					iivFirst = num;
				} else {
					if (num < iivLast) {
						QuickSortIndirect(rgiv, rgv, num, iivLast);
					}
					iivLast = num2;
				}
			}
		}

		public static void QuickSortIndirect(int[] rgiv, Rational[] rgv, int iivFirst, int iivLast)
		{
			if (iivLast - iivFirst < 20) {
				QuadSortIndirect(rgiv, rgv, iivFirst, iivLast);
				return;
			}
			while (iivFirst < iivLast) {
				int num = iivFirst;
				int num2 = iivLast;
				int num3 = num + num2 >> 1;
				Rational rational = rgv[rgiv[num3]];
				while (true) {
					if (rational > rgv[rgiv[num]]) {
						num++;
						continue;
					}
					while (rational < rgv[rgiv[num2]]) {
						num2--;
					}
					if (num > num2) {
						break;
					}
					if (num < num2) {
						Swap(ref rgiv[num], ref rgiv[num2]);
					}
					num++;
					num2--;
					if (num > num2) {
						break;
					}
				}
				if (num2 - iivFirst <= iivLast - num) {
					if (iivFirst < num2) {
						QuickSortIndirect(rgiv, rgv, iivFirst, num2);
					}
					iivFirst = num;
				} else {
					if (num < iivLast) {
						QuickSortIndirect(rgiv, rgv, num, iivLast);
					}
					iivLast = num2;
				}
			}
		}

		public static void QuadSortIndirect(int[] rgiv, int[] rgv, int iivFirst, int iivLast)
		{
			for (int num = iivLast; num > iivFirst; num--) {
				int num2 = num;
				int num3 = rgv[rgiv[num2]];
				int num4 = num;
				while (--num4 >= iivFirst) {
					if (num3 < rgv[rgiv[num4]]) {
						num2 = num4;
						num3 = rgv[rgiv[num2]];
					}
				}
				if (num2 != num) {
					Swap(ref rgiv[num2], ref rgiv[num]);
				}
			}
		}

		public static void QuadSortIndirect(int[] rgiv, Rational[] rgv, int iivFirst, int iivLast)
		{
			for (int num = iivLast; num > iivFirst; num--) {
				int num2 = num;
				Rational rational = rgv[rgiv[num2]];
				int num3 = num;
				while (--num3 >= iivFirst) {
					if (rational < rgv[rgiv[num3]]) {
						num2 = num3;
						rational = rgv[rgiv[num2]];
					}
				}
				if (num2 != num) {
					Swap(ref rgiv[num2], ref rgiv[num]);
				}
			}
		}

		public static uint CombineHash(uint u1, uint u2)
		{
			return ((u1 << 7) | (u1 >> 25)) ^ u2;
		}

		public static int CombineHash(int n1, int n2)
		{
			return (int)CombineHash((uint)n1, (uint)n2);
		}

		/// <summary>
		/// Hash the characters in a string. This MUST produce the same result
		/// as HashStrBldr produces for the same characters.
		/// </summary>
		public static uint HashString(string str)
		{
			uint num = 5381u;
			uint num2 = num;
			int num3 = str.Length;
			while (num3 > 0) {
				num = ((num << 5) + num) ^ str[--num3];
				if (num3 <= 0) {
					break;
				}
				num2 = ((num2 << 5) + num2) ^ str[--num3];
			}
			return HashUint(num + num2 * 1566083941);
		}

		/// <summary>
		/// Hash the characters in a string builder. This MUST produce the same result
		/// as HashString produces for the same characters.
		/// </summary>
		public static uint HashStrBldr(StringBuilder sb)
		{
			uint num = 5381u;
			uint num2 = num;
			int num3 = sb.Length;
			while (num3 > 0) {
				num = ((num << 5) + num) ^ sb[--num3];
				if (num3 <= 0) {
					break;
				}
				num2 = ((num2 << 5) + num2) ^ sb[--num3];
			}
			return HashUint(num + num2 * 1566083941);
		}

		public static uint HashUint(uint u)
		{
			ulong num = (ulong)u * 2146538777uL;
			return (uint)((int)num + (int)(num >> 32));
		}

		public static int CbitHighZero(uint u)
		{
			if (u == 0) {
				return 32;
			}
			int num = 0;
			if ((u & 0xFFFF0000u) == 0) {
				num += 16;
				u <<= 16;
			}
			if ((u & 0xFF000000u) == 0) {
				num += 8;
				u <<= 8;
			}
			if ((u & 0xF0000000u) == 0) {
				num += 4;
				u <<= 4;
			}
			if ((u & 0xC0000000u) == 0) {
				num += 2;
				u <<= 2;
			}
			if ((u & 0x80000000u) == 0) {
				num++;
			}
			return num;
		}

		public static int CbitLowZero(uint u)
		{
			if (u == 0) {
				return 32;
			}
			int num = 0;
			if ((u & 0xFFFF) == 0) {
				num += 16;
				u >>= 16;
			}
			if ((u & 0xFF) == 0) {
				num += 8;
				u >>= 8;
			}
			if ((u & 0xF) == 0) {
				num += 4;
				u >>= 4;
			}
			if ((u & 3) == 0) {
				num += 2;
				u >>= 2;
			}
			if ((u & 1) == 0) {
				num++;
			}
			return num;
		}

		public static int CbitHighZero(ulong uu)
		{
			if ((uu & 0xFFFFFFFF00000000uL) == 0) {
				return 32 + CbitHighZero((uint)uu);
			}
			return CbitHighZero((uint)(uu >> 32));
		}

		public static int CbitLowZero(ulong uu)
		{
			if ((uu & 0xFFFFFFFFu) == 0) {
				return 32 + CbitLowZero((uint)(uu >> 32));
			}
			return CbitLowZero((uint)uu);
		}

		public static int Cbit(uint u)
		{
			u = (u & 0x55555555) + ((u >> 1) & 0x55555555);
			u = (u & 0x33333333) + ((u >> 2) & 0x33333333);
			u = (u & 0xF0F0F0F) + ((u >> 4) & 0xF0F0F0F);
			u = (u & 0xFF00FF) + ((u >> 8) & 0xFF00FF);
			return (ushort)u + (ushort)(u >> 16);
		}

		public static int Cbit(ulong uu)
		{
			uu = (uu & 0x5555555555555555L) + ((uu >> 1) & 0x5555555555555555L);
			uu = (uu & 0x3333333333333333L) + ((uu >> 2) & 0x3333333333333333L);
			uu = (uu & 0xF0F0F0F0F0F0F0FL) + ((uu >> 4) & 0xF0F0F0F0F0F0F0FL);
			uu = (uu & 0xFF00FF00FF00FFL) + ((uu >> 8) & 0xFF00FF00FF00FFL);
			uu = (uu & 0xFFFF0000FFFFL) + ((uu >> 16) & 0xFFFF0000FFFFL);
			return (int)uu + (int)(uu >> 32);
		}

		public static T[] EnumerableToArray<T>(IEnumerable<T> iter, int limit)
		{
			List<T> list = new List<T>();
			int num = 0;
			foreach (T item in iter) {
				if (num < limit) {
					list.Add(item);
					num++;
					continue;
				}
				break;
			}
			return list.ToArray();
		}

		public static T[] EnumerableToArray<T>(IEnumerable<T> iter)
		{
			List<T> list = new List<T>();
			foreach (T item in iter) {
				list.Add(item);
			}
			return list.ToArray();
		}

		public static IEnumerable<T> EmptyIter<T>()
		{
			yield break;
		}

		public static IEnumerable<T> SingleIter<T>(T t)
		{
			yield return t;
		}

		public static IEnumerable<T> SingleOrEmptyIter<T>(T t)
		{
			if (t != null) {
				yield return t;
			}
		}

		public static IEnumerable<D> ImplicitCastIter<S, D>(IEnumerable<S> ebleSrc) where S : D
		{
			foreach (S item in ebleSrc) {
				yield return (D)(object)item;
			}
		}

		public static IEnumerable<D> ExplicitCastIter<S, D>(IEnumerable<S> ebleSrc) where D : S
		{
			foreach (S item in ebleSrc) {
				yield return (D)(object)item;
			}
		}

		internal static string VectorToString<T>(IEnumerable<T> vector, int count, int maxItemsToPrint)
		{
			DebugContracts.NonNull(vector);
			int count2 = Math.Min(maxItemsToPrint, count);
			string text = string.Join(", ", from x in vector.Take(count2)
											select x.ToString());
			if (count >= maxItemsToPrint) {
				text += "...";
			}
			return text;
		}

		internal static string DebugCommaDelimitedList(IEnumerable<object> inputValues)
		{
			return string.Join(",", inputValues.Select((object inputValue) => inputValue.ToString()).ToArray());
		}

		internal static string JoinArrayToString(object[] indexes)
		{
			DebugContracts.NonNull(indexes);
			StringBuilder stringBuilder = new StringBuilder();
			if (indexes.Length > 0) {
				stringBuilder.Append("(");
				for (int i = 0; i < indexes.Length; i++) {
					if (i > 0) {
						stringBuilder.Append(",");
					}
					stringBuilder.Append(indexes[i]);
				}
				stringBuilder.Append(")");
			}
			return stringBuilder.ToString();
		}
	}
}
