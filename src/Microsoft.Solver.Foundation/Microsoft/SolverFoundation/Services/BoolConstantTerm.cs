namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A constant Boolean term. This is also a numeric term, with the value 1.0 if true or 0.0 if false.
	/// </summary>
	internal sealed class BoolConstantTerm : ConstantTerm
	{
		internal override TermValueClass ValueClass => TermValueClass.Numeric;

		/// <summary>
		/// Construct a constant boolean term.
		/// </summary>
		/// <param name="value">The constant value.</param>
		internal BoolConstantTerm(bool value)
			: base(value ? 1.0 : 0.0)
		{
			_structure |= TermStructure.LinearConstraint | TermStructure.DifferentiableConstraint;
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}

		public override string ToString()
		{
			if (!(_value >= 0.5))
			{
				return "False";
			}
			return "True";
		}
	}
}
