using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	internal interface IIndexable
	{
		Set[] IndexSets { get; }

		TermValueClass ValueClass { get; }

		TermValueClass DomainValueClass { get; }

		string Name { get; }

		void SetIndexSet(int i, Set set);

		bool TryEvaluateConstantValue(object[] inputValues, out Rational value, Term.EvaluationContext context);
	}
}
