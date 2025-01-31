using System.Collections.Generic;
using System.Linq;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal static class Branching
	{
		/// <summary>
		/// Computes the best estimate for an integer solution from this node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="pseudoCosts"></param>
		/// <param name="relaxation"></param>
		/// <param name="branchingVariable"></param>
		/// <returns></returns>
		public static double BestDownEstimate(Node node, PseudoCosts pseudoCosts, Rational relaxation, int branchingVariable)
		{
			double num = (double)relaxation;
			foreach (KeyValuePair<int, Rational> item in IntegerVariablesWithNonIntegerValues(pseudoCosts.Thread))
			{
				if (item.Key == branchingVariable)
				{
					num += pseudoCosts.GetEstimatedGoalDownIncrease(node, item.Key, item.Value);
					continue;
				}
				double estimatedGoalDownIncrease = pseudoCosts.GetEstimatedGoalDownIncrease(node, item.Key, item.Value);
				double estimatedGoalUpIncrease = pseudoCosts.GetEstimatedGoalUpIncrease(node, item.Key, item.Value);
				num += ((estimatedGoalDownIncrease < estimatedGoalUpIncrease) ? estimatedGoalDownIncrease : estimatedGoalUpIncrease);
			}
			return num;
		}

		/// <summary>
		/// Computes the best estimate for an integer solution from this node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="pseudoCosts"></param>
		/// <param name="relaxation"></param>
		/// <param name="branchingVariable"></param>
		/// <returns></returns>
		public static double BestUpEstimate(Node node, PseudoCosts pseudoCosts, Rational relaxation, int branchingVariable)
		{
			double num = (double)relaxation;
			foreach (KeyValuePair<int, Rational> item in IntegerVariablesWithNonIntegerValues(pseudoCosts.Thread))
			{
				if (item.Key == branchingVariable)
				{
					num += pseudoCosts.GetEstimatedGoalUpIncrease(node, item.Key, item.Value);
					continue;
				}
				double estimatedGoalDownIncrease = pseudoCosts.GetEstimatedGoalDownIncrease(node, item.Key, item.Value);
				double estimatedGoalUpIncrease = pseudoCosts.GetEstimatedGoalUpIncrease(node, item.Key, item.Value);
				num += ((estimatedGoalDownIncrease < estimatedGoalUpIncrease) ? estimatedGoalDownIncrease : estimatedGoalUpIncrease);
			}
			return num;
		}

		/// <summary>
		/// Select the branching variable that has the largest pseudocost score.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="pseudoCosts"></param>
		/// <param name="preferBinaryVariables"></param>
		/// <param name="branchingVariable">A variable that can be branched on.</param>
		/// <param name="branchingValue">The value of the variable to branch on.</param>
		/// <returns>True if a variable to branch on is found; false otherwise.</returns>      
		public static bool FindLargestPeudoCost(Node node, PseudoCosts pseudoCosts, bool preferBinaryVariables, out int branchingVariable, out Rational branchingValue)
		{
			branchingVariable = -1;
			branchingValue = 0;
			bool flag = false;
			Rational rational = Rational.NegativeInfinity;
			foreach (KeyValuePair<int, Rational> item in IntegerVariablesWithNonIntegerValues(pseudoCosts.Thread))
			{
				bool flag2 = pseudoCosts.Thread.Model.IsBinary(pseudoCosts.Thread.Model.GetVar(item.Key));
				Rational rational2 = pseudoCosts.Score(node, item.Key, item.Value);
				if ((preferBinaryVariables && flag2 && !flag) || ((!preferBinaryVariables || flag2 == flag) && rational2 > rational))
				{
					branchingVariable = item.Key;
					branchingValue = item.Value;
					flag = flag2;
					rational = rational2;
				}
			}
			return branchingVariable >= 0;
		}

		/// <summary>
		/// Select the branching variable that has the largest pseudocost score.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="pseudoCosts"></param>
		/// <param name="preferBinaryVariables"></param>
		/// <param name="branchingVariable">A variable that can be branched on.</param>
		/// <param name="branchingValue">The value of the variable to branch on.</param>
		/// <returns>True if a variable to branch on is found; false otherwise.</returns>      
		public static bool FindSmallestPeudoCost(Node node, PseudoCosts pseudoCosts, bool preferBinaryVariables, out int branchingVariable, out Rational branchingValue)
		{
			branchingVariable = -1;
			branchingValue = 0;
			bool flag = false;
			Rational rational = Rational.PositiveInfinity;
			foreach (KeyValuePair<int, Rational> item in IntegerVariablesWithNonIntegerValues(pseudoCosts.Thread))
			{
				bool flag2 = pseudoCosts.Thread.Model.IsBinary(pseudoCosts.Thread.Model.GetVar(item.Key));
				Rational rational2 = pseudoCosts.Score(node, item.Key, item.Value);
				if ((preferBinaryVariables && flag2 && !flag) || ((!preferBinaryVariables || flag2 == flag) && rational2 < rational))
				{
					branchingVariable = item.Key;
					branchingValue = item.Value;
					flag = flag2;
					rational = rational2;
				}
			}
			return branchingVariable >= 0;
		}

		/// <summary>
		/// Select the branching variable that is the most fractional.
		/// </summary>
		/// <param name="thread">The thread for which the most fractional variable is sought.</param>
		/// <param name="preferBinaryVariables">Whether to prefer binary variables over other variables.</param>
		/// <param name="branchingVariable">A variable that can be branched on.</param>
		/// <param name="branchingValue">The value of the variable to branch on.</param>
		/// <returns>True if a variable to branch on is found; false otherwise.</returns>
		public static bool FindMostFractionalVariable(SimplexTask thread, bool preferBinaryVariables, out int branchingVariable, out Rational branchingValue)
		{
			branchingVariable = -1;
			branchingValue = 0;
			bool flag = false;
			Rational rational = 0;
			Rational rational2 = (Rational)1 / (Rational)2;
			foreach (KeyValuePair<int, Rational> item in IntegerVariablesWithNonIntegerValues(thread))
			{
				Rational floor = item.Value.GetFloor();
				Rational rational3 = item.Value - floor;
				if (rational3 > rational2)
				{
					rational3 = 1 - rational3;
				}
				bool flag2 = thread.Model.IsBinary(thread.Model.GetVar(item.Key));
				if ((preferBinaryVariables && flag2 && !flag) || ((!preferBinaryVariables || flag2 == flag) && rational3 > rational))
				{
					branchingVariable = item.Key;
					branchingValue = item.Value;
					rational = rational3;
					flag = flag2;
				}
			}
			return branchingVariable >= 0;
		}

		/// <summary>
		/// Select the branching variable that is the least fractional.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="preferBinaryVariables"></param>
		/// <param name="branchingVariable">A variable that can be branched on.</param>
		/// <param name="branchingValue">The value of the variable to branch on.</param>
		/// <returns>True if a variable to branch on is found; false otherwise.</returns>
		public static bool FindLeastFractionalVariable(SimplexTask thread, bool preferBinaryVariables, out int branchingVariable, out Rational branchingValue)
		{
			branchingVariable = -1;
			branchingValue = 0;
			bool flag = false;
			Rational rational = 1;
			Rational rational2 = (Rational)1 / (Rational)2;
			foreach (KeyValuePair<int, Rational> item in IntegerVariablesWithNonIntegerValues(thread))
			{
				bool flag2 = thread.Model.IsBinary(thread.Model.GetVar(item.Key));
				Rational floor = item.Value.GetFloor();
				Rational rational3 = item.Value - floor;
				if (rational3 > rational2)
				{
					rational3 = 1 - rational3;
				}
				if ((preferBinaryVariables && flag2 && !flag) || ((!preferBinaryVariables || flag2 == flag) && rational3 < rational))
				{
					branchingVariable = item.Key;
					branchingValue = item.Value;
					rational = rational3;
					flag = flag2;
				}
			}
			return branchingVariable >= 0;
		}

		/// <summary>
		/// Finds an integer variable that has a non integer value and that impacts the objective function as well as other variables.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="pseudoCosts"></param>
		/// <param name="preferBinaryVariables"></param>
		/// <param name="branchingVariable"></param>
		/// <param name="branchingValue"></param>
		/// <returns></returns>
		public static bool FindVectorLengthVariable(Node node, PseudoCosts pseudoCosts, bool preferBinaryVariables, out int branchingVariable, out Rational branchingValue)
		{
			branchingVariable = -1;
			branchingValue = 0;
			bool flag = false;
			Rational rational = Rational.NegativeInfinity;
			foreach (KeyValuePair<int, Rational> item in IntegerVariablesWithNonIntegerValues(pseudoCosts.Thread))
			{
				Rational rational2 = pseudoCosts.Score(node, item.Key, item.Value);
				Rational influenceCoefficient = GetInfluenceCoefficient(pseudoCosts.Thread, item.Key);
				Rational rational3 = rational2 * influenceCoefficient;
				bool flag2 = pseudoCosts.Thread.Model.IsBinary(pseudoCosts.Thread.Model.GetVar(item.Key));
				if ((preferBinaryVariables && flag2 && !flag) || ((!preferBinaryVariables || flag2 == flag) && rational3 > rational))
				{
					branchingVariable = item.Key;
					branchingValue = item.Value;
					rational = rational3;
					flag = flag2;
				}
			}
			return branchingVariable >= 0;
		}

		/// <summary>
		/// Computes the influence that a variable has on others. 
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="variable"></param>
		/// <returns></returns>
		private static Rational GetInfluenceCoefficient(SimplexTask thread, int variable)
		{
			Rational result = 1;
			if (thread.HasGoal())
			{
				int goalRow = thread.Model.GetGoalRow(0);
				CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(thread.Model.Matrix, thread.Model.GetVar(variable));
				while (colIter.IsValid)
				{
					if (colIter.Row != goalRow)
					{
						thread.Model.GetRowBounds(colIter.Row, out var lowerBound, out var upperBound);
						Rational varValue = thread.AlgorithmExact.GetVarValue(thread.Model.GetSlackVarForRow(colIter.Row));
						if (varValue == lowerBound || varValue == upperBound)
						{
							result += colIter.Exact.AbsoluteValue;
						}
					}
					colIter.Advance();
				}
			}
			return result;
		}

		/// <summary>
		/// Checks whether there is at least one fractional variable.
		/// </summary>
		/// <param name="thread"></param>
		/// <returns></returns>
		public static bool HasFractionalVariable(SimplexTask thread)
		{
			if (IntegerVariablesWithNonIntegerValues(thread).Any())
			{
				return true;
			}
			return false;
		}

		private static IEnumerable<KeyValuePair<int, Rational>> IntegerVariablesWithNonIntegerValues(SimplexTask thread)
		{
			int row = thread.Model.RowLim;
			while (true)
			{
				int num;
				row = (num = row - 1);
				if (num < 0)
				{
					break;
				}
				int variable = thread.Basis.GetBasicVar(row);
				if (thread.Model.IsVarInteger(variable))
				{
					Rational value = thread.Model.MapValueFromVarToVid(variable, thread.AlgorithmExact.GetBasicValue(row));
					if (!value.IsInteger())
					{
						yield return new KeyValuePair<int, Rational>(thread.Model.GetVid(variable), value);
					}
				}
			}
		}
	}
}
