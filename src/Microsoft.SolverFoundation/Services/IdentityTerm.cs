using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A term that does nothing but wrap another term. It is useful to keep nested comparisons operations from being flattened.
	/// </summary>
	internal sealed class IdentityTerm : Term
	{
		/// <summary>
		/// The wrapped term.
		/// </summary>
		internal readonly Term _input;

		internal override bool IsModelIndependentTerm => _owningModel == null;

		internal override TermType TermType => TermType.Identity;

		internal override Domain EnumeratedDomain => _input.EnumeratedDomain;

		internal override TermValueClass ValueClass => _input.ValueClass;

		/// <summary>
		/// Construct an identity term
		/// </summary>
		/// <param name="input">The term to wrap</param>
		internal IdentityTerm(Term input)
		{
			_input = input;
			_owningModel = input._owningModel;
			_structure = input._structure;
		}

		internal override Term Clone(string baseName)
		{
			throw new NotSupportedException(Resources.CannotCloneTerm);
		}

		internal override bool TryEvaluateConstantValue(out Rational value, EvaluationContext context)
		{
			return _input.TryEvaluateConstantValue(out value, context);
		}

		internal override bool TryEvaluateConstantValue(out object value, EvaluationContext context)
		{
			return _input.TryEvaluateConstantValue(out value, context);
		}

		internal override IEnumerable<Term> AllValues(EvaluationContext context)
		{
			return _input.AllValues(context);
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}
	}
}
