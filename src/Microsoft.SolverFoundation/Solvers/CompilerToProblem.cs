using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   An algorithm that creates a Core.Problem from the
	///   syntactical description of the terms
	/// </summary>
	internal class CompilerToProblem
	{
		private Problem _problem;

		private TypeSwitch _switch;

		private Dictionary<BooleanVariable, IntegerVariable> _asInts;

		private Dictionary<IntegerVariable, BooleanVariable> _asBools;

		/// <summary>
		///   whenever a term x = i is created we cache it. This allows
		///   two optimizations: (1) obviously sharing a variable
		///   representing the same logical expression; (2) we can
		///   optimize the compilation of these equalities. 
		///   This way we feel free to use and abuse them within the
		///   compilation process.
		/// </summary>
		private Dictionary<IntegerVariable, Dictionary<long, BooleanVariable>> _variableValuePairs;

		/// <summary>
		///    Main static function to use for compilation
		/// </summary>
		public static Problem Apply(IntegerSolver solver, CspSearchStrategy strategy, IEnumerable<DisolverTerm> allTerms, IEnumerable<DisolverTerm> constraints, IEnumerable<DisolverIntegerTerm> objectives, IEnumerable<DisolverIntegerTerm> allInts, IEnumerable<DisolverBooleanTerm> allBools)
		{
			CompilerToProblem compilerToProblem = new CompilerToProblem(solver, strategy, constraints, objectives);
			compilerToProblem.CompileBooleanVariables(allBools);
			compilerToProblem.CompileIntegerVariables(allInts);
			compilerToProblem.CompileConstraints(allTerms);
			compilerToProblem.CompileVariableValuePairs();
			compilerToProblem._problem.EndCompilation();
			return compilerToProblem._problem;
		}

		private CompilerToProblem(IntegerSolver solver, CspSearchStrategy strategy, IEnumerable<DisolverTerm> constraints, IEnumerable<DisolverIntegerTerm> objectives)
		{
			InitializeSwitch();
			bool useExplanations = false;
			switch (strategy.Variables)
			{
			case VariableEnumerationStrategy.Vsids:
				useExplanations = true;
				break;
			case VariableEnumerationStrategy.ConfLex:
				useExplanations = true;
				break;
			}
			_problem = new Problem(solver, this, useExplanations);
			_asBools = new Dictionary<IntegerVariable, BooleanVariable>();
			_asInts = new Dictionary<BooleanVariable, IntegerVariable>();
			_variableValuePairs = new Dictionary<IntegerVariable, Dictionary<long, BooleanVariable>>();
			SimpleAnalyser.Apply(constraints);
		}

		/// <summary>
		///   Create the dictionary associating actions 
		///   to each type of term
		/// </summary>
		private void InitializeSwitch()
		{
			_switch = new TypeSwitch(delegate
			{
			});
			_switch.Match<DisolverNot>(ProcessNot);
			_switch.Match<DisolverAnd>(ProcessAnd);
			_switch.Match<DisolverOr>(ProcessOr);
			_switch.Match<DisolverEqual>(ProcessEqual);
			_switch.Match<DisolverDifferent>(ProcessDifferent);
			_switch.Match<DisolverLessEqual>(ProcessLessEqual);
			_switch.Match<DisolverUnaryMinus>(ProcessUnaryMinus);
			_switch.Match<DisolverSum>(ProcessSum);
			_switch.Match<DisolverProduct>(ProcessProduct);
			_switch.Match<DisolverIndex>(ProcessIndex);
			_switch.Match<DisolverMatrixIndex>(ProcessMatrixIndex);
			_switch.Match<DisolverAbs>(ProcessAbs);
			_switch.Match<DisolverSquare>(ProcessSquare);
			_switch.Match<DisolverPositiveTableTerm>(ProcessPositiveTable);
			_switch.Match<DisolverMin>(ProcessMin);
			_switch.Match<DisolverMax>(ProcessMax);
			_switch.Match<DisolverIfThenElse>(ProcessIfThenElse);
			_switch.Match<DisolverMember>(ProcessMember);
			_switch.Match<DisolverBooleanAsInteger>(ProcessBooleanAsInteger);
		}

		/// <summary>
		///   Associates a BooleanVariable to every Boolean Term
		/// </summary>
		private void CompileBooleanVariables(IEnumerable<DisolverBooleanTerm> allBools)
		{
			foreach (DisolverBooleanTerm allBool in allBools)
			{
				_problem.CreateBooleanVariable(allBool);
			}
		}

		/// <summary>
		///   Compiles constraints for each term.
		///   These constraints will connect the variables representing
		///   the term and the variables representing its subterms
		/// </summary>
		private void CompileConstraints(IEnumerable<DisolverTerm> allTerms)
		{
			foreach (DisolverTerm allTerm in allTerms)
			{
				_switch.Apply(allTerm);
			}
		}

		/// <summary>
		///   Associates an IntegerVariable to every Integer Term
		/// </summary>
		private void CompileIntegerVariables(IEnumerable<DisolverIntegerTerm> allInts)
		{
			foreach (DisolverIntegerTerm allInt in allInts)
			{
				_problem.CreateIntegerVariable(allInt);
			}
		}

		/// <summary>
		///   Does the final compiling steps needed for the 
		///   variable/value pairs created and cached during the 
		///   compilation process.
		/// </summary>
		private void CompileVariableValuePairs()
		{
			List<long> list = new List<long>();
			foreach (KeyValuePair<IntegerVariable, Dictionary<long, BooleanVariable>> variableValuePair in _variableValuePairs)
			{
				IntegerVariable key = variableValuePair.Key;
				Dictionary<long, BooleanVariable> value = variableValuePair.Value;
				list.Clear();
				foreach (KeyValuePair<long, BooleanVariable> item in value)
				{
					list.Add(item.Key);
				}
				list.Sort();
				long[] array = list.ToArray();
				if (array.Length <= 2)
				{
					foreach (long num in array)
					{
						_problem.AddConstraint(new ReifiedEquality(_problem, key, _problem.GetIntegerConstant(num), value[num]));
					}
					continue;
				}
				BooleanVariable[] array2 = new BooleanVariable[array.Length];
				for (int j = 0; j < array.Length; j++)
				{
					long key2 = array[j];
					array2[j] = value[key2];
				}
				_problem.AddConstraint(new ReifiedValueSet(_problem, key, array, array2));
			}
		}

		protected void ProcessAbs(DisolverAbs t)
		{
			IntegerVariable @int = GetInt(t.SubTerms[0]);
			_problem.AddConstraint(new AbsoluteValue(_problem, @int, GetInt(t)));
		}

		protected void ProcessBooleanAsInteger(DisolverBooleanAsInteger t)
		{
			BooleanVariable @bool = GetBool(t.SubTerms[0]);
			IntegerVariable @int = GetInt(t);
			_problem.AddConstraint(new IntegerBooleanEquivalence(_problem, @int, @bool));
		}

		protected void ProcessIfThenElse(DisolverIfThenElse t)
		{
			CspTerm[] subTerms = t.SubTerms;
			BooleanVariable @bool = GetBool(subTerms[0]);
			IntegerVariable @int = GetInt(subTerms[1]);
			IntegerVariable int2 = GetInt(subTerms[2]);
			IntegerVariable int3 = GetInt(t);
			BooleanVariable equality = GetEquality(int3, @int);
			BooleanVariable equality2 = GetEquality(int3, int2);
			BooleanVariable booleanVariable = _problem.CreateInternalBooleanVariable();
			_problem.AddConstraint(new Negation(_problem, @bool, booleanVariable));
			_problem.AddConstraint(new Implication(_problem, @bool, equality));
			_problem.AddConstraint(new Implication(_problem, booleanVariable, equality2));
			_problem.AddConstraint(new SetMembership(_problem, int3, new IntegerVariable[2] { @int, int2 }));
		}

		protected void ProcessMin(DisolverMin t)
		{
			IntegerVariable @int = GetInt(t);
			IntegerVariable[] intArray = GetIntArray(t.SubTerms);
			for (int num = intArray.Length - 1; num >= 0; num--)
			{
				_problem.AddConstraint(new LessOrEqual(_problem, @int, intArray[num]));
			}
			_problem.AddConstraint(new SetMembership(_problem, @int, intArray));
		}

		protected void ProcessMax(DisolverMax t)
		{
			IntegerVariable @int = GetInt(t);
			IntegerVariable[] intArray = GetIntArray(t.SubTerms);
			for (int num = intArray.Length - 1; num >= 0; num--)
			{
				_problem.AddConstraint(new LessOrEqual(_problem, intArray[num], @int));
			}
			_problem.AddConstraint(new SetMembership(_problem, @int, intArray));
		}

		protected void ProcessMember(DisolverMember t)
		{
			IntegerVariable @int = GetInt(t.SubTerms[0]);
			if (t.InitialStatus == BooleanVariableState.True)
			{
				if (t._values is SparseDomain)
				{
					_problem.AddConstraint(new Member(_problem, @int, t._values as SparseDomain));
					return;
				}
				ConvexDomain convexDomain = t._values as ConvexDomain;
				long cst = convexDomain.First;
				long cst2 = convexDomain.Last;
				_problem.AddConstraint(new LessOrEqual(_problem, _problem.GetIntegerConstant(cst), @int));
				_problem.AddConstraint(new LessOrEqual(_problem, @int, _problem.GetIntegerConstant(cst2)));
			}
			else if (t._values is SparseDomain)
			{
				SparseDomain sparseDomain = t._values as SparseDomain;
				long num = sparseDomain.Count;
				BooleanVariable[] array = new BooleanVariable[num];
				int num2 = 0;
				foreach (int item in sparseDomain.Forward())
				{
					long i = item;
					array[num2] = GetEquality(@int, i);
					num2++;
				}
				CompileBooleanSumGreaterEqual(array, 1, GetBool(t));
			}
			else
			{
				ConvexDomain convexDomain2 = t._values as ConvexDomain;
				long cst3 = convexDomain2.First;
				long cst4 = convexDomain2.Last;
				BooleanVariable booleanVariable = _problem.CreateInternalBooleanVariable();
				BooleanVariable booleanVariable2 = _problem.CreateInternalBooleanVariable();
				_problem.AddConstraint(new ReifiedLessEqual(_problem, _problem.GetIntegerConstant(cst3), @int, booleanVariable));
				_problem.AddConstraint(new ReifiedLessEqual(_problem, @int, _problem.GetIntegerConstant(cst4), booleanVariable2));
				_problem.AddConstraint(new ReifiedConjunction(_problem, booleanVariable, booleanVariable2, GetBool(t)));
			}
		}

		protected void ProcessAnd(DisolverAnd t)
		{
			int num = t.SubTerms.Length;
			if (t.InitialStatus == BooleanVariableState.True)
			{
				return;
			}
			if (num <= 4)
			{
				BooleanVariable x = GetBool(t.SubTerms[0]);
				for (int i = 1; i < num; i++)
				{
					BooleanVariable @bool = GetBool(t.SubTerms[i]);
					BooleanVariable booleanVariable = ((i == num - 1) ? GetBool(t) : _problem.CreateInternalBooleanVariable());
					_problem.AddConstraint(new ReifiedConjunction(_problem, x, @bool, booleanVariable));
					x = booleanVariable;
				}
			}
			else
			{
				BooleanVariable[] boolArray = GetBoolArray(t.SubTerms);
				CompileBooleanSumEquals(boolArray, boolArray.Length, GetBool(t));
			}
		}

		protected void ProcessOr(DisolverOr t)
		{
			CspTerm[] subTerms = t.SubTerms;
			int num = subTerms.Length;
			if (t.InitialStatus == BooleanVariableState.False)
			{
				return;
			}
			if (t.InitialStatus == BooleanVariableState.True)
			{
				if (num == 2)
				{
					_problem.AddConstraint(new Disjunction(_problem, GetBool(subTerms[0]), GetBool(subTerms[1])));
				}
				else
				{
					_problem.AddConstraint(new LargeClause(_problem, GetBoolArray(subTerms)));
				}
			}
			else if (num <= 4)
			{
				BooleanVariable x = GetBool(subTerms[0]);
				for (int i = 1; i < num; i++)
				{
					BooleanVariable @bool = GetBool(subTerms[i]);
					BooleanVariable booleanVariable = ((i == num - 1) ? GetBool(t) : _problem.CreateInternalBooleanVariable());
					_problem.AddConstraint(new ReifiedDisjunction(_problem, x, @bool, booleanVariable));
					x = booleanVariable;
				}
			}
			else
			{
				BooleanVariable[] boolArray = GetBoolArray(subTerms);
				CompileBooleanSumGreaterEqual(boolArray, 1, GetBool(t));
			}
		}

		protected void ProcessPositiveTable(DisolverPositiveTableTerm t)
		{
			int[][] table = t._table;
			IntegerVariable[] intArray = GetIntArray(t._vars);
			if (t.InitialStatus == BooleanVariableState.True)
			{
				Set<int> set = new Set<int>();
				for (int i = 0; i < intArray.Length; i++)
				{
					IntegerVariable integerVariable = intArray[i];
					set.Clear();
					long num = long.MaxValue;
					long num2 = long.MinValue;
					for (int num3 = table.Length - 1; num3 >= 0; num3--)
					{
						int num4 = table[num3][i];
						set.Add(num4);
						num = Math.Min(num, num4);
						num2 = Math.Max(num2, num4);
					}
					if (num != integerVariable.LowerBound || num2 != integerVariable.UpperBound || num2 - num + 1 != set.Cardinality)
					{
						SparseDomain set2 = new SparseDomain(set);
						_problem.AddConstraint(new Member(_problem, integerVariable, set2));
					}
				}
				_problem.AddConstraint(new PositiveTable(_problem, intArray, table));
				return;
			}
			throw new NotImplementedException("Tables occurring in Boolean combinations are not allowed yet");
		}

		protected void ProcessDifferent(DisolverDifferent t)
		{
			int num = t.SubTerms.Length;
			if (t.InitialStatus == BooleanVariableState.True)
			{
				for (int i = 0; i < num; i++)
				{
					for (int j = i + 1; j < num; j++)
					{
						if (t.SubTerms[i] == t.SubTerms[j])
						{
							_problem.AddFalsity();
							return;
						}
					}
				}
				if (num <= 4)
				{
					for (int k = 0; k < num; k++)
					{
						for (int l = k + 1; l < num; l++)
						{
							_problem.AddConstraint(new Difference(_problem, GetInt(t.SubTerms[k]), GetInt(t.SubTerms[l])));
						}
					}
					return;
				}
				IntegerVariable[] intArray = GetIntArray(t.SubTerms);
				bool flag = false;
				IntegerVariable[] array = intArray;
				foreach (IntegerVariable integerVariable in array)
				{
					if (integerVariable.DomainSize >= 10 * num)
					{
						flag = true;
						break;
					}
				}
				_problem.AddConstraint(flag ? ((NaryConstraint<IntegerVariable>)new AllDifferentLight(_problem, intArray)) : ((NaryConstraint<IntegerVariable>)new AllDifferentLazy(_problem, intArray)));
				return;
			}
			if (num == 2)
			{
				BooleanVariable equality = GetEquality(GetInt(t.SubTerms[0]), GetInt(t.SubTerms[1]));
				BooleanVariable @bool = GetBool(t);
				_problem.AddConstraint(new Negation(_problem, equality, @bool));
				return;
			}
			IntegerVariable[] intArray2 = GetIntArray(t.SubTerms);
			List<BooleanVariable> list = new List<BooleanVariable>();
			for (int n = 0; n < num; n++)
			{
				for (int num2 = n + 1; num2 < num; num2++)
				{
					BooleanVariable equality2 = GetEquality(intArray2[n], intArray2[num2]);
					list.Add(equality2);
				}
			}
			CompileBooleanSumEquals(list.ToArray(), 0, GetBool(t));
		}

		protected void ProcessEqual(DisolverEqual t)
		{
			int num = t.SubTerms.Length;
			if (t.InitialStatus == BooleanVariableState.True)
			{
				if (Utils.TrueForAll(t.SubTerms, (DisolverTerm s) => s is DisolverBooleanTerm))
				{
					BooleanVariable[] boolArray = GetBoolArray(t.SubTerms);
					for (int i = 1; i < boolArray.Length; i++)
					{
						_problem.AddConstraint(new Equivalence(_problem, boolArray[0], boolArray[i]));
					}
				}
				else
				{
					IntegerVariable[] intArray = GetIntArray(t.SubTerms);
					for (int j = 1; j < intArray.Length; j++)
					{
						_problem.AddConstraint(new Equality(_problem, intArray[0], intArray[j]));
					}
				}
				return;
			}
			if (num == 2)
			{
				BooleanVariable equality = GetEquality(GetInt(t.SubTerms[0]), GetInt(t.SubTerms[1]));
				BooleanVariable @bool = GetBool(t);
				_problem.AddConstraint(new Equivalence(_problem, equality, @bool));
				return;
			}
			IntegerVariable[] intArray2 = GetIntArray(t.SubTerms);
			List<BooleanVariable> list = new List<BooleanVariable>();
			for (int k = 0; k < num; k++)
			{
				for (int l = k + 1; l < num; l++)
				{
					BooleanVariable equality2 = GetEquality(intArray2[k], intArray2[l]);
					list.Add(equality2);
				}
			}
			BooleanVariable[] array = list.ToArray();
			CompileBooleanSumEquals(array, array.Length, GetBool(t));
		}

		protected void ProcessIndex(DisolverIndex term)
		{
			IntegerVariable[] intArray = GetIntArray(term.Array);
			IntegerVariable @int = GetInt(term);
			IntegerVariable int2 = GetInt(term.Index);
			_problem.AddConstraint(new Index(_problem, intArray, int2, @int));
		}

		protected void ProcessMatrixIndex(DisolverMatrixIndex term)
		{
			int num = term.Matrix.Length;
			int num2 = Utils.Max(Utils.Apply(term.Matrix, (DisolverTerm[] elt) => elt.Length));
			IntegerVariable @int = GetInt(term);
			IntegerVariable int2 = GetInt(term.Index1);
			IntegerVariable int3 = GetInt(term.Index2);
			IntegerVariable[][] array = new IntegerVariable[num][];
			if (int2.LowerBound < 0)
			{
				_problem.AddConstraint(new LessOrEqual(_problem, _problem.GetIntegerConstant(0L), int2));
			}
			if (int2.UpperBound >= num)
			{
				_problem.AddConstraint(new LessStrict(_problem, int2, _problem.GetIntegerConstant(num)));
			}
			if (int3.LowerBound < 0)
			{
				_problem.AddConstraint(new LessOrEqual(_problem, _problem.GetIntegerConstant(0L), int3));
			}
			if (int3.UpperBound >= num2)
			{
				_problem.AddConstraint(new LessStrict(_problem, int3, _problem.GetIntegerConstant(num2)));
			}
			BooleanVariable[][] array2 = new BooleanVariable[num][];
			for (int i = 0; i < num; i++)
			{
				DisolverTerm[] array3 = term.Matrix[i];
				array[i] = GetIntArray(array3);
				array2[i] = new BooleanVariable[array3.Length];
				BooleanVariable equality = GetEquality(int2, i);
				for (int j = 0; j < array3.Length; j++)
				{
					BooleanVariable equality2 = GetEquality(int3, j);
					BooleanVariable booleanVariable = _problem.CreateInternalBooleanVariable();
					_problem.AddConstraint(new ReifiedConjunction(_problem, equality, equality2, booleanVariable));
					array2[i][j] = _problem.CreateInternalBooleanVariable();
					_problem.AddConstraint(new Implication(_problem, booleanVariable, array2[i][j]));
				}
			}
			List<IntegerVariable> list = new List<IntegerVariable>();
			List<BooleanVariable> list2 = new List<BooleanVariable>();
			for (int k = 0; k < num; k++)
			{
				for (int l = 0; l < array[k].Length; l++)
				{
					list.Add(array[k][l]);
					list2.Add(array2[k][l]);
				}
			}
			_problem.AddConstraint(new SetMembership(_problem, @int, list.ToArray(), list2.ToArray()));
			List<BooleanVariable> list3 = new List<BooleanVariable>();
			for (int m = 0; m < num; m++)
			{
				list3.Clear();
				for (int n = 0; n < array[m].Length; n++)
				{
					list3.Add(array2[m][n]);
				}
				_problem.AddConstraint(new Implication(_problem, GetEquality(int2, m), CreateDisjunction(list3)));
			}
			for (int num3 = 0; num3 < num2; num3++)
			{
				list3.Clear();
				for (int num4 = 0; num4 < num; num4++)
				{
					if (array[num4].Length > num3)
					{
						list3.Add(array2[num4][num3]);
					}
				}
				_problem.AddConstraint(new Implication(_problem, GetEquality(int3, num3), CreateDisjunction(list3)));
			}
		}

		protected void ProcessLessEqual(DisolverLessEqual t)
		{
			CspTerm[] subTerms = t.SubTerms;
			IntegerVariable @int = GetInt(subTerms[0]);
			IntegerVariable int2 = GetInt(subTerms[1]);
			if (t.InitialStatus == BooleanVariableState.False)
			{
				_problem.AddConstraint(new LessStrict(_problem, int2, @int));
			}
			else if (t.InitialStatus == BooleanVariableState.True)
			{
				_problem.AddConstraint(new LessOrEqual(_problem, @int, int2));
			}
			else
			{
				_problem.AddConstraint(new ReifiedLessEqual(_problem, @int, int2, GetBool(t)));
			}
		}

		protected void ProcessNot(DisolverNot t)
		{
			if (t.InitialStatus != BooleanVariableState.False && t.InitialStatus != 0)
			{
				_problem.AddConstraint(new Negation(_problem, GetBool(t.Subterm), GetBool(t)));
			}
		}

		protected void ProcessProduct(DisolverProduct t)
		{
			IntegerVariable @int = GetInt(t.SubTerms[0]);
			IntegerVariable int2 = GetInt(t.SubTerms[1]);
			long lowerBound = @int.LowerBound;
			long lowerBound2 = int2.LowerBound;
			long upperBound = @int.UpperBound;
			long upperBound2 = int2.UpperBound;
			if (lowerBound == upperBound)
			{
				if (lowerBound != 0)
				{
					_problem.AddConstraint(new ProductByConstant(_problem, int2, lowerBound, GetInt(t)));
				}
			}
			else if (lowerBound2 == upperBound2)
			{
				if (lowerBound2 != 0)
				{
					_problem.AddConstraint(new ProductByConstant(_problem, @int, lowerBound2, GetInt(t)));
				}
			}
			else
			{
				_problem.AddConstraint(new ProductByVariable(_problem, @int, int2, GetInt(t)));
			}
		}

		protected void ProcessSum(DisolverSum t)
		{
			int num = t.SubTerms.Length;
			if (AreAllBoolean(t.SubTerms))
			{
				BooleanVariable[] boolArray = GetBoolArray(t.SubTerms);
				_problem.AddConstraint(new LargePseudoBooleanSum(_problem, boolArray, GetInt(t)));
				return;
			}
			IntegerVariable integerVariable = GetInt(t.SubTerms[0]);
			long num2 = integerVariable.LowerBound;
			long num3 = integerVariable.UpperBound;
			for (int i = 1; i < num; i++)
			{
				IntegerVariable @int = GetInt(t.SubTerms[i]);
				num2 += @int.LowerBound;
				num2 = Math.Max(num2, -4611686018427387903L);
				num3 += @int.UpperBound;
				num3 = Math.Min(num3, 4611686018427387903L);
				IntegerVariable integerVariable2 = ((i == num - 1) ? GetInt(t) : _problem.CreateInternalIntegerVariable(num2, num3));
				if (integerVariable.IsInstantiated())
				{
					_problem.AddConstraint(new AdditionToConstant(_problem, @int, integerVariable.GetValue(), integerVariable2));
				}
				else if (@int.IsInstantiated())
				{
					_problem.AddConstraint(new AdditionToConstant(_problem, integerVariable, @int.GetValue(), integerVariable2));
				}
				else
				{
					_problem.AddConstraint(new Addition(_problem, integerVariable, @int, integerVariable2));
				}
				integerVariable = integerVariable2;
			}
		}

		protected void ProcessUnaryMinus(DisolverUnaryMinus t)
		{
			CspTerm t2 = t.SubTerms[0];
			_problem.AddConstraint(new Opposite(_problem, GetInt(t2), GetInt(t)));
		}

		protected void ProcessSquare(DisolverSquare t)
		{
			_problem.AddConstraint(new Square(_problem, GetInt(t.SubTerms[0]), GetInt(t)));
		}

		private BooleanVariable[] GetBoolArray(CspTerm[] tab)
		{
			int num = tab.Length;
			BooleanVariable[] array = new BooleanVariable[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = GetBool(tab[i]);
			}
			return array;
		}

		private IntegerVariable[] GetIntArray(CspTerm[] tab)
		{
			int num = tab.Length;
			IntegerVariable[] array = new IntegerVariable[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = GetInt(tab[i]);
			}
			return array;
		}

		private static bool AreAllBoolean(CspTerm[] t)
		{
			return Array.TrueForAll(t, (CspTerm a) => a is DisolverBooleanTerm);
		}

		private BooleanVariable GetBool(CspTerm t)
		{
			if (t is DisolverIntegerTerm)
			{
				return GetBooleanRepresentation(GetInt(t));
			}
			return _problem.GetImage(t as DisolverBooleanTerm);
		}

		private IntegerVariable GetInt(CspTerm t)
		{
			if (t is DisolverBooleanTerm)
			{
				return GetZeroOneRepresentation(GetBool(t));
			}
			return _problem.GetImage(t as DisolverIntegerTerm);
		}

		private IntegerVariable GetZeroOneRepresentation(BooleanVariable x)
		{
			if (!_asInts.TryGetValue(x, out var value))
			{
				value = _problem.CreateInternalIntegerVariable(0L, 1L);
				_problem.AddConstraint(new IntegerBooleanEquivalence(_problem, value, x));
				_asInts.Add(x, value);
			}
			return value;
		}

		private BooleanVariable GetBooleanRepresentation(IntegerVariable x)
		{
			if (!_asBools.TryGetValue(x, out var value))
			{
				value = _problem.CreateInternalBooleanVariable();
				_problem.AddConstraint(new IntegerBooleanEquivalence(_problem, x, value));
				_asBools.Add(x, value);
			}
			return value;
		}

		/// <summary>
		///   Compiles a pseudo-Boolean constraint imposing that result be
		///   true IFF the number of Boolean vars that are true is larger
		///   than the number.
		///   Warning: ownership of the array is transferred.
		/// </summary>
		private void CompileBooleanSumGreaterEqual(BooleanVariable[] vars, int nb, BooleanVariable result)
		{
			IntegerVariable integerVariable = _problem.CreateInternalIntegerVariable(0L, vars.Length);
			_problem.AddConstraint(new LargePseudoBooleanSum(_problem, vars, integerVariable));
			IntegerVariable integerConstant = _problem.GetIntegerConstant(nb);
			_problem.AddConstraint(new ReifiedLessEqual(_problem, integerConstant, integerVariable, result));
		}

		/// <summary>
		///   Compiles a pseudo-Boolean constraint imposing that result be
		///   true IFF the number of Boolean vars that are true equals the number.
		///   Warning: ownership of the array is transferred.
		/// </summary>
		private void CompileBooleanSumEquals(BooleanVariable[] vars, int nb, BooleanVariable result)
		{
			IntegerVariable integerVariable = _problem.CreateInternalIntegerVariable(0L, vars.Length);
			_problem.AddConstraint(new LargePseudoBooleanSum(_problem, vars, integerVariable));
			IntegerVariable integerConstant = _problem.GetIntegerConstant(nb);
			_problem.AddConstraint(new ReifiedEquality(_problem, integerConstant, integerVariable, result));
		}

		/// <summary>
		///   Creates a BooleanVariable that represents the disjunction
		///   of a list of Boolean terms
		/// </summary>
		private BooleanVariable CreateDisjunction(List<BooleanVariable> collec)
		{
			BooleanVariable[] vars = collec.ToArray();
			BooleanVariable result = _problem.CreateInternalBooleanVariable();
			CompileBooleanSumGreaterEqual(vars, 1, result);
			return result;
		}

		/// <summary>
		///   Gets a Boolean variable representing between a variable and a
		///   constant. This is optimized (cached and values for a variable
		///   are groupped) and should be used and abused whenever there is
		///   need for a leightweight form of watching.
		/// </summary>
		internal BooleanVariable GetEquality(IntegerVariable x, long i)
		{
			if (!_variableValuePairs.TryGetValue(x, out var value))
			{
				value = new Dictionary<long, BooleanVariable>();
				_variableValuePairs.Add(x, value);
			}
			if (!value.TryGetValue(i, out var value2))
			{
				value2 = _problem.CreateInternalBooleanVariable();
				value.Add(i, value2);
			}
			return value2;
		}

		/// <summary>
		///   Gets a Boolean variable representing the equality
		///   between two variables.
		/// </summary>
		internal BooleanVariable GetEquality(IntegerVariable x, IntegerVariable y)
		{
			bool flag = x.IsInstantiated();
			bool flag2 = y.IsInstantiated();
			if (flag && flag2)
			{
				if (x.GetValue() != y.GetValue())
				{
					return _problem.GetBooleanFalse();
				}
				return _problem.GetBooleanTrue();
			}
			if (flag)
			{
				return GetEquality(y, x.GetValue());
			}
			if (flag2)
			{
				return GetEquality(x, y.GetValue());
			}
			BooleanVariable booleanVariable = _problem.CreateInternalBooleanVariable();
			_problem.AddConstraint(new ReifiedEquality(_problem, x, y, booleanVariable));
			return booleanVariable;
		}
	}
}
