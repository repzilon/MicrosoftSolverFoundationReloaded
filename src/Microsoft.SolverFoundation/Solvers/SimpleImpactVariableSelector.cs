using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///
	/// </summary>
	internal class SimpleImpactVariableSelector : VariableSelector
	{
		private const int _intervalcutoffsize = 20;

		private BinaryHeap<DiscreteVariable> _heap;

		private ParameterlessProcedure _checkAbortion;

		private LazyExpression _functionSearchVolume;

		private LazyEvaluator _eval;

		private double _lastSearchVolume;

		private bool _updateImpact;

		private long nNumberOfDecisions;

		private LookupMap<DiscreteVariable, LazyValue> _domains;

		private LookupMap<DiscreteVariable, double> _impact;

		private LookupMap<DiscreteVariable, int> _updates;

		private LookupMap<DiscreteVariable, bool> _userDefined;

		public SimpleImpactVariableSelector(TreeSearchAlgorithm algo)
			: base(algo)
		{
			_checkAbortion = algo.Problem.Source.CheckAbortion;
			_userDefined = new LookupMap<DiscreteVariable, bool>(_problem.DiscreteVariables);
			foreach (DiscreteVariable item in _problem.DiscreteVariables.Enumerate())
			{
				_userDefined[item] = false;
			}
			foreach (DiscreteVariable userDefinedVariable in _problem.UserDefinedVariables)
			{
				_userDefined[userDefinedVariable] = true;
			}
			_problem.SubscribeToProblemRestored(WhenProblemRestored);
			_heap = new BinaryHeap<DiscreteVariable>(_problem.DiscreteVariables);
			foreach (DiscreteVariable userDefinedVariable2 in _problem.UserDefinedVariables)
			{
				_heap.Insert(userDefinedVariable2, double.MinValue);
			}
			_eval = new LazyEvaluator();
			_problem.SubscribeToVariablePropagated(WhenDomainChanges);
			_problem.SubscribeToVariableRestored(WhenDomainChanges);
			_domains = new LookupMap<DiscreteVariable, LazyValue>(_problem.DiscreteVariables);
			foreach (DiscreteVariable item2 in _problem.DiscreteVariables.Enumerate())
			{
				_domains[item2] = new LazyValue(_eval, item2.DomainSize);
			}
			_SetupIncrementalCompSearchSpace();
			_impact = new LookupMap<DiscreteVariable, double>(_problem.DiscreteVariables);
			_updates = new LookupMap<DiscreteVariable, int>(_problem.DiscreteVariables);
			foreach (DiscreteVariable item3 in _problem.DiscreteVariables.Enumerate())
			{
				_impact[item3] = 0.0;
				_updates[item3] = 0;
			}
			if (!_initializeImpacts())
			{
				_problem.AddFalsity();
			}
			_lastSearchVolume = _GetSearchVolume();
			_updateImpact = false;
			nNumberOfDecisions = 0L;
		}

		private void WhenDomainChanges(DiscreteVariable x)
		{
			LazyValue lazyValue = _domains[x];
			lazyValue.Value = x.DomainSize;
			if (_userDefined[x] && !_heap.Contains(x) && !x.CheckIfInstantiated())
			{
				_heap.Insert(x, _GetVarImpact(x));
			}
		}

		public override DiscreteVariable DecideNextVariable()
		{
			nNumberOfDecisions++;
			DiscreteVariable x = (_treeSearch.IsRootLevel() ? null : _treeSearch.LastDecision().Target);
			double num = _GetSearchVolume();
			if (num >= _lastSearchVolume)
			{
				_updateImpact = false;
			}
			if (_updateImpact)
			{
				double dImpact = num / _lastSearchVolume;
				_UpdateVarImpact(x, dImpact);
			}
			else
			{
				_updateImpact = true;
			}
			_lastSearchVolume = num;
			DiscreteVariable discreteVariable = _GetVarWithMaxImpact();
			if (discreteVariable == null)
			{
				return null;
			}
			return discreteVariable;
		}

		public void WhenProblemRestored()
		{
			_updateImpact = false;
		}

		/// <summary>
		/// Setup incremental function that computes the search volume
		/// </summary>
		private void _SetupIncrementalCompSearchSpace()
		{
			_functionSearchVolume = _eval.Atom(0.0);
			List<LazyExpression> list = new List<LazyExpression>();
			foreach (DiscreteVariable item2 in _problem.DiscreteVariables.Enumerate())
			{
				LazyExpression item = _eval.Log(_domains[item2]);
				list.Add(item);
			}
			_functionSearchVolume = _eval.Sum(list);
		}

		/// <summary>
		/// Get the current search volume
		/// </summary>
		/// <returns> double </returns>
		private double _GetSearchVolume()
		{
			_eval.Recompute();
			return Math.Max(_functionSearchVolume.Value, 0.0);
		}

		private int _GetUpdateCount(DiscreteVariable x)
		{
			return _updates[x];
		}

		private void _UpdateCount(DiscreteVariable x)
		{
			_updates[x] += 1;
		}

		private void _UpdateVarImpact(DiscreteVariable x, double dImpact)
		{
			int num = _GetUpdateCount(x);
			double num2 = _GetVarImpact(x);
			double dImpact2 = dImpact;
			if (num > 0)
			{
				double num3 = num2 * (double)num;
				dImpact2 = (num3 + dImpact) / (double)(num + 1);
			}
			_SetVarImpact(x, dImpact2);
			_UpdateCount(x);
		}

		private double _GetVarImpact(DiscreteVariable x)
		{
			return _impact[x];
		}

		private void _SetVarImpact(DiscreteVariable x, double dImpact)
		{
			_impact[x] = dImpact;
			if (_heap.Contains(x))
			{
				_heap.ChangeScore(x, 0.0 - dImpact);
			}
		}

		private DiscreteVariable _GetVarWithMaxImpact()
		{
			while (!_heap.Empty && _heap.Top().CheckIfInstantiated())
			{
				_heap.Pop();
			}
			if (_heap.Empty)
			{
				return null;
			}
			return _heap.Pop();
		}

		private double _SampleVariableValue(DiscreteVariable x, long nValue)
		{
			_problem.Save();
			double num = _GetSearchVolume();
			if (num == 0.0)
			{
				return 0.0;
			}
			bool flag = x.ImposeIntegerValue(nValue) && _problem.Simplify();
			double result = _GetSearchVolume() / num;
			_problem.Restore();
			if (!flag)
			{
				return -1.0;
			}
			return result;
		}

		private bool _initializeImpacts()
		{
			double num = 0.0;
			bool[] array = new bool[2] { false, true };
			foreach (DiscreteVariable item in _problem.DiscreteVariables.Enumerate())
			{
				_checkAbortion();
				if (item.CheckIfInstantiated())
				{
					continue;
				}
				double num2 = 0.0;
				int num3 = 0;
				if (item is BooleanVariable booleanVariable)
				{
					bool[] array2 = array;
					foreach (bool flag in array2)
					{
						num2 = ((!flag) ? _SampleVariableValue(item, 0L) : _SampleVariableValue(item, 1L));
						if (num2 == -1.0)
						{
							if (!booleanVariable.ImposeValue(!flag, Cause.Decision) || !_problem.Simplify())
							{
								return false;
							}
						}
						else
						{
							num += num2;
							num3++;
						}
					}
				}
				else
				{
					long lowerBound = item.GetLowerBound();
					long upperBound = item.GetUpperBound();
					long num4 = 1L;
					if (upperBound - lowerBound > 20)
					{
						double num5 = Math.Log((double)upperBound - (double)lowerBound);
						num4 = (long)Math.Round((double)(upperBound - lowerBound) / num5);
					}
					long num6 = lowerBound;
					while (num6 < upperBound)
					{
						for (; !item.IsAllowed(num6) && num6 < upperBound; num6 += num4)
						{
						}
						if (!item.IsAllowed(num6))
						{
							continue;
						}
						num2 = _SampleVariableValue(item, num6);
						IntegerVariable integerVariable = item as IntegerVariable;
						if (num2 == -1.0)
						{
							if (!integerVariable.ImposeBoundsDifferentFrom(num6, Cause.Decision) || !_problem.Simplify())
							{
								return false;
							}
						}
						else
						{
							num += num2;
							num3++;
						}
						num6 += num4;
					}
				}
				double dImpact = 0.0;
				if (num3 > 0)
				{
					dImpact = num / (double)num3;
				}
				_SetVarImpact(item, dImpact);
				if (_heap.Contains(item))
				{
					_heap.ChangeScore(item, _GetVarImpact(item));
				}
			}
			return true;
		}
	}
}
