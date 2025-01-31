using System.Text;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A constant enumerated term. This is basically a numeric term, where the number is associated with a string.
	/// Each constant enumerated term is associated with a specific enumerated domain, and is only compatible with
	/// other enumerated terms from the same domain.
	/// </summary>
	internal class EnumeratedConstantTerm : ConstantTerm
	{
		/// <summary>
		/// The enumerated domain this constant belongs to
		/// </summary>
		private readonly Domain _symbolDomain;

		internal override Domain EnumeratedDomain => _symbolDomain;

		internal string Value => _symbolDomain.EnumeratedNames[(int)_value];

		internal override TermValueClass ValueClass => TermValueClass.Enumerated;

		/// <summary>
		/// Construct a constant enumerated term
		/// </summary>
		/// <param name="symbolDomain">An enumerated domain. This must have a ValueClass of Enumerated.</param>
		/// <param name="index">The constant value, as the index of a string within the enumerated domain.</param>
		internal EnumeratedConstantTerm(Domain symbolDomain, int index)
			: base(index)
		{
			_symbolDomain = symbolDomain;
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("\"");
			stringBuilder.Append(Value);
			stringBuilder.Append("\"");
			return stringBuilder.ToString();
		}
	}
}
