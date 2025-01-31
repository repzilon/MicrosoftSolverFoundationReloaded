using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Encapsulates the validity of a model and any errors found.
	/// </summary>
	public sealed class ModelReport
	{
		internal List<string> _errors;

		internal bool _isValid;

		/// <summary>
		/// True if the model is valid (has no errors).
		/// </summary>
		public bool IsValid => _isValid;

		/// <summary>
		/// A list of errors found in the model.
		/// </summary>
		public IEnumerable<string> Errors => _errors;

		internal ModelReport(bool isValid)
		{
			_isValid = isValid;
			_errors = new List<string>();
		}

		internal void AddError(string error)
		{
			_isValid = false;
			_errors.Add(error);
		}
	}
}
