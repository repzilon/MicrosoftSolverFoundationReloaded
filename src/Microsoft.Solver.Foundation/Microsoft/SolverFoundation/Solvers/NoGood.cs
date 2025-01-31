namespace Microsoft.SolverFoundation.Solvers
{
	internal class NoGood : NaryConstraint<DiscreteVariable>
	{
		private readonly Interval[] _forbiddenRanges;

		private BasicEvent _trigger;

		public NoGood(Problem p, DiscreteVariable[] vars, Interval[] forbiddenRanges)
			: base(p, vars)
		{
			_forbiddenRanges = forbiddenRanges;
			_trigger = new BasicEvent(p.Scheduler);
			_trigger.Subscribe(WhenSomethingChanged);
			BasicEvent.Listener l = _trigger.RescheduleIfNeededBool;
			foreach (DiscreteVariable discreteVariable in vars)
			{
				if (discreteVariable is IntegerVariable integerVariable)
				{
					integerVariable.SubscribeToAnyModification(l);
					continue;
				}
				BooleanVariable booleanVariable = discreteVariable as BooleanVariable;
				booleanVariable.SubscribeToFalse(l);
				booleanVariable.SubscribeToTrue(l);
			}
		}

		public bool WhenSomethingChanged()
		{
			int num = int.MaxValue;
			for (int i = 0; i < _args.Length; i++)
			{
				if (!RangeForbidden(i))
				{
					num = i;
					break;
				}
			}
			if (num == int.MaxValue)
			{
				return _args[0].ImposeEmptyDomain(base.Cause);
			}
			int num2 = int.MaxValue;
			for (int j = num + 1; j < _args.Length; j++)
			{
				if (!RangeForbidden(j))
				{
					num2 = j;
					break;
				}
			}
			if (num2 == int.MaxValue)
			{
				DiscreteVariable discreteVariable = _args[num];
				long lowerBound = discreteVariable.GetLowerBound();
				long upperBound = discreteVariable.GetUpperBound();
				Interval interval = _forbiddenRanges[num];
				if (upperBound <= interval.Upper && !discreteVariable.ImposeIntegerUpperBound(interval.Lower - 1, base.Cause))
				{
					return false;
				}
				if (lowerBound >= interval.Lower && !discreteVariable.ImposeIntegerLowerBound(interval.Upper + 1, base.Cause))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		///   True if the current range of the var of index idx
		///   is entirely forbidden, i.e. included in forbidden interval
		/// </summary>
		private bool RangeForbidden(int idx)
		{
			DiscreteVariable discreteVariable = _args[idx];
			Interval interval = _forbiddenRanges[idx];
			long lowerBound = discreteVariable.GetLowerBound();
			long upperBound = discreteVariable.GetUpperBound();
			if (interval.Lower <= lowerBound)
			{
				return upperBound <= interval.Upper;
			}
			return false;
		}
	}
}
