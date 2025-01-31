namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Utilities shared by some of the global constraints
	/// </summary>
	internal static class GlobalConstraintUtilities
	{
		/// <summary>
		///   Union of two arrays of (subtypes of) Discrete Variables
		/// </summary>
		public static DiscreteVariable[] Join<A, B>(A[] list1, params B[] list2) where A : DiscreteVariable where B : DiscreteVariable
		{
			int num = 0;
			DiscreteVariable[] array = new DiscreteVariable[list1.Length + list2.Length];
			foreach (A val in list1)
			{
				array[num] = val;
				num++;
			}
			foreach (B val2 in list2)
			{
				array[num] = val2;
				num++;
			}
			return array;
		}
	}
}
