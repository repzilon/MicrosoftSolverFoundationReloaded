using System;
using Microsoft.SolverFoundation.Rewrite;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	internal class BooleanDomain : Domain
	{
		/// <summary>
		/// The type which a value that is an element of this domain can take.
		/// </summary>
		internal override TermValueClass ValueClass => TermValueClass.Numeric;

		internal BooleanDomain()
			: base(0, 1, intRestricted: true, null, null, isBoolean: true)
		{
		}

		/// <summary>
		/// Returns a string resembling the OML syntax which constructs this domain. Not guaranteed to parse as OML.
		/// </summary>
		/// <returns>A string.</returns>
		public override string ToString()
		{
			return "Booleans";
		}

		internal override CspDomain MakeCspDomain(ConstraintSystem solver)
		{
			return solver.DefaultBoolean;
		}

		internal override Expression MakeOmlDomain(OmlWriter writer, SolveRewriteSystem rewrite)
		{
			return rewrite.Builtin.Booleans;
		}

		internal override string FormatValue(IFormatProvider format, double value)
		{
			if (!(value >= 0.5))
			{
				return "False";
			}
			return "True";
		}
	}
}
