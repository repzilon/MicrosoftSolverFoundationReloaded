using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	internal class ElementOfTerm : Term
	{
		internal Term[] _tuple;

		internal Tuples _tupleList;

		internal override TermValueClass ValueClass => TermValueClass.Numeric;

		internal override bool IsModelIndependentTerm => _owningModel == null;

		internal override TermType TermType => TermType.ElementOf;

		internal ElementOfTerm(Term[] tuple, Tuples tupleList)
		{
			_tuple = tuple;
			_tupleList = tupleList;
			foreach (Term term in tuple)
			{
				if (term._owningModel != null)
				{
					_owningModel = term._owningModel;
					break;
				}
			}
			TermStructure termStructure = TermStructure.Constant | TermStructure.Integer;
			for (int j = 0; j < tuple.Length; j++)
			{
				termStructure &= tuple[j].Structure;
			}
			_structure |= termStructure;
			if ((termStructure & TermStructure.Constant) != 0)
			{
				_structure |= TermStructure.Linear | TermStructure.Quadratic | TermStructure.LinearConstraint | TermStructure.Differentiable | TermStructure.DifferentiableConstraint;
			}
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}

		internal override bool TryEvaluateConstantValue(out Rational value, EvaluationContext context)
		{
			value = 0;
			return false;
		}

		internal override Term Clone(string baseName)
		{
			throw new NotSupportedException();
		}
	}
}
