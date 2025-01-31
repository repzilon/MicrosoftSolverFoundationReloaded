namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Part of the Visitor pattern for visiting all components of a ConstraintSystem.  This does the work of the Visitor pattern.
	/// The Visit methods are overloaded for all the types where we are interested in exploring.  Different concrete implementations can have
	/// very different behavior.
	/// </summary>
	internal interface IVisitor
	{
		void Visit(Less lessTerm);

		void Visit(Greater greaterTerm);

		void Visit(LessEqual lessEqualTerm);

		void Visit(GreaterEqual greaterEqualTerm);

		void Visit(Equal equalTerm);

		void Visit(Unequal unequalTerm);

		void Visit(BooleanImplies impliesTerm);

		void Visit(BooleanEqual bEqualTerm);

		void Visit(BooleanUnequal bUnequalTerm);

		void Visit(BooleanNot notTerm);

		void Visit(BooleanAnd andTerm);

		void Visit(BooleanOr orTerm);

		void Visit(IsElementOf isElementOfTerm);

		void Visit(ExactlyMOfN exMofNTerm);

		void Visit(AtMostMOfN atMMofNTerm);

		void Visit(Product productTerm);

		void Visit(Power powTerm);

		void Visit(Abs absTerm);

		void Visit(Negate negTerm);

		void Visit(Sum sumTerm);

		void Visit(IntMap intMapTerm);

		void Visit(CspSolverDomain domain);

		void Visit(CspVariable variable);

		void VisitDefinition(ref CspVariable variable);

		void VisitConstraint(ref CspSolverTerm constraint);

		void VisitGoal(ref CspSolverTerm goal);
	}
}
