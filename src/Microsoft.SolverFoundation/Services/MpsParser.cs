using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// MPS file parser
	/// </summary>
	public class MpsParser
	{
		internal enum LinearRowKind
		{
			Objective,
			Less,
			Greater,
			Equal,
			Lim
		}

		private class ProcessQuadraticColumns
		{
			private int cvInteger;

			private ILinearModel mod;

			private Dictionary<NormStr, int> mpnstrvid;

			private MpsParser outer;

			private Dictionary<NormStr, bool> quadTerms;

			private int vidRow;

			public int IntegerVarCount
			{
				get
				{
					return cvInteger;
				}
				set
				{
					cvInteger = value;
				}
			}

			public int VidRow
			{
				set
				{
					vidRow = value;
				}
			}

			public ProcessQuadraticColumns(MpsParser outer, ILinearModel mod, Dictionary<NormStr, int> mpnstrvid, Dictionary<NormStr, bool> quadTerms)
			{
				this.mod = mod;
				this.outer = outer;
				this.mpnstrvid = mpnstrvid;
				this.quadTerms = quadTerms;
			}

			public bool GetVarEntry(NormStr nstrCol1, NormStr nstrCol2, Rational num)
			{
				if (quadTerms.TryGetValue(nstrCol1, out var value) && !value)
				{
					quadTerms[nstrCol1] = true;
				}
				if (quadTerms.TryGetValue(nstrCol2, out value) && !value)
				{
					quadTerms[nstrCol2] = true;
				}
				if (!mpnstrvid.TryGetValue(nstrCol1, out var value2))
				{
					outer.Error(Resources.UnknownVariable0, nstrCol1);
					return false;
				}
				if (!mpnstrvid.TryGetValue(nstrCol2, out var value3))
				{
					outer.Error(Resources.UnknownVariable0, nstrCol2);
					return false;
				}
				if (value2 == value3)
				{
					mod.SetCoefficient(vidRow, num / 2, value2, value3);
				}
				else
				{
					mod.SetCoefficient(vidRow, num, value2, value3);
				}
				return true;
			}

			public bool GetVarMarker(NormStr nstrCol, NormStr nstr1, NormStr nstr2)
			{
				if (nstr1.ToString() != "'MARKER'")
				{
					outer.Error(Resources.SyntaxError012, nstrCol, nstr1, nstr2);
					return false;
				}
				if (nstr2.ToString() == "'INTORG'")
				{
					cvInteger++;
				}
				else
				{
					if (!(nstr2.ToString() == "'INTEND'"))
					{
						outer.Error(Resources.UnknownMarker, nstr2);
						return false;
					}
					if (--cvInteger < 0)
					{
						outer.Error(Resources.UnexpectedINTENDMarker);
						return false;
					}
				}
				return true;
			}
		}

		private static int constCount;

		internal readonly MpsLexer _lex;

		internal TokenCursor _curs;

		internal LineMapper _map;

		internal List<string> _errors;

		/// <summary> A constructor which uses the default lexer 
		/// </summary>
		public MpsParser()
		{
			_lex = new MpsLexer(new NormStr.Pool());
		}

		/// <summary> constructor 
		/// </summary>
		/// <param name="lex"></param>
		internal MpsParser(MpsLexer lex)
		{
			_lex = lex;
		}

		/// <summary> Entry point to invoke the MPS parser 
		/// </summary>
		/// <param name="path"> source</param>
		/// <param name="fFixedFormat">whether the MPS is fixed or free format</param>
		/// <returns></returns>
		public LinearModel ProcessSource(string path, bool fFixedFormat)
		{
			return ProcessSource(new StaticText(path), fFixedFormat);
		}

		/// <summary> Entry point to invoke the MPS parser 
		/// </summary>
		/// <param name="text">text source</param>
		/// <param name="fFixedFormat">whether the MPS is fixed or free format</param>
		/// <returns></returns>
		internal LinearModel ProcessSource(IText text, bool fFixedFormat)
		{
			LinearModel linearModel = new LinearModel(null);
			if (ProcessSource(text, fFixedFormat, linearModel))
			{
				return linearModel;
			}
			return null;
		}

		/// <summary> Parse the MPS input into the linear model
		/// </summary>
		/// <param name="path"> source</param>
		/// <param name="fFixedFormat">whether the MPS is fixed or free format</param>
		/// <param name="mod">a linear model</param>
		/// <returns></returns>
		public bool ProcessSource(string path, bool fFixedFormat, ILinearModel mod)
		{
			return ProcessSource(new StaticText(path), fFixedFormat, mod);
		}

		/// <summary> Parse the MPS input into the linear model
		/// </summary>
		/// <param name="text">text source</param>
		/// <param name="fFixedFormat">whether the MPS is fixed or free format</param>
		/// <param name="mod">a linear model</param>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		internal bool ProcessSource(IText text, bool fFixedFormat, ILinearModel mod)
		{
			bool flag = false;
			bool flag2 = false;
			_map = new LineMapper(text.Version);
			IEnumerable<Token> rgtok = _lex.LexSource(text, fFixedFormat);
			Dictionary<NormStr, bool> dictionary = new Dictionary<NormStr, bool>();
			rgtok = TokenFilter.Filter(rgtok, delegate(ref Token tok)
			{
				if (tok.Kind == TokKind.NewLine)
				{
					_map.AddNewLine(tok.As<NewLineToken>());
				}
				return true;
			});
			_curs = new TokenCursor(new NoBufList<Token>(rgtok), 0);
			while (_curs.TidCur == TokKind.NewLine)
			{
				_curs.TidNext();
			}
			if (!EatTid(MpsTokKind.Name))
			{
				return false;
			}
			while (_curs.TidCur != TokKind.NewLine && _curs.TidCur != TokKind.Eof)
			{
				_curs.TidNext();
			}
			if (!EatTid(TokKind.NewLine))
			{
				return false;
			}
			while (_curs.TidCur == TokKind.NewLine)
			{
				_curs.TidNext();
			}
			if (TryEatTid(MpsTokKind.ObjSense) && EatTid(TokKind.NewLine))
			{
				switch (_curs.TidCur.Tke)
				{
				case (TokKindEnum)4013:
				case (TokKindEnum)4014:
					flag = true;
					break;
				case (TokKindEnum)4015:
				case (TokKindEnum)4016:
					flag = false;
					break;
				default:
					return false;
				}
				_curs.TidNext();
				while (_curs.TidCur == TokKind.NewLine)
				{
					_curs.TidNext();
				}
			}
			if (!EatTid(MpsTokKind.Rows) || !EatTid(TokKind.NewLine))
			{
				return false;
			}
			Dictionary<NormStr, LinearRowKind> mpnstrkind = new Dictionary<NormStr, LinearRowKind>();
			HashSet<int> hasDefaultBound = new HashSet<int>();
			int vidRow = -1;
			while (true)
			{
				LinearRowKind linearRowKind;
				NormStr nstr3;
				int vid;
				switch (_curs.TidCur.Tke)
				{
				case (TokKindEnum)4003:
					linearRowKind = LinearRowKind.Objective;
					goto IL_01ef;
				case (TokKindEnum)4001:
					linearRowKind = LinearRowKind.Less;
					goto IL_01ef;
				case (TokKindEnum)4002:
					linearRowKind = LinearRowKind.Greater;
					goto IL_01ef;
				case (TokKindEnum)4000:
					{
						linearRowKind = LinearRowKind.Equal;
						goto IL_01ef;
					}
					IL_01ef:
					_curs.TidNext();
					if (!GetIdent(out nstr3))
					{
						return false;
					}
					if (!mod.AddRow(nstr3, out vid))
					{
						Error(Resources.DuplicateRowNames, nstr3);
						return false;
					}
					mpnstrkind.Add(nstr3, linearRowKind);
					switch (linearRowKind)
					{
					case LinearRowKind.Less:
						mod.SetBounds(vid, Rational.NegativeInfinity, Rational.Zero);
						break;
					case LinearRowKind.Greater:
						mod.SetBounds(vid, Rational.Zero, Rational.PositiveInfinity);
						break;
					case LinearRowKind.Equal:
						mod.SetBounds(vid, Rational.Zero, Rational.Zero);
						break;
					case LinearRowKind.Objective:
						mod.AddGoal(vid, 0, !flag);
						vidRow = vid;
						mod.SetBounds(vid, Rational.NegativeInfinity, Rational.PositiveInfinity);
						break;
					}
					goto IL_02e1;
				}
				break;
				IL_02e1:
				if (!EatTid(TokKind.NewLine))
				{
					return false;
				}
			}
			while (_curs.TidCur == TokKind.NewLine)
			{
				_curs.TidNext();
			}
			if (!EatTid(MpsTokKind.Columns) || !EatTid(TokKind.NewLine))
			{
				return false;
			}
			Dictionary<NormStr, int> mpnstrvid = new Dictionary<NormStr, int>();
			int cvInteger = 0;
			int sufNext = 0;
			if (!ProcessColumns(delegate(NormStr nstrRow, NormStr nstrCol, Rational num)
			{
				if (!mod.TryGetIndexFromKey(nstrRow, out var vid2) || !mod.IsRow(vid2))
				{
					Error(Resources.UnknownRow, nstrRow);
					return false;
				}
				if (!mpnstrvid.TryGetValue(nstrCol, out var value))
				{
					NormStr key = nstrCol;
					if (mod.TryGetIndexFromKey(nstrCol, out value))
					{
						key = CreateUniqueName(_lex.Pool, nstrCol, mod, ref sufNext);
					}
					mod.AddVariable(key, out value);
					mod.SetLowerBound(value, 0);
					hasDefaultBound.Add(value);
					mpnstrvid.Add(nstrCol, value);
				}
				mod.SetCoefficient(vid2, value, num);
				if (cvInteger > 0)
				{
					mod.SetIntegrality(value, fInteger: true);
				}
				return true;
			}, delegate(NormStr nstrCol, NormStr nstr1, NormStr nstr2)
			{
				if (nstr1.ToString() != "'MARKER'")
				{
					Error(Resources.SyntaxError012, nstrCol, nstr1, nstr2);
					return false;
				}
				if (nstr2.ToString() == "'INTORG'")
				{
					cvInteger++;
				}
				else
				{
					if (!(nstr2.ToString() == "'INTEND'"))
					{
						Error(Resources.UnknownMarker, nstr2);
						return false;
					}
					if (--cvInteger < 0)
					{
						Error(Resources.UnexpectedINTENDMarker);
						return false;
					}
				}
				return true;
			}))
			{
				return false;
			}
			while (_curs.TidCur == TokKind.NewLine)
			{
				_curs.TidNext();
			}
			if (TryEatTid(MpsTokKind.SETS) && EatTid(TokKind.NewLine) && !ProcessSOSColumns(mod, cplexFormat: false))
			{
				return false;
			}
			ProcessQuadraticColumns processQuadraticColumns = new ProcessQuadraticColumns(this, mod, mpnstrvid, dictionary);
			while (_curs.TidCur == TokKind.NewLine)
			{
				_curs.TidNext();
			}
			if (!EatTid(MpsTokKind.Rhs) || !EatTid(TokKind.NewLine))
			{
				return false;
			}
			NormStr nstrRhs = null;
			if (!ProcessColumns(delegate(NormStr nstrRow, NormStr nstrCol, Rational num)
			{
				if (nstrRhs == null)
				{
					nstrRhs = nstrCol;
				}
				else if (nstrRhs != nstrCol)
				{
					return true;
				}
				if (!mod.TryGetIndexFromKey(nstrRow, out var vid3) || !mod.IsRow(vid3))
				{
					Error(Resources.UnknownRow, nstrRow);
					return false;
				}
				switch (mpnstrkind[nstrRow])
				{
				case LinearRowKind.Greater:
					mod.SetLowerBound(vid3, num);
					break;
				case LinearRowKind.Less:
					mod.SetUpperBound(vid3, num);
					break;
				case LinearRowKind.Equal:
					mod.SetBounds(vid3, num, num);
					break;
				case LinearRowKind.Objective:
				{
					int vid4 = -1;
					mod.AddVariable((string)nstrRow + ++constCount, out vid4);
					num = -num;
					mod.SetBounds(vid4, num, num);
					mod.SetCoefficient(vid3, vid4, 1);
					mod.SetValue(vid4, num);
					break;
				}
				}
				return true;
			}, null))
			{
				return false;
			}
			while (TryEatTid(MpsTokKind.QSection))
			{
				processQuadraticColumns.IntegerVarCount = cvInteger;
				if (!TryReadQSection(mod, processQuadraticColumns))
				{
					return false;
				}
				cvInteger = processQuadraticColumns.IntegerVarCount;
				flag2 = true;
			}
			if (_curs.TidCur == MpsTokKind.Ranges)
			{
				if (!EatTid(MpsTokKind.Ranges) || !EatTid(TokKind.NewLine))
				{
					return false;
				}
				NormStr nstrRange = null;
				if (!ProcessColumns(delegate(NormStr nstrRow, NormStr nstrCol, Rational num)
				{
					if (nstrRange == null)
					{
						nstrRange = nstrCol;
					}
					else if (nstrRange != nstrCol)
					{
						return true;
					}
					if (!mod.TryGetIndexFromKey(nstrRow, out var vid5) || !mod.IsRow(vid5))
					{
						Error(Resources.UnknownRow, nstrRow);
						return false;
					}
					mod.GetBounds(vid5, out var numLo, out var numHi);
					switch (mpnstrkind[nstrRow])
					{
					case LinearRowKind.Less:
						numLo = numHi - num.AbsoluteValue;
						break;
					case LinearRowKind.Greater:
						numHi = numLo + num.AbsoluteValue;
						break;
					case LinearRowKind.Equal:
						if (num < 0)
						{
							numLo = numHi + num;
						}
						else
						{
							numHi = numLo + num;
						}
						break;
					}
					mod.SetBounds(vid5, numLo, numHi);
					return true;
				}, null))
				{
					return false;
				}
			}
			while (_curs.TidCur == TokKind.NewLine)
			{
				_curs.TidNext();
			}
			if (_curs.TidCur == MpsTokKind.Bounds)
			{
				if (!EatTid(MpsTokKind.Bounds) || !EatTid(TokKind.NewLine))
				{
					return false;
				}
				NormStr normStr = null;
				while (true)
				{
					TokKind tidCur = _curs.TidCur;
					bool flag3;
					NormStr nstr4;
					NormStr nstr5;
					int value2;
					Rational num2;
					switch (tidCur.Tke)
					{
					case (TokKindEnum)4004:
					case (TokKindEnum)4005:
					case (TokKindEnum)4006:
					case (TokKindEnum)4011:
					case (TokKindEnum)4012:
						flag3 = true;
						goto IL_056c;
					case (TokKindEnum)4007:
					case (TokKindEnum)4008:
					case (TokKindEnum)4009:
					case (TokKindEnum)4010:
						{
							flag3 = false;
							goto IL_056c;
						}
						IL_056c:
						_curs.TidNext();
						if (!GetIdent(out nstr4) || !GetIdent(out nstr5))
						{
							return false;
						}
						if (!mpnstrvid.TryGetValue(nstr5, out value2))
						{
							NormStr key2 = nstr5;
							if (mod.TryGetIndexFromKey(nstr5, out value2))
							{
								key2 = CreateUniqueName(_lex.Pool, nstr5, mod, ref sufNext);
							}
							mod.AddVariable(key2, out value2);
							mod.SetLowerBound(value2, 0);
							mpnstrvid.Add(nstr5, value2);
							dictionary.Add(nstr5, value: false);
						}
						num2 = 0;
						if (flag3 && !GetNum(out num2))
						{
							return false;
						}
						if (!EatTid(TokKind.NewLine))
						{
							return false;
						}
						if (normStr == null)
						{
							normStr = nstr4;
						}
						else if (normStr != nstr4)
						{
							continue;
						}
						switch (tidCur.Tke)
						{
						case (TokKindEnum)4004:
							mod.SetLowerBound(value2, num2);
							break;
						case (TokKindEnum)4005:
							mod.SetUpperBound(value2, num2);
							break;
						case (TokKindEnum)4006:
							mod.SetBounds(value2, num2, num2);
							break;
						case (TokKindEnum)4007:
							mod.SetBounds(value2, Rational.NegativeInfinity, Rational.PositiveInfinity);
							break;
						case (TokKindEnum)4008:
							mod.SetLowerBound(value2, Rational.NegativeInfinity);
							break;
						case (TokKindEnum)4009:
							mod.SetUpperBound(value2, Rational.PositiveInfinity);
							break;
						case (TokKindEnum)4010:
							mod.SetBounds(value2, 0, 1);
							mod.SetIntegrality(value2, fInteger: true);
							break;
						case (TokKindEnum)4011:
							mod.SetLowerBound(value2, num2.GetCeiling());
							mod.SetIntegrality(value2, fInteger: true);
							break;
						case (TokKindEnum)4012:
							mod.SetUpperBound(value2, num2.GetFloor());
							mod.SetIntegrality(value2, fInteger: true);
							break;
						}
						continue;
					}
					break;
				}
			}
			while (_curs.TidCur == TokKind.NewLine)
			{
				_curs.TidNext();
			}
			if (TryEatTid(MpsTokKind.SOS) && EatTid(TokKind.NewLine) && !ProcessSOSColumns(mod, cplexFormat: true))
			{
				return false;
			}
			while (TryEatTid(MpsTokKind.CSection))
			{
				if (!(mod is SecondOrderConicModel secondOrderConicModel))
				{
					Error(string.Format(CultureInfo.InvariantCulture, Resources.SectionIsValidForConicModelsOnly0, new object[1] { MpsTokKind.CSection.ToString() }));
					return false;
				}
				if (!GetIdent(out var nstr6))
				{
					Error(Resources.InvalidCSectionFormat);
					return false;
				}
				if (!TryEatTid(TokKind.Ident) && !TryEatTid(TokKind.DecimalLit) && !TryEatTid(TokKind.IntLit))
				{
					Error(Resources.InvalidCSectionFormat);
					return false;
				}
				SecondOrderConeType secondOrderConeType = SecondOrderConeType.Quadratic;
				if (GetIdent(out var nstr7))
				{
					if (nstr7.ToString() == MpsTokKind.Quad.ToString())
					{
						secondOrderConeType = SecondOrderConeType.Quadratic;
					}
					else
					{
						if (!(nstr7.ToString() == MpsTokKind.RQuad.ToString()))
						{
							Error(string.Format(CultureInfo.InvariantCulture, Resources.InvalidConeType0, new object[1] { nstr7 }));
							return false;
						}
						secondOrderConeType = SecondOrderConeType.RotatedQuadratic;
					}
					EatTid(TokKind.NewLine);
					secondOrderConicModel.AddRow(nstr6, secondOrderConeType, out var vidRow2);
					int num3 = 0;
					int num4 = ((secondOrderConeType != SecondOrderConeType.RotatedQuadratic) ? 1 : 2);
					while (_curs.TidCur == TokKind.Ident)
					{
						GetIdent(out var nstr8);
						if (mpnstrvid.TryGetValue(nstr8, out var value3))
						{
							SecondOrderConeRowType rowType = ((num3 < num4) ? SecondOrderConeRowType.PrimaryConic : SecondOrderConeRowType.Conic);
							secondOrderConicModel.AddRow(GetConicRowName(nstr6, nstr8), vidRow2, rowType, out var vidRow3);
							secondOrderConicModel.SetCoefficient(vidRow3, value3, Rational.One);
							secondOrderConicModel.SetLowerBound(vidRow3, Rational.Zero);
							if (hasDefaultBound.Contains(value3))
							{
								secondOrderConicModel.SetBounds(value3, Rational.NegativeInfinity, Rational.PositiveInfinity);
								hasDefaultBound.Remove(value3);
							}
							num3++;
							EatTid(TokKind.NewLine);
							continue;
						}
						Error(Resources.UnknownVariable0, nstr8);
						return false;
					}
					continue;
				}
				Error(Resources.NoConeTypeSpecified);
				return false;
			}
			if (!flag2 && TryEatTid(MpsTokKind.Quadobj) && EatTid(TokKind.NewLine))
			{
				processQuadraticColumns.VidRow = vidRow;
				processQuadraticColumns.IntegerVarCount = cvInteger;
				if (!ProcessColumns(processQuadraticColumns.GetVarEntry, processQuadraticColumns.GetVarMarker))
				{
					return false;
				}
			}
			bool flag4 = true;
			foreach (KeyValuePair<NormStr, bool> item in dictionary)
			{
				if (!item.Value)
				{
					Error(Resources.UnknownVariable0, item.Key);
					flag4 = false;
				}
			}
			if (!flag4 || !EatTid(MpsTokKind.EndData))
			{
				return false;
			}
			return true;
		}

		private static string GetConicRowName(NormStr nstrConeName, NormStr nstrConeCol)
		{
			return string.Concat(nstrConeName, "_", nstrConeCol);
		}

		private bool TryReadQSection(ILinearModel mod, ProcessQuadraticColumns quad)
		{
			if (!GetIdent(out var nstr) || !mod.TryGetIndexFromKey(nstr, out var vid) || !mod.IsRow(vid))
			{
				Error(Resources.UnknownRow, nstr);
				return false;
			}
			quad.VidRow = vid;
			EatTid(TokKind.NewLine);
			if (!ProcessColumns(quad.GetVarEntry, quad.GetVarMarker))
			{
				return false;
			}
			return true;
		}

		internal virtual bool ProcessColumns(Func<NormStr, NormStr, Rational, bool> pfnEntry, Func<NormStr, NormStr, NormStr, bool> pfnMarker)
		{
			int num = 0;
			while (_curs.TidCur == TokKind.Ident)
			{
				NormStr val = _curs.TokCur.As<IdentToken>().Val;
				_curs.TidNext();
				if (_curs.TidCur != TokKind.Ident)
				{
					EatTid(TokKind.Ident);
					return false;
				}
				while (_curs.TidCur == TokKind.Ident)
				{
					NormStr val2 = _curs.TokCur.As<IdentToken>().Val;
					_curs.TidNext();
					TokKindEnum tke = _curs.TidCur.Tke;
					if (tke != (TokKindEnum)1)
					{
						switch (tke)
						{
						case (TokKindEnum)82:
							ErrorCore(_curs.TokCur, _curs.TokCur.As<ErrorToken>().Message);
							return false;
						case (TokKindEnum)5:
						{
							_curs.TokCur.As<DecimalLitToken>().GetRational(out var rat);
							_curs.TidNext();
							if (pfnEntry(val2, val, rat))
							{
								continue;
							}
							return false;
						}
						}
					}
					else if (pfnMarker != null)
					{
						NormStr val3 = _curs.TokCur.As<IdentToken>().Val;
						_curs.TidNext();
						if (!pfnMarker(val, val2, val3))
						{
							return false;
						}
						continue;
					}
					Error(Resources.ExpectedNumericValue);
					return false;
				}
				if (!EatTid(TokKind.NewLine))
				{
					return false;
				}
				num++;
			}
			return true;
		}

		internal virtual bool ProcessSOSColumns(ILinearModel mod, bool cplexFormat)
		{
			while (_curs.TidCur == MpsTokKind.SOSS1 || _curs.TidCur == MpsTokKind.SOSS2)
			{
				SpecialOrderedSetType sos = ((_curs.TidCur != MpsTokKind.SOSS1) ? SpecialOrderedSetType.SOS2 : SpecialOrderedSetType.SOS1);
				_curs.TidNext();
				if (_curs.TidCur != TokKind.Ident)
				{
					ErrorCore(_curs.TokCur, Resources.SOSRowNameMissing);
					return false;
				}
				NormStr val = _curs.TokCur.As<IdentToken>().Val;
				_curs.TidNext();
				if (TryEatTid(TokKind.Ident))
				{
					_curs.TidNext();
				}
				if (TryEatTid(TokKind.Ident))
				{
					_curs.TidNext();
				}
				if (!EatTid(TokKind.NewLine))
				{
					return false;
				}
				int vid = -1;
				Rational rat = 0;
				NormStr empty = NormStr.Empty;
				if (!mod.AddRow(val, sos, out var vidRow))
				{
					return false;
				}
				while (_curs.TidCur == TokKind.Ident)
				{
					if (!cplexFormat)
					{
						val = _curs.TokCur.As<IdentToken>().Val;
						_curs.TidNext();
						if (_curs.TidCur != TokKind.Ident)
						{
							EatTid(TokKind.Ident);
							return false;
						}
					}
					while (_curs.TidCur == TokKind.Ident)
					{
						empty = _curs.TokCur.As<IdentToken>().Val;
						if (!mod.TryGetIndexFromKey(empty, out vid))
						{
							return false;
						}
						_curs.TidNext();
						if (cplexFormat)
						{
							string s = _curs.TokCur.As<IdentToken>().Val.ToString();
							if (double.TryParse(s, out var result))
							{
								rat = result;
								_curs.TidNext();
								continue;
							}
							Error(Resources.ExpectedNumericValue);
							return false;
						}
						switch (_curs.TidCur.Tke)
						{
						default:
							Error(Resources.ExpectedNumericValue);
							return false;
						case (TokKindEnum)82:
							ErrorCore(_curs.TokCur, _curs.TokCur.As<ErrorToken>().Message);
							return false;
						case (TokKindEnum)5:
							_curs.TokCur.As<DecimalLitToken>().GetRational(out rat);
							_curs.TidNext();
							break;
						}
					}
					mod.SetCoefficient(vidRow, vid, rat);
					if (!EatTid(TokKind.NewLine))
					{
						return false;
					}
				}
			}
			return true;
		}

		internal virtual void ErrorOutput(string str)
		{
			Console.WriteLine(str);
			if (_errors == null)
			{
				_errors = new List<string>();
			}
			_errors.Add(str);
		}

		internal void Error(string str)
		{
			ErrorCore(_curs.TokCur, str);
		}

		internal void Error(string str, params object[] args)
		{
			ErrorCore(_curs.TokCur, string.Format(CultureInfo.InvariantCulture, str, args));
		}

		internal virtual void ErrorCore(Token tok, string str)
		{
			_map.MapSpanToPos(tok.Span, out var spos);
			ErrorOutput(string.Format(CultureInfo.InvariantCulture, Resources.Error012345, spos.pathMin, spos.lineMin, spos.colMin, spos.lineLim, spos.colLim, str));
		}

		internal bool EatTid(TokKind tid)
		{
			if (_curs.TidCur == tid)
			{
				_curs.TidNext();
				return true;
			}
			if (_curs.TidCur.Tke == (TokKindEnum)82)
			{
				ErrorCore(_curs.TokCur, _curs.TokCur.As<ErrorToken>().Message);
			}
			else
			{
				Error(Resources.Expected0, tid);
			}
			return false;
		}

		internal bool TryEatTid(TokKind tid)
		{
			if (_curs.TidCur == tid)
			{
				_curs.TidNext();
				return true;
			}
			if (_curs.TidCur.Tke == (TokKindEnum)82)
			{
				ErrorCore(_curs.TokCur, _curs.TokCur.As<ErrorToken>().Message);
			}
			return false;
		}

		internal bool CheckTid(TokKind tid)
		{
			if (_curs.TidCur != tid)
			{
				Error(Resources.Expected0, tid);
				return false;
			}
			return true;
		}

		internal bool GetIdent(out NormStr nstr)
		{
			if (_curs.TidCur != TokKind.Ident)
			{
				Error(Resources.ExpectedIdentifier);
				nstr = null;
				return false;
			}
			nstr = _curs.TokCur.As<IdentToken>().Val;
			_curs.TidNext();
			return true;
		}

		internal bool GetNum(out Rational num)
		{
			switch (_curs.TidCur.Tke)
			{
			default:
				Error(Resources.ExpectedNumericValue);
				num = default(Rational);
				return false;
			case (TokKindEnum)82:
				ErrorCore(_curs.TokCur, _curs.TokCur.As<ErrorToken>().Message);
				num = default(Rational);
				return false;
			case (TokKindEnum)3:
				num = _curs.TokCur.As<IntLitToken>().Val;
				_curs.TidNext();
				break;
			case (TokKindEnum)5:
				_curs.TokCur.As<DecimalLitToken>().GetRational(out num);
				_curs.TidNext();
				break;
			}
			return true;
		}

		internal static bool IsLetter(char ch)
		{
			if ('a' > ch || ch > 'z')
			{
				if ('A' <= ch)
				{
					return ch <= 'Z';
				}
				return false;
			}
			return true;
		}

		internal static bool IsDigit(char ch)
		{
			if ('0' <= ch)
			{
				return ch <= '9';
			}
			return false;
		}

		internal static NormStr CreateUniqueName(NormStr.Pool pool, NormStr nstr, ILinearModel mod, ref int sufNext)
		{
			NormStr normStr;
			int vid;
			do
			{
				normStr = pool.Add(string.Format(CultureInfo.InvariantCulture, "{0}:{1}", new object[2]
				{
					nstr,
					sufNext++
				}));
			}
			while (mod.TryGetIndexFromKey(normStr, out vid));
			return normStr;
		}
	}
}
