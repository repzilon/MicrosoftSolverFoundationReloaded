using System;
using System.Security.Cryptography;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Pseudo-random number generator based on the Rijndael algorithm.
	/// Used for verifing the MersenneTwisterPseudoRandom sequence.
	///
	/// Essentially the idea is to take the output of the MersenneTwisterPseudoRandom and
	/// encrypt it with the Rijndael algorithm in code book mode. This gives a nonlinear
	/// list of pseudo-random numbers with a very high period and reasonably good efficiency.
	/// The algorithm works because Rijndael has good bit dispersion and the input sequence
	/// has a good period and bit distribution. The paper below recommends the sequence 0, 1, 2, ...
	/// but this has a low period and poor bit distribution.
	///
	/// Algorithm based on
	/// Peter Hellekalek and Stefan Wegenkittl, Empirical Evidence Concerning AES
	/// ACM Transactions on Modeling and Computer Simulation, Vol. 13, No. 4, October 2003, Pages 322–333
	///
	/// Because the Rijndael algorithms block size does not fit evenly into the Mersenne twisters period,
	/// the period is large &gt;&gt;2^19937 - 1. The generator passes both the DIEHARD and NIST tests.
	///
	/// </summary>
	internal sealed class RijndaelPseudoRandom : PseudoRandom, IDisposable
	{
		private const uint _bufferCount = 320u;

		private readonly MersenneTwisterPseudoRandom _baseGenerator;

		private readonly ICryptoTransform _cryptoTransform;

		private readonly byte[] _inputBuffer = new byte[320];

		private readonly byte[] _iv = new byte[32]
		{
			18, 66, 72, 12, 194, 124, 151, 186, 243, 154,
			71, 203, 173, 11, 19, 41, 111, 6, 230, 108,
			234, 122, 17, 126, 137, 208, 33, 194, 55, 151,
			195, 183
		};

		private readonly byte[] _key = new byte[32]
		{
			68, 9, 17, 1, 15, 191, 175, 235, 107, 219,
			193, 133, 97, 10, 161, 212, 87, 211, 138, 57,
			223, 199, 61, 62, 173, 29, 48, 69, 227, 10,
			21, 231
		};

		private readonly byte[] _outputBuffer = new byte[320];

		private readonly RijndaelManaged _rijndael = new RijndaelManaged();

		private uint _currentByte = 320u;

		/// <summary>
		/// Create a new sequence of random numbers based on a seed.
		/// </summary>
		/// <param name="seed">The seed to base the generator on.</param>
		public RijndaelPseudoRandom(uint seed)
		{
			_baseGenerator = new MersenneTwisterPseudoRandom(seed);
			_rijndael.Mode = CipherMode.ECB;
			_rijndael.BlockSize = 256;
			_rijndael.IV = _iv;
			_rijndael.Key = _key;
			_cryptoTransform = _rijndael.CreateEncryptor();
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// If necessary fill the buffer with random bytes.
		/// </summary>
		private void FillBuffer()
		{
			if (_currentByte == 320)
			{
				_baseGenerator.NextBytes(_inputBuffer);
				_cryptoTransform.TransformBlock(_inputBuffer, 0, 320, _outputBuffer, 0);
				_currentByte = 0u;
			}
		}

		/// <summary>
		/// Return a uniform pseudo-random number in the range [0,0xffffffff] suitable for simulation work.
		/// Note: this number is not cryptographically secure.
		/// </summary>
		/// <remarks>
		/// The Rijndael algorithm is being used with fixed and easily discoverable keys thus the resulting numbers
		/// are not suitable for cryptographic uses.
		/// </remarks>
		/// <returns></returns>
		public override uint NextUInt32()
		{
			FillBuffer();
			return (uint)((_outputBuffer[_currentByte++] << 24) | (_outputBuffer[_currentByte++] << 16) | (_outputBuffer[_currentByte++] << 8) | _outputBuffer[_currentByte++]);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				_rijndael.Clear();
			}
		}
	}
}
