namespace Microsoft.SolverFoundation.Common
{
	internal class ThreadState
	{
		public Parallelizer izer;

		public int threadIndex;

		public ThreadState(Parallelizer p, int index)
		{
			izer = p;
			threadIndex = index;
		}
	}
}
