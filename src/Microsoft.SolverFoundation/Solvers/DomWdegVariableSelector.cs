using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A variable ordering heuristic in which we branch on one of the
	///   non-instantiated user-defined ariables whose domain has the minimal
	///   cardinality
	/// </summary>
	internal class DomWdegVariableSelector : VariableSelector
	{
		/// <summary>
		///   to each var we associate an expression whose value
		///   if the range of the variable
		/// </summary>
		private LookupMap<DiscreteVariable, LazyValue> _domainSize;

		/// <summary>
		///   to each var we associate an expression which is 1 iff 
		///   the range of variable is &gt; 1
		/// </summary>
		private LookupMap<DiscreteVariable, LazyExpression> _isUninstantiated;

		/// <summary>
		///   (complex!) score attached to every variable
		/// </summary>
		private LookupMap<DiscreteVariable, LazyExpression> _score;

		/// <summary>
		///   to each constraint we associate a weight, which the
		///   heuristic increments on conflict
		/// </summary>
		private LookupMap<DisolverConstraint, LazyValue> _weight;

		/// <summary>
		///   to each constraint we associate a lazy expression which
		///   is equal to the weight except when the constraint is
		///   inactive (less than 2 uninstantiated vars) in which
		///   case the expression evaluates to 0
		/// </summary>
		private LookupMap<DisolverConstraint, LazyExpression> _checkedWeight;

		private LazyEvaluator _eval;

		/// <summary>
		///   Construction
		/// </summary>
		public DomWdegVariableSelector(TreeSearchAlgorithm prob, int randomSeed)
			: base(prob)
		{
			_eval = new LazyEvaluator();
			LazyExpression lazyExpression = _eval.Constant(1.0);
			_domainSize = new LookupMap<DiscreteVariable, LazyValue>(_problem.DiscreteVariables);
			_isUninstantiated = new LookupMap<DiscreteVariable, LazyExpression>(_problem.DiscreteVariables);
			_score = new LookupMap<DiscreteVariable, LazyExpression>(_problem.DiscreteVariables);
			_weight = new LookupMap<DisolverConstraint, LazyValue>(_problem.Constraints);
			_checkedWeight = new LookupMap<DisolverConstraint, LazyExpression>(_problem.Constraints);
			foreach (DiscreteVariable item in _problem.DiscreteVariables.Enumerate())
			{
				_domainSize[item] = new LazyValue(_eval, item.DomainSize);
				_isUninstantiated[item] = _eval.Min(_domainSize[item] - 1.0, lazyExpression);
			}
			List<LazyExpression> list = new List<LazyExpression>();
			foreach (DisolverConstraint item2 in _problem.Constraints.Enumerate())
			{
				list.Clear();
				_weight[item2] = new LazyValue(_eval, 1.0);
				foreach (DiscreteVariable variable in item2.Signature.GetVariables())
				{
					list.Add(_isUninstantiated[variable]);
				}
				LazyExpression lazyExpression2 = _eval.Sum(list);
				LazyExpression lazyExpression3 = _eval.Max(_eval.Min(lazyExpression2 - 1.0, lazyExpression), _eval.Constant(0.0));
				_checkedWeight[item2] = lazyExpression3 * _weight[item2];
			}
			foreach (DiscreteVariable item3 in _problem.DiscreteVariables.Enumerate())
			{
				list.Clear();
				list.Add(lazyExpression);
				foreach (DisolverConstraint item4 in item3.EnumerateConstraints())
				{
					list.Add(_checkedWeight[item4]);
				}
				LazyExpression lazyExpression4 = _eval.Sum(list);
				_score[item3] = _domainSize[item3] / lazyExpression4;
			}
			_problem.SubscribeToVariablePropagated(WhenDomainChanges);
			_problem.SubscribeToVariableRestored(WhenDomainChanges);
			_problem.SubscribeToConflicts(WhenConflict);
			foreach (DiscreteVariable item5 in _problem.DiscreteVariables.Enumerate())
			{
				_ = item5;
			}
		}

		private void WhenDomainChanges(DiscreteVariable x)
		{
			if (_problem.IsUserDefined(x))
			{
				_domainSize[x].Value = x.DomainSize;
			}
		}

		public override DiscreteVariable DecideNextVariable()
		{
			_eval.Recompute();
			double num = double.MaxValue;
			DiscreteVariable result = null;
			foreach (DiscreteVariable userDefinedVariable in _problem.UserDefinedVariables)
			{
				if (!userDefinedVariable.CheckIfInstantiated())
				{
					double value = _score[userDefinedVariable].Value;
					if (value < num)
					{
						num = value;
						result = userDefinedVariable;
					}
				}
			}
			return result;
		}

		private void WhenConflict(Cause cstr)
		{
			if (cstr.Constraint != null)
			{
				_weight[cstr.Constraint].Value += 1.0;
			}
		}
	}
}
