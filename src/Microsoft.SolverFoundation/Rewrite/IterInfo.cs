using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class IterInfo
	{
		protected struct Number
		{
			public bool _fFloat;

			public Rational _rat;

			public double _dbl;

			public double GetDouble()
			{
				if (!_fFloat)
				{
					_fFloat = true;
					_dbl = (double)_rat;
				}
				return _dbl;
			}
		}

		protected Symbol _sym;

		protected int _ivNext;

		protected int _cv;

		protected Number _numMin;

		protected Number _numLim;

		protected Number _numIncr;

		protected Invocation _invList;

		protected IEnumerator<Expression> _ator;

		public Symbol Sym => _sym;

		public IterInfo()
		{
			_numIncr._rat = 1;
		}

		protected static bool GetNumber(Expression expr, bool fFinite, ref Number num)
		{
			if (expr.GetValue(out num._rat))
			{
				if (!fFinite)
				{
					return num._rat.HasSign;
				}
				return num._rat.IsFinite;
			}
			if (expr.GetValue(out num._dbl))
			{
				num._fFloat = true;
				if (!fFinite)
				{
					return !double.IsNaN(num._dbl);
				}
				return NumberUtils.IsFinite(num._dbl);
			}
			return false;
		}

		protected static bool GetSymbol(Expression expr, ref Symbol sym)
		{
			sym = expr as Symbol;
			return sym != null;
		}

		protected bool GetCount(bool fFinite, ref int cv)
		{
			if (_numLim._fFloat || _numMin._fFloat || _numIncr._fFloat)
			{
				if (_numIncr.GetDouble() == 0.0)
				{
					return false;
				}
				double num = Math.Ceiling((_numLim.GetDouble() - _numMin.GetDouble()) / _numIncr.GetDouble());
				if (double.IsNaN(num))
				{
					return false;
				}
				if (num <= 0.0)
				{
					cv = 0;
				}
				else if (num >= 2147483647.0)
				{
					cv = int.MaxValue;
				}
				else
				{
					cv = (int)num;
				}
			}
			else
			{
				Rational rational = (_numLim._rat - _numMin._rat) / _numIncr._rat;
				if (rational.IsIndeterminate)
				{
					return false;
				}
				if (rational <= 0)
				{
					cv = 0;
				}
				else if (rational >= int.MaxValue)
				{
					cv = int.MaxValue;
				}
				else if ((cv = (int)rational) != rational)
				{
					cv++;
				}
			}
			return true;
		}

		public bool Parse(Expression expr, bool fFinite)
		{
			if (ParseCore(expr, fFinite))
			{
				return true;
			}
			string text = string.Empty;
			if (expr != null && expr.PlacementInformation != null)
			{
				expr.PlacementInformation.Map.MapSpanToPos(expr.PlacementInformation.Span, out var spos);
				text = string.Format(CultureInfo.InvariantCulture, "({0},{1})-({2},{3}): ", spos.lineMin, spos.colMin, spos.lineLim, spos.colLim);
			}
			text += string.Format(CultureInfo.InvariantCulture, Resources.ParsingModelFailed01, new object[2]
			{
				Resources.BadIterator,
				expr
			});
			expr.Rewrite.Log(text);
			return false;
		}

		protected bool ParseCore(Expression expr, bool fFinite)
		{
			_sym = null;
			_ivNext = 0;
			_numMin = default(Number);
			_numLim = default(Number);
			_numIncr = default(Number);
			_numIncr._rat = 1;
			_invList = null;
			_ator = null;
			if (expr.Head != expr.Rewrite.Builtin.List)
			{
				if (GetNumber(expr, fFinite: true, ref _numLim))
				{
					return GetCount(fFinite: true, ref _cv);
				}
				return false;
			}
			Invocation invocation = (Invocation)expr;
			switch (invocation.Arity)
			{
			case 1:
				if (GetNumber(invocation[0], fFinite, ref _numLim))
				{
					return GetCount(fFinite, ref _cv);
				}
				return false;
			case 2:
				if (!GetSymbol(invocation[0], ref _sym))
				{
					return false;
				}
				if (invocation[1].Head == expr.Rewrite.Builtin.List)
				{
					_invList = (Invocation)invocation[1];
					return true;
				}
				if (invocation[1] is ExprSequence exprSequence)
				{
					_ator = exprSequence.GetEnumerator();
					return true;
				}
				if (GetNumber(invocation[1], fFinite, ref _numLim))
				{
					return GetCount(fFinite, ref _cv);
				}
				return false;
			case 3:
				if (GetSymbol(invocation[0], ref _sym) && GetNumber(invocation[1], fFinite: true, ref _numMin) && GetNumber(invocation[2], fFinite, ref _numLim))
				{
					return GetCount(fFinite, ref _cv);
				}
				return false;
			case 4:
				if (GetSymbol(invocation[0], ref _sym) && GetNumber(invocation[1], fFinite: true, ref _numMin) && GetNumber(invocation[2], fFinite, ref _numLim) && GetNumber(invocation[3], fFinite: true, ref _numIncr))
				{
					return GetCount(fFinite, ref _cv);
				}
				return false;
			default:
				return false;
			}
		}

		public Expression GetNext(RewriteSystem rs)
		{
			if (_ator != null)
			{
				if (!_ator.MoveNext())
				{
					_ator.Dispose();
					return null;
				}
				return _ator.Current;
			}
			if (_invList != null)
			{
				if (_ivNext >= _invList.Arity)
				{
					return null;
				}
				return _invList[_ivNext++];
			}
			if (_ivNext >= _cv)
			{
				return null;
			}
			if (_numIncr._fFloat)
			{
				return new FloatConstant(rs, _numMin.GetDouble() + (double)_ivNext++ * _numIncr.GetDouble());
			}
			return RationalConstant.Create(rs, _numMin._rat + _ivNext++ * _numIncr._rat);
		}
	}
}
