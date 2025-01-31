using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	internal class LinearModelWriter
	{
		protected class KeyStr
		{
			public string Str { get; set; }

			public KeyStr(object obj)
			{
				Str = obj.ToString();
			}
		}

		private readonly ILinearModel _mod;

		private readonly Dictionary<string, KeyStr> _mpstrkey;

		private readonly Dictionary<object, KeyStr> _mpvarkey;

		private readonly StringBuilder _sbWork;

		private int _sufNext;

		public LinearModelWriter(ILinearModel mod)
		{
			_mod = mod;
			_mpvarkey = new Dictionary<object, KeyStr>(_mod.KeyComparer);
			_mpstrkey = new Dictionary<string, KeyStr>();
			_sbWork = new StringBuilder();
		}

		public virtual void WriteModel(TextWriter wrt, bool fDouble)
		{
			BuildNameMappingTables();
			wrt.WriteLine("Model[");
			Rational rational = 0;
			Rational rational2 = Rational.PositiveInfinity;
			bool flag = false;
			bool flag2 = true;
			bool flag3 = false;
			bool flag4 = false;
			foreach (object variableKey in _mod.VariableKeys)
			{
				int indexFromKey = _mod.GetIndexFromKey(variableKey);
				_mod.GetBounds(indexFromKey, out var numLo, out var numHi);
				bool integrality = _mod.GetIntegrality(indexFromKey);
				if (flag2 || numLo != rational || numHi != rational2 || integrality != flag)
				{
					rational = numLo;
					rational2 = numHi;
					flag = integrality;
					if (flag3)
					{
						wrt.WriteLine();
						wrt.WriteLine("  ],");
					}
					wrt.WriteLine("  Decisions[");
					wrt.Write(flag ? "    Integers" : "    Reals");
					if (!rational.IsNegativeInfinity || !rational2.IsPositiveInfinity)
					{
						wrt.Write("[{0}, {1}]", NumStr(fDouble, rational), NumStr(fDouble, rational2));
					}
					wrt.WriteLine(",");
					wrt.Write("    {0}", VarStr(variableKey));
					flag3 = true;
					flag2 = false;
				}
				else
				{
					wrt.Write(", {0}", VarStr(variableKey));
				}
			}
			if (flag3)
			{
				wrt.WriteLine();
				wrt.Write("  ]");
				flag4 = true;
			}
			bool flag5 = true;
			foreach (ILinearGoal goal in _mod.Goals)
			{
				if (!goal.Enabled)
				{
					continue;
				}
				if (flag4)
				{
					wrt.WriteLine(",");
				}
				if (flag5)
				{
					wrt.WriteLine("  Goals[");
					flag5 = false;
				}
				if (goal.Minimize)
				{
					wrt.Write("    Minimize[ ");
				}
				else
				{
					wrt.Write("    Maximize[ ");
				}
				if (_mod.IsRow(goal.Index))
				{
					wrt.Write("{0} -> ", VarStr(_mod.GetKeyFromIndex(goal.Index)));
				}
				if (_mod.IsQuadraticModel)
				{
					WriteQRow(wrt, fDouble, goal.Index);
					if (_mod.GetRowEntries(goal.Index).FirstOrDefault().Key != null)
					{
						wrt.Write(' ');
						WriteRow(wrt, fDouble, goal.Index, linearObjAfterQuadObj: true);
					}
				}
				else if (!_mod.IsRow(goal.Index))
				{
					wrt.Write(VarStr(_mod.GetKeyFromIndex(goal.Index)));
				}
				else
				{
					WriteRow(wrt, fDouble, goal.Index);
				}
				wrt.Write(" ]");
				flag4 = true;
			}
			if (!flag5)
			{
				wrt.WriteLine("");
				wrt.Write("  ]");
				flag4 = true;
			}
			if (flag4)
			{
				wrt.WriteLine(",");
			}
			wrt.WriteLine("  Constraints[");
			int num = 0;
			foreach (object rowKey in _mod.RowKeys)
			{
				int indexFromKey2 = _mod.GetIndexFromKey(rowKey);
				if (_mod.GetIgnoreBounds(indexFromKey2))
				{
					continue;
				}
				_mod.GetBounds(indexFromKey2, out var numLo2, out var numHi2);
				if (!numLo2.IsNegativeInfinity || !numHi2.IsPositiveInfinity)
				{
					if (num > 0)
					{
						wrt.WriteLine(",");
					}
					num++;
					wrt.Write("    ");
					wrt.Write("{0} -> ", VarStr(rowKey));
					if (numLo2 == numHi2)
					{
						WriteRow(wrt, fDouble, indexFromKey2);
						wrt.Write(" == {0}", NumStr(fDouble, numLo2));
					}
					else if (numLo2 == Rational.NegativeInfinity)
					{
						WriteRow(wrt, fDouble, indexFromKey2);
						wrt.Write(" <= {0}", NumStr(fDouble, numHi2));
					}
					else if (numHi2 == Rational.PositiveInfinity)
					{
						WriteRow(wrt, fDouble, indexFromKey2);
						wrt.Write(" >= {0}", NumStr(fDouble, numLo2));
					}
					else
					{
						wrt.Write("{0} <= ", numLo2);
						WriteRow(wrt, fDouble, indexFromKey2);
						wrt.Write(" <= {0}", NumStr(fDouble, numHi2));
					}
				}
			}
			if (num > 0)
			{
				wrt.WriteLine();
			}
			wrt.WriteLine("  ]");
			wrt.WriteLine("]");
		}

		protected virtual void BuildNameMappingTables()
		{
			_mpvarkey.Clear();
			_mpstrkey.Clear();
			foreach (object key in _mod.Keys)
			{
				KeyStr keyStr = new KeyStr(key);
				SanitizeKeyStr(keyStr);
				_mpvarkey.Add(key, keyStr);
				_mpstrkey.Add(keyStr.Str, keyStr);
			}
		}

		protected virtual void SanitizeKeyStr(KeyStr key)
		{
			key.Str = key.Str.Trim();
			if (key.Str.Length == 0)
			{
				key.Str = MangleName("v");
				return;
			}
			bool flag = false;
			for (int i = 0; i < key.Str.Length; i++)
			{
				if (!IsIdentChar(key.Str[i]))
				{
					if (!flag)
					{
						_sbWork.Length = 0;
						_sbWork.Append(key.Str);
						flag = true;
					}
					_sbWork[i] = 'x';
				}
			}
			if (!IsIdentStartChar(flag ? _sbWork[0] : key.Str[0]))
			{
				if (!flag)
				{
					_sbWork.Length = 0;
					_sbWork.Append('v').Append(key.Str);
					flag = true;
				}
				else
				{
					_sbWork.Insert(0, 'v');
				}
			}
			if (flag)
			{
				key.Str = _sbWork.ToString();
			}
			if (_mpstrkey.ContainsKey(key.Str))
			{
				key.Str = MangleName(key.Str);
			}
		}

		protected virtual string MangleName(string strBase)
		{
			_sbWork.Length = 0;
			_sbWork.Append(strBase);
			int length = _sbWork.Length;
			string text;
			do
			{
				_sbWork.Length = length;
				_sbWork.Append(_sufNext++);
				text = _sbWork.ToString();
			}
			while (_mpstrkey.ContainsKey(text));
			return text;
		}

		protected virtual bool IsIdentStartChar(char ch)
		{
			switch (char.GetUnicodeCategory(ch))
			{
			case UnicodeCategory.UppercaseLetter:
			case UnicodeCategory.LowercaseLetter:
			case UnicodeCategory.TitlecaseLetter:
			case UnicodeCategory.ModifierLetter:
			case UnicodeCategory.OtherLetter:
			case UnicodeCategory.LetterNumber:
				return true;
			default:
				return false;
			}
		}

		protected virtual bool IsIdentChar(char ch)
		{
			switch (char.GetUnicodeCategory(ch))
			{
			case UnicodeCategory.UppercaseLetter:
			case UnicodeCategory.LowercaseLetter:
			case UnicodeCategory.TitlecaseLetter:
			case UnicodeCategory.ModifierLetter:
			case UnicodeCategory.OtherLetter:
			case UnicodeCategory.NonSpacingMark:
			case UnicodeCategory.SpacingCombiningMark:
			case UnicodeCategory.DecimalDigitNumber:
			case UnicodeCategory.LetterNumber:
			case UnicodeCategory.Format:
			case UnicodeCategory.ConnectorPunctuation:
				return true;
			default:
				return false;
			}
		}

		/// <summary>Writes a quadratic goal
		/// </summary>
		/// <param name="wrt"></param>
		/// <param name="fDouble"></param>
		/// <param name="goalIndex"></param>
		protected virtual void WriteQRow(TextWriter wrt, bool fDouble, int goalIndex)
		{
			int num = 0;
			foreach (int variableIndex in _mod.VariableIndices)
			{
				for (int i = 0; i <= variableIndex; i++)
				{
					Rational rational = _mod.GetCoefficient(goalIndex, variableIndex, i);
					if (rational.IsZero)
					{
						continue;
					}
					if (num > 0)
					{
						if (rational.Sign > 0)
						{
							wrt.Write(" + ");
						}
						else
						{
							wrt.Write(" - ");
							rational = -rational;
						}
					}
					num++;
					if (rational.IsOne)
					{
						wrt.Write("{0} * {1}", VarStr(_mod.GetKeyFromIndex(i)), VarStr(_mod.GetKeyFromIndex(variableIndex)));
					}
					else if (rational == -1)
					{
						wrt.Write("-{0} * {1}", VarStr(_mod.GetKeyFromIndex(i)), VarStr(_mod.GetKeyFromIndex(variableIndex)));
					}
					else
					{
						wrt.Write("{0} * {1} * {2}", NumStr(fDouble: true, rational), VarStr(_mod.GetKeyFromIndex(i)), VarStr(_mod.GetKeyFromIndex(variableIndex)));
					}
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="wrt"></param>
		/// <param name="fDouble"></param>
		/// <param name="vid"></param>
		/// <param name="linearObjAfterQuadObj">if the goal combines from Quad part and linear part 
		/// there should be sign to connect them</param>
		private void WriteRow(TextWriter wrt, bool fDouble, int vid, bool linearObjAfterQuadObj)
		{
			int num = 0;
			if (linearObjAfterQuadObj)
			{
				num = 1;
			}
			foreach (LinearEntry rowEntry in _mod.GetRowEntries(vid))
			{
				if (rowEntry.Index == vid)
				{
					continue;
				}
				Rational rational = rowEntry.Value;
				if (num > 0)
				{
					if (rational.Sign > 0)
					{
						wrt.Write(" + ");
					}
					else
					{
						wrt.Write(" - ");
						rational = -rational;
					}
				}
				num++;
				if (rational.IsOne)
				{
					wrt.Write("{0}", VarStr(rowEntry.Key));
				}
				else if (rational == -1)
				{
					wrt.Write("-{0}", VarStr(rowEntry.Key));
				}
				else
				{
					wrt.Write("{0} * {1}", NumStr(fDouble, rational), VarStr(rowEntry.Key));
				}
			}
			if (num == 0)
			{
				wrt.Write('0');
			}
		}

		protected virtual void WriteRow(TextWriter wrt, bool fDouble, int vid)
		{
			WriteRow(wrt, fDouble, vid, linearObjAfterQuadObj: false);
		}

		protected string NumStr(bool fDouble, Rational num)
		{
			if (fDouble)
			{
				_sbWork.Length = 0;
				if (num.Sign < 0)
				{
					_sbWork.Append('-');
					Rational.Negate(ref num);
				}
				num.AppendDecimalString(_sbWork, 23);
				return _sbWork.ToString();
			}
			return num.ToString();
		}

		protected string VarStr(object var)
		{
			return _mpvarkey[var].Str;
		}
	}
}
