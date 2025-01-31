using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A boolean which is equal to the mutual inequality of all the input expressions.
	/// </summary>
	internal sealed class Unequal : Comparison
	{
		/// <summary> The type of data that has to be allocated by
		///           every local search thread for each Unequal term
		/// </summary>
		/// <remarks> Currently this is just an alias for a Dictionary
		///           but we might change later to a class that aggregates
		///           a dictionary and other things, hence the alias
		/// </remarks>
		internal class LocalSearchData : LS_ValueMap<int>
		{
			internal LocalSearchData(int estimatedSize)
				: base(estimatedSize, 0)
			{
			}
		}

		/// <summary> Size at which we start using the incremental version
		/// </summary>
		public const int SizeLimit = 4;

		private int[] _knowns;

		private int _unresolved;

		private int _min;

		private int _cardinality;

		/// <summary> A unique ID from 0 to [number of unequal terms]-1,
		///           used as index for data specific to Unequal terms
		/// </summary>
		/// <remarks> The extra data are used to count (in a dictionary) the
		///           number of inputs that take any given value
		/// </remarks>
		internal readonly int OrdinalAmongUnequalTerms;

		/// <summary> Represent this class
		/// </summary>
		internal override string Name => "Unequal";

		/// <summary> A boolean which is equal to the mutual inequality of all the input expressions.
		/// </summary>
		internal Unequal(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, inputs)
		{
			Reset();
			_knowns = new int[Width];
			OrdinalAmongUnequalTerms = solver._numUnequalTerms++;
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		internal override void Reset()
		{
			_unresolved = Width;
			_min = int.MaxValue;
			_cardinality = 0;
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			bool changed = false;
			bool flag;
			do
			{
				flag = false;
				int unresolved = _unresolved;
				if (unresolved == 0)
				{
					conflict = null;
					return false;
				}
				if (base.Count == 0)
				{
					conflict = this;
					return false;
				}
				if (!ResolveSingles(out changed, out conflict))
				{
					return changed;
				}
				bool flag2 = true;
				bool flag3 = false;
				if (1 == base.Count)
				{
					flag2 = false;
					flag3 = !IsTrue;
				}
				if (_unresolved < unresolved)
				{
					int num = (flag3 ? (Width - _unresolved) : (unresolved - _unresolved));
					if (1 == num)
					{
						int num2 = _knowns[_unresolved];
						for (int i = _unresolved + 1; i < Width; i++)
						{
							if (num2 == _knowns[i])
							{
								_unresolved = 0;
								if (flag3)
								{
									conflict = null;
									return changed;
								}
								if (flag2)
								{
									return Force(choice: false, out conflict);
								}
								return _inputs[_unresolved].Intersect(ConstraintSystem.DEmpty, out conflict) || changed;
							}
						}
						if (!flag2 && !flag3)
						{
							for (int j = 0; j < _unresolved; j++)
							{
								changed |= _inputs[j].Exclude(ScaleToInput(num2, j), ScaleToInput(num2, j), out conflict);
								if (conflict != null)
								{
									if (flag2)
									{
										return Force(choice: false, out conflict);
									}
									return changed;
								}
								if (1 == _inputs[j].Count)
								{
									flag = true;
								}
							}
						}
					}
					else
					{
						for (int k = _unresolved; k < _unresolved + num; k++)
						{
							for (int l = k + 1; l < Width; l++)
							{
								if (_knowns[k] == _knowns[l])
								{
									_unresolved = 0;
									if (flag3)
									{
										conflict = null;
										return changed;
									}
									if (flag2)
									{
										return Force(choice: false, out conflict);
									}
									return _inputs[k].Intersect(ConstraintSystem.DEmpty, out conflict) || changed;
								}
							}
						}
						if (!flag2 && !flag3)
						{
							int[] array = CspSolverTerm.SubArray(_knowns, _unresolved, num);
							Array.Sort(array);
							for (int m = 0; m < _unresolved; m++)
							{
								changed |= _inputs[m].Exclude(out conflict, ScaleToInput(array, m));
								if (conflict != null)
								{
									return changed;
								}
								if (1 == _inputs[m].Count)
								{
									flag = true;
								}
							}
						}
					}
				}
				if (!flag && (_unresolved < 10 || _unresolved < Width / 2))
				{
					flag = RemoveClosedSubsets(flag2, flag3, out conflict);
					changed = changed || flag;
					if (conflict != null || _unresolved == 0)
					{
						return changed;
					}
				}
			}
			while (flag);
			return changed;
		}

		/// <summary> Scan the unresolveds for inputs which are now single-valued
		///           and move them to the end of the inputs.  Also measure the
		///           overall cardinality.
		/// </summary>
		/// <returns>True iff more work needs to be done after this method returns.</returns>
		private bool ResolveSingles(out bool changed, out CspSolverTerm conflict)
		{
			conflict = null;
			changed = false;
			if (_unresolved == 0)
			{
				return false;
			}
			int num = int.MinValue;
			int num2 = 0;
			while (num2 < _unresolved)
			{
				CspSolverTerm cspSolverTerm = _inputs[num2];
				if (cspSolverTerm.Count == 0)
				{
					conflict = cspSolverTerm;
					return false;
				}
				if (_cardinality == 0)
				{
					if (ScaleToOutput(cspSolverTerm.First, num2) < _min)
					{
						_min = ScaleToOutput(cspSolverTerm.First, num2);
					}
					if (num < ScaleToOutput(cspSolverTerm.Last, num2))
					{
						num = ScaleToOutput(cspSolverTerm.Last, num2);
					}
				}
				if (1 == cspSolverTerm.Count)
				{
					_unresolved--;
					int id = num2;
					if (num2 < _unresolved)
					{
						_inputs[num2] = _inputs[_unresolved];
						_inputs[_unresolved] = cspSolverTerm;
						Statics.Swap(ref _scales[num2], ref _scales[_unresolved]);
						id = _unresolved;
					}
					_knowns[_unresolved] = ScaleToOutput(cspSolverTerm.First, id);
				}
				else
				{
					num2++;
				}
			}
			if (_cardinality == 0)
			{
				_cardinality = num - _min + 1;
			}
			if (_unresolved == 0)
			{
				changed = true;
				bool flag = true;
				for (int i = 0; i < Width - 1; i++)
				{
					for (int j = i + 1; j < Width; j++)
					{
						if (_knowns[i] == _knowns[j])
						{
							flag = false;
							break;
						}
					}
				}
				if (1 == base.Count)
				{
					if ((IsTrue && !flag) || (!IsTrue && flag))
					{
						conflict = this;
					}
				}
				else if (flag)
				{
					Intersect(ConstraintSystem.DTrue, out conflict);
				}
				else
				{
					Intersect(ConstraintSystem.DFalse, out conflict);
				}
				return false;
			}
			return true;
		}

		/// <summary> In cases where cardinality exactly matches the inputs,
		///             look for value sets which exactly cover a matching count
		///             of inputs, and exclude them from any other input
		/// </summary>
		private bool RemoveClosedSubsets(bool defer, bool falsify, out CspSolverTerm conflict)
		{
			int[] array = new int[_unresolved];
			bool flag = false;
			conflict = null;
			int num = 0;
			int num2 = int.MaxValue;
			while (_cardinality < num2)
			{
				num++;
				num2 /= _cardinality;
			}
			for (int i = 0; i < _unresolved; i++)
			{
				CspSolverTerm cspSolverTerm = _inputs[i];
				if (num < cspSolverTerm.Count)
				{
					array[i] = int.MinValue;
					continue;
				}
				int num3 = 0;
				foreach (int item in cspSolverTerm.Backward())
				{
					num3 = num3 * _cardinality + (ScaleToOutput(item, i) - _min);
				}
				array[i] = num3;
			}
			for (int j = 0; j < _unresolved - 1; j++)
			{
				int num4 = array[j];
				array[j] = ~num4;
				if (0 > num4)
				{
					continue;
				}
				int num5 = _inputs[j].Count - 1;
				for (int k = j + 1; k < _unresolved; k++)
				{
					if (num4 != array[k])
					{
						continue;
					}
					array[k] = ~num4;
					if (--num5 == 0 && !falsify && !defer)
					{
						CspSolverDomain valuesAtOutputScale = ScaleToOutput(_inputs[j].FiniteValue, j);
						for (int l = 0; l < _unresolved; l++)
						{
							if (~num4 != array[l])
							{
								flag |= _inputs[l].Exclude(ScaleToInput(valuesAtOutputScale, l), out conflict);
								if (conflict != null)
								{
									return flag;
								}
							}
						}
						if (!flag)
						{
							break;
						}
						return true;
					}
					if (-1 == num5)
					{
						if (falsify)
						{
							_unresolved = 0;
							return flag;
						}
						_unresolved = 0;
						return Force(choice: false, out conflict) || flag;
					}
				}
			}
			return flag;
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.Unequal(inputs);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.Unequal(inputs);
		}

		/// <summary> Naive recomputation: Recompute the value of the term 
		///           from the value of all its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			if (_inputs.Length < 4)
			{
				RecomputationWithoutCounters(ls);
				return;
			}
			LocalSearchData localSearchData = GetLocalSearchData(ls);
			localSearchData.Clear();
			int num = 0;
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm expr in inputs)
			{
				localSearchData[ls.GetIntegerValue(expr)]++;
			}
			foreach (KeyValuePair<int, int> item in localSearchData.EnumerateModifiedEntries())
			{
				int value = item.Value;
				num += Math.Max(value - 1, 0);
			}
			ls[this] = BooleanFunction.NonZero(num);
		}

		/// <summary> Incremental recomputation: update the value of the term 
		///           when one of its arguments is changed
		/// </summary>
		internal override void PropagateChange(LocalSearchSolver ls, CspSolverTerm modifiedArg, int oldValue, int newValue)
		{
			if (_inputs.Length < 4)
			{
				RecomputationWithoutCounters(ls);
				return;
			}
			if (modifiedArg.IsBoolean)
			{
				oldValue = LocalSearchSolver.ViolationToZeroOne(oldValue);
				newValue = LocalSearchSolver.ViolationToZeroOne(newValue);
			}
			LocalSearchData localSearchData = GetLocalSearchData(ls);
			int num = BooleanFunction.NonNegative(ls[this]);
			int num2 = localSearchData[oldValue];
			int num3 = localSearchData[newValue];
			if (num2 > 1)
			{
				num--;
			}
			if (num3 > 0)
			{
				num++;
			}
			localSearchData[oldValue] = num2 - 1;
			localSearchData[newValue] = num3 + 1;
			ls[this] = BooleanFunction.NonZero(num);
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping,
		///           together with a suggestion of value for this subterm
		/// </summary>
		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			int num = _inputs.Length;
			Random randomSource = ls.RandomSource;
			if (target == 0)
			{
				return SelectSubtermToFlipTowardsEquality(ls);
			}
			if (num < 4)
			{
				return SelectSubtermToFlipTowardsDifference(ls);
			}
			LocalSearchData localSearchData = GetLocalSearchData(ls);
			CspSolverTerm cspSolverTerm = _inputs[0];
			foreach (CspSolverTerm item in RandomlyEnumerateSubterms(randomSource))
			{
				if (item.BaseValueSet.Count > 1 && localSearchData[ls[item]] > 1)
				{
					cspSolverTerm = item;
					break;
				}
			}
			foreach (int item2 in CspFunction.RandomlyEnumerateValues(cspSolverTerm.BaseValueSet, randomSource))
			{
				if (localSearchData[item2] == 0)
				{
					return new KeyValuePair<CspSolverTerm, int>(cspSolverTerm, item2);
				}
			}
			return new KeyValuePair<CspSolverTerm, int>(cspSolverTerm, cspSolverTerm.BaseValueSet.Pick(randomSource));
		}

		/// <summary> Algorithm that is preferable for small sizes </summary>
		/// <remarks> Will give better distance information.
		///           Also does not allocate the dictionary, which would be
		///           a waste for small disequalities.
		///           Quadratic complexity so not appropriate for large sizes.
		/// </remarks>
		internal void RecomputationWithoutCounters(LocalSearchSolver ls)
		{
			int num = _inputs.Length;
			int num2;
			if (num < 2)
			{
				num2 = BooleanFunction.Satisfied;
			}
			else
			{
				num2 = int.MinValue;
				for (int i = 0; i < num; i++)
				{
					int integerValue = ls.GetIntegerValue(_inputs[i]);
					for (int j = i + 1; j < num; j++)
					{
						int integerValue2 = ls.GetIntegerValue(_inputs[j]);
						num2 = BooleanFunction.And(num2, BooleanFunction.Unequal(integerValue, integerValue2));
					}
				}
			}
			ls[this] = num2;
		}

		/// <summary> Gets the dictionary living in the context of the
		///           given local search. Will be created the first time
		///           this method is called and looked-up afterwards.
		/// </summary>
		private LocalSearchData GetLocalSearchData(LocalSearchSolver ls)
		{
			LocalSearchData localSearchData = ls.GetExtraData(this);
			if (localSearchData == null)
			{
				localSearchData = new LocalSearchData(_inputs.Length);
				ls.SetExtraData(this, localSearchData);
			}
			return localSearchData;
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			if (_inputs.Length < 4)
			{
				RecomputeGradientsSmall(ls);
			}
			else
			{
				RecomputeGradientsLarge(ls);
			}
		}

		private void RecomputeGradientsSmall(LocalSearchSolver ls)
		{
			int num = _inputs.Length;
			if (num < 2)
			{
				ls.CancelGradients(this);
				return;
			}
			ValueWithGradients valueWithGradients = int.MinValue;
			for (int i = 0; i < num; i++)
			{
				ValueWithGradients gradients = ls.GetGradients(_inputs[i]);
				for (int j = i + 1; j < num; j++)
				{
					ValueWithGradients gradients2 = ls.GetGradients(_inputs[j]);
					valueWithGradients = Gradients.And(valueWithGradients, Gradients.Unequal(gradients, gradients2));
				}
			}
			ls.SetGradients(this, valueWithGradients);
		}

		private void RecomputeGradientsLarge(LocalSearchSolver ls)
		{
			LocalSearchData localSearchData = GetLocalSearchData(ls);
			int num = ls[this];
			CspVariable cspVariable = null;
			if (num > 0)
			{
				foreach (CspSolverTerm item in RandomlyEnumerateSubterms(ls.RandomSource))
				{
					cspVariable = ReassigningInputWouldDecreaseViolation(ls, item, localSearchData);
					if (cspVariable != null)
					{
						ls.SetGradients(this, (num == 1) ? (-2) : (-1), cspVariable, 0, null);
						break;
					}
				}
				if (cspVariable == null)
				{
					ls.CancelGradients(this);
				}
				return;
			}
			foreach (CspSolverTerm item2 in RandomlyEnumerateSubterms(ls.RandomSource))
			{
				cspVariable = ReassigningInputWouldIncreaseViolation(ls, item2, localSearchData);
				if (cspVariable != null)
				{
					ls.SetGradients(this, 0, null, 2, cspVariable);
					break;
				}
			}
			if (cspVariable == null)
			{
				ls.CancelGradients(this);
			}
		}

		private static CspVariable ReassigningInputWouldDecreaseViolation(LocalSearchSolver ls, CspSolverTerm t, LocalSearchData d)
		{
			ValueWithGradients integerGradients = ls.GetIntegerGradients(t);
			int num = d[integerGradients.Value];
			if (num > 1)
			{
				for (int i = integerGradients.DecGradient; i < 0; i++)
				{
					int key = integerGradients.Value + i;
					if (d[key] == 0)
					{
						return integerGradients.DecVariable;
					}
				}
				for (int num2 = integerGradients.IncGradient; num2 > 0; num2--)
				{
					int key = integerGradients.Value + num2;
					if (d[key] == 0)
					{
						return integerGradients.IncVariable;
					}
				}
			}
			return null;
		}

		private static CspVariable ReassigningInputWouldIncreaseViolation(LocalSearchSolver ls, CspSolverTerm t, LocalSearchData d)
		{
			ValueWithGradients integerGradients = ls.GetIntegerGradients(t);
			_ = d[integerGradients.Value];
			for (int i = integerGradients.DecGradient; i < 0; i++)
			{
				int key = integerGradients.Value + i;
				if (d[key] > 0)
				{
					return integerGradients.DecVariable;
				}
			}
			for (int num = integerGradients.IncGradient; num > 0; num--)
			{
				int key = integerGradients.Value + num;
				if (d[key] > 0)
				{
					return integerGradients.IncVariable;
				}
			}
			return null;
		}
	}
}
