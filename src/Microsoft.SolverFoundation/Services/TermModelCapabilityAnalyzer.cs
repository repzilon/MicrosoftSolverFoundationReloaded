using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Services
{
	internal class TermModelCapabilityAnalyzer : ITermVisitor<bool, int>
	{
		public HashSet<TermModelOperation> _operations = new HashSet<TermModelOperation>();

		public bool Visit(Decision term, int arg)
		{
			return true;
		}

		public bool Visit(RecourseDecision term, int arg)
		{
			return true;
		}

		public bool Visit(Parameter term, int arg)
		{
			return true;
		}

		public bool Visit(RandomParameter term, int arg)
		{
			return true;
		}

		public bool Visit(ConstantTerm term, int arg)
		{
			return true;
		}

		public bool Visit(NamedConstantTerm term, int arg)
		{
			return true;
		}

		public bool Visit(StringConstantTerm term, int arg)
		{
			return true;
		}

		public bool Visit(BoolConstantTerm term, int arg)
		{
			return true;
		}

		public bool Visit(EnumeratedConstantTerm term, int arg)
		{
			return true;
		}

		public bool Visit(IdentityTerm term, int arg)
		{
			return term._input.Visit(this, 0);
		}

		public bool Visit(OperatorTerm term, int arg)
		{
			_operations.Add(TermTask.GetTermModelOperation(term.Operation));
			Term[] inputs = term._inputs;
			foreach (Term term2 in inputs)
			{
				term2.Visit(this, 0);
			}
			return true;
		}

		public bool Visit(IndexTerm term, int arg)
		{
			Term[] inputs = term._inputs;
			foreach (Term term2 in inputs)
			{
				term2.Visit(this, 0);
			}
			return true;
		}

		public bool Visit(IterationTerm term, int arg)
		{
			return true;
		}

		public bool Visit(ForEachTerm term, int arg)
		{
			return term._valueExpression.Visit(this, 0);
		}

		public bool Visit(ForEachWhereTerm term, int arg)
		{
			return term._valueExpression.Visit(this, 0);
		}

		public bool Visit(RowTerm term, int arg)
		{
			_operations.Add(TermModelOperation.Plus);
			_operations.Add(TermModelOperation.Times);
			return true;
		}

		public bool Visit(ElementOfTerm term, int arg)
		{
			return true;
		}

		public bool Visit(Tuples term, int arg)
		{
			return true;
		}
	}
}
