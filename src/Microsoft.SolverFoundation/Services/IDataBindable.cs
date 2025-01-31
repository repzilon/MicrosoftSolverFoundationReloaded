namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// An object which needs to be handled during data binding, either because it contains data which is read during hydration
	/// or because it will recieve data during solve.
	/// </summary>
	internal interface IDataBindable
	{
		/// <summary>
		/// Reads data from a data source and binds it to the object.
		/// </summary>
		void DataBind(SolverContext context);

		/// <summary>
		/// Pushes values changes back to the data source.
		/// </summary>
		void PropagateValues(SolverContext context);
	}
}
