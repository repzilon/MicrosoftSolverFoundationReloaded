using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Buffers items of type T from an IEnumerator.
	/// </summary>
	internal class BufList<T> : IBufList<T>, IEnumerable<T>, IEnumerable, IDisposable
	{
		private T[] _rge;

		private int _ceCur;

		private bool _fDone;

		private bool _fDispose;

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

		public BufList(IEnumerable<T> ebleSrc)
			: this(ebleSrc.GetEnumerator())
		{
			_fDispose = true;
		}

		public BufList(IEnumerator<T> etorSrc)
		{
			_rge = new T[10];
			_ceCur = 0;
			_etorSrc = etorSrc;
		}

		public void Dispose()
		{
			_fDone = true;
			if (_fDispose && _etorSrc != null)
			{
				_etorSrc.Dispose();
				_etorSrc = null;
			}
		}

		public bool TryGet(int ie, out T e)
		{
			while (true)
			{
				if (ie >= _ceCur)
				{
					if (_fDone)
					{
						break;
					}
					if (!_etorSrc.MoveNext())
					{
						Dispose();
						break;
					}
					if (_ceCur >= _rge.Length)
					{
						T[] array = new T[_rge.Length + _rge.Length / 2];
						Array.Copy(_rge, array, _rge.Length);
						_rge = array;
					}
					_rge[_ceCur++] = _etorSrc.Current;
					continue;
				}
				e = _rge[ie];
				return true;
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
