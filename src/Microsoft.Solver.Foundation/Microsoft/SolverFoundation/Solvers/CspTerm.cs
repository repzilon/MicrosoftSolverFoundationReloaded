using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A Term is the InnerSolver unit of modelling, representing a CspVariable or a Function.
	///           A Term is not changed by solving (if you look at how solutions are returned, the ValueSets
	///             are paired but distinct) and multiple solutions will return the same instances for the Variables.
	/// </summary>
	public abstract class CspTerm
	{
		/// <summary> The ConstraintSystem that this Term belongs to.
		/// </summary>
		public abstract ConstraintSystem Model { get; }

		/// <summary>  All Variable Terms are created with a key object that serves as their identifier.
		///             Variable keys will be added to a Dictionary and must be non-null and unique.
		///            A Function Term by default has no key.  You can assign a key if you wish.  The Key is then
		///             added to the Dictionary and will appear in the Solution.
		///           Attempting to change an existing key will cause an InvalidOperationException.
		/// </summary>
		public abstract object Key { get; set; }

		/// <summary>
		/// Return the value kind of this Term
		/// </summary>
		public abstract CspDomain.ValueKind Kind { get; }

		/// <summary>
		/// Enumerates current possible values in the domain of this Term. Returned values need to be cast into the
		/// correct type depending on the data type of this Term.
		/// </summary>
		public abstract IEnumerable<object> CurrentValues { get; }

		/// <summary>  Is this Term tied to a Boolean Domain?
		/// </summary>
		public abstract bool IsBoolean { get; }

		/// <summary> The array of Terms which are inputs to this Term.  This will be null if there are no inputs,
		///           which includes all Variables.
		/// </summary>
		public abstract IEnumerable<CspTerm> Inputs { get; }

		/// <summary>
		/// Get the field labeled with the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns>An array of terms that correspond to all elements in the field. If the field is a singleton, then the returned array has only one element.</returns>
		public abstract IEnumerable<CspTerm> Fields(object key);

		/// <summary>
		/// Get the ith element of the field labeld with the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="index">The index</param>
		/// <returns></returns>
		public abstract CspTerm Field(object key, int index);

		/// <summary>
		/// Get the first element (or the only element if the field is a singleton) of the fielded labeled by the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public CspTerm Field(object key)
		{
			return Field(key, 0);
		}

		/// <summary>
		/// Return a Term that represents term1 + term2
		/// </summary>
		public static CspTerm operator +(CspTerm term1, CspTerm term2)
		{
			return term1.Model.Sum(term1, term2);
		}

		/// <summary>
		/// Return a Term that represents term + num
		/// </summary>
		public static CspTerm operator +(CspTerm term, int num)
		{
			return term.Model.Sum(term, term.Model.Constant(num));
		}

		/// <summary>
		/// Return a Term that represents num + term
		/// </summary>
		public static CspTerm operator +(int num, CspTerm term)
		{
			return term.Model.Sum(term.Model.Constant(num), term);
		}

		/// <summary>
		/// Return a Term that represents term + num
		/// </summary>
		public static CspTerm operator +(CspTerm term, double num)
		{
			return term.Model.Sum(term, term.Model.Constant(num));
		}

		/// <summary>
		/// Return a Term that represents num + term
		/// </summary>
		public static CspTerm operator +(double num, CspTerm term)
		{
			return term.Model.Sum(term.Model.Constant(num), term);
		}

		/// <summary>
		/// Return a Term that represents term1 - term2
		/// </summary>
		public static CspTerm operator -(CspTerm term1, CspTerm term2)
		{
			return term1.Model.Sum(term1, -term2);
		}

		/// <summary>
		/// Return a Term that represents term - num
		/// </summary>
		public static CspTerm operator -(CspTerm term, int num)
		{
			return term.Model.Sum(term, term.Model.Constant(-num));
		}

		/// <summary>
		/// Return a Term that represents num - term
		/// </summary>
		public static CspTerm operator -(int num, CspTerm term)
		{
			return term.Model.Sum(term.Model.Constant(num), -term);
		}

		/// <summary>
		/// Return a Term that represents term - num
		/// </summary>
		public static CspTerm operator -(CspTerm term, double num)
		{
			return term.Model.Sum(term, term.Model.Constant(0.0 - num));
		}

		/// <summary>
		/// Return a Term that represents num - term
		/// </summary>
		public static CspTerm operator -(double num, CspTerm term)
		{
			return term.Model.Sum(term.Model.Constant(num), -term);
		}

		/// <summary>
		/// Return a Term that represents term1 * term2
		/// </summary>
		public static CspTerm operator *(CspTerm term1, CspTerm term2)
		{
			return term1.Model.Product(term1, term2);
		}

		/// <summary>
		/// Return a Term that represents term * num
		/// </summary>
		public static CspTerm operator *(CspTerm term, int num)
		{
			return term.Model.Product(term, term.Model.Constant(num));
		}

		/// <summary>
		/// Return a Term that represents num * term
		/// </summary>
		public static CspTerm operator *(int num, CspTerm term)
		{
			return term.Model.Product(term.Model.Constant(num), term);
		}

		/// <summary>
		/// Return a Term that represents term * num
		/// </summary>
		public static CspTerm operator *(CspTerm term, double num)
		{
			return term.Model.Product(term, term.Model.Constant(num));
		}

		/// <summary>
		/// Return a Term that represents num * term
		/// </summary>
		public static CspTerm operator *(double num, CspTerm term)
		{
			return term.Model.Product(term.Model.Constant(num), term);
		}

		/// <summary>
		/// Return a Term that represents -term
		/// </summary>
		public static CspTerm operator -(CspTerm term)
		{
			return term.Model.Neg(term);
		}

		/// <summary>
		/// Return a Boolean Term that represents Not(term)
		/// </summary>
		public static CspTerm operator !(CspTerm term)
		{
			return term.Model.Not(term);
		}

		/// <summary>
		/// Return a Boolean Term that represents Or(term1, term2)
		/// </summary>
		public static CspTerm operator |(CspTerm term1, CspTerm term2)
		{
			return term1.Model.Or(term1, term2);
		}

		/// <summary>
		/// Return a Boolean Term that represents And(term1, term2)
		/// </summary>
		public static CspTerm operator &(CspTerm term1, CspTerm term2)
		{
			return term1.Model.And(term1, term2);
		}

		/// <summary>
		/// Return a Boolean Term that represents LessEqual(term1, term2)
		/// </summary>
		public static CspTerm operator <=(CspTerm term1, CspTerm term2)
		{
			return term1.Model.LessEqual(term1, term2);
		}

		/// <summary>
		/// Return a Boolean Term that represents LessEqual(term, num)
		/// </summary>
		public static CspTerm operator <=(CspTerm term, int num)
		{
			return term.Model.LessEqual(term, term.Model.Constant(num));
		}

		/// <summary>
		/// Return a Boolean Term that represents LessEqual(num, term)
		/// </summary>
		public static CspTerm operator <=(int num, CspTerm term)
		{
			return term.Model.LessEqual(term.Model.Constant(num), term);
		}

		/// <summary>
		/// Return a Boolean Term that represents LessEqual(term, num)
		/// </summary>
		public static CspTerm operator <=(CspTerm term, double num)
		{
			return term.Model.LessEqual(term, term.Model.Constant(num));
		}

		/// <summary>
		/// Return a Boolean Term that represents LessEqual(num, term)
		/// </summary>
		public static CspTerm operator <=(double num, CspTerm term)
		{
			return term.Model.LessEqual(term.Model.Constant(num), term);
		}

		/// <summary>
		/// Return a Boolean Term that represents Less(term1, term2)
		/// </summary>
		public static CspTerm operator <(CspTerm term1, CspTerm term2)
		{
			return term1.Model.Less(term1, term2);
		}

		/// <summary>
		/// Return a Boolean Term that represents Less(term, num)
		/// </summary>
		public static CspTerm operator <(CspTerm term, int num)
		{
			return term.Model.Less(term, term.Model.Constant(num));
		}

		/// <summary>
		/// Return a Boolean Term that represents Less(num, term)
		/// </summary>
		public static CspTerm operator <(int num, CspTerm term)
		{
			return term.Model.Less(term.Model.Constant(num), term);
		}

		/// <summary>
		/// Return a Boolean Term that represents Less(term, num)
		/// </summary>
		public static CspTerm operator <(CspTerm term, double num)
		{
			return term.Model.Less(term, term.Model.Constant(num));
		}

		/// <summary>
		/// Return a Boolean Term that represents Less(num, term)
		/// </summary>
		public static CspTerm operator <(double num, CspTerm term)
		{
			return term.Model.Less(term.Model.Constant(num), term);
		}

		/// <summary>
		/// Return a Boolean Term that represents GreaterEqual(term1, term2)
		/// </summary>
		public static CspTerm operator >=(CspTerm term1, CspTerm term2)
		{
			return term1.Model.GreaterEqual(term1, term2);
		}

		/// <summary>
		/// Return a Boolean Term that represents GreaterEqual(term, num)
		/// </summary>
		public static CspTerm operator >=(CspTerm term, int num)
		{
			return term.Model.GreaterEqual(term, term.Model.Constant(num));
		}

		/// <summary>
		/// Return a Boolean Term that represents GreaterEqual(num, term)
		/// </summary>
		public static CspTerm operator >=(int num, CspTerm term)
		{
			return term.Model.GreaterEqual(term.Model.Constant(num), term);
		}

		/// <summary>
		/// Return a Boolean Term that represents GreaterEqual(term, num)
		/// </summary>
		public static CspTerm operator >=(CspTerm term, double num)
		{
			return term.Model.GreaterEqual(term, term.Model.Constant(num));
		}

		/// <summary>
		/// Return a Boolean Term that represents GreaterEqual(num, term)
		/// </summary>
		public static CspTerm operator >=(double num, CspTerm term)
		{
			return term.Model.GreaterEqual(term.Model.Constant(num), term);
		}

		/// <summary>
		/// Return a Boolean Term that represents Greater(term1, term2)
		/// </summary>
		public static CspTerm operator >(CspTerm term1, CspTerm term2)
		{
			return term1.Model.Greater(term1, term2);
		}

		/// <summary>
		/// Return a Boolean Term that represents Greater(term, num)
		/// </summary>
		public static CspTerm operator >(CspTerm term, int num)
		{
			return term.Model.Greater(term, term.Model.Constant(num));
		}

		/// <summary>
		/// Return a Boolean Term that represents Greater(num, term)
		/// </summary>
		public static CspTerm operator >(int num, CspTerm term)
		{
			return term.Model.Greater(term.Model.Constant(num), term);
		}

		/// <summary>
		/// Return a Boolean Term that represents Greater(term, num)
		/// </summary>
		public static CspTerm operator >(CspTerm term, double num)
		{
			return term.Model.Greater(term, term.Model.Constant(num));
		}

		/// <summary>
		/// Return a Boolean Term that represents Greater(num, term)
		/// </summary>
		public static CspTerm operator >(double num, CspTerm term)
		{
			return term.Model.Greater(term.Model.Constant(num), term);
		}
	}
}
