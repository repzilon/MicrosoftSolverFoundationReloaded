using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> A Domain represents the set of possible values for a Decision or Parameter.
	/// </summary>
	/// <remarks>
	/// Domains determine the range of acceptable values for a Decision or Parameter. A Domain
	/// instance is created by calling a static method on the Domain class. Frequently
	/// used domains include Domain.Real and Domain.Integer. The domains of decisions and 
	/// parameters are considered in determining which solvers may be used to solve a model.
	/// </remarks>
	public abstract class Domain
	{
		private static readonly Domain _real = new NumericRangeDomain(double.NegativeInfinity, double.PositiveInfinity, intRestricted: false);

		private static readonly Domain _realNonnegative = new NumericRangeDomain(0, double.PositiveInfinity, intRestricted: false);

		private static readonly Domain _integer = new NumericRangeDomain(Rational.NegativeInfinity, Rational.PositiveInfinity, intRestricted: true);

		private static readonly Domain _integerNonnegative = new NumericRangeDomain(0, Rational.PositiveInfinity, intRestricted: true);

		private static readonly Domain _boolean = new BooleanDomain();

		private static readonly Domain _realZeroOne = new NumericRangeDomain(0, 1, intRestricted: false);

		private static readonly Domain _integerZeroOne = new NumericRangeDomain(0, 1, intRestricted: true);

		private static readonly Domain _any = new AnyDomain();

		private static readonly Domain _probability = new ProbabilityDomain();

		private static readonly Domain _distributedValue = new DistributedDomain();

		private readonly Rational _minValue;

		private readonly Rational _maxValue;

		private readonly bool _isBoolean;

		private readonly bool _intRestricted;

		private readonly string[] _enumeratedNames;

		private string _name;

		private readonly Rational[] _validValues;

		private HashSet<double> _validValueCache;

		/// <summary>
		/// A domain representing any real value
		/// </summary>
		public static Domain Real => _real;

		/// <summary>
		/// A domain representing any DistributedValue
		/// </summary>
		internal static Domain DistributedValue => _distributedValue;

		/// <summary>
		/// A domain representing any positive real value or zero
		/// </summary>
		public static Domain RealNonnegative => _realNonnegative;

		/// <summary>
		/// A domain representing any integer value
		/// </summary>
		public static Domain Integer => _integer;

		/// <summary>
		/// A domain representing any positive integer or zero
		/// </summary>
		public static Domain IntegerNonnegative => _integerNonnegative;

		/// <summary>
		/// A domain representing a true or false value
		/// </summary>
		public static Domain Boolean => _boolean;

		/// <summary>
		/// A domain representing any number or string
		/// </summary>
		public static Domain Any => _any;

		/// <summary>
		/// A domain representing probability
		/// </summary>
		public static Domain Probability => _probability;

		/// <summary>
		/// The minimum possible value of an element of the domain. Double.NegativeInfinity for no limit.
		/// </summary>
		internal Rational MinValue => _minValue;

		/// <summary>
		/// The maximum possible value of an element of the domain. Double.PositiveInfinity for no limit.
		/// </summary>
		internal Rational MaxValue => _maxValue;

		internal bool IsBoolean => _isBoolean;

		/// <summary>
		/// If true, elements of the domain must be exact integers (that is, (int)x == x).
		/// </summary>
		internal bool IntRestricted => _intRestricted;

		/// <summary>
		/// If non-null, an array of strings which are used in place of numbers when inputting/outputting values of this domain.
		/// If set then IntRestricted must be true, and MinValue and MaxValue must be legal indexes into the array.
		/// </summary>
		internal string[] EnumeratedNames => _enumeratedNames;

		/// <summary>
		/// If non-null, the name of this domain.  Otherwise a default name based on the domain's other properties will be used.
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (value.Length == 0)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				_name = value;
			}
		}

		internal Rational[] ValidValues => _validValues;

		internal IEnumerable<Rational> Values
		{
			get
			{
				if (ValidValues != null)
				{
					try
					{
						Rational[] validValues = ValidValues;
						for (int i = 0; i < validValues.Length; i++)
						{
							yield return validValues[i];
						}
						yield break;
					}
					finally
					{
					}
				}
				if (IntRestricted && MinValue.IsFinite && MaxValue.IsFinite)
				{
					for (Rational r = MinValue; r <= MaxValue; r += (Rational)1)
					{
						yield return r;
					}
					yield break;
				}
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// The type which a value that is an element of this domain can take.
		/// </summary>
		internal abstract TermValueClass ValueClass { get; }

		/// <summary>
		/// Returns true if this value is numeric or boolean.
		/// </summary>
		internal bool IsNumeric => ValueClass == TermValueClass.Numeric;

		/// <summary>
		/// A domain representing a real value in a restricted range
		/// </summary>
		/// <param name="min">The minimum value in the range.</param>
		/// <param name="max">The maximum value in the range.</param>
		/// <returns>A domain representing the range.</returns>
		public static Domain RealRange(Rational min, Rational max)
		{
			if (min.IsZero && max.IsPositiveInfinity)
			{
				return RealNonnegative;
			}
			if (min.IsZero && max.IsOne)
			{
				return _realZeroOne;
			}
			if (min.IsNegativeInfinity && max.IsPositiveInfinity)
			{
				return Real;
			}
			return new NumericRangeDomain(min, max, intRestricted: false);
		}

		/// <summary>
		/// A domain representing an integer value in a restricted range.
		/// </summary>
		/// <param name="min">The minimum value in the range.</param>
		/// <param name="max">The maximum value in the range.</param>
		/// <returns>A domain representing the range.</returns>
		public static Domain IntegerRange(Rational min, Rational max)
		{
			if (min.IsZero && max.IsPositiveInfinity)
			{
				return IntegerNonnegative;
			}
			if (min.IsZero && max.IsOne)
			{
				return _integerZeroOne;
			}
			if (min.IsNegativeInfinity && max.IsPositiveInfinity)
			{
				return Integer;
			}
			return new NumericRangeDomain(min, max, intRestricted: true);
		}

		/// <summary>A domain representing values from a discrete set.
		/// </summary>
		/// <param name="values">The values in the set.</param>
		/// <returns>A domain representing the discrete set.</returns>
		public static Domain Set(params int[] values)
		{
			if (values == null)
			{
				throw new ArgumentNullException("values");
			}
			if (values.Length == 0)
			{
				throw new ArgumentException(Resources.ValueSetCannotBeEmpty, "values");
			}
			Rational[] array = new Rational[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				ref Rational reference = ref array[i];
				reference = values[i];
			}
			return SetDomain(array, intRestricted: true);
		}

		/// <summary>A domain representing values from a discrete set.
		/// </summary>
		/// <param name="values">The values in the set.</param>
		/// <returns>A domain representing the discrete set.</returns>
		public static Domain Set(params Rational[] values)
		{
			if (values == null)
			{
				throw new ArgumentNullException("values");
			}
			if (values.Length == 0)
			{
				throw new ArgumentException(Resources.ValueSetCannotBeEmpty, "values");
			}
			Rational[] array = new Rational[values.Length];
			bool intRestricted = true;
			for (int i = 0; i < values.Length; i++)
			{
				ref Rational reference = ref array[i];
				reference = values[i];
				if (!values[i].IsInteger())
				{
					intRestricted = false;
				}
			}
			return SetDomain(array, intRestricted);
		}

		private static Domain SetDomain(Rational[] rationalValues, bool intRestricted)
		{
			Array.Sort(rationalValues);
			Rational minValue = 0;
			Rational maxValue = 0;
			if (rationalValues.Length > 0)
			{
				minValue = rationalValues[0];
				maxValue = rationalValues[rationalValues.Length - 1];
			}
			return new NumericSetDomain(minValue, maxValue, intRestricted, rationalValues);
		}

		/// <summary>
		/// A domain representing a choice between a group of strings
		/// </summary>
		public static Domain Enum(params string[] names)
		{
			VerifyEnumDomainValues(names);
			string[] array = new string[names.Length];
			Array.Copy(names, array, names.Length);
			return new EnumDomain(array);
		}

		private static void VerifyEnumDomainValues(string[] names)
		{
			if (names == null)
			{
				throw new ArgumentNullException("names", Resources.EnumDomainMustHaveAtLeastOneElement);
			}
			if (names.Length == 0)
			{
				throw new ArgumentException(Resources.EnumDomainMustHaveAtLeastOneElement, "names");
			}
			HashSet<string> hashSet = new HashSet<string>();
			foreach (string text in names)
			{
				if (string.IsNullOrEmpty(text))
				{
					throw new ArgumentException(Resources.EnumDomainCannotContainNullOrEmptyStringValue, "names");
				}
				if (hashSet.Contains(text))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.EnumDomainCannotContainDuplicatedValues0, new object[1] { text }), "names");
				}
				hashSet.Add(text);
			}
		}

		/// <summary>
		/// Looks up the index of a string in an enum domain.
		///
		/// This is a helper function which is used internally. It is not intended to be called by user code.
		/// </summary>
		/// <param name="value">The string to look up.</param>
		/// <returns>The index.</returns>
		internal virtual Rational GetOrdinal(string value)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Construct a new domain. See comments on properties of this class for restrictions on parameters.
		/// </summary>
		internal Domain(Rational minValue, Rational maxValue, bool intRestricted, string[] enumeratedNames, Rational[] validValues, bool isBoolean)
		{
			if (validValues != null)
			{
				_ = validValues.Length;
				_ = 0;
			}
			_minValue = minValue;
			_maxValue = maxValue;
			_intRestricted = intRestricted;
			_enumeratedNames = enumeratedNames;
			_validValues = validValues;
			_isBoolean = isBoolean;
		}

		/// <summary>
		/// Try to convert a boxed builtin value to a double using a lossless conversion.
		/// </summary>
		/// <param name="value">A boxed value.</param>
		/// <param name="dblValue">The converted double.</param>
		/// <returns>True if the conversion succeeded. False if the input was not a boxed value, or could not be converted losslessly.</returns>
		internal static bool TryCastToDouble(object value, out double dblValue)
		{
			if (value is double)
			{
				dblValue = (double)value;
				return true;
			}
			if (value is sbyte)
			{
				dblValue = (sbyte)value;
				return true;
			}
			if (value is byte)
			{
				dblValue = (int)(byte)value;
				return true;
			}
			if (value is short)
			{
				dblValue = (short)value;
				return true;
			}
			if (value is ushort)
			{
				dblValue = (int)(ushort)value;
				return true;
			}
			if (value is int)
			{
				dblValue = (int)value;
				return true;
			}
			if (value is uint)
			{
				dblValue = (uint)value;
				return true;
			}
			if (value is long)
			{
				dblValue = (long)value;
				return true;
			}
			if (value is ulong)
			{
				dblValue = (ulong)value;
				return true;
			}
			if (value is float)
			{
				dblValue = (float)value;
				return true;
			}
			if (value is Rational)
			{
				dblValue = (double)(Rational)value;
				return true;
			}
			dblValue = 0.0;
			return false;
		}

		/// <summary>
		/// Tests whether a boxed value is a member of this domain. For enumerated domains, tests whether
		/// the value is a member of the underlying (numeric) domain, rather than a member of the possible strings.
		///
		/// REVIEW shahark: The data check is done seperatly for the Random Parameters. The value here is the just the IDistributedValue
		/// itself and not each scenario (for example when dealing with scenarios)
		/// There should be domain type that fits here, then the check can be done
		/// </summary>
		/// <param name="value">The value to test.</param>
		/// <returns>True if the value is a member of the domain.</returns>
		internal bool IsValidValue(object value)
		{
			try
			{
				if (value is string)
				{
					return IsValidStringValue();
				}
				if (TryCastToDouble(value, out var dblValue))
				{
					return IsValidDoubleValue(dblValue);
				}
				if (value is Microsoft.SolverFoundation.Services.DistributedValue)
				{
					return IsValidDistributedValue();
				}
				return false;
			}
			catch (InvalidCastException)
			{
				return false;
			}
		}

		/// <summary>
		/// Always returns true, the data check is done in the IDistributedValue impl
		/// </summary>
		internal virtual bool IsValidDistributedValue()
		{
			return false;
		}

		/// <summary>
		/// Tests whether a string is a member of this domain. For enumerated domains, returns false, because a
		/// string is not a member of the underlying (numeric) domain.
		/// </summary>
		internal virtual bool IsValidStringValue()
		{
			return false;
		}

		/// <summary>
		/// Tests whether a number is a member of this domain. For enumerated domains, tests whether
		/// the value is a member of the underlying (numeric) domain.
		/// </summary>
		/// <param name="dblValue"></param>
		/// <returns></returns>
		private bool IsValidDoubleValue(double dblValue)
		{
			try
			{
				if (dblValue < MinValue || dblValue > MaxValue)
				{
					return false;
				}
				if (IntRestricted && (double)(int)dblValue != dblValue)
				{
					return false;
				}
				if (ValidValues != null)
				{
					if (_validValueCache == null)
					{
						_validValueCache = new HashSet<double>();
						Rational[] validValues = ValidValues;
						foreach (Rational rational in validValues)
						{
							_validValueCache.Add((double)rational);
						}
					}
					return _validValueCache.Contains(dblValue);
				}
				return true;
			}
			catch (InvalidCastException)
			{
				return false;
			}
		}

		internal abstract CspDomain MakeCspDomain(ConstraintSystem solver);

		internal static int FiniteIntFromRational(Rational rat)
		{
			if (rat > ConstraintSystem.MaxFinite)
			{
				return ConstraintSystem.MaxFinite;
			}
			if (rat < ConstraintSystem.MinFinite)
			{
				return ConstraintSystem.MinFinite;
			}
			return (int)rat;
		}

		internal abstract Expression MakeOmlDomain(OmlWriter writer, SolveRewriteSystem rewrite);

		internal abstract string FormatValue(IFormatProvider format, double value);
	}
}
