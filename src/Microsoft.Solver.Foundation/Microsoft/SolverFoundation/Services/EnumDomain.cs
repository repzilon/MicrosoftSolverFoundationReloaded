using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	internal class EnumDomain : Domain
	{
		internal Dictionary<string, int> _ordinalMapping;

		/// <summary>
		/// The type which a value that is an element of this domain can take.
		/// </summary>
		internal override TermValueClass ValueClass => TermValueClass.Enumerated;

		internal EnumDomain(string[] enumeratedNames)
			: base(0, enumeratedNames.Length - 1, intRestricted: true, enumeratedNames, null, isBoolean: false)
		{
		}

		/// <summary>
		/// Returns a string resembling the OML syntax which constructs this domain. Not guaranteed to parse as OML.
		/// </summary>
		/// <returns>A string.</returns>
		public override string ToString()
		{
			return "Enumerated[...]";
		}

		/// <summary>
		/// Looks up the index of a string in an enum domain.
		///
		/// This is a helper function which is used internally. It is not intended to be called by user code.
		/// </summary>
		/// <param name="value">The string to look up.</param>
		/// <returns>The index.</returns>
		internal override Rational GetOrdinal(string value)
		{
			if (_ordinalMapping == null)
			{
				_ordinalMapping = new Dictionary<string, int>();
				for (int i = 0; i < base.EnumeratedNames.Length; i++)
				{
					_ordinalMapping.Add(base.EnumeratedNames[i], i);
				}
			}
			if (!_ordinalMapping.TryGetValue(value, out var value2))
			{
				throw new InvalidModelDataException(string.Format(CultureInfo.CurrentCulture, Resources.TheValue0WasNotPresentInTheEnumeratedDomain, new object[1] { value }));
			}
			return value2;
		}

		internal override CspDomain MakeCspDomain(ConstraintSystem solver)
		{
			Rational minValue = base.MinValue;
			Rational maxValue = base.MaxValue;
			return solver.CreateIntegerInterval(Domain.FiniteIntFromRational(minValue.GetCeiling()), Domain.FiniteIntFromRational(maxValue.GetFloor()));
		}

		internal override Expression MakeOmlDomain(OmlWriter writer, SolveRewriteSystem rewrite)
		{
			Expression[] array = new Expression[base.EnumeratedNames.Length];
			for (int i = 0; i < base.EnumeratedNames.Length; i++)
			{
				array[i] = new StringConstant(rewrite, base.EnumeratedNames[i]);
			}
			Expression domainExpr = rewrite.Builtin.Enum.Invoke(rewrite.Builtin.List.Invoke(array));
			return writer.AddDomainsSection(this, domainExpr);
		}

		internal override string FormatValue(IFormatProvider format, double value)
		{
			return base.EnumeratedNames[(int)value];
		}
	}
}
