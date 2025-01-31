using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Class for modeling non-linear model for CQN solver
	/// </summary>
	public abstract class CompactQuasiNewtonModel : UnconstrainedNonlinearModel
	{
		/// <summary>
		/// Creates CompactQuasiNewtonModel.
		/// </summary>
		/// <param name="comparer">Key comparer</param>
		protected CompactQuasiNewtonModel(IEqualityComparer<object> comparer)
			: base(comparer)
		{
		}

		/// <summary>Set a property for the specified index.
		/// </summary>
		/// <param name="propertyName">The name of the property to set, see SolverProperties.</param>
		/// <param name="vid">The variable index.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="T:System.ArgumentNullException">The property name is null.</exception>
		/// <exception cref="T:System.ArgumentException">The variable index is invalid.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidSolverPropertyException">The property is not supported. The Reason property indicates why the property is not supported.</exception>
		/// <remarks> This method is typically called by Solver Foundation Services in response to event handler code.
		/// </remarks>
		public virtual void SetProperty(string propertyName, int vid, object value)
		{
			if (propertyName == null)
			{
				throw new ArgumentNullException("propertyName");
			}
			Rational value2 = Rational.ConvertToRational(value);
			if (propertyName == SolverProperties.VariableStartValue)
			{
				if (IsRow(vid))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { vid }));
				}
				SetValue(vid, value2);
				return;
			}
			throw new InvalidSolverPropertyException(string.Format(CultureInfo.InvariantCulture, Resources.PropertyNameIsNotSupported0, new object[1] { propertyName }), InvalidSolverPropertyReason.InvalidPropertyName);
		}

		/// <summary>Get a property for the specified index.
		/// </summary>
		/// <param name="propertyName">The name of the property to get, see SolverProperties.</param>
		/// <param name="vid">The variable index.</param>
		/// <returns>The value.</returns>
		/// <exception cref="T:System.ArgumentNullException">The property name is null.</exception>
		/// <exception cref="T:System.ArgumentException">The variable index is invalid.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidSolverPropertyException">The property is not supported. The Reason property indicates why the property is not supported.</exception>
		/// <remarks> This method is typically called by Solver Foundation Services in response to event handler code.
		/// </remarks>
		public virtual object GetProperty(string propertyName, int vid)
		{
			if (propertyName == null)
			{
				throw new ArgumentNullException("propertyName");
			}
			if (propertyName == SolverProperties.VariableStartValue)
			{
				if (IsRow(vid))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { vid }));
				}
				return GetValue(vid);
			}
			throw new InvalidSolverPropertyException(string.Format(CultureInfo.InvariantCulture, Resources.PropertyNameIsNotSupported0, new object[1] { propertyName }), InvalidSolverPropertyReason.InvalidPropertyName);
		}
	}
}
