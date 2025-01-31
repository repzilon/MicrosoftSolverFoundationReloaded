using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Conic inequality structure.
	/// </summary>
	internal class ConicStructure
	{
		public int kL;

		public List<int> mQ;

		public List<int> mR;

		public ConicStructure(int mL, List<int> mQ, List<int> mR)
		{
			kL = mL;
			this.mQ = new List<int>(mQ);
			if (mQ.Count > 0 && mQ.Min() < 2)
			{
				throw new ArgumentException("Convex quadratic cones are defined for at least 2 dimensions.");
			}
			this.mR = new List<int>(mR);
			if (mR.Count > 0 && mR.Min() < 3)
			{
				throw new ArgumentException("Rotated quadratic cones are defined for at least 3 dimensions.");
			}
		}

		/// <summary>Create a new instance.
		/// </summary>
		public ConicStructure(ConicStructure kone)
		{
			kL = kone.kL;
			mQ = new List<int>(kone.mQ);
			mR = new List<int>(kone.mR);
		}
	}
}
