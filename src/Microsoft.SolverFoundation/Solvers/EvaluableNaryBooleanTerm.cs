using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Root class for Boolean terms that have an array of Boolean inputs
	/// </summary>
	internal abstract class EvaluableNaryBooleanTerm : EvaluableBooleanTerm
	{
		public readonly EvaluableBooleanTerm[] Inputs;

		internal EvaluableNaryBooleanTerm(EvaluableBooleanTerm[] inputs)
			: base(1 + EvaluableTerm.MaxDepth(inputs))
		{
			Inputs = inputs;
		}

		internal sealed override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return Array.AsReadOnly((EvaluableTerm[])Inputs);
		}

		internal override IEnumerable<EvaluableTerm> EnumerateMoveCandidates()
		{
			try
			{
				EvaluableBooleanTerm[] inputs = Inputs;
				foreach (EvaluableBooleanTerm x in inputs)
				{
					if (x.Value == base.Value)
					{
						yield return x;
					}
				}
			}
			finally
			{
			}
		}

		internal EvaluableNaryBooleanTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map, Func<EvaluableBooleanTerm[], EvaluableNaryBooleanTerm> constructor)
		{
			bool flag = false;
			EvaluableBooleanTerm[] array = new EvaluableBooleanTerm[Inputs.Length];
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
				array[i] = (EvaluableBooleanTerm)value;
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
