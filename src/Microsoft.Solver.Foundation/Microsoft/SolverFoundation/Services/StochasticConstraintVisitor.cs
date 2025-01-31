using System;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// This visitor is used for determine if a constraint term belongs to the 
	/// second stage
	/// </summary>
	/// <remarks>Arg type in ITermVisitor is not in use. If a new visitor interface
	/// is declared with just Result and no Arg, all types will need to implement another Visit method
	/// so it is better to use the exist one with unused parameter</remarks>
	internal class StochasticConstraintVisitor : ITermVisitor<bool, byte>
	{
		public bool Visit(Decision term, byte reserved)
		{
			return false;
		}

		public bool Visit(RecourseDecision term, byte reserved)
		{
			return true;
		}

		public bool Visit(Parameter term, byte reserved)
		{
			return false;
		}

		public bool Visit(RandomParameter term, byte reserved)
		{
			return true;
		}

		public bool Visit(ConstantTerm term, byte reserved)
		{
			return false;
		}

		public bool Visit(NamedConstantTerm term, byte reserved)
		{
			return false;
		}

		public bool Visit(StringConstantTerm term, byte reserved)
		{
			return false;
		}

		public bool Visit(BoolConstantTerm term, byte reserved)
		{
			return false;
		}

		public bool Visit(EnumeratedConstantTerm term, byte reserved)
		{
			return false;
		}

		public bool Visit(IdentityTerm term, byte reserved)
		{
			return term._input.Visit(this, reserved);
		}

		public bool Visit(OperatorTerm term, byte reserved)
		{
			bool flag = false;
			Term[] inputs = term.Inputs;
			foreach (Term term2 in inputs)
			{
				flag |= term2.Visit(this, reserved);
			}
			return flag;
		}

		public bool Visit(IndexTerm term, byte reserved)
		{
			return ((Term)term._table)?.Visit(this, reserved) ?? false;
		}

		/// <summary>
		/// Should not get here, as i don't care about itarations so i don't
		/// call it from ForEachTerm and ForEachWhereTerm
		/// </summary>
		public bool Visit(IterationTerm term, byte reserved)
		{
			throw new NotImplementedException();
		}

		public bool Visit(ForEachTerm term, byte reserved)
		{
			return term._valueExpression.Visit(this, reserved);
		}

		public bool Visit(ForEachWhereTerm term, byte reserved)
		{
			return term._valueExpression.Visit(this, reserved);
		}

		/// <summary>
		/// REVIEW shahark: what should i do here?
		/// </summary>
		/// <param name="term"></param>
		/// <param name="reserved"></param>
		/// <returns></returns>
		public bool Visit(RowTerm term, byte reserved)
		{
			throw new NotImplementedException();
		}

		public bool Visit(ElementOfTerm tuple, byte reserved)
		{
			throw new NotImplementedException();
		}

		public bool Visit(Tuples term, byte reserved)
		{
			throw new NotImplementedException();
		}
	}
}
