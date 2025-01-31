using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Helper methods for Parameter and Decision data binding.
	/// </summary>
	public static class BindingUtilities
	{
		/// <summary>An object array wrapper to support data binding.
		/// </summary>
		internal class ArrayWrapper
		{
			private object[] _data;

			public object this[string index] => _data[Convert.ToInt32(index, CultureInfo.InvariantCulture)];

			public ArrayWrapper(object[] data)
			{
				_data = data;
			}
		}

		/// <summary>Bind a parameter to a scalar value.
		/// </summary>
		/// <param name="parameter">A Parameter (which should have no index sets).</param>
		/// <param name="data">The scalar value.</param>
		public static void SetBinding<T>(this Parameter parameter, T data)
		{
			if (parameter.IndexSets.Count != 0)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.OnlyValidWhenCalledOnParametersWith0Indexes, new object[1] { 0 }));
			}
			parameter.SetBinding(ToIEnumerable(data), "Value");
		}

		/// <summary>Bind an indexed parameter to an IEnumerable of values.
		/// </summary>
		/// <param name="parameter">A Parameter (which should have one index set).</param>
		/// <param name="data">An IEnumerable containing the data.</param>
		public static void SetBinding<T>(this Parameter parameter, IEnumerable<T> data)
		{
			if (parameter.IndexSets.Count != 1)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.OnlyValidWhenCalledOnParametersWith0Indexes, new object[1] { 1 }));
			}
			parameter.SetBinding(ToIEnumerable(data), "Value", "Key");
		}

		/// <summary>Bind an indexed parameter to a table.
		/// </summary>
		/// <param name="parameter">A Parameter (which should have two index sets).</param>
		/// <param name="data">An IEnumerable containing the data. Each entry contains values for the first index.</param>
		public static void SetBinding<T>(this Parameter parameter, IEnumerable<IEnumerable<T>> data)
		{
			if (parameter.IndexSets.Count != 2)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.OnlyValidWhenCalledOnParametersWith0Indexes, new object[1] { 2 }));
			}
			parameter.SetBinding(ToIEnumerable(data), "Item3", "Item1", "Item2");
		}

		/// <summary>Bind an indexed parameter to an IEnumerable.
		/// </summary>
		/// <remarks>The first slot in each element of values is assumed to store the value. The remaining slots
		/// store the indexes.
		/// </remarks>
		/// <param name="parameter">A Parameter.</param>
		/// <param name="data">An IEnumerable containing the indexes and values. The first entry of each item in the IEnumerable contains the value; the remaining entries are indexes.</param>
		public static void SetBinding(this Parameter parameter, IEnumerable<object[]> data)
		{
			int num = 0;
			if (data.Any())
			{
				num = data.First().Length;
				if (parameter.IndexSets.Count != num - 1)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.OnlyValidWhenCalledOnParametersWith0Indexes, new object[1] { parameter.IndexSets.Count }));
				}
				string[] array = new string[num - 1];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = (i + 1).ToString(CultureInfo.InvariantCulture);
				}
				string valueField = 0.ToString(CultureInfo.InvariantCulture);
				parameter.SetBinding(data.Select((object[] r) => new ArrayWrapper(r)), valueField, array);
				return;
			}
			throw new ArgumentException(Resources.ListMustHaveAtLeastOneElement, "data");
		}

		/// <summary>Gets the values for a decision with one index Set.
		/// </summary>
		/// <returns>An IEnumerable containing the values.</returns>
		/// <remarks>The values are returned ordered by index.</remarks>
		public static IEnumerable<double> GetValuesByIndex(this Decision decision)
		{
			if (decision.IndexSets.Count != 1)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.OnlyValidWhenCalledOnDecisionsWith0Indexes, new object[1] { 1 }));
			}
			return from r in decision.GetValues()
				select Convert.ToDouble(r[0], CultureInfo.InvariantCulture);
		}

		/// <summary>Gets the values for a decision with two index Sets.
		/// </summary>
		/// <returns>An IEnumerable containing the values, grouped by the first index.</returns>
		/// <remarks>The values are returned ordered by index.</remarks>
		public static IEnumerable<IEnumerable<double>> GetValuesByFirstIndex(this Decision decision)
		{
			if (decision.IndexSets.Count != 2)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.OnlyValidWhenCalledOnDecisionsWith0Indexes, new object[1] { 2 }));
			}
			return from r in decision.GetValues()
				group r by r[1] into g
				select from r in g
					select Convert.ToDouble(r[0], CultureInfo.InvariantCulture);
		}

		private static IEnumerable<KeyValuePair<int, T>> ToIEnumerable<T>(T value)
		{
			return new KeyValuePair<int, T>[1]
			{
				new KeyValuePair<int, T>(0, value)
			};
		}

		private static IEnumerable<KeyValuePair<int, T>> ToIEnumerable<T>(IEnumerable<T> vector)
		{
			return vector.Select((T d, int index) => new KeyValuePair<int, T>(index, d));
		}

		private static IEnumerable<Tuple<int, int, T>> ToIEnumerable<T>(IEnumerable<IEnumerable<T>> matrix)
		{
			IEnumerable<IEnumerable<Tuple<int, int, T>>> source = matrix.Select((IEnumerable<T> row, int i) => row.Select((T cell, int j) => new Tuple<int, int, T>(i, j, cell)));
			return from cell in source.SelectMany((IEnumerable<Tuple<int, int, T>> c) => c)
				select (cell);
		}
	}
}
