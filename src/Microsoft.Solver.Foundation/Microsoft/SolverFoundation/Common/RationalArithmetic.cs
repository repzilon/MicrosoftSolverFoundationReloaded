namespace Microsoft.SolverFoundation.Common
{
	internal sealed class RationalArithmetic : Arithmetic<Rational>
	{
		public override Rational Zero => Rational.Zero;

		public override Rational One => Rational.One;

		public override Rational MinusOne => -Rational.One;

		public override bool IsZero(Rational num)
		{
			return num.IsZero;
		}

		public override bool IsOne(Rational num)
		{
			return num.IsOne;
		}

		public override void Negate(ref Rational num)
		{
			Rational.Negate(ref num);
		}

		public override void Invert(ref Rational num)
		{
			num.Invert();
		}

		public override void AddTo(ref Rational numDst, Rational numSrc)
		{
			numDst += numSrc;
		}

		public override bool AddToQ(ref Rational numDst, Rational numSrc)
		{
			numDst += numSrc;
			return numDst.IsZero;
		}

		public override void AddToMul(ref Rational numDst, Rational num1, Rational num2)
		{
			numDst = Rational.AddMul(numDst, num1, num2);
		}

		public override bool AddToMulQ(ref Rational numDst, Rational num1, Rational num2)
		{
			numDst = Rational.AddMul(numDst, num1, num2);
			return numDst.IsZero;
		}

		public override void MulTo(ref Rational numDst, Rational numSrc)
		{
			numDst *= numSrc;
		}

		public override void DivTo(ref Rational numDst, Rational numSrc)
		{
			numDst /= numSrc;
		}
	}
}
