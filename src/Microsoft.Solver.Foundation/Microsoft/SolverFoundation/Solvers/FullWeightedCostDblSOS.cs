using System;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class FullWeightedCostDblSOS : FullWeightedCostDbl
	{
		public FullWeightedCostDblSOS(SimplexTask thd, PrimalDouble pds)
			: base(thd, pds)
		{
		}

		public override void Init()
		{
			base.Init();
		}

		protected override int GetBestVar(ref VectorDDouble vec, out double numCostRet, out int signRet)
		{
			int[] varIVar = base.Pds.Basis.VarIVar;
			numCostRet = 0.0;
			signRet = 0;
			int num = -1;
			double num2 = 0.0;
			SOSUtils.GetCurrentBasis(_thd, base.Pds);
			VectorDDouble.Iter iter = new VectorDDouble.Iter(ref _vecCostFiltered);
			while (iter.IsValid)
			{
				int num3 = iter.Rc(ref vec);
				if (SOSUtils.IsSOSVar(_thd, num3) && !SOSUtils.IsEnteringCandidate(_thd, num3))
				{
					iter.RemoveAndAdvance(ref vec);
					continue;
				}
				double num4 = iter.Value(ref vec);
				double num5 = _mpvarnumWeights[num3];
				SimplexVarValKind simplexVarValKind = SimplexBasis.InterpretVvk(varIVar[num3]);
				double num6 = num4 * num4;
				double num7 = num5 * num2;
				if ((simplexVarValKind == SimplexVarValKind.Lower && num4 < 0.0) || (0.0 < num4 && simplexVarValKind == SimplexVarValKind.Upper) || simplexVarValKind == SimplexVarValKind.Zero)
				{
					iter.Advance();
					if (num6 > num7)
					{
						signRet = Math.Sign(num4);
						num2 = num6 / num5;
						num = num3;
					}
				}
				else
				{
					iter.RemoveAndAdvance(ref vec);
				}
			}
			if (num > 0 && _thd.Model.IsSOS)
			{
				SOSUtils.UpdateEnteringVar(_thd, num);
			}
			numCostRet = num2;
			return num;
		}
	}
}
