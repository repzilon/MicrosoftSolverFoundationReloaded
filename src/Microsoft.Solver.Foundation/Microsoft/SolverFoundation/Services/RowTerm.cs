using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// This represents a sum of products by as a row of a sparse matrix.
	/// </summary>
	internal class RowTerm : Term
	{
		/// <summary>
		/// A linear model whose coefficient matrix is used to find the coefficients of the sum
		/// </summary>
		internal ILinearModel _model;

		/// <summary>
		/// A mapping from variable vids to Terms representing the variables
		/// </summary>
		internal Term[] _variables;

		/// <summary>
		/// The vid of the row this term corresponds to
		/// </summary>
		internal int _vid;

		internal override bool IsModelIndependentTerm => _owningModel == null;

		internal override TermType TermType => TermType.Row;

		internal override TermValueClass ValueClass => TermValueClass.Numeric;

		internal RowTerm(ILinearModel model, Term[] variables, int vid, TermStructure structure)
		{
			_model = model;
			_variables = variables;
			for (int i = 0; i < variables.Length; i++)
			{
				if ((object)variables[i] != null && !variables[i].IsModelIndependentTerm)
				{
					_owningModel = variables[i]._owningModel;
					break;
				}
			}
			_vid = vid;
			_structure |= structure;
		}

		internal override Term Clone(string baseName)
		{
			throw new NotSupportedException(Resources.CannotCloneTerm);
		}

		/// <summary>
		/// This is a stub method which always returns false. The variables will generally be decisions, so a RowTerm almost always can't be evaluated.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		internal override bool TryEvaluateConstantValue(out Rational value, EvaluationContext context)
		{
			value = 0.0;
			return false;
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}
	}
}
