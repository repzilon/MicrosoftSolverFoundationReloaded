using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// This is a forward only buffer
	/// </summary>
	internal class NoBufList<T> : IBufList<T>, IEnumerable<T>, IEnumerable, IDisposable
	{
		private bool _fDispose;

		private bool _fDone;

		private int _ceCur;

		private IEnumerator<T> _etorSrc;

		public T this[int i]
		{
			get
			{
				if (!TryGet(i, out var e))
				{
					throw new IndexOutOfRangeException();
				}
				return e;
			}
		}

		public int LowerCount => _ceCur;

		public NoBufList(IEnumerable<T> ebleSrc)
			: this(ebleSrc.GetEnumerator())
		{
			_fDispose = true;
			_ceCur = 0;
		}

		public NoBufList(IEnumerator<T> etorSrc)
		{
			_etorSrc = etorSrc;
		}

		public void Dispose()
		{
			_fDone = true;
			_ceCur = -1;
			if (_fDispose && _etorSrc != null)
			{
				_etorSrc.Dispose();
				_etorSrc = null;
			}
		}

		public bool TryGet(int ie, out T e)
		{
			if (ie >= _ceCur && !_fDone)
			{
				if (_etorSrc.MoveNext())
				{
					e = _etorSrc.Current;
					_ceCur++;
					return true;
				}
				Dispose();
			}
			e = default(T);
			return false;
		}

		public IEnumerator<T> GetEnumerator()
		{
			T e;
			for (int ieCur = 0; TryGet(ieCur, out e); ieCur++)
			{
				yield return e;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
