using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A Variable with a Finite CspSolverDomain
	/// </summary>
	internal class CspVariable : CspSolverTerm, IVisitable
	{
		private CspDomain.ValueKind _kind;

		private int _scale;

		private CspSymbolDomain _symbols;

		public bool IsConstant => base.TermKind == TermKinds.Constant;

		/// <summary>
		/// Get the value kind of this variable
		/// </summary>
		public override CspDomain.ValueKind Kind => _kind;

		internal override int OutputScale => _scale;

		internal override CspSymbolDomain Symbols => _symbols;

		internal override CspSolverDomain BaseValueSet => _values[0];

		/// <summary> There are no inputs to a Variable.
		/// </summary>
		internal sealed override int Width => 0;

		internal override bool IsTrue
		{
			get
			{
				if (1 != base.Count)
				{
					throw new InvalidOperationException(Resources.InvalidIsTrueCall + ToString());
				}
				return 0 != First;
			}
		}

		/// <summary> Does this Variable have any influence on the model?
		/// </summary>
		internal sealed override bool Participates => 0 < base.Dependents.Count;

		/// <summary> There are no inputs to a Variable.
		/// </summary>
		internal sealed override CspSolverTerm[] Args => null;

		internal CspVariable(ConstraintSystem solver, CspComposite domain, object key)
			: base(solver, domain, 0)
		{
			if (key == null)
			{
				Key = this;
			}
			else
			{
				Key = key;
			}
		}

		/// <summary> A Variable with a Finite CspSolverDomain
		/// </summary>
		internal CspVariable(ConstraintSystem solver, CspSolverDomain domain, TermKinds kind, object key)
			: base(solver, domain, kind, 0)
		{
			if (key == null)
			{
				Key = this;
			}
			else
			{
				Key = key;
			}
			_kind = domain.Kind;
			_scale = domain.Scale;
			_symbols = domain as CspSymbolDomain;
			base.TermKind = kind;
			if (kind == TermKinds.DecisionVariable || kind == TermKinds.Constant)
			{
				solver.AddVariable(this);
			}
		}

		internal CspVariable(ConstraintSystem solver, CspSolverDomain domain, TermKinds kind)
			: this(solver, domain, kind, null)
		{
		}

		/// <summary> A variable that represents a symbol constant
		/// </summary>
		internal CspVariable(ConstraintSystem solver, CspSymbolDomain symbols, object key, int constant)
			: base(solver, (CspSolverDomain)solver.CreateIntegerInterval(constant, constant), TermKinds.Constant, 0)
		{
			Key = key;
			_symbols = symbols;
			_kind = symbols.Kind;
			_scale = symbols.Scale;
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		internal sealed override bool Propagate(out CspSolverTerm conflict)
		{
			conflict = null;
			return true;
		}

		/// <summary> String representation of this variable
		/// </summary>
		public override string ToString()
		{
			string text = ((Key == this) ? "var" : Key.ToString());
			return string.Format(CultureInfo.InvariantCulture, "{0}({1}): Finite{{{2}}}", new object[3]
			{
				text,
				base.Ordinal,
				base.FiniteValue.AppendTo("", 9, this)
			});
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			int num = 0;
			int num2 = 0;
			if (!ls.IsFiltered(this))
			{
				int num3 = ls[this];
				CspSolverDomain baseValueSet = BaseValueSet;
				num = baseValueSet.First;
				num2 = baseValueSet.Last;
				num -= num3;
				num2 -= num3;
			}
			ls.SetGradients(this, num, (num == 0) ? null : this, num2, (num2 == 0) ? null : this);
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping
		/// </summary>
		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			return CspSolverTerm.CreateFlipSuggestion(this, target, ls.RandomSource);
		}
	}
}
