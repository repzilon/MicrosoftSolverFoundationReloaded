using System;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A Term representing indexing a decision or parameter table.
	/// </summary>
	internal sealed class IndexTerm : Term
	{
		/// <summary>
		/// The indexes being used
		/// </summary>
		internal readonly Term[] _inputs;

		/// <summary>
		/// The term representing the decision or parameter being indexed into.
		/// </summary>
		internal readonly IIndexable _table;

		/// <summary>
		/// The type of the single element (Boolean, numeric, etc.)
		/// </summary>
		private readonly TermValueClass _valueClass;

		internal override bool IsModelIndependentTerm => _owningModel == null;

		internal override TermType TermType => TermType.Index;

		internal override TermValueClass ValueClass => _valueClass;

		internal override Domain EnumeratedDomain => ((Term)_table).EnumeratedDomain;

		/// <summary>
		/// Construct a term representing indexing a table to get a specific element.
		/// </summary>
		/// <param name="table">The decision or parameter being indexed. Must have a ValueClass of Table.</param>
		/// <param name="owningModel">The model that this IndexTerm should be added to.</param>
		/// <param name="inputs">The indexes. Must match the index sets of the table.</param>
		/// <param name="valueClass">The ValueClass of the resulting element.</param>
		internal IndexTerm(IIndexable table, Model owningModel, Term[] inputs, TermValueClass valueClass)
		{
			_table = table;
			_inputs = inputs;
			_owningModel = owningModel;
			_valueClass = valueClass;
			bool flag = true;
			foreach (Term term in inputs)
			{
				if (!term.HasStructure(TermStructure.Constant))
				{
					flag = false;
				}
			}
			_structure = ((Term)table)._structure;
			if (!flag)
			{
				_structure &= TermStructure.Integer;
			}
		}

		internal override Term Clone(string baseName)
		{
			throw new NotSupportedException(Resources.CannotCloneTerm);
		}

		internal override bool TryEvaluateConstantValue(out Rational value, EvaluationContext context)
		{
			object[] array = new object[_inputs.Length];
			for (int i = 0; i < _inputs.Length; i++)
			{
				if (!_inputs[i].TryEvaluateConstantValue(out array[i], context))
				{
					value = 0;
					return false;
				}
			}
			return _table.TryEvaluateConstantValue(array, out value, context);
		}

		/// <summary>Returns a string that represents the current Decision.
		/// </summary>
		/// <returns>The value of the current instance in the specified format.</returns>
		public override string ToString()
		{
			return ToString(null, null);
		}

		/// <summary>Formats the value of the current instance using the specified format.
		/// </summary>
		/// <param name="format">The format to use (or null).</param>
		/// <param name="formatProvider">The provider to use to format the value (or null).</param>
		/// <returns>The value of the current instance in the specified format.</returns>
		public override string ToString(string format, IFormatProvider formatProvider)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(_table.Name);
			stringBuilder.Append("[");
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
			stringBuilder.Append("]");
			return stringBuilder.ToString();
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}
	}
}
