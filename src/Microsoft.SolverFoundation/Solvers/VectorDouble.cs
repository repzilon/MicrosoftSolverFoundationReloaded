using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal sealed class VectorDouble : Vector<double>
	{
		public VectorDouble(int rcLim)
			: base(rcLim, 0)
		{
		}

		public VectorDouble(int rcLim, int cslotInit)
			: base(rcLim, cslotInit)
		{
		}

		/// <summary> Copy the contents of vecSrc into this vector.
		/// </summary>
		public void CopyFrom(VectorRational vecSrc)
		{
			Clear();
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(vecSrc);
			while (iter.IsValid)
			{
				SetCoefNonZero(iter.Rc, (double)iter.Value);
				iter.Advance();
			}
		}

		/// <summary> Set a value, which the caller guarantees is not zero.
		/// </summary>
		public void SetCoefNonZero(int rc, double num)
		{
			SetCoefCore(rc, num);
		}

		/// <summary> Set a value, which may or may not be zero
		/// </summary>
		public override void SetCoef(int rc, double num)
		{
			if (0.0 == num)
			{
				RemoveCoef(rc);
			}
			else
			{
				SetCoefCore(rc, num);
			}
		}

		/// <summary> Add a number to the element, and shadow it
		///           by adding the Abs value to the shadow slot.
		/// </summary>
		public void AddAndShadow(int rc, double num, ref double[] shadows)
		{
			if (0.0 == num)
			{
				return;
			}
			int num2 = _mprcslot[rc];
			if (num2 == 0)
			{
				if (_slotLim > base.Capacity)
				{
					GrowSlotHeap(_slotLim + (_slotLim >> 1), ref shadows);
				}
				num2 = _slotLim++;
				_rgrc[num2] = rc;
				_mprcslot[rc] = num2;
				_rgnum[num2] = num;
				shadows[num2] = Math.Abs(num);
			}
			else
			{
				_rgnum[num2] += num;
				shadows[num2] += Math.Abs(num);
			}
		}

		/// <summary> Multiply every element of the vector by a.
		/// </summary>
		public override void ScaleBy(double a)
		{
			if (0.0 == a)
			{
				Clear();
			}
			else
			{
				if (1.0 == a)
				{
					return;
				}
				Iter iter = new Iter(this);
				while (iter.IsValid)
				{
					_rgnum[iter.Slot] *= a;
					if (0.0 == _rgnum[iter.Slot])
					{
						iter.RemoveAndAdvance();
					}
					else
					{
						iter.Advance();
					}
				}
			}
		}

		/// <summary> Ensure every element exceeds the stability threshold.
		/// </summary>
		public void SlamToZero(double[] shadows, double epsilon)
		{
			Iter iter = new Iter(this);
			while (iter.IsValid)
			{
				if (Math.Abs(iter.Value) < epsilon * shadows[iter.Slot])
				{
					shadows[iter.Slot] = 0.0;
					iter.RemoveAndAdvance();
				}
				else
				{
					shadows[iter.Slot] = 0.0;
					iter.Advance();
				}
			}
		}

		/// <summary> Dot product, this[:i]*v[:i]
		/// </summary>
		/// <param name="v"> The other vector </param>
		/// <param name="epsilon"> numerical stability threshold </param>
		/// <returns></returns>
		public double Dot(VectorDouble v, double epsilon)
		{
			BigSum bigSum = 0.0;
			VectorDouble vec;
			VectorDouble vectorDouble;
			if (base.RcCount <= v.RcCount)
			{
				vec = this;
				vectorDouble = v;
			}
			else
			{
				vec = v;
				vectorDouble = this;
			}
			Iter iter = new Iter(vec);
			while (iter.IsValid)
			{
				if (vectorDouble.TryGetCoef(iter.Rc, out var value))
				{
					bigSum.Add(value * iter.Value);
				}
				iter.Advance();
			}
			return bigSum.ToDouble();
		}
	}
}
