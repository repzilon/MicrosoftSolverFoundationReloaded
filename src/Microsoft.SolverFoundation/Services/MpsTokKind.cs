using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	internal class MpsTokKind : TokKind
	{
		internal const TokKindEnum BetaId = (TokKindEnum)4028;

		internal const TokKindEnum BinaryBoundId = (TokKindEnum)4010;

		internal const TokKindEnum BoundsId = (TokKindEnum)3005;

		internal const TokKindEnum ColumnsId = (TokKindEnum)3002;

		internal const TokKindEnum CSectionId = (TokKindEnum)3011;

		internal const TokKindEnum DiscreteId = (TokKindEnum)4024;

		internal const TokKindEnum EndDataId = (TokKindEnum)3007;

		internal const TokKindEnum EqualId = (TokKindEnum)4000;

		internal const TokKindEnum ExplicitId = (TokKindEnum)4022;

		internal const TokKindEnum FixedVariableId = (TokKindEnum)4006;

		internal const TokKindEnum FreeVariableId = (TokKindEnum)4007;

		internal const TokKindEnum GammaId = (TokKindEnum)4027;

		internal const TokKindEnum GreaterEqualId = (TokKindEnum)4002;

		internal const TokKindEnum ImplicitId = (TokKindEnum)4021;

		internal const TokKindEnum IndepId = (TokKindEnum)3015;

		internal const TokKindEnum IntegerLowerBoundId = (TokKindEnum)4011;

		internal const TokKindEnum IntegerUpperBoundId = (TokKindEnum)4012;

		internal const TokKindEnum KeyIdLim = (TokKindEnum)4033;

		internal const TokKindEnum KeyIdMin = (TokKindEnum)4000;

		internal const TokKindEnum LessEqualId = (TokKindEnum)4001;

		internal const TokKindEnum LognormId = (TokKindEnum)4029;

		internal const TokKindEnum LowerBoundId = (TokKindEnum)4004;

		internal const TokKindEnum LpId = (TokKindEnum)4023;

		internal const TokKindEnum MultiplyId = (TokKindEnum)4031;

		internal const TokKindEnum NameId = (TokKindEnum)3000;

		internal const TokKindEnum NoLowerBoundId = (TokKindEnum)4008;

		internal const TokKindEnum NormalId = (TokKindEnum)4025;

		internal const TokKindEnum NoUpperBoundId = (TokKindEnum)4009;

		internal const TokKindEnum ObjectiveId = (TokKindEnum)4003;

		internal const TokKindEnum ObjSenseID = (TokKindEnum)3008;

		internal const TokKindEnum ObjSenseMaxID = (TokKindEnum)4014;

		internal const TokKindEnum ObjSenseMaximizeID = (TokKindEnum)4013;

		internal const TokKindEnum ObjSenseMinID = (TokKindEnum)4016;

		internal const TokKindEnum ObjSenseMinimizeID = (TokKindEnum)4015;

		internal const TokKindEnum PeriodsId = (TokKindEnum)3013;

		internal const TokKindEnum QSectionId = (TokKindEnum)3010;

		internal const TokKindEnum QuadId = (TokKindEnum)4019;

		internal const TokKindEnum QUADOBJID = (TokKindEnum)3006;

		internal const TokKindEnum RangesId = (TokKindEnum)3004;

		internal const TokKindEnum ReplaceId = (TokKindEnum)4032;

		internal const TokKindEnum RhsId = (TokKindEnum)3003;

		internal const TokKindEnum RowsId = (TokKindEnum)3001;

		internal const TokKindEnum RQuadId = (TokKindEnum)4020;

		internal const TokKindEnum SectionIdLim = (TokKindEnum)3017;

		internal const TokKindEnum SectionIdMin = (TokKindEnum)3000;

		internal const TokKindEnum SmpsAddId = (TokKindEnum)4030;

		internal const TokKindEnum SETSId = (TokKindEnum)3009;

		internal const TokKindEnum SOSId = (TokKindEnum)3016;

		internal const TokKindEnum SOSS1ID = (TokKindEnum)4017;

		internal const TokKindEnum SOSS2ID = (TokKindEnum)4018;

		internal const TokKindEnum StochId = (TokKindEnum)3014;

		internal const TokKindEnum TimeId = (TokKindEnum)3012;

		internal const TokKindEnum UniformId = (TokKindEnum)4026;

		internal const TokKindEnum UpperBoundId = (TokKindEnum)4005;

		internal static readonly TokKind Beta = new MpsTokKind("BETA", (TokKindEnum)4028);

		internal static readonly TokKind BinaryBound = new MpsTokKind("BV", (TokKindEnum)4010);

		internal static readonly TokKind Bounds = new MpsTokKind("BOUNDS", (TokKindEnum)3005);

		internal static readonly TokKind Columns = new MpsTokKind("COLUMNS", (TokKindEnum)3002);

		internal static readonly TokKind CSection = new MpsTokKind("CSECTION", (TokKindEnum)3011);

		internal static readonly TokKind Discrete = new MpsTokKind("DISCRETE", (TokKindEnum)4024);

		internal static readonly TokKind EndData = new MpsTokKind("ENDATA", (TokKindEnum)3007);

		internal static readonly TokKind Equal = new MpsTokKind("E", (TokKindEnum)4000);

		internal static readonly TokKind Explicit = new MpsTokKind("EXPLICIT", (TokKindEnum)4022);

		internal static readonly TokKind FixedVariable = new MpsTokKind("FX", (TokKindEnum)4006);

		internal static readonly TokKind FreeVariable = new MpsTokKind("FR", (TokKindEnum)4007);

		internal static readonly TokKind Gamma = new MpsTokKind("GAMMA", (TokKindEnum)4027);

		internal static readonly TokKind GreaterEqual = new MpsTokKind("G", (TokKindEnum)4002);

		internal static readonly TokKind Implicit = new MpsTokKind("IMPLICIT", (TokKindEnum)4021);

		internal static readonly TokKind Indep = new MpsTokKind("INDEP", (TokKindEnum)3015);

		internal static readonly TokKind IntegerLowerBound = new MpsTokKind("LI", (TokKindEnum)4011);

		internal static readonly TokKind IntegerUpperBound = new MpsTokKind("UI", (TokKindEnum)4012);

		internal static readonly TokKind LessEqual = new MpsTokKind("L", (TokKindEnum)4001);

		internal static readonly TokKind Lognorm = new MpsTokKind("LOGNORM", (TokKindEnum)4029);

		internal static readonly TokKind LowerBound = new MpsTokKind("LO", (TokKindEnum)4004);

		internal static readonly TokKind Lp = new MpsTokKind("LP", (TokKindEnum)4023);

		internal static readonly TokKind Multiply = new MpsTokKind("MULTIPLY", (TokKindEnum)4031);

		internal static readonly TokKind Name = new MpsTokKind("NAME", (TokKindEnum)3000);

		internal static readonly TokKind NoLowerBound = new MpsTokKind("MI", (TokKindEnum)4008);

		internal static readonly TokKind Normal = new MpsTokKind("NORMAL", (TokKindEnum)4025);

		internal static readonly TokKind NoUpperBound = new MpsTokKind("PL", (TokKindEnum)4009);

		internal static readonly TokKind Objective = new MpsTokKind("N", (TokKindEnum)4003);

		internal static readonly TokKind ObjSense = new MpsTokKind("OBJSENSE", (TokKindEnum)3008);

		internal static readonly TokKind ObjSenseMax = new MpsTokKind("MAX", (TokKindEnum)4014);

		internal static readonly TokKind ObjSenseMaximize = new MpsTokKind("MAXIMIZE", (TokKindEnum)4013);

		internal static readonly TokKind ObjSenseMin = new MpsTokKind("MIN", (TokKindEnum)4016);

		internal static readonly TokKind ObjSenseMinimize = new MpsTokKind("MINIMIZE", (TokKindEnum)4015);

		internal static readonly TokKind Periods = new MpsTokKind("PERIODS", (TokKindEnum)3013);

		internal static readonly TokKind QSection = new MpsTokKind("QSECTION", (TokKindEnum)3010);

		internal static readonly TokKind Quad = new MpsTokKind("QUAD", (TokKindEnum)4019);

		internal static readonly TokKind Quadobj = new MpsTokKind("QUADOBJ", (TokKindEnum)3006);

		internal static readonly TokKind Ranges = new MpsTokKind("RANGES", (TokKindEnum)3004);

		internal static readonly TokKind Replace = new MpsTokKind("REPLACE", (TokKindEnum)4032);

		internal static readonly TokKind Rhs = new MpsTokKind("RHS", (TokKindEnum)3003);

		internal static readonly TokKind Rows = new MpsTokKind("ROWS", (TokKindEnum)3001);

		internal static readonly TokKind RQuad = new MpsTokKind("RQUAD", (TokKindEnum)4020);

		internal static readonly TokKind SmpsAdd = new MpsTokKind("ADD", (TokKindEnum)4030);

		internal static readonly TokKind SETS = new MpsTokKind("SETS", (TokKindEnum)3009);

		internal static readonly TokKind SOS = new MpsTokKind("SOS", (TokKindEnum)3016);

		internal static readonly TokKind SOSS1 = new MpsTokKind("S1", (TokKindEnum)4017);

		internal static readonly TokKind SOSS2 = new MpsTokKind("S2", (TokKindEnum)4018);

		internal static readonly TokKind Stoch = new MpsTokKind("STOCH", (TokKindEnum)3014);

		internal static readonly TokKind Time = new MpsTokKind("TIME", (TokKindEnum)3012);

		internal static readonly TokKind Uniform = new MpsTokKind("UNIFORM", (TokKindEnum)4026);

		internal static readonly TokKind UpperBound = new MpsTokKind("UP", (TokKindEnum)4005);

		internal MpsTokKind(string str, TokKindEnum tke)
			: base(str, tke)
		{
		}
	}
}
