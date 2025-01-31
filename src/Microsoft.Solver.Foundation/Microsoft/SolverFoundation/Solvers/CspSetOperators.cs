using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Helper class to model all set operators
	/// </summary>
	public static class CspSetOperators
	{
		/// <summary>
		/// Create a CspPowerSet, which works as a power-set domain of the baseline
		/// </summary>
		public static CspPowerSet CreatePowerSet(this ConstraintSystem solver, object key, CspDomain baseline)
		{
			if (solver == null || baseline == null)
			{
				throw new ArgumentNullException(Resources.NullInput);
			}
			return new CspPowerSet(solver, key, baseline);
		}

		/// <summary>
		/// Create a constant integer set
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="subset">An array of unique integers</param>
		/// <returns>Term representing the constant interger set</returns>
		public static CspTerm ConstantIntegerSet(this ConstraintSystem solver, params int[] subset)
		{
			if (solver == null || subset == null)
			{
				throw new ArgumentNullException(Resources.SetListNullInput);
			}
			CspSetListHelper.NormalizeConstantSet(subset);
			if (solver._setConstants.TryGetValue(subset, out var value))
			{
				return value;
			}
			CspDomain baseline = solver.CreateIntegerSet(subset);
			CspPowerSet domain = solver.CreatePowerSet(null, baseline);
			value = (CspCompositeVariable)solver.CreateVariable(domain);
			if (subset.Length != 0)
			{
				CspTerm[] array = value.FieldsInternal("set");
				CspTerm cspTerm = value.FieldInternal("cardinality", 0);
				for (int i = 0; i < array.Length; i++)
				{
					solver.AddConstraints(array[i]);
				}
				solver.AddConstraints(solver.Equal(subset.Length, cspTerm));
			}
			solver._setConstants.Add(subset, value);
			return value;
		}

		/// <summary>
		/// Create a constant decimal set using the given scale
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="subset">An array of unique decimals</param>
		/// <returns>Term representing the constant decimal set</returns>
		public static CspTerm ConstantDecimalSet(this ConstraintSystem solver, params double[] subset)
		{
			if (solver == null || subset == null)
			{
				throw new ArgumentNullException(Resources.SetListNullInput);
			}
			CspSetListHelper.NormalizeConstantSet(solver.Precision, subset);
			if (solver._setConstants.TryGetValue(subset, out var value))
			{
				solver._setConstants.Add(subset, value);
				return value;
			}
			CspDomain baseline = solver.CreateDecimalSet(subset);
			CspPowerSet domain = solver.CreatePowerSet(null, baseline);
			value = (CspCompositeVariable)solver.CreateVariable(domain);
			if (subset.Length != 0)
			{
				CspTerm[] array = value.FieldsInternal("set");
				CspTerm cspTerm = value.FieldInternal("cardinality", 0);
				for (int i = 0; i < array.Length; i++)
				{
					solver.AddConstraints(array[i]);
				}
				solver.AddConstraints(solver.Equal(subset.Length, cspTerm));
			}
			solver._setConstants.Add(subset, value);
			return value;
		}

		/// <summary>
		/// Create a constant symbol set
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="symbolDomain">The symbol domain to which the given symbols belong</param>
		/// <param name="subset">An array of unique symbols that belong to the symbol domain (symbols that do not belong to symbolDomain will be discarded silently)</param>
		/// <returns>Term representing the constant symbol set</returns>
		public static CspTerm ConstantSymbolSet(this ConstraintSystem solver, CspDomain symbolDomain, params string[] subset)
		{
			if (solver == null || symbolDomain == null || subset == null)
			{
				throw new ArgumentNullException(Resources.SetListNullInput);
			}
			if (symbolDomain.Kind != CspDomain.ValueKind.Symbol)
			{
				throw new ArgumentException(Resources.InvalidStringConstant);
			}
			if (symbolDomain.Count < subset.Length)
			{
				throw new ArgumentException(Resources.InvalidStringConstant);
			}
			if (solver._setConstants.TryGetValue(subset, out var value))
			{
				solver._setConstants.Add(subset, value);
				return value;
			}
			CspPowerSet domain = solver.CreatePowerSet(null, symbolDomain);
			value = (CspCompositeVariable)solver.CreateVariable(domain);
			CspSetListHelper.GetPowerSetFields(value, out var card, out var set);
			Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
			foreach (string key in subset)
			{
				if (!dictionary.ContainsKey(key))
				{
					dictionary.Add(key, value: true);
				}
			}
			Dictionary<object, int> powerSetValueIndexMap = CspSetListHelper.GetPowerSetValueIndexMap(value);
			foreach (object item in symbolDomain.Values())
			{
				if (dictionary.ContainsKey((string)item))
				{
					solver.AddConstraints(set[powerSetValueIndexMap[item]]);
				}
				else
				{
					solver.AddConstraints(!set[powerSetValueIndexMap[item]]);
				}
			}
			solver.AddConstraints(solver.Equal(subset.Length, card));
			solver._setConstants.Add(subset, value);
			return value;
		}

		/// <summary>
		/// Create a Term that represents setVar1 = setVar2
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="setVar1">The CspPowerSet variable that represents the first set variable</param>
		/// <param name="setVar2">The CspPowerSet variable that represents the second set variable</param>
		/// <returns>Term representing setVar1 = setVar2</returns>
		public static CspTerm SetEqual(this ConstraintSystem solver, CspTerm setVar1, CspTerm setVar2)
		{
			CspSetListHelper.CheckForBadInputs(isCheckingForSets: true, solver, setVar1, setVar2);
			if (setVar1 == setVar2)
			{
				return solver.True;
			}
			CspSolverDomain powerSetListBaseline = CspSetListHelper.GetPowerSetListBaseline(setVar1);
			CspSolverDomain powerSetListBaseline2 = CspSetListHelper.GetPowerSetListBaseline(setVar2);
			CspSetListHelper.GetPowerSetFields(setVar1, out var card, out var set);
			CspSetListHelper.GetPowerSetFields(setVar2, out var card2, out var set2);
			if (powerSetListBaseline.Count == 0 && powerSetListBaseline2.Count == 0)
			{
				return solver.True;
			}
			if (powerSetListBaseline.Count == 0)
			{
				return solver.Equal(0, card2);
			}
			if (powerSetListBaseline2.Count == 0)
			{
				return solver.Equal(0, card);
			}
			List<CspTerm> list = new List<CspTerm>();
			list.Add(solver.Equal(card, card2));
			int i = 0;
			int j = 0;
			while (i < powerSetListBaseline.Count && j < powerSetListBaseline2.Count)
			{
				int num = CspSetListHelper.CompareCspValues(powerSetListBaseline, powerSetListBaseline[i], powerSetListBaseline2, powerSetListBaseline2[j]);
				if (num == 0)
				{
					list.Add(solver.Equal(set[i], set2[j]));
					i++;
					j++;
				}
				else if (num < 0)
				{
					list.Add(!set[i]);
					i++;
				}
				else
				{
					list.Add(!set2[j]);
					j++;
				}
			}
			for (; i < powerSetListBaseline.Count; i++)
			{
				list.Add(!set[i]);
			}
			for (; j < powerSetListBaseline2.Count; j++)
			{
				list.Add(!set2[j]);
			}
			return solver.And(list.ToArray());
		}

		/// <summary>
		/// Create a Term that represents setVar1 = set2
		/// </summary>
		public static CspTerm SetEqual(this ConstraintSystem constraintSolver, CspTerm setVar1, int[] set2)
		{
			return constraintSolver.SetEqual(setVar1, constraintSolver.ConstantIntegerSet(set2));
		}

		/// <summary>
		/// Create a Term that represents set1 = setVar2
		/// </summary>
		public static CspTerm SetEqual(this ConstraintSystem constraintSolver, int[] set1, CspTerm setVar2)
		{
			return constraintSolver.SetEqual(constraintSolver.ConstantIntegerSet(set1), setVar2);
		}

		/// <summary>
		/// Create a Term that represents "val is a member of setVar"
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="valVar">The value</param>
		/// <param name="setVar">The set where the value belongs</param>
		/// <returns>Term representing "val is a member of setVar"</returns>
		public static CspTerm MemberOf(this ConstraintSystem solver, CspTerm valVar, CspTerm setVar)
		{
			if (solver == null || valVar == null || setVar == null)
			{
				throw new ArgumentNullException(Resources.SetListNullInput);
			}
			if (!CspSetListHelper.IsSetVariable(setVar))
			{
				throw new ArgumentException(Resources.SetListWrongDomain);
			}
			if (CspSetListHelper.IsSetVariable(valVar))
			{
				throw new ArgumentException(Resources.SetListWrongDomain);
			}
			CspSolverTerm cspSolverTerm = valVar as CspSolverTerm;
			if (cspSolverTerm.Kind != CspSetListHelper.GetPowerSetDomain(setVar).Kind)
			{
				return !solver.True;
			}
			CspSolverDomain powerSetListBaseline = CspSetListHelper.GetPowerSetListBaseline(setVar);
			if (powerSetListBaseline.Count == 0)
			{
				return !solver.True;
			}
			CspSetListHelper.GetPowerSetFields(setVar, out var _, out var set);
			Dictionary<object, int> powerSetValueIndexMap = CspSetListHelper.GetPowerSetValueIndexMap(setVar);
			CspSolverDomain baseValueSet = cspSolverTerm.BaseValueSet;
			CspTerm[] array = new CspTerm[baseValueSet.Count];
			int num = 0;
			foreach (object item in baseValueSet.Values())
			{
				CspTerm cspTerm = CspSetListHelper.ValueEqual(solver, valVar, item);
				if (powerSetValueIndexMap.ContainsKey(item))
				{
					array[num++] = !cspTerm | set[powerSetValueIndexMap[item]];
				}
				else
				{
					array[num++] = !cspTerm;
				}
			}
			return solver.And(array);
		}

		/// <summary>
		/// Create a Term that represents "val is a member of setVar"
		/// </summary>
		public static CspTerm MemberOf(this ConstraintSystem constraintSolver, int val, CspTerm setVar)
		{
			return constraintSolver.MemberOf(constraintSolver.Constant(val), setVar);
		}

		/// <summary>
		/// Create a Term that represents "valVar is a member of set"
		/// </summary>
		public static CspTerm MemberOf(this ConstraintSystem constraintSolver, CspTerm valVar, int[] set)
		{
			return constraintSolver.MemberOf(valVar, constraintSolver.ConstantIntegerSet(set));
		}

		/// <summary>
		/// Create a Term that represents "setVar1 is a subset of setVar2"
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="setVar1">The first set variable (the subset)</param>
		/// <param name="setVar2">The second set variable (the superset)</param>
		/// <returns>Term representing "setVar1 is a subset of setVar2"</returns>
		public static CspTerm SubsetEq(this ConstraintSystem solver, CspTerm setVar1, CspTerm setVar2)
		{
			CspSetListHelper.CheckForBadInputs(isCheckingForSets: true, solver, setVar1, setVar2);
			CspSolverDomain powerSetListBaseline = CspSetListHelper.GetPowerSetListBaseline(setVar1);
			CspSolverDomain powerSetListBaseline2 = CspSetListHelper.GetPowerSetListBaseline(setVar2);
			if (powerSetListBaseline.Count == 0)
			{
				return solver.True;
			}
			CspSetListHelper.GetPowerSetFields(setVar1, out var card, out var set);
			CspSetListHelper.GetPowerSetFields(setVar2, out var _, out var set2);
			if (powerSetListBaseline2.Count == 0)
			{
				return solver.Equal(0, card);
			}
			Dictionary<object, int> powerSetValueIndexMap = CspSetListHelper.GetPowerSetValueIndexMap(setVar1);
			Dictionary<object, int> powerSetValueIndexMap2 = CspSetListHelper.GetPowerSetValueIndexMap(setVar2);
			CspTerm[] array = new CspTerm[powerSetListBaseline.Count];
			int num = 0;
			foreach (object item in powerSetListBaseline.Values())
			{
				if (powerSetValueIndexMap2.ContainsKey(item))
				{
					array[num++] = !set[powerSetValueIndexMap[item]] | set2[powerSetValueIndexMap2[item]];
				}
				else
				{
					array[num++] = !set[powerSetValueIndexMap[item]];
				}
			}
			return solver.And(array);
		}

		/// <summary>
		/// Create a Term that represents "setVar1 is a subset of set2"
		/// </summary>
		public static CspTerm SubsetEq(this ConstraintSystem constraintSolver, CspTerm setVar1, int[] set2)
		{
			return constraintSolver.SubsetEq(setVar1, constraintSolver.ConstantIntegerSet(set2));
		}

		/// <summary>
		/// Create a Term that represents "set1 is a subset of setVar2"
		/// </summary>
		public static CspTerm SubsetEq(this ConstraintSystem constraintSolver, int[] set1, CspTerm setVar2)
		{
			return constraintSolver.SubsetEq(constraintSolver.ConstantIntegerSet(set1), setVar2);
		}

		/// <summary>
		/// Create a Term that represents "the cardinality of setVar"
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="setVar">The set variable</param>
		/// <returns>Term representing the cardinality of setVar</returns>
		public static CspTerm Cardinality(this ConstraintSystem solver, CspTerm setVar)
		{
			CspSetListHelper.CheckForBadInputs(isCheckingForSets: true, solver, setVar);
			CspSetListHelper.GetPowerSetFields(setVar, out var card, out var _);
			return card;
		}

		/// <summary>
		/// Create a Term that represents "the union of setVar1 and setVar2". Two set variables must have the same value kind.
		/// </summary>
		/// <returns>Term representing the union of setVar1 and setVar2</returns>
		public static CspTerm Union(this ConstraintSystem solver, CspTerm setVar1, CspTerm setVar2)
		{
			CspSetListHelper.CheckForBadInputs(isCheckingForSets: true, solver, setVar1, setVar2);
			CspSolverDomain powerSetListBaseline = CspSetListHelper.GetPowerSetListBaseline(setVar1);
			CspSolverDomain powerSetListBaseline2 = CspSetListHelper.GetPowerSetListBaseline(setVar2);
			CspSetListHelper.CheckForIncompatibleSymbolSets(powerSetListBaseline, powerSetListBaseline2);
			CspSetListHelper.GetPowerSetFields(setVar1, out var _, out var set);
			CspSetListHelper.GetPowerSetFields(setVar2, out var _, out var set2);
			if (powerSetListBaseline.Count == 0)
			{
				return setVar2;
			}
			if (powerSetListBaseline2.Count == 0)
			{
				return setVar1;
			}
			CspDomain cspDomain = CspSetListHelper.DomainUnion(solver, powerSetListBaseline, powerSetListBaseline2);
			CspPowerSet domain = solver.CreatePowerSet(null, cspDomain);
			CspTerm cspTerm = solver.CreateVariable(domain);
			if (cspDomain.Count == 0)
			{
				return cspTerm;
			}
			CspSetListHelper.GetPowerSetFields(cspTerm, out var _, out var set3);
			Dictionary<object, int> powerSetValueIndexMap = CspSetListHelper.GetPowerSetValueIndexMap(setVar1);
			Dictionary<object, int> powerSetValueIndexMap2 = CspSetListHelper.GetPowerSetValueIndexMap(setVar2);
			Dictionary<object, int> powerSetValueIndexMap3 = CspSetListHelper.GetPowerSetValueIndexMap(cspTerm);
			CspTerm[] array = new CspTerm[set3.Length];
			int num = 0;
			foreach (object item in cspDomain.Values())
			{
				bool flag = powerSetValueIndexMap.ContainsKey(item);
				bool flag2 = powerSetValueIndexMap2.ContainsKey(item);
				if (flag && flag2)
				{
					array[num++] = solver.Equal(set3[powerSetValueIndexMap3[item]], set[powerSetValueIndexMap[item]] | set2[powerSetValueIndexMap2[item]]);
				}
				else if (flag)
				{
					array[num++] = solver.Equal(set3[powerSetValueIndexMap3[item]], set[powerSetValueIndexMap[item]]);
				}
				else if (flag2)
				{
					array[num++] = solver.Equal(set3[powerSetValueIndexMap3[item]], set2[powerSetValueIndexMap2[item]]);
				}
				else
				{
					array[num++] = !set3[powerSetValueIndexMap3[item]];
				}
			}
			solver.AddConstraints(array);
			return cspTerm;
		}

		/// <summary>
		/// Create a Term that represents "the intersection of setVar1 and setVar2". Two set variables must have the same value kind.
		/// </summary>
		/// <returns>Term representing the intersection of setVar1 and setVar2</returns>
		public static CspTerm Intersection(this ConstraintSystem solver, CspTerm setVar1, CspTerm setVar2)
		{
			CspSetListHelper.CheckForBadInputs(isCheckingForSets: true, solver, setVar1, setVar2);
			CspSolverDomain powerSetListBaseline = CspSetListHelper.GetPowerSetListBaseline(setVar1);
			CspSolverDomain powerSetListBaseline2 = CspSetListHelper.GetPowerSetListBaseline(setVar2);
			CspSetListHelper.CheckForIncompatibleSymbolSets(powerSetListBaseline, powerSetListBaseline2);
			CspSetListHelper.GetPowerSetFields(setVar1, out var _, out var set);
			CspSetListHelper.GetPowerSetFields(setVar2, out var _, out var set2);
			CspDomain cspDomain = DomainIntersection(solver, powerSetListBaseline, powerSetListBaseline2);
			CspPowerSet domain = solver.CreatePowerSet(null, cspDomain);
			CspTerm cspTerm = solver.CreateVariable(domain);
			if (cspDomain.Count == 0)
			{
				return cspTerm;
			}
			CspSetListHelper.GetPowerSetFields(cspTerm, out var _, out var set3);
			Dictionary<object, int> powerSetValueIndexMap = CspSetListHelper.GetPowerSetValueIndexMap(setVar1);
			Dictionary<object, int> powerSetValueIndexMap2 = CspSetListHelper.GetPowerSetValueIndexMap(setVar2);
			Dictionary<object, int> powerSetValueIndexMap3 = CspSetListHelper.GetPowerSetValueIndexMap(cspTerm);
			CspTerm[] array = new CspTerm[set3.Length];
			int num = 0;
			foreach (object item in cspDomain.Values())
			{
				bool flag = powerSetValueIndexMap.ContainsKey(item);
				bool flag2 = powerSetValueIndexMap2.ContainsKey(item);
				if (flag && flag2)
				{
					array[num++] = solver.Equal(set3[powerSetValueIndexMap3[item]], set[powerSetValueIndexMap[item]] & set2[powerSetValueIndexMap2[item]]);
				}
				else
				{
					array[num++] = !set3[powerSetValueIndexMap3[item]];
				}
			}
			solver.AddConstraints(array);
			return cspTerm;
		}

		/// <summary>
		/// Create a Term that represents "the set difference of setVar1 with setVar2". Two set variables must have the same value kind.
		/// </summary>
		/// <returns>Term representing the set difference of setVar1 with setVar2</returns>
		public static CspTerm Difference(this ConstraintSystem solver, CspTerm setVar1, CspTerm setVar2)
		{
			CspSetListHelper.CheckForBadInputs(isCheckingForSets: true, solver, setVar1, setVar2);
			CspSolverDomain powerSetListBaseline = CspSetListHelper.GetPowerSetListBaseline(setVar1);
			CspSolverDomain powerSetListBaseline2 = CspSetListHelper.GetPowerSetListBaseline(setVar2);
			CspSetListHelper.CheckForIncompatibleSymbolSets(powerSetListBaseline, powerSetListBaseline2);
			CspSetListHelper.GetPowerSetFields(setVar1, out var _, out var set);
			CspSetListHelper.GetPowerSetFields(setVar2, out var _, out var set2);
			if (powerSetListBaseline2.Count == 0)
			{
				return setVar1;
			}
			CspDomain cspDomain = powerSetListBaseline;
			CspPowerSet powerSetDomain = CspSetListHelper.GetPowerSetDomain(setVar1);
			CspTerm cspTerm = solver.CreateVariable(powerSetDomain);
			if (cspDomain.Count == 0)
			{
				return cspTerm;
			}
			CspSetListHelper.GetPowerSetFields(cspTerm, out var _, out var set3);
			Dictionary<object, int> powerSetValueIndexMap = CspSetListHelper.GetPowerSetValueIndexMap(setVar1);
			Dictionary<object, int> powerSetValueIndexMap2 = CspSetListHelper.GetPowerSetValueIndexMap(setVar2);
			Dictionary<object, int> powerSetValueIndexMap3 = CspSetListHelper.GetPowerSetValueIndexMap(cspTerm);
			CspTerm[] array = new CspTerm[set3.Length];
			int num = 0;
			foreach (object item in cspDomain.Values())
			{
				powerSetValueIndexMap.ContainsKey(item);
				if (powerSetValueIndexMap2.ContainsKey(item))
				{
					array[num++] = solver.Equal(set3[powerSetValueIndexMap3[item]], set[powerSetValueIndexMap[item]] & !set2[powerSetValueIndexMap2[item]]);
				}
				else
				{
					array[num++] = solver.Equal(set3[powerSetValueIndexMap3[item]], set[powerSetValueIndexMap[item]]);
				}
			}
			solver.AddConstraints(array);
			return cspTerm;
		}

		private static CspDomain DomainIntersection(ConstraintSystem solver, CspDomain domain1, CspDomain domain2)
		{
			if (domain1 == domain2)
			{
				return domain1;
			}
			if (domain1.Count == 0 || domain2.Count == 0)
			{
				return solver.Empty;
			}
			CspIntervalDomain intervalD = domain1 as CspIntervalDomain;
			CspIntervalDomain intervalD2 = domain2 as CspIntervalDomain;
			CspSetDomain setD = domain1 as CspSetDomain;
			CspSetDomain setD2 = domain2 as CspSetDomain;
			switch (domain1.Kind)
			{
			case CspDomain.ValueKind.Integer:
				return IntegerDomainIntersection(solver, intervalD, intervalD2, setD, setD2);
			case CspDomain.ValueKind.Decimal:
			{
				int scale = (domain1 as CspSolverDomain).Scale;
				int scale2 = (domain2 as CspSolverDomain).Scale;
				return DecimalDomainIntersection(solver, intervalD, intervalD2, setD, setD2, scale, scale2);
			}
			default:
				throw new ArgumentException(Resources.NoDomainValueKindSpecified);
			}
		}

		private static CspDomain IntegerDomainIntersection(ConstraintSystem solver, CspIntervalDomain intervalD1, CspIntervalDomain intervalD2, CspSetDomain setD1, CspSetDomain setD2)
		{
			int num = 0;
			if (intervalD1 != null && intervalD2 != null)
			{
				if (intervalD1.First <= intervalD2.First && intervalD2.Last <= intervalD1.Last)
				{
					return intervalD2;
				}
				if (intervalD2.First <= intervalD1.First && intervalD1.Last <= intervalD2.Last)
				{
					return intervalD1;
				}
				int num2 = ((intervalD1.First >= intervalD2.First) ? intervalD1.First : intervalD2.First);
				int num3 = ((intervalD1.Last <= intervalD2.Last) ? intervalD1.Last : intervalD2.Last);
				if (num2 > num3)
				{
					return solver.Empty;
				}
				return solver.CreateIntegerInterval(num2, num3);
			}
			int[] array;
			if (intervalD1 != null || intervalD2 != null)
			{
				bool flag;
				int first;
				int last;
				int[] set;
				int num4;
				if (intervalD1 != null)
				{
					flag = true;
					first = intervalD1.First;
					last = intervalD1.Last;
					set = setD2.Set;
					num4 = intervalD1.Count + setD2.Count;
				}
				else
				{
					flag = false;
					first = intervalD2.First;
					last = intervalD2.Last;
					set = setD1.Set;
					num4 = intervalD2.Count + setD1.Count;
				}
				if (first <= set[0] && set[set.Length - 1] <= last)
				{
					if (flag)
					{
						return setD2;
					}
					return setD1;
				}
				array = new int[num4];
				int i;
				for (i = 0; i < set.Length && set[i] < first; i++)
				{
				}
				for (; i < set.Length && set[i] <= last; i++)
				{
					array[num++] = set[i];
				}
			}
			else
			{
				int num5 = 0;
				int num6 = 0;
				int[] set2 = setD1.Set;
				int[] set3 = setD2.Set;
				array = new int[set2.Length + set3.Length];
				bool flag2 = true;
				bool flag3 = true;
				while (num5 < set2.Length && num6 < set3.Length)
				{
					if (set2[num5] == set3[num6])
					{
						array[num++] = set2[num5];
						num5++;
						num6++;
					}
					else if (set2[num5] < set3[num6])
					{
						flag2 = false;
						num5++;
					}
					else
					{
						flag3 = false;
						num6++;
					}
				}
				if (flag2 && num5 == set2.Length)
				{
					return setD1;
				}
				if (flag3 && num6 == set3.Length)
				{
					return setD2;
				}
			}
			if (num == 0)
			{
				return solver.Empty;
			}
			return CspSetDomain.Create(array, 0, num);
		}

		private static CspDomain DecimalDomainIntersection(ConstraintSystem solver, CspIntervalDomain intervalD1, CspIntervalDomain intervalD2, CspSetDomain setD1, CspSetDomain setD2, int scale1, int scale2)
		{
			int precision = ((scale1 >= scale2) ? scale1 : scale2);
			int num = 0;
			if (intervalD1 != null && intervalD2 != null)
			{
				double num2 = (double)intervalD1.First / (double)scale1;
				double num3 = (double)intervalD2.First / (double)scale2;
				double num4 = (double)intervalD1.Last / (double)scale1;
				double num5 = (double)intervalD2.Last / (double)scale2;
				long num6 = (long)intervalD1.First * (long)scale2;
				long num7 = (long)intervalD2.First * (long)scale1;
				long num8 = (long)intervalD1.Last * (long)scale2;
				long num9 = (long)intervalD2.Last * (long)scale1;
				if (num6 <= num7 && num9 <= num8 && scale1 == scale2)
				{
					return intervalD2;
				}
				if (num7 <= num6 && num8 <= num9 && scale1 == scale2)
				{
					return intervalD1;
				}
				double num10 = ((num6 >= num7) ? num2 : num3);
				double num11 = ((num8 <= num9) ? num4 : num5);
				if (num10 > num11)
				{
					return solver.Empty;
				}
				return solver.CreateDecimalInterval(precision, num10, num11);
			}
			double[] array;
			if (intervalD1 != null || intervalD2 != null)
			{
				bool flag;
				int first;
				int last;
				int num12;
				int num13;
				int[] set;
				int num14;
				if (intervalD1 != null)
				{
					flag = true;
					first = intervalD1.First;
					last = intervalD1.Last;
					num12 = scale1;
					num13 = scale2;
					set = setD2.Set;
					num14 = intervalD1.Count + setD2.Count;
				}
				else
				{
					flag = false;
					first = intervalD2.First;
					last = intervalD2.Last;
					num12 = scale2;
					num13 = scale1;
					set = setD1.Set;
					num14 = intervalD2.Count + setD1.Count;
				}
				long num15 = (long)first * (long)num13;
				long num16 = (long)set[0] * (long)num12;
				long num17 = (long)last * (long)num13;
				long num18 = (long)set[set.Length - 1] * (long)num12;
				if (num15 <= num16 && num18 <= num17 && scale1 == scale2)
				{
					if (flag)
					{
						return setD2;
					}
					return setD1;
				}
				array = new double[num14];
				int i;
				for (i = 0; i < set.Length && (long)set[i] * (long)num12 < num15; i++)
				{
				}
				for (; i < set.Length && (long)set[i] * (long)num12 <= num17; i++)
				{
					array[num++] = (double)set[i] / (double)num13;
				}
			}
			else
			{
				int num19 = 0;
				int num20 = 0;
				int[] set2 = setD1.Set;
				int[] set3 = setD2.Set;
				array = new double[set2.Length + set3.Length];
				bool flag2 = true;
				bool flag3 = true;
				while (num19 < set2.Length && num20 < set3.Length)
				{
					long num21 = (long)set2[num19] * (long)scale2;
					long num22 = (long)set3[num20] * (long)scale1;
					if (num21 == num22)
					{
						array[num++] = (double)set2[num19] / (double)scale1;
						num19++;
						num20++;
					}
					else if (num21 < num22)
					{
						flag2 = false;
						num19++;
					}
					else
					{
						flag3 = false;
						num20++;
					}
				}
				if (flag2 && num19 == set2.Length)
				{
					return setD1;
				}
				if (flag3 && num20 == set3.Length)
				{
					return setD2;
				}
			}
			if (num == 0)
			{
				return solver.Empty;
			}
			return CspSetDomain.Create(precision, array, 0, num);
		}
	}
}
