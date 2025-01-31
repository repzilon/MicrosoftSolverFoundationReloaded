using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// An entity whose value can be Boolean, integer, or real, 
	/// and that can be re-evaluated incrementally subject to changes
	/// in the values of other evaluables.
	/// </summary>
	internal abstract class EvaluableTerm
	{
		/// <summary>
		/// A constant whose bits are 1, 0, 0, 0, 0, 0...
		/// </summary>
		private const int _mask100000etc = int.MinValue;

		/// <summary>
		/// A constant whose bits and 0, 1, 1, 1, 1, 1...
		/// </summary>
		private const int _mask011111etc = int.MaxValue;

		/// <summary>
		/// Stores both the Enqueued tag (sign bit)
		/// and the depth of the term (other bits)
		/// </summary>
		private int _state;

		/// <summary>
		/// List of evaluables that depend on this. 
		/// Null if there is no dependent
		/// </summary>
		protected EvaluableTerm[] _dependents;

		/// <summary>
		/// Flag that records whether 
		/// the term is currently enqueued for re-evaluation
		/// </summary>
		public bool IsEnqueued => _state < 0;

		/// <summary>
		/// Depth: 0 for an evaluable without predecessor,
		/// otherwise 1 plus the depth of the highest predecessor
		/// </summary>
		public int Depth => _state & 0x7FFFFFFF;

		/// <summary>
		/// True if the term is immutable
		/// </summary>
		public virtual bool IsConstant => false;

		/// <summary>
		/// Get the operation that labels this term.
		/// Returns an invalid TermModelOperation (-1) if the term is not an operation
		/// </summary>
		internal virtual TermModelOperation Operation => (TermModelOperation)(-1);

		/// <summary>
		/// Get the value of the term, interpreted as a real
		/// </summary>
		public abstract double ValueAsDouble { get; }

		/// <summary>
		/// Get the double value used internally by the 
		/// term (violation or numerical result)
		/// </summary>
		/// <remarks>
		/// This is used mostly for some assertions; for normal
		/// use do NOT use this method.
		/// </remarks>
		internal abstract double StoredValue { get; }

		/// <summary>
		/// Constructor called by derived classes;
		/// the depth is specified at constrution time
		/// </summary>
		protected EvaluableTerm(int depth)
		{
			_state = depth;
		}

		protected static int MaxDepth(params EvaluableTerm[] termList)
		{
			int num = 0;
			foreach (EvaluableTerm evaluableTerm in termList)
			{
				num = Math.Max(num, evaluableTerm.Depth);
			}
			return num;
		}

		/// <summary>        
		/// Indicate that the term is currently enqueued for re-evaluation
		/// </summary>
		internal void MarkEnqueued()
		{
			_state |= int.MinValue;
		}

		/// <summary>
		/// Indicate that the term is not currently enqueued for re-evaluation
		/// </summary>
		internal void MarkDequeued()
		{
			_state &= int.MaxValue;
		}

		/// <summary>
		/// MarkDequeued a whole set of terms
		/// </summary>
		internal static void DequeueAll(IEnumerable<EvaluableTerm> termSet)
		{
			foreach (EvaluableTerm item in termSet)
			{
				item.MarkDequeued();
			}
		}

		/// <summary>
		/// Naive recomputation: Recompute the value of this
		/// from the value of all its inputs
		/// </summary>
		internal abstract void Recompute(out bool change);

		/// <summary>
		/// (Re)intialization. Defaults to a  simple recomputation.
		/// Overload if the term needs more during its (re)initialization, 
		/// in particular to (re) allocate data-structures
		/// </summary>
		internal virtual void Reinitialize(out bool change)
		{
			Recompute(out change);
		}

		/// <summary>
		/// True if the term is currently correctly evaluated
		/// from its inputs.
		/// </summary>
		internal bool IsStable()
		{
			Reinitialize(out var change);
			if (change)
			{
				return double.IsNaN(StoredValue);
			}
			return true;
		}

		/// <summary>
		/// Compute recursively the set of terms that are inputs of
		/// inputs... of this term. Includes the term itself.
		/// </summary>
		internal List<EvaluableTerm> CollectSubTerms()
		{
			return CollectSubTerms(new EvaluableTerm[1] { this });
		}

		/// <summary>
		/// Computes the set of descendants from an initial list of terms: 
		/// includes the terms themselves, their inputs, their inputs'inputs, ...
		/// </summary>
		/// <remarks>
		/// Implementation is not recursive to avoid stack overflow 
		/// problems with very deep terms.
		///
		/// Uses the IsEnqueued Tag to mark the visited nodes. 
		/// For this reason should be called ONLY when we are not 
		/// evaluating the terms, and when no term is enqueued.
		/// </remarks>
		internal static List<EvaluableTerm> CollectSubTerms(IEnumerable<EvaluableTerm> initialTerms)
		{
			Stack<EvaluableTerm> stack = new Stack<EvaluableTerm>();
			List<EvaluableTerm> list = new List<EvaluableTerm>();
			foreach (EvaluableTerm initialTerm in initialTerms)
			{
				Explore(list, stack, initialTerm);
			}
			while (stack.Count > 0)
			{
				EvaluableTerm evaluableTerm = stack.Pop();
				foreach (EvaluableTerm item in evaluableTerm.EnumerateInputs())
				{
					Explore(list, stack, item);
				}
			}
			DequeueAll(list);
			return list;
		}

		/// <summary>
		/// Marking used by Depth-first search algorithms
		/// </summary>
		internal static void Explore(List<EvaluableTerm> set, Stack<EvaluableTerm> stack, EvaluableTerm term)
		{
			if (!term.IsEnqueued)
			{
				set.Add(term);
				stack.Push(term);
				term.MarkEnqueued();
			}
		}

		/// <summary>
		/// Reschedules all dependents of the term
		/// </summary>
		internal void RescheduleDependents(TermEvaluator solver)
		{
			if (_dependents != null)
			{
				EvaluableTerm[] dependents = _dependents;
				foreach (EvaluableTerm e in dependents)
				{
					solver.Reschedule(e);
				}
			}
		}

		/// <summary>
		/// Initialize the list of dependents of the term
		/// </summary>
		internal void InitializeDependentList(EvaluableTerm[] list)
		{
			_dependents = list;
		}

		/// <summary>
		/// Enumerates all the terms that this one depends on
		/// </summary>
		internal abstract IEnumerable<EvaluableTerm> EnumerateInputs();

		/// <summary>
		/// Enumerate the inputs which, if re-assigned, 
		/// are likely to change the term's value. 
		/// </summary>
		/// <remarks>
		/// In particular for a Boolean Term the inputs returned are
		/// restricted to the ones whose re-assignment might change the
		/// term's polarity. For numerical terms using the default 
		/// enumeration of all inputs is typical.
		/// </remarks>
		internal virtual IEnumerable<EvaluableTerm> EnumerateMoveCandidates()
		{
			return EnumerateInputs();
		}

		public abstract EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map);
	}
}
