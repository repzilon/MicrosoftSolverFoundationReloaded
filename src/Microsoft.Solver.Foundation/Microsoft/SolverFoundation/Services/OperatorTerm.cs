using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A term representing a mathematical operation, such as addition or equality.
	/// </summary>
	internal abstract class OperatorTerm : Term
	{
		internal readonly Term[] _inputs;

		/// <summary>
		/// The type of value (Boolean, numeric, etc.), which is determined by the operation and the inputs.
		/// </summary>
		private readonly TermValueClass _valueClass;

		/// <summary>
		/// The mathematical operation this term performs
		/// </summary>
		internal abstract Operator Operation { get; }

		/// <summary>
		/// The inputs of the operation. An input might be a ForEachTerm which represents multiple values.
		/// </summary>
		internal Term[] Inputs => _inputs;

		internal override bool IsModelIndependentTerm => _owningModel == null;

		internal override TermType TermType => TermType.Operator;

		internal override TermValueClass ValueClass => _valueClass;

		internal virtual bool IsAssociativeAndCommutative
		{
			[DebuggerStepThrough]
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Construct a term representing a mathematical operation
		/// </summary>
		/// <param name="inputs">The inputs of the operation. An input can be a ForEachTerm representing multiple values.</param>
		/// <param name="valueClass">The value class of the result, which is determined by the operation and inputs.</param>
		internal OperatorTerm(Term[] inputs, TermValueClass valueClass)
		{
			_inputs = inputs;
			for (int i = 0; i < inputs.Length; i++)
			{
				if (!inputs[i].IsModelIndependentTerm)
				{
					_owningModel = inputs[i]._owningModel;
					break;
				}
			}
			TermStructure termStructure = InputStructure(inputs, TermStructure.Constant | TermStructure.Integer);
			_structure |= termStructure;
			if ((termStructure & TermStructure.Constant) != 0)
			{
				_structure |= TermStructure.Linear | TermStructure.Quadratic | TermStructure.Differentiable;
				if (valueClass == TermValueClass.Numeric)
				{
					_structure |= TermStructure.LinearConstraint | TermStructure.DifferentiableConstraint;
				}
			}
			_valueClass = valueClass;
		}

		internal static TermStructure InputStructure(Term[] inputs, TermStructure termStructure)
		{
			TermStructure termStructure2 = termStructure;
			for (int i = 0; i < inputs.Length; i++)
			{
				termStructure2 &= inputs[i].Structure;
			}
			return termStructure2;
		}

		/// <summary>
		/// Get the NL Symbol representing the mathematical operation this Term performs
		/// </summary>
		/// <param name="rs">The RewriteSystem the symbol should belong to</param>
		/// <returns>A Symbol</returns>
		protected abstract Symbol GetHeadSymbol(RewriteSystem rs);

		internal override Term Clone(string baseName)
		{
			throw new NotSupportedException(Resources.CannotCloneTerm);
		}

		internal override bool TryEvaluateConstantValue(out Rational value, EvaluationContext context)
		{
			List<Rational> list = new List<Rational>();
			Stack<OperatorTerm> stack = new Stack<OperatorTerm>();
			stack.Push(this);
			while (stack.Count > 0)
			{
				OperatorTerm operatorTerm = stack.Pop();
				for (int i = 0; i < operatorTerm._inputs.Length; i++)
				{
					Term term = operatorTerm._inputs[i];
					ForEachTerm forEachTerm = term as ForEachTerm;
					OperatorTerm operatorTerm2 = term as OperatorTerm;
					if ((object)forEachTerm != null)
					{
						if (!forEachTerm.TryEvaluateConstantValueList(list, context))
						{
							value = 0;
							return false;
						}
						continue;
					}
					if (IsAssociativeAndCommutative && (object)operatorTerm2 != null && operatorTerm2.Operation == Operation)
					{
						stack.Push(operatorTerm2);
						continue;
					}
					if (!term.TryEvaluateConstantValue(out Rational value2, context))
					{
						value = 0;
						return false;
					}
					list.Add(value2);
				}
			}
			value = Evaluate(list.ToArray());
			return true;
		}

		internal abstract Rational Evaluate(Rational[] inputs);

		internal IEnumerable<Term> AllInputs(EvaluationContext context)
		{
			if (IsAssociativeAndCommutative)
			{
				Stack<OperatorTerm> worklist = new Stack<OperatorTerm>();
				worklist.Push(this);
				while (worklist.Count > 0)
				{
					OperatorTerm currentTerm = worklist.Pop();
					try
					{
						Term[] inputs = currentTerm._inputs;
						foreach (Term input in inputs)
						{
							if (input is OperatorTerm operatorInput && operatorInput.Operation == Operation)
							{
								worklist.Push(operatorInput);
								continue;
							}
							foreach (Term item in input.AllValues(context))
							{
								yield return item;
							}
						}
					}
					finally
					{
					}
				}
				yield break;
			}
			try
			{
				Term[] inputs2 = _inputs;
				foreach (Term input2 in inputs2)
				{
					foreach (Term item2 in input2.AllValues(context))
					{
						yield return item2;
					}
				}
			}
			finally
			{
			}
		}

		/// <summary>Formats the value of the current instance using the specified format.
		/// </summary>
		/// <param name="format">The format to use (or null).</param>
		/// <param name="formatProvider">The provider to use to format the value (or null).</param>
		/// <returns>The value of the current instance in the specified format.</returns>
		public override string ToString(string format, IFormatProvider formatProvider)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(Operation.ToString());
			stringBuilder.Append("(");
			int num = 0;
			Term[] inputs = _inputs;
			foreach (Term term in inputs)
			{
				if (num > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(term.ToString(format, formatProvider));
				num++;
				if (num >= 5)
				{
					stringBuilder.Append(", ...");
					break;
				}
			}
			stringBuilder.Append(")");
			return stringBuilder.ToString();
		}

		/// <summary>Returns a string that represents the current Decision.
		/// </summary>
		/// <returns>The value of the current instance in the specified format.</returns>
		public override string ToString()
		{
			return ToString(null, null);
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}
	}
}
