using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	internal class NumericSetDomain : Domain
	{
		/// <summary>
		/// The type which a value that is an element of this domain can take.
		/// </summary>
		internal override TermValueClass ValueClass => TermValueClass.Numeric;

		internal NumericSetDomain(Rational minValue, Rational maxValue, bool intRestricted, Rational[] validValues)
			: base(minValue, maxValue, intRestricted, null, validValues, isBoolean: false)
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
			int[] array = new int[base.ValidValues.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (int)base.ValidValues[i];
			}
			return solver.CreateIntegerSet(array);
		}

		internal override Expression MakeOmlDomain(OmlWriter writer, SolveRewriteSystem rewrite)
		{
			List<Expression> list = new List<Expression>();
			Rational[] validValues = base.ValidValues;
			foreach (Rational rat in validValues)
			{
				list.Add(RationalConstant.Create(rewrite, rat));
			}
			if (base.IntRestricted)
			{
				return rewrite.Builtin.Integers.Invoke(rewrite.Builtin.List.Invoke(list.ToArray()));
			}
			return rewrite.Builtin.Reals.Invoke(rewrite.Builtin.List.Invoke(list.ToArray()));
		}

		internal override string FormatValue(IFormatProvider format, double value)
		{
			return value.ToString(format);
		}
	}
}
