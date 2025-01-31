using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A random parameter with exponential distribution.
	/// </summary>
	/// <remarks>
	/// An exponential distribution is a continuous random distribution defined by a rate
	/// parameter.  The distribution describes a Poisson process where independent events 
	/// occur continuously at that rate. 
	/// </remarks>
	public sealed class ExponentialDistributionParameter : RandomParameter
	{
		/// <summary>Creates a non-indexed exponential distribution parameter.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		public ExponentialDistributionParameter(string name)
			: base(name)
		{
		}

		/// <summary>Creates a non-indexed exponential distribution parameter with the specified rate.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="rate">The rate argument for the distribution.</param>
		public ExponentialDistributionParameter(string name, double rate)
			: base(name)
		{
			base.ValueTable = ValueTable<DistributedValue>.Create(_domain);
			ExponentialUnivariateValue value;
			try
			{
				value = new ExponentialUnivariateValue(rate);
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw new InvalidModelDataException(Resources.InvalidArgumentsForExponentialDistribution, innerException);
			}
			base.ValueTable.Add(value);
		}

		/// <summary>Creates an indexed exponential distribution parameter.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="indexSets">The index sets to use. Omit to create a scalar parameter.</param>
		public ExponentialDistributionParameter(string name, params Set[] indexSets)
			: base(name, indexSets)
		{
		}

		private ExponentialDistributionParameter(string baseName, ExponentialDistributionParameter source)
			: base(baseName, source)
		{
		}

		internal override Term Clone(string baseName)
		{
			return new ExponentialDistributionParameter(baseName, this);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(es) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="rateField">The name of the property of each input object which contains the rate for the distribution.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string rateField)
		{
			SetBinding(binding, rateField, new string[0]);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(es) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		///
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="rateField">The name of the property of each input object which contains the rate for the distribution.</param>
		/// <param name="indexFields">The names of the properties of each input object which contain the indexes of the data elements, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string rateField, params string[] indexFields)
		{
			if (rateField == null)
			{
				throw new ArgumentNullException("rateField");
			}
			Func<T, object>[] indexFieldGetters = GetIndexFieldGetters(binding, indexFields);
			Func<T, double> rateFieldGetter = DataBindingSupport.MakeAccessorDelegate<T, double>(rateField, Domain.Real);
			_addValueTableElement = delegate(object obj, ValueTable<DistributedValue> table)
			{
				object[] indexes = RandomParameter.GetIndexes(obj, indexFieldGetters);
				double rate = rateFieldGetter((T)obj);
				ExponentialUnivariateValue value;
				try
				{
					value = new ExponentialUnivariateValue(rate);
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
