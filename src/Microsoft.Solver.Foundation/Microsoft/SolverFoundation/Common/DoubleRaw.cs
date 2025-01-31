using System.Runtime.InteropServices;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Implements IEEE Double operations that System.Math fails to provide.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	internal struct DoubleRaw
	{
		[FieldOffset(0)]
		private double x;

		[FieldOffset(0)]
		private ulong bits;

		/// <summary> Return the exponent, biased so zero has exponent -0x3FF, infinity +0x400
		/// </summary>
		public int Exponent => ((int)(bits >> 52) & 0x7FF) - 1023;

		/// <summary> Return the fraction, unsigned, implied bit restored, justified left
		/// </summary>
		public ulong Fraction => (bits | 0x10000000000000L) << 11;

		public DoubleRaw(double x)
		{
			bits = 0uL;
			this.x = x;
		}
	}
}
