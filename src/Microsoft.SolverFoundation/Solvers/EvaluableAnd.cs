using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term of the form And(X1, X2...), where the Xs are Boolean terms
	/// </summary>
	internal sealed class EvaluableAnd : EvaluableNaryBooleanTerm
	{
		internal override TermModelOperation Operation => TermModelOperation.And;

		internal EvaluableAnd(EvaluableBooleanTerm[] args)
			: base(args)
		{
		}

		internal override void Recompute(out bool change)
		{
			double num = Inputs[0].Violation;
			for (int num2 = Inputs.Length - 1; num2 > 0; num2--)
			{
				num = EvaluableBinaryAnd.Apply(num, Inputs[num2].Violation);
			}
			change = _violation != num;
			_violation = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableBooleanTerm[] newInputs) => new EvaluableAnd(newInputs));
		}
	}
}
