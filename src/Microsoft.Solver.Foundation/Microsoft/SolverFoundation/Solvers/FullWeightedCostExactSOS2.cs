using System;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class FullWeightedCostExactSOS2 : FullWeightedCostApprox
	{
		public FullWeightedCostExactSOS2(SimplexTask thd, PrimalExact pes)
			: base(thd, pes)
		{
		}

		public override void Init()
		{
			base.Init();
		}

		protected override bool GetNextVar(double numMin, ref Vector<double>.Iter iter, out int varRet, out double numCostRet, out int signRet)
		{
			SimplexBasis basis = _pes.Basis;
			SOSUtils.GetCurrentBasis(_thd, _pes);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				double value = iter.Value;
				if (SOSUtils.IsSOSVar(_thd, rc) && !SOSUtils.IsEnteringCandidate(_thd, rc))
				{
					iter.RemoveAndAdvance();
					continue;
				}
				switch (basis.GetVvk(rc))
				{
				case SimplexVarValKind.Lower:
					if (value >= 0.0)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					break;
				case SimplexVarValKind.Upper:
					if (value <= 0.0)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					break;
				default:
					iter.RemoveAndAdvance();
					continue;
				case SimplexVarValKind.Zero:
					break;
				}
				iter.Advance();
				double num = value * value / _mpvarnumWeights[rc];
				if (num > numMin)
				{
					signRet = Math.Sign(value);
					numCostRet = num;
					varRet = rc;
					if (varRet > 0 && _thd.Model.IsSOS)
					{
						SOSUtils.UpdateEnteringVar(_thd, varRet);
					}
					return true;
				}
			}
			numCostRet = 0.0;
			signRet = 0;
			varRet = -1;
			return false;
		}
	}
}
