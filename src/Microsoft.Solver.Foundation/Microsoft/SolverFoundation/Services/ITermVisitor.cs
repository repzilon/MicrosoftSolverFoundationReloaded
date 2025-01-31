namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// The Visitor pattern implemented for Term.
	/// </summary>
	/// <typeparam name="Result"></typeparam>
	/// <typeparam name="Arg"></typeparam>
	internal interface ITermVisitor<Result, Arg>
	{
		Result Visit(Decision term, Arg arg);

		Result Visit(RecourseDecision term, Arg arg);

		Result Visit(Parameter term, Arg arg);

		Result Visit(RandomParameter term, Arg arg);

		Result Visit(ConstantTerm term, Arg arg);

		Result Visit(NamedConstantTerm term, Arg arg);

		Result Visit(StringConstantTerm term, Arg arg);

		Result Visit(BoolConstantTerm term, Arg arg);

		Result Visit(EnumeratedConstantTerm term, Arg arg);

		Result Visit(IdentityTerm term, Arg arg);

		Result Visit(OperatorTerm term, Arg arg);

		Result Visit(IndexTerm term, Arg arg);

		Result Visit(IterationTerm term, Arg arg);

		Result Visit(ForEachTerm term, Arg arg);

		Result Visit(ForEachWhereTerm term, Arg arg);

		Result Visit(RowTerm term, Arg arg);

		Result Visit(ElementOfTerm term, Arg arg);

		Result Visit(Tuples term, Arg arg);
	}
}
