using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term true if the values of the sequence X1, X2, X3... are non-decreasing
	/// </summary>
	internal sealed class EvaluableLessEqual : EvaluableBooleanTerm
	{
		private readonly EvaluableNumericalTerm[] Inputs;

		internal override TermModelOperation Operation => TermModelOperation.LessEqual;

		internal EvaluableLessEqual(EvaluableNumericalTerm[] inputs)
			: base(1 + EvaluableTerm.MaxDepth(inputs))
		{
			Inputs = inputs;
		}

		internal override void Recompute(out bool change)
		{
			double num = double.MinValue;
			for (int i = 1; i < Inputs.Length; i++)
			{
				num = EvaluableBinaryAnd.Apply(num, EvaluableBinaryLessEqual.Apply(Inputs[i - 1].Value, Inputs[i].Value));
			}
			change = _violation != num;
			_violation = num;
		}

		internal override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return Inputs;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
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
				return new EvaluableLessEqual(array);
			}
			return null;
		}
	}
}
