using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// As we inherits from MPS parser we have the entry points of ProcessSource
	/// Those will work just for the cor file, for the time and stoch one there are different methods
	/// As 
	/// </summary>
	internal class SmpsParser : MpsParser
	{
		/// <summary>
		/// One for all periods data for the whole model
		/// </summary>
		internal class PeriodsData
		{
			internal bool _implicit;

			internal Dictionary<object, bool> _isSecondStage = new Dictionary<object, bool>();
		}

		/// <summary>
		/// There will be one RandomParameterData for each stochastic parameter
		/// </summary>
		internal class RandomParameterData
		{
			internal double _firstArgument;

			internal RandomParameterModification _modification;

			internal Scenario[] _scenarios;

			internal double _secondArgument;

			internal RandomParameterType _type;

			/// <summary>
			/// For the usage of discrete parameter
			/// </summary>
			/// <param name="scenarios">Scenarios for this parameter</param>
			/// <param name="modification">Add, Multiply or Replace</param>
			public RandomParameterData(Scenario[] scenarios, RandomParameterModification modification)
			{
				DebugContracts.NonNull(scenarios);
				_scenarios = scenarios;
				_modification = modification;
				_type = RandomParameterType.Discrete;
			}

			/// <summary>
			/// For all but discrete
			/// </summary>
			/// <param name="firstArgument">first (depends on the distribution)</param>
			/// <param name="secondArgument">second (depends on the distribution)</param>
			/// <param name="type">type of distribution</param>
			public RandomParameterData(double firstArgument, double secondArgument, RandomParameterType type)
			{
				_modification = RandomParameterModification.Replace;
				_type = type;
				_firstArgument = firstArgument;
				_secondArgument = secondArgument;
			}
		}

		/// <summary>
		/// The modification of the data with respect to what has been suggested in the core file
		/// REVIEW shahark: i am not sure if that was supposed to be supported for all type of random data
		/// but for now it is only done for Discrete data
		/// </summary>
		internal enum RandomParameterModification
		{
			Replace,
			Add,
			Multiply
		}

		/// <summary>
		/// Those reflects the different type of INDEP section
		/// </summary>
		internal enum RandomParameterType
		{
			Discrete,
			Uniform,
			Normal,
			Gamma,
			Beta,
			Lognorm
		}

		private const int MaxStages = 2;

		/// <summary>
		/// As for now i preffer not changin the work of the core file parser
		/// I will take the variables from the LinearModel. I will put them to hash
		/// for efficiency
		/// </summary>
		private HashSet<object> _colsKeys;

		/// <summary>
		/// Encapsulates the data from time file
		/// </summary>
		private PeriodsData _periodsData = new PeriodsData();

		/// <summary>
		/// Encapsulates the data from the stoch file.
		/// the First Key is the row, so for each row we have a dictionary colkey-&gt;randomdata
		/// </summary>
		private Dictionary<object, Dictionary<object, RandomParameterData>> _randomParametersSubstitution = new Dictionary<object, Dictionary<object, RandomParameterData>>();

		private string[] CoreSuffixes = new string[2] { ".cor", ".core" };

		private string[] StochSuffixes = new string[2] { ".sto", ".stoch" };

		private string[] TimeSuffixes = new string[2] { ".tim", ".time" };

		/// <summary>
		/// Encapsulates the data from the stoch file.
		/// the First Key is the row, so for each row we have a dictionary colkey-&gt;randomdata
		/// </summary>
		public Dictionary<object, Dictionary<object, RandomParameterData>> RandomParametersSubstitution => _randomParametersSubstitution;

		/// <summary>
		/// Encapsulates the data from time file
		/// </summary>
		public PeriodsData PeriodsInfo => _periodsData;

		public SmpsParser(SmpsLexer lex)
			: base(lex)
		{
		}

		/// <summary> Entry point to invoke the MPS parser 
		/// </summary>
		/// <param name="path"> source</param>
		/// <param name="fFixedFormat">whether the MPS is fixed or free format</param>
		/// <param name="model">model to be filled</param>
		/// <returns></returns>
		public bool ProcessSmpsSource(string path, bool fFixedFormat, ILinearModel model)
		{
			bool flag = false;
			string filePath;
			if (Directory.Exists(path))
			{
				filePath = Path.Combine(path, Path.GetFileNameWithoutExtension(path));
			}
			else
			{
				if (!Array.Exists(CoreSuffixes, (string suffix) => string.Compare(suffix, Path.GetExtension(path), StringComparison.OrdinalIgnoreCase) == 0))
				{
					string text = string.Join(", ", CoreSuffixes);
					throw new MsfException(string.Format(CultureInfo.InvariantCulture, Resources.StochasticCoreFileShouldHaveOneOfThoseSuffixes0, new object[1] { text }));
				}
				filePath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
			}
			string text2 = "";
			string text3 = "";
			string text4 = "";
			text2 = FindFile(filePath, CoreSuffixes, string.Format(CultureInfo.InvariantCulture, Resources.StochasticFileCannotBeFound0, new object[1] { "Core" }));
			flag = ProcessCoreSource(new StaticText(text2), fFixedFormat, model);
			FinishReadFile();
			if (!flag)
			{
				return false;
			}
			_colsKeys = new HashSet<object>(model.VariableKeys);
			text3 = FindFile(filePath, TimeSuffixes, string.Format(CultureInfo.InvariantCulture, Resources.StochasticFileCannotBeFound0, new object[1] { "Time" }));
			flag = ProcessTimeSource(new StaticText(text3), fFixedFormat);
			FinishReadFile();
			if (!flag)
			{
				return false;
			}
			text4 = FindFile(filePath, StochSuffixes, string.Format(CultureInfo.InvariantCulture, Resources.StochasticFileCannotBeFound0, new object[1] { "Stoch" }));
			flag = ProcessStochSource(new StaticText(text4), fFixedFormat);
			FinishReadFile();
			if (!flag)
			{
				return false;
			}
			return true;
		}

		private static string FindFile(string filePath, string[] suffixes, string errorMessage)
		{
			string text = string.Empty;
			bool flag = false;
			foreach (string extension in suffixes)
			{
				text = Path.ChangeExtension(filePath, extension);
				if (File.Exists(text))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				throw new FileNotFoundException(errorMessage);
			}
			return text;
		}

		/// <summary>
		/// REVIEW shahark: consider refactor in MpsParser and use the same code
		/// </summary>
		/// <param name="text"></param>
		/// <param name="fFixedFormat"></param>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		private void InitiateLexer(IText text, bool fFixedFormat)
		{
			_map = new LineMapper(text.Version);
			IEnumerable<Token> rgtok = _lex.LexSource(text, fFixedFormat);
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
		}

		/// <summary> Entry point to invoke the core MPS parser 
		/// </summary>
		/// <param name="text">text source</param>
		/// <param name="fFixedFormat">whether the MPS is fixed or free format</param>
		/// <param name="mod">model to be filled</param>
		/// <returns></returns>
		public bool ProcessCoreSource(IText text, bool fFixedFormat, ILinearModel mod)
		{
			return ProcessSource(text, fFixedFormat, mod);
		}

		/// <summary> Entry point to invoke the Time parser 
		/// </summary>
		/// <param name="text">text source</param>
		/// <param name="fFixedFormat">whether the MPS is fixed or free format</param>
		/// <returns></returns>
		public bool ProcessTimeSource(IText text, bool fFixedFormat)
		{
			InitiateLexer(text, fFixedFormat);
			if (!EatTid(MpsTokKind.Time))
			{
				return false;
			}
			if (_curs.TidCur == TokKind.Ident)
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
			if (!EatTid(MpsTokKind.Periods))
			{
				return false;
			}
			bool flag;
			if (_curs.TidCur == TokKind.NewLine)
			{
				flag = true;
			}
			else
			{
				if (!TryGetMpsKind(_curs.TokCur, out var tid))
				{
					return false;
				}
				switch (tid.Tke)
				{
				case (TokKindEnum)4021:
				case (TokKindEnum)4023:
					flag = true;
					break;
				case (TokKindEnum)4022:
					flag = false;
					break;
				default:
					return false;
				}
				_curs.TidNext();
			}
			if (!EatTid(TokKind.NewLine))
			{
				return false;
			}
			_periodsData._implicit = flag;
			if (flag)
			{
				return AnalyzeImplicitPeriodSection();
			}
			return AnalyzeExplicitPeriodSection();
		}

		private bool AnalyzeImplicitPeriodSection()
		{
			for (int i = 0; i < 2; i++)
			{
				Token tokCur = _curs.TokCur;
				if (!CheckTid(Resources.WrongLineInPERIODSSection, TokKind.Ident))
				{
					return false;
				}
				_curs.TidNext();
				if (!CheckTid(Resources.WrongLineInPERIODSSection, TokKind.Ident))
				{
					return false;
				}
				_curs.TidNext();
				if (!CheckTid(Resources.WrongLineInPERIODSSection, TokKind.Ident))
				{
					return false;
				}
				_curs.TidNext();
				_periodsData._isSecondStage[tokCur.As<IdentToken>().Val] = i == 1;
				if (!EatTid(TokKind.NewLine))
				{
					return false;
				}
			}
			if (_curs.TidCur != TokKind.Eof && _curs.TidCur != MpsTokKind.EndData)
			{
				Error(Resources.OnlyTwoStageProblemsAreSupported);
				return false;
			}
			return true;
		}

		private static bool AnalyzeExplicitPeriodSection()
		{
			throw new NotImplementedException();
		}

		/// <summary> Entry point to invoke the Stoch parser 
		/// </summary>
		/// <param name="text">text source</param>
		/// <param name="fFixedFormat">whether the MPS is fixed or free format</param>
		/// <returns></returns>
		public bool ProcessStochSource(IText text, bool fFixedFormat)
		{
			InitiateLexer(text, fFixedFormat);
			if (!EatTid(MpsTokKind.Stoch))
			{
				return false;
			}
			if (_curs.TidCur == TokKind.Ident)
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
			while (_curs.TidCur != TokKind.Eof && _curs.TidCur != MpsTokKind.EndData)
			{
				if (!AnalyzeStochasticParameterSection())
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// For now just support INDEP section
		/// </summary>
		/// <returns></returns>
		private bool AnalyzeStochasticParameterSection()
		{
			if (!EatTid(MpsTokKind.Indep))
			{
				return false;
			}
			return AnalyzeIndepSection();
		}

		private bool AnalyzeIndepSection()
		{
			if (!TryGetMpsKind(_curs.TokCur, out var tid))
			{
				return false;
			}
			switch (tid.Tke)
			{
			case (TokKindEnum)4026:
				return AnalyzeGeneralDistributionIndep(RandomParameterType.Uniform);
			case (TokKindEnum)4025:
				return AnalyzeGeneralDistributionIndep(RandomParameterType.Normal);
			case (TokKindEnum)4027:
				return AnalyzeGeneralDistributionIndep(RandomParameterType.Gamma);
			case (TokKindEnum)4028:
				return AnalyzeGeneralDistributionIndep(RandomParameterType.Beta);
			case (TokKindEnum)4029:
				return AnalyzeGeneralDistributionIndep(RandomParameterType.Lognorm);
			default:
				ErrorCore(_curs.TokCur, Resources.NotSupportedRandomType);
				return false;
			case (TokKindEnum)4024:
			{
				_curs.TidNext();
				RandomParameterModification modification;
				if (!TryGetMpsKind(_curs.TokCur, out var tid2))
				{
					modification = RandomParameterModification.Replace;
				}
				else
				{
					switch (tid2.Tke)
					{
					case (TokKindEnum)4032:
						modification = RandomParameterModification.Replace;
						break;
					case (TokKindEnum)4030:
						modification = RandomParameterModification.Add;
						break;
					case (TokKindEnum)4031:
						modification = RandomParameterModification.Multiply;
						break;
					default:
						ErrorCore(_curs.TokCur, Resources.OnlyREPLACEADDAndMULTIPLYAreSupported);
						return false;
					}
					_curs.TidNext();
				}
				Token token = null;
				Token token2 = null;
				NormStr normStr = null;
				NormStr normStr2 = null;
				List<Scenario> list = new List<Scenario>();
				if (!EatTid(TokKind.NewLine))
				{
					return false;
				}
				RandomParameterData value;
				while (_curs.TidCur != TokKind.Eof && _curs.TidCur != MpsTokKind.EndData && _curs.TidCur != MpsTokKind.Indep)
				{
					if (_curs.TidCur == TokKind.NewLine)
					{
						_curs.TidNext();
						continue;
					}
					Token tokCur = _curs.TokCur;
					if (!CheckTid(Resources.WrongLineInINDEPSection, TokKind.Ident))
					{
						return false;
					}
					_curs.TidNext();
					Token tokCur2 = _curs.TokCur;
					if (!CheckTid(Resources.WrongLineInINDEPSection, TokKind.Ident))
					{
						return false;
					}
					_curs.TidNext();
					if (token != null && (normStr != tokCur2.As<IdentToken>().Val || normStr2 != tokCur.As<IdentToken>().Val))
					{
						value = new RandomParameterData(list.ToArray(), modification);
						if (!_randomParametersSubstitution.ContainsKey(normStr))
						{
							_randomParametersSubstitution[normStr] = new Dictionary<object, RandomParameterData>();
						}
						_randomParametersSubstitution[normStr].Add(GetKeyForColumn(token, token2), value);
						list.Clear();
					}
					token2 = tokCur;
					token = tokCur2;
					normStr = token.As<IdentToken>().Val;
					normStr2 = token2.As<IdentToken>().Val;
					Token tokCur3 = _curs.TokCur;
					if (!CheckTid(Resources.WrongLineInINDEPSection, TokKind.DecimalLit, TokKind.IntLit))
					{
						return false;
					}
					_curs.TidNext();
					if (!CheckTid(Resources.WrongLineInINDEPSection, TokKind.Ident))
					{
						return false;
					}
					_curs.TidNext();
					Token tokCur4 = _curs.TokCur;
					if (!CheckTid(Resources.WrongLineInINDEPSection, TokKind.DecimalLit, TokKind.IntLit))
					{
						return false;
					}
					_curs.TidNext();
					if (!EatTid(TokKind.NewLine))
					{
						return false;
					}
					tokCur4.As<DecimalLitToken>().GetRational(out var rat);
					tokCur3.As<DecimalLitToken>().GetRational(out var rat2);
					Scenario item;
					try
					{
						item = new Scenario(rat, rat2);
					}
					catch (ArgumentException ex)
					{
						Error(ex.Message);
						return false;
					}
					list.Add(item);
				}
				if (list.Count == 0)
				{
					Error(Resources.EmptyINDEPSection);
				}
				value = new RandomParameterData(list.ToArray(), modification);
				if (!_randomParametersSubstitution.ContainsKey(normStr))
				{
					_randomParametersSubstitution[normStr] = new Dictionary<object, RandomParameterData>();
				}
				_randomParametersSubstitution[normStr].Add(GetKeyForColumn(token, token2), value);
				list.Clear();
				return true;
			}
			}
		}

		/// <summary>
		/// In the case of RHS we use the row as the key for the column, as the
		/// the stochastic value has no real column (decision) which related to ir
		/// RHS column is recognized as an ident which is not used as variable
		/// </summary>
		/// <param name="rowTok">row token (e.g. Row1)</param>
		/// <param name="colTok">col token (e.g. X1 or RHS1)</param>
		/// <returns></returns>
		private NormStr GetKeyForColumn(Token rowTok, Token colTok)
		{
			NormStr val = colTok.As<IdentToken>().Val;
			if (_colsKeys.Contains(val))
			{
				return val;
			}
			return rowTok.As<IdentToken>().Val;
		}

		private static bool AnalyzeGeneralDistributionIndep(RandomParameterType type)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// This is done instead of changing the lexer behaviour
		/// so we got the TokKind instead of Ident
		/// </summary>
		/// <param name="tok"></param>
		/// <param name="tid"></param>
		/// <returns></returns>
		private bool TryGetMpsKind(Token tok, out TokKind tid)
		{
			tid = null;
			if (tok.Kind != TokKind.Ident)
			{
				return false;
			}
			NormStr val = tok.As<IdentToken>().Val;
			return _lex.IsKeyWord(val, out tid);
		}

		/// <summary>
		/// There is a bug which that disposing the file happens just when finish reading 
		/// all of it (see LexSource(IText tv, int ichInit, bool fLineStart) so this make sure we will get there 
		/// </summary>
		private void FinishReadFile()
		{
			while (_curs.TidNext() != TokKind.Eof)
			{
			}
		}

		private bool CheckTid(string errorMessage, params TokKind[] tids)
		{
			if (!Array.Exists(tids, (TokKind tid) => tid == _curs.TidCur))
			{
				string text = errorMessage + ". ";
				if (_curs.TidCur == TokKind.Error)
				{
					text += _curs.TokCur.As<ErrorToken>().Message;
				}
				else
				{
					string text2 = string.Join(Resources.OrComma, tids.Select((TokKind tid) => tid.ToString()).ToArray());
					text += string.Format(CultureInfo.InvariantCulture, Resources.Expected0, new object[1] { text2 });
				}
				Error(text);
				return false;
			}
			return true;
		}
	}
}
