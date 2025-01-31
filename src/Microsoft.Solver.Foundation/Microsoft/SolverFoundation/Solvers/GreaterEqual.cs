using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A boolean which is equal to whether a sequence of integers is descending.
	/// </summary>
	internal sealed class GreaterEqual : CspInequality
	{
		/// <summary> Represent this class
		/// </summary>
		internal override string Name => ">=";

		/// <summary> A boolean which is equal to whether a sequence of integers is descending.
		/// </summary>
		internal GreaterEqual(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, inputs)
		{
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		internal override CspSolverTerm Scan(out bool isFalse, out bool isTrue)
		{
			int num = _rgLo.Length;
			int num2 = _rgHi[num - 1];
			int num3 = _rgLo[num - 1];
			isFalse = false;
			isTrue = true;
			CspSolverTerm result = null;
			int num4 = num - 2;
			while (0 <= num4)
			{
				if (!isFalse && _rgHi[num4] < num3)
				{
					result = _inputs[num4];
					isTrue = false;
					isFalse = true;
				}
				num3 = Math.Max(num3, _rgLo[num4]);
				if (isTrue)
				{
					isTrue = num2 <= _rgLo[num4];
					num2 = _rgHi[num4];
				}
				num4--;
			}
			if (!isTrue)
			{
				for (int i = 1; i < num; i++)
				{
					if (_rgHi[i] > _rgHi[i - 1])
					{
						_rgHi[i] = _rgHi[i - 1];
						_rgChanged[i] = true;
					}
				}
				int num5 = num - 2;
				while (0 <= num5)
				{
					if (_rgLo[num5] < _rgLo[num5 + 1])
					{
						_rgLo[num5] = _rgLo[num5 + 1];
						_rgChanged[num5] = true;
					}
					num5--;
				}
			}
			return result;
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.GreaterEqual(inputs);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.GreaterEqual(inputs);
		}

		/// <summary> Naive recomputation: Recompute the value of the term 
		///           from the value of all its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
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
				int integerValue = ls.GetIntegerValue(_inputs[0]);
				for (int i = 1; i < num; i++)
				{
					int r = integerValue;
					integerValue = ls.GetIntegerValue(_inputs[i]);
					num2 = BooleanFunction.And(num2, BooleanFunction.LessEqual(integerValue, r));
				}
			}
			ls[this] = num2;
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			if (_inputs.Length < 2)
			{
				ls.CancelGradients(this);
				return;
			}
			ValueWithGradients valueWithGradients = new ValueWithGradients(int.MinValue);
			ValueWithGradients integerGradients = ls.GetIntegerGradients(_inputs[0]);
			for (int i = 1; i < _inputs.Length; i++)
			{
				ValueWithGradients y = integerGradients;
				integerGradients = ls.GetIntegerGradients(_inputs[i]);
				valueWithGradients = Gradients.And(valueWithGradients, Gradients.LessEqual(integerGradients, y));
			}
			ls.SetGradients(this, valueWithGradients);
		}
	}
}
