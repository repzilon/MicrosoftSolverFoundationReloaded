using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	internal sealed class Permutation
	{
		private const int kcivMin = 10;

		private const int kmaskHead = int.MinValue;

		private int _ivLim;

		private int[] _rgiv;

		private bool _fHeadValid;

		private bool _fInvValid;

		private int[] _rgivInv;

		public int this[int iv]
		{
			get
			{
				if (iv >= _ivLim)
				{
					return iv;
				}
				return _rgiv[iv] & 0x7FFFFFFF;
			}
		}

		public Permutation()
		{
		}

		public Permutation(int ivInit)
		{
			_rgiv = new int[ivInit];
		}

		public void Clear()
		{
			_ivLim = 0;
		}

		public void Set(int[] rgiv, int ivLim)
		{
			while (ivLim > 0 && rgiv[ivLim - 1] == ivLim - 1)
			{
				ivLim--;
			}
			_fHeadValid = false;
			_fInvValid = false;
			EnsureMainBufferSize(ivLim);
			_ivLim = 0;
			Array.Clear(_rgiv, 0, ivLim);
			for (int i = 0; i < ivLim; i++)
			{
				int num = rgiv[i];
				if (0 > num || num >= ivLim || _rgiv[num] != 0)
				{
					throw new InvalidOperationException(Resources.InvalidPermutation);
				}
				_rgiv[num] = 1;
			}
			_ivLim = ivLim;
			Array.Copy(rgiv, _rgiv, _ivLim);
		}

		public bool IsIdentity()
		{
			if (!_fHeadValid)
			{
				SetHeadBits();
			}
			return _ivLim == 0;
		}

		private void EnsureMainBufferSize(int ivLim)
		{
			if (_rgiv == null)
			{
				_rgiv = new int[Math.Max(ivLim, 10)];
			}
			else if (_rgiv.Length < ivLim)
			{
				int newSize = Math.Max(20, Math.Max(_rgiv.Length + _rgiv.Length / 2, ivLim));
				Array.Resize(ref _rgiv, newSize);
			}
		}

		private void ExtendTo(int ivLim)
		{
			if (ivLim <= _ivLim)
			{
				return;
			}
			EnsureMainBufferSize(ivLim);
			if (_fInvValid)
			{
				if (_rgivInv == null || _rgivInv.Length < ivLim)
				{
					Array.Resize(ref _rgivInv, _rgiv.Length);
				}
				for (int i = _ivLim; i < ivLim; i++)
				{
					_rgivInv[i] = i;
				}
			}
			while (_ivLim < ivLim)
			{
				_rgiv[_ivLim] = _ivLim | int.MinValue;
				_ivLim++;
			}
		}

		/// <summary>
		/// This sets the head bits correctly and sets _ivLim minimally.
		/// </summary>
		private void SetHeadBits()
		{
			int num = _ivLim;
			while (--num >= 0)
			{
				_rgiv[num] |= int.MinValue;
			}
			int num2 = -1;
			for (int i = 0; i < _ivLim; i++)
			{
				if ((_rgiv[i] & int.MinValue) == 0)
				{
					num2 = i;
					continue;
				}
				for (int num3 = _rgiv[i] & 0x7FFFFFFF; num3 != i; num3 = _rgiv[num3])
				{
					_rgiv[num3] &= int.MaxValue;
				}
			}
			_ivLim = num2 + 1;
			_fHeadValid = true;
		}

		/// <summary>
		/// This builds the inverse.
		/// </summary>
		private void BuildInverse()
		{
			if (_rgivInv == null || _rgivInv.Length < _ivLim)
			{
				_rgivInv = new int[_rgiv.Length];
			}
			int num = _ivLim;
			while (--num >= 0)
			{
				_rgivInv[_rgiv[num] & 0x7FFFFFFF] = num;
			}
			_fInvValid = true;
		}

		public void Swap(int iv1, int iv2)
		{
			if (iv1 != iv2)
			{
				ExtendTo(Math.Max(iv1, iv2) + 1);
				Statics.Swap(ref _rgiv[iv1], ref _rgiv[iv2]);
				_fHeadValid = false;
				if (_fInvValid)
				{
					_rgivInv[_rgiv[iv1] & 0x7FFFFFFF] = iv1;
					_rgivInv[_rgiv[iv2] & 0x7FFFFFFF] = iv2;
				}
			}
		}

		public void MoveTo(int ivSrc, int ivDst)
		{
			if (ivSrc != ivDst)
			{
				ExtendTo(Math.Max(ivSrc, ivDst) + 1);
				Statics.MoveItem(_rgiv, ivSrc, ivDst);
				_fHeadValid = false;
				_fInvValid = false;
			}
		}

		/// <summary>
		/// Forces ivSrc to map to ivDst. If ivCur currently maps to ivDst, this
		/// is equivalent to Swap(ivSrc, ivCur).
		/// </summary>
		public void ForceMap(int ivSrc, int ivDst)
		{
			int iv = MapInverse(ivDst);
			Swap(ivSrc, iv);
		}

		/// <summary>
		/// This moves item rgv[iv] to rgv[this[iv]].
		/// </summary>
		public void Apply<T>(T[] rgv)
		{
			if (_fInvValid)
			{
				ApplyInvCore(rgv, _rgivInv);
				return;
			}
			if (!_fHeadValid)
			{
				SetHeadBits();
			}
			int num = _ivLim;
			while (--num >= 0)
			{
				if ((_rgiv[num] & int.MinValue) != 0 && num != (_rgiv[num] & 0x7FFFFFFF))
				{
					T a = rgv[num];
					for (int num2 = _rgiv[num] & 0x7FFFFFFF; num2 != num; num2 = _rgiv[num2])
					{
						Statics.Swap(ref a, ref rgv[num2]);
					}
					rgv[num] = a;
				}
			}
		}

		/// <summary>
		/// This moves item rgv[this[iv]] to rgv[iv].
		/// </summary>
		public void ApplyInverse<T>(T[] rgv)
		{
			ApplyInvCore(rgv, _rgiv);
		}

		/// <summary>
		/// This moves item rgv[rgivMap[iv]] to rgv[iv]. This assumes
		/// rgivMap has the same head list as _rgiv (and _rgivInv).
		/// In fact, this is currently only called with rgivMap set to
		/// _rgiv or _rgivInv.
		/// </summary>
		private void ApplyInvCore<T>(T[] rgv, int[] rgivMap)
		{
			if (!_fHeadValid)
			{
				SetHeadBits();
			}
			int num = _ivLim;
			while (--num >= 0)
			{
				if ((_rgiv[num] & int.MinValue) == 0)
				{
					continue;
				}
				int num2 = rgivMap[num] & 0x7FFFFFFF;
				if (num != num2)
				{
					T val = rgv[num];
					int num3 = num;
					for (int num4 = num2; num4 != num; num4 = rgivMap[num3])
					{
						rgv[num3] = rgv[num4];
						num3 = num4;
					}
					rgv[num3] = val;
				}
			}
		}

		public bool IsCycleHead(int iv)
		{
			if (iv >= _ivLim)
			{
				return true;
			}
			if (!_fHeadValid)
			{
				SetHeadBits();
				if (iv >= _ivLim)
				{
					return true;
				}
			}
			return (_rgiv[iv] & int.MinValue) != 0;
		}

		public int Map(int iv)
		{
			if (iv >= _ivLim)
			{
				return iv;
			}
			return _rgiv[iv] & 0x7FFFFFFF;
		}

		public int MapInverse(int iv)
		{
			if (iv >= _ivLim)
			{
				return iv;
			}
			if (!_fInvValid)
			{
				BuildInverse();
				if (iv >= _ivLim)
				{
					return iv;
				}
			}
			return _rgivInv[iv];
		}
	}
}
