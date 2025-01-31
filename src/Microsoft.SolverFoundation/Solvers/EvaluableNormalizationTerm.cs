using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term that takes as inputs a Boolean term and whose value is the numerical
	/// normalized violation in [0, 1]. 
	/// This is done by maintaining the maximum violation
	/// </summary>
	internal sealed class EvaluableNormalizationTerm : EvaluableNumericalTerm
	{
		/// <summary>
		/// Normalization scales quadratically up to this limit, then 
		/// logarithmically. The function is anyway 'strictly increasing'
		/// (up to limitations due to floating point rounding).
		/// </summary>
		internal const double QuadraticIncreaseLimit = 1.0;

		private readonly EvaluableBooleanTerm Input;

		internal EvaluableNormalizationTerm(EvaluableBooleanTerm source)
			: base(1 + source.Depth)
		{
			Input = source;
		}

		/// <summary>
		/// A slowly-increasing normalization function
		/// </summary>
		/// <remarks>
		/// The intention is to prevent some terms from having an enormous
		/// violation (say x should be equal to y, with large domains
		/// and current values 1e-6 and 1e+6: the violation is 2e+6)
		/// that can overwhelm the cumulated violation function 
		/// (if we have another equality constraint between two variables
		/// in [0, 1] the violation of this constraint will be negligible). 
		///
		/// Note that internal violations use a symmetric negative
		/// scoring (truth degrees can vary, this allows the scoring to
		/// be well-behaved w.r.t. negation for instance). In this term
		/// we convert this back to a externally visible violation that is 
		/// intended to be summed. This violation should be non-negative, 
		/// with value 0 IFF the Boolean term represented is satisfied.
		/// </remarks>
		/// <param name="violation">
		/// A non-zero violation: 
		/// strictly negative if the Boolean term it represents is satisfied
		/// strictly positive if it is violated,
		/// NaN if the truth of the term could not even be correctly determined
		/// (which is treated as the highest possible violation)
		/// </param>
		/// <returns>
		/// A non-negative violation indicator, 
		/// treated as a regular, non-NaN, numerical value
		/// (NOT an internal violation):
		/// Strictly zero if the Boolean term it represents is satisfied,
		/// Strictly positive if the Boolean term is violated. 
		/// </returns>
		internal static double Normalize(double violation)
		{
			if (violation < 0.0)
			{
				return 0.0;
			}
			if (violation <= 1.0)
			{
				return violation * violation;
			}
			double num = 1.0 + Math.Log(violation - 1.0 + 1.0);
			if (double.IsNaN(num))
			{
				return double.PositiveInfinity;
			}
			return num;
		}

		internal override void Recompute(out bool change)
		{
			double num = Normalize(Input.Violation);
			change = _value != num;
			_value = num;
		}

		internal override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return EvaluationStatics.Enumerate((EvaluableTerm)Input);
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			if (!map.TryGetValue(Input, out var value))
			{
				value = Input.Substitute(map);
			}
			if (value != null)
			{
				return new EvaluableNormalizationTerm((EvaluableBooleanTerm)value);
			}
			return null;
		}
	}
}
