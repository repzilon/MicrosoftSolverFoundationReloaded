using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Kind of token
	/// </summary>
	internal class TokKind
	{
		/// <summary> NoneId
		/// </summary>
		public const TokKindEnum NoneId = (TokKindEnum)0;

		/// <summary> IdentId
		/// </summary>
		public const TokKindEnum IdentId = (TokKindEnum)1;

		/// <summary> CommentId
		/// </summary>
		public const TokKindEnum CommentId = (TokKindEnum)2;

		/// <summary> IntLitId
		/// </summary>
		public const TokKindEnum IntLitId = (TokKindEnum)3;

		/// <summary> DecimalLitId
		/// </summary>
		public const TokKindEnum DecimalLitId = (TokKindEnum)5;

		/// <summary> CharLitId
		/// </summary>
		public const TokKindEnum CharLitId = (TokKindEnum)7;

		/// <summary> StrLitId
		/// </summary>
		public const TokKindEnum StrLitId = (TokKindEnum)8;

		/// <summary> AddId
		/// </summary>
		public const TokKindEnum AddId = (TokKindEnum)20;

		/// <summary> AddAddId
		/// </summary>
		public const TokKindEnum AddAddId = (TokKindEnum)21;

		/// <summary> AddEquId
		/// </summary>
		public const TokKindEnum AddEquId = (TokKindEnum)22;

		/// <summary> SubId
		/// </summary>
		public const TokKindEnum SubId = (TokKindEnum)23;

		/// <summary> SubSubId
		/// </summary>
		public const TokKindEnum SubSubId = (TokKindEnum)24;

		/// <summary> SubEquId
		/// </summary>
		public const TokKindEnum SubEquId = (TokKindEnum)25;

		/// <summary> SubGrtId
		/// </summary>
		public const TokKindEnum SubGrtId = (TokKindEnum)26;

		/// <summary> GrtId
		/// </summary>
		public const TokKindEnum GrtId = (TokKindEnum)27;

		/// <summary> GrtGrtId
		/// </summary>
		public const TokKindEnum GrtGrtId = (TokKindEnum)28;

		/// <summary> GrtGrtEquId
		/// </summary>
		public const TokKindEnum GrtGrtEquId = (TokKindEnum)29;

		/// <summary> GrtEquId
		/// </summary>
		public const TokKindEnum GrtEquId = (TokKindEnum)30;

		/// <summary> LssId
		/// </summary>
		public const TokKindEnum LssId = (TokKindEnum)31;

		/// <summary> LssLssId
		/// </summary>
		public const TokKindEnum LssLssId = (TokKindEnum)32;

		/// <summary> LssLssEquId
		/// </summary>
		public const TokKindEnum LssLssEquId = (TokKindEnum)33;

		/// <summary> LssEquId
		/// </summary>
		public const TokKindEnum LssEquId = (TokKindEnum)34;

		/// <summary> AndId
		/// </summary>
		public const TokKindEnum AndId = (TokKindEnum)35;

		/// <summary> AndAndId
		/// </summary>
		public const TokKindEnum AndAndId = (TokKindEnum)36;

		/// <summary> AndEquId
		/// </summary>
		public const TokKindEnum AndEquId = (TokKindEnum)37;

		/// <summary> OrId
		/// </summary>
		public const TokKindEnum OrId = (TokKindEnum)38;

		/// <summary> OrOrId
		/// </summary>
		public const TokKindEnum OrOrId = (TokKindEnum)39;

		/// <summary> OrEquId
		/// </summary>
		public const TokKindEnum OrEquId = (TokKindEnum)40;

		/// <summary> MulId
		/// </summary>
		public const TokKindEnum MulId = (TokKindEnum)41;

		/// <summary> MulEquId
		/// </summary>
		public const TokKindEnum MulEquId = (TokKindEnum)42;

		/// <summary> DivId
		/// </summary>
		public const TokKindEnum DivId = (TokKindEnum)43;

		/// <summary> DivEquId
		/// </summary>
		public const TokKindEnum DivEquId = (TokKindEnum)44;

		/// <summary> NotId
		/// </summary>
		public const TokKindEnum NotId = (TokKindEnum)45;

		/// <summary> NotEquId
		/// </summary>
		public const TokKindEnum NotEquId = (TokKindEnum)46;

		/// <summary> EquId
		/// </summary>
		public const TokKindEnum EquId = (TokKindEnum)47;

		/// <summary> EquEquId
		/// </summary>
		public const TokKindEnum EquEquId = (TokKindEnum)48;

		/// <summary> ModId
		/// </summary>
		public const TokKindEnum ModId = (TokKindEnum)49;

		/// <summary> ModEquId
		/// </summary>
		public const TokKindEnum ModEquId = (TokKindEnum)50;

		/// <summary> XorId
		/// </summary>
		public const TokKindEnum XorId = (TokKindEnum)51;

		/// <summary> XorEquId
		/// </summary>
		public const TokKindEnum XorEquId = (TokKindEnum)52;

		/// <summary> QuestId
		/// </summary>
		public const TokKindEnum QuestId = (TokKindEnum)53;

		/// <summary> QuestQuestId
		/// </summary>
		public const TokKindEnum QuestQuestId = (TokKindEnum)54;

		/// <summary> ColonId
		/// </summary>
		public const TokKindEnum ColonId = (TokKindEnum)55;

		/// <summary> ColonColonId
		/// </summary>
		public const TokKindEnum ColonColonId = (TokKindEnum)56;

		/// <summary> TildeId
		/// </summary>
		public const TokKindEnum TildeId = (TokKindEnum)57;

		/// <summary> DotId
		/// </summary>
		public const TokKindEnum DotId = (TokKindEnum)58;

		/// <summary> CommaId
		/// </summary>
		public const TokKindEnum CommaId = (TokKindEnum)59;

		/// <summary> SemiId
		/// </summary>
		public const TokKindEnum SemiId = (TokKindEnum)60;

		/// <summary> HashId
		/// </summary>
		public const TokKindEnum HashId = (TokKindEnum)61;

		/// <summary> DollarId
		/// </summary>
		public const TokKindEnum DollarId = (TokKindEnum)62;

		/// <summary> BackSlashId
		/// </summary>
		public const TokKindEnum BackSlashId = (TokKindEnum)63;

		/// <summary> BackTickId
		/// </summary>
		public const TokKindEnum BackTickId = (TokKindEnum)64;

		/// <summary> CurlOpenId
		/// </summary>
		public const TokKindEnum CurlOpenId = (TokKindEnum)70;

		/// <summary> CurlCloseId
		/// </summary>
		public const TokKindEnum CurlCloseId = (TokKindEnum)71;

		/// <summary> ParenOpenId
		/// </summary>
		public const TokKindEnum ParenOpenId = (TokKindEnum)72;

		/// <summary> ParenCloseId
		/// </summary>
		public const TokKindEnum ParenCloseId = (TokKindEnum)73;

		/// <summary> SquareOpenId
		/// </summary>
		public const TokKindEnum SquareOpenId = (TokKindEnum)74;

		/// <summary> SquareCloseId
		/// </summary>
		public const TokKindEnum SquareCloseId = (TokKindEnum)75;

		/// <summary> EofId
		/// </summary>
		public const TokKindEnum EofId = (TokKindEnum)80;

		/// <summary> NewLineId
		/// </summary>
		public const TokKindEnum NewLineId = (TokKindEnum)81;

		/// <summary> ErrorId
		/// </summary>
		public const TokKindEnum ErrorId = (TokKindEnum)82;

		private static readonly ReaderWriterLock _lock = new ReaderWriterLock();

		private static readonly Dictionary<TokKindEnum, TokKind> _mptketid = new Dictionary<TokKindEnum, TokKind>();

		/// <summary> None
		/// </summary>
		public static readonly TokKind None = new TokKind("<None>", (TokKindEnum)0);

		/// <summary> Ident
		/// </summary>
		public static readonly TokKind Ident = new TokKind("<Ident>", (TokKindEnum)1);

		/// <summary> Comment
		/// </summary>
		public static readonly TokKind Comment = new TokKind("<Comment>", (TokKindEnum)2);

		/// <summary> IntLit
		/// </summary>
		public static readonly TokKind IntLit = new TokKind("<IntLit>", (TokKindEnum)3);

		/// <summary> DecimalLit
		/// </summary>
		public static readonly TokKind DecimalLit = new TokKind("<DecimalLit>", (TokKindEnum)5);

		/// <summary> CharLit
		/// </summary>
		public static readonly TokKind CharLit = new TokKind("<CharLit>", (TokKindEnum)7);

		/// <summary> StrLit
		/// </summary>
		public static readonly TokKind StrLit = new TokKind("<StrLit>", (TokKindEnum)8);

		/// <summary> Add
		/// </summary>
		public static readonly TokKind Add = new TokKind("+", (TokKindEnum)20);

		/// <summary> AddAdd
		/// </summary>
		public static readonly TokKind AddAdd = new TokKind("++", (TokKindEnum)21);

		/// <summary> AddEqu
		/// </summary>
		public static readonly TokKind AddEqu = new TokKind("+=", (TokKindEnum)22);

		/// <summary> Sub
		/// </summary>
		public static readonly TokKind Sub = new TokKind("-", (TokKindEnum)23);

		/// <summary> SubSub
		/// </summary>
		public static readonly TokKind SubSub = new TokKind("--", (TokKindEnum)24);

		/// <summary> SubEqu
		/// </summary>
		public static readonly TokKind SubEqu = new TokKind("-=", (TokKindEnum)25);

		/// <summary> SubGrt
		/// </summary>
		public static readonly TokKind SubGrt = new TokKind("->", (TokKindEnum)26);

		/// <summary> Grt
		/// </summary>
		public static readonly TokKind Grt = new TokKind(">", (TokKindEnum)27);

		/// <summary> GrtGrt
		/// </summary>
		public static readonly TokKind GrtGrt = new TokKind(">>", (TokKindEnum)28);

		/// <summary> GrtGrtEqu
		/// </summary>
		public static readonly TokKind GrtGrtEqu = new TokKind(">>=", (TokKindEnum)29);

		/// <summary> GrtEqu
		/// </summary>
		public static readonly TokKind GrtEqu = new TokKind(">=", (TokKindEnum)30);

		/// <summary> Lss
		/// </summary>
		public static readonly TokKind Lss = new TokKind("<", (TokKindEnum)31);

		/// <summary> LssLss
		/// </summary>
		public static readonly TokKind LssLss = new TokKind("<<", (TokKindEnum)32);

		/// <summary> LssLssEqu
		/// </summary>
		public static readonly TokKind LssLssEqu = new TokKind("<<=", (TokKindEnum)33);

		/// <summary> LssEqu
		/// </summary>
		public static readonly TokKind LssEqu = new TokKind("<=", (TokKindEnum)34);

		/// <summary> And
		/// </summary>
		public static readonly TokKind And = new TokKind("&", (TokKindEnum)35);

		/// <summary> AndAnd
		/// </summary>
		public static readonly TokKind AndAnd = new TokKind("&&", (TokKindEnum)36);

		/// <summary> AndEqu
		/// </summary>
		public static readonly TokKind AndEqu = new TokKind("&=", (TokKindEnum)37);

		/// <summary> Or
		/// </summary>
		public static readonly TokKind Or = new TokKind("|", (TokKindEnum)38);

		/// <summary> OrOr
		/// </summary>
		public static readonly TokKind OrOr = new TokKind("||", (TokKindEnum)39);

		/// <summary> OrEqu
		/// </summary>
		public static readonly TokKind OrEqu = new TokKind("|=", (TokKindEnum)40);

		/// <summary> Mul
		/// </summary>
		public static readonly TokKind Mul = new TokKind("*", (TokKindEnum)41);

		/// <summary> MulEqu
		/// </summary>
		public static readonly TokKind MulEqu = new TokKind("*=", (TokKindEnum)42);

		/// <summary> Div
		/// </summary>
		public static readonly TokKind Div = new TokKind("/", (TokKindEnum)43);

		/// <summary> DivEqu
		/// </summary>
		public static readonly TokKind DivEqu = new TokKind("/=", (TokKindEnum)44);

		/// <summary> Not
		/// </summary>
		public static readonly TokKind Not = new TokKind("!", (TokKindEnum)45);

		/// <summary> NotEqu
		/// </summary>
		public static readonly TokKind NotEqu = new TokKind("!=", (TokKindEnum)46);

		/// <summary> Equ
		/// </summary>
		public static readonly TokKind Equ = new TokKind("=", (TokKindEnum)47);

		/// <summary> EquEqu
		/// </summary>
		public static readonly TokKind EquEqu = new TokKind("==", (TokKindEnum)48);

		/// <summary> Mod
		/// </summary>
		public static readonly TokKind Mod = new TokKind("%", (TokKindEnum)49);

		/// <summary> ModEqu
		/// </summary>
		public static readonly TokKind ModEqu = new TokKind("%=", (TokKindEnum)50);

		/// <summary> Xor
		/// </summary>
		public static readonly TokKind Xor = new TokKind("^", (TokKindEnum)51);

		/// <summary> XorEqu
		/// </summary>
		public static readonly TokKind XorEqu = new TokKind("^=", (TokKindEnum)52);

		/// <summary> Quest
		/// </summary>
		public static readonly TokKind Quest = new TokKind("?", (TokKindEnum)53);

		/// <summary> QuestQuest
		/// </summary>
		public static readonly TokKind QuestQuest = new TokKind("??", (TokKindEnum)54);

		/// <summary> Colon
		/// </summary>
		public static readonly TokKind Colon = new TokKind(":", (TokKindEnum)55);

		/// <summary> ColonColon
		/// </summary>
		public static readonly TokKind ColonColon = new TokKind("::", (TokKindEnum)56);

		/// <summary> Tilde
		/// </summary>
		public static readonly TokKind Tilde = new TokKind("~", (TokKindEnum)57);

		/// <summary> Dot
		/// </summary>
		public static readonly TokKind Dot = new TokKind(".", (TokKindEnum)58);

		/// <summary> Comma
		/// </summary>
		public static readonly TokKind Comma = new TokKind(",", (TokKindEnum)59);

		/// <summary> Semi
		/// </summary>
		public static readonly TokKind Semi = new TokKind(";", (TokKindEnum)60);

		/// <summary> Hash
		/// </summary>
		public static readonly TokKind Hash = new TokKind("#", (TokKindEnum)61);

		/// <summary> Dollar
		/// </summary>
		public static readonly TokKind Dollar = new TokKind("$", (TokKindEnum)62);

		/// <summary> BackSlash
		/// </summary>
		public static readonly TokKind BackSlash = new TokKind("\\", (TokKindEnum)63);

		/// <summary> BackTick
		/// </summary>
		public static readonly TokKind BackTick = new TokKind("`", (TokKindEnum)64);

		/// <summary> CurlOpen
		/// </summary>
		public static readonly TokKind CurlOpen = new TokKind("{", (TokKindEnum)70);

		/// <summary> CurlClose
		/// </summary>
		public static readonly TokKind CurlClose = new TokKind("}", (TokKindEnum)71);

		/// <summary> ParenOpen
		/// </summary>
		public static readonly TokKind ParenOpen = new TokKind("(", (TokKindEnum)72);

		/// <summary> ParenClose
		/// </summary>
		public static readonly TokKind ParenClose = new TokKind(")", (TokKindEnum)73);

		/// <summary> SquareOpen
		/// </summary>
		public static readonly TokKind SquareOpen = new TokKind("[", (TokKindEnum)74);

		/// <summary> SquareClose
		/// </summary>
		public static readonly TokKind SquareClose = new TokKind("]", (TokKindEnum)75);

		/// <summary> NewLine
		/// </summary>
		public static readonly TokKind NewLine = new TokKind("<NewLine>", (TokKindEnum)81);

		/// <summary> Error
		/// </summary>
		public static readonly TokKind Error = new TokKind("<Error>", (TokKindEnum)82);

		/// <summary> Eof
		/// </summary>
		public static readonly TokKind Eof = new TokKind("<Eof>", (TokKindEnum)80);

		private readonly string _str;

		private readonly TokKindEnum _tke;

		internal TokKindEnum Tke => _tke;

		internal TokKind(string str, TokKindEnum tke)
		{
			_str = str;
			_tke = tke;
			_lock.AcquireWriterLock(-1);
			try
			{
				if (_mptketid.TryGetValue(_tke, out var _))
				{
					throw new InvalidOperationException(Resources.DuplicateTokKindForTokKindEnumValue);
				}
				_mptketid[tke] = this;
			}
			finally
			{
				_lock.ReleaseWriterLock();
			}
		}

		internal static TokKind TidFromTke(TokKindEnum tke)
		{
			_lock.AcquireReaderLock(-1);
			try
			{
				if (!_mptketid.TryGetValue(tke, out var value))
				{
					return null;
				}
				return value;
			}
			finally
			{
				_lock.ReleaseReaderLock();
			}
		}

		/// <summary> string representation of this
		/// </summary>
		public override string ToString()
		{
			return _str;
		}
	}
}
