using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Root class for numerical terms that have an array of numerical inputs
	/// </summary>
	internal abstract class EvaluableNaryNumericalTerm : EvaluableNumericalTerm
	{
		public readonly EvaluableNumericalTerm[] Inputs;

		internal EvaluableNaryNumericalTerm(EvaluableNumericalTerm[] inputs)
			: base(1 + EvaluableTerm.MaxDepth(inputs))
		{
			Inputs = inputs;
		}

		internal sealed override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return Array.AsReadOnly((EvaluableTerm[])Inputs);
		}

		internal EvaluableNaryNumericalTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map, Func<EvaluableNumericalTerm[], EvaluableNaryNumericalTerm> constructor)
		{
			bool flag = false;
			EvaluableNumericalTerm[] array = new EvaluableNumericalTerm[Inputs.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (!map.TryGetValue(Inputs[i], out var value))
				{
					value = Inputs[i].Substitute(map);
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
						array[j] = Inputs[j];
					}
				}
				return constructor(array);
			}
			return null;
		}
	}
}
