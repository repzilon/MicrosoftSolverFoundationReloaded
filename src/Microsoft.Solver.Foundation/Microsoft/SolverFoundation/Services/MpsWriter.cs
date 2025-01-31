using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// MPS file generator
	/// </summary>
	public class MpsWriter
	{
		private const int kcchMaxFixedName = 8;

		private const int kcchMaxFixedValue = 14;

		private readonly ILinearModel _mod;

		private readonly Dictionary<string, int> _mpstrvid;

		private readonly Dictionary<int, string> _mpvidstr;

		private readonly Dictionary<int, string> _mpvidstrGoal;

		private readonly StringBuilder _sbWork;

		private int _sufNext;

		/// <summary>
		/// constructor 
		/// </summary>
		/// <param name="mod">a linear model</param>
		public MpsWriter(ILinearModel mod)
		{
			_mod = mod;
			_mpvidstr = new Dictionary<int, string>();
			_mpvidstrGoal = new Dictionary<int, string>();
			_mpstrvid = new Dictionary<string, int>();
			_sbWork = new StringBuilder();
		}

		internal static void WriteRow(TextWriter wrt, char chKind, string str)
		{
			wrt.WriteLine(" {0}  {1,-8}", chKind, str);
		}

		internal static void WriteEntry(TextWriter wrt, ref bool fFirst, string str1, string str2, string strNum)
		{
			if (fFirst)
			{
				wrt.Write("    {0,-8}  {1,-8}  {2,-14}", str1, str2, strNum);
				fFirst = false;
			}
			else
			{
				wrt.Write(" {0,-8}  {1}\r\n", str2, strNum);
				fFirst = true;
			}
		}

		internal static void WriteBound(TextWriter wrt, string strKind, string strVar, string strNum)
		{
			wrt.WriteLine(" {0} BOUND     {1,-8}  {2}", strKind, strVar, strNum);
		}

		/// <summary>
		/// Entry point to generate MPS file 
		/// </summary>
		/// <param name="wrt">output writer</param>
		/// <param name="fFixed">whether output in free or fixed format</param>
		public virtual void WriteMps(TextWriter wrt, bool fFixed)
		{
			BuildNameMappingTables(fFixed);
			bool flag = false;
			bool flag2 = false;
			bool flag3 = _mod.IsQuadraticModel;
			bool flag4 = _mod is ISecondOrderConicModel secondOrderConicModel && secondOrderConicModel.IsSocpModel;
			wrt.WriteLine("NAME          {0}", "MODEL");
			wrt.WriteLine("ROWS");
			Rational numLo;
			Rational numHi;
			foreach (int rowIndex in _mod.RowIndices)
			{
				if (_mod.GetIgnoreBounds(rowIndex))
				{
					numLo = Rational.NegativeInfinity;
					numHi = Rational.PositiveInfinity;
				}
				else
				{
					GetRowBounds(rowIndex, out numLo, out numHi);
				}
				char chKind;
				if (numLo.IsNegativeInfinity)
				{
					if (numHi.IsPositiveInfinity)
					{
						if (_mod.IsGoal(rowIndex))
						{
							_mpvidstrGoal[rowIndex] = _mpvidstr[rowIndex];
						}
						else if (!_mod.IsSpecialOrderedSet)
						{
							throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.RowIsUnbounded, new object[1] { _mpvidstr[rowIndex] }));
						}
						continue;
					}
					chKind = 'L';
				}
				else if (numLo == numHi)
				{
					chKind = 'E';
				}
				else
				{
					chKind = 'G';
					if (!numHi.IsPositiveInfinity)
					{
						flag = true;
					}
				}
				WriteRow(wrt, chKind, _mpvidstr[rowIndex]);
			}
			foreach (ILinearGoal goal2 in _mod.Goals)
			{
				if (goal2.Enabled)
				{
					WriteRow(wrt, 'N', _mpvidstrGoal[goal2.Index]);
				}
			}
			wrt.WriteLine("COLUMNS");
			bool fFirst = true;
			bool flag5 = false;
			foreach (int variableIndex in _mod.VariableIndices)
			{
				if (!fFirst)
				{
					wrt.WriteLine();
					fFirst = true;
				}
				if (_mod.GetIntegrality(variableIndex))
				{
					if (!flag5)
					{
						wrt.WriteLine("    INTMARK   'MARKER'                 'INTORG'");
						flag5 = true;
					}
				}
				else if (flag5)
				{
					wrt.WriteLine("    INTMARK   'MARKER'                 'INTEND'");
					flag5 = false;
				}
				int num = 0;
				string str = _mpvidstr[variableIndex];
				if (_mod.IsGoal(variableIndex, out var goal) && goal.Enabled)
				{
					WriteEntry(wrt, ref fFirst, str, _mpvidstrGoal[variableIndex], NumStr(goal.Minimize ? 1 : (-1), fFixed));
					num++;
				}
				foreach (LinearEntry variableEntry in _mod.GetVariableEntries(variableIndex))
				{
					int index = variableEntry.Index;
					if (_mod.GetIgnoreBounds(index) && !_mod.IsGoal(index))
					{
						continue;
					}
					Rational value = variableEntry.Value;
					if (_mod.IsGoal(index, out goal))
					{
						WriteEntry(wrt, ref fFirst, str, _mpvidstrGoal[index], NumStr(goal.Minimize ? value : (-value), fFixed));
						num++;
						if (_mpvidstr[index] == _mpvidstrGoal[index])
						{
							continue;
						}
					}
					if (!_mod.IsSpecialOrderedSet)
					{
						WriteEntry(wrt, ref fFirst, str, _mpvidstr[index], NumStr(value, fFixed));
					}
					else
					{
						bool flag6 = false;
						foreach (int specialOrderedSetTypeRowIndex in _mod.GetSpecialOrderedSetTypeRowIndexes(SpecialOrderedSetType.SOS1))
						{
							if (specialOrderedSetTypeRowIndex == index)
							{
								flag6 = true;
								break;
							}
						}
						foreach (int specialOrderedSetTypeRowIndex2 in _mod.GetSpecialOrderedSetTypeRowIndexes(SpecialOrderedSetType.SOS2))
						{
							if (specialOrderedSetTypeRowIndex2 == index)
							{
								flag6 = true;
								break;
							}
						}
						if (!flag6)
						{
							WriteEntry(wrt, ref fFirst, str, _mpvidstr[index], NumStr(value, fFixed));
						}
					}
					num++;
				}
				if (!flag2 && num > 0)
				{
					_mod.GetBounds(variableIndex, out numLo, out numHi);
					if (!numLo.IsZero || !numHi.IsPositiveInfinity)
					{
						flag2 = true;
					}
					else if (_mod.GetIntegrality(variableIndex))
					{
						flag2 = true;
					}
				}
			}
			if (!fFirst)
			{
				wrt.WriteLine();
			}
			if (flag5)
			{
				wrt.WriteLine("    INTMARK   'MARKER'                 'INTEND'");
			}
			if (_mod.IsSpecialOrderedSet)
			{
				wrt.WriteLine("SETS");
				foreach (int specialOrderedSetTypeRowIndex3 in _mod.GetSpecialOrderedSetTypeRowIndexes(SpecialOrderedSetType.SOS1))
				{
					string stringFromObject = GetStringFromObject(_mod.GetKeyFromIndex(specialOrderedSetTypeRowIndex3));
					wrt.WriteLine(" {0}  {1,-8}", "S1", stringFromObject);
					foreach (LinearEntry rowEntry in _mod.GetRowEntries(specialOrderedSetTypeRowIndex3))
					{
						string stringFromObject2 = GetStringFromObject(rowEntry.Key);
						wrt.WriteLine("    {0,-8}  {1,-8}  {2,-14}", stringFromObject, stringFromObject2, NumStr(rowEntry.Value, fFixed));
					}
				}
				foreach (int specialOrderedSetTypeRowIndex4 in _mod.GetSpecialOrderedSetTypeRowIndexes(SpecialOrderedSetType.SOS2))
				{
					string stringFromObject3 = GetStringFromObject(_mod.GetKeyFromIndex(specialOrderedSetTypeRowIndex4));
					wrt.WriteLine(" {0}  {1,-8}", "S2", stringFromObject3);
					foreach (LinearEntry rowEntry2 in _mod.GetRowEntries(specialOrderedSetTypeRowIndex4))
					{
						string stringFromObject4 = GetStringFromObject(rowEntry2.Key);
						wrt.WriteLine("    {0,-8}  {1,-8}  {2,-14}", stringFromObject3, stringFromObject4, NumStr(rowEntry2.Value, fFixed));
					}
				}
			}
			if (flag4)
			{
				WriteQuadratic(wrt, "QSECTION", fFixed);
				flag3 = false;
			}
			wrt.WriteLine("RHS");
			fFirst = true;
			foreach (int rowIndex2 in _mod.RowIndices)
			{
				if (_mod.GetIgnoreBounds(rowIndex2))
				{
					continue;
				}
				GetRowBounds(rowIndex2, out numLo, out numHi);
				Rational num2;
				if (numLo.IsNegativeInfinity)
				{
					if (numHi.IsPositiveInfinity)
					{
						continue;
					}
					num2 = numHi;
				}
				else
				{
					num2 = numLo;
				}
				if (!num2.IsZero)
				{
					WriteEntry(wrt, ref fFirst, "RHS", _mpvidstr[rowIndex2], NumStr(num2, fFixed));
				}
			}
			if (!fFirst)
			{
				wrt.WriteLine();
			}
			if (flag)
			{
				wrt.WriteLine("RANGES");
				fFirst = true;
				foreach (int rowIndex3 in _mod.RowIndices)
				{
					if (!_mod.GetIgnoreBounds(rowIndex3))
					{
						GetRowBounds(rowIndex3, out numLo, out numHi);
						if (!numLo.IsNegativeInfinity && !numHi.IsPositiveInfinity && numLo != numHi)
						{
							WriteEntry(wrt, ref fFirst, "RANGE", _mpvidstr[rowIndex3], NumStr(numHi - numLo, fFixed));
						}
					}
				}
				if (!fFirst)
				{
					wrt.WriteLine();
				}
			}
			if (flag2 || _mod.IsQuadraticModel)
			{
				wrt.WriteLine("BOUNDS");
				foreach (int variableIndex2 in _mod.VariableIndices)
				{
					if (_mod.GetVariableEntryCount(variableIndex2) == 0 && !_mod.IsQuadraticVariable(variableIndex2))
					{
						continue;
					}
					_mod.GetBounds(variableIndex2, out numLo, out numHi);
					if (numLo == numHi)
					{
						WriteBound(wrt, "FX", _mpvidstr[variableIndex2], NumStr(numLo, fFixed));
						continue;
					}
					if (_mod.GetIntegrality(variableIndex2) && numHi == 1 && numLo.IsZero)
					{
						WriteBound(wrt, "BV", _mpvidstr[variableIndex2], "");
						continue;
					}
					if (numLo.IsNegativeInfinity)
					{
						if (numHi.IsPositiveInfinity)
						{
							WriteBound(wrt, "FR", _mpvidstr[variableIndex2], "");
							continue;
						}
						WriteBound(wrt, "MI", _mpvidstr[variableIndex2], "");
					}
					else if (!numLo.IsZero)
					{
						WriteBound(wrt, "LO", _mpvidstr[variableIndex2], NumStr(numLo, fFixed));
					}
					if (!numHi.IsPositiveInfinity)
					{
						WriteBound(wrt, "UP", _mpvidstr[variableIndex2], NumStr(numHi, fFixed));
					}
				}
			}
			if (flag4)
			{
				WriteConic(wrt, "CSECTION");
			}
			if (_mod.IsQuadraticModel && flag3)
			{
				WriteQuadratic(wrt, "QUADOBJ", fFixed);
			}
			wrt.WriteLine("ENDATA");
		}

		private void WriteConic(TextWriter wrt, string title)
		{
		}

		private void WriteQuadratic(TextWriter wrt, string title, bool fFixed)
		{
			wrt.WriteLine(title);
			foreach (ILinearGoal goal in _mod.Goals)
			{
				if (!goal.Enabled)
				{
					continue;
				}
				foreach (int variableIndex in _mod.VariableIndices)
				{
					for (int i = 0; i <= variableIndex; i++)
					{
						Rational coefficient = _mod.GetCoefficient(goal.Index, variableIndex, i);
						if (variableIndex == i)
						{
							coefficient *= (Rational)2;
						}
						if (!coefficient.IsZero)
						{
							wrt.WriteLine("    {0,-8}  {1,-8} {2}", _mpvidstr[variableIndex], _mpvidstr[i], NumStr(goal.Minimize ? coefficient : (-coefficient), fFixed));
						}
					}
				}
			}
		}

		internal virtual void GetRowBounds(int vidRow, out Rational numLo, out Rational numHi)
		{
			Rational rational = -_mod.GetCoefficient(vidRow, vidRow);
			if (rational.IsOne)
			{
				_mod.GetBounds(vidRow, out numLo, out numHi);
				return;
			}
			if (rational.Sign < 0)
			{
				_mod.GetBounds(vidRow, out numHi, out numLo);
			}
			else
			{
				_mod.GetBounds(vidRow, out numLo, out numHi);
			}
			numLo *= rational;
			numHi *= rational;
		}

		internal virtual void BuildNameMappingTables(bool fFixed)
		{
			_mpvidstr.Clear();
			_mpvidstrGoal.Clear();
			_mpstrvid.Clear();
			foreach (int index2 in _mod.Indices)
			{
				object keyFromIndex = _mod.GetKeyFromIndex(index2);
				string text = SanitizeName(GetStringFromObject(keyFromIndex), fRow: true, fFixed);
				_mpvidstr.Add(index2, text);
				_mpstrvid.Add(text, index2);
			}
			foreach (ILinearGoal goal in _mod.Goals)
			{
				if (goal.Enabled)
				{
					int index = goal.Index;
					string text2 = SanitizeName(GetStringFromObject(goal.Key), fRow: true, fFixed);
					_mpvidstrGoal.Add(index, text2);
					_mpstrvid.Add(text2, index);
				}
			}
		}

		/// <summary>
		/// Get the value from obj which is Expression, and use ToString() for other objects 
		/// That's is done because ToString of Expression adds quotes to the string
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		private static string GetStringFromObject(object obj)
		{
			if (obj is Expression expression && expression.GetValue(out string val))
			{
				return val;
			}
			return obj.ToString();
		}

		internal virtual string SanitizeName(string str, bool fRow, bool fFixed)
		{
			str = str.Trim();
			if (str.Length == 0)
			{
				return MangleName(fRow ? "R" : "V", fFixed);
			}
			if (!fFixed)
			{
				bool flag = false;
				for (int i = 0; i < str.Length; i++)
				{
					if (!IsLegalFreeChar(str[i]))
					{
						if (!flag)
						{
							_sbWork.Length = 0;
							_sbWork.Append(str);
							flag = true;
						}
						_sbWork[i] = '_';
					}
				}
				if (flag)
				{
					str = _sbWork.ToString();
				}
			}
			else if (str.Length > 8)
			{
				str = str.Substring(0, 8);
			}
			if (_mpstrvid.ContainsKey(str))
			{
				str = MangleName(str, fFixed);
			}
			return str;
		}

		internal virtual bool IsLegalFreeChar(char ch)
		{
			switch (char.GetUnicodeCategory(ch))
			{
			case UnicodeCategory.SpaceSeparator:
			case UnicodeCategory.LineSeparator:
			case UnicodeCategory.ParagraphSeparator:
			case UnicodeCategory.Control:
			case UnicodeCategory.Surrogate:
			case UnicodeCategory.PrivateUse:
			case UnicodeCategory.OtherNotAssigned:
				return false;
			default:
				return true;
			}
		}

		internal virtual string MangleName(string strBase, bool fFixed)
		{
			_sbWork.Length = 0;
			_sbWork.Append(strBase);
			int num = _sbWork.Length;
			string text;
			do
			{
				_sbWork.Length = num;
				_sbWork.Append(_sufNext++);
				if (fFixed && _sbWork.Length > 8 && num > 1)
				{
					int num2 = num;
					num = Math.Max(1, num - _sbWork.Length + 8);
					_sbWork.Remove(num, num2 - num);
				}
				text = _sbWork.ToString();
			}
			while (_mpstrvid.ContainsKey(text));
			return text;
		}

		internal string NumStr(Rational num, bool fFixed)
		{
			_sbWork.Length = 0;
			if (num.Sign < 0)
			{
				_sbWork.Append('-');
				Rational.Negate(ref num);
			}
			else if (fFixed)
			{
				_sbWork.Append(' ');
			}
			if (!fFixed)
			{
				num.AppendDecimalString(_sbWork, 23);
			}
			else
			{
				num.AppendDecimalString(_sbWork, 13);
			}
			return _sbWork.ToString();
		}
	}
}
