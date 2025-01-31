using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Helper class to model all list operators
	/// </summary>
	public static class CspListOperators
	{
		/// <summary>
		/// Create a constant integer list
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="list">An array of integers</param>
		/// <returns>Term representing the constant interger list</returns>
		public static CspTerm ConstantIntegerList(this ConstraintSystem solver, params int[] list)
		{
			if (solver == null || list == null)
			{
				throw new ArgumentNullException(Resources.SetListNullInput);
			}
			if (solver._listConstants.TryGetValue(list, out var value))
			{
				return value;
			}
			CspSetListHelper.NormalizeConstantList(list, out var uniqueOrderedSet);
			CspDomain baseline = solver.CreateIntegerSet(uniqueOrderedSet);
			CspPowerList domain = solver.CreatePowerList(null, baseline, list.Length);
			value = (CspCompositeVariable)solver.CreateVariable(domain);
			CspTerm[] array = value.FieldsInternal("list");
			CspTerm cspTerm = value.FieldInternal("length", 0);
			if (list.Length != 0)
			{
				solver.AddConstraints(solver.Equal(list.Length, cspTerm));
				for (int i = 0; i < list.Length; i++)
				{
					solver.AddConstraints(solver.Equal(list[i], array[i]));
				}
			}
			solver._listConstants.Add(list, value);
			return value;
		}

		/// <summary>
		/// Create a constant decimal list using the given scale
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="list">An array of decimals</param>
		/// <returns>Term representing the constant decimal list</returns>
		public static CspTerm ConstantDecimalList(this ConstraintSystem solver, params double[] list)
		{
			if (solver == null || list == null)
			{
				throw new ArgumentNullException(Resources.SetListNullInput);
			}
			if (solver._listConstants.TryGetValue(list, out var value))
			{
				return value;
			}
			CspSetListHelper.NormalizeConstantList(solver.Precision, list, out var uniqueOrderedSet);
			CspDomain baseline = solver.CreateDecimalSet(uniqueOrderedSet);
			CspPowerList domain = solver.CreatePowerList(null, baseline, list.Length);
			value = (CspCompositeVariable)solver.CreateVariable(domain);
			CspTerm[] array = value.FieldsInternal("list");
			CspTerm cspTerm = value.FieldInternal("length", 0);
			if (list.Length != 0)
			{
				solver.AddConstraints(solver.Equal(list.Length, cspTerm));
				for (int i = 0; i < list.Length; i++)
				{
					solver.AddConstraints(solver.Equal(solver.Constant(list[i]), array[i]));
				}
			}
			solver._listConstants.Add(list, value);
			return value;
		}

		/// <summary>
		/// Create a constant symbol list
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="symbolDomain">The symbol domain to which the given symbols belong</param>
		/// <param name="list">An array of symbols that belong to the symbol domain</param>
		/// <returns>Term representing the constant symbol list</returns>
		public static CspTerm ConstantSymbolList(this ConstraintSystem solver, CspDomain symbolDomain, params string[] list)
		{
			if (solver == null || symbolDomain == null || list == null)
			{
				throw new ArgumentNullException(Resources.SetListNullInput);
			}
			if (symbolDomain.Kind != CspDomain.ValueKind.Symbol)
			{
				throw new ArgumentException(Resources.InvalidStringConstant);
			}
			if (solver._listConstants.TryGetValue(list, out var value))
			{
				return value;
			}
			CspPowerList domain = solver.CreatePowerList(null, symbolDomain, list.Length);
			value = (CspCompositeVariable)solver.CreateVariable(domain);
			CspTerm[] array = value.FieldsInternal("list");
			CspTerm cspTerm = value.FieldInternal("length", 0);
			if (list.Length != 0)
			{
				solver.AddConstraints(solver.Equal(list.Length, cspTerm));
				for (int i = 0; i < list.Length; i++)
				{
					solver.AddConstraints(solver.Equal(solver.Constant(symbolDomain, list[i]), array[i]));
				}
			}
			solver._listConstants.Add(list, value);
			return value;
		}

		/// <summary>
		/// Create a CspPowerList, which works as a power-list domain of the baseline
		/// </summary>
		public static CspPowerList CreatePowerList(this ConstraintSystem solver, object key, CspDomain baseline, int maxLength)
		{
			if (solver == null || baseline == null || maxLength < 0)
			{
				throw new ArgumentNullException(Resources.NullInput);
			}
			return new CspPowerList(solver, key, baseline, maxLength);
		}

		/// <summary>
		/// Create a Term that represents listVar1 = listVar2
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="listVar1">The CspPowerList variable that represents the first list variable</param>
		/// <param name="listVar2">The CspPowerList variable that represents the second list variable</param>
		/// <returns>Term representing listVar1 = listVar2</returns>
		public static CspTerm ListEqual(this ConstraintSystem solver, CspTerm listVar1, CspTerm listVar2)
		{
			CspSetListHelper.CheckForBadInputs(isCheckingForSets: false, solver, listVar1, listVar2);
			if (listVar1 == listVar2)
			{
				return solver.True;
			}
			CspSolverDomain powerSetListBaseline = CspSetListHelper.GetPowerSetListBaseline(listVar1);
			CspSolverDomain powerSetListBaseline2 = CspSetListHelper.GetPowerSetListBaseline(listVar2);
			CspSetListHelper.GetPowerListFields(listVar1, out var length, out var list);
			CspSetListHelper.GetPowerListFields(listVar2, out var length2, out var list2);
			if (powerSetListBaseline.Count == 0 && powerSetListBaseline2.Count == 0)
			{
				return solver.True;
			}
			if (powerSetListBaseline.Count == 0)
			{
				return solver.Equal(0, length2);
			}
			if (powerSetListBaseline2.Count == 0)
			{
				return solver.Equal(0, length);
			}
			int num = ((list.Length <= list2.Length) ? list.Length : list2.Length);
			CspTerm[] array = new CspTerm[num + 1];
			array[0] = solver.Equal(length, length2);
			for (int i = 0; i < num; i++)
			{
				array[i + 1] = solver.Equal(list[i], list2[i]) | ((length <= i) & (length2 <= i));
			}
			return solver.And(array);
		}

		/// <summary>
		/// Create a Term that represents listVar1 = list2
		/// </summary>
		public static CspTerm ListEqual(this ConstraintSystem constraintSolver, CspTerm listVar1, int[] list2)
		{
			return constraintSolver.ListEqual(listVar1, constraintSolver.ConstantIntegerList(list2));
		}

		/// <summary>
		/// Create a Term that represents list1 = listVar2
		/// </summary>
		public static CspTerm ListEqual(this ConstraintSystem constraintSolver, int[] list1, CspTerm listVar2)
		{
			return constraintSolver.ListEqual(listVar2, list1);
		}

		/// <summary>
		/// Create a Term that represents "listVar1 is a sublist of listVar2"
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="listVar1">The first list variable (the sublist)</param>
		/// <param name="listVar2">The second list variable (the superlist)</param>
		/// <returns>Term representing "listVar1 is a sublist of listVar2"</returns>
		public static CspTerm SublistEq(this ConstraintSystem solver, CspTerm listVar1, CspTerm listVar2)
		{
			CspSetListHelper.CheckForBadInputs(isCheckingForSets: false, solver, listVar1, listVar2);
			if (listVar1 == listVar2)
			{
				return solver.True;
			}
			CspSolverDomain powerSetListBaseline = CspSetListHelper.GetPowerSetListBaseline(listVar1);
			if (powerSetListBaseline.Count == 0)
			{
				return solver.True;
			}
			CspSetListHelper.GetPowerListFields(listVar1, out var length, out var list);
			CspSetListHelper.GetPowerListFields(listVar2, out var length2, out var list2);
			CspSolverDomain powerSetListBaseline2 = CspSetListHelper.GetPowerSetListBaseline(listVar2);
			if (powerSetListBaseline2.Count == 0)
			{
				return solver.Equal(0, length);
			}
			int powerListMaxLength = CspSetListHelper.GetPowerListMaxLength(listVar1);
			CspSetListHelper.GetPowerListMaxLength(listVar2);
			CspTerm[] array = new CspTerm[list2.Length];
			for (int i = 0; i < list2.Length; i++)
			{
				int num = list2.Length - i;
				int num2 = powerListMaxLength;
				CspTerm[] array2;
				if (powerListMaxLength > num)
				{
					num2 = num;
					array2 = new CspTerm[num2 + 1];
					array2[num2] = solver.LessEqual(length, solver.Constant(num2));
				}
				else
				{
					array2 = new CspTerm[num2];
				}
				int num3 = i + num2;
				for (int j = i; j < num3; j++)
				{
					array2[j - i] = (j - i >= length) | (j >= length2) | solver.Equal(list[j - i], list2[j]);
				}
				array[i] = solver.And(array2) & (length + i <= length2);
			}
			return solver.Or(array);
		}

		/// <summary>
		/// Create a Term that represents "listVar1 is a subset of list2"
		/// </summary>
		public static CspTerm SublistEq(this ConstraintSystem constraintSolver, CspTerm listVar1, int[] list2)
		{
			return constraintSolver.SublistEq(listVar1, constraintSolver.ConstantIntegerList(list2));
		}

		/// <summary>
		/// Create a Term that represents "list1 is a sublist of listVar2"
		/// </summary>
		public static CspTerm SublistEq(this ConstraintSystem constraintSolver, int[] list1, CspTerm listVar2)
		{
			return constraintSolver.SublistEq(constraintSolver.ConstantIntegerList(list1), listVar2);
		}

		/// <summary>
		/// Create a Term that represents "the length of listVar"
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="listVar">The list variable</param>
		/// <returns>Term representing the length of listVar</returns>
		public static CspTerm Length(this ConstraintSystem solver, CspTerm listVar)
		{
			CspSetListHelper.CheckForBadInputs(isCheckingForSets: false, solver, listVar);
			CspSetListHelper.GetPowerListFields(listVar, out var length, out var _);
			return length;
		}

		/// <summary>
		/// Return Boolean Term that says the element in listVar at position index equals the elementToBe
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="index">Index Term (must be a constant integer Term or have an integer interval from 0 to listVar's power-list domain's MaxLength - 1)</param>
		/// <param name="listVar">The list variable</param>
		/// <param name="elementToBe">The target Term</param>
		/// <returns>A Boolean Term that says the element in listVar at position index equals result</returns>
		public static CspTerm ElementAt(this ConstraintSystem solver, CspTerm index, CspTerm listVar, CspTerm elementToBe)
		{
			CspSetListHelper.CheckForNonscalorInputs(index);
			CspSetListHelper.CheckForBadInputs(isCheckingForSets: false, solver, listVar);
			CspSolverTerm cspSolverTerm = index as CspSolverTerm;
			int powerListMaxLength = CspSetListHelper.GetPowerListMaxLength(listVar);
			if (powerListMaxLength == 0)
			{
				return !solver.True;
			}
			if (cspSolverTerm.Kind != CspDomain.ValueKind.Integer || !(cspSolverTerm.BaseValueSet is CspIntervalDomain))
			{
				throw new ArgumentException(Resources.SetListIncompatibleIndex);
			}
			if (cspSolverTerm.First < 0 || cspSolverTerm.Last > powerListMaxLength - 1)
			{
				throw new ArgumentOutOfRangeException(Resources.SetListIncompatibleIndex);
			}
			if (cspSolverTerm.Count != 1 && cspSolverTerm.Count != powerListMaxLength)
			{
				throw new ArgumentOutOfRangeException(Resources.SetListIncompatibleIndex);
			}
			CspSetListHelper.GetPowerSetListBaseline(listVar);
			CspSetListHelper.GetPowerListFields(listVar, out var length, out var list);
			if (cspSolverTerm.Count == 1)
			{
				return (length > cspSolverTerm.First) & solver.Equal(elementToBe, list[cspSolverTerm.First]);
			}
			return (length > index) & solver.Equal(elementToBe, solver.Index(list, index));
		}

		/// <summary>
		/// Create a Term whose value will be the number of times that element Term appears in listVar
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="element">the element to count</param>
		/// <param name="listVar">the list variable</param>
		/// <returns>A Term whose value will be the number of times that element appears in listVar</returns>
		public static CspTerm ElementCount(this ConstraintSystem solver, CspTerm element, CspTerm listVar)
		{
			CspSetListHelper.CheckForNonscalorInputs(element);
			CspSetListHelper.CheckForBadInputs(isCheckingForSets: false, solver, listVar);
			CspSolverDomain powerSetListBaseline = CspSetListHelper.GetPowerSetListBaseline(listVar);
			if (powerSetListBaseline.Count == 0)
			{
				return solver.Constant(0);
			}
			int powerListMaxLength = CspSetListHelper.GetPowerListMaxLength(listVar);
			CspSetListHelper.GetPowerListFields(listVar, out var length, out var list);
			CspTerm[] array = new CspTerm[powerListMaxLength];
			CspTerm[] array2 = new CspTerm[powerListMaxLength];
			for (int i = 0; i < powerListMaxLength; i++)
			{
				array[i] = i < length;
				array2[i] = solver.Equal(element, list[i]);
			}
			return solver.FilteredSum(array, array2);
		}

		/// <summary>
		/// Create a Boolean Term that says the index (0-based) of the first occurrence of element in listVar is the indexToBe
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="element">The element</param>
		/// <param name="listVar">The list variable</param>
		/// <param name="indexToBe">The target index</param>
		/// <returns>A Boolean Term that says the index (0-based) of the first occurrence of element in listVar is result</returns>
		public static CspTerm FirstOccurrence(this ConstraintSystem solver, CspTerm element, CspTerm listVar, CspTerm indexToBe)
		{
			CspSetListHelper.CheckForNonscalorInputs(element);
			CspSetListHelper.CheckForBadInputs(isCheckingForSets: false, solver, listVar);
			CspSetListHelper.CheckForNonscalorInputs(indexToBe);
			if (indexToBe.Kind != CspDomain.ValueKind.Integer)
			{
				throw new ArgumentException(Resources.SetListOccurrenceResultNotInteger);
			}
			CspSolverDomain powerSetListBaseline = CspSetListHelper.GetPowerSetListBaseline(listVar);
			if (powerSetListBaseline.Count == 0)
			{
				return !solver.True;
			}
			int powerListMaxLength = CspSetListHelper.GetPowerListMaxLength(listVar);
			CspSetListHelper.GetPowerListFields(listVar, out var length, out var list);
			CspTerm[] array = new CspTerm[powerListMaxLength];
			array[0] = !solver.Equal(element, list[0]) & (length > 0);
			for (int i = 1; i < powerListMaxLength; i++)
			{
				array[i] = array[i - 1] & !solver.Equal(element, list[i]) & (length > i);
			}
			CspTerm cspTerm = solver.Sum(array);
			CspTerm cspTerm2 = cspTerm < length;
			CspTerm cspTerm3 = !solver.Equal(0, cspTerm) | solver.Equal(element, list[0]);
			return solver.And(solver.Equal(cspTerm, indexToBe), cspTerm2, cspTerm3);
		}

		/// <summary>
		/// Create a Boolean Term that says the index (0-based) of the last occurrence of element in listVar is the indexToBe
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="element">The element</param>
		/// <param name="listVar">The list variable</param>
		/// <param name="indexToBe">The target index</param>
		/// <returns>A Boolean Term that says the index (0-based) of the last occurrence of element in listVar is result</returns>
		public static CspTerm LastOccurrence(this ConstraintSystem solver, CspTerm element, CspTerm listVar, CspTerm indexToBe)
		{
			CspSetListHelper.CheckForNonscalorInputs(element);
			CspSetListHelper.CheckForBadInputs(isCheckingForSets: false, solver, listVar);
			CspSetListHelper.CheckForNonscalorInputs(indexToBe);
			if (indexToBe.Kind != CspDomain.ValueKind.Integer)
			{
				throw new ArgumentException(Resources.SetListOccurrenceResultNotInteger);
			}
			CspSolverDomain powerSetListBaseline = CspSetListHelper.GetPowerSetListBaseline(listVar);
			if (powerSetListBaseline.Count == 0)
			{
				return !solver.True;
			}
			int powerListMaxLength = CspSetListHelper.GetPowerListMaxLength(listVar);
			CspSetListHelper.GetPowerListFields(listVar, out var length, out var list);
			CspTerm[] array = new CspTerm[powerListMaxLength];
			CspTerm[] array2 = new CspTerm[powerListMaxLength];
			array[powerListMaxLength - 1] = !solver.Equal(element, list[powerListMaxLength - 1]);
			array2[powerListMaxLength - 1] = length > powerListMaxLength - 1;
			for (int num = powerListMaxLength - 2; num >= 0; num--)
			{
				array[num] = array[num + 1] & !solver.Equal(element, list[num]);
				array2[num] = length > num;
			}
			CspTerm[] array3 = new CspTerm[powerListMaxLength];
			for (int num2 = powerListMaxLength - 1; num2 >= 0; num2--)
			{
				array3[num2] = array[num2] & array2[num2];
			}
			CspTerm cspTerm = length - solver.Sum(array3) - 1;
			CspTerm cspTerm2 = cspTerm >= 0;
			CspTerm[] array4 = new CspTerm[powerListMaxLength];
			for (int i = 0; i < powerListMaxLength; i++)
			{
				array4[i] = !solver.Equal(i + 1, length) | solver.Equal(element, list[i]);
			}
			CspTerm cspTerm3 = !solver.Equal(length - 1, cspTerm) | solver.And(array4);
			return solver.And(solver.Equal(indexToBe, cspTerm), cspTerm2, cspTerm3);
		}

		/// <summary>
		/// Create a CspPowerList Term that represents the concatenation of the two input list varaibles, with listVar1 proceeding listVar2
		/// </summary>
		/// <param name="solver">The solver</param>
		/// <param name="listVar1">The first list</param>
		/// <param name="listVar2">The second list</param>
		/// <returns>A CspPowerList Term that represents the concatenation</returns>
		public static CspTerm Concatenation(this ConstraintSystem solver, CspTerm listVar1, CspTerm listVar2)
		{
			CspSetListHelper.CheckForBadInputs(isCheckingForSets: false, solver, listVar1, listVar2);
			CspSolverDomain powerSetListBaseline = CspSetListHelper.GetPowerSetListBaseline(listVar1);
			if (powerSetListBaseline.Count == 0)
			{
				return listVar2;
			}
			CspSolverDomain powerSetListBaseline2 = CspSetListHelper.GetPowerSetListBaseline(listVar2);
			if (powerSetListBaseline2.Count == 0)
			{
				return listVar1;
			}
			CspSetListHelper.CheckForIncompatibleSymbolSets(powerSetListBaseline, powerSetListBaseline2);
			int powerListMaxLength = CspSetListHelper.GetPowerListMaxLength(listVar1);
			int powerListMaxLength2 = CspSetListHelper.GetPowerListMaxLength(listVar2);
			CspSetListHelper.GetPowerListFields(listVar1, out var length, out var list);
			CspSetListHelper.GetPowerListFields(listVar2, out var length2, out var list2);
			CspDomain baseline = CspSetListHelper.DomainUnion(solver, powerSetListBaseline, powerSetListBaseline2);
			CspPowerList domain = solver.CreatePowerList(null, baseline, powerListMaxLength + powerListMaxLength2);
			CspTerm cspTerm = solver.CreateVariable(domain);
			int powerListMaxLength3 = CspSetListHelper.GetPowerListMaxLength(cspTerm);
			CspSetListHelper.GetPowerListFields(cspTerm, out var length3, out var list3);
			solver.AddConstraints(solver.Equal(length3, length + length2));
			for (int i = 0; i < powerListMaxLength; i++)
			{
				solver.AddConstraints((i >= length) | solver.Equal(list3[i], list[i]));
			}
			CspDomain domain2 = solver.CreateIntegerInterval(0, powerListMaxLength2 - 1);
			for (int j = 0; j < powerListMaxLength3; j++)
			{
				CspTerm cspTerm2 = solver.CreateVariable(domain2);
				solver.AddConstraints((j < length) | (j >= length + length2) | (solver.Equal(cspTerm2, j - length) & solver.Equal(list3[j], solver.Index(list2, cspTerm2))));
				solver.AddConstraints(((j >= length) & (j < length + length2)) | solver.Equal(0, cspTerm2));
			}
			return cspTerm;
		}
	}
}
