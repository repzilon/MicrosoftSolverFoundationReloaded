using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// This class contains static functions to support data binding.
	/// </summary>
	internal static class DataBindingSupport
	{
		internal static Func<T, TResult> MakeAccessorDelegate<T, TResult>(string valueField, Domain domain)
		{
			return MakeAccessorDelegate<T, TResult>(valueField, domain, null);
		}

		/// <summary>
		/// Create a delegate which extracts a given field from an object, including casting it to the result type.
		/// </summary>
		/// <typeparam name="T">The type of the object.</typeparam>
		/// <typeparam name="TResult">The type of the result.</typeparam>
		/// <param name="valueField">The name of the field to extract.</param>
		/// <param name="domain">The domain of the field to extract.</param>
		/// <param name="intermediateType">Intermediate result type (optional).</param>
		/// <returns>A delegate.</returns>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">The property or field isn't found.</exception>
		internal static Func<T, TResult> MakeAccessorDelegate<T, TResult>(string valueField, Domain domain, Type intermediateType)
		{
			Type typeFromHandle = typeof(T);
			PropertyInfo defaultMemberProperty = GetDefaultMemberProperty(typeFromHandle);
			PropertyInfo propertyInfo = null;
			FieldInfo fieldInfo = null;
			if (defaultMemberProperty == null)
			{
				try
				{
					propertyInfo = typeFromHandle.GetProperty(valueField);
					fieldInfo = typeFromHandle.GetField(valueField);
				}
				catch (AmbiguousMatchException innerException)
				{
					throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.ThePropertyOrField0WasNotFound, new object[1] { valueField }), innerException);
				}
			}
			if (propertyInfo == null && fieldInfo == null && defaultMemberProperty == null)
			{
				throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.ThePropertyOrField0WasNotFoundOnType1, new object[2] { valueField, typeFromHandle }));
			}
			ParameterExpression parameterExpression = Expression.Parameter(typeFromHandle, "element");
			Expression expression = ((!(defaultMemberProperty != null)) ? ((Expression)Expression.PropertyOrField(parameterExpression, valueField)) : ((Expression)Expression.Call(parameterExpression, defaultMemberProperty.GetGetMethod(), Expression.Constant(valueField))));
			if (intermediateType != null && intermediateType != typeof(TResult))
			{
				expression = Expression.Call(typeof(Convert), "ChangeType", null, expression, Expression.Constant(intermediateType, typeof(Type)));
				expression = Expression.Convert(expression, intermediateType);
			}
			if (domain.ValueClass == TermValueClass.Enumerated)
			{
				expression = Expression.Call(Expression.Constant(domain), typeof(Domain).GetMethod("GetOrdinal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), expression);
			}
			if (expression.Type == typeof(bool))
			{
				expression = Expression.Condition(expression, Expression.Constant(1), Expression.Constant(0));
			}
			expression = Expression.Convert(expression, typeof(TResult));
			LambdaExpression lambdaExpression = Expression.Lambda(typeof(Func<T, TResult>), expression, parameterExpression);
			return lambdaExpression.Compile() as Func<T, TResult>;
		}

		internal static Type GetIntermediateValueFieldType<T>(IEnumerable<T> binding, string valueField, Domain targetDomain)
		{
			Type result = null;
			if (typeof(T) == typeof(BindingUtilities.ArrayWrapper))
			{
				if (!targetDomain.IsNumeric)
				{
					return typeof(object);
				}
				if (!targetDomain.IntRestricted)
				{
					return typeof(double);
				}
				return typeof(int);
			}
			if (binding.Any())
			{
				T val = binding.First();
				if (val is DataRow dataRow && dataRow.Table != null)
				{
					DataColumn dataColumn = dataRow.Table.Columns[valueField];
					if (dataColumn != null)
					{
						result = dataColumn.DataType;
					}
				}
			}
			return result;
		}

		internal static PropertyInfo GetDefaultMemberProperty(Type objType)
		{
			PropertyInfo result = null;
			object[] customAttributes = objType.GetCustomAttributes(typeof(DefaultMemberAttribute), inherit: true);
			if (customAttributes.Length > 0)
			{
				result = objType.GetProperty(((DefaultMemberAttribute)customAttributes[0]).MemberName, new Type[1] { typeof(string) });
			}
			return result;
		}

		internal static Func<T, TResult> MakeConversionDelegate<T, TResult>(Domain domain)
		{
			Type typeFromHandle = typeof(T);
			ParameterExpression parameterExpression = Expression.Parameter(typeFromHandle, "value");
			Expression expression = parameterExpression;
			if (domain.IsBoolean)
			{
				expression = Expression.GreaterThan(expression, Expression.Constant(0.5));
			}
			UnaryExpression body = Expression.Convert(expression, typeof(TResult));
			LambdaExpression lambdaExpression = Expression.Lambda(typeof(Func<T, TResult>), body, parameterExpression);
			return lambdaExpression.Compile() as Func<T, TResult>;
		}

		internal static void ValidateIndexArguments(string name, Set[] indexSets, Term[] indexes)
		{
			if (indexes.Length != indexSets.Length)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.ExpectsExactly1IndexesBut2WereProvided, new object[3] { name, indexSets.Length, indexes.Length }));
			}
			for (int i = 0; i < indexes.Length; i++)
			{
				bool flag = false;
				TermValueClass valueClass = indexes[i].ValueClass;
				switch (indexSets[i].ItemValueClass)
				{
				case TermValueClass.Numeric:
					flag = valueClass == TermValueClass.Numeric;
					break;
				case TermValueClass.Any:
					flag = valueClass != TermValueClass.Enumerated;
					break;
				case TermValueClass.Enumerated:
				{
					int num;
					switch (valueClass)
					{
					case TermValueClass.Enumerated:
						num = ((indexSets[i]._domain == indexes[i].EnumeratedDomain) ? 1 : 0);
						break;
					default:
						num = 0;
						break;
					case TermValueClass.String:
						num = 1;
						break;
					}
					flag = (byte)num != 0;
					break;
				}
				case TermValueClass.String:
					flag = false;
					break;
				}
				if (!flag)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.Argument0ToIndexOperationIsOfAnInvalidType, new object[1] { i }));
				}
			}
		}
	}
}
