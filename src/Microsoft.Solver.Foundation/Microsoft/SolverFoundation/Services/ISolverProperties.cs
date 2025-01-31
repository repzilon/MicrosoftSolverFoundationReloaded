namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Interface for solver which support property bag like getter and setter.
	/// This property bag may be used in events handler.
	/// </summary>
	public interface ISolverProperties
	{
		/// <summary>Set a solver-related property.
		/// </summary>
		/// <param name="propertyName">The name of the property to get.</param>
		/// <param name="vid">An index for the item of interest.</param>
		/// <param name="value">The property value.</param>
		void SetProperty(string propertyName, int vid, object value);

		/// <summary>Get the value of a property.
		/// </summary>
		/// <param name="propertyName">The name of the property to get.</param>
		/// <param name="vid">An index for the item of interest.</param>
		/// <returns>The property value as a System.Object.</returns>
		object GetProperty(string propertyName, int vid);
	}
}
