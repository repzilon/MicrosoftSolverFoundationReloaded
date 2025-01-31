using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A composite class that is designed specific for representing power-set domains.
	/// </summary>
	public sealed class CspPowerSet : CspComposite
	{
		private CspDomain.ValueKind _kind;

		private CspDomain _baseline;

		private Dictionary<object, int> _valueIndexMap;

		/// <summary>
		/// Get the value kind of this power-set domain.
		/// </summary>
		public CspDomain.ValueKind Kind => _kind;

		internal CspDomain Baseline => _baseline;

		internal Dictionary<object, int> ValueIndexMap => _valueIndexMap;

		/// <summary>
		/// Construct a composite that represents the power-set of the baseline.
		/// </summary>
		internal CspPowerSet(ConstraintSystem solver, object key, CspDomain baseline)
			: base(solver, key)
		{
			_kind = baseline.Kind;
			_baseline = baseline;
			_valueIndexMap = new Dictionary<object, int>();
			if (baseline.Count == 0)
			{
				_ = base.AddField(solver.CreateIntegerInterval(0, 0), "cardinality", 1)[0];
				CspTerm[] array = base.AddField(solver.DefaultBoolean, "set", 1);
				AddConstraints(array[0]);
			}
			else
			{
				int count = baseline.Count;
				CspTerm cspTerm = base.AddField(solver.CreateIntegerInterval(0, count), "cardinality", 1)[0];
				CspTerm[] inputs = base.AddField(solver.DefaultBoolean, "set", count);
				AddConstraints(Equal(cspTerm, Sum(inputs)));
				CspSolverDomain cspSolverDomain = baseline as CspSolverDomain;
				int num = 0;
				foreach (object item in cspSolverDomain.Values())
				{
					_valueIndexMap.Add(item, num++);
				}
			}
			Freeze();
		}
	}
}
