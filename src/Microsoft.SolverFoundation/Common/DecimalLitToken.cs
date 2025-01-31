using System;
using System.Globalization;
using System.Text;

namespace Microsoft.SolverFoundation.Common
{
	internal class DecimalLitToken : Token
	{
		private BigInteger _bnMan;

		private BigInteger _bnExp;

		private char _chFmt;

		public char FormatChar => _chFmt;

		public BigInteger Mantissa => _bnMan;

		public BigInteger Exponent => _bnExp;

		public DecimalLitToken(BigInteger bnMan, BigInteger bnExp, char chFmt)
			: base(TokKind.DecimalLit)
		{
			_bnMan = bnMan;
			_bnExp = bnExp;
			_chFmt = chFmt;
		}

		public bool GetDouble(out double dbl)
		{
			try
			{
				dbl = double.Parse(ToStringCore(fFmt: false), CultureInfo.InvariantCulture);
			}
			catch (OverflowException)
			{
				dbl = double.PositiveInfinity;
			}
			return true;
		}

		public bool GetRational(out Rational rat)
		{
			if (_bnExp == 0)
			{
				rat = _bnMan;
				return true;
			}
			if (!BigInteger.Power((BigInteger)10, _bnExp, out rat))
			{
				return false;
			}
			rat *= (Rational)_bnMan;
			return true;
		}

		public override string ToString()
		{
			return ToStringCore(fFmt: true);
		}

		protected string ToStringCore(bool fFmt)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(_bnMan);
			BigInteger bnExp = _bnExp;
			if (bnExp < 0)
			{
				bnExp += (BigInteger)stringBuilder.Length;
				if (bnExp > 0)
				{
					stringBuilder.Insert((int)bnExp, '.');
				}
				else if (bnExp == 0)
				{
					stringBuilder.Insert(0, "0.");
				}
				else if (bnExp >= -10)
				{
					stringBuilder.Insert(0, "0." + new string('0', -(int)bnExp));
				}
				else
				{
					if (stringBuilder.Length > 1)
					{
						stringBuilder.Insert(1, '.');
					}
					bnExp -= (BigInteger)1;
					stringBuilder.AppendFormat("e{0}", bnExp);
				}
			}
			else if (bnExp < 10)
			{
				stringBuilder.Append(new string('0', (int)bnExp));
			}
			else
			{
				if (stringBuilder.Length > 1)
				{
					stringBuilder.Insert(1, '.');
					bnExp += (BigInteger)(stringBuilder.Length - 1);
				}
				stringBuilder.AppendFormat("e{0}", bnExp);
			}
			if (fFmt && _chFmt != 0)
			{
				stringBuilder.Append(_chFmt);
			}
			return stringBuilder.ToString();
		}
	}
}
