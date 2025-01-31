using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary> Implement small mappings (few variables). It uses linear search
	/// to resolve the mappings.
	/// </summary>
	internal class Substitution
	{
		private struct Map
		{
			public Symbol _sym;

			public Expression _expr;

			public bool _fMulti;

			public bool _fIdentity;

			public Map(Symbol sym, Expression expr, bool fMulti)
			{
				_sym = sym;
				_expr = expr;
				_fMulti = fMulti;
				_fIdentity = !_fMulti && expr == sym;
			}
		}

		private List<Map> _rgmap;

		public Substitution()
		{
			_rgmap = new List<Map>();
		}

		public void Clear()
		{
			_rgmap.Clear();
		}

		public bool Add(Symbol sym, Expression expr)
		{
			if (MapsSymbol(sym, out int imap))
			{
				if (_rgmap[imap]._expr.Equivalent(expr))
				{
					return !_rgmap[imap]._fMulti;
				}
				return false;
			}
			_rgmap.Add(new Map(sym, expr, fMulti: false));
			return true;
		}

		public bool AddList(Symbol sym, Invocation inv, int iargMin, int iargLim)
		{
			if (iargLim == iargMin + 1)
			{
				return Add(sym, inv[iargMin]);
			}
			if (MapsSymbol(sym, out int imap))
			{
				Map map = _rgmap[imap];
				if (!map._fMulti || map._expr.Arity != iargLim - iargMin)
				{
					return false;
				}
				for (int i = 0; i < map._expr.Arity; i++)
				{
					if (!map._expr[i].Equivalent(inv[i + iargMin]))
					{
						return false;
					}
				}
				return true;
			}
			Expression[] array = new Expression[iargLim - iargMin];
			for (int j = iargMin; j < iargLim; j++)
			{
				array[j - iargMin] = inv[j];
			}
			_rgmap.Add(new Map(sym, inv.Rewrite.Builtin.ArgumentSplice.Invoke(array), fMulti: true));
			return true;
		}

		public bool MapsSymbol(Symbol sym, out Expression expr)
		{
			for (int i = 0; i < _rgmap.Count; i++)
			{
				if (_rgmap[i]._sym == sym)
				{
					expr = _rgmap[i]._expr;
					return true;
				}
			}
			expr = null;
			return false;
		}

		private bool MapsSymbol(Symbol sym, out int imap)
		{
			for (imap = 0; imap < _rgmap.Count; imap++)
			{
				if (_rgmap[imap]._sym == sym)
				{
					return true;
				}
			}
			return false;
		}

		public int PushMark()
		{
			return _rgmap.Count;
		}

		public void PopToMark(int mark)
		{
			if (mark < 0 || mark > _rgmap.Count)
			{
				throw new InvalidOperationException(Resources.BadMark);
			}
			Statics.TrimList(_rgmap, mark);
		}

		public override string ToString()
		{
			if (_rgmap == null || _rgmap.Count == 0)
			{
				return "{}";
			}
			StringBuilder stringBuilder = new StringBuilder();
			char value = '{';
			foreach (Map item in _rgmap)
			{
				stringBuilder.Append(value);
				value = ',';
				stringBuilder.Append(item._sym);
				stringBuilder.Append("->");
				stringBuilder.Append(item._expr);
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		public Expression Apply(Expression expr)
		{
			if (_rgmap == null || expr == null)
			{
				return expr;
			}
			return ApplyCore(expr, null);
		}

		public Expression Apply(Expression expr, Func<Symbol, bool> fnOkToMap)
		{
			if (_rgmap == null || expr == null)
			{
				return expr;
			}
			return ApplyCore(expr, fnOkToMap);
		}

		private Expression ApplyCore(Expression expr, Func<Symbol, bool> fnOkToMap)
		{
			if (expr is Invocation inv)
			{
				return MapInvocation(inv, fnOkToMap);
			}
			if (expr is Symbol symbol && MapsSymbol(symbol, out int imap) && (fnOkToMap == null || fnOkToMap(symbol)))
			{
				return _rgmap[imap]._expr;
			}
			return expr;
		}

		private void MapArg(Expression expr, InvocationBuilder ib, Func<Symbol, bool> fnOkToMap)
		{
			if (expr is Invocation inv)
			{
				ib.AddNewArg(MapInvocation(inv, fnOkToMap));
				return;
			}
			if (expr is Symbol symbol && MapsSymbol(symbol, out int imap) && (fnOkToMap == null || fnOkToMap(symbol)))
			{
				Map map = _rgmap[imap];
				if (map._fMulti)
				{
					for (int i = 0; i < map._expr.Arity; i++)
					{
						ib.AddNewArg(map._expr[i]);
					}
					return;
				}
				if (symbol != map._expr)
				{
					ib.AddNewArg(map._expr);
					return;
				}
			}
			ib.AddNewArg(expr);
		}

		private Expression MapInvocation(Invocation inv, Func<Symbol, bool> fnOkToMap)
		{
			using (InvocationBuilder invocationBuilder = InvocationBuilder.GetBuilder(inv, fKeepAll: false))
			{
				invocationBuilder.HeadNew = ApplyCore(invocationBuilder.HeadOld, fnOkToMap);
				while (invocationBuilder.StartNextArg())
				{
					MapArg(invocationBuilder.ArgCur, invocationBuilder, fnOkToMap);
				}
				return invocationBuilder.GetNew();
			}
		}

		public static Expression operator *(Expression expr, Substitution sub)
		{
			return sub.Apply(expr);
		}
	}
}
