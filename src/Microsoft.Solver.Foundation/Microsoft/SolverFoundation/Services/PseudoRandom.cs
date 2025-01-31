using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Abstract pseudo-random number generator. Use the various Create methods to create instances.
	///
	/// The periods are large (at least 2^19937 - 1) and the generators pass both the DIEHARD and NIST tests.
	/// </summary>
	internal abstract class PseudoRandom
	{
		private static uint _nextSeed = 1u;

		/// <summary>
		/// Return the default pseudo-random number generator--a high quality linear algorithm.
		/// The same sequence of numbers will be returned for every run of the algorithm. But different
		/// instances will typically produce different sequences.
		/// REVIEW shahark: i believe the unchecked is unnecessary, as it happens by default
		/// </summary>
		/// <returns></returns>
		public static PseudoRandom Create()
		{
			return new MersenneTwisterPseudoRandom(_nextSeed++);
		}

		/// <summary>
		/// Return the default pseudo-random number generator--a high quality linear algorithm.
		/// The same sequence of numbers will be returned for every run of the algorithm with the same seed.
		/// Different seeds will result in different sequences.
		/// </summary>
		/// <param name="seed">The seed to initialize the pseudo-random number generator with.</param>
		/// <returns></returns>
		public static PseudoRandom Create(uint seed)
		{
			return new MersenneTwisterPseudoRandom(seed);
		}

		/// <summary>
		/// Return the default pseudo-random number generator--a high quality linear algorithm.
		/// The same sequence of numbers will be returned for every run of the algorithm with the same seed.
		/// Different seeds will result in different sequences.
		/// </summary>
		/// <param name="seed">The seed to initialize the pseudo-random number generator with.</param>
		/// <returns></returns>
		public static PseudoRandom Create(int seed)
		{
			return Create((uint)seed);
		}

		/// <summary>
		/// Return the verification pseudo-random number generator--a high quality nonlinear algorithm.
		/// This generator is slower than the one returned by default and is intended for validating
		/// results to guard against unforeseen flaws in the default generator.
		/// The same sequence of numbers will be returned for every run of the algorithm. But different
		/// instances will typically produce different sequences.
		/// </summary>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		public static PseudoRandom CreateVerification()
		{
			return new RijndaelPseudoRandom(_nextSeed++);
		}

		/// <summary>
		/// Return the verification pseudo-random number generator--a high quality nonlinear algorithm.
		/// This generator is slower than the one returned by default and is intended for validating
		/// results to guard against unforseen flaws in the default generator.
		/// The same sequence of numbers will be returned for every run of the algorithm with the same seed.
		/// Different seeds will result in different sequences.
		/// </summary>
		/// <param name="seed">The seed to initialize the pseudo-random number generator with.</param>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		public static PseudoRandom CreateVerification(uint seed)
		{
			return new RijndaelPseudoRandom(seed);
		}

		/// <summary>
		/// Return the verification pseudo-random number generator--a high quality nonlinear algorithm.
		/// This generator is slower than the one returned by default and is intended for validating
		/// results to guard against unforseen flaws in the default generator.
		/// The same sequence of numbers will be returned for every run of the algorithm with the same seed.
		/// Different seeds will result in different sequences.
		/// </summary>
		/// <param name="seed">The seed to initialize the pseudo-random number generator with.</param>
		/// <returns></returns>
		public static PseudoRandom CreateVerification(int seed)
		{
			return CreateVerification((uint)seed);
		}

		/// <summary>
		/// Return a uniform pseudo-random number in the range [0, UInt32.MaxValue] suitable for simulation work.
		/// Note: this number is not cryptographically secure.
		/// </summary>
		/// <returns></returns>
		public abstract uint NextUInt32();

		/// <summary>
		/// Return a series of pseudo-random bytes suitable for simulation work.
		/// Note: this byte sequence is not cryptographically secure.
		/// </summary>
		/// <param name="bytes">The array of bytes to fill.</param>
		public void NextBytes(byte[] bytes)
		{
			uint num = 0u;
			for (uint num2 = 0u; num2 < bytes.Length; num2++)
			{
				num = ((num2 % 4 != 0) ? (num >> 8) : NextUInt32());
				bytes[num2] = (byte)(num & 0xFF);
			}
		}

		/// <summary>
		/// Return a single precision uniform pseudo-random number in the range [0,1] suitable for simulation work.
		/// Note: this number is not cryptographically secure.
		/// </summary>
		/// <returns></returns>
		public float NextSingle()
		{
			return (float)NextUInt32() / 4.2949673E+09f;
		}

		/// <summary>
		/// Return a single precision uniform pseudo-random number in the range lowerBound...upperBound suitable for simulation work.
		/// Note: this number is not cryptographically secure.
		/// </summary>
		/// <param name="interval">Interval</param>
		/// <returns></returns>
		public float NextSingle(Interval<float> interval)
		{
			if (interval.LowerBound == interval.UpperBound && interval.LowerBoundClosed && interval.UpperBoundClosed)
			{
				return interval.LowerBound;
			}
			float num2;
			do
			{
				float num = NextSingle();
				num2 = num * interval.UpperBound + (1f - num) * interval.LowerBound;
			}
			while (!interval.Contains(num2));
			return num2;
		}

		/// <summary>
		/// Return a double precision uniform pseudo-random number in the range [0,1] suitable for simulation work.
		/// Note: this number is not cryptographically secure.
		/// </summary>
		/// <returns></returns>
		public double NextDouble()
		{
			return ((double)(NextUInt32() >> 5) * 67108864.0 + (double)(NextUInt32() >> 6)) / 9007199254740992.0;
		}

		/// <summary>
		/// Return a double precision uniform pseudo-random number in the range (0,1] suitable for simulation work.
		/// Note: this number is not cryptographically secure.
		/// </summary>
		/// <returns></returns>
		public double NextDoubleGreaterThan0()
		{
			double num;
			do
			{
				num = NextDouble();
			}
			while (num == 0.0);
			return num;
		}

		/// <summary>
		/// Return a double precision uniform pseudo-random number in the range (0,1) suitable for simulation work.
		/// Note: this number is not cryptographically secure.
		/// </summary>
		/// <returns></returns>
		public double NextDoubleGreaterThan0LessThan1()
		{
			double num;
			do
			{
				num = NextDouble();
			}
			while (num == 0.0 || num == 1.0);
			return num;
		}

		/// <summary>
		/// Return a double precision uniform pseudo-random number in the range [0,1) suitable for simulation work.
		/// Note: this number is not cryptographically secure.
		/// </summary>
		/// <returns></returns>
		public double NextDoubleLessThan1()
		{
			double num;
			do
			{
				num = NextDouble();
			}
			while (num == 1.0);
			return num;
		}

		/// <summary>
		/// Return a double precision uniform pseudo-random number in the range lowerBound...upperBound suitable for simulation work.
		/// Note: this number is not cryptographically secure.
		/// </summary>
		/// <param name="interval">Interval</param>
		/// <returns></returns>
		public double NextDouble(Interval<double> interval)
		{
			if (interval == null)
			{
				throw new ArgumentNullException("interval");
			}
			if (interval.LowerBound == interval.UpperBound)
			{
				if (interval.LowerBoundClosed & interval.UpperBoundClosed)
				{
					return interval.LowerBound;
				}
				throw new ArgumentException(Resources.InvalidInterval, "interval");
			}
			double num2;
			do
			{
				double num = NextDouble();
				num2 = num * interval.UpperBound + (1.0 - num) * interval.LowerBound;
			}
			while (!interval.Contains(num2));
			return num2;
		}
	}
}
