using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A map is an N-dimensional index.  The axes each have a discrete otherSet of possible choices,
	///           and the axes provide coordinates to select minus100 of the variables referenced by the map.
	/// </summary>
	internal sealed class IntMap : CspFunction
	{
		private CspSolverDomain[] _axes;

		private int _N;

		private int _dim;

		private bool _isBool;

		private CspSymbolDomain _symbols;

		internal int Dimension => _dim;

		public override bool IsBoolean => _isBool;

		internal override CspSymbolDomain Symbols => _symbols;

		internal override bool IsTrue
		{
			get
			{
				if (!_isBool || 1 != base.Count)
				{
					throw new InvalidOperationException(Resources.InvalidIsTrueCall + ToString());
				}
				return 1 == First;
			}
		}

		internal override string Name => "Map";

		/// <summary> {arg0, arg1, ..argN-1} [argN] [argN+1] [..].  A map is an N-dimensional index.
		///           The axes each have a discrete otherSet of possible choices,
		///           and the axes provide coordinates to select one of the variables referenced by the map.
		///           The variables being indexed are [0..N-1] of the inputs, then follow the indexer args.
		///
		/// </summary>
		internal IntMap(ConstraintSystem solver, CspSolverTerm[] inputs, params CspSolverDomain[] axes)
			: base(solver, inputs)
		{
			_dim = axes.Length;
			_N = inputs.Length - _dim;
			_symbols = CspFunction.AllowConsistentSymbols(inputs, 0, _N);
			InitUnitScales();
			for (int i = _N; i < inputs.Length; i++)
			{
				if (_inputs[i].OutputScale != 1)
				{
					throw new ArgumentException(Resources.IntMapNonIntegerIndex);
				}
			}
			int num = _N;
			_axes = axes;
			CspSolverTerm conflict = null;
			for (int j = 0; j < _dim; j++)
			{
				num /= _axes[j].Count;
				IndexVar(j).Intersect(_axes[j], out conflict);
				if (conflict != null)
				{
					throw new ModelException(Resources.IndexVariableRangesDoNotMatchArrayShape);
				}
			}
			_isBool = true;
			for (int k = 0; k < _N; k++)
			{
				if (!_inputs[k].IsBoolean)
				{
					_isBool = false;
					break;
				}
			}
			SortedUniqueIntSet sortedUniqueIntSet = default(SortedUniqueIntSet);
			int num2 = ConstraintSystem.MaxFinite;
			int num3 = ConstraintSystem.MinFinite;
			bool flag = false;
			for (int l = 0; l < _N; l++)
			{
				if ((sortedUniqueIntSet.Set == null || 5000 > sortedUniqueIntSet.Set.Length) && 1000 > _inputs[l].FiniteValue.Count)
				{
					sortedUniqueIntSet.Union(_inputs[l].FiniteValue);
					continue;
				}
				flag = true;
				if (_inputs[l].First < num2)
				{
					num2 = _inputs[l].First;
				}
				if (num3 < _inputs[l].Last)
				{
					num3 = _inputs[l].Last;
				}
			}
			if (flag)
			{
				Intersect(num2, num3, out conflict);
			}
			else if (sortedUniqueIntSet.Set != null)
			{
				Intersect(out conflict, sortedUniqueIntSet.Set);
			}
			if (conflict != null)
			{
				throw new ModelException(Resources.IndexVariableRangesDoNotMatchArrayShape);
			}
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			CspTerm[] array = new CspTerm[_N];
			for (int i = 0; i < _N; i++)
			{
				array[i] = inputs[i];
			}
			if (_dim == 1)
			{
				return newModel.Index(array, inputs[_N], _axes[0]);
			}
			CspTerm[] array2 = new CspTerm[_dim];
			for (int j = 0; j < _dim; j++)
			{
				array2[j] = inputs[_N + j];
			}
			return newModel.Index(array, array2, _axes);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			CspTerm[] array = new CspTerm[_N];
			for (int i = 0; i < _N; i++)
			{
				array[i] = inputs[i];
			}
			if (_dim == 1)
			{
				return newModel.Index(array, inputs[_N]);
			}
			CspTerm[] array2 = new CspTerm[_dim];
			for (int j = 0; j < _dim; j++)
			{
				array2[j] = inputs[_N + j];
			}
			return newModel.Index(array, array2);
		}

		private CspSolverTerm IndexVar(int i)
		{
			return _inputs[_N + i];
		}

		private CspSolverTerm SingleMap(out int index)
		{
			int num = _axes.Length;
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				int count = _axes[i].Count;
				num2 = num2 * count + _axes[i].IndexOf(IndexVar(i).First);
			}
			index = num2;
			return _inputs[num2];
		}

		private void SliceMap(out CspSolverTerm[] values, out int[] indices, out CspSolverDomain presentAxis)
		{
			int num = 0;
			CspSolverTerm cspSolverTerm = null;
			int num2 = 0;
			CspSolverDomain cspSolverDomain = null;
			presentAxis = null;
			for (int i = 0; i < _dim; i++)
			{
				int count = _axes[i].Count;
				num *= count;
				if (1 == IndexVar(i).Count)
				{
					num += _axes[i].IndexOf(IndexVar(i).First);
					num2 *= count;
					continue;
				}
				cspSolverTerm = IndexVar(i);
				num2 = 1;
				presentAxis = cspSolverTerm.FiniteValue;
				cspSolverDomain = _axes[i];
			}
			values = new CspSolverTerm[presentAxis.Count];
			indices = new int[presentAxis.Count];
			int num3 = 0;
			foreach (int item in cspSolverDomain.Forward())
			{
				if (presentAxis.Contains(item))
				{
					values[num3] = _inputs[num];
					indices[num3] = num;
					num3++;
				}
				num += num2;
			}
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			int num = _axes.Length;
			int count = base.Count;
			if (count == 0)
			{
				conflict = this;
				return false;
			}
			int num2 = 1;
			int num3 = 0;
			int i = -1;
			for (int j = 0; j < num; j++)
			{
				CspSolverTerm cspSolverTerm = IndexVar(j);
				int count2 = cspSolverTerm.Count;
				if (count2 == 0)
				{
					conflict = cspSolverTerm;
					return false;
				}
				if (1 < count2)
				{
					num3++;
					num2 *= count2;
					i = j;
				}
			}
			bool flag = false;
			conflict = null;
			if (1 == num2)
			{
				int index;
				CspSolverTerm cspSolverTerm2 = SingleMap(out index);
				flag = cspSolverTerm2.Intersect(ScaleToInput(base.FiniteValue, index), out conflict);
				if (conflict == null)
				{
					flag |= Intersect(ScaleToOutput(cspSolverTerm2.FiniteValue, index), out conflict);
				}
			}
			else if (1 == num3 && count < 1000 && num2 <= 10)
			{
				SliceMap(out var values, out var indices, out var presentAxis);
				int first = First;
				int last = Last;
				int[] array = new int[values.Length];
				int num4 = 0;
				SortedUniqueIntSet sortedUniqueIntSet = default(SortedUniqueIntSet);
				bool flag2 = false;
				CspSolverDomain cspSolverDomain = null;
				for (int k = 0; k < values.Length; k++)
				{
					CspSolverTerm cspSolverTerm3 = values[k];
					int id = indices[k];
					int num5 = ScaleToOutput(cspSolverTerm3.First, id);
					int num6 = ScaleToOutput(cspSolverTerm3.Last, id);
					if (count > 100 || cspSolverTerm3.Count > 100)
					{
						if (num5 <= last && first <= num6)
						{
							array[num4++] = presentAxis[k];
							if (num5 <= first && last <= num6)
							{
								flag2 = true;
							}
							else if (!flag2)
							{
								sortedUniqueIntSet.Union(ScaleToOutput(cspSolverTerm3.FiniteValue, id));
							}
						}
						continue;
					}
					CspSolverDomain cspSolverDomain2 = ScaleToOutput(cspSolverTerm3.FiniteValue, id);
					if (cspSolverDomain == null)
					{
						cspSolverDomain = base.FiniteValue.Clone();
					}
					CspSolverDomain newD;
					bool flag3 = !cspSolverDomain.Intersect(cspSolverDomain2, out newD);
					if (newD.Count != 0)
					{
						array[num4++] = presentAxis[k];
						if (flag3)
						{
							flag2 = true;
						}
						else if (!flag2)
						{
							sortedUniqueIntSet.Union(cspSolverDomain2);
						}
					}
					if (!flag3)
					{
						cspSolverDomain = null;
					}
				}
				if (num4 == 0)
				{
					return Intersect(ConstraintSystem.DEmpty, out conflict);
				}
				if (!flag2)
				{
					flag = Intersect(out conflict, sortedUniqueIntSet.Set);
				}
				if (conflict == null && num4 < values.Length)
				{
					flag |= IndexVar(i).Intersect(out conflict, CspSolverTerm.SubArray(array, num4));
				}
			}
			return flag;
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			int num = ComputePosition(ls);
			if (0 <= num && num < _N)
			{
				ls[this] = (_isBool ? ls[_inputs[num]] : ls.GetIntegerValue(_inputs[num]));
			}
			else
			{
				ls.SignalOverflow(this);
			}
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			ValueWithGradients v = new ValueWithGradients(ls[this]);
			for (int i = 0; i < _dim; i++)
			{
				CspSolverTerm cspSolverTerm = IndexVar(i);
				ValueWithGradients integerGradients = ls.GetIntegerGradients(cspSolverTerm);
				for (int j = integerGradients.DecGradient; j < 0; j++)
				{
					int? num = EstimateIndexChange(ls, cspSolverTerm, integerGradients.Value + j);
					if (num.HasValue)
					{
						v.Expand(integerGradients.DecVariable, num.Value);
					}
				}
				for (int num2 = integerGradients.IncGradient; num2 > 0; num2--)
				{
					int? num = EstimateIndexChange(ls, cspSolverTerm, integerGradients.Value + num2);
					if (num.HasValue)
					{
						v.Expand(integerGradients.IncVariable, num.Value);
					}
				}
			}
			int num3 = ComputePosition(ls);
			if (0 <= num3 && num3 < _N)
			{
				CspSolverTerm term = _inputs[num3];
				ValueWithGradients valueWithGradients = (_isBool ? ls.GetGradients(term) : ls.GetIntegerGradients(term));
				v.Expand(valueWithGradients.DecVariable, valueWithGradients.Value + valueWithGradients.DecGradient);
				v.Expand(valueWithGradients.IncVariable, valueWithGradients.Value + valueWithGradients.IncGradient);
			}
			ls.SetGradients(this, v);
		}

		/// <summary>
		/// looks-up the current values of the index terms
		/// and computes the corresponding position in the input array
		/// </summary>
		private int ComputePosition(LocalSearchSolver ls)
		{
			int num = 0;
			for (int i = 0; i < _dim; i++)
			{
				int count = _axes[i].Count;
				num = num * count + _axes[i].IndexOf(ls.GetIntegerValue(IndexVar(i)));
			}
			return num;
		}

		/// <summary>
		/// What would be the value if we re-assigned one of the index terms to newval?
		/// </summary>
		private int? EstimateIndexChange(LocalSearchSolver ls, CspSolverTerm modifiedIndexTerm, int newval)
		{
			int num = 0;
			for (int i = 0; i < _dim; i++)
			{
				int count = _axes[i].Count;
				CspSolverTerm cspSolverTerm = IndexVar(i);
				int x = ((cspSolverTerm != modifiedIndexTerm) ? ls.GetIntegerValue(cspSolverTerm) : newval);
				num = num * count + _axes[i].IndexOf(x);
			}
			if (0 > num || num >= _N)
			{
				return null;
			}
			return _isBool ? ls[_inputs[num]] : ls.GetIntegerValue(_inputs[num]);
		}
	}
}
