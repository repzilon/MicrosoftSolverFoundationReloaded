namespace Microsoft.SolverFoundation.Solvers
{
	internal class SOS2Status : ISOSStatus
	{
		public int Count { get; private set; }

		public int Var1 { get; private set; }

		public int Var2 { get; private set; }

		public void Append(int var)
		{
			if (Count == 0)
			{
				Var1 = var;
			}
			else
			{
				Var2 = var;
			}
			Count++;
		}

		public void Remove()
		{
			if (Count == 2)
			{
				Var2 = -1;
			}
			else
			{
				Var1 = -1;
			}
			Count--;
		}

		public void Clear()
		{
			Count = 0;
			Var1 = -1;
			Var2 = -1;
		}
	}
}
