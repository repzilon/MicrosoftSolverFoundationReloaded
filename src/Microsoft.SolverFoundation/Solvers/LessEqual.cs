using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A boolean which is equal to whether a sequence of integers is ascending.
	/// </summary>
	internal sealed class LessEqual : CspInequality
	{
		/// <summary> Represent this class
		/// </summary>
		internal override string Name => "<=";

		/// <summary> A boolean which is equal to whether a sequence of integers is ascending.
		/// </summary>
		internal LessEqual(ConstraintSystem solver, params CspSolverTerm[] inputs)
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
			int num2 = _rgLo[0];
			int num3 = _rgHi[0];
			isFalse = false;
			isTrue = true;
			CspSolverTerm result = null;
			for (int i = 1; i < num; i++)
			{
				if (!isFalse && _rgHi[i] < num2)
				{
					result = _inputs[i];
					isTrue = false;
					isFalse = true;
				}
				num2 = Math.Max(num2, _rgLo[i]);
				if (isTrue)
				{
					isTrue = num3 <= _rgLo[i];
					num3 = _rgHi[i];
				}
			}
			if (!isTrue)
			{
				for (int j = 1; j < num; j++)
				{
					if (_rgLo[j] < _rgLo[j - 1])
					{
						_rgLo[j] = _rgLo[j - 1];
						_rgChanged[j] = true;
					}
				}
				int num4 = num - 2;
				while (0 <= num4)
				{
					if (_rgHi[num4 + 1] < _rgHi[num4])
					{
						_rgHi[num4] = _rgHi[num4 + 1];
						_rgChanged[num4] = true;
					}
					num4--;
				}
			}
			return result;
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.LessEqual(inputs);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.LessEqual(inputs);
		}

		/// <summary> Recompute the value of the term from the value of its inputs
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
					int l = integerValue;
					integerValue = ls.GetIntegerValue(_inputs[i]);
					num2 = BooleanFunction.And(num2, BooleanFunction.LessEqual(l, integerValue));
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
				ValueWithGradients x = integerGradients;
				integerGradients = ls.GetIntegerGradients(_inputs[i]);
				valueWithGradients = Gradients.And(valueWithGradients, Gradients.LessEqual(x, integerGradients));
			}
			ls.SetGradients(this, valueWithGradients);
		}
	}
}
