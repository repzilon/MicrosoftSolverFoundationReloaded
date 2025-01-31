using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal sealed class VectorRational : Vector<Rational>
	{
		public VectorRational(int rcLim)
			: base(rcLim, 0)
		{
		}

		public VectorRational(int rcLim, int cslotInit)
			: base(rcLim, cslotInit)
		{
		}

		/// <summary> Set a value, which the caller guarantees is not zero.
		/// </summary>
		public void SetCoefNonZero(int rc, Rational num)
		{
			SetCoefCore(rc, num);
		}

		/// <summary> Set a value, which may or may not be zero
		/// </summary>
		public override void SetCoef(int rc, Rational num)
		{
			if (num.IsZero)
			{
				RemoveCoef(rc);
			}
			else
			{
				SetCoefCore(rc, num);
			}
		}

		/// <summary> Multiply every element of the vector by a.
		/// </summary>
		public override void ScaleBy(Rational a)
		{
			if (0 == a)
			{
				Clear();
			}
			else if (!a.IsOne)
			{
				Iter iter = new Iter(this);
				while (iter.IsValid)
				{
					SetCoefNonZero(iter.Rc, iter.Value * a);
					iter.Advance();
				}
			}
		}

		/// <summary> Dot product, this[:i]*v[:i]
		/// </summary>
		/// <param name="v"> The other vector </param>
		/// <returns></returns>
		public Rational Dot(VectorRational v)
		{
			Rational rational = 0;
			VectorRational vec;
			VectorRational vectorRational;
			if (base.RcCount <= v.RcCount)
			{
				vec = this;
				vectorRational = v;
			}
			else
			{
				vec = v;
				vectorRational = this;
			}
			Iter iter = new Iter(vec);
			while (iter.IsValid)
			{
				if (vectorRational.TryGetCoef(iter.Rc, out var value))
				{
					rational = Rational.AddMul(rational, value, iter.Value);
				}
				iter.Advance();
			}
			return rational;
		}
	}
}
