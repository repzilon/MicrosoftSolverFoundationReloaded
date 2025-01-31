using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A constant numeric term.
	/// </summary>
	internal class ConstantTerm : Term
	{
		/// <summary>
		/// The numeric value of this term, as a Rational.
		/// </summary>
		internal readonly Rational _value;

		internal override bool IsModelIndependentTerm => true;

		internal override TermType TermType => TermType.Constant;

		internal override TermValueClass ValueClass => TermValueClass.Numeric;

		/// <summary>
		/// Construct a constant numeric term.
		/// </summary>
		/// <param name="value">The constant value.</param>
		internal ConstantTerm(Rational value)
		{
			_value = value;
			_structure = TermStructure.Constant | TermStructure.Linear | TermStructure.Quadratic | TermStructure.Differentiable;
			if (_value.IsInteger())
			{
				_structure |= TermStructure.Integer;
			}
		}

		internal override Term Clone(string baseName)
		{
			throw new NotSupportedException(Resources.CannotCloneTerm);
		}

		internal override bool TryEvaluateConstantValue(out Rational value, EvaluationContext context)
		{
			value = _value;
			return true;
		}

		public override string ToString()
		{
			return _value.ToString();
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}
	}
}
