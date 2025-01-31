using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Heuristics that chooses variables based on a variable selector and 
	///   that instantiates it to a variable. When the variable is chosen 
	///   Calls are made to a value selector until the chosen variable is 
	///   assigned to a satisfactory value and only then do we ask for 
	///   another variable.
	/// </summary>
	/// <remarks>
	///   In addition to the randomization that some variable or value
	///   heuristics may have, we add a weak source of randomness in the order
	///   in which we branch left or right when considering a value. For
	///   some heuristics this will nevertheless produce entirely deterministic
	///   effects.
	/// </remarks>
	internal class VariableValueHeuristic : Heuristic
	{
		private readonly VariableSelector _variableOrder;

		private readonly ValueSelector _valueOrder;

		private Random _prng;

		/// <summary>
		///   Construction with explicit random seed
		/// </summary>
		public VariableValueHeuristic(TreeSearchAlgorithm algo, int randomSeed, VariableSelector varOrder, ValueSelector valOrder)
			: base(algo)
		{
			_variableOrder = varOrder;
			_valueOrder = valOrder;
			_prng = new Random(randomSeed);
		}

		/// <summary>
		///   Decides the next decision by delegating the variable
		///   choice and the value choice to the dedicated objects.
		/// </summary>
		/// <remarks>
		///   A difficulty is that when the value heuristic makes a choice we may 
		///   make two decisions: one saying that the variable is greater/equal 
		///   to the value; then one the other way around. A failure
		///   may be detected between the two decisions. The same variable may
		///   be branched twice or more: 
		///
		///   The reason why we do that is that this allows to rely on refutation
		///   (we just return bound modifications to the tree search), while
		///   allowing the value ordering to return arbitrary values within the
		///   domain. Search will be complete independently of whether we are 
		///   using a sparse representation for the variables.
		/// </remarks>
		public override DisolverDecision NextDecision()
		{
			if (_treeSearch.IsRootLevel())
			{
				return ChoseVariable();
			}
			DisolverDecision disolverDecision = _treeSearch.LastDecision();
			DiscreteVariable target = disolverDecision.Target;
			long value = disolverDecision.Value;
			long lowerBound = target.GetLowerBound();
			long upperBound = target.GetUpperBound();
			if (lowerBound == upperBound)
			{
				return ChoseVariable();
			}
			if (value == lowerBound || value == upperBound)
			{
				return ChoseBoundDecision(target, value);
			}
			long v = _valueOrder.DecideValue(target);
			return ChoseBoundDecision(target, v);
		}

		/// <summary>
		///   Chose a new variable a make a decision on it. 
		/// </summary>
		private DisolverDecision ChoseVariable()
		{
			DiscreteVariable discreteVariable = _variableOrder.DecideNextVariable();
			if (discreteVariable == null)
			{
				return DisolverDecision.SolutionFound();
			}
			long v = _valueOrder.DecideValue(discreteVariable);
			return ChoseBoundDecision(discreteVariable, v);
		}

		/// <summary>
		///   Given a variable and a value v that is within its bounds;
		///   return a decision to narrow one bound of x towards v.
		/// </summary>
		/// <remarks>
		///   We take care of not taking a decision that would not prune 
		///   anything, i.e. if the value is one of the bounds we narrow
		///   the other bound. Direction is otherwise random
		/// </remarks>
		private DisolverDecision ChoseBoundDecision(DiscreteVariable x, long v)
		{
			if (v == x.GetLowerBound())
			{
				return DisolverDecision.ImposeUpperBound(x, v);
			}
			if (v == x.GetUpperBound())
			{
				return DisolverDecision.ImposeLowerBound(x, v);
			}
			if (_prng.Next(2) != 0)
			{
				return DisolverDecision.ImposeLowerBound(x, v);
			}
			return DisolverDecision.ImposeUpperBound(x, v);
		}
	}
}
