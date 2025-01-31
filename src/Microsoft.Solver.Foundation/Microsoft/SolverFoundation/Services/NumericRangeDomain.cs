using System;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	internal class NumericRangeDomain : Domain
	{
		/// <summary>
		/// The type which a value that is an element of this domain can take.
		/// </summary>
		internal override TermValueClass ValueClass => TermValueClass.Numeric;

		internal NumericRangeDomain(Rational minValue, Rational maxValue, bool intRestricted)
			: base(minValue, maxValue, intRestricted, null, null, isBoolean: false)
		{
		}

		/// <summary>
		/// Returns a string resembling the OML syntax which constructs this domain. Not guaranteed to parse as OML.
		/// </summary>
		/// <returns>A string.</returns>
		public override string ToString()
		{
			if (base.IntRestricted)
			{
				return string.Format(CultureInfo.InvariantCulture, "Integers[{0},{1}]", new object[2] { base.MinValue, base.MaxValue });
			}
			return string.Format(CultureInfo.InvariantCulture, "Reals[{0},{1}]", new object[2] { base.MinValue, base.MaxValue });
		}

		internal override CspDomain MakeCspDomain(ConstraintSystem solver)
		{
			Rational minValue = base.MinValue;
			Rational maxValue = base.MaxValue;
			return solver.CreateIntegerInterval(Domain.FiniteIntFromRational(minValue.GetCeiling()), Domain.FiniteIntFromRational(maxValue.GetFloor()));
		}

		internal override Expression MakeOmlDomain(OmlWriter writer, SolveRewriteSystem rewrite)
		{
			if (base.IntRestricted)
			{
				return rewrite.Builtin.Integers.Invoke(RationalConstant.Create(rewrite, base.MinValue), RationalConstant.Create(rewrite, base.MaxValue));
			}
			return rewrite.Builtin.Reals.Invoke(RationalConstant.Create(rewrite, base.MinValue), RationalConstant.Create(rewrite, base.MaxValue));
		}

		internal override string FormatValue(IFormatProvider format, double value)
		{
			return value.ToString(format);
		}
	}
}
