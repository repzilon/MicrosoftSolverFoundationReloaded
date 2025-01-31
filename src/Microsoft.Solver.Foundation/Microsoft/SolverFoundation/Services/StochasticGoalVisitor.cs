using System;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// This visitor is used for determine if a goal is valid, stochastic wise.
	/// A goal is legal it doesn't have term1 * term2 when one of the terms include Decision and 
	/// the other include RandomParameter
	/// </summary>
	/// <remarks>Arg type in ITermVisitor is not in use. If a new visitor interface
	/// is declared with just Result and no Arg, all types will need to implement another Visit method
	/// so it is better to use the exist one with unused parameter</remarks>
	internal class StochasticGoalVisitor : ITermVisitor<StochasticGoalComponents, byte>
	{
		public StochasticGoalComponents Visit(Decision term, byte reserved)
		{
			return StochasticGoalComponents.Decision;
		}

		public StochasticGoalComponents Visit(RecourseDecision term, byte reserved)
		{
			return StochasticGoalComponents.None;
		}

		public StochasticGoalComponents Visit(Parameter term, byte reserved)
		{
			return StochasticGoalComponents.None;
		}

		public StochasticGoalComponents Visit(RandomParameter term, byte reserved)
		{
			return StochasticGoalComponents.RandomParameter;
		}

		public StochasticGoalComponents Visit(ConstantTerm term, byte reserved)
		{
			return StochasticGoalComponents.None;
		}

		public StochasticGoalComponents Visit(NamedConstantTerm term, byte reserved)
		{
			return StochasticGoalComponents.None;
		}

		public StochasticGoalComponents Visit(StringConstantTerm term, byte reserved)
		{
			return StochasticGoalComponents.None;
		}

		public StochasticGoalComponents Visit(BoolConstantTerm term, byte reserved)
		{
			return StochasticGoalComponents.None;
		}

		public StochasticGoalComponents Visit(EnumeratedConstantTerm term, byte reserved)
		{
			return StochasticGoalComponents.None;
		}

		public StochasticGoalComponents Visit(IdentityTerm term, byte reserved)
		{
			return term._input.Visit(this, reserved);
		}

		/// <summary>This is the only visitor which actually may add RandomParameterTimesDecision
		/// to the enum
		/// </summary>
		public StochasticGoalComponents Visit(OperatorTerm term, byte reserved)
		{
			StochasticGoalComponents stochasticGoalComponents = StochasticGoalComponents.None;
			if (term.Operation != Operator.Times)
			{
				Term[] inputs = term.Inputs;
				foreach (Term term2 in inputs)
				{
					stochasticGoalComponents |= term2.Visit(this, reserved);
				}
				return stochasticGoalComponents;
			}
			int num = 0;
			Term[] inputs2 = term.Inputs;
			foreach (Term term3 in inputs2)
			{
				StochasticGoalComponents stochasticGoalComponents2 = term3.Visit(this, reserved);
				if (stochasticGoalComponents2 != 0)
				{
					num++;
				}
				stochasticGoalComponents |= stochasticGoalComponents2;
			}
			if (num > 1 && (stochasticGoalComponents & StochasticGoalComponents.Decision) == StochasticGoalComponents.Decision && (stochasticGoalComponents & StochasticGoalComponents.RandomParameter) == StochasticGoalComponents.RandomParameter)
			{
				return stochasticGoalComponents | StochasticGoalComponents.RandomParameterTimesDecision;
			}
			return stochasticGoalComponents;
		}

		public StochasticGoalComponents Visit(IndexTerm term, byte reserved)
		{
			return ((Term)term._table)?.Visit(this, reserved) ?? StochasticGoalComponents.None;
		}

		/// <summary>
		/// Should not get here, as i don't care about itarations so i don't
		/// call it from ForEachTerm and ForEachWhereTerm
		/// </summary>
		public StochasticGoalComponents Visit(IterationTerm term, byte reserved)
		{
			throw new NotImplementedException();
		}

		public StochasticGoalComponents Visit(ForEachTerm term, byte reserved)
		{
			return term._valueExpression.Visit(this, reserved);
		}

		public StochasticGoalComponents Visit(ForEachWhereTerm term, byte reserved)
		{
			return term._valueExpression.Visit(this, reserved);
		}

		public StochasticGoalComponents Visit(RowTerm term, byte reserved)
		{
			throw new NotImplementedException();
		}

		public StochasticGoalComponents Visit(ElementOfTerm term, byte reserved)
		{
			throw new NotImplementedException();
		}

		public StochasticGoalComponents Visit(Tuples term, byte reserved)
		{
			throw new NotImplementedException();
		}
	}
}
