using System;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A value set of symbols
	/// </summary>
	internal class CspSymbolDomain : CspIntervalDomain
	{
		private string[] _strings;

		internal string[] Symbols => _strings;

		private CspSymbolDomain(string[] orderedSymbols)
			: base(0, orderedSymbols.Length - 1, ValueKind.Symbol, 1)
		{
			_strings = orderedSymbols;
		}

		/// <summary> A string value set {string1, string2, ...}.
		/// </summary>
		internal static CspSolverDomain Create(string[] uniqueSymbols)
		{
			return new CspSymbolDomain(uniqueSymbols);
		}

		internal override CspSolverDomain Clone()
		{
			return new CspSymbolDomain(_strings);
		}

		internal static bool IsUniqueSet(string[] symbols)
		{
			for (int i = 0; i < symbols.Length - 1; i++)
			{
				for (int j = i + 1; j < symbols.Length; j++)
				{
					if (symbols[i].Equals(symbols[j]))
					{
						return false;
					}
				}
			}
			return true;
		}

		internal static bool IsUniqueSymbolsFromDomain(CspSymbolDomain domain, string[] symbols)
		{
			int num = 0;
			int num2 = 0;
			if (symbols.Length > domain.Count)
			{
				return false;
			}
			while (num2 < symbols.Length && num < domain.Count)
			{
				if (domain._strings[num] != symbols[num2])
				{
					num++;
					continue;
				}
				num++;
				num2++;
			}
			return num2 >= symbols.Length;
		}

		internal override string AppendTo(string sline, int itemLimit)
		{
			StringBuilder stringBuilder = new StringBuilder(sline);
			int i;
			for (i = 0; i < itemLimit && i < _strings.Length - 1; i++)
			{
				stringBuilder.Append(_strings[i]).Append(", ");
			}
			if (i < _strings.Length - 1)
			{
				stringBuilder.Append("..");
			}
			stringBuilder.Append(_strings[_strings.Length - 1]);
			return stringBuilder.ToString();
		}

		internal override string AppendTo(string line, int itemLimit, CspVariable var)
		{
			return AppendTo(line, itemLimit);
		}

		/// <summary> String representation of this symbol domain
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}]", new object[1] { AppendTo("SymbolDomain [", 9) });
		}

		/// <summary>
		/// Return the string value represented by integer i
		/// </summary>
		/// <param name="i">The interger representation (starts from 0)</param>
		/// <returns></returns>
		public string GetSymbol(int i)
		{
			if (i < 0 || i >= _strings.Length)
			{
				throw new ArgumentOutOfRangeException(Resources.DomainIndexOutOfRange + ToString());
			}
			return _strings[i];
		}

		/// <summary>
		/// Return the integer representation of the string
		/// </summary>
		/// <param name="s"></param>
		/// <returns>0 if s does not belong to this CspSymbolDomain</returns>
		public int GetIntegerValue(string s)
		{
			for (int i = 0; i < _strings.Length; i++)
			{
				if (_strings[i].Equals(s))
				{
					return i;
				}
			}
			return -1;
		}
	}
}
