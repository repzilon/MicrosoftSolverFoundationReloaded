using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A normal (Gaussian) distribution parameter.
	/// </summary>
	/// <remarks>
	/// A normal distribution is a bell-shaped continuous random distribution.  The distribution
	/// is defined by its mean value and its standard distribution.  The distribution is symmetric
	/// about the mean value.  Normal distributions are sometimes defined using the variance rather
	/// than standard deviation - the variance is the square of the standard deviation.
	/// </remarks>
	public sealed class NormalDistributionParameter : RandomParameter
	{
		/// <summary>Creates a non-indexed normal distribution parameter.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		public NormalDistributionParameter(string name)
			: base(name)
		{
		}

		/// <summary>Creates a non-indexed normal distribution parameter with the specified values.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="mean">The mean value.</param>
		/// <param name="standardDeviation">The standard deviation.</param>
		public NormalDistributionParameter(string name, double mean, double standardDeviation)
			: base(name)
		{
			base.ValueTable = ValueTable<DistributedValue>.Create(_domain);
			NormalUnivariateValue value;
			try
			{
				value = new NormalUnivariateValue(mean, standardDeviation);
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw new InvalidModelDataException(Resources.InvalidArgumentsForNormalDistribution, innerException);
			}
			base.ValueTable.Add(value);
		}

		/// <summary>Creates an indexed normal distribution parameter.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="indexSets">The index sets to use. Omit to create a scalar parameter.</param>
		public NormalDistributionParameter(string name, params Set[] indexSets)
			: base(name, indexSets)
		{
		}

		private NormalDistributionParameter(string baseName, NormalDistributionParameter source)
			: base(baseName, source)
		{
		}

		internal override Term Clone(string baseName)
		{
			return new NormalDistributionParameter(baseName, this);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(es) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="meanField">The name of the property of each input object which contains the mean for the distribution.</param>
		/// <param name="stdField">The name of the property of each input object which contains the value of the standard deviation for the distribution.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string meanField, string stdField)
		{
			SetBinding(binding, meanField, stdField, new string[0]);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(es) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		///
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="meanField">The name of the property of each input object which contains the mean for the distribution.</param>
		/// <param name="stdField">The name of the property of each input object which contains the value of the standard deviation for the distribution.</param>
		/// <param name="indexFields">The names of the properties of each input object which contain the indexes of the data elements, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string meanField, string stdField, params string[] indexFields)
		{
			if (meanField == null)
			{
				throw new ArgumentNullException("meanField");
			}
			if (stdField == null)
			{
				throw new ArgumentNullException("stdField");
			}
			Func<T, object>[] indexFieldGetters = GetIndexFieldGetters(binding, indexFields);
			Func<T, double> meanFieldGetter = DataBindingSupport.MakeAccessorDelegate<T, double>(meanField, Domain.Real);
			Func<T, double> stdFieldGetter = DataBindingSupport.MakeAccessorDelegate<T, double>(stdField, Domain.Real);
			_addValueTableElement = delegate(object obj, ValueTable<DistributedValue> table)
			{
				object[] indexes = RandomParameter.GetIndexes(obj, indexFieldGetters);
				double mean = meanFieldGetter((T)obj);
				double standardDeviation = stdFieldGetter((T)obj);
				NormalUnivariateValue value;
				try
				{
					value = new NormalUnivariateValue(mean, standardDeviation);
				}
				catch (ArgumentOutOfRangeException innerException)
				{
					throw new InvalidModelDataException(Resources.InvalidArgumentsForNormalDistribution, innerException);
				}
				AddToTable(table, value, indexes);
			};
		}
	}
}
