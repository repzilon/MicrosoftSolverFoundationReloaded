namespace Microsoft.SolverFoundation.Common
{
	internal abstract class Arithmetic<Number>
	{
		private static Arithmetic<Number> _inst;

		public static Arithmetic<Number> Instance => _inst;

		public abstract Number Zero { get; }

		public abstract Number One { get; }

		public abstract Number MinusOne { get; }

		static Arithmetic()
		{
			if (typeof(Number) == typeof(Rational))
			{
				_inst = (Arithmetic<Number>)(object)new RationalArithmetic();
			}
			else if (typeof(Number) == typeof(double))
			{
				_inst = (Arithmetic<Number>)(object)new DoubleArithmetic();
			}
		}

		public abstract bool IsZero(Number num);

		public abstract bool IsOne(Number num);

		public abstract void Negate(ref Number num);

		public abstract void Invert(ref Number num);

		public abstract void AddTo(ref Number numDst, Number numSrc);

		public abstract bool AddToQ(ref Number numDst, Number numSrc);

		public abstract void AddToMul(ref Number numDst, Number num1, Number num2);

		public abstract bool AddToMulQ(ref Number numDst, Number num1, Number num2);

		public abstract void MulTo(ref Number numDst, Number numSrc);

		public abstract void DivTo(ref Number numDst, Number numSrc);
	}
}
