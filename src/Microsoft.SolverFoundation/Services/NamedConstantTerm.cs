using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A NamedConstantTerm represents a named constant ("Parameters[..., P = constant]" in OML), or
	/// a simple function of one or more inputs.
	///
	/// It can be indexed just like a Parameter.
	///
	/// It is possible that the value of a NamedConstantTerm could depend on the value of a Parameter
	/// or another NamedConstantTerm. Ensuring that there are no loops is the job of the OML importer.
	/// </summary>
	internal class NamedConstantTerm : Term, IIndexable
	{
		private readonly string _name;

		internal Domain _domain;

		internal IterationTerm[] _indexes;

		internal Set[] _indexSets;

		internal Term _innerTerm;

		internal override bool IsModelIndependentTerm => true;

		internal override TermType TermType => TermType.NamedConstant;

		internal override TermValueClass ValueClass
		{
			get
			{
				if (_indexSets.Length > 0)
				{
					return TermValueClass.Table;
				}
				return TermValueClass.Numeric;
			}
		}

		public string Name => _name;

		TermValueClass IIndexable.ValueClass => ValueClass;

		TermValueClass IIndexable.DomainValueClass => TermValueClass.Numeric;

		Set[] IIndexable.IndexSets => _indexSets;

		internal NamedConstantTerm(string name, Term innerTerm, IterationTerm[] indexes, Set[] indexSets, Domain domain)
		{
			_name = name;
			_innerTerm = innerTerm;
			_indexSets = indexSets;
			_indexes = indexes;
			_domain = domain;
			_structure = innerTerm._structure;
		}

		bool IIndexable.TryEvaluateConstantValue(object[] indexes, out Rational value, EvaluationContext context)
		{
			if (indexes.Length != _indexes.Length)
			{
				value = 0;
				return false;
			}
			for (int i = 0; i < _indexes.Length; i++)
			{
				context.SetValue(_indexes[i], indexes[i]);
			}
			bool result = _innerTerm.TryEvaluateConstantValue(out value, context);
			for (int num = _indexes.Length - 1; num >= 0; num--)
			{
				context.ClearValue(_indexes[num]);
			}
			return result;
		}

		void IIndexable.SetIndexSet(int i, Set set)
		{
			_indexSets[i] = set;
		}

		internal override Term Clone(string baseName)
		{
			throw new NotSupportedException(Resources.CannotCloneTerm);
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}

		public override string ToString()
		{
			return _name;
		}

		internal override bool TryEvaluateConstantValue(out object value, EvaluationContext context)
		{
			return _innerTerm.TryEvaluateConstantValue(out value, context);
		}

		internal override bool TryEvaluateConstantValue(out Rational value, EvaluationContext context)
		{
			return _innerTerm.TryEvaluateConstantValue(out value, context);
		}
	}
}
