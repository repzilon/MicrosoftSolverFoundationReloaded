using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A constraint of the form bResult == (b0 &amp; b1 &amp; ...) where bN are Booleans
	///           There is no support for shortcut "&amp;&amp;" in the solver: that is not declarative.
	/// </summary>
	internal sealed class BooleanAnd : LogicFunction
	{
		/// <summary> Size at which we start using the incremental version
		/// </summary>
		public const int SizeLimit = 4;

		private bool _resolved;

		/// <summary> A unique ID from 0 to [number of BooleanAnd terms]-1,
		///           used as index for data specific to Unequal terms
		/// </summary>
		/// <remarks>
		///   The extra LS_Value is used to maintain incrementally the 
		///   sum of positive violations 
		/// </remarks>
		internal readonly int OrdinalAmongBooleanAndTerms;

		internal override string Name => "&";

		/// <summary> A constraint of the form bResult == (b0 &amp; b1 &amp; ...) where bN are Booleans
		///           There is no support for shortcut "&amp;&amp;" in the solver.
		/// </summary>
		internal BooleanAnd(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, inputs)
		{
			OrdinalAmongBooleanAndTerms = solver._numBooleanAndTerms++;
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		internal override void Reset()
		{
			_resolved = false;
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			if (base.Count == 0)
			{
				conflict = this;
				return false;
			}
			conflict = null;
			if (_resolved)
			{
				return false;
			}
			int width = Width;
			int num = 0;
			conflict = null;
			for (int i = 0; i < Width; i++)
			{
				CspSolverTerm cspSolverTerm = _inputs[i];
				if (cspSolverTerm.Count == 0)
				{
					conflict = cspSolverTerm;
					return false;
				}
				if (1 < cspSolverTerm.Count)
				{
					num++;
				}
				else if (!cspSolverTerm.IsTrue)
				{
					conflict = cspSolverTerm;
					break;
				}
			}
			if (conflict != null)
			{
				_resolved = true;
				return Force(choice: false, out conflict);
			}
			if (num == 0)
			{
				_resolved = true;
				return Force(choice: true, out conflict);
			}
			bool flag = false;
			if (1 == base.Count && (IsTrue || 1 == num))
			{
				for (int j = 0; j < width; j++)
				{
					CspSolverTerm cspSolverTerm2 = _inputs[j];
					if (1 < cspSolverTerm2.Count)
					{
						flag |= cspSolverTerm2.Force(IsTrue, out conflict);
						if (conflict != null)
						{
							return flag;
						}
					}
				}
				_resolved = true;
			}
			return flag;
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.And(inputs);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.And(inputs);
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		/// <remarks> The violation of the Term is an indicator of whether the 
		///           values currently assigned by the local search algorithm to 
		///           all inputs are negative (subterms satisfied). 
		///           If such is the case we take the max of the negative values. 
		///           If on the contrary some terms have a positive violation
		///           the violation of this is their sum.
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
				if (num3 > 0)
				{
					num += num3;
				}
			}
			ls.SetExtraData(this, num);
			ls[this] = ((num > 0) ? num : RecomputeMaxNegativeViolations(ls));
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
			bool flag = num > 0;
			if (oldValue > 0)
			{
				num -= oldValue;
			}
			if (newValue > 0)
			{
				num += newValue;
			}
			ls.SetExtraData(this, num);
			if (num > 0)
			{
				ls[this] = num;
				return;
			}
			int num2 = ls[this];
			if (flag || (oldValue == num2 && newValue < num2))
			{
				ls[this] = RecomputeMaxNegativeViolations(ls);
			}
			else if (newValue > num2)
			{
				ls[this] = newValue;
			}
		}

		/// <summary> Recomputes the highest negative values
		/// </summary>
		private int RecomputeMaxNegativeViolations(LocalSearchSolver ls)
		{
			int num = int.MinValue;
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm expr in inputs)
			{
				int num2 = ls[expr];
				if (num2 < 0)
				{
					num = Math.Max(num, num2);
				}
			}
			return num;
		}

		/// <summary> Algorithm used when we have few inputs
		/// </summary>
		internal void NaiveRecomputationForSmallSize(LocalSearchSolver ls)
		{
			int num = int.MinValue;
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm expr in inputs)
			{
				num = BooleanFunction.And(num, ls[expr]);
			}
			ls[this] = num;
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			ValueWithGradients valueWithGradients = new ValueWithGradients(int.MinValue);
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm term in inputs)
			{
				ValueWithGradients gradients = ls.GetGradients(term);
				valueWithGradients = Gradients.And(valueWithGradients, gradients);
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
