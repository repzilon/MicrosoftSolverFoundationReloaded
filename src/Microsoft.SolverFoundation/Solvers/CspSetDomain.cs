using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> An integer otherSet of choices {j, j, r, ..}
	/// </summary>
	internal class CspSetDomain : CspIntegerDomain
	{
		private int[] _set;

		/// <summary> How many distinct choices is this restriction allowing?
		/// </summary>
		public override int Count
		{
			[DebuggerStepThrough]
			get
			{
				return _set.Length;
			}
		}

		/// <summary> Return the otherSet of permitted choices.
		/// </summary>
		public int[] Set
		{
			[DebuggerStepThrough]
			get
			{
				return _set;
			}
		}

		/// <summary> The first value in the restriction otherSet
		/// </summary>
		internal override int First
		{
			[DebuggerStepThrough]
			get
			{
				return _set[0];
			}
		}

		/// <summary> The last value in the restriction otherSet
		/// </summary>
		internal override int Last
		{
			[DebuggerStepThrough]
			get
			{
				return _set[_set.Length - 1];
			}
		}

		/// <summary> Get the i'th value of the Domain.
		/// </summary>
		internal override int this[int i]
		{
			get
			{
				if (0 <= i && i < Count)
				{
					return _set[i];
				}
				throw new IndexOutOfRangeException(Resources.DomainIndexOutOfRange + ToString());
			}
		}

		/// <summary> An integer otherSet {j, j, r, ..}.
		/// </summary>
		private CspSetDomain(int[] orderedUniqueSet, ValueKind kind, int scale)
			: base(kind, scale)
		{
			_set = orderedUniqueSet;
		}

		/// <summary> An integer otherSet {j, j, r, ..}.
		/// </summary>
		internal static CspSolverDomain Create(int[] orderedUniqueSet, int from, int count)
		{
			if (count < 1)
			{
				return ConstraintSystem.DEmpty;
			}
			if (count - 1 == orderedUniqueSet[from + count - 1] - orderedUniqueSet[from])
			{
				return CspIntervalDomain.Create(orderedUniqueSet[from], orderedUniqueSet[from + count - 1]);
			}
			int[] array = new int[count];
			for (int i = from; i < from + count; i++)
			{
				array[i - from] = orderedUniqueSet[i];
			}
			return new CspSetDomain(array, ValueKind.Integer, 1);
		}

		/// <summary> An integer otherSet {j, j, r, ..}.
		/// </summary>
		internal static CspSolverDomain Create(params int[] orderedUniqueSet)
		{
			if (orderedUniqueSet != null && 0 < orderedUniqueSet.Length)
			{
				return Create(orderedUniqueSet, 0, orderedUniqueSet.Length);
			}
			return ConstraintSystem.DEmpty;
		}

		/// <summary> An integer otherSet {j, j, r, ..}.
		/// </summary>
		internal static CspSolverDomain Create(int precision, double[] orderedUniqueSet, int from, int count)
		{
			if (count < 1)
			{
				return ConstraintSystem.DEmpty;
			}
			if (count - 1 == (int)((orderedUniqueSet[from + count - 1] - orderedUniqueSet[from]) * (double)precision))
			{
				return CspIntervalDomain.Create(precision, orderedUniqueSet[from], orderedUniqueSet[from + count - 1]);
			}
			int[] array = new int[count];
			for (int i = from; i < from + count; i++)
			{
				array[i - from] = (int)Math.Round(orderedUniqueSet[i] * (double)precision, 0);
			}
			return new CspSetDomain(array, ValueKind.Decimal, precision);
		}

		/// <summary> An integer otherSet {j, j, r, ..}.
		/// </summary>
		internal static CspSolverDomain Create(int precision, params double[] orderedUniqueSet)
		{
			if (orderedUniqueSet != null && 0 < orderedUniqueSet.Length)
			{
				return Create(precision, orderedUniqueSet, 0, orderedUniqueSet.Length);
			}
			return ConstraintSystem.DEmpty;
		}

		internal override CspSolverDomain Clone()
		{
			return new CspSetDomain(_set, Kind, base.Scale);
		}

		internal static bool IsOrderedUniqueSet(int[] set, int from, int count)
		{
			for (int i = from; i < from + count - 1; i++)
			{
				if (set[i] >= set[i + 1])
				{
					return false;
				}
			}
			return true;
		}

		internal static bool IsOrderedUniqueSet(int precision, double[] set, int from, int count)
		{
			for (int i = from; i < from + count - 1; i++)
			{
				if (set[i] > set[i + 1] || CspIntegerDomain.AreDecimalsEqual(precision, set[i], set[i + 1]))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary> Does the domain include item?
		/// </summary>
		internal override bool Contains(int x)
		{
			return 0 <= Array.BinarySearch(_set, x);
		}

		/// <summary> Enumerate allowed choices from first to last inclusive.
		/// </summary>
		internal override IEnumerable<int> Forward(int first, int last)
		{
			first = Math.Max(first, _set[0]);
			last = Math.Min(last, _set[_set.Length - 1]);
			for (int i = CspIntegerDomain.FirstBound(first, _set); i < _set.Length && last >= _set[i]; i++)
			{
				yield return _set[i];
			}
		}

		/// <summary> Enumerate allowed choices from first to last inclusive.
		/// </summary>
		internal override IEnumerable<int> Backward(int last, int first)
		{
			first = Math.Max(first, _set[0]);
			last = Math.Min(last, _set[_set.Length - 1]);
			int i = CspIntegerDomain.LastBound(last, _set);
			while (0 <= i && _set[i] >= first)
			{
				yield return _set[i];
				i--;
			}
		}

		/// <summary> Check if this CspSolverDomain and the other CspSolverDomain have identical contents.
		/// </summary>
		public override bool SetEqual(CspDomain otherValueSet)
		{
			if (!(otherValueSet is CspSolverDomain cspSolverDomain) || Count != cspSolverDomain.Count || First != cspSolverDomain.First || Last != cspSolverDomain.Last)
			{
				return false;
			}
			if (Count < 3 || cspSolverDomain is CspIntervalDomain)
			{
				return true;
			}
			int num = 0;
			foreach (int item in cspSolverDomain.Forward())
			{
				if (item != _set[num++])
				{
					return false;
				}
			}
			return true;
		}

		/// <summary> The predecessor within the domain, of the given value, or Int32.MinValue if none.
		/// </summary>
		internal override int Pred(int x)
		{
			int num = Array.BinarySearch(_set, x);
			num = ((0 > num) ? (-num - 2) : (num - 1));
			if (0 > num)
			{
				return int.MinValue;
			}
			return _set[num];
		}

		/// <summary> The successor within the domain, of the given value, or Int32.MaxValue if none.
		/// </summary>
		internal override int Succ(int x)
		{
			int num = Array.BinarySearch(_set, x);
			num = ((0 > num) ? (-num - 1) : (num + 1));
			if (num >= _set.Length)
			{
				return int.MaxValue;
			}
			return _set[num];
		}

		internal override string AppendTo(string sline, int itemLimit)
		{
			StringBuilder stringBuilder = new StringBuilder(sline);
			int i;
			for (i = 0; i < itemLimit && i < _set.Length - 1; i++)
			{
				stringBuilder.Append(_set[i]).Append(", ");
			}
			if (i < _set.Length - 1)
			{
				stringBuilder.Append("..");
			}
			stringBuilder.Append(_set[_set.Length - 1]);
			return stringBuilder.ToString();
		}

		internal override string AppendTo(string sline, int itemLimit, CspVariable var)
		{
			StringBuilder stringBuilder = new StringBuilder(sline);
			int i;
			for (i = 0; i < itemLimit && i < _set.Length - 1; i++)
			{
				stringBuilder.Append(var.GetValue(_set[i]).ToString()).Append(", ");
			}
			if (i < _set.Length - 1)
			{
				stringBuilder.Append("..");
			}
			stringBuilder.Append(var.GetValue(_set[_set.Length - 1]).ToString());
			return stringBuilder.ToString();
		}

		/// <summary> String representation of this set domain
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}}}", new object[1] { AppendTo("SetDomain [", 9) });
		}

		/// <summary> Return the index for the exact match of value x if the Domain were enumerated.
		/// </summary>
		internal override int IndexOf(int x)
		{
			int num = Array.BinarySearch(_set, x);
			if (0 > num)
			{
				return int.MinValue;
			}
			return num;
		}

		/// <summary> Intersect with the given bounds.  Return false if no change.
		/// </summary>
		/// <param name="min">the lowerbound</param>
		/// <param name="max">the upperbound</param>
		/// <param name="otherValueSet"> Allows sharing the other set, if that proves to be answer </param>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Intersect(int min, int max, CspIntervalDomain otherValueSet, out CspSolverDomain newD)
		{
			int num = _set[0];
			int num2 = _set[_set.Length - 1];
			if (max < num || num2 < min)
			{
				newD = ConstraintSystem.DEmpty;
				return true;
			}
			if (min <= num && num2 <= max)
			{
				newD = this;
				return false;
			}
			num = CspIntegerDomain.FirstBound(min, _set);
			num2 = CspIntegerDomain.LastBound(max, _set);
			if (otherValueSet != null && num2 - num == max - min)
			{
				newD = otherValueSet;
				return true;
			}
			newD = Create(_set, num, num2 + 1 - num);
			return true;
		}

		/// <summary> Intersect with the given ordered distinct otherSet.  Return false if no change.
		/// </summary>
		/// <param name="orderedUniqueSet">The set to intersect</param>
		/// <param name="otherValueSet"> Allows sharing the other set, if that proves to be answer </param>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Intersect(int[] orderedUniqueSet, CspSetDomain otherValueSet, out CspSolverDomain newD)
		{
			if (orderedUniqueSet == null || orderedUniqueSet.Length < 1)
			{
				newD = ConstraintSystem.DEmpty;
				return true;
			}
			int num = Math.Min(_set.Length, orderedUniqueSet.Length);
			int[] array = new int[num];
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			while (num2 < _set.Length && num3 < orderedUniqueSet.Length)
			{
				if (_set[num2] < orderedUniqueSet[num3])
				{
					num2++;
					continue;
				}
				if (_set[num2] == orderedUniqueSet[num3])
				{
					array[num4++] = _set[num2++];
				}
				num3++;
			}
			if (Count == num4)
			{
				newD = this;
				return false;
			}
			if (otherValueSet != null && num4 == orderedUniqueSet.Length)
			{
				newD = otherValueSet;
				return true;
			}
			newD = Create(array, 0, num4);
			return true;
		}

		/// <summary> Remove the range [min..max] from the otherSet.  Return false if no change.
		/// </summary>
		/// <param name="min">the lowerbound</param>
		/// <param name="max">the upperbound</param>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Exclude(int min, int max, out CspSolverDomain newD)
		{
			int num = _set[0];
			int num2 = _set[_set.Length - 1];
			if (max < min || max < num || num2 < min)
			{
				newD = this;
				return false;
			}
			if (min <= num && num2 <= max)
			{
				newD = ConstraintSystem.DEmpty;
				return true;
			}
			num = CspIntegerDomain.FirstBound(min, _set);
			num2 = CspIntegerDomain.LastBound(max, _set);
			int num3 = _set.Length - (num2 - num + 1);
			if (num3 == _set.Length)
			{
				newD = this;
				return false;
			}
			int[] array = new int[num3];
			int i;
			for (i = 0; i < num; i++)
			{
				array[i] = _set[i];
			}
			int num4 = num2 + 1;
			while (num4 < _set.Length)
			{
				array[i++] = _set[num4++];
			}
			newD = Create(array);
			return true;
		}

		/// <summary> Remove the orderedUniqueSet from the interval.  Return false if no change.
		/// </summary>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <param name="orderedUniqueSet">The set to exclude</param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Exclude(out CspSolverDomain newD, params int[] orderedUniqueSet)
		{
			if (orderedUniqueSet == null || orderedUniqueSet.Length < 1)
			{
				newD = this;
				return false;
			}
			int num = _set[0];
			int num2 = _set[_set.Length - 1];
			int num3 = orderedUniqueSet[0];
			int num4 = orderedUniqueSet[orderedUniqueSet.Length - 1];
			if (num4 < num || num2 < num3)
			{
				newD = this;
				return false;
			}
			int[] array = new int[_set.Length];
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			while (num5 < _set.Length && num6 < orderedUniqueSet.Length)
			{
				if (orderedUniqueSet[num6] < _set[num5])
				{
					num6++;
					continue;
				}
				if (_set[num5] != orderedUniqueSet[num6])
				{
					array[num7++] = _set[num5];
				}
				num5++;
			}
			if (num7 == num5)
			{
				newD = this;
				return false;
			}
			while (num5 < _set.Length)
			{
				array[num7++] = _set[num5++];
			}
			newD = Create(array, 0, num7);
			return true;
		}
	}
}
