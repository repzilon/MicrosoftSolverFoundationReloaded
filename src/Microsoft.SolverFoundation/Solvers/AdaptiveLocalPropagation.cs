using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	internal sealed class AdaptiveLocalPropagation
	{
		private ConstraintSystem _solver;

		private int _propagationCt;

		internal AdaptiveLocalPropagation(ConstraintSystem solver)
		{
			_solver = solver;
		}

		internal void Backtrack(int changeCount)
		{
			if (changeCount <= _propagationCt)
			{
				_propagationCt = changeCount;
			}
		}

		/// <summary> Advance through the Restriction List, propagating the consequences
		///           of each Restriction, until all the List has been processed.  Note
		///           that the List grows during this because new Restrictions caused
		///           by propagation are added to the end of the List, so reaching the
		///           end of the list is synonymous with propagating all consequences.
		/// </summary>
		/// <returns> true if no conflict, false and sets the conflict Term.
		/// </returns>
		internal bool Propagate(out CspSolverTerm conflict)
		{
			conflict = null;
			int count = _solver._changes.Count;
			Stack<CspFunction> stack = new Stack<CspFunction>();
			Stack<CspFunction> stack2 = new Stack<CspFunction>();
			while ((_propagationCt < _solver._changes.Count || 0 < stack.Count) && !_solver.CheckAbort())
			{
				int count2 = _solver._changes.Count;
				while (_propagationCt < _solver._changes.Count && !_solver.CheckAbort())
				{
					KeyValuePair<CspSolverTerm, int> keyValuePair = _solver._changes[_propagationCt++];
					if (0 > keyValuePair.Value)
					{
						continue;
					}
					CspSolverTerm key = keyValuePair.Key;
					int changeCount = key.ChangeCount;
					if (key is CspFunction cspFunction && changeCount <= count2)
					{
						if (IsEqualConstraintWithConstants(cspFunction))
						{
							stack2.Push(cspFunction);
						}
						else
						{
							stack.Push(cspFunction);
							cspFunction._flags |= 1u;
						}
					}
					List<CspFunction> dependents = key.Dependents;
					foreach (CspFunction item in dependents)
					{
						if ((item._flags & 1) == 0 && item.ChangeCount <= changeCount)
						{
							if (IsEqualConstraintWithConstants(item))
							{
								stack2.Push(item);
								continue;
							}
							stack.Push(item);
							item._flags |= 1u;
						}
					}
				}
				while (stack2.Count > 0)
				{
					CspFunction cspFunction2 = stack2.Pop();
					stack.Push(cspFunction2);
					cspFunction2._flags |= 1u;
				}
				if (_solver.IsInterrupted || 0 >= stack.Count)
				{
					continue;
				}
				CspFunction cspFunction3 = stack.Pop();
				cspFunction3._flags &= 4294967294u;
				int changeCount2 = cspFunction3.ChangeCount;
				if ((2 & cspFunction3._flags) != 0)
				{
					cspFunction3._flags |= 2u;
				}
				cspFunction3.Propagate(out conflict);
				if (changeCount2 == cspFunction3.ChangeCount)
				{
					if (changeCount2 <= count)
					{
						_solver._changes.Add(new KeyValuePair<CspSolverTerm, int>(cspFunction3, -1));
						cspFunction3.ChangeCount = _solver._changes.Count;
					}
					else
					{
						cspFunction3.ChangeCount = count2;
					}
				}
				if (conflict != null)
				{
					while (0 < stack.Count)
					{
						stack.Pop()._flags &= 4294967294u;
					}
					return false;
				}
			}
			return true;
		}

		private static bool IsEqualConstraintWithConstants(CspFunction func)
		{
			if (!(func is Equal equal))
			{
				return false;
			}
			if (!equal.IsConstraint)
			{
				return false;
			}
			foreach (CspTerm input in equal.Inputs)
			{
				if (input is CspSolverTerm cspSolverTerm)
				{
					if (cspSolverTerm.TermKind == CspSolverTerm.TermKinds.Constant)
					{
						return true;
					}
					if (cspSolverTerm.FiniteValue.Count == 1)
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
