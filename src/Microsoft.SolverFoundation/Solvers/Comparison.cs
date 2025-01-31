using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A boolean which is equal to the sequential ordering of a otherSet of integers.
	/// </summary>
	internal abstract class Comparison : BooleanFunction
	{
		protected CspSymbolDomain _symbols;

		/// <summary> A boolean which is equal to the sequential ordering of a otherSet of integers.
		/// </summary>
		internal Comparison(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, inputs)
		{
			InitMaximalScales();
			_symbols = CspFunction.AllowConsistentSymbols(inputs, 0, inputs.Length);
		}

		/// <summary> Suggests a subterm and a suggestion of value for it
		///           that are likely to make the inputs as equal as possible
		/// </summary>
		protected KeyValuePair<CspSolverTerm, int> SelectSubtermToFlipTowardsEquality(LocalSearchSolver ls)
		{
			Random randomSource = ls.RandomSource;
			CspSolverTerm cspSolverTerm = PickInput(randomSource);
			int value = SelectValueOfOtherInput(ls, randomSource, cspSolverTerm);
			return new KeyValuePair<CspSolverTerm, int>(cspSolverTerm, value);
		}

		/// <summary> Suggests a subterm and a suggestion of value for it
		///           that are likely to make the inputs as different as possible
		/// </summary>
		protected KeyValuePair<CspSolverTerm, int> SelectSubtermToFlipTowardsDifference(LocalSearchSolver ls)
		{
			Random randomSource = ls.RandomSource;
			CspSolverTerm cspSolverTerm = PickInput(randomSource);
			int value = SelectFreeValue(ls, randomSource, cspSolverTerm);
			return new KeyValuePair<CspSolverTerm, int>(cspSolverTerm, value);
		}

		/// <summary> Picks a value for the term that is preferably not
		///           currently used by an input
		/// </summary>
		/// <remarks> Makes a bounded number of attempts as the desired value
		///           may not always exist and this is used only for heuristic
		///           purposes anyway
		/// </remarks>
		protected int SelectFreeValue(LocalSearchSolver ls, Random prng, CspSolverTerm t)
		{
			int num = 1;
			int num2;
			while (true)
			{
				num2 = t.BaseValueSet.Pick(prng);
				if (num >= 10 || !IsUsedByAsubterm(ls, num2))
				{
					break;
				}
				num++;
			}
			return num2;
		}

		/// <summary> Picks a value used by a subterm but different from the
		///           indicated one </summary>
		/// <param name="ls">The Local Search we work in</param>
		/// <param name="prng">a Pseudo random Number Generator</param>
		/// <param name="skipped">the subterm to avoid</param>
		protected int SelectValueOfOtherInput(LocalSearchSolver ls, Random prng, CspSolverTerm skipped)
		{
			foreach (CspSolverTerm item in RandomlyEnumerateSubterms(prng))
			{
				if (!object.ReferenceEquals(item, skipped))
				{
					int num = ls[item];
					if (skipped.Contains(num))
					{
						return num;
					}
				}
			}
			return skipped.BaseValueSet.Pick(prng);
		}

		/// <summary> True if a subterm has the indicated value
		/// </summary>
		protected bool IsUsedByAsubterm(LocalSearchSolver ls, int val)
		{
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm expr in inputs)
			{
				if (ls[expr] == val)
				{
					return true;
				}
			}
			return false;
		}
	}
}
