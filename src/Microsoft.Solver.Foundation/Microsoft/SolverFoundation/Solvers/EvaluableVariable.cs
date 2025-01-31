using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Variable, whose value can be explicitly modified
	/// </summary>
	[DebuggerDisplay("Value={Value}")]
	internal sealed class EvaluableVariable : EvaluableNumericalTerm
	{
		/// <summary>
		/// A domain can optionally be attached to the variable
		/// </summary>
		public LocalSearchDomain Domain { get; private set; }

		/// <summary>
		/// Variable with an initial value and specific domain
		/// </summary>
		internal EvaluableVariable(double initialValue, LocalSearchDomain domain)
			: base(0)
		{
			_value = initialValue;
			Domain = domain;
		}

		/// <summary>
		/// Variable with a default domain (full set of real values)
		/// </summary>
		internal EvaluableVariable(double initialValue)
			: this(initialValue, LocalSearchDomain.DefaultDomain)
		{
		}

		internal void ChangeValue(double newValue, out bool change)
		{
			change = _value != newValue;
			_value = newValue;
		}

		internal override void Recompute(out bool change)
		{
			change = false;
		}

		internal override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return Enumerable.Empty<EvaluableTerm>();
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return null;
		}

		internal void ResetDomain(LocalSearchDomain dom, Random prng)
		{
			Domain = dom;
			if (!dom.Contains(_value))
			{
				_value = dom.Sample(prng);
			}
		}

		[Conditional("DEBUG")]
		private void CheckValue()
		{
		}
	}
}
