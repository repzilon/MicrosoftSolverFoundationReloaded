namespace Microsoft.SolverFoundation.Solvers
{
	internal struct BasicVariableValueTuple
	{
		public int Var { get; set; }

		public int Row { get; set; }

		public double Val { get; set; }

		public BasicVariableValueTuple(int var, int row, double val)
		{
			this = default(BasicVariableValueTuple);
			Var = var;
			Row = row;
			Val = val;
		}
	}
}
