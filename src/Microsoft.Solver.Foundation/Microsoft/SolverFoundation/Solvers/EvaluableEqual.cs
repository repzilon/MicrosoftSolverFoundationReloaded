using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term of the form X equals Y where X and Y are numerical terms
	/// </summary>
	internal sealed class EvaluableEqual : EvaluableBooleanTerm
	{
		public const double DefaultTolerance = 1E-08;

		private readonly TermEvaluator _evaluator;

		private readonly EvaluableNumericalTerm Input1;

		private readonly EvaluableNumericalTerm Input2;

		internal override TermModelOperation Operation => TermModelOperation.Equal;

		/// <summary>
		/// 0 if the equality is strict;
		/// A positive (usually small) value indicates that two values
		/// that are different but within distance at most Tolerance
		/// are effectively considered equal
		/// </summary>
		internal double Tolerance => _evaluator.EqualityTolerance;

		internal EvaluableEqual(EvaluableNumericalTerm input1, EvaluableNumericalTerm input2, TermEvaluator evaluator)
			: base(1 + Math.Max(input1.Depth, input2.Depth))
		{
			Input1 = input1;
			Input2 = input2;
			_evaluator = evaluator;
		}

		internal double Apply(double x, double y)
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
			double num = Apply(Input1.Value, Input2.Value);
			change = _violation != num;
			_violation = num;
		}

		internal override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return EvaluationStatics.Enumerate((EvaluableTerm)Input1, (EvaluableTerm)Input2);
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			if (!map.TryGetValue(Input1, out var value))
			{
				value = Input1.Substitute(map);
			}
			if (!map.TryGetValue(Input2, out var value2))
			{
				value2 = Input2.Substitute(map);
			}
			if (value != null || value2 != null)
			{
				return new EvaluableEqual(((EvaluableNumericalTerm)value) ?? Input1, ((EvaluableNumericalTerm)value2) ?? Input2, _evaluator);
			}
			return null;
		}
	}
}
