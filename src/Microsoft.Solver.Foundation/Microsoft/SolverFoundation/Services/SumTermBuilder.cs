using System;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A SumTermBuilder is used to build Sum terms efficiently.
	/// </summary>
	/// <remarks>
	/// This class represents a list of operand terms that will be combined into a Sum term.
	/// The resulting Sum term can be used in goals or constraints. This class is useful when
	/// the number of terms in the sum is unknown at compilation time (otherwise the operands
	/// may be passed as an array to Model.Sum). Individual terms are added to SumTermBuilder
	/// using the Add method. The ToTerm method returns a Sum term with the previously added
	/// terms as operands.  The Clear method is convenient when the same SumTermBuilder is
	/// used repeatedly to create Sum terms.
	/// </remarks>
	public sealed class SumTermBuilder
	{
		private Term[] _terms;

		private int _count;

		/// <summary>Creates a new instance with the specified initial capacity.
		/// </summary>
		/// <param name="capacity">The initial capacity of the SumTermBuilder.</param>
		/// <remarks>If the number of terms exceeds the capacity the SumTermBuilder will be 
		/// resized appropriately.</remarks>
		public SumTermBuilder(int capacity)
		{
			if (capacity < 1)
			{
				capacity = 1;
			}
			_terms = new Term[capacity];
		}

		/// <summary>Clears the SumTermBuilder.
		/// </summary>
		public void Clear()
		{
			_terms = new Term[_count];
			_count = 0;
		}

		/// <summary>Add a new Term.
		/// </summary>
		/// <param name="term">A term to add to the SumTermBuilder.</param>
		public void Add(Term term)
		{
			if (_terms.Length <= _count)
			{
				Array.Resize(ref _terms, _count * 2);
			}
			_terms[_count++] = term;
		}

		/// <summary>Returns a Term object that represents the Sum of the previously added operand terms.
		/// </summary>
		/// <returns>A Sum term.</returns>
		public Term ToTerm()
		{
			if (_terms.Length != _count)
			{
				Array.Resize(ref _terms, _count);
			}
			Model.VerifyNumericInputs(Operator.Plus, _terms);
			return new PlusTerm(_terms, TermValueClass.Numeric);
		}
	}
}
