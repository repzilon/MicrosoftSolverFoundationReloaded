using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A Term is the ConstraintSystem unit of modelling, representing a CspVariable or a Function.
	/// </summary>
	[DebuggerDisplay("{ToString()} : {Ordinal.ToString()}")]
	internal abstract class CspSolverTerm : CspTerm, IVisitable
	{
		internal enum TermKinds
		{
			DecisionVariable,
			Function,
			Constant,
			TemplateVariable,
			CompositeVariable
		}

		internal List<CspSolverDomain> _values;

		private List<CspFunction> _dependents;

		private int _changeCount;

		private int _ordinal;

		private readonly int _depth;

		private bool _isConstraint;

		private ConstraintSystem _solver;

		private object _key;

		private TermKinds _termKind;

		/// <summary> Maximum Value manipulated by local search 
		/// </summary>
		internal static readonly int MaxSafePositiveValue = 1073741823;

		/// <summary> Minimum Value manipulated by local search
		/// </summary>
		internal static readonly int MinSafeNegativeValue = -1073741823;

		internal int Ordinal => _ordinal;

		/// <summary> Depth of the term: 0 for terms with no inputs; 
		///           otherwise 1 + max(depth of the inputs)
		/// </summary>
		internal int Depth => _depth;

		/// <summary> Flag set to true iff the term is a Boolean term
		///           that is imposed as a constraint
		/// </summary>
		internal bool IsConstraint
		{
			get
			{
				return _isConstraint;
			}
			set
			{
				_isConstraint = value;
			}
		}

		public override ConstraintSystem Model
		{
			[DebuggerStepThrough]
			get
			{
				return _solver;
			}
		}

		public sealed override object Key
		{
			get
			{
				return _key;
			}
			set
			{
				if (_key != null)
				{
					throw new InvalidOperationException(Resources.InvalidKeyChange + _key.ToString());
				}
				_key = value;
			}
		}

		public override IEnumerable<object> CurrentValues
		{
			get
			{
				foreach (int t in FiniteValue.Forward())
				{
					yield return GetValue(t);
				}
			}
		}

		public override IEnumerable<CspTerm> Inputs
		{
			get
			{
				if (Args != null)
				{
					for (int i = 0; i < Args.Length; i++)
					{
						yield return Args[i];
					}
				}
			}
		}

		internal TermKinds TermKind
		{
			get
			{
				return _termKind;
			}
			set
			{
				_termKind = value;
			}
		}

		/// <summary> Does this Term have any influence on the model?
		/// </summary>
		internal virtual bool Participates => true;

		internal ConstraintSystem InnerSolver
		{
			[DebuggerStepThrough]
			get
			{
				return _solver;
			}
		}

		internal List<CspFunction> Dependents
		{
			[DebuggerStepThrough]
			get
			{
				return _dependents;
			}
		}

		internal CspSolverDomain FiniteValue
		{
			[DebuggerStepThrough]
			get
			{
				return _values[_values.Count - 1];
			}
		}

		internal abstract CspSolverDomain BaseValueSet { get; }

		internal abstract CspSolverTerm[] Args { get; }

		/// <summary>
		/// Return the scale factor of the output.
		/// </summary>
		/// <returns>The scale factor</returns>
		internal abstract int OutputScale { get; }

		/// <summary>
		/// Return the symbols if this CspSolverTerm is a string variable. Otherwise, return null.
		/// </summary>
		internal virtual CspSymbolDomain Symbols => null;

		internal List<CspSolverDomain> Values
		{
			[DebuggerStepThrough]
			get
			{
				return _values;
			}
		}

		/// <summary> The count of inputs on this xConstraint.
		/// </summary>
		internal abstract int Width { get; }

		/// <summary> The count of possible choices of this Term.
		/// </summary>
		public int Count
		{
			[DebuggerStepThrough]
			get
			{
				return FiniteValue.Count;
			}
		}

		/// <summary> The first value in the restriction otherSet
		/// </summary>
		internal virtual int First
		{
			[DebuggerStepThrough]
			get
			{
				return FiniteValue.First;
			}
		}

		/// <summary> The last value in the restriction otherSet
		/// </summary>
		internal virtual int Last
		{
			[DebuggerStepThrough]
			get
			{
				return FiniteValue.Last;
			}
		}

		/// <summary> The ChangeCount records the time of the most recent propagation through
		///           this Term.  The ChangeCount of inputs and dependents are compared to see
		///           if the dependent needs to be re-evaluated or is up-to-date.
		/// </summary>
		internal int ChangeCount
		{
			get
			{
				return _changeCount;
			}
			set
			{
				_changeCount = value;
			}
		}

		/// <summary>  Is this Term tied to a Boolean Domain?
		/// </summary>
		public override bool IsBoolean => false;

		internal abstract bool IsTrue { get; }

		internal CspSolverTerm(ConstraintSystem solver, CspComposite domain, int depth)
		{
			_solver = solver;
			_termKind = TermKinds.CompositeVariable;
			_depth = depth;
			_isConstraint = false;
		}

		/// <summary> A Term is the ConstraintSystem unit of modelling, representing a CspVariable or a Function.
		/// </summary>
		internal CspSolverTerm(ConstraintSystem solver, CspSolverDomain domain, TermKinds kind, int depth)
		{
			_solver = solver;
			_dependents = new List<CspFunction>();
			_values = new List<CspSolverDomain>();
			_termKind = kind;
			_ordinal = _solver.AllTerms.Count;
			_values.Add(domain);
			solver.AddTerm(this);
			_depth = depth;
			_isConstraint = false;
			ConstraintSystem.UpdateAllTermCount(_solver, 1);
		}

		public abstract void Accept(IVisitor visitor);

		public override IEnumerable<CspTerm> Fields(object key)
		{
			throw new InvalidOperationException(Resources.NonCompositeFieldAccess);
		}

		public override CspTerm Field(object key, int index)
		{
			throw new InvalidOperationException(Resources.NonCompositeFieldAccess);
		}

		/// <summary> Is the specified Variable currently watched by this xConstraint?  Some Constraints
		///           (for example, CNF) have dynamically changing watch lists.
		/// </summary>
		internal virtual bool IsWatched(CspSolverTerm var)
		{
			if (Args == null)
			{
				return false;
			}
			return 0 <= Array.IndexOf(Args, var);
		}

		/// <summary> Enumerate allowed choices from first to last inclusive.
		/// </summary>
		internal virtual IEnumerable<int> Forward(int first, int last)
		{
			return FiniteValue.Forward(first, last);
		}

		/// <summary> Enumerate all allowed choices from least to greatest.
		/// </summary>
		internal virtual IEnumerable<int> Forward()
		{
			return FiniteValue.Forward();
		}

		/// <summary> Enumerate allowed choices from first to last inclusive.
		/// </summary>
		internal virtual IEnumerable<int> Backward(int last, int first)
		{
			return FiniteValue.Backward(last, first);
		}

		/// <summary> Enumerate all allowed choices from greatest to least.
		/// </summary>
		internal virtual IEnumerable<int> Backward()
		{
			return FiniteValue.Backward();
		}

		/// <summary> Check if the given value is an element of the current domain.
		/// </summary>
		internal virtual bool Contains(int val)
		{
			return FiniteValue.Contains(val);
		}

		/// <summary> Check if any of the given choices is an element of the current domain.
		/// </summary>
		internal bool TestIntersect(int[] newSet)
		{
			if (newSet != null)
			{
				foreach (int val in newSet)
				{
					if (Contains(val))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary> Check if any of the given choices is an element of the current domain.
		/// </summary>
		internal bool TestIntersect(int min, int max)
		{
			if (min <= max && First <= max)
			{
				return min <= Last;
			}
			return false;
		}

		/// <summary> Check if this Expression and the other Expression have identical value sets.
		/// </summary>
		internal bool SameAs(CspSolverTerm otherExp)
		{
			return FiniteValue.SetEqual(otherExp.FiniteValue);
		}

		/// <summary> Change to a new, tighter domain.  Return the new cardinality.
		/// </summary>
		internal int Restrain(CspSolverDomain newDomain)
		{
			_values.Add(newDomain);
			_solver.AddChange(this, _values.Count);
			return newDomain.Count;
		}

		/// <summary> Set the boolean to be true or false.  Return true if the CspSolverDomain changed.
		/// </summary>
		internal bool Force(bool choice, out CspSolverTerm conflict)
		{
			CspSolverDomain finiteValue = FiniteValue;
			if (2 < finiteValue.Count)
			{
				throw new InvalidOperationException(Resources.InvalidForceOperation);
			}
			if (2 == finiteValue.Count)
			{
				Restrain(choice ? ConstraintSystem.DTrue : ConstraintSystem.DFalse);
				conflict = null;
				return true;
			}
			if (1 == finiteValue.Count)
			{
				if (choice == (1 == finiteValue.First))
				{
					conflict = null;
					return false;
				}
				Restrain(ConstraintSystem.DEmpty);
				conflict = this;
				return true;
			}
			conflict = this;
			return false;
		}

		/// <summary> Intersect with the specified otherSet.  Return true if the domain changed.
		/// </summary>
		internal bool Intersect(out CspSolverTerm conflict, params int[] orderedUniqueSet)
		{
			conflict = null;
			if (FiniteValue.Intersect(out var newD, orderedUniqueSet))
			{
				if (Restrain(newD) == 0)
				{
					conflict = this;
				}
				return true;
			}
			return false;
		}

		/// <summary> Intersect with the specified interval.  Return true if the domain changed.
		/// </summary>
		internal bool Intersect(int min, int max, out CspSolverTerm conflict)
		{
			conflict = null;
			if (FiniteValue.Intersect(min, max, out var newD))
			{
				if (Restrain(newD) == 0)
				{
					conflict = this;
				}
				return true;
			}
			return false;
		}

		/// <summary> Intersect with the specified domain.  Return true if the domain changed.
		/// </summary>
		internal bool Intersect(CspSolverDomain otherD, out CspSolverTerm conflict)
		{
			conflict = null;
			if (FiniteValue.Intersect(otherD, out var newD))
			{
				if (Restrain(newD) == 0)
				{
					conflict = this;
				}
				return true;
			}
			return false;
		}

		/// <summary> Exclude the specified otherSet.  Return true if the domain changed.
		/// </summary>
		internal bool Exclude(out CspSolverTerm conflict, params int[] orderedUniqueSet)
		{
			conflict = null;
			if (FiniteValue.Exclude(out var newD, orderedUniqueSet))
			{
				if (Restrain(newD) == 0)
				{
					conflict = this;
				}
				return true;
			}
			return false;
		}

		/// <summary> Exclude the specified interval.  Return true if the domain changed.
		/// </summary>
		internal bool Exclude(int min, int max, out CspSolverTerm conflict)
		{
			conflict = null;
			if (FiniteValue.Exclude(min, max, out var newD))
			{
				if (Restrain(newD) == 0)
				{
					conflict = this;
				}
				return true;
			}
			return false;
		}

		/// <summary> Exclude the specified domain.  Return true if the domain changed.
		/// </summary>
		internal bool Exclude(CspSolverDomain otherD, out CspSolverTerm conflict)
		{
			conflict = null;
			if (FiniteValue.Exclude(otherD, out var newD))
			{
				if (Restrain(newD) == 0)
				{
					conflict = this;
				}
				return true;
			}
			return false;
		}

		/// <summary> Override this to clear any internal temporary propagation state.
		/// </summary>
		internal virtual void Reset()
		{
		}

		/// <summary> Revert to the state at the time of some earlier decision
		/// </summary>
		internal void Backtrack(int valueCount)
		{
			if (valueCount < _values.Count)
			{
				if (0 <= valueCount)
				{
					_values.RemoveRange(valueCount, _values.Count - valueCount);
				}
				_changeCount = 0;
				Reset();
			}
		}

		/// <summary> After a xDomain or a Decision (which is a singular xDomain)
		///             is assigned to a Variable, the consequences may be propagated.
		/// </summary>
		/// <param name="conflict"> Conflict Term if contradiction occurs. </param>
		internal abstract bool Propagate(out CspSolverTerm conflict);

		/// <summary> Utility function to create a new integer array subsetting an existing one
		/// </summary>
		internal static int[] SubArray(int[] inArray, int first, int newLength)
		{
			if (inArray == null || (first == 0 && inArray.Length <= newLength))
			{
				return inArray;
			}
			int[] array = new int[newLength];
			for (int i = 0; i < newLength; i++)
			{
				array[i] = inArray[first + i];
			}
			return array;
		}

		/// <summary> Utility function to create a new integer array subsetting an existing one
		/// </summary>
		internal static int[] SubArray(int[] inArray, int newLength)
		{
			return SubArray(inArray, 0, newLength);
		}

		/// <summary> Find an integer value in a sorted otherSet where the item must exist in the otherSet
		/// </summary>
		internal static int IndexOf(int item, int[] sortedSet)
		{
			int num = 0;
			int num2 = sortedSet.Length;
			while (num < num2)
			{
				int num3 = (num + num2) / 2;
				if (item == sortedSet[num3])
				{
					return num3;
				}
				if (item < sortedSet[num3])
				{
					num2 = num3;
				}
				else
				{
					num = num3 + 1;
				}
			}
			throw new InvalidOperationException(Resources.IndexError);
		}

		/// <summary>
		/// Return the actural value of this CspSolverTerm that is mapped to the input. 
		/// Based on the value type, the returned value could be an integer, a double, or a string.
		/// </summary>
		/// <param name="intval">The internal integer value</param>
		/// <returns>The external value that is mapped to the input integer value.</returns>
		internal object GetValue(int intval)
		{
			switch (Kind)
			{
			case CspDomain.ValueKind.Integer:
				return intval;
			case CspDomain.ValueKind.Decimal:
				return (double)intval / (double)OutputScale;
			case CspDomain.ValueKind.Symbol:
				return Symbols.GetSymbol(intval);
			default:
				throw new InvalidCastException(Resources.InvalidValueType);
			}
		}

		internal object GetValue()
		{
			return GetValue(FiniteValue.First);
		}

		/// <summary>
		/// Return the internal integer representation of the given value, based on its value type.
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		internal int GetInteger(object val)
		{
			switch (Kind)
			{
			case CspDomain.ValueKind.Integer:
				return (int)val;
			case CspDomain.ValueKind.Decimal:
				return (int)Math.Round((double)val * (double)OutputScale, 0);
			case CspDomain.ValueKind.Symbol:
				return Symbols.GetIntegerValue((string)val);
			default:
				throw new InvalidCastException(Resources.InvalidValueType);
			}
		}

		/// <summary> True if the value is within a safe range where it can
		///           be added/subtracted to another safe value without overflow
		/// </summary>
		/// <remarks> During the computations made in local search some values may
		///           occasionally out of this range. This will systematically be
		///           detected and cause a very strong increase in the penalty
		///           function, so solutions for which this happens will be ignored
		/// </remarks>
		internal static bool IsSafe(int val)
		{
			if (MinSafeNegativeValue <= val)
			{
				return val <= MaxSafePositiveValue;
			}
			return false;
		}

		internal static bool IsSafe(double val)
		{
			if ((double)MinSafeNegativeValue <= val)
			{
				return val <= (double)MaxSafePositiveValue;
			}
			return false;
		}

		/// <summary> Naive recomputation: Recompute the value of the term 
		///           from the value of all its inputs
		/// </summary>
		/// <remarks> Naive, non-incremental. Used, in particular, for initialization
		/// </remarks>
		/// <param name="ls">The local search algorithm in which we are working
		/// </param>
		internal abstract void RecomputeValue(LocalSearchSolver ls);

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		/// <remarks> Naive, non-incremental. Used, in particular, for initialization
		/// </remarks>
		/// <param name="ls">The local search algorithm in which we are working
		/// </param>
		internal abstract void RecomputeGradients(LocalSearchSolver ls);

		/// <summary> Incremental recomputation: update the value of the term 
		///           when one of its arguments is changed
		/// </summary>
		/// <remarks> Redefined only if something incremental can be done -
		///           otherwise the default behaviour is to naively recompute
		/// </remarks>
		///
		/// <param name="modifiedArg">A subterm that has been modified</param>
		/// <param name="oldValue">The value the subterm had last time we 
		///           recomputed this Term, in internal format
		/// </param>
		/// <param name="newValue">The currrent value of the modified subterm,
		///           in internal format
		/// </param>
		/// <param name="ls">The local search algorithm in which we are working
		/// </param>
		internal virtual void PropagateChange(LocalSearchSolver ls, CspSolverTerm modifiedArg, int oldValue, int newValue)
		{
			RecomputeValue(ls);
		}

		/// <summary> Incremental recomputation of the gradients: update the
		///           gradients when one of the arguments is changed
		/// </summary>
		/// <remarks> Redefined only if something incremental can be done -
		///           otherwise the default behaviour is RecomputeGradients
		/// </remarks>
		///
		/// <param name="modifiedArg">A subterm that has been modified</param>
		/// <param name="ls">The local search algorithm in which we are working
		/// </param>
		internal virtual void PropagateGradientChange(LocalSearchSolver ls, CspSolverTerm modifiedArg)
		{
			RecomputeGradients(ls);
		}

		/// <summary> Update the value of all dependents of the Term
		/// </summary>
		/// <param name="oldValue">the value of the Term before its last modification
		/// </param>
		/// <param name="newValue">the current value of the Term,
		///           in internal format
		/// </param>
		/// <param name="ls">
		/// The local search algorithm in which we are working
		/// </param>
		internal void DispatchValueChange(int oldValue, int newValue, LocalSearchSolver ls)
		{
			if (_dependents != null)
			{
				foreach (CspFunction dependent in _dependents)
				{
					dependent.PropagateChange(ls, this, oldValue, newValue);
				}
			}
			if (IsConstraint)
			{
				ls.PropagateChangeInViolation(this, oldValue, newValue);
			}
		}

		/// <summary> 
		/// Update the gradients of all dependents of the Term
		/// </summary>
		internal void DispatchGradientChange(LocalSearchSolver ls)
		{
			if (_dependents == null)
			{
				return;
			}
			foreach (CspFunction dependent in _dependents)
			{
				dependent.PropagateGradientChange(ls, this);
			}
		}

		/// <summary>
		/// Asserts that the value of this term has been freshly re-computed:
		/// if we re-run a forced Recomputation the value would be unchanged
		/// </summary>
		/// <remarks>
		/// Precondition of many gradient recomputations: they
		/// assume that the evaluation of the current value of the
		/// term has been called
		/// </remarks>
		[Conditional("DEBUG")]
		protected void AssertValueStable(LocalSearchSolver ls)
		{
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping,
		///           together with a suggestion of value for this subterm
		/// </summary>
		/// <param name="ls">The local search algorithm in which we are working </param>
		/// <param name="target">A value we would like to obtain for this Term,
		///           in external format
		/// </param>=
		/// <returns> A sub-term paired with a hint, or value that should be targetted
		///           for this subterm. The hint is in external format and should 
		///           belong to the domain of the term.
		/// </returns>
		internal abstract KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target);

		/// <summary> Suggests a flip that is likely to bring this term closer to the 
		///           target value
		/// </summary>
		/// <param name="ls">The local search algorithm in which we are working </param>
		/// <param name="target">A value we would like to obtain for this Term,
		///           in external representation
		/// </param>
		/// <returns> A variable paired with a hint, or value worth considering for 
		///           this variable. The hint is in internal format and should 
		///           belong to the domain of the variable.
		/// </returns>
		internal KeyValuePair<CspVariable, int> SelectFlipInternal(LocalSearchSolver ls, int target)
		{
			KeyValuePair<CspVariable, int> keyValuePair = SelectFlip(ls, target);
			return new KeyValuePair<CspVariable, int>(keyValuePair.Key, LocalSearchSolver.ToInternalRepresentation(keyValuePair.Key, keyValuePair.Value));
		}

		/// <summary> Suggests a flip that is likely to bring this term closer to the 
		///           target value
		/// </summary>
		/// <param name="ls">The local search algorithm in which we are working </param>
		/// <param name="target">A value we would like to obtain for this Term,
		///           in external representation
		/// </param>
		/// <returns> A variable paired with a hint, or value worth considering for 
		///           this variable. The hint is in external format and should 
		///           belong to the domain of the variable.
		/// </returns>
		internal KeyValuePair<CspVariable, int> SelectFlip(LocalSearchSolver ls, int target)
		{
			CspSolverTerm cspSolverTerm = this;
			CspVariable cspVariable = this as CspVariable;
			int num = target;
			while (cspVariable == null)
			{
				KeyValuePair<CspSolverTerm, int> keyValuePair = cspSolverTerm.SelectSubtermToFlip(ls, num);
				cspSolverTerm = keyValuePair.Key;
				num = keyValuePair.Value;
				cspVariable = cspSolverTerm as CspVariable;
			}
			if (!cspVariable.Contains(num))
			{
				num = cspVariable.FiniteValue.Pick(ls.RandomSource);
			}
			return new KeyValuePair<CspVariable, int>(cspVariable, num);
		}

		/// <summary> Given a Term, takes a non-checked hint for the term and 
		///           returns a correct suggestion, where the hint is checked
		///           and corrected if needed
		/// </summary>
		protected static KeyValuePair<CspSolverTerm, int> CreateFlipSuggestion(CspSolverTerm t, int val, Random prng)
		{
			CspSolverDomain baseValueSet = t.BaseValueSet;
			if (!baseValueSet.Contains(val))
			{
				val = baseValueSet.Pick(prng);
			}
			return new KeyValuePair<CspSolverTerm, int>(t, val);
		}
	}
}
