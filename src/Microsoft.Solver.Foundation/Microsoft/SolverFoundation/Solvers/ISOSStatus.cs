namespace Microsoft.SolverFoundation.Solvers
{
	internal interface ISOSStatus
	{
		void Append(int var);

		void Remove();

		void Clear();
	}
}
