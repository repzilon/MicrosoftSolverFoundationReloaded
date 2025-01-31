using System;
using Microsoft.SolverFoundation.Rewrite;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	internal class AnyDomain : Domain
	{
		/// <summary>
		/// The type which a value that is an element of this domain can take.
		/// </summary>
		internal override TermValueClass ValueClass => TermValueClass.Any;

		internal AnyDomain()
			: base(double.NegativeInfinity, double.PositiveInfinity, intRestricted: false, null, null, isBoolean: false)
		{
		}

		/// <summary>
		/// Returns a string resembling the OML syntax which constructs this domain. Not guaranteed to parse as OML.
		/// </summary>
		/// <returns>A string.</returns>
		public override string ToString()
		{
			return "Any";
		}

		internal override bool IsValidStringValue()
		{
			return true;
		}

		internal override CspDomain MakeCspDomain(ConstraintSystem solver)
		{
			throw new NotSupportedException();
		}

		internal override Expression MakeOmlDomain(OmlWriter writer, SolveRewriteSystem rewrite)
		{
			return rewrite.Builtin.Any;
		}

		internal override string FormatValue(IFormatProvider format, double value)
		{
			return value.ToString(format);
		}
	}
}
