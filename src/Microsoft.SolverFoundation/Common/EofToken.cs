namespace Microsoft.SolverFoundation.Common
{
	internal class EofToken : Token
	{
		public EofToken()
			: base(TokKind.Eof)
		{
		}
	}
}
