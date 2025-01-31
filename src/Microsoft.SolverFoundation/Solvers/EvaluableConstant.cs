using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Constants, with an immutable real value
	/// </summary>
	internal sealed class EvaluableConstant : EvaluableNumericalTerm
	{
		public override bool IsConstant => true;

		internal EvaluableConstant(double value)
			: base(0)
		{
			_value = value;
		}

		internal override void Recompute(out bool change)
		{
			change = false;
		}

		internal override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return Enumerable.Empty<EvaluableTerm>();
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return null;
		}
	}
}
