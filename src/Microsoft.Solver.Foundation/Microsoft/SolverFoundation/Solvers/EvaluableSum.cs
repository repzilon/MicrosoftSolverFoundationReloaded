using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A sum of terms
	/// </summary>
	/// <remarks>
	/// For sums one choice could be simply to have a unique
	/// version, EvaluableSumWithRealCoefs. The array of coefficients
	/// can be null if all coefficients are 1. 
	///
	/// We specialise the code so that we can store arrays of ints
	/// as such, rather than systematically using doubles everywhere.
	/// So we have one class for simple sums with no coefs, one for
	/// double coefficients, one for integer coefficients.
	///
	/// The only reason to do that is that these are straightforward, 
	/// if modest, optimizations to do, with little code added, or
	/// just a different organization of the code. If we can divide
	/// by two the storage of coefficients by using 32-bit integers
	/// rather than doubles, why not do it?
	/// </remarks>
	internal abstract class EvaluableSum : EvaluableNumericalTerm
	{
		protected readonly EvaluableNumericalTerm[] _inputs;

		internal override TermModelOperation Operation => TermModelOperation.Plus;

		internal EvaluableSum(EvaluableNumericalTerm[] inputs)
			: base(1 + EvaluableTerm.MaxDepth(inputs))
		{
			_inputs = inputs;
		}

		internal override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return Array.AsReadOnly((EvaluableTerm[])_inputs);
		}

		internal EvaluableSum Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map, Func<EvaluableNumericalTerm[], EvaluableSum> constructor)
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
				return constructor(array);
			}
			return null;
		}
	}
}
