using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class UnequalTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Unequal;

		internal UnequalTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Unequal;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			for (int i = 0; i < inputs.Length - 1; i++)
			{
				for (int j = i + 1; j < inputs.Length; j++)
				{
					if (inputs[i] == inputs[j])
					{
						return 0;
					}
				}
			}
			return 1;
		}

		internal override bool TryEvaluateConstantValue(out Rational value, EvaluationContext context)
		{
			Term[] inputs = _inputs;
			foreach (Term term in inputs)
			{
				if (term.ValueClass != TermValueClass.String)
				{
					return base.TryEvaluateConstantValue(out value, context);
				}
			}
			List<object> list = new List<object>();
			foreach (Term item in AllInputs(context))
			{
				if (!item.TryEvaluateConstantValue(out object value2, context))
				{
					value = Rational.Zero;
					return false;
				}
				list.Add(value2);
			}
			list.Sort();
			value = 1.0;
			for (int j = 1; j < list.Count; j++)
			{
				if (list[j - 1].Equals(list[j]))
				{
					value = 0.0;
					break;
				}
			}
			return true;
		}
	}
}
