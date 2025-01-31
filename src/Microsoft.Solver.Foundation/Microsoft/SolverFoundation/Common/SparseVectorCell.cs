namespace Microsoft.SolverFoundation.Common
{
	/// <summary> a (row, value) pair used to build sparse vectors
	/// </summary>
	internal struct SparseVectorCell
	{
		/// <summary> the position of a value in the vector
		/// </summary>
		public int Index;

		/// <summary> a value in a vector
		/// </summary>
		public double Value;

		/// <summary> a (row, value) pair used to build sparse vectors
		/// </summary>
		public SparseVectorCell(int index, double value)
		{
			Index = index;
			Value = value;
		}

		/// <summary> A formatted string for this
		/// </summary>
		public override string ToString()
		{
			return $"[{Index}] {Value}";
		}
	}
}
