using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class InvocationBuilder : IDisposable
	{
		private InvocationBuilder _ibNextFree;

		private Invocation _invOld;

		private Expression _exprHeadNew;

		private List<Expression> _argsNew;

		private int _iargCurOld;

		private int _iargCurNew;

		private bool _fDiff;

		private static object _lock = new object();

		private static InvocationBuilder _ibFree;

		private static int _cibFree;

		public bool Diff
		{
			get
			{
				if (!_fDiff)
				{
					return _invOld.Head != _exprHeadNew;
				}
				return true;
			}
		}

		public int IargCur => _iargCurOld;

		public Expression HeadOld => _invOld.Head;

		public Expression HeadNew
		{
			get
			{
				return _exprHeadNew;
			}
			set
			{
				_exprHeadNew = value;
			}
		}

		public Expression ArgCur => _invOld[_iargCurOld];

		private bool AtEnd => _iargCurOld == _invOld.Arity;

		public int Count
		{
			get
			{
				if (!_fDiff)
				{
					return _invOld.Arity;
				}
				return _argsNew.Count;
			}
		}

		public Expression this[int iv]
		{
			get
			{
				if (!_fDiff)
				{
					return _invOld[iv];
				}
				return _argsNew[iv];
			}
			set
			{
				if (!_fDiff)
				{
					if (value == _invOld[iv])
					{
						return;
					}
					SetDiff();
				}
				_argsNew[iv] = value;
			}
		}

		internal Expression[] ArgsArray
		{
			get
			{
				if (!_fDiff)
				{
					return _invOld.ArgsArray;
				}
				return _argsNew.ToArray();
			}
		}

		public static InvocationBuilder GetBuilder(Invocation inv, bool fKeepAll)
		{
			InvocationBuilder invocationBuilder = null;
			lock (_lock)
			{
				if (_ibFree != null)
				{
					invocationBuilder = _ibFree;
					_ibFree = invocationBuilder._ibNextFree;
					_cibFree--;
					invocationBuilder._ibNextFree = null;
				}
			}
			if (invocationBuilder == null)
			{
				invocationBuilder = new InvocationBuilder(inv, fKeepAll);
			}
			else
			{
				invocationBuilder.Reset(inv, fKeepAll);
			}
			return invocationBuilder;
		}

		public void Dispose()
		{
			lock (_lock)
			{
				if (_cibFree < 100)
				{
					_ibNextFree = _ibFree;
					_ibFree = this;
					_cibFree++;
				}
			}
		}

		private InvocationBuilder(Invocation invOld, bool fKeepAll)
		{
			_invOld = invOld;
			_exprHeadNew = _invOld.Head;
			if (fKeepAll)
			{
				_iargCurOld = (_iargCurNew = _invOld.Arity);
			}
			else
			{
				_iargCurOld = -1;
			}
		}

		public void Reset(Invocation invOld, bool fKeepAll)
		{
			_invOld = invOld;
			_exprHeadNew = _invOld.Head;
			if (_argsNew != null)
			{
				_argsNew.Clear();
			}
			if (fKeepAll)
			{
				_iargCurOld = (_iargCurNew = _invOld.Arity);
			}
			else
			{
				_iargCurOld = -1;
				_iargCurNew = 0;
			}
			_fDiff = false;
		}

		public bool StartNextArg()
		{
			if (_iargCurOld >= _invOld.Arity)
			{
				return false;
			}
			if (!_fDiff && _iargCurOld >= _iargCurNew)
			{
				SetDiff();
			}
			_iargCurOld++;
			return _iargCurOld < _invOld.Arity;
		}

		public void AddNewArg(Expression arg)
		{
			if (_fDiff)
			{
				_argsNew.Add(arg);
			}
			else if (AtEnd || ArgCur != arg)
			{
				SetDiff();
				_argsNew.Add(arg);
			}
			else
			{
				_iargCurNew++;
			}
		}

		private void SetDiff()
		{
			if (_argsNew == null)
			{
				_argsNew = new List<Expression>();
			}
			for (int i = 0; i < _iargCurOld; i++)
			{
				_argsNew.Add(_invOld[i]);
			}
			_fDiff = true;
		}

		public Expression GetNew()
		{
			if (!_fDiff)
			{
				if (HeadNew == _invOld.Head)
				{
					return _invOld;
				}
				return _invOld.Apply(HeadNew);
			}
			return HeadNew.Invoke(_argsNew.ToArray());
		}

		public Invocation GetNewRaw()
		{
			if (!_fDiff)
			{
				if (HeadNew == _invOld.Head)
				{
					return _invOld;
				}
				return _invOld.Apply(HeadNew);
			}
			return HeadNew.InvokeRaw(fCanOwnArray: true, _argsNew.ToArray());
		}

		public void RemoveRange(int ivMin, int ivLim)
		{
			if (ivMin < ivLim)
			{
				if (!_fDiff)
				{
					SetDiff();
				}
				_argsNew.RemoveRange(ivMin, ivLim - ivMin);
			}
		}

		public void Insert(int iv, Expression expr)
		{
			if (!_fDiff)
			{
				SetDiff();
			}
			_argsNew.Insert(iv, expr);
		}

		public void SortArgs()
		{
			Expression[] array = new Expression[Count];
			for (int i = 0; i < Count; i++)
			{
				array[i] = this[i];
			}
			Array.Sort(array, OrderSymbol.Compare);
			for (int j = 0; j < Count; j++)
			{
				this[j] = array[j];
			}
		}
	}
}
