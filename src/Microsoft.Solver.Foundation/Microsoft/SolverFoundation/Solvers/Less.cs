using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A boolean which is equal to whether a sequence of integers is strictly ascending.
	/// </summary>
	internal sealed class Less : CspInequality
	{
		/// <summary> Represent this class
		/// </summary>
		internal override string Name => "<";

		/// <summary> A boolean which is equal to whether a sequence of integers is strictly ascending.
		/// </summary>
		internal Less(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, inputs)
		{
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		internal override CspSolverTerm Scan(out bool isFalse, out bool isTrue)
		{
			int width = Width;
			int num = _rgLo[0];
			int num2 = _rgHi[0];
			isFalse = false;
			isTrue = true;
			CspSolverTerm result = null;
			for (int i = 1; i < width; i++)
			{
				if (!isFalse && _rgHi[i] <= num)
				{
					result = _inputs[i];
					isTrue = false;
					isFalse = true;
				}
				num = Math.Max(num, _rgLo[i]);
				if (isTrue)
				{
					isTrue = num2 < _rgLo[i];
					num2 = _rgHi[i];
				}
			}
			if (!isTrue)
			{
				for (int j = 1; j < width; j++)
				{
					if (_rgLo[j] <= _rgLo[j - 1])
					{
						_rgLo[j] = _rgLo[j - 1] + 1;
						_rgChanged[j] = true;
					}
				}
				int num3 = width - 2;
				while (0 <= num3)
				{
					if (_rgHi[num3 + 1] <= _rgHi[num3])
					{
						_rgHi[num3] = _rgHi[num3 + 1] - 1;
						_rgChanged[num3] = true;
					}
					num3--;
				}
			}
			return result;
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.Less(inputs);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.Less(inputs);
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
					num2 = BooleanFunction.And(num2, BooleanFunction.LessStrict(l, integerValue));
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
				valueWithGradients = Gradients.And(valueWithGradients, Gradients.LessStrict(x, integerGradients));
			}
			ls.SetGradients(this, valueWithGradients);
		}
	}
}
