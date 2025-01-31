using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	internal class SmpsLexer : MpsLexer
	{
		public SmpsLexer(NormStr.Pool pool)
			: base(pool)
		{
			AddSingleKeyWord(MpsTokKind.Time);
			AddSingleKeyWord(MpsTokKind.Periods);
			AddSingleKeyWord(MpsTokKind.Implicit);
			AddSingleKeyWord(MpsTokKind.Explicit);
			AddSingleKeyWord(MpsTokKind.Lp);
			AddSingleKeyWord(MpsTokKind.Stoch);
			AddSingleKeyWord(MpsTokKind.Indep);
			AddSingleKeyWord(MpsTokKind.Replace);
			AddSingleKeyWord(MpsTokKind.SmpsAdd);
			AddSingleKeyWord(MpsTokKind.Multiply);
			AddSingleKeyWord(MpsTokKind.Discrete);
			AddSingleKeyWord(MpsTokKind.Normal);
			AddSingleKeyWord(MpsTokKind.Uniform);
			AddSingleKeyWord(MpsTokKind.Beta);
			AddSingleKeyWord(MpsTokKind.Gamma);
			AddSingleKeyWord(MpsTokKind.Lognorm);
		}
	}
}
