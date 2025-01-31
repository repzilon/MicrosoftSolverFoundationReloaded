namespace Microsoft.SolverFoundation.Common
{
	internal sealed class DoubleArithmetic : Arithmetic<double>
	{
		public override double Zero => 0.0;

		public override double One => 1.0;

		public override double MinusOne => -1.0;

		public override bool IsZero(double num)
		{
			return num == 0.0;
		}

		public override bool IsOne(double num)
		{
			return num == 1.0;
		}

		public override void Negate(ref double num)
		{
			num = 0.0 - num;
		}

		public override void Invert(ref double num)
		{
			num = 1.0 / num;
		}

		public override void AddTo(ref double numDst, double numSrc)
		{
			numDst += numSrc;
		}

		public override bool AddToQ(ref double numDst, double numSrc)
		{
			numDst += numSrc;
			return numDst == 0.0;
		}

		public override void AddToMul(ref double numDst, double num1, double num2)
		{
			numDst += num1 * num2;
		}

		public override bool AddToMulQ(ref double numDst, double num1, double num2)
		{
			numDst += num1 * num2;
			return numDst == 0.0;
		}

		public override void MulTo(ref double numDst, double numSrc)
		{
			numDst *= numSrc;
		}

		public override void DivTo(ref double numDst, double numSrc)
		{
			numDst /= numSrc;
		}
	}
}
