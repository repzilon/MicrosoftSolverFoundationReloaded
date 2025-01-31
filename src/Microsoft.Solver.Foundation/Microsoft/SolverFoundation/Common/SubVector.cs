using System;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> A continuous section of a Vector.
	/// </summary>
	/// <remarks>For some operations it may be slightly more efficient to use a Vector.  The advantage 
	/// of working with SubVector is that no memory is allocated.
	/// </remarks>
	internal class SubVector : Vector
	{
		private readonly int _count;

		/// <summary> The length of the vector.
		/// </summary>
		public override int Length => _count;

		/// <summary>Create a new instance from a Vector.
		/// </summary>
		/// <param name="vector">The vector.</param>
		/// <param name="start">The first index from the Vector.</param>
		/// <param name="count">The number of elements in the SubVector.</param>
		public SubVector(Vector vector, int start, int count)
			: base(vector, start + vector.Start)
		{
			if (vector == null)
			{
				throw new ArgumentNullException("vector");
			}
			_count = count;
			if (_count < 0 || base.Start + _count > _v.Length)
			{
				throw new ArgumentOutOfRangeException("count");
			}
		}

		/// <summary> The contents of the vector.
		/// </summary>
		public override double[] ToArray()
		{
			double[] array = new double[_count];
			Array.Copy(_v, base.Start, array, 0, _count);
			return array;
		}
	}
}
