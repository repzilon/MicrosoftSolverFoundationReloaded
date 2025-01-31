using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term of the form AllDifferent (X1 ... Xn) where all args are numerical
	/// </summary>
	internal sealed class EvaluableAllDifferent : EvaluableBooleanTerm, IEvaluationObserver
	{
		/// <summary>
		/// The inputs of the alldiff are in fact the observers 
		/// </summary>
		private readonly EvaluableNumericalObservable[] _inputs;

		/// <summary>
		/// A dictionary containing pairs (value taken by all inputs,
		/// number of inputs that have the corresponding value)
		/// </summary>
		/// <remarks>
		/// This can be improved: in the CSP local search a data-structure
		/// called LSValueMap is used which uses array indexing if the values
		/// are continous - this is common with AllDifferents - and a Dictionary
		/// otherwise.
		/// </remarks>
		private Dictionary<double, int> _count;

		internal override TermModelOperation Operation => TermModelOperation.Unequal;

		internal EvaluableAllDifferent(EvaluableNumericalTerm[] args)
			: base(2 + EvaluableTerm.MaxDepth(args))
		{
			_inputs = new EvaluableNumericalObservable[args.Length];
			for (int i = 0; i < args.Length; i++)
			{
				_inputs[i] = new EvaluableNumericalObservable(args[i], this);
			}
		}

		internal override void Reinitialize(out bool change)
		{
			EvaluableNumericalObservable[] inputs = _inputs;
			foreach (EvaluableNumericalObservable evaluableNumericalObservable in inputs)
			{
				evaluableNumericalObservable.Reinitialize(out var _);
			}
			_count = InitializeDictionary();
			double num = ComputeViolation();
			change = _violation != num;
			_violation = num;
		}

		internal override void Recompute(out bool change)
		{
			change = true;
		}

		public void ValueChange(EvaluableTerm arg, double oldValue, double newValue)
		{
			double num = Math.Max(0.0, _violation);
			int count = GetCount(_count, oldValue);
			int count2 = GetCount(_count, newValue);
			if (count > 1)
			{
				num -= 1.0;
			}
			if (count2 > 0)
			{
				num += 1.0;
			}
			_count[newValue] = count2 + 1;
			if (count == 1)
			{
				_count.Remove(oldValue);
			}
			else
			{
				_count[oldValue] = count - 1;
			}
			_violation = ((num == 0.0) ? (-1.0) : num);
		}

		private Dictionary<double, int> InitializeDictionary()
		{
			Dictionary<double, int> dictionary = new Dictionary<double, int>();
			EvaluableNumericalObservable[] inputs = _inputs;
			foreach (EvaluableNumericalObservable evaluableNumericalObservable in inputs)
			{
				int count = GetCount(dictionary, evaluableNumericalObservable.Value);
				dictionary[evaluableNumericalObservable.Value] = count + 1;
			}
			return dictionary;
		}

		private double ComputeViolation()
		{
			int num = 0;
			foreach (KeyValuePair<double, int> item in _count)
			{
				if (item.Value > 1)
				{
					num += item.Value - 1;
				}
			}
			return (num == 0) ? (-1) : num;
		}

		private static int GetCount(Dictionary<double, int> dictionary, double val)
		{
			if (!dictionary.TryGetValue(val, out var value))
			{
				return 0;
			}
			return value;
		}

		internal override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return Array.AsReadOnly((EvaluableTerm[])_inputs);
		}

		internal override IEnumerable<EvaluableTerm> EnumerateMoveCandidates()
		{
			if (base.Value)
			{
				return EnumerateInputs();
			}
			return EnumerateConflictInputs();
		}

		private IEnumerable<EvaluableTerm> EnumerateConflictInputs()
		{
			try
			{
				EvaluableNumericalObservable[] inputs = _inputs;
				foreach (EvaluableNumericalObservable x in inputs)
				{
					if (GetCount(_count, x.Value) > 1)
					{
						yield return x.Input;
					}
				}
			}
			finally
			{
			}
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			bool flag = false;
			EvaluableNumericalTerm[] array = new EvaluableNumericalTerm[_inputs.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!map.TryGetValue(_inputs[i], out var value))
				{
					value = _inputs[i].Substitute(map);
				}
				if (value != null)
				{
					flag = true;
				}
				array[i] = (EvaluableNumericalTerm)value;
			}
			if (flag)
			{
				for (int j = 0; j < array.Length; j++)
				{
					if (array[j] == null)
					{
						array[j] = _inputs[j];
					}
				}
				return new EvaluableAllDifferent(array);
			}
			return null;
		}
	}
}
