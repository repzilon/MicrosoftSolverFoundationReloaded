using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Base class for Set/List related operations
	/// </summary>
	internal static class CspSetListHelper
	{
		/// <summary>
		/// Validate inputs
		/// </summary>
		/// <param name="isCheckingForSets">Whether we are verifying the input vars for set variables</param>
		/// <param name="solver">The solver</param>
		/// <param name="setlistVar1">The first set/list variable</param>
		/// <param name="setlistVar2">The second set/list variable</param>
		internal static void CheckForBadInputs(bool isCheckingForSets, ConstraintSystem solver, CspTerm setlistVar1, CspTerm setlistVar2)
		{
			if (solver == null || setlistVar1 == null || setlistVar2 == null)
			{
				throw new ArgumentNullException(Resources.SetListNullInput);
			}
			if (IsSetVariable(setlistVar1) != isCheckingForSets || IsSetVariable(setlistVar2) != isCheckingForSets || IsListVariable(setlistVar1) == isCheckingForSets || IsListVariable(setlistVar2) == isCheckingForSets)
			{
				throw new ArgumentException(Resources.SetListWrongDomain);
			}
			CspSolverDomain powerSetListBaseline = GetPowerSetListBaseline(setlistVar1);
			CspSolverDomain powerSetListBaseline2 = GetPowerSetListBaseline(setlistVar2);
			if (powerSetListBaseline.Kind != powerSetListBaseline2.Kind && powerSetListBaseline.Count != 0 && powerSetListBaseline2.Count != 0)
			{
				throw new ArgumentException(Resources.SetListIncompatibleDomainKind);
			}
			CheckForIncompatibleSymbolSets(powerSetListBaseline, powerSetListBaseline2);
		}

		internal static void CheckForBadInputs(bool isCheckingForSets, ConstraintSystem solver, CspTerm setlistVar)
		{
			if (solver == null || setlistVar == null)
			{
				throw new ArgumentNullException(Resources.SetListNullInput);
			}
			if (IsSetVariable(setlistVar) != isCheckingForSets || IsListVariable(setlistVar) == isCheckingForSets)
			{
				throw new ArgumentException(Resources.SetListWrongDomain);
			}
		}

		internal static void CheckForBadInputs(bool isCheckingForSets, ConstraintSystem solver, CspTerm setlistVar, int[] set)
		{
			if (solver == null || setlistVar == null || set == null)
			{
				throw new ArgumentNullException(Resources.SetListNullInput);
			}
			if (IsSetVariable(setlistVar) != isCheckingForSets || IsListVariable(setlistVar) == isCheckingForSets)
			{
				throw new ArgumentException(Resources.SetListWrongDomain);
			}
		}

		internal static void CheckForBadInputs(bool isCheckingForSets, ConstraintSystem solver, int[] set, CspTerm setlistVar)
		{
			if (solver == null || setlistVar == null || set == null)
			{
				throw new ArgumentNullException(Resources.SetListNullInput);
			}
			if (IsSetVariable(setlistVar) != isCheckingForSets || IsListVariable(setlistVar) == isCheckingForSets)
			{
				throw new ArgumentException(Resources.SetListWrongDomain);
			}
		}

		internal static void CheckForNonscalorInputs(CspTerm element)
		{
			if (element == null)
			{
				throw new ArgumentNullException(Resources.SetListNullInput);
			}
			if (element is CspCompositeVariable)
			{
				throw new ArgumentException(Resources.SetListNonscalorElement);
			}
		}

		internal static void CheckForIncompatibleSymbolSets(CspDomain baseline1, CspDomain baseline2)
		{
			if (baseline1 == null || baseline2 == null)
			{
				throw new ArgumentNullException(Resources.SetListNullBaseline);
			}
			if (baseline1.Kind == CspDomain.ValueKind.Symbol && baseline2.Kind == CspDomain.ValueKind.Symbol && baseline1 != baseline2)
			{
				throw new ArgumentException(Resources.SetListSymbolSetVarNotAllowed);
			}
		}

		/// <summary>
		/// Check if the Term is a Set variable
		/// </summary>
		internal static bool IsSetVariable(CspTerm setVar)
		{
			if (setVar is CspCompositeVariable cspCompositeVariable)
			{
				return cspCompositeVariable.DomainComposite is CspPowerSet;
			}
			return false;
		}

		/// <summary>
		/// Check if the Term is a List variable
		/// </summary>
		internal static bool IsListVariable(CspTerm listVar)
		{
			if (listVar is CspCompositeVariable cspCompositeVariable)
			{
				return cspCompositeVariable.DomainComposite is CspPowerList;
			}
			return false;
		}

		/// <summary>
		/// Sort the given integer subset
		/// </summary>
		internal static void NormalizeConstantSet(int[] subset)
		{
			if (!CspSetDomain.IsOrderedUniqueSet(subset, 0, subset.Length))
			{
				Statics.QuickSort(subset, 0, subset.Length - 1);
				if (!CspSetDomain.IsOrderedUniqueSet(subset, 0, subset.Length))
				{
					throw new ArgumentException(Resources.SetListWrongSubset);
				}
			}
		}

		/// <summary>
		/// Sort the given decimal subset
		/// </summary>
		internal static void NormalizeConstantSet(int precision, double[] subset)
		{
			if (!CspSetDomain.IsOrderedUniqueSet(precision, subset, 0, subset.Length))
			{
				Array.Sort(subset);
				if (!CspSetDomain.IsOrderedUniqueSet(precision, subset, 0, subset.Length))
				{
					throw new ArgumentException(Resources.SetListWrongSubset);
				}
			}
		}

		/// <summary>
		/// Sort the given integer list and remove duplicates
		/// </summary>
		internal static void NormalizeConstantList(int[] list, out int[] uniqueOrderedSet)
		{
			if (!CspSetDomain.IsOrderedUniqueSet(list, 0, list.Length))
			{
				uniqueOrderedSet = new int[list.Length];
				for (int i = 0; i < list.Length; i++)
				{
					uniqueOrderedSet[i] = list[i];
				}
				Statics.QuickSort(uniqueOrderedSet, 0, uniqueOrderedSet.Length - 1);
				int num = 0;
				for (int j = 1; j < uniqueOrderedSet.Length; j++)
				{
					if (uniqueOrderedSet[j] != uniqueOrderedSet[j - 1])
					{
						num++;
						uniqueOrderedSet[num] = uniqueOrderedSet[j];
					}
				}
				Array.Resize(ref uniqueOrderedSet, num + 1);
			}
			else
			{
				uniqueOrderedSet = list;
			}
		}

		/// <summary>
		/// Sort the given decimal list and remove duplicates
		/// </summary>
		internal static void NormalizeConstantList(int precision, double[] list, out double[] uniqueOrderedSet)
		{
			if (!CspSetDomain.IsOrderedUniqueSet(precision, list, 0, list.Length))
			{
				uniqueOrderedSet = new double[list.Length];
				for (int i = 0; i < list.Length; i++)
				{
					uniqueOrderedSet[i] = list[i];
				}
				Array.Sort(uniqueOrderedSet);
				int num = 0;
				for (int j = 1; j < uniqueOrderedSet.Length; j++)
				{
					if (!CspIntegerDomain.AreDecimalsEqual(precision, uniqueOrderedSet[j], uniqueOrderedSet[j - 1]))
					{
						num++;
						uniqueOrderedSet[num] = uniqueOrderedSet[j];
					}
				}
				Array.Resize(ref uniqueOrderedSet, num + 1);
			}
			else
			{
				uniqueOrderedSet = list;
			}
		}

		/// <summary>
		/// Decide if the booleanVar is fixed to value True.
		/// </summary>
		/// <param name="booleanVar">A CspBooleanVariable whose value must be fixed</param>
		/// <returns>Whether booleanVar is fixed to True</returns>
		internal static bool IsTrue(CspTerm booleanVar)
		{
			CspBooleanVariable cspBooleanVariable = booleanVar as CspBooleanVariable;
			return cspBooleanVariable.IsTrue;
		}

		/// <summary>
		/// Convert the integer into a decimal in the domain.
		/// </summary>
		/// <param name="domain">A decimal domain</param>
		/// <param name="ival">An integer representation of the decimal in the domain</param>
		/// <returns>The decimal value</returns>
		internal static double GetDecimalValue(CspDomain domain, int ival)
		{
			CspSolverDomain cspSolverDomain = domain as CspSolverDomain;
			return (double)cspSolverDomain.GetValue(ival);
		}

		/// <summary>
		/// Convert the integer into a symbol in the domain.
		/// </summary>
		/// <param name="domain">A symbol domain</param>
		/// <param name="ival">An integer representation of the symbol in the domain</param>
		/// <returns>The symbol value</returns>
		internal static string GetSymbolValue(CspDomain domain, int ival)
		{
			CspSymbolDomain cspSymbolDomain = domain as CspSymbolDomain;
			return (string)cspSymbolDomain.GetValue(ival);
		}

		/// <summary>
		/// Create a Term that represents the equality Term: var = val, based on var's kind
		/// </summary>
		internal static CspTerm ValueEqual(ConstraintSystem model, CspTerm var, object val)
		{
			CspSolverTerm cspSolverTerm = AsCspSolverTerm(var);
			switch (cspSolverTerm.Kind)
			{
			case CspDomain.ValueKind.Integer:
				return model.Equal((int)val, var);
			case CspDomain.ValueKind.Decimal:
			{
				CspTerm cspTerm2 = model.Constant(cspSolverTerm.OutputScale, (double)val);
				return model.Equal(cspTerm2, var);
			}
			case CspDomain.ValueKind.Symbol:
			{
				CspTerm cspTerm = model.Constant(cspSolverTerm.BaseValueSet, (string)val);
				return model.Equal(cspTerm, var);
			}
			default:
				throw new ArgumentException(Resources.UnknownDomainType);
			}
		}

		/// <summary>
		/// Cast Term t to CspSolverTerm
		/// </summary>
		internal static CspSolverTerm AsCspSolverTerm(CspTerm t)
		{
			CspSolverTerm cspSolverTerm = t as CspSolverTerm;
			DebugContracts.NonNull(cspSolverTerm);
			return cspSolverTerm;
		}

		/// <summary>
		/// Cast Domain d to CspSolverDomain
		/// </summary>
		internal static CspSolverDomain AsCspSolverDomain(CspDomain d)
		{
			CspSolverDomain cspSolverDomain = d as CspSolverDomain;
			DebugContracts.NonNull(cspSolverDomain);
			return cspSolverDomain;
		}

		/// <summary>
		/// Get the CspPowerSet composite from the setVar
		/// </summary>
		internal static CspPowerSet GetPowerSetDomain(CspTerm setVar)
		{
			return (setVar as CspCompositeVariable).DomainComposite as CspPowerSet;
		}

		/// <summary>
		/// Get the CspPowerSet or CspPowerList composite baseline from the setlistVar
		/// </summary>
		internal static CspSolverDomain GetPowerSetListBaseline(CspTerm setlistVar)
		{
			if (IsSetVariable(setlistVar))
			{
				return GetPowerSetDomain(setlistVar).Baseline as CspSolverDomain;
			}
			if (IsListVariable(setlistVar))
			{
				return GetPowerListDomain(setlistVar).Baseline as CspSolverDomain;
			}
			throw new ArgumentException(Resources.GetPowerSetListBaselineIsCalledOnANonSetNonListVar);
		}

		/// <summary>
		/// Get the CspPowerSet composite ValueIndexMap from the setVar
		/// </summary>
		internal static Dictionary<object, int> GetPowerSetValueIndexMap(CspTerm setVar)
		{
			return GetPowerSetDomain(setVar).ValueIndexMap;
		}

		/// <summary>
		/// Get the Fields and Cardinality Terms from the setVar
		/// </summary>
		internal static void GetPowerSetFields(CspTerm setVar, out CspTerm card, out CspTerm[] set)
		{
			CspCompositeVariable cspCompositeVariable = setVar as CspCompositeVariable;
			card = cspCompositeVariable.FieldInternal("cardinality", 0);
			set = cspCompositeVariable.FieldsInternal("set");
		}

		/// <summary>
		/// Get the CspPowerList composite from the listVar
		/// </summary>
		internal static CspPowerList GetPowerListDomain(CspTerm listVar)
		{
			return (listVar as CspCompositeVariable).DomainComposite as CspPowerList;
		}

		/// <summary>
		/// Get the list and length field Terms from the listVar
		/// </summary>
		internal static void GetPowerListFields(CspTerm listVar, out CspTerm length, out CspTerm[] list)
		{
			CspCompositeVariable cspCompositeVariable = listVar as CspCompositeVariable;
			length = cspCompositeVariable.FieldInternal("length", 0);
			list = cspCompositeVariable.FieldsInternal("list");
		}

		/// <summary>
		/// Get the max length of the sublists in the power-list domain from the listVar
		/// </summary>
		internal static int GetPowerListMaxLength(CspTerm listVar)
		{
			return GetPowerListDomain(listVar).MaxLength;
		}

		/// <summary>
		/// Compare two domains and two integer representations of values in the domains. The two domains must be of the same kind.
		/// If they are symbol domains, they must also be the same domain object.
		/// </summary>
		/// <returns>negative value if v1 less than v2; 0 if v1 = v2; positive value if v1 greater than v2</returns>
		internal static int CompareCspValues(CspDomain domain1, int ival1, CspDomain domain2, int ival2)
		{
			CspSolverDomain cspSolverDomain = AsCspSolverDomain(domain1);
			CspSolverDomain cspSolverDomain2 = AsCspSolverDomain(domain2);
			switch (cspSolverDomain.Kind)
			{
			case CspDomain.ValueKind.Integer:
			case CspDomain.ValueKind.Symbol:
				if (cspSolverDomain.Kind == CspDomain.ValueKind.Symbol && domain1 != domain2)
				{
					throw new ArgumentException(Resources.SetListSymbolSetVarNotAllowed);
				}
				if (ival1 < ival2)
				{
					return -1;
				}
				if (ival1 == ival2)
				{
					return 0;
				}
				return 1;
			case CspDomain.ValueKind.Decimal:
			{
				int scale = cspSolverDomain.Scale;
				int scale2 = cspSolverDomain2.Scale;
				long num = (long)ival1 * (long)scale2;
				long num2 = (long)ival2 * (long)scale;
				if (num == num2)
				{
					return 0;
				}
				if (num < num2)
				{
					return -1;
				}
				return 1;
			}
			default:
				throw new ArgumentException(Resources.UnknownDomainType);
			}
		}

		internal static object GetSetListVarValue(CspCompositeVariable var)
		{
			return GetSetListVarValue(var, null);
		}

		internal static object GetSetListVarValue(CspCompositeVariable var, Dictionary<CspTerm, CspTerm> termMap)
		{
			bool flag = IsSetVariable(var);
			IsListVariable(var);
			CspSolverDomain cspSolverDomain;
			CspVariable cspVariable;
			CspDomain.ValueKind kind;
			CspTerm[] array;
			if (flag)
			{
				CspPowerSet cspPowerSet = var.DomainComposite as CspPowerSet;
				cspSolverDomain = cspPowerSet.Baseline as CspSolverDomain;
				cspVariable = var.FieldInternal("cardinality", 0) as CspVariable;
				kind = cspPowerSet.Kind;
				array = var.FieldsInternal("set");
			}
			else
			{
				CspPowerList cspPowerList = var.DomainComposite as CspPowerList;
				cspSolverDomain = cspPowerList.Baseline as CspSolverDomain;
				cspVariable = var.FieldInternal("length", 0) as CspVariable;
				kind = cspPowerList.Kind;
				array = var.FieldsInternal("list");
			}
			if (termMap != null)
			{
				CspTerm[] array2 = array;
				if (!termMap.TryGetValue(cspVariable, out var value))
				{
					throw new ArgumentException(Resources.UnknownVariable + var.ToString());
				}
				cspVariable = value as CspVariable;
				array = new CspTerm[array2.Length];
				for (int i = 0; i < array2.Length; i++)
				{
					if (!termMap.TryGetValue(array2[i], out array[i]))
					{
						throw new ArgumentException(Resources.UnknownVariable + var.ToString());
					}
				}
			}
			if ((int)cspVariable.GetValue() == 0)
			{
				switch (kind)
				{
				case CspDomain.ValueKind.Integer:
					return new int[0];
				case CspDomain.ValueKind.Decimal:
					return new double[0];
				case CspDomain.ValueKind.Symbol:
					return new string[0];
				default:
					throw new ArgumentException(Resources.InvalidValueType);
				}
			}
			int num = (int)cspVariable.GetValue();
			int num2 = array.Length;
			if (flag)
			{
				int num3 = 0;
				switch (kind)
				{
				case CspDomain.ValueKind.Integer:
				{
					int[] array5 = new int[num];
					for (int l = 0; l < num2; l++)
					{
						if (IsTrue(array[l]))
						{
							array5[num3++] = cspSolverDomain[l];
						}
					}
					return array5;
				}
				case CspDomain.ValueKind.Decimal:
				{
					double[] array4 = new double[num];
					for (int k = 0; k < num2; k++)
					{
						if (IsTrue(array[k]))
						{
							array4[num3++] = GetDecimalValue(cspSolverDomain, cspSolverDomain[k]);
						}
					}
					return array4;
				}
				case CspDomain.ValueKind.Symbol:
				{
					string[] array3 = new string[num];
					for (int j = 0; j < num2; j++)
					{
						if (IsTrue(array[j]))
						{
							array3[num3++] = GetSymbolValue(cspSolverDomain, cspSolverDomain[j]);
						}
					}
					return array3;
				}
				default:
					throw new ArgumentException(Resources.InvalidValueType);
				}
			}
			switch (kind)
			{
			case CspDomain.ValueKind.Integer:
			{
				int[] array8 = new int[num];
				for (int num4 = 0; num4 < num; num4++)
				{
					array8[num4] = (int)(array[num4] as CspVariable).GetValue();
				}
				return array8;
			}
			case CspDomain.ValueKind.Decimal:
			{
				double[] array7 = new double[num];
				for (int n = 0; n < num; n++)
				{
					array7[n] = (double)(array[n] as CspVariable).GetValue();
				}
				return array7;
			}
			case CspDomain.ValueKind.Symbol:
			{
				string[] array6 = new string[num];
				for (int m = 0; m < num; m++)
				{
					array6[m] = (string)(array[m] as CspVariable).GetValue();
				}
				return array6;
			}
			default:
				throw new ArgumentException(Resources.InvalidValueType);
			}
		}

		/// <summary>
		/// Compute the union of the two input domains
		/// </summary>
		internal static CspDomain DomainUnion(ConstraintSystem solver, CspDomain domain1, CspDomain domain2)
		{
			if (domain1 == domain2)
			{
				return domain1;
			}
			if (domain1.Count == 0)
			{
				return domain2;
			}
			if (domain2.Count == 0)
			{
				return domain1;
			}
			CspIntervalDomain intervalD = domain1 as CspIntervalDomain;
			CspIntervalDomain intervalD2 = domain2 as CspIntervalDomain;
			CspSetDomain setD = domain1 as CspSetDomain;
			CspSetDomain setD2 = domain2 as CspSetDomain;
			switch (domain1.Kind)
			{
			case CspDomain.ValueKind.Integer:
				return IntegerDomainUnion(solver, intervalD, intervalD2, setD, setD2);
			case CspDomain.ValueKind.Decimal:
			{
				int scale = (domain1 as CspSolverDomain).Scale;
				int scale2 = (domain2 as CspSolverDomain).Scale;
				return DecimalDomainUnion(solver, intervalD, intervalD2, setD, setD2, scale, scale2);
			}
			default:
				throw new ArgumentException(Resources.NoDomainValueKindSpecified);
			}
		}

		private static CspDomain IntegerDomainUnion(ConstraintSystem solver, CspIntervalDomain intervalD1, CspIntervalDomain intervalD2, CspSetDomain setD1, CspSetDomain setD2)
		{
			int count = 0;
			if (intervalD1 != null && intervalD2 != null)
			{
				if (intervalD1.First <= intervalD2.First && intervalD2.Last <= intervalD1.Last)
				{
					return intervalD1;
				}
				if (intervalD2.First <= intervalD1.First && intervalD1.Last <= intervalD2.Last)
				{
					return intervalD2;
				}
				int first = ((intervalD1.First <= intervalD2.First) ? intervalD1.First : intervalD2.First);
				int last = ((intervalD1.Last >= intervalD2.Last) ? intervalD1.Last : intervalD2.Last);
				return solver.CreateIntegerInterval(first, last);
			}
			int[] array;
			if (intervalD1 != null || intervalD2 != null)
			{
				bool flag;
				int first2;
				int last2;
				int[] set;
				int num;
				if (intervalD1 != null)
				{
					flag = true;
					first2 = intervalD1.First;
					last2 = intervalD1.Last;
					set = setD2.Set;
					num = intervalD1.Count + setD2.Count;
				}
				else
				{
					flag = false;
					first2 = intervalD2.First;
					last2 = intervalD2.Last;
					set = setD1.Set;
					num = intervalD2.Count + setD1.Count;
				}
				if (first2 <= set[0] && set[set.Length - 1] <= last2)
				{
					if (flag)
					{
						return intervalD1;
					}
					return intervalD2;
				}
				array = new int[num];
				int i;
				for (i = 0; i < set.Length && set[i] < first2; i++)
				{
					array[count++] = set[i];
				}
				for (int j = first2; j <= last2; j++)
				{
					array[count++] = j;
				}
				for (; i < set.Length && set[i] <= last2; i++)
				{
				}
				for (; i < set.Length; i++)
				{
					array[count++] = set[i];
				}
			}
			else
			{
				int k = 0;
				int l = 0;
				int[] set2 = setD1.Set;
				int[] set3 = setD2.Set;
				array = new int[set2.Length + set3.Length];
				bool flag2 = true;
				bool flag3 = true;
				while (k < set2.Length && l < set3.Length)
				{
					if (set2[k] == set3[l])
					{
						array[count++] = set2[k];
						k++;
						l++;
					}
					else if (set2[k] < set3[l])
					{
						flag2 = false;
						array[count++] = set2[k];
						k++;
					}
					else
					{
						flag3 = false;
						array[count++] = set3[l];
						l++;
					}
				}
				if (flag2 && k == set2.Length)
				{
					return setD2;
				}
				if (flag3 && l == set3.Length)
				{
					return setD1;
				}
				for (; k < set2.Length; k++)
				{
					array[count++] = set2[k];
				}
				for (; l < set3.Length; l++)
				{
					array[count++] = set3[l];
				}
			}
			return CspSetDomain.Create(array, 0, count);
		}

		private static CspDomain DecimalDomainUnion(ConstraintSystem solver, CspIntervalDomain intervalD1, CspIntervalDomain intervalD2, CspSetDomain setD1, CspSetDomain setD2, int scale1, int scale2)
		{
			int precision = ((scale1 >= scale2) ? scale1 : scale2);
			int count = 0;
			if (intervalD1 != null && intervalD2 != null)
			{
				double num = (double)intervalD1.First / (double)scale1;
				double num2 = (double)intervalD2.First / (double)scale2;
				double num3 = (double)intervalD1.Last / (double)scale1;
				double num4 = (double)intervalD2.Last / (double)scale2;
				long num5 = (long)intervalD1.First * (long)scale2;
				long num6 = (long)intervalD2.First * (long)scale1;
				long num7 = (long)intervalD1.Last * (long)scale2;
				long num8 = (long)intervalD2.Last * (long)scale1;
				if (num5 <= num6 && num8 <= num7 && scale1 == scale2)
				{
					return intervalD1;
				}
				if (num6 <= num5 && num7 <= num8 && scale1 == scale2)
				{
					return intervalD2;
				}
				double first = ((num5 <= num6) ? num : num2);
				double last = ((num7 >= num8) ? num3 : num4);
				return solver.CreateDecimalInterval(precision, first, last);
			}
			double[] array;
			if (intervalD1 != null || intervalD2 != null)
			{
				bool flag;
				int first2;
				int last2;
				int num9;
				int num10;
				int[] set;
				int num11;
				if (intervalD1 != null)
				{
					flag = true;
					first2 = intervalD1.First;
					last2 = intervalD1.Last;
					num9 = scale1;
					num10 = scale2;
					set = setD2.Set;
					num11 = intervalD1.Count + setD2.Count;
				}
				else
				{
					flag = false;
					first2 = intervalD2.First;
					last2 = intervalD2.Last;
					num9 = scale2;
					num10 = scale1;
					set = setD1.Set;
					num11 = intervalD2.Count + setD1.Count;
				}
				long num12 = (long)first2 * (long)num10;
				long num13 = (long)set[0] * (long)num9;
				long num14 = (long)last2 * (long)num10;
				long num15 = (long)set[set.Length - 1] * (long)num9;
				if (num12 <= num13 && num15 <= num14 && scale1 == scale2)
				{
					if (flag)
					{
						return intervalD1;
					}
					return intervalD2;
				}
				array = new double[num11];
				int i;
				for (i = 0; i < set.Length && (long)set[i] * (long)num9 < num12; i++)
				{
					array[count++] = (double)set[i] / (double)num10;
				}
				for (int j = first2; j <= last2; j++)
				{
					array[count++] = (double)j / (double)num9;
				}
				for (; i < set.Length && (long)set[i] * (long)num9 <= num14; i++)
				{
				}
				for (; i < set.Length; i++)
				{
					array[count++] = (double)set[i] / (double)num10;
				}
			}
			else
			{
				int k = 0;
				int l = 0;
				int[] set2 = setD1.Set;
				int[] set3 = setD2.Set;
				array = new double[set2.Length + set3.Length];
				bool flag2 = true;
				bool flag3 = true;
				while (k < set2.Length && l < set3.Length)
				{
					double num16 = (double)set2[k] / (double)scale1;
					double num17 = (double)set3[l] / (double)scale2;
					long num18 = (long)set2[k] * (long)scale2;
					long num19 = (long)set3[l] * (long)scale1;
					if (num18 == num19)
					{
						array[count++] = num16;
						k++;
						l++;
					}
					else if (num18 < num19)
					{
						flag2 = false;
						array[count++] = num16;
						k++;
					}
					else
					{
						flag3 = false;
						array[count++] = num17;
						l++;
					}
				}
				if (flag2 && k == set2.Length)
				{
					return setD2;
				}
				if (flag3 && l == set3.Length)
				{
					return setD1;
				}
				for (; k < set2.Length; k++)
				{
					array[count++] = (double)set2[k] / (double)scale1;
				}
				for (; l < set3.Length; l++)
				{
					array[count++] = (double)set3[l] / (double)scale2;
				}
			}
			return CspSetDomain.Create(precision, array, 0, count);
		}
	}
}
