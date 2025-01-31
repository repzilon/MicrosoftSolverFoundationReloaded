using System;
using System.Globalization;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Struct for a literal. A literal is a Boolean variable or the negation of a Boolean variable
	/// </summary>
	public struct Literal : IComparable<Literal>
	{
		private int _id;

		/// <summary>
		/// Null literal
		/// </summary>
		public static readonly Literal Nil = new Literal(-1);

		/// <summary>
		/// Get the ID of the literal
		/// </summary>
		public int Id => _id;

		/// <summary>
		/// Get the Boolean variable that forms this literal
		/// </summary>
		public int Var => _id >> 1;

		/// <summary>
		/// Get the sign of the literal
		/// </summary>
		public bool Sense => (_id & 1) != 0;

		/// <summary>
		/// Whether this literal is a null literal
		/// </summary>
		public bool IsNil => _id < 0;

		/// <summary>
		/// Construct a literal
		/// </summary>
		public Literal(int var, bool fSense)
		{
			_id = var << 1;
			if (fSense)
			{
				_id |= 1;
			}
		}

		internal Literal(int id)
		{
			_id = id;
		}

		/// <summary>
		/// Construct the dual literal of lit
		/// </summary>
		public static Literal operator ~(Literal lit)
		{
			return new Literal(lit._id ^ 1);
		}

		/// <summary>
		/// Compare whether the two literals are the same
		/// </summary>
		public static bool operator ==(Literal lit1, Literal lit2)
		{
			return lit1._id == lit2._id;
		}

		/// <summary>
		/// Compare whether the two literals are different
		/// </summary>
		public static bool operator !=(Literal lit1, Literal lit2)
		{
			return lit1._id != lit2._id;
		}

		/// <summary>
		/// Whether lit1.Id is less than lit2.Id
		/// </summary>
		public static bool operator <(Literal lit1, Literal lit2)
		{
			return lit1._id < lit2._id;
		}

		/// <summary>
		/// Whether lit1.Id is greater than lit2.Id
		/// </summary>
		public static bool operator >(Literal lit1, Literal lit2)
		{
			return lit1._id > lit2._id;
		}

		/// <summary>
		/// Whether lit1.Id is less than or equal to lit2.Id
		/// </summary>
		public static bool operator <=(Literal lit1, Literal lit2)
		{
			return lit1._id <= lit2._id;
		}

		/// <summary>
		/// Whether lit1.Id is greater than or equal to lit2.Id
		/// </summary>
		public static bool operator >=(Literal lit1, Literal lit2)
		{
			return lit1._id >= lit2._id;
		}

		/// <summary>
		/// Return the string representation of this literal
		/// </summary>
		public override string ToString()
		{
			if (!Sense)
			{
				return "~" + Var;
			}
			return Var.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Test whether obj is the same literal as this one
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj is Literal)
			{
				return ((Literal)obj)._id == _id;
			}
			return false;
		}

		/// <summary>
		/// Get the hash code of this literal
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return _id.GetHashCode();
		}

		/// <summary>
		/// Compare this literal to lit
		/// </summary>
		public int CompareTo(Literal lit)
		{
			return _id.CompareTo(lit._id);
		}
	}
}
