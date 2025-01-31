namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// This task is used exclusively for writing MSP/QPS files
	/// and not for solving.
	/// </summary>
	internal class MpsWriterTask : LinearTask
	{
		public MpsWriterTask(SolverContext context, Model model, ILinearSolver ls, ISolverParameters solverParams, Directive directive)
			: base(context, model, ls, solverParams, directive)
		{
			_useNamesAsKeys = true;
		}
	}
}
