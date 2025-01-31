using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class ValueTableAdapter : Expression
	{
		internal ValueTable<double> _table;

		public override Expression Head => base.Rewrite.Builtin.Root;

		public int IndexCount => _table.IndexCount;

		public ValueTableAdapter(RewriteSystem rs, Domain domain, ValueSet[] indexSets)
			: base(rs)
		{
			_table = ValueTable<double>.Create(domain, indexSets);
		}

		public ValueTableAdapter(RewriteSystem rs, Domain domain, ValueSetAdapter[] indexSetAdapters)
			: base(rs)
		{
			ValueSet[] array = new ValueSet[indexSetAdapters.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = indexSetAdapters[i]._set;
			}
			_table = ValueTable<double>.Create(domain, array);
		}

		public ValueTableAdapter(RewriteSystem rs, ValueTable<double> table)
			: base(rs)
		{
			_table = table;
		}

		public override bool Equivalent(Expression expr)
		{
			if (!(expr is ValueTableAdapter valueTableAdapter))
			{
				return false;
			}
			return valueTableAdapter._table == _table;
		}

		public override int GetEquivalenceHash()
		{
			return GetHashCode();
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != _table._indexSets.Length)
			{
				throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.ExpectingIndexes0, new object[1] { _table._indexSets.Length }), this, OmlParseExceptionReason.InvalidIndexCount);
			}
			object[] array = new object[ib.Count];
			for (int i = 0; i < ib.Count; i++)
			{
				object exprValue = ValueTable<double>.GetExprValue(ib.ArgsArray[i]);
				if (exprValue == null)
				{
					return null;
				}
				array[i] = exprValue;
			}
			if (_table.TryGetValue(out var value, array))
			{
				return RationalConstant.Create(base.Rewrite, value);
			}
			string text = string.Join(", ", array.Select((object o) => o.ToString()).ToArray());
			if (array.Length > 1)
			{
				throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.IndexesCanNotBeFound0, new object[1] { text }), this, OmlParseExceptionReason.InvalidIndex);
			}
			throw new ModelClauseException(string.Format(CultureInfo.InvariantCulture, Resources.IndexCanNotBeFound0, new object[1] { text }), this, OmlParseExceptionReason.InvalidIndex);
		}

		public void Add(double value, params object[] indexes)
		{
			_table.Add(value, indexes);
		}
	}
}
