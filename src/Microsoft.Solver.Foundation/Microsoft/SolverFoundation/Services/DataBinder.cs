namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// This class is responsible of binding the data to a model 
	/// </summary>
	internal class DataBinder
	{
		private readonly Model _model;

		private readonly SolverContext _context;

		public DataBinder(SolverContext context, Model model)
		{
			_model = model;
			_context = context;
		}

		public void BindData(bool boundIfAlreadyBound)
		{
			if (boundIfAlreadyBound || !_model._dataBound)
			{
				_model.BindData(_context);
			}
		}
	}
}
