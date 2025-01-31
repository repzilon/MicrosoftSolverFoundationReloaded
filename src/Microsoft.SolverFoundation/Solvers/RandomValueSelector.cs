using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A value ordering that always returns a value chosen
	///   uniformly at random between the two bounds 
	/// </summary>
	internal class RandomValueSelector : ValueSelector
	{
		private Random _random;

		public RandomValueSelector(TreeSearchAlgorithm p, int seed)
			: base(p)
		{
			_random = new Random(seed);
		}

		public override long DecideValue(DiscreteVariable x)
		{
			long lowerBound = x.GetLowerBound();
			long num = x.GetUpperBound() + 1;
			return _random.Next((int)lowerBound, (int)num);
		}
	}
}
