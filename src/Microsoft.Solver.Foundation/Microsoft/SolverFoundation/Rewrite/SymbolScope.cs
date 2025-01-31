using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class SymbolScope : IEnumerable<Symbol>, IEnumerable
	{
		private readonly SymbolScope _scopePar;

		private Expression _exprPar;

		private readonly Dictionary<string, Symbol> _mpstrsym;

		public int Count => _mpstrsym.Count;

		public SymbolScope Parent => _scopePar;

		public Expression ParentExpression
		{
			get
			{
				return _exprPar;
			}
			set
			{
				if (_exprPar != null)
				{
					throw new InvalidOperationException(Resources.SymbolScopeAlreadyBoundToAnExpression);
				}
				_exprPar = value;
			}
		}

		public SymbolScope(SymbolScope scopePar)
		{
			_scopePar = scopePar;
			_mpstrsym = new Dictionary<string, Symbol>();
		}

		public void AddSymbol(Symbol sym)
		{
			sym.Scope = this;
			_mpstrsym.Add(sym.Name, sym);
		}

		public void RemoveSymbol(Symbol sym)
		{
			if (sym.Scope == this)
			{
				sym.Scope = null;
				_mpstrsym.Remove(sym.Name);
			}
		}

		public void RemoveAll()
		{
			foreach (Symbol value in _mpstrsym.Values)
			{
				value.Scope = null;
			}
			_mpstrsym.Clear();
		}

		public bool GetSymbolThis(string name, out Symbol sym)
		{
			return _mpstrsym.TryGetValue(name, out sym);
		}

		public bool GetSymbolAll(string name, out Symbol sym)
		{
			if (_mpstrsym.TryGetValue(name, out sym))
			{
				return true;
			}
			if (_scopePar != null)
			{
				return _scopePar.GetSymbolAll(name, out sym);
			}
			return false;
		}

		public IEnumerator<Symbol> GetEnumerator()
		{
			foreach (Symbol value in _mpstrsym.Values)
			{
				yield return value;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
