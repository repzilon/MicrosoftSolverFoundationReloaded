namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint on 1 integer variable imposing that its value be
	///   chosen among a given ValueSet
	/// </summary>
	internal class Member : UnaryConstraint<IntegerVariable>
	{
		private long[] _orderedValues;

		private Backtrackable<int> _firstIdx;

		private Backtrackable<int> _lastIdx;

		public Member(Problem p, IntegerVariable x, SparseDomain set)
			: base(p, x)
		{
			_orderedValues = set.GetOrderedUniqueValueSet();
			int init = _orderedValues.Length - 1;
			_firstIdx = new Backtrackable<int>(p.IntTrail, 0);
			_lastIdx = new Backtrackable<int>(p.IntTrail, init);
			x.SubscribeToAnyModification(WhenXmodified);
		}

		private bool WhenXmodified()
		{
			if (NarrowLowerBound())
			{
				return NarrowUpperBound();
			}
			return false;
		}

		private bool NarrowLowerBound()
		{
			long lowerBound = _x.LowerBound;
			long num = _orderedValues[_firstIdx.Value];
			if (lowerBound < num)
			{
				return _x.ImposeLowerBound(num, base.Cause);
			}
			if (lowerBound == num)
			{
				return true;
			}
			int num2 = _orderedValues.Length;
			for (int i = _firstIdx.Value + 1; i < num2; i++)
			{
				long num3 = _orderedValues[i];
				if (num3 >= lowerBound)
				{
					_firstIdx.Value = i;
					return _x.ImposeLowerBound(num3, base.Cause);
				}
			}
			return _x.ImposeEmptyDomain(base.Cause);
		}

		private bool NarrowUpperBound()
		{
			long upperBound = _x.UpperBound;
			long num = _orderedValues[_lastIdx.Value];
			if (upperBound > num)
			{
				return _x.ImposeUpperBound(num, base.Cause);
			}
			if (upperBound == num)
			{
				return true;
			}
			for (int num2 = _lastIdx.Value - 1; num2 >= 0; num2--)
			{
				long num3 = _orderedValues[num2];
				if (num3 <= upperBound)
				{
					_lastIdx.Value = num2;
					return _x.ImposeUpperBound(num3, base.Cause);
				}
			}
			return _x.ImposeEmptyDomain(base.Cause);
		}
	}
}
