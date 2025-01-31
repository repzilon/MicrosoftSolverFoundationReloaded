using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A log normal distribution parameter.
	/// </summary>
	/// <remarks>
	/// A log normal random parameter is a continuous random parameter where the log of the
	/// parameter is normally distributed. The distribution is defined by the natural log 
	/// of its mean value and standard distribution.  
	/// </remarks>
	public sealed class LogNormalDistributionParameter : RandomParameter
	{
		/// <summary>Creates a non-indexed log normal distribution parameter.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		public LogNormalDistributionParameter(string name)
			: base(name)
		{
		}

		/// <summary>Creates a non-indexed log normal distribution parameter with the specified values.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="meanLog">Mean of the logarithm of the distribution</param>
		/// <param name="standardDeviationLog">Standard deviation of the logarithm of the distribution</param>
		public LogNormalDistributionParameter(string name, double meanLog, double standardDeviationLog)
			: base(name)
		{
			base.ValueTable = ValueTable<DistributedValue>.Create(_domain);
			LogNormalUnivariateValue value;
			try
			{
				value = new LogNormalUnivariateValue(meanLog, standardDeviationLog);
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw new InvalidModelDataException(Resources.InvalidArgumentsForLognormalDistribution, innerException);
			}
			base.ValueTable.Add(value);
		}

		/// <summary>Creates an indexed log normal distribution parameter.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="indexSets">The index sets to use. Omit to create a scalar parameter.</param>
		public LogNormalDistributionParameter(string name, params Set[] indexSets)
			: base(name, indexSets)
		{
		}

		private LogNormalDistributionParameter(string baseName, LogNormalDistributionParameter source)
			: base(baseName, source)
		{
		}

		internal override Term Clone(string baseName)
		{
			return new LogNormalDistributionParameter(baseName, this);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(es) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="meanLogField">The name of the property of each input object which contains the mean of the logarithm for the distribution.</param>
		/// <param name="stdLogField">The name of the property of each input object which contains the value of the standard deviation of the logarithm for the distribution.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string meanLogField, string stdLogField)
		{
			SetBinding(binding, meanLogField, stdLogField, new string[0]);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(es) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		///
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="meanLogField">The name of the property of each input object which contains the mean of the logarithm for the distribution.</param>
		/// <param name="stdLogField">The name of the property of each input object which contains the value of the standard deviation of the logarithm for the distribution.</param>
		/// <param name="indexFields">The names of the properties of each input object which contain the indexes of the data elements, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string meanLogField, string stdLogField, params string[] indexFields)
		{
			if (meanLogField == null)
			{
				throw new ArgumentNullException("meanLogField");
			}
			if (stdLogField == null)
			{
				throw new ArgumentNullException("stdLogField");
			}
			Func<T, object>[] indexFieldGetters = GetIndexFieldGetters(binding, indexFields);
			Func<T, double> meanLogFieldGetter = DataBindingSupport.MakeAccessorDelegate<T, double>(meanLogField, Domain.Real);
			Func<T, double> stdLogFieldGetter = DataBindingSupport.MakeAccessorDelegate<T, double>(stdLogField, Domain.Real);
			_addValueTableElement = delegate(object obj, ValueTable<DistributedValue> table)
			{
				object[] indexes = RandomParameter.GetIndexes(obj, indexFieldGetters);
				double meanLog = meanLogFieldGetter((T)obj);
				double standardDeviationLog = stdLogFieldGetter((T)obj);
				LogNormalUnivariateValue value;
				try
				{
					value = new LogNormalUnivariateValue(meanLog, standardDeviationLog);
				}
				catch (ArgumentOutOfRangeException innerException)
				{
					throw new InvalidModelDataException(Resources.InvalidArgumentsForLognormalDistribution, innerException);
				}
				AddToTable(table, value, indexes);
			};
		}
	}
}
