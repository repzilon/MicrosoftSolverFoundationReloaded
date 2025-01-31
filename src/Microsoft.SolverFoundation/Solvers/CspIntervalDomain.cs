using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> An integer interval [min .. max].
	/// </summary>
	internal class CspIntervalDomain : CspIntegerDomain, IVisitable
	{
		private int _min;

		private int _max;

		/// <summary> How many distinct choices is this restriction allowing?
		/// </summary>
		public override int Count
		{
			[DebuggerStepThrough]
			get
			{
				return _max - _min + 1;
			}
		}

		/// <summary> The first value in the restriction otherSet
		/// </summary>
		internal override int First
		{
			[DebuggerStepThrough]
			get
			{
				return _min;
			}
		}

		/// <summary> The last value in the restriction otherSet
		/// </summary>
		internal override int Last
		{
			[DebuggerStepThrough]
			get
			{
				return _max;
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
					return i + _min;
				}
				throw new IndexOutOfRangeException(Resources.DomainIndexOutOfRange + ToString());
			}
		}

		protected CspIntervalDomain(int min, int max, ValueKind kind, int scale)
			: base(kind, scale)
		{
			_min = min;
			_max = max;
		}

		public void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		/// <summary> An integer interval [min .. max].
		/// </summary>
		internal static CspSolverDomain Create(int min, int max)
		{
			return new CspIntervalDomain(min, max, ValueKind.Integer, 1);
		}

		/// <summary>
		/// A discrete real interval [min .. max] with the given precision.
		/// </summary>
		/// <param name="min">The min value of the interval</param>
		/// <param name="max">The max value of the interval</param>
		/// <param name="precision">The values in the interval are accurate upto 1/precision</param>
		/// <returns></returns>
		internal static CspSolverDomain Create(int precision, double min, double max)
		{
			return new CspIntervalDomain((int)Math.Round(min * (double)precision, 0), (int)Math.Round(max * (double)precision, 0), ValueKind.Decimal, precision);
		}

		internal override CspSolverDomain Clone()
		{
			return new CspIntervalDomain(_min, _max, Kind, base.Scale);
		}

		/// <summary> Does the domain include item?
		/// </summary>
		internal override bool Contains(int x)
		{
			if (_min <= x)
			{
				return x <= _max;
			}
			return false;
		}

		/// <summary> Enumerate allowed choices from first to last inclusive.
		/// </summary>
		internal override IEnumerable<int> Forward(int first, int last)
		{
			first = Math.Max(first, _min);
			last = Math.Min(last, _max);
			for (int i = first; i <= last; i++)
			{
				yield return i;
			}
		}

		/// <summary> Enumerate allowed choices from first to last inclusive.
		/// </summary>
		internal override IEnumerable<int> Backward(int last, int first)
		{
			first = Math.Max(first, _min);
			last = Math.Min(last, _max);
			int i = last;
			while (first <= i)
			{
				yield return i;
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
			return true;
		}

		/// <summary> The predecessor within the domain, of the given value, or Int32.MinValue if none.
		/// </summary>
		internal override int Pred(int x)
		{
			if (_max >= x)
			{
				if (_min >= x)
				{
					return int.MinValue;
				}
				return x - 1;
			}
			return _max;
		}

		/// <summary> The successor within the domain, of the given value, or Int32.MaxValue if none.
		/// </summary>
		internal override int Succ(int x)
		{
			if (x >= _min)
			{
				if (x >= _max)
				{
					return int.MaxValue;
				}
				return x + 1;
			}
			return _min;
		}

		internal override string AppendTo(string line, int itemLimit)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}{1}..{2}", new object[3] { line, _min, _max });
		}

		internal override string AppendTo(string line, int itemLimit, CspVariable var)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}{1}..{2}", new object[3]
			{
				line,
				var.GetValue(_min),
				var.GetValue(_max)
			});
		}

		/// <summary> String representation of this interval domain
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}]", new object[1] { AppendTo("IntervalDomain [", 2) });
		}

		/// <summary> Return the index for the exact match of value x if the Domain were enumerated.
		/// </summary>
		internal override int IndexOf(int x)
		{
			if (_min <= x && x <= _max)
			{
				return x - _min;
			}
			return int.MinValue;
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
			if (max < _min || _max < min)
			{
				newD = ConstraintSystem.DEmpty;
				return true;
			}
			if (min <= _min && _max <= max)
			{
				newD = this;
				return false;
			}
			if (otherValueSet != null && _min <= min && max <= _max)
			{
				newD = otherValueSet;
				return true;
			}
			newD = Create(Math.Max(_min, min), Math.Min(_max, max));
			return true;
		}

		/// <summary> Intersect with the given ordered distinct otherSet.  Return false if no change.
		/// </summary>
		/// <param name="orderedUniqueSet">The set to </param>
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
			int num = orderedUniqueSet.Length;
			int num2 = orderedUniqueSet[0];
			int num3 = orderedUniqueSet[num - 1];
			if (num3 < _min || _max < num2)
			{
				newD = ConstraintSystem.DEmpty;
				return true;
			}
			if (otherValueSet != null && _min <= num2 && num3 <= _max)
			{
				newD = otherValueSet;
				return true;
			}
			int num4 = int.MaxValue;
			num2 = int.MinValue;
			for (int i = 0; i < num; i++)
			{
				int num5 = orderedUniqueSet[i];
				if (num5 <= num2)
				{
					throw new InvalidOperationException(Resources.DomainValueOutOfRange + ToString());
				}
				num2 = num5;
				if (_min <= num5)
				{
					if (num5 > _max)
					{
						num = i;
						break;
					}
					if (i < num4)
					{
						num4 = i;
					}
				}
			}
			if (num <= num4)
			{
				newD = ConstraintSystem.DEmpty;
				return true;
			}
			if (Count == num - num4)
			{
				newD = this;
				return false;
			}
			newD = CspSetDomain.Create(orderedUniqueSet, num4, num - num4);
			return true;
		}

		/// <summary> Remove the range [min..max] from the interval.  Return false if no change.
		/// </summary>
		/// <param name="min">the lowerbound</param>
		/// <param name="max">the upperbound</param>
		/// <param name="newD"> New CspSolverDomain, or "this" if no change. </param>
		/// <returns> True iff a narrower restraint is resulting. </returns>
		internal override bool Exclude(int min, int max, out CspSolverDomain newD)
		{
			if (max < min || max < _min || _max < min)
			{
				newD = this;
				return false;
			}
			if (min <= _min && _max <= max)
			{
				newD = ConstraintSystem.DEmpty;
				return true;
			}
			if (min <= _min)
			{
				newD = Create(max + 1, _max);
				return true;
			}
			if (_max <= max)
			{
				newD = Create(_min, min - 1);
				return true;
			}
			int num = _max - max + (min - _min);
			if (5000 < num || (1000 < num && max - min < num))
			{
				newD = this;
				return false;
			}
			int[] array = new int[num];
			int num2 = 0;
			int min2 = _min;
			while (min2 < min)
			{
				array[num2++] = min2++;
			}
			min2 = max + 1;
			while (min2 <= _max)
			{
				array[num2++] = min2++;
			}
			newD = CspSetDomain.Create(array);
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
			int num = orderedUniqueSet[0];
			int num2 = orderedUniqueSet[orderedUniqueSet.Length - 1];
			if (num2 < _min || _max < num)
			{
				newD = this;
				return false;
			}
			int num3 = CspIntegerDomain.FirstBound(_min, orderedUniqueSet);
			int num4 = CspIntegerDomain.LastBound(_max, orderedUniqueSet);
			int num5 = Count - (num4 - num3 + 1);
			if (num5 == Count)
			{
				newD = this;
				return false;
			}
			if (5000 < num5)
			{
				newD = this;
				return false;
			}
			int[] array = new int[num5];
			int num6 = 0;
			int i = _min;
			int num7 = orderedUniqueSet[num3];
			for (; i <= _max; i++)
			{
				if (i < num7)
				{
					array[num6++] = i;
				}
				else
				{
					num7 = ((num3 != num4) ? orderedUniqueSet[++num3] : int.MaxValue);
				}
			}
			newD = CspSetDomain.Create(array);
			return true;
		}
	}
}
