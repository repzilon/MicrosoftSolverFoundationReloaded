using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between 3 integer variables X, Y and Z imposing that 
	///   X * Y be equal to Z
	/// </summary>
	internal class ProductByVariable : TernaryConstraint<IntegerVariable, IntegerVariable, IntegerVariable>
	{
		private List<long> _zvalues;

		private List<long> _ovalues;

		public ProductByVariable(Problem p, IntegerVariable x, IntegerVariable y, IntegerVariable z)
			: base(p, x, y, z)
		{
			x.SubscribeToAnyModification(WhenXmodified);
			y.SubscribeToAnyModification(WhenYmodified);
			z.SubscribeToAnyModification(WhenZmodified);
			_zvalues = new List<long>(2);
			_ovalues = new List<long>(4);
		}

		private bool WhenXmodified()
		{
			if (NarrowZ())
			{
				return NarrowXorY(_y, _x);
			}
			return false;
		}

		private bool WhenYmodified()
		{
			if (NarrowZ())
			{
				return NarrowXorY(_x, _y);
			}
			return false;
		}

		private bool WhenZmodified()
		{
			if (NarrowXorY(_x, _y))
			{
				return NarrowXorY(_y, _x);
			}
			return false;
		}

		private bool NarrowZ()
		{
			long lowerBound = _x.LowerBound;
			long upperBound = _x.UpperBound;
			long lowerBound2 = _y.LowerBound;
			long upperBound2 = _y.UpperBound;
			_ovalues.Clear();
			_ovalues.Add(lowerBound * lowerBound2);
			_ovalues.Add(lowerBound * upperBound2);
			_ovalues.Add(upperBound * lowerBound2);
			_ovalues.Add(upperBound * upperBound2);
			long num = long.MaxValue;
			long num2 = long.MinValue;
			for (int i = 0; i < 4; i++)
			{
				num = Math.Min(num, _ovalues[i]);
				num2 = Math.Max(num2, _ovalues[i]);
			}
			return _z.ImposeRange(num, num2, base.Cause);
		}

		/// <summary>
		///   Code common to narrowing of X or Y (works symmetrically)
		/// </summary>
		/// <param name="nar">the variable that is being narrowed</param>
		/// <param name="other">the other variable</param>
		private bool NarrowXorY(IntegerVariable nar, IntegerVariable other)
		{
			long lowerBound = other.LowerBound;
			long upperBound = other.UpperBound;
			long lowerBound2 = _z.LowerBound;
			long upperBound2 = _z.UpperBound;
			if (lowerBound == 0 && upperBound == 0)
			{
				return true;
			}
			if (lowerBound <= 0 && 0 <= upperBound && lowerBound2 <= 0 && 0 <= upperBound2)
			{
				return true;
			}
			_zvalues.Clear();
			_zvalues.Add(lowerBound2);
			_zvalues.Add(upperBound2);
			_ovalues.Clear();
			if (lowerBound != 0)
			{
				_ovalues.Add(lowerBound);
			}
			if (upperBound != 0)
			{
				_ovalues.Add(upperBound);
			}
			if (lowerBound < -1 && -1 < upperBound)
			{
				_ovalues.Add(-1L);
			}
			if (lowerBound < 1 && 1 < upperBound)
			{
				_ovalues.Add(1L);
			}
			long num = long.MaxValue;
			long num2 = long.MinValue;
			for (int num3 = _zvalues.Count - 1; num3 >= 0; num3--)
			{
				long num4 = _zvalues[num3];
				for (int num5 = _ovalues.Count - 1; num5 >= 0; num5--)
				{
					long num6 = _ovalues[num5];
					double num7 = (double)num4 / (double)num6;
					long result = (long)Math.Floor(num7);
					Utils.CorrectFloorOfDiv(num4, num6, ref result);
					num = Math.Min(num, result);
					long result2 = (long)Math.Ceiling(num7);
					Utils.CorrectCeilOfDiv(num4, num6, ref result2);
					num2 = Math.Max(num2, result2);
				}
			}
			return nar.ImposeRange(num, num2, base.Cause);
		}
	}
}
