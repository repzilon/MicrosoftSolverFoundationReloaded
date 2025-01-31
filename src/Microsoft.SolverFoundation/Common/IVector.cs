namespace Microsoft.SolverFoundation.Common
{
	/// <summary> A wrapper for double[] which implements algebraic operators.
	/// </summary>
	internal interface IVector
	{
		/// <summary> The length of the vector.
		/// </summary>
		int Length { get; }

		/// <summary> Element accessor for the vector.
		/// </summary>
		/// <remarks>If performance is a concern, directly access the array using V and Start.</remarks>
		double this[int i] { get; set; }

		/// <summary> Construct a new Vector as a copy of this
		/// </summary>
		Vector Copy();

		/// <summary> Copy values from another vector.
		/// </summary>
		void CopyFrom(Vector y);

		/// <summary> The contents of the vector.
		/// </summary>
		double[] ToArray();

		/// <summary> x[] = 0
		/// </summary>
		Vector ZeroFill();

		/// <summary> v[] = c
		/// </summary>
		void ConstantFill(double c);

		/// <summary> Return maximum value
		/// </summary>
		double Max();

		/// <summary> Return minimumvalue
		/// </summary>
		double Min();

		/// <summary> a = 2-norm of vector x.
		/// </summary>
		/// <returns> Euclidean norm of vector x </returns>
		double Norm2();

		/// <summary> a = Infinity-norm of vector x.
		/// </summary>
		/// <returns> Infinity-norm of vector x </returns>
		double NormInf();

		/// <summary> return sum of all values
		/// </summary>
		double Sum();

		/// <summary> return sum of all values
		/// </summary>
		BigSum BigSum();

		/// <summary> v[] += y[]
		/// </summary>
		Vector Add(Vector y);

		/// <summary> v[] += y
		/// </summary>
		Vector AddConstant(double y);

		/// <summary> Inner product as a BigSum.
		/// </summary>
		/// <returns> inner (dot) product of x and y </returns>
		BigSum BigInnerProduct(Vector y);

		/// <summary> v[] /= y[]
		/// </summary>
		Vector ElementDivide(Vector y);

		/// <summary> y[] = 1 / x[] -- pairwise
		/// </summary>
		void ElementInvert(Vector y);

		/// <summary> v[] *= y[]
		/// </summary>
		Vector ElementMultiply(Vector y);

		/// <summary> z = x[]·y[] 
		/// </summary>
		/// <returns> inner (dot) product of x and y </returns>
		double InnerProduct(Vector y);

		/// <summary> v[] /= y
		/// </summary>
		Vector Over(double y);

		/// <summary> v[] *= y
		/// </summary>
		Vector ScaleBy(double y);

		/// <summary> [ x[]; y[] ] = this  -- split vector into x and y (pre-allocated memory)
		/// </summary>
		void Split(Vector x, Vector y);

		/// <summary> v[] -= y[]
		/// </summary>
		Vector Subtract(Vector y);
	}
}
