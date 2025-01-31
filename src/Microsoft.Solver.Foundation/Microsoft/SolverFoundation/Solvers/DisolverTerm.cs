using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   As term as manipulated by Disolver.
	/// </summary>
	/// <remarks> 
	///   The main goal of terms is to be used as "intermediate representations"
	///   of the problem on which preprocessing can be done and from which one
	///   or more representation(s) specialised for the algorithm(s) considered
	///   can be created, depending on the analysis of the problem.
	/// </remarks>
	internal abstract class DisolverTerm : CspTerm
	{
		protected readonly IntegerSolver _solver;

		protected object _key;

		protected readonly DisolverTerm[] _subterms;

		public override ConstraintSystem Model => null;

		public override CspDomain.ValueKind Kind => CspDomain.ValueKind.Integer;

		public override IEnumerable<CspTerm> Inputs
		{
			get
			{
				try
				{
					DisolverTerm[] subterms = _subterms;
					for (int i = 0; i < subterms.Length; i++)
					{
						yield return subterms[i];
					}
				}
				finally
				{
				}
			}
		}

		/// <summary>
		///   Direct, internal access to the array of subterms
		/// </summary>
		internal DisolverTerm[] SubTerms => _subterms;

		/// <summary>
		///   accessor is to be called by user only (for internal initialization
		///   use protected _key field directly instead as call to key
		///   would add the term to user-named terms)
		/// </summary>
		public override object Key
		{
			get
			{
				return _key;
			}
			set
			{
				_key = value;
				_solver.RecordKey(value, this);
			}
		}

		/// <summary>
		///   Returns the interval of values for the term
		/// </summary>
		public Interval InitialRange => new Interval(InitialLowerBound, InitialUpperBound);

		/// <summary>
		///   Get initial lower bound of the term, implictly seen as Integer 
		/// </summary>
		public abstract long InitialLowerBound { get; }

		/// <summary>
		///   Get initial upper bound of the term, implictly seen as Integer 
		/// </summary>
		public abstract long InitialUpperBound { get; }

		public override IEnumerable<object> CurrentValues
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		///   Construction of a Term
		/// </summary>
		internal DisolverTerm(IntegerSolver solver, DisolverTerm[] subterms)
		{
			_solver = solver;
			_subterms = subterms;
			solver.AddTerm(this);
		}

		/// <summary>
		///   True if the term is NOT functionally-dependent on other Terms.
		///   Typically this means we have to choose a value for this Term
		///   (in contrast the value of other Terms is determined)
		/// </summary>
		public virtual bool IsUserDefined()
		{
			return false;
		}

		/// <summary>
		///   True if the variable is initially instantiated
		/// </summary>
		public bool IsInstantiated()
		{
			return InitialLowerBound == InitialUpperBound;
		}

		/// <summary>
		///   Gets the value of an instantiated term
		/// </summary>
		public long GetValue()
		{
			return InitialUpperBound;
		}

		public override CspTerm Field(object key, int index)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<CspTerm> Fields(object key)
		{
			throw new NotImplementedException();
		}
	}
}
