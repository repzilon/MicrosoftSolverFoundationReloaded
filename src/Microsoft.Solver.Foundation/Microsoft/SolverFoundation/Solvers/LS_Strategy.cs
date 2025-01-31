using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>A class from which some local search strategies may derive 
	///          (this is by no means the only allowed pattern)
	///          to benefit from a few utilities which help dealing with state
	/// </summary>
	internal abstract class LS_Strategy
	{
		private List<int> _valuesSkippedBySampleMethod;

		private LocalSearchSolver _solver;

		private ConstraintSystem _model;

		protected Random _prng;

		/// <summary>The solver, cast into its concrete type</summary>
		protected LocalSearchSolver Solver => _solver;

		/// <summary>The model (cached)</summary>
		protected ConstraintSystem Model => _model;

		protected LS_Strategy()
			: this(123)
		{
		}

		protected LS_Strategy(int RandomSeed)
		{
			_solver = null;
			_valuesSkippedBySampleMethod = new List<int>();
			_prng = new Random(RandomSeed);
		}

		/// <summary>Initialization of the strategy's state
		///          (does nothing by default)
		/// </summary>
		protected virtual void Initialize(ILocalSearchProcess solver)
		{
		}

		/// <summary>Call this method every time a parameter of type
		///          ILocalSearchSolver is received
		/// </summary>
		protected void CheckSolver(ILocalSearchProcess solver)
		{
			if (_solver == null)
			{
				_solver = solver as LocalSearchSolver;
				_model = _solver.Model;
				Initialize(solver);
			}
		}

		/// <summary>Samples a number of values (in external format)
		///          from the domain of a Term. The samples are non-redundant;
		///          their number is dependent on the domain size
		/// </summary>
		/// <param name="scaling">A function that determines the sample size as
		///          a function of the cardinality of the domain. The function 
		///          should be monotonic (increasing) but should never give
		///          unreasonably large values if the cardinality if large
		/// </param>
		/// <param name="t">The term whose domain is sampled</param>
		internal IEnumerable<int> Sample(CspSolverTerm t, Func<int, int> scaling)
		{
			CspSolverDomain finiteValue = t.FiniteValue;
			int count = finiteValue.Count;
			int num = scaling(count);
			if (1.1 * (double)num >= (double)count)
			{
				return EnumerateRandomly(finiteValue);
			}
			return Sample(finiteValue, num);
		}

		/// <summary>Samples a number of values (in external format)
		///          from the domain of a Term. The samples are non-redundant.
		///          Up to a certain size the sample set will cover all values.
		///          For large domains we make sure that the size of the sample
		///          set grows slowly
		/// </summary>
		internal IEnumerable<int> LargeSample(CspSolverTerm t)
		{
			return Sample(t, LocalSearch._scaleDownUsingRoot);
		}

		/// <summary>Samples a number of values (in external format)
		///          from the domain of a Term. The samples are non-redundant.
		///          The sample set contains a small (logarithmic) number of values
		/// </summary>
		internal IEnumerable<int> SmallSample(CspSolverTerm t)
		{
			return Sample(t, LocalSearch._scaleDownLogarithmically);
		}

		private IEnumerable<int> EnumerateRandomly(CspSolverDomain dom)
		{
			int size = dom.Count;
			int start = _prng.Next(size);
			for (int i = start; i < size; i++)
			{
				yield return dom[i];
			}
			for (int j = 0; j < start; j++)
			{
				yield return dom[j];
			}
		}

		private IEnumerable<int> Sample(CspSolverDomain dom, int nbSamples)
		{
			_valuesSkippedBySampleMethod.Clear();
			for (int sample = 0; sample < nbSamples; sample++)
			{
				int next = dom.Pick(_prng);
				if (!IgnoreSample(next))
				{
					_valuesSkippedBySampleMethod.Add(next);
					yield return next;
				}
			}
		}

		private bool IgnoreSample(int sample)
		{
			int count = _valuesSkippedBySampleMethod.Count;
			for (int i = 0; i < count; i++)
			{
				if (_valuesSkippedBySampleMethod[i] == sample)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary> Returns a random flip;
		///           use occasionally for default moves or perturbations
		/// </summary>
		protected LocalSearch.Move RandomFlip()
		{
			KeyValuePair<CspTerm, int> keyValuePair = _solver.RandomFlip(_prng);
			return LocalSearch.CreateVariableFlip(keyValuePair.Key, keyValuePair.Value);
		}
	}
}
