using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Heuristic that choses variables by calling a Variable selector
	///   and that splits the domain variable into two
	/// </summary>
	/// <remarks>
	///   The decision to branch left or right is taken at random
	/// </remarks>
	internal class DichotomicBranchingHeuristic : Heuristic
	{
		private readonly VariableSelector _variableOrder;

		private Random _prng;

		/// <summary>
		///   Construction with explicit random seed
		/// </summary>
		public DichotomicBranchingHeuristic(TreeSearchAlgorithm algo, int randomSeed, VariableSelector varOrder)
			: base(algo)
		{
			_variableOrder = varOrder;
			_prng = new Random(randomSeed);
		}

		/// <summary>
		///   Decides the next decision by delegating the variable
		///   choice and the value choice to the dedicated objects.
		/// </summary>
		public override DisolverDecision NextDecision()
		{
			DiscreteVariable discreteVariable = _variableOrder.DecideNextVariable();
			if (discreteVariable == null)
			{
				return DisolverDecision.SolutionFound();
			}
			long lowerBound = discreteVariable.GetLowerBound();
			long upperBound = discreteVariable.GetUpperBound();
			long num = (lowerBound + upperBound) / 2;
			if (num == upperBound)
			{
				num--;
			}
			if (_prng.Next(2) == 0)
			{
				return DisolverDecision.ImposeLowerBound(discreteVariable, num + 1);
			}
			return DisolverDecision.ImposeUpperBound(discreteVariable, num);
		}
	}
}
