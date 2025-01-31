using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Exactly M of the N Boolean Term inputs must be true.
	/// </summary>
	internal sealed class ExactlyMOfN : MOfNConstraint
	{
		internal override string Name => "sExactlyMOfN";

		/// <summary> Exactly M of the N Boolean Term inputs must be true.
		/// </summary>
		internal ExactlyMOfN(ConstraintSystem solver, int M, params CspSolverTerm[] inputs)
			: base(solver, M, inputs)
		{
			Reset();
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			if (!PrePropagate(out conflict))
			{
				return false;
			}
			if (_M < _trueCount || _trueCount + _unresolved < _M)
			{
				_unresolved = 0;
				return Force(choice: false, out conflict);
			}
			if (_unresolved == 0)
			{
				return Force(_M == _trueCount, out conflict);
			}
			if (1 == base.Count)
			{
				bool choice;
				if (IsTrue)
				{
					if (_trueCount == _M)
					{
						choice = false;
					}
					else
					{
						if (_trueCount + _unresolved != _M)
						{
							return false;
						}
						choice = true;
					}
				}
				else
				{
					if (1 != _unresolved)
					{
						return false;
					}
					choice = _trueCount == _M;
				}
				int unresolved = _unresolved;
				_unresolved = 0;
				bool flag = false;
				for (int i = 0; i < unresolved; i++)
				{
					CspSolverTerm cspSolverTerm = _inputs[i];
					flag |= cspSolverTerm.Force(choice, out conflict);
					if (conflict != null)
					{
						break;
					}
				}
				return flag;
			}
			return false;
		}

		/// <summary> Represent this class.
		/// </summary>
		public override string ToString()
		{
			return "sExactlyMOfN[" + _trueCount.ToString(CultureInfo.InvariantCulture) + "]";
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.ExactlyMofN(_M, inputs);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.ExactlyMofN(_M, inputs);
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			int l = PseudoBooleanSum(ls);
			ls[this] = BooleanFunction.Equal(l, _M);
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			ValueWithGradients x = PseudoBooleanSumWithGradients(ls);
			ValueWithGradients v = Gradients.Equal(x, _M);
			ls.SetGradients(this, v);
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping,
		///           together with a suggestion of value for this subterm
		/// </summary>
		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			Random randomSource = ls.RandomSource;
			bool flag = target == 1;
			int num = NumberInputsSatisfied(ls);
			CspSolverTerm cspSolverTerm;
			int val;
			if (flag && num < _M)
			{
				cspSolverTerm = PickViolatedInput(ls);
				val = 1;
			}
			else if (flag && num > _M)
			{
				cspSolverTerm = PickSatisfiedInput(ls);
				val = 0;
			}
			else
			{
				cspSolverTerm = _inputs[randomSource.Next(_inputs.Length)];
				val = 1 - ls.GetIntegerValue(cspSolverTerm);
			}
			return CspSolverTerm.CreateFlipSuggestion(cspSolverTerm, val, randomSource);
		}
	}
}
