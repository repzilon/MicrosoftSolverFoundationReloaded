using System;
using System.Text;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class OptimalGoalValues
	{
		private Rational[] _rgnum;

		public int Count => _rgnum.Length;

		public Rational this[int inum]
		{
			get
			{
				return _rgnum[inum];
			}
			set
			{
				_rgnum[inum] = value;
			}
		}

		protected OptimalGoalValues()
		{
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="cnum"></param>
		public OptimalGoalValues(int cnum)
		{
			_rgnum = new Rational[cnum];
			Clear();
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="cnum"></param>
		/// <param name="rgnum"></param>
		internal OptimalGoalValues(int cnum, params Rational[] rgnum)
		{
			_rgnum = new Rational[cnum];
			Array.Copy(rgnum, _rgnum, cnum);
		}

		public void Clear()
		{
			int num = _rgnum.Length;
			while (--num >= 0)
			{
				ref Rational reference = ref _rgnum[num];
				reference = Rational.Indeterminate;
			}
		}

		public int CompareTo(OptimalGoalValues ogv)
		{
			return Compare(this, ogv);
		}

		public OptimalGoalValues Clone()
		{
			OptimalGoalValues optimalGoalValues = new OptimalGoalValues();
			optimalGoalValues._rgnum = (Rational[])_rgnum.Clone();
			return optimalGoalValues;
		}

		internal OptimalGoalValues Scale(Rational factor)
		{
			OptimalGoalValues optimalGoalValues = new OptimalGoalValues(Count);
			for (int i = 0; i < Count; i++)
			{
				ref Rational reference = ref optimalGoalValues._rgnum[i];
				reference = _rgnum[i] * factor;
			}
			return optimalGoalValues;
		}

		internal OptimalGoalValues ScaleToUserModel(SimplexTask thread)
		{
			OptimalGoalValues optimalGoalValues = new OptimalGoalValues(Count);
			for (int i = 0; i < Count; i++)
			{
				ref Rational reference = ref optimalGoalValues._rgnum[i];
				reference = thread.Model.MapValueFromVarToVid(thread.Model.GetGoalVar(i), _rgnum[i]);
			}
			return optimalGoalValues;
		}

		public static int Compare(OptimalGoalValues ogv1, OptimalGoalValues ogv2)
		{
			if (ogv2 == null)
			{
				if (ogv1 != null)
				{
					return -1;
				}
				return 0;
			}
			if (ogv1 == null)
			{
				return 1;
			}
			for (int i = 0; i < ogv1._rgnum.Length; i++)
			{
				int num = ogv1._rgnum[i].CompareTo(ogv2._rgnum[i]);
				if (num != 0)
				{
					return num;
				}
			}
			return 0;
		}

		public static bool operator >=(OptimalGoalValues ogv1, OptimalGoalValues ogv2)
		{
			return Compare(ogv1, ogv2) >= 0;
		}

		public static bool operator <=(OptimalGoalValues ogv1, OptimalGoalValues ogv2)
		{
			return Compare(ogv1, ogv2) <= 0;
		}

		public static bool operator >(OptimalGoalValues ogv1, OptimalGoalValues ogv2)
		{
			return Compare(ogv1, ogv2) > 0;
		}

		public static bool operator <(OptimalGoalValues ogv1, OptimalGoalValues ogv2)
		{
			return Compare(ogv1, ogv2) < 0;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < Count - 1; i++)
			{
				stringBuilder.AppendFormat("{0} ", (double)_rgnum[i]);
			}
			stringBuilder.Append((double)_rgnum[Count - 1]);
			return stringBuilder.ToString();
		}
	}
}
