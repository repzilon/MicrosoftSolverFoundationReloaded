namespace Microsoft.SolverFoundation.Solvers
{
	internal class SOS1Status : ISOSStatus
	{
		public bool IsFull => Var >= 0;

		public int Var { get; private set; }

		public SOS1Status()
		{
			Clear();
		}

		public void Append(int var)
		{
			Var = var;
		}

		public void Remove()
		{
			Var = -1;
		}

		public void Clear()
		{
			Var = -1;
		}
	}
}
