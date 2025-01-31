using System;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A constant string term.
	/// </summary>
	internal sealed class StringConstantTerm : Term
	{
		internal readonly string _value;

		internal override bool IsModelIndependentTerm => true;

		internal override TermType TermType => TermType.StringConstant;

		internal override TermValueClass ValueClass => TermValueClass.String;

		/// <summary>
		/// Construct a constant string term.
		/// </summary>
		/// <param name="value">The constant value.</param>
		internal StringConstantTerm(string value)
		{
			_value = value;
			_structure = TermStructure.Constant | TermStructure.Integer;
		}

		internal override Term Clone(string baseName)
		{
			throw new NotSupportedException(Resources.CannotCloneTerm);
		}

		internal override bool TryEvaluateConstantValue(out object value, EvaluationContext context)
		{
			value = _value;
			return true;
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}

		internal override bool TryEvaluateConstantValue(out Rational value, EvaluationContext context)
		{
			value = 0.0;
			return false;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("\"");
			stringBuilder.Append(_value);
			stringBuilder.Append("\"");
			return stringBuilder.ToString();
		}
	}
}
