using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A continuous uniformly distributed random parameter.
	/// </summary>
	/// <remarks>
	/// A uniform distribution is a continuous random distribution defined by an upper and lower
	/// bound.  The values within the (closed) interval occur with equal probability.  The upper
	/// and lower bounds must be finite.
	/// </remarks>
	public class UniformDistributionParameter : RandomParameter
	{
		/// <summary>Creates a new continuous uniform distribution.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		public UniformDistributionParameter(string name)
			: base(name)
		{
		}

		/// <summary>Creates a new continuous uniform distribution.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="lowerBound">The lower bound (inclusive).</param>
		/// <param name="upperBound">The upper bound (inclusive).</param>
		public UniformDistributionParameter(string name, double lowerBound, double upperBound)
			: base(name)
		{
			base.ValueTable = ValueTable<DistributedValue>.Create(_domain);
			ContinuousUniformUnivariateValue value;
			try
			{
				value = new ContinuousUniformUnivariateValue(lowerBound, upperBound);
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw new InvalidModelDataException(Resources.InvalidArgumentsForNormalDistribution, innerException);
			}
			base.ValueTable.Add(value);
		}

		/// <summary>Creates a new continuous uniform distribution.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="indexSets">The index sets to use. Omit to create a scalar parameter.</param>
		public UniformDistributionParameter(string name, params Set[] indexSets)
			: base(name, indexSets)
		{
		}

		/// <summary>Creates a new continuous uniform distribution.
		/// </summary>
		protected UniformDistributionParameter(string baseName, UniformDistributionParameter source)
			: base(baseName, source)
		{
		}

		internal override Term Clone(string baseName)
		{
			return new UniformDistributionParameter(baseName, this);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(es) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="lowerBoundField">The name of the property of each input object which contains the lower bound for the distribution.</param>
		/// <param name="upperBoundField">The name of the property of each input object which contains the value of the upper bound for the distribution.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string lowerBoundField, string upperBoundField)
		{
			SetBinding(binding, lowerBoundField, upperBoundField, new string[0]);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(es) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		///
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="lowerBoundField">The name of the property of each input object which contains the lower bound for the distribution.</param>
		/// <param name="upperBoundField">The name of the property of each input object which contains the value of the upper bound for the distribution.</param>
		/// <param name="indexFields">The names of the properties of each input object which contain the indexes of the data elements, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		public virtual void SetBinding<T>(IEnumerable<T> binding, string lowerBoundField, string upperBoundField, params string[] indexFields)
		{
			SetBindingCore(binding, lowerBoundField, upperBoundField, indexFields);
		}

		/// <summary>Bind parameter to data.
		/// </summary>
		private void SetBindingCore<T>(IEnumerable<T> binding, string lowerBoundField, string upperBoundField, params string[] indexFields)
		{
			if (lowerBoundField == null)
			{
				throw new ArgumentNullException("lowerBoundField");
			}
			if (upperBoundField == null)
			{
				throw new ArgumentNullException("upperBoundField");
			}
			Func<T, object>[] indexFieldGetters = GetIndexFieldGetters(binding, indexFields);
			Func<T, double> lowerBoundFieldGetter = DataBindingSupport.MakeAccessorDelegate<T, double>(lowerBoundField, Domain.Real);
			Func<T, double> upperBoundFieldGetter = DataBindingSupport.MakeAccessorDelegate<T, double>(upperBoundField, Domain.Real);
			_addValueTableElement = delegate(object obj, ValueTable<DistributedValue> table)
			{
				object[] indexes = RandomParameter.GetIndexes(obj, indexFieldGetters);
				double dLowerBound = lowerBoundFieldGetter((T)obj);
				double dUpperBound = upperBoundFieldGetter((T)obj);
				AddToTable(table, GetTableValue(dLowerBound, dUpperBound), indexes);
			};
		}

		internal virtual DistributedValue GetTableValue(double dLowerBound, double dUpperBound)
		{
			try
			{
				return new ContinuousUniformUnivariateValue(dLowerBound, dUpperBound);
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw new InvalidModelDataException(Resources.InvalidArgumentsForUniformDistribution, innerException);
			}
		}
	}
}
