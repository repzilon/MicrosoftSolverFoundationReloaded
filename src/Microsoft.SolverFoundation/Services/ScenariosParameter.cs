using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A parameter representing a fixed, finite number of discrete scenarios.
	/// </summary>
	/// <remarks>
	/// A ScenariosParameter is associated with an underlying set of Scenario objects.  
	/// Each Scenario defines a possible value for the random parameter along with the 
	/// probability of the scenario actually occurring.  The sum of probabilities over all
	/// scenarios equals 1.0.
	/// </remarks>
	public sealed class ScenariosParameter : RandomParameter
	{
		/// <summary>Creates a non-indexed random parameter representing a finite set of scenarios.
		/// </summary>
		/// <param name="name">The name of the parameter, which must be unique.</param>
		public ScenariosParameter(string name)
			: base(name)
		{
		}

		/// <summary>Creates a non-indexed random parameter representing the given set of scenarios.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="scenarios">Array of scenarios</param>
		public ScenariosParameter(string name, IEnumerable<Scenario> scenarios)
			: base(name)
		{
			if (scenarios == null)
			{
				throw new ArgumentNullException("scenarios");
			}
			if (scenarios.Count() == 0)
			{
				throw new ArgumentException(Resources.ThereShouldBeAtLeastOneScenario, "scenarios");
			}
			FillExplicitScenarios(scenarios);
		}

		/// <summary>Creates an indexed random parameter representing a finite set of scenarios.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="indexSets">The index sets to use. Omit to create a scalar parameter.</param>
		public ScenariosParameter(string name, params Set[] indexSets)
			: base(name, indexSets)
		{
		}

		private ScenariosParameter(string baseName, ScenariosParameter source)
			: base(baseName, source)
		{
			base.Binding = source.Binding;
		}

		/// <summary>Fill in the scenarios for this parameter.
		/// </summary>
		/// <param name="scenarios">The scenarios.</param>
		private void FillExplicitScenarios(IEnumerable<Scenario> scenarios)
		{
			base.ValueTable = ValueTable<DistributedValue>.Create(_domain);
			DiscreteScenariosValue discreteScenariosValue = new DiscreteScenariosValue();
			try
			{
				foreach (Scenario scenario in scenarios)
				{
					discreteScenariosValue.AddScenario(scenario);
				}
			}
			catch (ArgumentException innerException)
			{
				throw new InvalidModelDataException(Resources.InvalidArgumentsForScenariosDistribution, innerException);
			}
			base.ValueTable.Add(discreteScenariosValue);
			VerifyProbabilitySumToOne();
		}

		internal override Term Clone(string baseName)
		{
			return new ScenariosParameter(baseName, this);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(es) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="probabilityField">The name of the property of each input object which contains the probability for the value.</param>
		/// <param name="valueField">The name of the property of each input object which contains the value of the data element.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string probabilityField, string valueField)
		{
			SetBinding(binding, probabilityField, valueField, new string[0]);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(es) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		///
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="probabilityField">The name of the property of each input object which contains the probability for the value.</param>
		/// <param name="valueField">The name of the property of each input object which contains the value of the data element.</param>
		/// <param name="indexFields">The names of the properties of each input object which contain the indexes of the data elements, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string probabilityField, string valueField, params string[] indexFields)
		{
			if (probabilityField == null)
			{
				throw new ArgumentNullException("probabilityField");
			}
			if (valueField == null)
			{
				throw new ArgumentNullException("valueField");
			}
			Func<T, object>[] indexFieldGetters = GetIndexFieldGetters(binding, indexFields);
			Func<T, Rational> probabilityFieldGetter = DataBindingSupport.MakeAccessorDelegate<T, Rational>(probabilityField, Domain.Real);
			Func<T, Rational> valueFieldGetter = DataBindingSupport.MakeAccessorDelegate<T, Rational>(valueField, _domain);
			_addValueTableElement = delegate(object obj, ValueTable<DistributedValue> table)
			{
				object[] indexes = RandomParameter.GetIndexes(obj, indexFieldGetters);
				Rational probability = probabilityFieldGetter((T)obj);
				Rational value = valueFieldGetter((T)obj);
				DiscreteScenariosValue discreteScenariosValue;
				if (!table.TryGetValue(out var value2, indexes))
				{
					discreteScenariosValue = new DiscreteScenariosValue();
					table.Add(discreteScenariosValue, indexes);
				}
				else
				{
					discreteScenariosValue = value2 as DiscreteScenariosValue;
				}
				try
				{
					discreteScenariosValue.AddScenario(probability, value);
				}
				catch (ArgumentException innerException)
				{
					throw new InvalidModelDataException(Resources.InvalidArgumentsForScenariosDistribution, innerException);
				}
				catch (ModelException innerException2)
				{
					throw new InvalidModelDataException(Resources.InvalidArgumentsForScenariosDistribution, innerException2);
				}
			};
		}

		/// <summary>
		/// Actually binds the data to the parameter, using the delegates constructed in advance
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">Thrown if duplicate or out-of-range data is detected in the model.</exception>
		protected override void DataBind(SolverContext context)
		{
			base.DataBind(context);
			try
			{
				VerifyProbabilitySumToOne();
			}
			catch (ModelException innerException)
			{
				throw new InvalidModelDataException(Resources.InvalidArgumentsForScenariosDistribution, innerException);
			}
		}

		/// <summary>
		/// Makes sure that probabilites sums up to 1.
		/// Can called after data binding (if the parameter is bound), or after
		/// ctr when the parameter has the scenarios is ctr.
		/// </summary>
		private void VerifyProbabilitySumToOne()
		{
			foreach (DiscreteScenariosValue value in base.ValueTable.Values)
			{
				value.ValidateScenarios();
			}
		}
	}
}
