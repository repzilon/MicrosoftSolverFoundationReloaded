using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Represents an object which recieves a value as a result of solving.
	/// This interface is internal because it exposes NL Symbols.
	/// </summary>
	internal interface IVariable
	{
		/// <summary>
		/// Set the value (or one of the values) of the object. Called after solving.
		/// </summary>
		/// <param name="value">The value to set.</param>
		/// <param name="indexes"></param>
		void SetValue(Rational value, object[] indexes);
	}
}
