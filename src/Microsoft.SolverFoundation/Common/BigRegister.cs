using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// BigRegister holds a multiprecision unsigned integer value. It is mutable and
	/// supports common arithmetic operations. Be careful NOT to simply assign one
	/// BigRegister to another, unless you really know what you are doing. The proper
	/// way to replicate a BigRegister is via the constructor "BigRegister(ref BigRegister reg)",
	/// or with reg1.Load(ref reg2). Using the ctor marks the buffer as shared so changing the
	/// value of one register will not affect the other. Using Load copies the contents from
	/// one to the other. Either way, the internal buffer isn't aliased incorrectly.
	/// </summary>
	internal struct BigRegister
	{
		private const int kcbitUint = 32;

		private static readonly uint[] s_rguTwo32 = new uint[2] { 0u, 1u };

		private int _iuLast;

		private uint _uSmall;

		private uint[] _rgu;

		private bool _fWritable;

		private static readonly double kdblLn2To32 = 32.0 * Math.Log(2.0);

		private static byte[] _rgbInv = new byte[128]
		{
			1, 171, 205, 183, 57, 163, 197, 239, 241, 27,
			61, 167, 41, 19, 53, 223, 225, 139, 173, 151,
			25, 131, 165, 207, 209, 251, 29, 135, 9, 243,
			21, 191, 193, 107, 141, 119, 249, 99, 133, 175,
			177, 219, 253, 103, 233, 211, 245, 159, 161, 75,
			109, 87, 217, 67, 101, 143, 145, 187, 221, 71,
			201, 179, 213, 127, 129, 43, 77, 55, 185, 35,
			69, 111, 113, 155, 189, 39, 169, 147, 181, 95,
			97, 11, 45, 23, 153, 3, 37, 79, 81, 123,
			157, 7, 137, 115, 149, 63, 65, 235, 13, 247,
			121, 227, 5, 47, 49, 91, 125, 231, 105, 83,
			117, 31, 33, 203, 237, 215, 89, 195, 229, 15,
			17, 59, 93, 199, 73, 51, 85, 255
		};

		public int Size => _iuLast + 1;

		public uint High
		{
			get
			{
				if (_iuLast != 0)
				{
					return _rgu[_iuLast];
				}
				return _uSmall;
			}
		}

		[Conditional("DEBUG")]
		private void AssertValid(bool fTrimmed)
		{
		}

		public BigRegister(ref BigRegister reg)
		{
			this = reg;
			if (_fWritable)
			{
				_fWritable = false;
				if (_iuLast == 0)
				{
					_rgu = null;
				}
				else
				{
					reg._fWritable = false;
				}
			}
		}

		public BigRegister(int cuAlloc)
		{
			_iuLast = 0;
			_uSmall = 0u;
			if (cuAlloc > 1)
			{
				_rgu = new uint[cuAlloc];
				_fWritable = true;
			}
			else
			{
				_rgu = null;
				_fWritable = false;
			}
		}

		public BigRegister(BigInteger bn)
			: this(bn._Sign, bn._Bits)
		{
		}

		public BigRegister(BigInteger bn, ref int sign)
			: this(bn._Sign, bn._Bits, ref sign)
		{
		}

		public BigRegister(int sign, uint[] bits)
		{
			_fWritable = false;
			_rgu = bits;
			if (_rgu == null)
			{
				_iuLast = 0;
				_uSmall = Statics.Abs(sign);
				return;
			}
			_iuLast = _rgu.Length - 1;
			_uSmall = _rgu[0];
			while (_iuLast > 0 && _rgu[_iuLast] == 0)
			{
				_iuLast--;
			}
		}

		public BigRegister(int sign, uint[] bits, ref int signDst)
		{
			_fWritable = false;
			_rgu = bits;
			int num = sign >> 31;
			signDst = (signDst ^ num) - num;
			if (_rgu == null)
			{
				_iuLast = 0;
				_uSmall = (uint)((sign ^ num) - num);
				return;
			}
			_iuLast = _rgu.Length - 1;
			_uSmall = _rgu[0];
			while (_iuLast > 0 && _rgu[_iuLast] == 0)
			{
				_iuLast--;
			}
		}

		public BigInteger GetInteger(int sign)
		{
			GetIntegerParts(sign, out sign, out var bits);
			return new BigInteger(sign, bits);
		}

		internal void GetIntegerParts(int signSrc, out int sign, out uint[] bits)
		{
			if (_iuLast == 0)
			{
				if (_uSmall <= int.MaxValue)
				{
					sign = signSrc * (int)_uSmall;
					bits = null;
					return;
				}
				if (_rgu == null)
				{
					_rgu = new uint[1] { _uSmall };
				}
				else if (_fWritable)
				{
					_rgu[0] = _uSmall;
				}
				else if (_rgu[0] != _uSmall)
				{
					_rgu = new uint[1] { _uSmall };
				}
			}
			sign = signSrc;
			int num = _rgu.Length - _iuLast - 1;
			if (num <= 1)
			{
				if (num == 0 || _rgu[_iuLast + 1] == 0)
				{
					_fWritable = false;
					bits = _rgu;
					return;
				}
				if (_fWritable)
				{
					_rgu[_iuLast + 1] = 0u;
					_fWritable = false;
					bits = _rgu;
					return;
				}
			}
			bits = _rgu;
			Array.Resize(ref bits, _iuLast + 1);
			if (!_fWritable)
			{
				_rgu = bits;
			}
		}

		public void Set(uint u)
		{
			_uSmall = u;
			_iuLast = 0;
		}

		public void Set(ulong uu)
		{
			uint hi = Statics.GetHi(uu);
			if (hi == 0)
			{
				_uSmall = Statics.GetLo(uu);
				_iuLast = 0;
			}
			else
			{
				SetSizeLazy(2);
				_rgu[0] = (uint)uu;
				_rgu[1] = hi;
			}
		}

		public bool IsSingle(uint u)
		{
			if (_uSmall == u)
			{
				return _iuLast == 0;
			}
			return false;
		}

		public void GetApproxParts(out int exp, out ulong man)
		{
			if (_iuLast == 0)
			{
				man = _uSmall;
				exp = 0;
				return;
			}
			int num = _iuLast - 1;
			man = Statics.MakeUlong(_rgu[num + 1], _rgu[num]);
			exp = num * 32;
			int num2;
			if (num > 0 && (num2 = Statics.CbitHighZero(_rgu[num + 1])) > 0)
			{
				man = (man << num2) | (_rgu[num - 1] >> 32 - num2);
				exp -= num2;
			}
		}

		private void Trim()
		{
			if (_iuLast > 0 && _rgu[_iuLast] == 0)
			{
				_uSmall = _rgu[0];
				while (--_iuLast > 0 && _rgu[_iuLast] == 0)
				{
				}
			}
		}

		private int GetCuNonZero()
		{
			int num = 0;
			for (int num2 = _iuLast; num2 >= 0; num2--)
			{
				if (_rgu[num2] != 0)
				{
					num++;
				}
			}
			return num;
		}

		private void SetSizeLazy(int cu)
		{
			if (cu <= 1)
			{
				_iuLast = 0;
				return;
			}
			if (!_fWritable || _rgu.Length < cu)
			{
				_rgu = new uint[cu];
				_fWritable = true;
			}
			_iuLast = cu - 1;
		}

		private void SetSizeClear(int cu)
		{
			if (cu <= 1)
			{
				_iuLast = 0;
				_uSmall = 0u;
				return;
			}
			if (!_fWritable || _rgu.Length < cu)
			{
				_rgu = new uint[cu];
				_fWritable = true;
			}
			else
			{
				Array.Clear(_rgu, 0, cu);
			}
			_iuLast = cu - 1;
		}

		private void SetSizeKeep(int cu, int cuExtra)
		{
			if (cu <= 1)
			{
				if (_iuLast > 0)
				{
					_uSmall = _rgu[0];
				}
				_iuLast = 0;
				return;
			}
			if (!_fWritable || _rgu.Length < cu)
			{
				uint[] array = new uint[cu + cuExtra];
				if (_iuLast == 0)
				{
					array[0] = _uSmall;
				}
				else
				{
					Array.Copy(_rgu, array, Math.Min(cu, _iuLast + 1));
				}
				_rgu = array;
				_fWritable = true;
			}
			else if (_iuLast + 1 < cu)
			{
				Array.Clear(_rgu, _iuLast + 1, cu - _iuLast - 1);
				if (_iuLast == 0)
				{
					_rgu[0] = _uSmall;
				}
			}
			_iuLast = cu - 1;
		}

		public void EnsureWritable(int cu, int cuExtra)
		{
			if (_fWritable && _rgu.Length >= cu)
			{
				return;
			}
			uint[] array = new uint[cu + cuExtra];
			if (_iuLast > 0)
			{
				if (_iuLast >= cu)
				{
					_iuLast = cu - 1;
				}
				Array.Copy(_rgu, array, _iuLast + 1);
			}
			_rgu = array;
			_fWritable = true;
		}

		public void EnsureWritable(int cuExtra)
		{
			if (!_fWritable)
			{
				uint[] array = new uint[_iuLast + 1 + cuExtra];
				Array.Copy(_rgu, array, _iuLast + 1);
				_rgu = array;
				_fWritable = true;
			}
		}

		public void EnsureWritable()
		{
			EnsureWritable(0);
		}

		public void Load(ref BigRegister reg)
		{
			Load(ref reg, 0);
		}

		public void Load(ref BigRegister reg, int cuExtra)
		{
			if (reg._iuLast == 0)
			{
				_uSmall = reg._uSmall;
				_iuLast = 0;
				return;
			}
			if (!_fWritable || _rgu.Length <= reg._iuLast)
			{
				_rgu = new uint[reg._iuLast + 1 + cuExtra];
				_fWritable = true;
			}
			_iuLast = reg._iuLast;
			Array.Copy(reg._rgu, _rgu, _iuLast + 1);
		}

		public void Add(uint u)
		{
			if (_iuLast == 0)
			{
				if ((_uSmall += u) < u)
				{
					SetSizeLazy(2);
					_rgu[0] = _uSmall;
					_rgu[1] = 1u;
				}
			}
			else if (u != 0)
			{
				uint num = _rgu[0] + u;
				if (num < u)
				{
					EnsureWritable(1);
					ApplyCarry(1);
				}
				else if (!_fWritable)
				{
					EnsureWritable();
				}
				_rgu[0] = num;
			}
		}

		public void Add(ulong uu)
		{
			uint lo = Statics.GetLo(uu);
			uint num = Statics.GetHi(uu);
			if (num == 0)
			{
				Add(lo);
			}
			else if (_iuLast == 0)
			{
				if ((lo += _uSmall) < _uSmall && ++num == 0)
				{
					SetSizeLazy(3);
					_rgu[2] = 1u;
				}
				else
				{
					SetSizeLazy(2);
				}
				_rgu[1] = num;
				_rgu[0] = lo;
			}
			else
			{
				EnsureWritable(1);
				uint uCarry = AddCarry(ref _rgu[0], lo, 0u);
				if (AddCarry(ref _rgu[1], num, uCarry) != 0)
				{
					ApplyCarry(2);
				}
			}
		}

		public void Add(ref BigRegister reg)
		{
			if (reg._iuLast == 0)
			{
				Add(reg._uSmall);
				return;
			}
			if (_iuLast == 0)
			{
				uint uSmall = _uSmall;
				if (uSmall == 0)
				{
					this = new BigRegister(ref reg);
					return;
				}
				Load(ref reg, 1);
				Add(uSmall);
				return;
			}
			EnsureWritable(Math.Max(_iuLast, reg._iuLast) + 1, 1);
			int num = reg._iuLast + 1;
			if (_iuLast < reg._iuLast)
			{
				num = _iuLast + 1;
				Array.Copy(reg._rgu, _iuLast + 1, _rgu, _iuLast + 1, reg._iuLast - _iuLast);
				_iuLast = reg._iuLast;
			}
			uint num2 = 0u;
			for (int i = 0; i < num; i++)
			{
				num2 = AddCarry(ref _rgu[i], reg._rgu[i], num2);
			}
			if (num2 != 0)
			{
				ApplyCarry(num);
			}
		}

		public void Sub(ref int sign, uint u)
		{
			if (_iuLast == 0)
			{
				if (u <= _uSmall)
				{
					_uSmall -= u;
					return;
				}
				_uSmall = u - _uSmall;
				sign = -sign;
			}
			else if (u != 0)
			{
				EnsureWritable();
				uint num = _rgu[0];
				_rgu[0] = num - u;
				if (num < u)
				{
					ApplyBorrow(1);
					Trim();
				}
			}
		}

		public void Sub(ref int sign, ulong uu)
		{
			if (_iuLast == 0)
			{
				if (uu <= _uSmall)
				{
					_uSmall -= (uint)(int)uu;
					return;
				}
				Set(uu - _uSmall);
				sign = -sign;
			}
			else
			{
				if (uu == 0)
				{
					return;
				}
				EnsureWritable();
				ulong num = Statics.MakeUlong(_rgu[1], _rgu[0]);
				ulong num2 = num - uu;
				if (uu > num)
				{
					if (_iuLast == 1)
					{
						num2 = 0L - num2;
						sign = -sign;
					}
					else
					{
						ApplyBorrow(2);
					}
				}
				_rgu[0] = Statics.GetLo(num2);
				_rgu[1] = Statics.GetHi(num2);
				Trim();
			}
		}

		public void Sub(ref int sign, ref BigRegister reg)
		{
			if (reg._iuLast == 0)
			{
				Sub(ref sign, reg._uSmall);
				return;
			}
			if (_iuLast == 0)
			{
				uint uSmall = _uSmall;
				if (uSmall == 0)
				{
					this = new BigRegister(ref reg);
				}
				else
				{
					Load(ref reg);
					Sub(ref sign, uSmall);
				}
				sign = -sign;
				return;
			}
			if (_iuLast < reg._iuLast)
			{
				SubRev(ref reg);
				sign = -sign;
				return;
			}
			int num = reg._iuLast + 1;
			if (_iuLast == reg._iuLast)
			{
				_iuLast = BigInteger.GetDiffLength(_rgu, reg._rgu, _iuLast + 1) - 1;
				if (_iuLast < 0)
				{
					_iuLast = 0;
					_uSmall = 0u;
					return;
				}
				uint num2 = _rgu[_iuLast];
				uint num3 = reg._rgu[_iuLast];
				if (_iuLast == 0)
				{
					if (num2 < num3)
					{
						_uSmall = num3 - num2;
						sign = -sign;
					}
					else
					{
						_uSmall = num2 - num3;
					}
					return;
				}
				if (num2 < num3)
				{
					reg._iuLast = _iuLast;
					SubRev(ref reg);
					reg._iuLast = num - 1;
					sign = -sign;
					return;
				}
				num = _iuLast + 1;
			}
			EnsureWritable();
			uint num4 = 0u;
			for (int i = 0; i < num; i++)
			{
				num4 = SubBorrow(ref _rgu[i], reg._rgu[i], num4);
			}
			if (num4 != 0)
			{
				ApplyBorrow(num);
			}
			Trim();
		}

		private void SubRev(ref BigRegister reg)
		{
			EnsureWritable(reg._iuLast + 1, 0);
			int num = _iuLast + 1;
			if (_iuLast < reg._iuLast)
			{
				Array.Copy(reg._rgu, _iuLast + 1, _rgu, _iuLast + 1, reg._iuLast - _iuLast);
				_iuLast = reg._iuLast;
			}
			uint num2 = 0u;
			for (int i = 0; i < num; i++)
			{
				num2 = SubRevBorrow(ref _rgu[i], reg._rgu[i], num2);
			}
			if (num2 != 0)
			{
				ApplyBorrow(num);
			}
			Trim();
		}

		public void Mul(uint u)
		{
			switch (u)
			{
			case 0u:
				Set(0u);
				return;
			case 1u:
				return;
			}
			if (_iuLast == 0)
			{
				Set((ulong)_uSmall * (ulong)u);
				return;
			}
			EnsureWritable(1);
			uint num = 0u;
			for (int i = 0; i <= _iuLast; i++)
			{
				num = MulCarry(ref _rgu[i], u, num);
			}
			if (num != 0)
			{
				SetSizeKeep(_iuLast + 2, 0);
				_rgu[_iuLast] = num;
			}
		}

		public void Mul(ref BigRegister regMul)
		{
			if (regMul._iuLast == 0)
			{
				Mul(regMul._uSmall);
				return;
			}
			if (_iuLast == 0)
			{
				uint uSmall = _uSmall;
				switch (uSmall)
				{
				case 1u:
					this = new BigRegister(ref regMul);
					break;
				default:
					Load(ref regMul, 1);
					Mul(uSmall);
					break;
				case 0u:
					break;
				}
				return;
			}
			int num = _iuLast + 1;
			SetSizeKeep(num + regMul._iuLast, 1);
			int num2 = num;
			while (--num2 >= 0)
			{
				uint uMul = _rgu[num2];
				_rgu[num2] = 0u;
				uint num3 = 0u;
				for (int i = 0; i <= regMul._iuLast; i++)
				{
					num3 = AddMulCarry(ref _rgu[num2 + i], regMul._rgu[i], uMul, num3);
				}
				if (num3 != 0)
				{
					int num4 = num2 + regMul._iuLast + 1;
					while (num3 != 0 && num4 <= _iuLast)
					{
						num3 = AddCarry(ref _rgu[num4], 0u, num3);
						num4++;
					}
					if (num3 != 0)
					{
						SetSizeKeep(_iuLast + 2, 0);
						_rgu[_iuLast] = num3;
					}
				}
			}
		}

		public void Mul(ref BigRegister reg1, ref BigRegister reg2)
		{
			if (reg1._iuLast == 0)
			{
				if (reg2._iuLast == 0)
				{
					Set((ulong)reg1._uSmall * (ulong)reg2._uSmall);
					return;
				}
				Load(ref reg2, 1);
				Mul(reg1._uSmall);
				return;
			}
			if (reg2._iuLast == 0)
			{
				Load(ref reg1, 1);
				Mul(reg2._uSmall);
				return;
			}
			SetSizeClear(reg1._iuLast + reg2._iuLast + 2);
			uint[] rgu;
			int num;
			uint[] rgu2;
			int num2;
			if (reg1.GetCuNonZero() <= reg2.GetCuNonZero())
			{
				rgu = reg1._rgu;
				num = reg1._iuLast + 1;
				rgu2 = reg2._rgu;
				num2 = reg2._iuLast + 1;
			}
			else
			{
				rgu = reg2._rgu;
				num = reg2._iuLast + 1;
				rgu2 = reg1._rgu;
				num2 = reg1._iuLast + 1;
			}
			for (int i = 0; i < num; i++)
			{
				uint num3 = rgu[i];
				if (num3 != 0)
				{
					uint num4 = 0u;
					int num5 = i;
					int num6 = 0;
					while (num6 < num2)
					{
						num4 = AddMulCarry(ref _rgu[num5], num3, rgu2[num6], num4);
						num6++;
						num5++;
					}
					while (num4 != 0)
					{
						num4 = AddCarry(ref _rgu[num5++], 0u, num4);
					}
				}
			}
			Trim();
		}

		public void AddMul(ref BigRegister reg, uint u)
		{
			if (u == 0)
			{
				return;
			}
			if (reg._iuLast == 0)
			{
				Add((ulong)reg._uSmall * (ulong)u);
				return;
			}
			if (u == 1)
			{
				Add(ref reg);
				return;
			}
			SetSizeKeep(Math.Max(reg._iuLast + 2, _iuLast + 1), 1);
			uint num = 0u;
			int i;
			for (i = 0; i <= reg._iuLast; i++)
			{
				num = AddMulCarry(ref _rgu[i], reg._rgu[i], u, num);
			}
			while (num != 0)
			{
				if (i > _iuLast)
				{
					SetSizeKeep(_iuLast + 2, 0);
					_rgu[_iuLast] = num;
					return;
				}
				num = AddCarry(ref _rgu[i], 0u, num);
				i++;
			}
			Trim();
		}

		public void AddMul(ref BigRegister reg1, ref BigRegister reg2)
		{
			if (reg1._iuLast == 0)
			{
				AddMul(ref reg2, reg1._uSmall);
				return;
			}
			if (reg2._iuLast == 0)
			{
				AddMul(ref reg1, reg2._uSmall);
				return;
			}
			SetSizeKeep(Math.Max(reg1._iuLast + reg2._iuLast + 2, _iuLast + 1), 1);
			uint[] rgu;
			int num;
			uint[] rgu2;
			int num2;
			if (reg1.GetCuNonZero() <= reg2.GetCuNonZero())
			{
				rgu = reg1._rgu;
				num = reg1._iuLast + 1;
				rgu2 = reg2._rgu;
				num2 = reg2._iuLast + 1;
			}
			else
			{
				rgu = reg2._rgu;
				num = reg2._iuLast + 1;
				rgu2 = reg1._rgu;
				num2 = reg1._iuLast + 1;
			}
			for (int i = 0; i < num; i++)
			{
				uint num3 = rgu[i];
				if (num3 == 0)
				{
					continue;
				}
				uint num4 = 0u;
				int num5 = i;
				int num6 = 0;
				while (num6 < num2)
				{
					num4 = AddMulCarry(ref _rgu[num5], num3, rgu2[num6], num4);
					num6++;
					num5++;
				}
				while (num4 != 0)
				{
					if (num5 > _iuLast)
					{
						SetSizeKeep(_iuLast + 2, 0);
						_rgu[_iuLast] = num4;
						break;
					}
					num4 = AddCarry(ref _rgu[num5++], 0u, num4);
				}
			}
			Trim();
		}

		public void SubMul(ref int sign, ref BigRegister reg, uint u)
		{
			if (u == 0)
			{
				return;
			}
			if (reg._iuLast == 0)
			{
				Sub(ref sign, (ulong)reg._uSmall * (ulong)u);
				return;
			}
			if (u == 1)
			{
				Sub(ref sign, ref reg);
				return;
			}
			SetSizeKeep(Math.Max(reg._iuLast + 2, _iuLast + 1), 0);
			uint num = 0u;
			int i;
			for (i = 0; i <= reg._iuLast; i++)
			{
				num = SubMulBorrow(ref _rgu[i], reg._rgu[i], u, num);
			}
			while (num != 0)
			{
				if (i > _iuLast)
				{
					NegateClip();
					sign = -sign;
					break;
				}
				num = SubBorrow(ref _rgu[i], 0u, num);
				i++;
			}
			Trim();
		}

		public void SubMul(ref int sign, ref BigRegister reg1, ref BigRegister reg2)
		{
			if (reg1._iuLast == 0)
			{
				SubMul(ref sign, ref reg2, reg1._uSmall);
				return;
			}
			if (reg2._iuLast == 0)
			{
				SubMul(ref sign, ref reg1, reg2._uSmall);
				return;
			}
			SetSizeKeep(Math.Max(reg1._iuLast + reg2._iuLast + 2, _iuLast + 1), 0);
			uint[] rgu;
			int num;
			uint[] rgu2;
			int num2;
			if (reg1.GetCuNonZero() <= reg2.GetCuNonZero())
			{
				rgu = reg1._rgu;
				num = reg1._iuLast + 1;
				rgu2 = reg2._rgu;
				num2 = reg2._iuLast + 1;
			}
			else
			{
				rgu = reg2._rgu;
				num = reg2._iuLast + 1;
				rgu2 = reg1._rgu;
				num2 = reg1._iuLast + 1;
			}
			bool flag = false;
			for (int i = 0; i < num; i++)
			{
				uint num3 = rgu[i];
				if (num3 == 0)
				{
					continue;
				}
				uint num4 = 0u;
				int num5 = i;
				int num6 = 0;
				while (num6 < num2)
				{
					num4 = SubMulBorrow(ref _rgu[num5], num3, rgu2[num6], num4);
					num6++;
					num5++;
				}
				while (num4 != 0)
				{
					if (num5 > _iuLast)
					{
						flag = true;
						break;
					}
					num4 = SubBorrow(ref _rgu[num5++], 0u, num4);
				}
			}
			if (flag)
			{
				NegateClip();
				sign = -sign;
			}
			Trim();
		}

		public uint DivMod(uint uDen)
		{
			if (uDen == 1)
			{
				return 0u;
			}
			if (_iuLast == 0)
			{
				uint uSmall = _uSmall;
				_uSmall = uSmall / uDen;
				return uSmall % uDen;
			}
			EnsureWritable();
			ulong num = 0uL;
			for (int num2 = _iuLast; num2 >= 0; num2--)
			{
				num = Statics.MakeUlong((uint)num, _rgu[num2]);
				_rgu[num2] = (uint)(num / uDen);
				num %= uDen;
			}
			Trim();
			return (uint)num;
		}

		public static uint Mod(ref BigRegister regNum, uint uDen)
		{
			if (uDen == 1)
			{
				return 0u;
			}
			if (regNum._iuLast == 0)
			{
				return regNum._uSmall % uDen;
			}
			ulong num = 0uL;
			for (int num2 = regNum._iuLast; num2 >= 0; num2--)
			{
				num = Statics.MakeUlong((uint)num, regNum._rgu[num2]);
				num %= uDen;
			}
			return (uint)num;
		}

		public void Mod(ref BigRegister regDen)
		{
			if (regDen._iuLast == 0)
			{
				Set(Mod(ref this, regDen._uSmall));
			}
			else if (_iuLast != 0)
			{
				BigRegister regQuo = default(BigRegister);
				ModDivCore(ref this, ref regDen, fQuo: false, ref regQuo);
			}
		}

		public void Div(ref BigRegister regDen)
		{
			if (regDen._iuLast == 0)
			{
				DivMod(regDen._uSmall);
				return;
			}
			if (_iuLast == 0)
			{
				_uSmall = 0u;
				return;
			}
			BigRegister regQuo = default(BigRegister);
			ModDivCore(ref this, ref regDen, fQuo: true, ref regQuo);
			Statics.Swap(ref this, ref regQuo);
		}

		public void ModDiv(ref BigRegister regDen, ref BigRegister regQuo)
		{
			if (regDen._iuLast == 0)
			{
				regQuo.Set(DivMod(regDen._uSmall));
				Statics.Swap(ref this, ref regQuo);
			}
			else if (_iuLast != 0)
			{
				ModDivCore(ref this, ref regDen, fQuo: true, ref regQuo);
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		private static void ModDivCore(ref BigRegister regNum, ref BigRegister regDen, bool fQuo, ref BigRegister regQuo)
		{
			regQuo.Set(0u);
			if (regNum._iuLast < regDen._iuLast)
			{
				return;
			}
			int num = regDen._iuLast + 1;
			int num2 = regNum._iuLast - regDen._iuLast;
			int num3 = num2;
			int num4 = regNum._iuLast;
			while (true)
			{
				if (num4 < num2)
				{
					num3++;
					break;
				}
				if (regDen._rgu[num4 - num2] != regNum._rgu[num4])
				{
					if (regDen._rgu[num4 - num2] < regNum._rgu[num4])
					{
						num3++;
					}
					break;
				}
				num4--;
			}
			if (num3 == 0)
			{
				return;
			}
			if (fQuo)
			{
				regQuo.SetSizeLazy(num3);
			}
			uint num5 = regDen._rgu[num - 1];
			uint num6 = regDen._rgu[num - 2];
			int num7 = Statics.CbitHighZero(num5);
			int num8 = 32 - num7;
			if (num7 > 0)
			{
				num5 = (num5 << num7) | (num6 >> num8);
				num6 <<= num7;
				if (num > 2)
				{
					num6 |= regDen._rgu[num - 3] >> num8;
				}
			}
			regNum.EnsureWritable();
			int num9 = num3;
			while (--num9 >= 0)
			{
				uint num10 = ((num9 + num <= regNum._iuLast) ? regNum._rgu[num9 + num] : 0u);
				ulong num11 = Statics.MakeUlong(num10, regNum._rgu[num9 + num - 1]);
				uint num12 = regNum._rgu[num9 + num - 2];
				if (num7 > 0)
				{
					num11 = (num11 << num7) | (num12 >> num8);
					num12 <<= num7;
					if (num9 + num >= 3)
					{
						num12 |= regNum._rgu[num9 + num - 3] >> num8;
					}
				}
				ulong num13 = num11 / num5;
				ulong num14 = (uint)(num11 % num5);
				if (num13 > uint.MaxValue)
				{
					num14 += num5 * (num13 - uint.MaxValue);
					num13 = 4294967295uL;
				}
				for (; num14 <= uint.MaxValue && num13 * num6 > Statics.MakeUlong((uint)num14, num12); num14 += num5)
				{
					num13--;
				}
				if (num13 != 0)
				{
					ulong num15 = 0uL;
					for (int i = 0; i < num; i++)
					{
						num15 += regDen._rgu[i] * num13;
						uint num16 = (uint)num15;
						num15 >>= 32;
						if (regNum._rgu[num9 + i] < num16)
						{
							num15++;
						}
						regNum._rgu[num9 + i] -= num16;
					}
					if (num10 < num15)
					{
						uint uCarry = 0u;
						for (int j = 0; j < num; j++)
						{
							uCarry = AddCarry(ref regNum._rgu[num9 + j], regDen._rgu[j], uCarry);
						}
						num13--;
					}
					regNum._iuLast = num9 + num - 1;
				}
				if (fQuo)
				{
					if (num3 == 1)
					{
						regQuo._uSmall = (uint)num13;
					}
					else
					{
						regQuo._rgu[num9] = (uint)num13;
					}
				}
			}
			regNum._iuLast = num - 1;
			regNum.Trim();
		}

		public void BitShiftRight(int sign, int cbit)
		{
			if (cbit <= 0)
			{
				if (cbit != 0)
				{
					uint num = (uint)(-cbit);
					BitShiftLeftCore((int)(num / 32), (int)(num % 32));
				}
			}
			else
			{
				BitShiftRightCore(sign, cbit / 32, cbit % 32);
			}
		}

		public void BitShiftRightCore(int sign, int cuShift, int cbitShift)
		{
			if ((cuShift | cbitShift) == 0 || (_uSmall | (uint)_iuLast) == 0)
			{
				return;
			}
			if (cuShift > _iuLast)
			{
				Set((sign < 0) ? 1u : 0u);
				return;
			}
			if (_iuLast == 0)
			{
				if (sign < 0 && (_uSmall & (uint)((1 << cbitShift) - 1)) != 0)
				{
					_uSmall = (_uSmall >> cbitShift) + 1;
				}
				else
				{
					_uSmall >>= cbitShift;
				}
				return;
			}
			bool flag = false;
			if (sign < 0)
			{
				if (cbitShift > 0 && (_rgu[cuShift] & (uint)((1 << cbitShift) - 1)) != 0)
				{
					flag = true;
				}
				else
				{
					int num = cuShift;
					while (--num >= 0)
					{
						if (_rgu[num] != 0)
						{
							flag = true;
							break;
						}
					}
				}
			}
			uint[] rgu = _rgu;
			int num2 = _iuLast + 1;
			_iuLast -= cuShift;
			if (_iuLast == 0)
			{
				_uSmall = rgu[cuShift] >> cbitShift;
			}
			else
			{
				if (!_fWritable)
				{
					_rgu = new uint[_iuLast + 1];
					_fWritable = true;
				}
				if (cbitShift > 0)
				{
					int num3 = cuShift + 1;
					int num4 = 0;
					while (num3 < num2)
					{
						_rgu[num4] = (rgu[num3 - 1] >> cbitShift) | (rgu[num3] << 32 - cbitShift);
						num3++;
						num4++;
					}
					_rgu[_iuLast] = rgu[num2 - 1] >> cbitShift;
					Trim();
				}
				else
				{
					Array.Copy(rgu, cuShift, _rgu, 0, _iuLast + 1);
				}
			}
			if (flag)
			{
				Add(1u);
			}
		}

		public void BitShiftLeft(int sign, int cbit)
		{
			if (cbit <= 0)
			{
				if (cbit != 0)
				{
					uint num = (uint)(-cbit);
					BitShiftRightCore(sign, (int)(num / 32), (int)(num % 32));
				}
			}
			else
			{
				BitShiftLeftCore(cbit / 32, cbit % 32);
			}
		}

		private void BitShiftLeftCore(int cuShift, int cbitShift)
		{
			if ((cuShift | cbitShift) == 0 || (_uSmall | (uint)_iuLast) == 0)
			{
				return;
			}
			int num = _iuLast + cuShift;
			uint num2 = 0u;
			if (cbitShift > 0)
			{
				num2 = High >> 32 - cbitShift;
				if (num2 != 0)
				{
					num++;
				}
			}
			if (num == 0)
			{
				_uSmall <<= cbitShift;
				return;
			}
			uint[] rgu = _rgu;
			bool flag = cuShift > 0;
			if (!_fWritable || _rgu.Length <= num)
			{
				_rgu = new uint[num + 1];
				_fWritable = true;
				flag = false;
			}
			if (_iuLast == 0)
			{
				if (num2 != 0)
				{
					_rgu[cuShift + 1] = num2;
				}
				_rgu[cuShift] = _uSmall << cbitShift;
			}
			else if (cbitShift == 0)
			{
				Array.Copy(rgu, 0, _rgu, cuShift, _iuLast + 1);
			}
			else
			{
				int num3 = _iuLast;
				int num4 = _iuLast + cuShift;
				if (num4 < num)
				{
					_rgu[num] = num2;
				}
				while (num3 > 0)
				{
					_rgu[num4] = (rgu[num3] << cbitShift) | (rgu[num3 - 1] >> 32 - cbitShift);
					num3--;
					num4--;
				}
				_rgu[cuShift] = rgu[0] << cbitShift;
			}
			_iuLast = num;
			if (flag)
			{
				Array.Clear(_rgu, 0, cuShift);
			}
		}

		private int FindNonZero()
		{
			int i;
			for (i = 0; _rgu[i] == 0; i++)
			{
			}
			return i;
		}

		private void SetToTwo32()
		{
			_fWritable = false;
			_iuLast = 1;
			_uSmall = 0u;
			_rgu = s_rguTwo32;
		}

		public void BitAnd(uint uHigh, ref BigRegister reg, uint uHigh2)
		{
			if (_iuLast == 0)
			{
				if (reg._iuLast == 0)
				{
					uint num = uHigh & uHigh2;
					_uSmall = ((((_uSmall ^ uHigh) - uHigh) & ((reg._uSmall ^ uHigh2) - uHigh2)) ^ num) - num;
					if (_uSmall == 0 && num == uint.MaxValue)
					{
						SetToTwo32();
					}
				}
				else
				{
					Statics.Swap(ref this, ref reg);
					BitAndSmall(uHigh2, reg._uSmall, uHigh);
				}
			}
			else if (reg._iuLast == 0)
			{
				BitAndSmall(uHigh, reg._uSmall, uHigh2);
			}
			else if (uHigh == 0)
			{
				if (uHigh2 == 0)
				{
					BitAndPositive(ref reg);
				}
				else
				{
					BitAndPosNeg(ref reg);
				}
			}
			else if (uHigh2 == 0)
			{
				Statics.Swap(ref this, ref reg);
				BitAndPosNeg(ref reg);
			}
			else
			{
				BitAndNegative(ref reg);
			}
		}

		private void BitAndPositive(ref BigRegister reg)
		{
			int num = _iuLast;
			if (num > reg._iuLast)
			{
				num = reg._iuLast;
				Statics.Swap(ref this, ref reg);
			}
			else if (num == reg._iuLast)
			{
				while (_rgu[num] == reg._rgu[num])
				{
					if (--num < 0)
					{
						return;
					}
				}
				if (_rgu[num] > reg._rgu[num])
				{
					Statics.Swap(ref this, ref reg);
				}
			}
			uint num2 = _rgu[num] & reg._rgu[num];
			if (num2 == 0 && num == _iuLast)
			{
				do
				{
					if (--num < 0)
					{
						Set(0u);
						return;
					}
				}
				while ((num2 = _rgu[num] & reg._rgu[num]) == 0);
				if (num == 0)
				{
					Set(num2);
					return;
				}
				SetSizeKeep(num + 1, 0);
			}
			else
			{
				if (num2 == _rgu[num])
				{
					do
					{
						if (--num < 0)
						{
							return;
						}
					}
					while ((num2 = _rgu[num] & reg._rgu[num]) == _rgu[num]);
				}
				EnsureWritable();
			}
			_rgu[num] = num2;
			while (--num >= 0)
			{
				_rgu[num] &= reg._rgu[num];
			}
		}

		private void BitAndNegative(ref BigRegister reg)
		{
			int a = FindNonZero();
			if (reg._iuLast < a)
			{
				return;
			}
			int b = reg.FindNonZero();
			if (_iuLast < b)
			{
				Statics.Swap(ref this, ref reg);
				return;
			}
			int num = Math.Max(a, b);
			uint num2 = 1 + (((a < num) ? _rgu[num] : (_rgu[num] - 1)) | ((b < num) ? reg._rgu[num] : (reg._rgu[num] - 1)));
			if (_iuLast <= reg._iuLast && (_iuLast < reg._iuLast || a < b || (num2 == reg._rgu[num] && num2 != _rgu[num])))
			{
				Statics.Swap(ref this, ref reg);
				Statics.Swap(ref a, ref b);
			}
			int num3 = reg._iuLast;
			if (num == a && num2 == _rgu[num])
			{
				while (true)
				{
					if (num3 == num)
					{
						return;
					}
					if ((~_rgu[num3] & reg._rgu[num3]) != 0)
					{
						break;
					}
					num3--;
				}
			}
			EnsureWritable(1);
			if (num > a)
			{
				Array.Clear(_rgu, a, num - a);
			}
			while (num3 > num)
			{
				_rgu[num3] |= reg._rgu[num3];
				num3--;
			}
			_rgu[num] = num2;
			if (num2 == 0)
			{
				ApplyCarry(num + 1);
			}
		}

		private void BitAndPosNeg(ref BigRegister reg)
		{
			int num = FindNonZero();
			if (reg._iuLast < num)
			{
				return;
			}
			int num2 = reg.FindNonZero();
			if (_iuLast < num2)
			{
				Set(0u);
				return;
			}
			uint num3 = _rgu[num2] & (0 - reg._rgu[num2]);
			int num4 = Math.Max(num2 + 1, num);
			int num5 = _iuLast;
			if (num5 <= reg._iuLast)
			{
				while (true)
				{
					if (num5 < num4)
					{
						if (num3 != 0 && num5 > 0)
						{
							break;
						}
						Set(num3);
						return;
					}
					if ((_rgu[num5] & ~reg._rgu[num5]) != 0)
					{
						break;
					}
					num5--;
				}
			}
			int num6 = Math.Min(num5, reg._iuLast);
			if (num5 == _iuLast && num >= num2 && _rgu[num2] == num3)
			{
				while (true)
				{
					if (num6 < num4)
					{
						return;
					}
					if ((_rgu[num6] & reg._rgu[num6]) != 0)
					{
						break;
					}
					num6--;
				}
			}
			SetSizeKeep(num5 + 1, 0);
			if (num2 > num)
			{
				Array.Clear(_rgu, num, num2 - num);
			}
			while (num6 >= num4)
			{
				_rgu[num6] &= ~reg._rgu[num6];
				num6--;
			}
			_rgu[num2] = num3;
		}

		private void BitAndSmall(uint uHigh, uint uSmall, uint uHigh2)
		{
			uSmall = (uSmall ^ uHigh2) - uHigh2;
			uint num = (_rgu[0] ^ uHigh) - uHigh;
			uint num2 = num & uSmall;
			if (uHigh2 == 0)
			{
				Set(num2);
			}
			else if (num2 != num)
			{
				if (uHigh == 0)
				{
					int sign = 1;
					Sub(ref sign, num - num2);
				}
				else
				{
					Add(num - num2);
				}
			}
		}

		public void BitOr(uint uHigh, ref BigRegister reg, uint uHigh2)
		{
			if (_iuLast == 0)
			{
				if (reg._iuLast == 0)
				{
					uint num = uHigh | uHigh2;
					_uSmall = ((((_uSmall ^ uHigh) - uHigh) | ((reg._uSmall ^ uHigh2) - uHigh2)) ^ num) - num;
				}
				else
				{
					Statics.Swap(ref this, ref reg);
					BitOrSmall(uHigh2, reg._uSmall, uHigh);
				}
			}
			else if (reg._iuLast == 0)
			{
				BitOrSmall(uHigh, reg._uSmall, uHigh2);
			}
			else if (uHigh == 0)
			{
				if (uHigh2 == 0)
				{
					BitOrPositive(ref reg);
					return;
				}
				Statics.Swap(ref this, ref reg);
				BitOrNegPos(ref reg);
			}
			else if (uHigh2 == 0)
			{
				BitOrNegPos(ref reg);
			}
			else
			{
				BitOrNegative(ref reg);
			}
		}

		private void BitOrPositive(ref BigRegister reg)
		{
			int num = reg._iuLast;
			if (num > _iuLast)
			{
				num = _iuLast;
				Statics.Swap(ref this, ref reg);
			}
			else if (num == _iuLast)
			{
				while (_rgu[num] == reg._rgu[num])
				{
					if (--num < 0)
					{
						return;
					}
				}
				if (_rgu[num] < reg._rgu[num])
				{
					Statics.Swap(ref this, ref reg);
				}
			}
			uint num2 = _rgu[num] | reg._rgu[num];
			if (num2 == _rgu[num])
			{
				do
				{
					if (--num < 0)
					{
						return;
					}
				}
				while ((num2 = _rgu[num] | reg._rgu[num]) == _rgu[num]);
			}
			EnsureWritable();
			_rgu[num] = num2;
			while (--num >= 0)
			{
				_rgu[num] |= reg._rgu[num];
			}
		}

		private void BitOrNegative(ref BigRegister reg)
		{
			int b = reg.FindNonZero();
			if (_iuLast < b)
			{
				return;
			}
			int a = FindNonZero();
			if (reg._iuLast < a)
			{
				Statics.Swap(ref this, ref reg);
				return;
			}
			int num = Math.Min(a, b);
			uint num2 = ((_rgu[num] - 1) & (reg._rgu[num] - 1)) + 1;
			if (a >= b && (a > b || (_iuLast >= reg._iuLast && (_iuLast > reg._iuLast || (num2 == reg._rgu[num] && num2 != _rgu[num])))))
			{
				Statics.Swap(ref this, ref reg);
				Statics.Swap(ref a, ref b);
			}
			int num3 = Math.Min(_iuLast, reg._iuLast);
			while (true)
			{
				if (num3 > b)
				{
					if ((_rgu[num3] & reg._rgu[num3]) != 0)
					{
						break;
					}
					num3--;
					continue;
				}
				while (true)
				{
					if (num3 > a)
					{
						if ((_rgu[num3] & (reg._rgu[num3] - 1)) != 0)
						{
							break;
						}
						num3--;
						continue;
					}
					if (num3 != 0)
					{
						break;
					}
					Set(num2);
					return;
				}
				break;
			}
			int num4 = num3;
			if (num3 == _iuLast && num2 == _rgu[a])
			{
				while (true)
				{
					if (num4 > b)
					{
						if ((_rgu[num4] & ~reg._rgu[num4]) != 0)
						{
							break;
						}
						num4--;
						continue;
					}
					if (b != a && (_rgu[b] & (0 - reg._rgu[b])) != 0)
					{
						break;
					}
					return;
				}
			}
			SetSizeKeep(num3 + 1, 0);
			while (num4 > b)
			{
				_rgu[num4] &= reg._rgu[num4];
				num4--;
			}
			if (b == a)
			{
				_rgu[a] = num2;
			}
			else if (b <= num3)
			{
				_rgu[b] &= reg._rgu[b] - 1;
			}
		}

		private void BitOrNegPos(ref BigRegister reg)
		{
			int num = reg.FindNonZero();
			if (_iuLast < num)
			{
				return;
			}
			int num2 = FindNonZero();
			if (reg._iuLast < num2)
			{
				int sign = 1;
				Sub(ref sign, ref reg);
				return;
			}
			int num3 = Math.Min(num2, num);
			uint num4 = ((_rgu[num3] - 1) & ~reg._rgu[num3]) + 1;
			int num5 = _iuLast;
			if (num5 <= reg._iuLast)
			{
				while (true)
				{
					if (num5 > num2)
					{
						if ((_rgu[num5] & ~reg._rgu[num5]) != 0)
						{
							break;
						}
						num5--;
						continue;
					}
					while (true)
					{
						if (num5 > num3)
						{
							if (((_rgu[num5] - 1) & ~reg._rgu[num5]) != 0)
							{
								break;
							}
							num5--;
							continue;
						}
						if (num5 != 0)
						{
							break;
						}
						Set(num4);
						return;
					}
					break;
				}
			}
			int num6 = Math.Min(num5, reg._iuLast);
			if (num3 == num2 && num5 == _iuLast && num4 == _rgu[num3])
			{
				while ((_rgu[num6] & reg._rgu[num6]) == 0)
				{
					if (--num6 < num)
					{
						return;
					}
				}
			}
			SetSizeKeep(num5 + 1, 0);
			int num7 = Math.Max(num2 + 1, num);
			while (num6 >= num7)
			{
				_rgu[num6] &= ~reg._rgu[num6];
				num6--;
			}
			if (num6 > num)
			{
				_rgu[num6] = (_rgu[num6] - 1) & ~reg._rgu[num6];
				while (--num6 > num3)
				{
					_rgu[num6] = ~reg._rgu[num6];
				}
			}
			_rgu[num3] = num4;
		}

		private void BitOrSmall(uint uHigh, uint uSmall, uint uHigh2)
		{
			uSmall = (uSmall ^ uHigh2) - uHigh2;
			uint num = (_rgu[0] ^ uHigh) - uHigh;
			uint num2 = num | uSmall;
			if (uHigh2 != 0)
			{
				Set(0 - num2);
			}
			else if (num2 != num)
			{
				if (uHigh == 0)
				{
					Add(num2 - num);
					return;
				}
				int sign = 1;
				Sub(ref sign, num2 - num);
			}
		}

		public void BitXor(uint uHigh, ref BigRegister reg, uint uHigh2)
		{
			if (_iuLast == 0)
			{
				if (reg._iuLast == 0)
				{
					uint num = uHigh ^ uHigh2;
					_uSmall = (((_uSmall ^ uHigh) - uHigh) ^ ((reg._uSmall ^ uHigh2) - uHigh2) ^ num) - num;
					if (_uSmall == 0 && num == uint.MaxValue)
					{
						SetToTwo32();
					}
				}
				else
				{
					Statics.Swap(ref this, ref reg);
					BitXorSmall(uHigh2, reg._uSmall, uHigh);
				}
			}
			else if (reg._iuLast == 0)
			{
				BitXorSmall(uHigh, reg._uSmall, uHigh2);
			}
			else if (uHigh == 0)
			{
				if (uHigh2 == 0)
				{
					BitXorPositive(ref reg);
					return;
				}
				Statics.Swap(ref this, ref reg);
				BitXorNegPos(ref reg);
			}
			else if (uHigh2 == 0)
			{
				BitXorNegPos(ref reg);
			}
			else
			{
				BitXorNegative(ref reg);
			}
		}

		private void BitXorPositive(ref BigRegister reg)
		{
			int num = _iuLast;
			if (num < reg._iuLast)
			{
				num = reg._iuLast;
				Statics.Swap(ref this, ref reg);
			}
			else if (num == reg._iuLast)
			{
				while (_rgu[num] == reg._rgu[num])
				{
					if (--num <= 0)
					{
						Set(_rgu[0] ^ reg._rgu[0]);
						return;
					}
				}
			}
			SetSizeKeep(num + 1, 0);
			int num2 = Math.Min(num, reg._iuLast);
			do
			{
				_rgu[num2] ^= reg._rgu[num2];
			}
			while (--num2 >= 0);
		}

		private void BitXorNegative(ref BigRegister reg)
		{
			int b = reg.FindNonZero();
			int a = FindNonZero();
			int num = _iuLast;
			if (num < reg._iuLast)
			{
				num = reg._iuLast;
				Statics.Swap(ref this, ref reg);
				Statics.Swap(ref a, ref b);
			}
			else if (num == reg._iuLast)
			{
				if (a > b)
				{
					Statics.Swap(ref this, ref reg);
					Statics.Swap(ref a, ref b);
				}
				while (true)
				{
					if (num > b)
					{
						if (reg._rgu[num] != _rgu[num])
						{
							break;
						}
						num--;
						continue;
					}
					while (true)
					{
						if (num > a)
						{
							if (reg._rgu[num] - 1 != _rgu[num])
							{
								break;
							}
							num--;
							continue;
						}
						uint num2 = (_rgu[num] - 1) ^ (reg._rgu[num] - 1);
						if (num2 != 0 && num != 0)
						{
							break;
						}
						Set(num2);
						return;
					}
					break;
				}
			}
			SetSizeKeep(num + 1, 0);
			int num3 = Math.Min(a, b);
			ApplyBorrow(num3);
			if (_rgu[num] == 0 && num > reg._iuLast)
			{
				SetSizeKeep(num, 0);
			}
			int num4;
			for (num4 = Math.Min(_iuLast, reg._iuLast); num4 > b; num4--)
			{
				_rgu[num4] ^= reg._rgu[num4];
			}
			while (num4 >= num3)
			{
				_rgu[num4] ^= reg._rgu[num4] - 1;
				num4--;
			}
		}

		private void BitXorNegPos(ref BigRegister reg)
		{
			int a = FindNonZero();
			int b = reg.FindNonZero();
			int num;
			if (_iuLast > reg._iuLast)
			{
				EnsureWritable((a == b) ? 1 : 0);
				int sign = 1;
				Sub(ref sign, 1u);
				for (num = reg._iuLast; num >= b; num--)
				{
					_rgu[num] ^= reg._rgu[num];
				}
				Add(1u);
				return;
			}
			if (_iuLast < reg._iuLast)
			{
				Statics.Swap(ref this, ref reg);
				Statics.Swap(ref a, ref b);
				EnsureWritable((a == b) ? 1 : 0);
				for (num = reg._iuLast; num > b; num--)
				{
					_rgu[num] ^= reg._rgu[num];
				}
				while (true)
				{
					_rgu[num] ^= reg._rgu[num] - 1;
					if (num <= a)
					{
						break;
					}
					num--;
				}
			}
			else
			{
				int num2 = _iuLast;
				while (true)
				{
					if (num2 > a)
					{
						if (reg._rgu[num2] != _rgu[num2])
						{
							break;
						}
						num2--;
						continue;
					}
					while (true)
					{
						if (num2 > b)
						{
							if (reg._rgu[num2] != _rgu[num2] - 1)
							{
								break;
							}
							num2--;
							continue;
						}
						uint num3 = ((_rgu[num2] - 1) ^ reg._rgu[num2]) + 1;
						if (num2 == 0)
						{
							if (num3 == 0)
							{
								SetToTwo32();
							}
							else
							{
								Set(num3);
							}
							return;
						}
						if (num3 != 0)
						{
							break;
						}
						SetSizeClear(num2 + 2);
						_rgu[num2 + 1] = 1u;
						return;
					}
					break;
				}
				SetSizeKeep(num2 + 1, (a == b) ? 1 : 0);
				for (num = Math.Min(num2, reg._iuLast); num > a; num--)
				{
					_rgu[num] ^= reg._rgu[num];
				}
				while (true)
				{
					_rgu[num] = (_rgu[num] - 1) ^ reg._rgu[num];
					if (num <= b)
					{
						break;
					}
					num--;
				}
			}
			ApplyCarry(num);
		}

		private void BitXorSmall(uint uHigh, uint uSmall, uint uHigh2)
		{
			if (uSmall == 0)
			{
				return;
			}
			if (uHigh2 != 0)
			{
				uSmall--;
			}
			EnsureWritable();
			int sign = 1;
			if (uHigh != 0)
			{
				Sub(ref sign, 1u);
				if (_iuLast == 0)
				{
					_uSmall ^= uSmall;
				}
				else
				{
					_rgu[0] ^= uSmall;
				}
			}
			else
			{
				_rgu[0] ^= uSmall;
			}
			if (uHigh != uHigh2)
			{
				Add(1u);
			}
		}

		public bool TestBit(int sign, long ibit)
		{
			if (ibit < 0)
			{
				return false;
			}
			long num = ibit / 32;
			if (num > _iuLast)
			{
				return sign < 0;
			}
			int num2 = (int)(ibit % 32);
			if (sign < 0)
			{
				if (_iuLast == 0)
				{
					return ((0 - _uSmall) & (uint)(1 << num2)) != 0;
				}
				for (int i = 0; i < num; i++)
				{
					if (_rgu[i] != 0)
					{
						return (_rgu[num] & (1 << num2)) == 0;
					}
				}
				return ((_rgu[num] - 1) & (1 << num2)) == 0;
			}
			if (_iuLast == 0)
			{
				return (_uSmall & (uint)(1 << num2)) != 0;
			}
			return (_rgu[num] & (uint)(1 << num2)) != 0;
		}

		public bool ComputeFactorial(int n)
		{
			if (n < 0)
			{
				return false;
			}
			Set(1u);
			if (n >= 13)
			{
				int cu = (int)Math.Ceiling((((double)n + 1.0) * Math.Log((double)n + 1.0) - (double)n) / kdblLn2To32);
				try
				{
					EnsureWritable(cu, 0);
				}
				catch (OutOfMemoryException)
				{
					return false;
				}
			}
			for (uint num = 2u; num <= (uint)n; num++)
			{
				Mul(num);
			}
			return true;
		}

		public static void Reduce(ref BigRegister reg1, ref BigRegister reg2)
		{
			if (reg1._iuLast == 0)
			{
				ReduceSmall(ref reg1, ref reg2);
				return;
			}
			if (reg2._iuLast == 0)
			{
				ReduceSmall(ref reg2, ref reg1);
				return;
			}
			int i;
			for (i = 0; (reg1._rgu[i] | reg2._rgu[i]) == 0; i++)
			{
			}
			int num = Statics.CbitLowZero(reg1._rgu[i] | reg2._rgu[i]);
			if (num > 0 || i > 0)
			{
				reg1.BitShiftRightCore(1, i, num);
				reg2.BitShiftRightCore(1, i, num);
				if (reg1._iuLast == 0)
				{
					ReduceSmall(ref reg1, ref reg2);
					return;
				}
				if (reg2._iuLast == 0)
				{
					ReduceSmall(ref reg2, ref reg1);
					return;
				}
			}
			BigRegister reg3 = reg1;
			reg3._fWritable = false;
			BigRegister reg4 = reg2;
			reg4._fWritable = false;
			GCD(ref reg3, ref reg4);
			if (!reg3.IsSingle(1u))
			{
				if (reg3._rgu == reg1._rgu && reg3._iuLast == reg1._iuLast)
				{
					reg2.Div(ref reg1);
					reg1.Set(1u);
				}
				else if (reg3._rgu == reg2._rgu && reg3._iuLast == reg2._iuLast)
				{
					reg1.Div(ref reg2);
					reg2.Set(1u);
				}
				else
				{
					reg1.Div(ref reg3);
					reg2.Div(ref reg3);
				}
			}
		}

		private static void ReduceSmall(ref BigRegister regSmall, ref BigRegister regOther)
		{
			uint uSmall = regSmall._uSmall;
			if (uSmall == 1)
			{
				return;
			}
			if (regOther._iuLast == 0)
			{
				uint uSmall2 = regOther._uSmall;
				if (uSmall2 != 1)
				{
					uint num = Statics.Gcd(uSmall, uSmall2);
					if (num != 0)
					{
						regSmall.Set(uSmall / num);
						regOther.Set(uSmall2 / num);
					}
				}
			}
			else if (uSmall == 0)
			{
				regOther.Set(1u);
			}
			else
			{
				uint u = Mod(ref regOther, uSmall);
				uint num2 = Statics.Gcd(uSmall, u);
				regSmall.Set(uSmall / num2);
				regOther.DivMod(num2);
			}
		}

		private ulong GetHigh2(int cu)
		{
			if (cu - 1 <= _iuLast)
			{
				return Statics.MakeUlong(_rgu[cu - 1], _rgu[cu - 2]);
			}
			if (cu - 2 == _iuLast)
			{
				return _rgu[cu - 2];
			}
			return 0uL;
		}

		private void NegateClip()
		{
			int i;
			for (i = 0; i <= _iuLast && _rgu[i] == 0; i++)
			{
			}
			if (i <= _iuLast)
			{
				_rgu[i] = ~_rgu[i] + 1;
				while (++i <= _iuLast)
				{
					_rgu[i] = ~_rgu[i];
				}
			}
		}

		private void ApplyCarry(int iu)
		{
			while (true)
			{
				if (iu > _iuLast)
				{
					if (_iuLast + 1 == _rgu.Length)
					{
						Array.Resize(ref _rgu, _iuLast + 2);
					}
					_rgu[++_iuLast] = 1u;
					break;
				}
				if (++_rgu[iu] != 0)
				{
					break;
				}
				iu++;
			}
		}

		private void ApplyBorrow(int iuMin)
		{
			for (int i = iuMin; i <= _iuLast; i++)
			{
				if (_rgu[i]-- != 0)
				{
					break;
				}
			}
		}

		private static uint AddCarry(ref uint u1, uint u2, uint uCarry)
		{
			ulong num = (ulong)((long)u1 + (long)u2 + uCarry);
			u1 = (uint)num;
			return (uint)(num >> 32);
		}

		private static uint SubBorrow(ref uint u1, uint u2, uint uBorrow)
		{
			ulong num = (ulong)((long)u1 - (long)u2 - uBorrow);
			u1 = (uint)num;
			return (uint)(-(int)(num >> 32));
		}

		private static uint SubRevBorrow(ref uint u1, uint u2, uint uBorrow)
		{
			ulong num = (ulong)((long)u2 - (long)u1 - uBorrow);
			u1 = (uint)num;
			return (uint)(-(int)(num >> 32));
		}

		private static uint MulCarry(ref uint u1, uint u2, uint uCarry)
		{
			ulong num = (ulong)((long)u1 * (long)u2 + uCarry);
			u1 = (uint)num;
			return (uint)(num >> 32);
		}

		private static uint AddMulCarry(ref uint uAdd, uint uMul1, uint uMul2, uint uCarry)
		{
			ulong num = (ulong)((long)uMul1 * (long)uMul2 + uAdd + uCarry);
			uAdd = (uint)num;
			return (uint)(num >> 32);
		}

		private static uint SubMulBorrow(ref uint uAdd, uint uMul1, uint uMul2, uint uBorrow)
		{
			ulong num = (ulong)(uAdd - (long)uMul1 * (long)uMul2 - uBorrow);
			uAdd = (uint)num;
			return (uint)(-(int)(num >> 32));
		}

		public static void GCD(ref BigRegister reg1, ref BigRegister reg2)
		{
			if ((reg1._iuLast > 0 && reg1._rgu[0] == 0) || (reg2._iuLast > 0 && reg2._rgu[0] == 0))
			{
				int val = reg1.MakeOdd();
				int val2 = reg2.MakeOdd();
				LehmerGcd(ref reg1, ref reg2);
				int num = Math.Min(val, val2);
				if (num > 0)
				{
					reg1.BitShiftLeftCore(num / 32, num % 32);
				}
			}
			else
			{
				LehmerGcd(ref reg1, ref reg2);
			}
		}

		private static void LehmerGcd(ref BigRegister reg1, ref BigRegister reg2)
		{
			int sign = 1;
			while (true)
			{
				int a = reg1._iuLast + 1;
				int b = reg2._iuLast + 1;
				if (a < b)
				{
					Statics.Swap(ref reg1, ref reg2);
					Statics.Swap(ref a, ref b);
				}
				if (b == 1)
				{
					if (a == 1)
					{
						reg1._uSmall = Statics.Gcd(reg1._uSmall, reg2._uSmall);
					}
					else if (reg2._uSmall != 0)
					{
						reg1.Set(Statics.Gcd(Mod(ref reg1, reg2._uSmall), reg2._uSmall));
					}
					return;
				}
				if (a == 2)
				{
					break;
				}
				if (b <= a - 2)
				{
					reg1.Mod(ref reg2);
					continue;
				}
				ulong a2 = reg1.GetHigh2(a);
				ulong b2 = reg2.GetHigh2(a);
				int num = Statics.CbitHighZero(a2 | b2);
				if (num > 0)
				{
					a2 = (a2 << num) | (reg1._rgu[a - 3] >> 32 - num);
					b2 = (b2 << num) | (reg2._rgu[a - 3] >> 32 - num);
				}
				if (a2 < b2)
				{
					Statics.Swap(ref a2, ref b2);
					Statics.Swap(ref reg1, ref reg2);
				}
				if (a2 == ulong.MaxValue || b2 == ulong.MaxValue)
				{
					a2 >>= 1;
					b2 >>= 1;
				}
				if (a2 == b2)
				{
					reg1.Sub(ref sign, ref reg2);
					continue;
				}
				if (Statics.GetHi(b2) == 0)
				{
					reg1.Mod(ref reg2);
					continue;
				}
				uint num2 = 1u;
				uint num3 = 0u;
				uint num4 = 0u;
				uint num5 = 1u;
				do
				{
					uint num6 = 1u;
					ulong num7 = a2 - b2;
					while (num7 >= b2 && num6 < 32)
					{
						num7 -= b2;
						num6++;
					}
					if (num7 >= b2)
					{
						ulong num8 = a2 / b2;
						if (num8 > uint.MaxValue)
						{
							break;
						}
						num6 = (uint)num8;
						num7 = a2 - num6 * b2;
					}
					ulong num9 = (ulong)(num2 + (long)num6 * (long)num4);
					ulong num10 = (ulong)(num3 + (long)num6 * (long)num5);
					if (num9 > int.MaxValue || num10 > int.MaxValue || num7 < num10 || num7 + num9 > b2 - num4)
					{
						break;
					}
					num2 = (uint)num9;
					num3 = (uint)num10;
					a2 = num7;
					if (a2 <= num3)
					{
						break;
					}
					num6 = 1u;
					num7 = b2 - a2;
					while (num7 >= a2 && num6 < 32)
					{
						num7 -= a2;
						num6++;
					}
					if (num7 >= a2)
					{
						ulong num11 = b2 / a2;
						if (num11 > uint.MaxValue)
						{
							break;
						}
						num6 = (uint)num11;
						num7 = b2 - num6 * a2;
					}
					num9 = (ulong)(num5 + (long)num6 * (long)num3);
					num10 = (ulong)(num4 + (long)num6 * (long)num2);
					if (num9 > int.MaxValue || num10 > int.MaxValue || num7 < num10 || num7 + num9 > a2 - num3)
					{
						break;
					}
					num5 = (uint)num9;
					num4 = (uint)num10;
					b2 = num7;
				}
				while (b2 > num4);
				if (num3 == 0)
				{
					if (a2 / 2 >= b2)
					{
						reg1.Mod(ref reg2);
					}
					else
					{
						reg1.Sub(ref sign, ref reg2);
					}
					continue;
				}
				reg1.SetSizeKeep(b, 0);
				reg2.SetSizeKeep(b, 0);
				int num12 = 0;
				int num13 = 0;
				for (int i = 0; i < b; i++)
				{
					uint num14 = reg1._rgu[i];
					uint num15 = reg2._rgu[i];
					long num16 = (long)num14 * (long)num2 - (long)num15 * (long)num3 + num12;
					long num17 = (long)num15 * (long)num5 - (long)num14 * (long)num4 + num13;
					num12 = (int)(num16 >> 32);
					num13 = (int)(num17 >> 32);
					reg1._rgu[i] = (uint)num16;
					reg2._rgu[i] = (uint)num17;
				}
				reg1.Trim();
				reg2.Trim();
			}
			reg1.Set(Statics.Gcd(reg1.GetHigh2(2), reg2.GetHigh2(2)));
		}

		public int CbitLowZero()
		{
			if (_iuLast == 0)
			{
				if ((_uSmall & 1) != 0 || _uSmall == 0)
				{
					return 0;
				}
				return Statics.CbitLowZero(_uSmall);
			}
			int i;
			for (i = 0; _rgu[i] == 0; i++)
			{
			}
			int num = Statics.CbitLowZero(_rgu[i]);
			return num + i * 32;
		}

		public int MakeOdd()
		{
			int num = CbitLowZero();
			if (num > 0)
			{
				BitShiftRightCore(1, num / 32, num % 32);
			}
			return num;
		}
	}
}
