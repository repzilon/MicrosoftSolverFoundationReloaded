using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A constraint about M of the N Boolean Term inputs.
	/// </summary>
	internal abstract class MOfNConstraint : LogicFunction
	{
		internal int _M;

		internal int _unresolved;

		internal int _trueCount;

		internal int M => _M;

		/// <summary> Exactly M of the N Boolean Term inputs must be true.
		/// </summary>
		internal MOfNConstraint(ConstraintSystem solver, int M, params CspSolverTerm[] inputs)
			: base(solver, inputs)
		{
			_M = M;
		}

		internal override void Reset()
		{
			_unresolved = Width;
			_trueCount = 0;
		}

		internal bool PrePropagate(out CspSolverTerm conflict)
		{
			if (base.Count == 0)
			{
				conflict = this;
				return false;
			}
			conflict = null;
			if (_unresolved == 0)
			{
				return false;
			}
			int num = 0;
			while (num < _unresolved)
			{
				CspSolverTerm cspSolverTerm = _inputs[num];
				if (cspSolverTerm.Count == 0)
				{
					conflict = cspSolverTerm;
					return false;
				}
				if (1 == cspSolverTerm.Count)
				{
					_unresolved--;
					if (cspSolverTerm.IsTrue)
					{
						_trueCount++;
					}
					if (num < _unresolved)
					{
						_inputs[num] = _inputs[_unresolved];
						_inputs[_unresolved] = cspSolverTerm;
					}
				}
				else
				{
					num++;
				}
			}
			return true;
		}

		/// <summary> Finds the T violated terms that are least violated 
		///           and sums their violations.
		///           The result is guaranteed to be a positive violation:
		///           if no term is violated then we return +1, otherwise
		///           if the number N of violated terms is less than T we
		///           sum these N terms.
		/// </summary>
		/// <param name="ls">Local search algorithm in which we are working
		/// </param>
		/// <param name="T">Number of violated terms to consider</param>
		internal int SumTleastViolatedFalseTerms(LocalSearchSolver ls, int T)
		{
			int[] orderedSubTermViolations = GetOrderedSubTermViolations(ls);
			int num = orderedSubTermViolations.Length;
			int num2 = 0;
			while (BooleanFunction.IsSatisfied(orderedSubTermViolations[num2]))
			{
				num2++;
				if (num2 >= num)
				{
					return 1;
				}
			}
			int num3 = num2;
			int num4 = Math.Min(num2 + T - 1, num - 1);
			int num5 = 0;
			for (int i = num3; i <= num4; i++)
			{
				num5 += orderedSubTermViolations[i];
			}
			num5 = Math.Max(1, num5);
			return BooleanFunction.ScaleDown(num5);
		}

		/// <summary> Finds the T satisfied terms that are least satisfied 
		///           and sums their violations. 
		///           The result is guaranteed to be a negative violation:
		///           if no term is satisfied then we return -1, otherwise
		///           if the number N of satisfied terms is less than T we
		///           sum these N terms.
		/// </summary>
		/// <param name="ls">Local search algorithm in which we are working
		/// </param>
		/// <param name="T">Number of satisfied terms to consider</param>
		internal int SumTleastSatisfiedTrueTerms(LocalSearchSolver ls, int T)
		{
			int[] orderedSubTermViolations = GetOrderedSubTermViolations(ls);
			int num = orderedSubTermViolations.Length;
			int num2 = num - 1;
			while (!BooleanFunction.IsSatisfied(orderedSubTermViolations[num2]))
			{
				num2--;
				if (num2 < 0)
				{
					return -1;
				}
			}
			int num3 = num2;
			int num4 = Math.Max(0, num2 - T + 1);
			int num5 = 0;
			for (int i = num4; i <= num3; i++)
			{
				num5 += orderedSubTermViolations[i];
			}
			num5 = Math.Min(-1, num5);
			return BooleanFunction.ScaleDown(num5);
		}

		/// <summary> Puts the violations of all subterms in an array and
		///           sorts them in increasing order
		/// </summary>
		/// <param name="ls">Local search algorithm in which we are working
		/// </param>
		internal int[] GetOrderedSubTermViolations(LocalSearchSolver ls)
		{
			int num = _inputs.Length;
			int[] array = new int[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = ls[_inputs[i]];
			}
			Array.Sort(array);
			return array;
		}

		/// <summary>
		/// Computes the number of inputs that are true
		/// </summary>
		/// <remarks>
		/// Sum of 0/1 values will never overflow so no check to do
		/// </remarks>
		protected int PseudoBooleanSum(LocalSearchSolver ls)
		{
			int num = 0;
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm expr in inputs)
			{
				num += ls.GetIntegerValue(expr);
			}
			return num;
		}

		/// <summary>
		/// Computes the sum of the 0/1 values of all the (Boolean)
		/// inputs together with their gradients
		/// </summary>
		protected ValueWithGradients PseudoBooleanSumWithGradients(LocalSearchSolver ls)
		{
			ValueWithGradients valueWithGradients = 0;
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm term in inputs)
			{
				ValueWithGradients integerGradients = ls.GetIntegerGradients(term);
				valueWithGradients = Gradients.Sum(valueWithGradients, integerGradients);
			}
			return valueWithGradients;
		}
	}
}
