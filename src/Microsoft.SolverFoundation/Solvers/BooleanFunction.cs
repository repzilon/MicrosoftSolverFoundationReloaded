using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Abstract base class for all Boolean-result constraints
	/// </summary>
	internal abstract class BooleanFunction : CspFunction
	{
		/// <summary> A possible violation for a false term. By no means normative
		/// </summary>
		internal static readonly int Violated = ScaleDown(CspSolverTerm.MaxSafePositiveValue);

		/// <summary> A possible violation for a True term. By no means normative
		/// </summary>
		internal static readonly int Satisfied = -Violated;

		public override bool IsBoolean => true;

		internal override bool IsTrue
		{
			get
			{
				if (1 != base.Count)
				{
					throw new InvalidOperationException(Resources.InvalidIsTrueCall + ToString());
				}
				return 1 == First;
			}
		}

		internal override int OutputScale => 1;

		/// <summary> Abstract base class for all Boolean-result constraints
		/// </summary>
		internal BooleanFunction(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, ConstraintSystem.DBoolean, inputs)
		{
		}

		/// <summary> Represent this class
		/// </summary>
		public override string ToString()
		{
			return Name + ((1 != base.Count) ? ((1 < base.Count) ? " {?}" : " {}") : (IsTrue ? " {T}" : " {F}"));
		}

		/// <summary> Scales a possibly large number down to something within the
		///           range [-32, +32]. Scaling-down is sign-preserving 
		///           and reflects somehow how far we are from 0. In particular 
		///           the result is guaranteed to be 0 only for the input 0
		/// </summary>
		/// <remarks> Used for two reasons: (1) we don't want some violations to 
		///           have an overwhelmingly large value: if an equality between
		///           two large numbers has a violation equal to the
		///           distance between them this will tend to make the violation
		///           of an Unequal term negligible. (2) by systematically scaling
		///           down any potentially large number we avoid any overflow
		/// </remarks>
		internal static int ScaleDown(int num)
		{
			if (num == int.MinValue)
			{
				return -32;
			}
			uint num2 = (uint)((num >= 0) ? num : (-num));
			int num3 = 0;
			while (num2 != 0)
			{
				num3++;
				num2 >>= 1;
			}
			if (num < 0)
			{
				return -num3;
			}
			return num3;
		}

		/// <summary> Scales a possibly large number down to something within the
		///           range [-64, +64]. Scaling-down is sign-preserving 
		///           and reflects somehow how far we are from 0. In particular 
		///           the result is guaranteed to be 0 only for the input 0
		/// </summary>
		internal static int ScaleDown(long num)
		{
			if (num == long.MinValue)
			{
				return -64;
			}
			ulong num2 = (ulong)((num >= 0) ? num : (-num));
			int num3 = 0;
			while (num2 != 0)
			{
				num3++;
				num2 >>= 1;
			}
			if (num < 0)
			{
				return -num3;
			}
			return num3;
		}

		/// <summary> Given a non-zero violation as represented internally, returns 
		///           a non-negative version (if the expression is true we return a 
		///           violation of 0 instead of the negative truth indicator)
		/// </summary>
		/// <param name="violation">A non-zero violation indicator 
		///           (the higher the more violated)
		/// </param>
		internal static int NonNegative(int violation)
		{
			if (violation <= 0)
			{
				return 0;
			}
			return violation;
		}

		/// <summary> Given a non-negative violation, returns a non-zero version
		///           as suitable for an internal representation (if the
		///           violation is 0 then we return -1)
		/// </summary>
		/// <param name="violation">A non-zero violation indicator 
		///           (the higher the more violated)
		/// </param>
		internal static int NonZero(int violation)
		{
			if (violation != 0)
			{
				return violation;
			}
			return -1;
		}

		/// <summary> Given a non-zero violation as represented internally, 
		///           checks if it actually represents falsity (negative)
		///           of truth (positive)
		/// </summary>
		/// <param name="violation">A non-zero violation indicator 
		///           (the higher the more violated)
		/// </param>
		internal static bool IsSatisfied(int violation)
		{
			return violation < 0;
		}

		/// <summary> And of two (non-zero) violations
		/// </summary>
		/// <param name="l">A non-zero violation indicator 
		///           (the higher the more violated)</param>
		/// <param name="r">A non-zero violation indicator 
		///           (the higher the more violated)</param>
		/// <remarks>
		///   if both sides are positive (violated) then the conjunction is 
		///   positive and equals the work to do to truthify both, i.e. sum;
		///   if both sides are negative (satisfied) then the conjunction is 
		///   negative and as satisfied as the least satisfied, i.e. max;
		///   if exactly 1 side is positive (violated) then the conjunction is
		///   as violated as this side which being the only positive is the max
		/// </remarks>
		internal static int And(int l, int r)
		{
			return (l > 0 && r > 0) ? (l + r) : Math.Max(l, r);
		}

		/// <summary> Or of two (non-zero) violations
		/// </summary>
		/// <param name="l">A non-zero violation indicator 
		///           (the higher the more violated)</param>
		/// <param name="r">A non-zero violation indicator 
		///           (the higher the more violated)</param>
		/// <remarks>
		///   if both sides are negative (satisfied) then the disjunction is 
		///   negative and equals the work to do to falsify both, i.e. sum;
		///   if both sides are positive (violated) then the disjunction is 
		///   positive and as violated as the least violated, i.e. min;
		///   if exactly 1 side is negative (satisfied) then the disjunction is
		///   as satisfied as this side which being the only negative is the min
		/// </remarks>
		internal static int Or(int l, int r)
		{
			return (l < 0 && r < 0) ? (l + r) : Math.Min(l, r);
		}

		/// <summary> Negation of a violation  </summary>
		/// <param name="violation">A non-zero violation indicator 
		///           (the higher the more violated)</param>
		internal static int Not(int violation)
		{
			return -violation;
		}

		/// <summary> Returns a score of how much l and r violate the LessEqual order
		/// </summary>
		/// <remarks>
		///   LessEqual(0, 0) gives -1, not violated and one-unit move
		///   would suffice to violate;
		///   LessEqual(0, 10) gives something a bit more negative
		///   LessEqual(1, 0) gives +1 : violated and a one-unit move
		///   would suffice to satisfy;
		///   LessEqual(10, 0) gives something a bit more positive
		/// </remarks>
		internal static int LessEqual(int l, int r)
		{
			if (CspSolverTerm.IsSafe(l) && CspSolverTerm.IsSafe(r))
			{
				int num = l - r;
				if (num <= 0)
				{
					num--;
				}
				return ScaleDown(num);
			}
			if (l > r)
			{
				return Violated;
			}
			return Satisfied;
		}

		/// <summary> Returns a score of how much l and r violate the LessStrict order
		/// </summary>
		internal static int LessStrict(int l, int r)
		{
			if (CspSolverTerm.IsSafe(l) && CspSolverTerm.IsSafe(r))
			{
				int num = l - r;
				if (num >= 0)
				{
					num++;
				}
				return ScaleDown(num);
			}
			if (l >= r)
			{
				return Violated;
			}
			return Satisfied;
		}

		/// <summary> Returns a score of how much l and r violate the equality
		/// </summary>
		internal static int Equal(int l, int r)
		{
			if (CspSolverTerm.IsSafe(l) && CspSolverTerm.IsSafe(r))
			{
				if (l == r)
				{
					return -1;
				}
				int num = Math.Abs(l - r);
				return ScaleDown(num);
			}
			if (l != r)
			{
				return Violated;
			}
			return Satisfied;
		}

		/// <summary> Returns a score of how much l and r violate the disequality
		/// </summary>
		internal static int Unequal(int l, int r)
		{
			return Not(Equal(l, r));
		}
	}
}
