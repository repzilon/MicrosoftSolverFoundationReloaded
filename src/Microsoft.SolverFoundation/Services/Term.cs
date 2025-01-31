using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> A term used in a model decision, formula, goal or constraint.
	/// </summary>
	/// <remarks>
	/// Terms may represent data such as constants, decisions, or parameters. Terms may also
	/// represent operations that combine other Term objects, for example arithmetic or logical
	/// operations. The Model class has static methods that produce terms for many standard
	/// mathematical operations.
	/// </remarks>
	public abstract class Term : IFormattable
	{
		internal class EvaluationContext
		{
			private readonly Dictionary<Term, int> _depthAdded = new Dictionary<Term, int>();

			private readonly Dictionary<Term, object> _fixedValues = new Dictionary<Term, object>();

			public Constraint Constraint { get; set; }

			public Goal Goal { get; set; }

			public int Depth => _fixedValues.Count;

			public IEnumerable<object> Keys => from kvp in _fixedValues
				orderby _depthAdded[kvp.Key]
				select kvp.Value;

			public object GetValue(Term term)
			{
				return _fixedValues[term];
			}

			public void SetValue(Term term, object value)
			{
				if (!_depthAdded.ContainsKey(term))
				{
					_depthAdded[term] = Depth;
				}
				_fixedValues[term] = value;
			}

			public void ClearValue(Term term)
			{
				if (_fixedValues.ContainsKey(term))
				{
					_depthAdded.Remove(term);
					_fixedValues.Remove(term);
				}
			}

			public EvaluationContext Clone()
			{
				EvaluationContext evaluationContext = new EvaluationContext();
				foreach (Term key in _fixedValues.Keys)
				{
					evaluationContext._fixedValues[key] = _fixedValues[key];
				}
				evaluationContext.Constraint = Constraint;
				evaluationContext.Goal = Goal;
				return evaluationContext;
			}
		}

		internal Model _owningModel;

		internal TermStructure _structure;

		private static BoolConstantTerm _constantFalse = new BoolConstantTerm(value: false);

		private static BoolConstantTerm _constantTrue = new BoolConstantTerm(value: true);

		private static ConstantTerm _constant0 = new ConstantTerm(0);

		private static ConstantTerm _constant1 = new ConstantTerm(1);

		/// <summary>
		/// Whether the term can be used in any models. Example of such terms are: constant terms, operator terms whose inputs are all constants.
		/// </summary>
		internal abstract bool IsModelIndependentTerm { get; }

		/// <summary>
		/// The subclass of term (for fast switching)
		/// </summary>
		internal abstract TermType TermType { get; }

		/// <summary>
		/// The type of this term (boolean, numeric, enumerated, etc.)
		/// </summary>
		internal abstract TermValueClass ValueClass { get; }

		/// <summary>
		/// Some information about the structure of the term (used for model analysis)
		/// </summary>
		internal TermStructure Structure => _structure;

		/// <summary>
		/// True if this is numeric or boolean.
		/// </summary>
		internal bool IsNumeric
		{
			get
			{
				if (ValueClass != 0)
				{
					return ValueClass == TermValueClass.Distribution;
				}
				return true;
			}
		}

		/// <summary>
		/// If this is an enumerated term, this contains its domain (for getting the enumerated strings).
		/// If this isn't an enumerated term, it contains either the domain or null.
		/// </summary>
		internal virtual Domain EnumeratedDomain => null;

		/// <summary>
		/// Internal constructor to ensure external users cannot subclass from Term or any derived classes of Term (such as Decision)
		/// </summary>
		internal Term()
		{
		}

		internal bool HasStructure(TermStructure test)
		{
			return (_structure & test) != 0;
		}

		/// <summary>
		/// Represent multiplication
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator *(Term left, Term right)
		{
			Model.VerifySaneInputs(left, right);
			return Model.Product(left, right);
		}

		/// <summary>
		/// Represent addition
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator +(Term left, Term right)
		{
			Model.VerifySaneInputs(left, right);
			return Model.Sum(left, right);
		}

		/// <summary>
		/// Represent subtraction
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator -(Term left, Term right)
		{
			Model.VerifySaneInputs(left, right);
			return Model.Difference(left, right);
		}

		/// <summary>
		/// Represent negation
		/// </summary>
		/// <param name="term"></param>
		/// <returns></returns>
		public static Term operator -(Term term)
		{
			Model.VerifySaneInputs(term);
			return Model.Negate(term);
		}

		/// <summary>
		/// Represent division
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator /(Term left, Term right)
		{
			Model.VerifySaneInputs(left, right);
			return Model.Quotient(left, right);
		}

		/// <summary>
		/// Represent less-than
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator <(Term left, Term right)
		{
			return CreateBinaryComparison(Operator.Less, left, right);
		}

		/// <summary>
		/// Represent greater-than
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator >(Term left, Term right)
		{
			return CreateBinaryComparison(Operator.Greater, left, right);
		}

		/// <summary>
		/// Represent less-than-or-equal
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator <=(Term left, Term right)
		{
			return CreateBinaryComparison(Operator.LessEqual, left, right);
		}

		/// <summary>
		/// Represent greater-than-or-equal
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator >=(Term left, Term right)
		{
			return CreateBinaryComparison(Operator.GreaterEqual, left, right);
		}

		/// <summary>
		/// Represent equality
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator ==(Term left, Term right)
		{
			return CreateBinaryComparison(Operator.Equal, left, right);
		}

		/// <summary>
		/// Represent inequality
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator !=(Term left, Term right)
		{
			return CreateBinaryComparison(Operator.Unequal, left, right);
		}

		/// <summary>
		/// Represent equality
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator ==(Term left, string right)
		{
			if (left.ValueClass != TermValueClass.Enumerated)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.IsNotSymbolic, new object[1] { left }));
			}
			if (FindEnumeratedConstant(left.EnumeratedDomain, right, out var constantTerm))
			{
				return left == constantTerm;
			}
			throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolNotFoundInDomain, new object[2] { right, left }));
		}

		/// <summary>
		/// Represent equality
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator ==(string left, Term right)
		{
			return right == left;
		}

		/// <summary>
		/// Represent inequality
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator !=(Term left, string right)
		{
			if (left.ValueClass != TermValueClass.Enumerated)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.IsNotSymbolic, new object[1] { left }));
			}
			if (FindEnumeratedConstant(left.EnumeratedDomain, right, out var constantTerm))
			{
				return left != constantTerm;
			}
			throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolNotFoundInDomain, new object[2] { right, left }));
		}

		/// <summary>
		/// Represent inequality
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator !=(string left, Term right)
		{
			return right != left;
		}

		/// <summary>
		/// Represent less-than
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
		public static Term operator <(Term left, string right)
		{
			if (left.ValueClass != TermValueClass.Enumerated)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.IsNotSymbolic, new object[1] { left }));
			}
			if (FindEnumeratedConstant(left.EnumeratedDomain, right, out var constantTerm))
			{
				return left < constantTerm;
			}
			throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolNotFoundInDomain, new object[2] { right, left }));
		}

		/// <summary>
		/// Represent less-than
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
		public static Term operator <(string left, Term right)
		{
			return right > left;
		}

		/// <summary>
		/// Represent less-than-or-equal
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
		public static Term operator <=(Term left, string right)
		{
			if (left.ValueClass != TermValueClass.Enumerated)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.IsNotSymbolic, new object[1] { left }));
			}
			if (FindEnumeratedConstant(left.EnumeratedDomain, right, out var constantTerm))
			{
				return left <= constantTerm;
			}
			throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolNotFoundInDomain, new object[2] { right, left }));
		}

		/// <summary>
		/// Represent less-than-or-equal
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
		public static Term operator <=(string left, Term right)
		{
			return right >= left;
		}

		/// <summary>
		/// Represent greater-than
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
		public static Term operator >(Term left, string right)
		{
			if (left.ValueClass != TermValueClass.Enumerated)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.IsNotSymbolic, new object[1] { left }));
			}
			if (FindEnumeratedConstant(left.EnumeratedDomain, right, out var constantTerm))
			{
				return left > constantTerm;
			}
			throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolNotFoundInDomain, new object[2] { right, left }));
		}

		/// <summary>
		/// Represent greater-than
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
		public static Term operator >(string left, Term right)
		{
			return right < left;
		}

		/// <summary>
		/// Represent greater-than-or-equal
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
		public static Term operator >=(Term left, string right)
		{
			if (left.ValueClass != TermValueClass.Enumerated)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.IsNotSymbolic, new object[1] { left }));
			}
			if (FindEnumeratedConstant(left.EnumeratedDomain, right, out var constantTerm))
			{
				return left >= constantTerm;
			}
			throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolNotFoundInDomain, new object[2] { right, left }));
		}

		/// <summary>
		/// Represent greater-than-or-equal
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
		public static Term operator >=(string left, Term right)
		{
			return right <= left;
		}

		private static bool FindEnumeratedConstant(Domain domain, string value, out EnumeratedConstantTerm constantTerm)
		{
			constantTerm = null;
			for (int i = 0; i < domain.EnumeratedNames.Length; i++)
			{
				if (domain.EnumeratedNames[i] == value)
				{
					constantTerm = new EnumeratedConstantTerm(domain, i);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Helper function for creating binary comparisons. This is much faster than using the generic
		/// (params) CreateInvocationTerm.
		/// </summary>
		/// <param name="head"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		private static Term CreateBinaryComparison(Operator head, Term left, Term right)
		{
			if ((object)left == null)
			{
				throw new ArgumentNullException("left");
			}
			if ((object)right == null)
			{
				throw new ArgumentNullException("right");
			}
			Model.VerifySaneInputs(left, right);
			Term term = left;
			OperatorTerm operatorTerm = term as OperatorTerm;
			if ((object)operatorTerm != null && operatorTerm.Operation == head)
			{
				term = operatorTerm.Inputs[0];
			}
			if (term.ValueClass == TermValueClass.Enumerated || term.ValueClass == TermValueClass.String)
			{
				if (term.ValueClass == TermValueClass.Enumerated && right.ValueClass == TermValueClass.Enumerated)
				{
					if (right.EnumeratedDomain != term.EnumeratedDomain)
					{
						throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.Inputs0And1HaveDifferentSymbolDomains, new object[2] { term, right }));
					}
				}
				else if (term.ValueClass == TermValueClass.Enumerated && right.ValueClass == TermValueClass.String)
				{
					if (right is StringConstantTerm stringConstantTerm && !term.EnumeratedDomain.EnumeratedNames.Contains(stringConstantTerm._value))
					{
						throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolNotFoundInDomain, new object[2] { stringConstantTerm._value, term }));
					}
				}
				else if (term.ValueClass == TermValueClass.String && right.ValueClass == TermValueClass.Enumerated)
				{
					if (term is StringConstantTerm stringConstantTerm2 && !right.EnumeratedDomain.EnumeratedNames.Contains(stringConstantTerm2._value))
					{
						throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolNotFoundInDomain, new object[2] { stringConstantTerm2._value, right }));
					}
				}
				else if (right.ValueClass != TermValueClass.String)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.IsNotSymbolic, new object[1] { right }));
				}
			}
			else
			{
				if ((object)term == left && !left.IsNumeric)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.IsNotNumeric, new object[1] { left }));
				}
				if (!right.IsNumeric)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.IsNotNumeric, new object[1] { right }));
				}
			}
			TermValueClass valueClass = TermValueClass.Numeric;
			if ((object)term != left)
			{
				Term[] array = new Term[operatorTerm.Inputs.Length + 1];
				for (int i = 0; i < operatorTerm.Inputs.Length; i++)
				{
					array[i] = operatorTerm.Inputs[i];
				}
				array[operatorTerm.Inputs.Length] = right;
				return Model.CreateInvocationTermImpl(head, valueClass, array);
			}
			return Model.CreateInvocationTermImpl(head, valueClass, new Term[2] { left, right });
		}

		/// <summary>
		/// Represent Boolean and
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator &(Term left, Term right)
		{
			Model.VerifySaneInputs(left, right);
			return Model.And(left, right);
		}

		/// <summary>
		/// Represent Boolean or
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns></returns>
		public static Term operator |(Term left, Term right)
		{
			Model.VerifySaneInputs(left, right);
			return Model.Or(left, right);
		}

		/// <summary>
		/// Represent Boolean negation
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static Term operator !(Term input)
		{
			Model.VerifySaneInputs(input);
			return Model.Not(input);
		}

		/// <summary>
		/// Constant double term
		/// </summary>
		/// <param name="value">The constant value</param>
		/// <returns>A new Term</returns>
		public static implicit operator Term(double value)
		{
			if (value == 0.0)
			{
				return _constant0;
			}
			if (value == 1.0)
			{
				return _constant1;
			}
			return new ConstantTerm(value);
		}

		/// <summary>
		/// Constant string term
		/// </summary>
		/// <param name="value">The constant value</param>
		/// <returns>A new Term</returns>
		public static implicit operator Term(string value)
		{
			return new StringConstantTerm(value);
		}

		/// <summary>
		/// Constant boolean term
		/// </summary>
		/// <param name="value">The constant value</param>
		/// <returns>A new Term</returns>
		public static implicit operator Term(bool value)
		{
			if (!value)
			{
				return _constantFalse;
			}
			return _constantTrue;
		}

		/// <summary>
		/// Constant Rational term
		/// </summary>
		/// <param name="value">The constant value</param>
		/// <returns>A new Term</returns>
		public static implicit operator Term(Rational value)
		{
			if (value.IsZero)
			{
				return _constant0;
			}
			if (value.IsOne)
			{
				return _constant1;
			}
			return new ConstantTerm(value);
		}

		internal abstract bool TryEvaluateConstantValue(out Rational value, EvaluationContext context);

		internal virtual bool TryEvaluateConstantValue(out object value, EvaluationContext context)
		{
			Rational value2;
			bool result = TryEvaluateConstantValue(out value2, context);
			value = value2;
			return result;
		}

		internal virtual IEnumerable<Term> AllValues(EvaluationContext context)
		{
			yield return this;
		}

		internal abstract Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg);

		internal abstract Term Clone(string baseName);

		internal static string BuildFullName(string baseName, string name)
		{
			if (!string.IsNullOrEmpty(baseName))
			{
				return baseName + "." + name;
			}
			return name;
		}

		/// <summary>Formats the value of the current instance using the specified format.
		/// </summary>
		/// <param name="format">The format to use (or null).</param>
		/// <param name="formatProvider">The provider to use to format the value (or null).</param>
		/// <returns>The value of the current instance in the specified format.</returns>
		public virtual string ToString(string format, IFormatProvider formatProvider)
		{
			return ToString();
		}
	}
}
