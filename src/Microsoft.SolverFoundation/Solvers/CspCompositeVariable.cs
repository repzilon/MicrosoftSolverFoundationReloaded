using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	internal sealed class CspCompositeVariable : CspVariable
	{
		private ConstraintSystem _solver;

		private CspComposite _domain;

		private List<object> _keys;

		private Dictionary<object, CspTerm[]> _fieldVarInternal;

		internal CspComposite DomainComposite => _domain;

		public override bool IsBoolean => false;

		public override CspDomain.ValueKind Kind
		{
			get
			{
				throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
			}
		}

		public override IEnumerable<object> CurrentValues
		{
			get
			{
				throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
			}
		}

		internal override CspSolverDomain BaseValueSet
		{
			get
			{
				throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
			}
		}

		internal override int First
		{
			get
			{
				throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
			}
		}

		public override IEnumerable<CspTerm> Inputs
		{
			get
			{
				throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
			}
		}

		internal override bool IsTrue
		{
			get
			{
				throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
			}
		}

		internal override int Last
		{
			get
			{
				throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
			}
		}

		internal override int OutputScale
		{
			get
			{
				throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
			}
		}

		internal override CspSymbolDomain Symbols
		{
			get
			{
				throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
			}
		}

		internal CspCompositeVariable(ConstraintSystem solver, CspComposite domain)
			: this(solver, domain, null)
		{
		}

		internal CspCompositeVariable(ConstraintSystem solver, CspComposite domain, object key)
			: base(solver, domain, key)
		{
			domain.Freeze();
			_solver = solver;
			_domain = domain;
			_keys = new List<object>();
			_fieldVarInternal = new Dictionary<object, CspTerm[]>();
			List<CspComposite.Tuple> allFields = _domain.AllFields;
			Dictionary<CspTerm, CspTerm> dictionary = new Dictionary<CspTerm, CspTerm>();
			foreach (CspComposite.Tuple item in allFields)
			{
				StringBuilder stringBuilder = ((key != null) ? new StringBuilder(key.ToString()) : new StringBuilder());
				stringBuilder.Append(".");
				stringBuilder.Append(item.Key.ToString());
				stringBuilder.Append(".");
				CspTerm[] array = ((item.Domain != _solver.DefaultBoolean) ? _solver.CreateVariableVector(item.Domain, stringBuilder, item.Arity) : _solver.CreateBooleanVector(stringBuilder, item.Arity));
				for (int i = 0; i < array.Length; i++)
				{
					dictionary.Add(item.Vars[i], array[i]);
				}
				_keys.Add(item.Key);
				_fieldVarInternal.Add(item.Key, array);
			}
			ConstraintSystem.AppendModel(_domain.ConstraintContainer, _solver, dictionary);
			solver.AddCompositeVariable(this);
		}

		public override IEnumerable<CspTerm> Fields(object key)
		{
			if (!_fieldVarInternal.ContainsKey(key))
			{
				throw new ArgumentException(Resources.CompositeInvalidFieldReference + ((key != null) ? key.ToString() : "null"));
			}
			_fieldVarInternal.TryGetValue(key, out var vars);
			try
			{
				CspTerm[] array = vars;
				for (int i = 0; i < array.Length; i++)
				{
					yield return array[i];
				}
			}
			finally
			{
			}
		}

		public override CspTerm Field(object key, int index)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException(Resources.CompositeIndexOutOfRange + index.ToString(CultureInfo.InvariantCulture));
			}
			int num = 0;
			foreach (CspTerm item in Fields(key))
			{
				if (num == index)
				{
					return item;
				}
				num++;
			}
			throw new ArgumentOutOfRangeException(Resources.CompositeInvalidFieldReference + ((key != null) ? key.ToString() : "null"));
		}

		internal CspTerm[] FieldsInternal(object key)
		{
			if (!_fieldVarInternal.ContainsKey(key))
			{
				throw new ArgumentOutOfRangeException(Resources.CompositeInvalidFieldReference + ((key != null) ? key.ToString() : "null"));
			}
			_fieldVarInternal.TryGetValue(key, out var value);
			return value;
		}

		internal CspTerm FieldInternal(object key, int index)
		{
			CspTerm[] array = FieldsInternal(key);
			if (index < 0 || index >= array.Length)
			{
				throw new ArgumentOutOfRangeException(Resources.CompositeIndexOutOfRange + ((key != null) ? key.ToString() : "null"));
			}
			return array[index];
		}

		/// <summary> String representation of this Composite variable
		/// </summary>
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (Key != this)
			{
				stringBuilder.Append(Key.ToString());
			}
			else
			{
				stringBuilder.Append("CompositeVar");
			}
			stringBuilder.Append("{ ");
			foreach (object key in _keys)
			{
				stringBuilder.Append("[ ");
				CspTerm[] array = FieldsInternal(key);
				for (int i = 0; i < 3 && i < array.Length; i++)
				{
					stringBuilder.Append(array[i].ToString());
					stringBuilder.Append("; ");
				}
				if (array.Length > 3)
				{
					stringBuilder.Append("... ");
				}
				stringBuilder.Append("]; ");
			}
			stringBuilder.Append(" }");
			return stringBuilder.ToString();
		}

		public override void Accept(IVisitor visitor)
		{
			throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
		}

		internal override IEnumerable<int> Backward()
		{
			throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
		}

		internal override IEnumerable<int> Backward(int last, int first)
		{
			throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
		}

		internal override bool Contains(int val)
		{
			throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
		}

		internal override IEnumerable<int> Forward()
		{
			throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
		}

		internal override IEnumerable<int> Forward(int first, int last)
		{
			throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
		}

		internal override bool IsWatched(CspSolverTerm var)
		{
			throw new InvalidOperationException(Resources.InvalidCompositeVariableOperation);
		}
	}
}
