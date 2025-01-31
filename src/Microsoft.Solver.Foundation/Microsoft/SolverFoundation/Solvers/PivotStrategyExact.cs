using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal abstract class PivotStrategyExact : PivotStrategy<Rational>
	{
		protected PrimalExact _pes;

		private VectorRational _vecDelta;

		public VectorRational Delta => _vecDelta;

		protected PivotStrategyExact(SimplexTask thd, PrimalExact pes)
			: base(thd)
		{
			_pes = pes;
		}

		public override void Init()
		{
			base.Init();
			if (_vecDelta == null || _vecDelta.RcCount != _rowLim)
			{
				_vecDelta = new VectorRational(_rowLim);
			}
		}

		/// <summary>
		/// Calls FindEnteringVar to set _varEnter, _vvkEnter, and _sign.
		/// Calls ComputeDelta to set _vecDelta.
		/// Calls ValidateCost to validate the entering variable.
		/// Calls FindLeavingVar to set _scale, _ivarLeave, _varLeave, and _vvkLeave.
		/// </summary>
		public override bool FindNext()
		{
			InitFindNext();
			if (!FindEnteringVar())
			{
				return false;
			}
			ComputeDelta();
			FindLeavingVar();
			return true;
		}

		protected virtual void InitFindNext()
		{
		}

		protected abstract bool FindEnteringVar();

		protected virtual void ComputeDelta()
		{
			SimplexSolver.ComputeColumnDelta(_mod.Matrix, base.VarEnter, _pes.Basis, _vecDelta);
			_ivarKey = -1;
			_numKey = Rational.Indeterminate;
		}

		protected virtual Rational ComputeRelativeCostFromDelta()
		{
			SimplexBasis basis = _pes.Basis;
			Rational rational = 0;
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_pes.GetCostVector());
			while (iter.IsValid)
			{
				int basisSlot = basis.GetBasisSlot(iter.Rc);
				if (basisSlot >= 0)
				{
					Rational coef = _vecDelta.GetCoef(basisSlot);
					if (!coef.IsZero)
					{
						rational += coef * iter.Value;
					}
				}
				iter.Advance();
			}
			return _pes.Cost(base.VarEnter) - rational;
		}

		protected virtual void FindLeavingVar()
		{
			base.Scale = _pes.GetUpperBound(base.VarEnter) - _pes.GetLowerBound(base.VarEnter);
			base.IvarLeave = -1;
			base.VarLeave = base.VarEnter;
			base.VvkLeave = ((base.Sign < 0) ? SimplexVarValKind.Lower : SimplexVarValKind.Upper);
			SimplexBasis basis = _pes.Basis;
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_vecDelta);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				Rational value = iter.Value;
				int basicVar = basis.GetBasicVar(rc);
				Rational rational = ((base.Sign < 0) ? (-value) : value);
				Rational rational2 = ((rational < 0) ? _pes.GetLowerBound(basicVar) : _pes.GetUpperBound(basicVar));
				if (rational2.IsFinite)
				{
					Rational rational3 = (rational2 - _pes.GetBasicValue(rc)) / rational;
					if (!(rational3 > base.Scale) && (!base.Scale.IsZero || basicVar <= base.VarLeave))
					{
						base.Scale = rational3;
						base.IvarLeave = rc;
						_ivarKey = rc;
						_numKey = _vecDelta.GetCoef(_ivarKey);
						base.VarLeave = basicVar;
						base.VvkLeave = ((rational < 0) ? SimplexVarValKind.Lower : SimplexVarValKind.Upper);
					}
				}
				iter.Advance();
			}
		}

		protected override Rational GetRationalFromNumber(Rational num)
		{
			return num;
		}
	}
}
