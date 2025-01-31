using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal static class SOSUtils
	{
		public static void GetCurrentBasis<T>(SimplexTask thd, AlgorithmBase<T> algorithm)
		{
			foreach (ISOSStatus value2 in thd._sosStatus.Values)
			{
				value2.Clear();
			}
			int[] rgvarBasic = algorithm.Basis._rgvarBasic;
			foreach (int var in rgvarBasic)
			{
				int row = GetRow(thd.Model._mpvarSOS2Row, var);
				int row2 = GetRow(thd.Model._mpvarSOS1Row, var);
				if (row < 0 && row2 < 0)
				{
					continue;
				}
				ISOSStatus value;
				if (row >= 0)
				{
					if (!thd._sosStatus.TryGetValue(row, out value))
					{
						value = new SOS2Status();
						thd._sosStatus[row] = value;
					}
				}
				else if (!thd._sosStatus.TryGetValue(row2, out value))
				{
					value = new SOS1Status();
					thd._sosStatus[row2] = value;
				}
				value.Append(var);
			}
		}

		public static int GetRow(int[] _mpvarSOSRow, int var)
		{
			if (_mpvarSOSRow == null)
			{
				return -1;
			}
			return _mpvarSOSRow[var];
		}

		/// <summary> check status of SOS1/SOS2 row
		/// For SOS2 return true if no element is basic or if a Neightbor is basic, otherwise returns false.
		/// For SOS1 returns true if no element is basic, otherwise returns false.
		/// </summary>
		/// <param name="thd"></param>
		/// <param name="var"></param>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "sos1Row")]
		public static bool IsEnteringCandidate(SimplexTask thd, int var)
		{
			int row = GetRow(thd.Model._mpvarSOS2Row, var);
			int row2 = GetRow(thd.Model._mpvarSOS1Row, var);
			bool flag = row >= 0;
			int key = (flag ? row : row2);
			if (!thd._sosStatus.TryGetValue(key, out var value))
			{
				value = ((!flag) ? ((ISOSStatus)new SOS1Status()) : ((ISOSStatus)new SOS2Status()));
				thd._sosStatus[key] = value;
			}
			if (flag)
			{
				SOS2Status sOS2Status = value as SOS2Status;
				DebugContracts.NonNull(sOS2Status);
				if (sOS2Status.Count == 0)
				{
					return true;
				}
				if (sOS2Status.Count == 1 && IsNeightbor(thd, row, sOS2Status, var))
				{
					return true;
				}
				return false;
			}
			SOS1Status sOS1Status = value as SOS1Status;
			return !sOS1Status.IsFull;
		}

		/// <summary>Called when variable is decided to be the entering to the basis
		/// </summary>
		public static void UpdateEnteringVar(SimplexTask thd, int var)
		{
			int row = GetRow(thd.Model._mpvarSOS2Row, var);
			int row2 = GetRow(thd.Model._mpvarSOS1Row, var);
			if (row >= 0 || row2 >= 0)
			{
				if (row >= 0)
				{
					thd._sosStatus[row].Append(var);
				}
				else
				{
					thd._sosStatus[row2].Append(var);
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="thd"></param>
		/// <param name="sos2Row"></param>
		/// <param name="sos2Status"></param>
		/// <param name="var"></param>
		/// <returns></returns>
		public static bool IsNeightbor(SimplexTask thd, int sos2Row, SOS2Status sos2Status, int var)
		{
			SOSNode sOSNode = thd.Model._sosRows[sos2Row];
			if (sOSNode.Vars.Length <= 1)
			{
				return true;
			}
			int num = Array.FindIndex(sOSNode.Vars, (int tempVar) => tempVar == sos2Status.Var1);
			if (num == 0)
			{
				if (sOSNode.Vars[num + 1] == var)
				{
					return true;
				}
			}
			else if (num == sOSNode.Vars.Length - 1)
			{
				if (sOSNode.Vars[num - 1] == var)
				{
					return true;
				}
			}
			else if (sOSNode.Vars[num - 1] == var || sOSNode.Vars[num + 1] == var)
			{
				return true;
			}
			return false;
		}

		/// <summary>Is var from type of SOS
		/// </summary>
		/// <param name="thd">the SimplexTask</param>
		/// <param name="var">vid of var</param>
		/// <returns></returns>
		public static bool IsSOSVar(SimplexTask thd, int var)
		{
			if (GetRow(thd.Model._mpvarSOS2Row, var) == -1)
			{
				return GetRow(thd.Model._mpvarSOS1Row, var) != -1;
			}
			return true;
		}
	}
}
