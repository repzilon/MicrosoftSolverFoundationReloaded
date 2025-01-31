using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A constraint of the form bResult == (b0 | b1 | ...) where bN are Booleans
	///           There is no support for shortcut "||" in the solver: that is not declarative.
	/// </summary>
	internal sealed class BooleanOr : LogicFunction
	{
		/// <summary>
		///   size at which we start using the incremental version
		/// </summary>
		public const int SizeLimit = 4;

		private int _unresolved;

		/// <summary> A unique ID from 0 to [number of BooleanOr terms]-1,
		///           used as index for data specific to BooleanOr terms
		/// </summary>
		/// <remarks>
		///   The extra LS_Value is used to maintain incrementally the 
		///   sum of negative violations 
		/// </remarks>
		internal readonly int OrdinalAmongBooleanOrTerms;

		internal override string Name => "|";

		/// <summary> A constraint of the form bResult == (b0 | b1 | ...) where bN are Booleans
		///           There is no support for shortcut "||" in the solver: that is not declarative.
		/// </summary>
		internal BooleanOr(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, inputs)
		{
			Reset();
			OrdinalAmongBooleanOrTerms = solver._numBooleanOrTerms++;
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		internal override void Reset()
		{
			_unresolved = Width;
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			if (base.Count == 0)
			{
				conflict = this;
				return false;
			}
			conflict = null;
			if (_unresolved == 0)
			{
				return false;
			}
			if (1 == base.Count && !IsTrue)
			{
				bool flag = false;
				int unresolved = _unresolved;
				_unresolved = 0;
				for (int i = 0; i < unresolved; i++)
				{
					CspSolverTerm cspSolverTerm = _inputs[i];
					flag |= cspSolverTerm.Force(choice: false, out conflict);
					if (conflict != null)
					{
						break;
					}
				}
				return flag;
			}
			int num = 0;
			while (num < _unresolved)
			{
				CspSolverTerm cspSolverTerm2 = _inputs[num];
				if (cspSolverTerm2.Count == 0)
				{
					conflict = cspSolverTerm2;
					return false;
				}
				if (1 == cspSolverTerm2.Count)
				{
					if (cspSolverTerm2.IsTrue)
					{
						_unresolved = 0;
						return Force(choice: true, out conflict);
					}
					_unresolved--;
					if (num < _unresolved)
					{
						_inputs[num] = _inputs[_unresolved];
						_inputs[_unresolved] = cspSolverTerm2;
					}
				}
				else
				{
					num++;
				}
			}
			if (_unresolved == 0)
			{
				return Force(choice: false, out conflict);
			}
			if (1 == _unresolved && 1 == base.Count && IsTrue)
			{
				return _inputs[0].Force(choice: true, out conflict);
			}
			return false;
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.Or(inputs);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.Or(inputs);
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		/// <remarks> The violation of the Term is an indicator of whether the 
		///           value currently assigned by the local search algorithm to 
		///           one of the inputs is negative (subterm satisfied). 
		///           If such is the case we take the sum of the negative inputs.
		///           If all inputs are positive we take the min of these 
		///           positive values.
		/// </remarks>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			if (_inputs.Length < 4)
			{
				NaiveRecomputationForSmallSize(ls);
				return;
			}
			int num = 0;
			for (int num2 = _inputs.Length - 1; num2 >= 0; num2--)
			{
				int num3 = ls[_inputs[num2]];
				if (num3 < 0)
				{
					num += num3;
				}
			}
			ls.SetExtraData(this, num);
			ls[this] = ((num < 0) ? num : RecomputeMinPositiveViolations(ls));
		}

		/// <summary> Incremental recomputation: update the value of the term 
		///           when one of its arguments is changed
		/// </summary>
		internal override void PropagateChange(LocalSearchSolver ls, CspSolverTerm modifiedArg, int oldValue, int newValue)
		{
			if (_inputs.Length < 4)
			{
				NaiveRecomputationForSmallSize(ls);
				return;
			}
			int num = ls.GetExtraData(this);
			bool flag = num < 0;
			if (oldValue < 0)
			{
				num -= oldValue;
			}
			if (newValue < 0)
			{
				num += newValue;
			}
			ls.SetExtraData(this, num);
			if (num < 0)
			{
				ls[this] = num;
				return;
			}
			int num2 = ls[this];
			if (flag || (oldValue == num2 && newValue > num2))
			{
				ls[this] = RecomputeMinPositiveViolations(ls);
			}
			else if (newValue < num2)
			{
				ls[this] = newValue;
			}
		}

		/// <summary> Recomputes the highest negative values
		/// </summary>
		private int RecomputeMinPositiveViolations(LocalSearchSolver ls)
		{
			int num = int.MaxValue;
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm expr in inputs)
			{
				int num2 = ls[expr];
				if (num2 > 0)
				{
					num = Math.Min(num, num2);
				}
			}
			return num;
		}

		/// <summary> Algorithm used when wehave few inputs
		/// </summary>
		internal void NaiveRecomputationForSmallSize(LocalSearchSolver ls)
		{
			int num = int.MaxValue;
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm expr in inputs)
			{
				num = BooleanFunction.Or(num, ls[expr]);
			}
			ls[this] = num;
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			ValueWithGradients valueWithGradients = new ValueWithGradients(int.MaxValue);
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm term in inputs)
			{
				valueWithGradients = Gradients.Or(valueWithGradients, ls.GetGradients(term));
			}
			ls.SetGradients(this, valueWithGradients);
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping,
		///           together with a suggestion of value for this subterm
		/// </summary>
		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			return SelectSubtermWithWrongPolarity(ls, target);
		}
	}
}
