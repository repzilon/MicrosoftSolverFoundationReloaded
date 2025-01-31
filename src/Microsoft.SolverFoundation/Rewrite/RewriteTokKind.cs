using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class RewriteTokKind : TokKind
	{
		public const TokKindEnum TrueId = (TokKindEnum)2003;

		public const TokKindEnum FalseId = (TokKindEnum)2004;

		public const TokKindEnum InfinityId = (TokKindEnum)2005;

		public const TokKindEnum UnsignedInfinityId = (TokKindEnum)2006;

		public const TokKindEnum IndeterminateId = (TokKindEnum)2007;

		public const TokKindEnum HoleId = (TokKindEnum)2010;

		public const TokKindEnum TripleHoleId = (TokKindEnum)2011;

		public const TokKindEnum SlotId = (TokKindEnum)2012;

		public const TokKindEnum SlotSpliceId = (TokKindEnum)2013;

		public const TokKindEnum AssignImmedId = (TokKindEnum)20;

		public const TokKindEnum AssignDelayedId = (TokKindEnum)2014;

		public const TokKindEnum RuleImmedId = (TokKindEnum)26;

		public const TokKindEnum RuleDelayedId = (TokKindEnum)2015;

		public const TokKindEnum RuleApplyOnceId = (TokKindEnum)2016;

		public const TokKindEnum RuleApplyManyId = (TokKindEnum)2017;

		public const TokKindEnum ConditionalId = (TokKindEnum)2018;

		public const TokKindEnum InlineFunctionId = (TokKindEnum)35;

		public const TokKindEnum UnsetId = (TokKindEnum)2019;

		public const TokKindEnum SquareColonOpenId = (TokKindEnum)2020;

		public const TokKindEnum SquareColonCloseId = (TokKindEnum)2021;

		public const TokKindEnum EquEquEquId = (TokKindEnum)2022;

		public const TokKindEnum NotEquEquId = (TokKindEnum)2023;

		public const TokKindEnum CaretOrId = (TokKindEnum)2024;

		public const TokKindEnum MinusColonId = (TokKindEnum)2025;

		public const TokKindEnum ExcelInputBindingId = (TokKindEnum)2026;

		public const TokKindEnum ExcelOutputBindingId = (TokKindEnum)2027;

		public static readonly TokKind True = new RewriteTokKind("True", (TokKindEnum)2003);

		public static readonly TokKind False = new RewriteTokKind("False", (TokKindEnum)2004);

		public static readonly TokKind Infinity = new RewriteTokKind("Infinity", (TokKindEnum)2005);

		public static readonly TokKind UnsignedInfinity = new RewriteTokKind("UnsignedInfinity", (TokKindEnum)2006);

		public static readonly TokKind Indeterminate = new RewriteTokKind("Indeterminate", (TokKindEnum)2007);

		public static readonly TokKind Hole = new RewriteTokKind("_", (TokKindEnum)2010);

		public static readonly TokKind TripleHole = new RewriteTokKind("___", (TokKindEnum)2011);

		public static readonly TokKind Slot = new RewriteTokKind("#", (TokKindEnum)2012);

		public static readonly TokKind SlotSplice = new RewriteTokKind("##", (TokKindEnum)2013);

		public static readonly TokKind AssignImmed = TokKind.Add;

		public static readonly TokKind AssignDelayed = new RewriteTokKind(":=", (TokKindEnum)2014);

		public static readonly TokKind RuleImmed = TokKind.SubGrt;

		public static readonly TokKind RuleDelayed = new RewriteTokKind(":>", (TokKindEnum)2015);

		public static readonly TokKind RuleApplyOnce = new RewriteTokKind("/.", (TokKindEnum)2016);

		public static readonly TokKind RuleApplyMany = new RewriteTokKind("/..", (TokKindEnum)2017);

		public static readonly TokKind Conditional = new RewriteTokKind("/;", (TokKindEnum)2018);

		public static readonly TokKind InlineFunction = TokKind.And;

		public static readonly TokKind Unset = new RewriteTokKind("=.", (TokKindEnum)2019);

		public static readonly TokKind SquareColonOpen = new RewriteTokKind("[:", (TokKindEnum)2020);

		public static readonly TokKind SquareColonClose = new RewriteTokKind(":]", (TokKindEnum)2021);

		public static readonly TokKind EquEquEqu = new RewriteTokKind("===", (TokKindEnum)2022);

		public static readonly TokKind NotEquEqu = new RewriteTokKind("!==", (TokKindEnum)2023);

		public static readonly TokKind CaretOr = new RewriteTokKind("^|", (TokKindEnum)2024);

		public static readonly TokKind MinusColon = new RewriteTokKind("-:", (TokKindEnum)2025);

		public static readonly TokKind ExcelInputBinding = new RewriteTokKind("<==", (TokKindEnum)2026);

		public static readonly TokKind ExcelOutputBinding = new RewriteTokKind("->@", (TokKindEnum)2027);

		protected RewriteTokKind(string str, TokKindEnum tke)
			: base(str, tke)
		{
		}
	}
}
