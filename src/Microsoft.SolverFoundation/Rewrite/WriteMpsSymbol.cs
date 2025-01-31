using System.Globalization;
using System.IO;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary> Writes the content of a model as MPS. 
	/// Arguments: A model or a simplex solver, an optional boolean indicating whether to use fixed format (true) or not (false).
	/// The format defaults to fixed.
	/// </summary>
	internal sealed class WriteMpsSymbol : BaseSolveSymbol
	{
		public WriteMpsSymbol(SolveRewriteSystem rs)
			: base(rs, "WriteMps")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			LinearModel linearModel = null;
			bool val = true;
			if ((ib.Count == 1 || (ib.Count == 2 && ib[1].GetValue(out val))) && ib[0] is LinearSolverWrapper linearSolverWrapper)
			{
				linearModel = linearSolverWrapper.Model;
			}
			if (linearModel == null)
			{
				base.Rewrite.Log(Resources.WriteMpsNeedsAModelOrSimplexSolverOptionallyFollowedByABooleanIndicatingFixedTrueOrFreeFalse);
				return null;
			}
			using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
			{
				MpsWriter mpsWriter = new MpsWriter(linearModel);
				mpsWriter.WriteMps(stringWriter, val);
				return new StringConstant(base.Rewrite, stringWriter.ToString());
			}
		}
	}
}
