namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A Boolean which is equal to the sequential ordering of a otherSet of integers
	/// </summary>
	internal abstract class CspInequality : Comparison
	{
		internal bool _resolved;

		internal int[] _rgLo;

		internal int[] _rgHi;

		internal bool[] _rgChanged;

		internal CspInequality(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, inputs)
		{
			_rgLo = new int[Width];
			_rgHi = new int[Width];
			_rgChanged = new bool[Width];
		}

		internal abstract CspSolverTerm Scan(out bool isFalse, out bool isTrue);

		internal override void Reset()
		{
			_resolved = false;
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			if (base.Count == 0)
			{
				conflict = this;
				return false;
			}
			conflict = null;
			if (_resolved)
			{
				return false;
			}
			int width = Width;
			if (width < 2)
			{
				return Force(choice: true, out conflict);
			}
			for (int i = 0; i < width; i++)
			{
				_rgChanged[i] = false;
				CspSolverTerm cspSolverTerm = _inputs[i];
				if (cspSolverTerm.Count == 0)
				{
					conflict = cspSolverTerm;
					return false;
				}
				_rgLo[i] = ScaleToOutput(cspSolverTerm.First, i);
				_rgHi[i] = ScaleToOutput(cspSolverTerm.Last, i);
			}
			conflict = Scan(out var isFalse, out var isTrue);
			bool flag = false;
			if (isTrue || isFalse)
			{
				_resolved = true;
				if (1 == base.Count)
				{
					if (isFalse && IsTrue)
					{
						return conflict.Intersect(ConstraintSystem.DEmpty, out conflict);
					}
					if (isTrue && !IsTrue)
					{
						return Intersect(ConstraintSystem.DEmpty, out conflict);
					}
					conflict = null;
				}
				else
				{
					flag = Force(isTrue, out conflict);
				}
			}
			if (1 != base.Count || !IsTrue)
			{
				return flag;
			}
			for (int j = 0; j < width; j++)
			{
				if (_rgChanged[j])
				{
					CspSolverTerm cspSolverTerm2 = _inputs[j];
					if (_rgLo[j] > _rgHi[j])
					{
						return _inputs[j].Intersect(ConstraintSystem.DEmpty, out conflict);
					}
					flag |= cspSolverTerm2.Intersect(ScaleToInput(_rgLo[j], j), ScaleToInput(_rgHi[j], j), out conflict);
					if (conflict != null)
					{
						break;
					}
				}
			}
			return flag;
		}
	}
}
