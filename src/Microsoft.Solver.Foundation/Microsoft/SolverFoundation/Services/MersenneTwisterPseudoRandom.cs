namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Pseudo-random number generator based on the Mersenne Twister algorithm.
	///
	/// Makoto Matsumoto and Takuji Nishimura
	/// "Mersenne Twister: A 623-Dimensionally Equidistributed Uniform Pseudo-Random Number Generator"
	/// ACM Transactions on Modeling and Computer Simulation, Vol. 8, No. 1, January 1998, Pages 3–30.
	///
	/// The period is large 2^19937 - 1 and the generator passes both the DIEHARD and NIST tests.
	///
	/// </summary>
	/// <remarks>
	/// The original algorithm used 64 bit arithmetic heavily. Since CLR 64 bit integer arithmetic is slow on
	/// x86 architectures, I have converted it to use 32 bit integer arithmetic where possible.
	/// </remarks>
	internal sealed class MersenneTwisterPseudoRandom : PseudoRandom
	{
		private const uint _m = 397u;

		private const uint _matrixA = 2567483615u;

		private const uint _seedCount = 624u;

		private readonly uint[] _mag01 = new uint[2] { 0u, 2567483615u };

		private uint _currentSeed = 624u;

		private uint[] _seeds = new uint[624];

		/// <summary>
		/// Create a new sequence of random numbers based on a seed.
		/// </summary>
		/// <param name="seed">The seed to base the generator on.</param>
		public MersenneTwisterPseudoRandom(uint seed)
		{
			_seeds[0] = seed;
			for (uint num = 1u; num < 624; num++)
			{
				_seeds[num] = (uint)((1812433253L * (long)(_seeds[num - 1] ^ (_seeds[num - 1] >> 30)) + num) & 0xFFFFFFFFu);
			}
		}

		/// <summary>
		/// If the current list of seeds is exhausted build a new list.
		/// </summary>
		private void BuildSeeds()
		{
			if (_currentSeed == 624)
			{
				uint num;
				uint num2;
				for (num = 0u; num < 227; num++)
				{
					num2 = (_seeds[num] & 0x80000000u) | (_seeds[num + 1] & 0x7FFFFFFF);
					_seeds[num] = _seeds[num + 397] ^ (num2 >> 1) ^ _mag01[num2 & 1];
				}
				for (; num < 623; num++)
				{
					num2 = (_seeds[num] & 0x80000000u) | (_seeds[num + 1] & 0x7FFFFFFF);
					_seeds[num] = _seeds[num + -227] ^ (num2 >> 1) ^ _mag01[num2 & 1];
				}
				num2 = (_seeds[623] & 0x80000000u) | (_seeds[0] & 0x7FFFFFFF);
				_seeds[623] = _seeds[396] ^ (num2 >> 1) ^ _mag01[num2 & 1];
				_currentSeed = 0u;
			}
		}

		/// <summary>
		/// Return a uniform pseudo-random number in the range [0,0xffffffff] suitable for simulation work.
		/// Note: this number is not cryptographically secure.
		/// </summary>
		/// <returns></returns>
		public override uint NextUInt32()
		{
			BuildSeeds();
			ulong num = _seeds[_currentSeed++];
			num ^= num >> 11;
			num ^= (num << 7) & 0x9D2C5680u;
			num ^= (num << 15) & 0xEFC60000u;
			num ^= num >> 18;
			return (uint)(num & 0xFFFFFFFFu);
		}
	}
}
