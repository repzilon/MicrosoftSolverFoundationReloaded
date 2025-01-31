namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A composite class that is designed specific for representing power-list domains.
	/// </summary>
	public sealed class CspPowerList : CspComposite
	{
		private CspDomain.ValueKind _kind;

		private CspDomain _baseline;

		private int _maxLength;

		/// <summary>
		/// Get the value kind of this power-set domain.
		/// </summary>
		public CspDomain.ValueKind Kind => _kind;

		internal CspDomain Baseline => _baseline;

		internal int MaxLength => _maxLength;

		/// <summary>
		/// Construct a composite that represents the power-set of the baseline.
		/// </summary>
		internal CspPowerList(ConstraintSystem solver, object key, CspDomain baseline, int maxLength)
			: base(solver, key)
		{
			_kind = baseline.Kind;
			_baseline = baseline;
			_maxLength = maxLength;
			if (baseline.Count == 0 || maxLength == 0)
			{
				_baseline = solver.Empty;
				base.AddField(solver.CreateIntegerInterval(0, 0), "length", 1);
				base.AddField(solver.CreateIntegerInterval(0, 0), "list", 1);
			}
			else
			{
				CspTerm cspTerm = base.AddField(solver.CreateIntegerInterval(0, maxLength), "length", 1)[0];
				CspTerm[] array = base.AddField(baseline, "list", maxLength);
				CspSolverDomain cspSolverDomain = CspSetListHelper.AsCspSolverDomain(baseline);
				for (int i = 0; i < maxLength; i++)
				{
					AddConstraints((Constant(i) < cspTerm) | CspSetListHelper.ValueEqual(base.ConstraintContainer, array[i], cspSolverDomain.GetValue(cspSolverDomain.First)));
				}
			}
			Freeze();
		}
	}
}
