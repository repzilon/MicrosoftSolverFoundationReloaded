namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Solves a knapsack problem.
	/// </summary>
	/// <remarks>
	/// The solver is not generic. It is targeted at cut cover separation algorithm.
	/// </remarks>
	internal class KnapsackSolver
	{
		/// <summary>
		/// Returns a array indicating which values should be included into the knapsak to minimize the total value.
		/// </summary>
		/// <param name="values">The values of the items.</param>
		/// <param name="weights">The weights of the items.</param>
		/// <param name="length">The number of items.</param>
		/// <param name="requiredWeight">The minimum weight.</param>
		/// <param name="longer">Indicates whether we prefer having more (true) or less (false) items in the solution.</param>
		/// <param name="cutoffValue">The value above which the result is rejected.</param>
		/// <param name="selection">The array indicating which values should be included.</param>
		/// <param name="value">The value of the selection.</param>
		/// <returns></returns>
		public static bool Solve(double[] values, double[] weights, int length, double requiredWeight, bool longer, double cutoffValue, out bool[] selection, out double value)
		{
			value = 0.0;
			double num = 0.0;
			for (int i = 0; i < length; i++)
			{
				num += weights[i];
			}
			return Solve(values, weights, length, requiredWeight, longer, num, cutoffValue, 0, out selection, ref value);
		}

		public static bool Solve(double[] values, double[] weights, int length, double requiredWeight, bool longer, double remainingWeight, double cutoffValue, int start, out bool[] selection, ref double value)
		{
			if (value >= cutoffValue || remainingWeight < requiredWeight)
			{
				selection = new bool[length];
				return false;
			}
			if (requiredWeight <= 0.0)
			{
				selection = new bool[length];
				return true;
			}
			if (start == length)
			{
				selection = new bool[length];
				return requiredWeight <= 0.0;
			}
			double value2 = value + (longer ? values[start] : 0.0);
			bool[] selection2;
			bool flag = Solve(values, weights, length, requiredWeight - (longer ? weights[start] : 0.0), longer, remainingWeight - weights[start], cutoffValue, start + 1, out selection2, ref value2);
			if (flag && value2 < cutoffValue)
			{
				selection = selection2;
				selection[start] = longer;
				value = value2;
				return true;
			}
			double value3 = value + (longer ? 0.0 : values[start]);
			bool[] selection3;
			bool flag2 = Solve(values, weights, length, requiredWeight - (longer ? 0.0 : weights[start]), longer, remainingWeight - weights[start], cutoffValue, start + 1, out selection3, ref value3);
			if (!flag && !flag2)
			{
				selection = null;
				return false;
			}
			if (!flag || (flag2 && value3 <= value2))
			{
				selection = selection3;
				selection[start] = !longer;
				value = value3;
			}
			else
			{
				selection = selection2;
				selection[start] = longer;
				value = value2;
			}
			if (value >= cutoffValue)
			{
				return false;
			}
			return true;
		}
	}
}
