using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A dummy term used as an iteration variable.
	/// </summary>
	internal sealed class IterationTerm : Term
	{
		/// <summary>
		/// The name of this term, for debugging purposes. Not necessarily unique.
		/// </summary>
		private readonly string _name;

		/// <summary>
		/// The type of this term (Boolean, numeric, etc.)
		/// </summary>
		private readonly TermValueClass _valueClass;

		private readonly Domain _domain;

		internal override bool IsModelIndependentTerm => _owningModel == null;

		internal override TermType TermType => TermType.Iteration;

		internal override TermValueClass ValueClass => _valueClass;

		internal override Domain EnumeratedDomain => _domain;

		/// <summary>
		/// Constructs a new dummy term for use as an iteration variable.
		/// </summary>
		/// <param name="name">A name for the term. Doesn't need to be unique.</param>
		/// <param name="valueClass">The type of the term.</param>
		/// <param name="domain">The domain of the term.</param>
		internal IterationTerm(string name, TermValueClass valueClass, Domain domain)
		{
			_name = name;
			_valueClass = valueClass;
			_domain = domain;
			_structure = TermStructure.Constant | TermStructure.Linear | TermStructure.Quadratic | TermStructure.Differentiable;
			if (domain.IntRestricted)
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
			if (ValueClass == TermValueClass.Any || ValueClass == TermValueClass.String)
			{
				value = 0;
				return false;
			}
			object value2 = context.GetValue(this);
			if (value2 is Rational)
			{
				value = (Rational)value2;
			}
			else
			{
				if (!(value2 is double))
				{
					value = 0;
					return false;
				}
				value = (double)value2;
			}
			return true;
		}

		internal override bool TryEvaluateConstantValue(out object value, EvaluationContext context)
		{
			value = context.GetValue(this);
			return true;
		}

		public override string ToString()
		{
			return _name;
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}
	}
}
