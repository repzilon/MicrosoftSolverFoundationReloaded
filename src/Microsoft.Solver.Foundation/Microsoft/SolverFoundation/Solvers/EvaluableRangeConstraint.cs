using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A Boolean term of them form X in [Lower, Upper]
	/// Where is is a numerical term, and Lower/Upper are constants
	/// </summary>
	/// <remarks>
	/// This is equivalent to LessEqual(Lower, X, Upper).
	/// The only reason for a specialized implementation is that we need 
	/// to be able to look-up the bounds of the constraints and it is even
	/// easier if we can modify them. This is because we can SetBounds of
	/// arbitrary numerical terms, and modify any of the Bounds arbitrarily.
	/// </remarks>
	internal sealed class EvaluableRangeConstraint : EvaluableBooleanTerm
	{
		private readonly TermEvaluator _evaluator;

		internal readonly EvaluableNumericalTerm Input;

		internal override TermModelOperation Operation => TermModelOperation.LessEqual;

		/// <summary>
		/// 0 if the equality is strict;
		/// A positive (usually small) value indicates that two values
		/// that are different but within distance at most Tolerance
		/// are effectively considered equal
		/// </summary>
		internal double Tolerance => _evaluator.EqualityTolerance;

		internal double Lower { get; set; }

		internal double Upper { get; set; }

		internal EvaluableRangeConstraint(double lower, EvaluableNumericalTerm input, double upper, TermEvaluator evaluator)
			: base(1 + input.Depth)
		{
			Input = input;
			Lower = lower;
			Upper = upper;
			_evaluator = evaluator;
		}

		internal double ApplyEqual(double x, double y)
		{
			double num = Math.Abs(x - y);
			if (num <= Tolerance)
			{
				num = -1.0;
			}
			return num;
		}

		internal override void Recompute(out bool change)
		{
			double num = ((Lower == Upper) ? ApplyEqual(Input.Value, Lower) : EvaluableBinaryAnd.Apply(EvaluableBinaryLessEqual.Apply(Lower, Input.Value), EvaluableBinaryLessEqual.Apply(Input.Value, Upper)));
			change = num != _violation;
			_violation = num;
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
				return new EvaluableRangeConstraint(Lower, (EvaluableNumericalTerm)value, Upper, _evaluator);
			}
			return null;
		}
	}
}
