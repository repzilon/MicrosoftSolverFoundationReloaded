using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A random parameter with geometric distribution.
	/// </summary>
	/// <remarks>
	/// A geometric distribution is a discrete random distribution defined by a success probability
	/// parameter.  The distribution describes a process where independent Bernoulli trials 
	/// are taken with the given success probability. The probability distribution is of the number 
	/// Y = X - 1 of Bernoulli trial failures before the first success, supported on the set { 0, 1, 2, 3, ... }.
	/// (Note that this is different from an alternate convention which defines the distribution as the first successful trial 
	/// supported on the set {1, 2, 3, ... }.)
	/// </remarks>
	public sealed class GeometricDistributionParameter : RandomParameter
	{
		/// <summary>Creates a non-indexed geometric distribution parameter.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		public GeometricDistributionParameter(string name)
			: base(name)
		{
		}

		/// <summary>Creates a non-indexed geometric distribution parameter with the specified success probability.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="successProbability">Success probability of the distribution</param>
		public GeometricDistributionParameter(string name, double successProbability)
			: base(name)
		{
			base.ValueTable = ValueTable<DistributedValue>.Create(_domain);
			GeometricUnivariateValue value;
			try
			{
				value = new GeometricUnivariateValue(successProbability);
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw new InvalidModelDataException(Resources.InvalidArgumentsForGeometricDistribution, innerException);
			}
			base.ValueTable.Add(value);
		}

		/// <summary>Creates an indexed geometric distribution parameter.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="indexSets">The index sets to use. Omit to create a scalar parameter.</param>
		public GeometricDistributionParameter(string name, params Set[] indexSets)
			: base(name, indexSets)
		{
		}

		private GeometricDistributionParameter(string baseName, GeometricDistributionParameter source)
			: base(baseName, source)
		{
		}

		internal override Term Clone(string baseName)
		{
			return new GeometricDistributionParameter(baseName, this);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(es) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="successProbabilityField">The name of the property of each input object which contains the success probability for the distribution.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string successProbabilityField)
		{
			SetBinding(binding, successProbabilityField, new string[0]);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(es) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		///
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="successProbabilityField">The name of the property of each input object which contains the success probability for the distribution.</param>
		/// <param name="indexFields">The names of the properties of each input object which contain the indexes of the data elements, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string successProbabilityField, params string[] indexFields)
		{
			if (successProbabilityField == null)
			{
				throw new ArgumentNullException("successProbabilityField");
			}
			Func<T, object>[] indexFieldGetters = GetIndexFieldGetters(binding, indexFields);
			Func<T, double> successProbabilityFieldGetter = DataBindingSupport.MakeAccessorDelegate<T, double>(successProbabilityField, Domain.Real);
			_addValueTableElement = delegate(object obj, ValueTable<DistributedValue> table)
			{
				object[] indexes = RandomParameter.GetIndexes(obj, indexFieldGetters);
				double successProbability = successProbabilityFieldGetter((T)obj);
				GeometricUnivariateValue value;
				try
				{
					value = new GeometricUnivariateValue(successProbability);
				}
				catch (ArgumentOutOfRangeException innerException)
				{
					throw new InvalidModelDataException(Resources.InvalidArgumentsForExponentialDistribution, innerException);
				}
				AddToTable(table, value, indexes);
			};
		}
	}
}
