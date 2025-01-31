using System;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class PartialPricingDblSOS : PartialPricingDbl
	{
		public PartialPricingDblSOS(SimplexTask thd, PrimalDouble pds, int cvarTarget)
			: base(thd, pds, cvarTarget)
		{
		}

		/// <summary>
		/// Sets _varEnter and _sign.
		/// </summary>
		protected override bool FindEnteringVar()
		{
			double num = 0.0;
			base.VarEnter = -1;
			SimplexBasis basis = base.Pds.Basis;
			SOSUtils.GetCurrentBasis(_thd, base.Pds);
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecCost);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				double value = iter.Value;
				if (SOSUtils.IsSOSVar(_thd, rc) && !SOSUtils.IsEnteringCandidate(_thd, rc))
				{
					iter.RemoveAndAdvance();
					continue;
				}
				SimplexVarValKind vvk = basis.GetVvk(rc);
				int sign;
				switch (vvk)
				{
				case SimplexVarValKind.Lower:
					if (value >= 0.0 - _numThresh)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					sign = -1;
					break;
				case SimplexVarValKind.Upper:
					if (value <= _numThresh)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					sign = 1;
					break;
				case SimplexVarValKind.Zero:
					sign = Math.Sign(value);
					if (Math.Abs(value) <= _numThresh)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					break;
				default:
					iter.RemoveAndAdvance();
					continue;
				}
				iter.Advance();
				value = value * value / _mpvarnumWeights[rc];
				if (num < value)
				{
					num = value;
					base.VarEnter = rc;
					base.VvkEnter = vvk;
					base.Sign = sign;
					_dblApproxCost = value;
				}
			}
			if (base.VarEnter > 0 && _thd.Model.IsSOS)
			{
				SOSUtils.UpdateEnteringVar(_thd, base.VarEnter);
			}
			return base.VarEnter >= 0;
		}
	}
}
