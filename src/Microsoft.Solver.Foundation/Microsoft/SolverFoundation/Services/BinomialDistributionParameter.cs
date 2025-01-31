using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A binomial distribution parameter.
	/// </summary>
	/// <remarks>
	/// A binomial distribution is a discrete random distribution defined by a success probability
	/// and number of trials.  The distribution represents the number of successes in a sequence of trials with the
	/// given success rate. 
	/// </remarks>
	public sealed class BinomialDistributionParameter : RandomParameter
	{
		/// <summary>Creates a non-indexed binomial distribution parameter.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <remarks>If the case of : numberOfTrials * min(successProbability, 1 - successProbability) &gt;= 10 
		/// the distribution cannot be sampled with Latin hypercube method
		/// </remarks>
		public BinomialDistributionParameter(string name)
			: base(name)
		{
		}

		/// <summary>Creates a non-indexed binomial distribution parameter from the specified values.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="numberOfTrials">Number of trials of the distribution</param>
		/// <param name="successProbability">Success probability of the distribution</param>
		/// <remarks>If the case of : numberOfTrials * min(successProbability, 1 - successProbability) &gt;= 10 
		/// the distribution cannot be sampled with Latin hypercube method
		/// </remarks>
		public BinomialDistributionParameter(string name, int numberOfTrials, double successProbability)
			: base(name)
		{
			base.ValueTable = ValueTable<DistributedValue>.Create(_domain);
			BinomialUnivariateValue value;
			try
			{
				value = new BinomialUnivariateValue(numberOfTrials, successProbability);
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw new InvalidModelDataException(Resources.InvalidArgumentsForBinomialDistribution, innerException);
			}
			base.ValueTable.Add(value);
		}

		/// <summary>Creates an indexed binomial distribution parameter.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="indexSets">The index sets to use. Omit to create a scalar parameter.</param>
		/// <remarks>If the case of : numberOfTrials * min(successProbability, 1 - successProbability) &gt;= 10 
		/// the distribution cannot be sampled with Latin hypercube method
		/// </remarks>
		public BinomialDistributionParameter(string name, params Set[] indexSets)
			: base(name, indexSets)
		{
		}

		private BinomialDistributionParameter(string baseName, BinomialDistributionParameter source)
			: base(baseName, source)
		{
		}

		internal override Term Clone(string baseName)
		{
			return new BinomialDistributionParameter(baseName, this);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(es) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="numberOfTrialsField">The name of the property of each input object which contains the number of trials for the distribution.</param>
		/// <param name="successProbabilityField">The name of the property of each input object which contains the success probability for the distribution.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string numberOfTrialsField, string successProbabilityField)
		{
			SetBinding(binding, numberOfTrialsField, successProbabilityField, new string[0]);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(es) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		///
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="numberOfTrialsField">The name of the property of each input object which contains the number of trials for the distribution.</param>
		/// <param name="successProbabilityField">The name of the property of each input object which contains the success probability for the distribution.</param>
		/// <param name="indexFields">The names of the properties of each input object which contain the indexes of the data elements, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string numberOfTrialsField, string successProbabilityField, params string[] indexFields)
		{
			if (numberOfTrialsField == null)
			{
				throw new ArgumentNullException("numberOfTrialsField");
			}
			if (successProbabilityField == null)
			{
				throw new ArgumentNullException("successProbabilityField");
			}
			Func<T, object>[] indexFieldGetters = GetIndexFieldGetters(binding, indexFields);
			Func<T, double> successProbabilityFieldGetter = DataBindingSupport.MakeAccessorDelegate<T, double>(successProbabilityField, Domain.Real);
			Func<T, int> numberOfTrialsFieldGetter = DataBindingSupport.MakeAccessorDelegate<T, int>(numberOfTrialsField, Domain.Integer);
			_addValueTableElement = delegate(object obj, ValueTable<DistributedValue> table)
			{
				object[] indexes = RandomParameter.GetIndexes(obj, indexFieldGetters);
				double successProbability = successProbabilityFieldGetter((T)obj);
				int numberOfTrials = numberOfTrialsFieldGetter((T)obj);
				BinomialUnivariateValue value;
				try
				{
					value = new BinomialUnivariateValue(numberOfTrials, successProbability);
				}
				catch (ArgumentOutOfRangeException innerException)
				{
					throw new InvalidModelDataException(Resources.InvalidArgumentsForBinomialDistribution, innerException);
				}
				AddToTable(table, value, indexes);
			};
		}
	}
}
