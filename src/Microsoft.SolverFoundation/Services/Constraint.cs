using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> A Constraint encapsulates a term and its role as a constraint in the model.
	/// </summary>
	[DebuggerDisplay("Constraint({_name}: {_term}")]
	public sealed class Constraint
	{
		/// <summary>
		/// The name of this constraint. Must be unique.
		/// </summary>
		internal string _name;

		internal string _expression;

		internal SolverContext _context;

		internal Term _term;

		internal List<SubmodelInstance> _path;

		private bool _enabled = true;

		internal Term Term
		{
			[DebuggerStepThrough]
			get
			{
				return _term;
			}
		}

		/// <summary>
		/// The name of the constraint.
		/// </summary>
		public string Name => _name;

		/// <summary>
		/// The expression of the constraint.
		/// </summary>
		public string Expression
		{
			get
			{
				if (_expression == null)
				{
					SolveRewriteSystem rs = new SolveRewriteSystem();
					OmlWriter omlWriter = new OmlWriter(rs, _context);
					Expression expression = omlWriter.Translate(_term, TermValueClass.Numeric);
					_expression = expression.ToString(new OmlFormatter(rs));
				}
				return _expression;
			}
		}

		/// <summary>
		/// A comment.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Whether to enable the constraint.
		/// </summary>
		public bool Enabled
		{
			get
			{
				return _enabled;
			}
			set
			{
				_enabled = value;
			}
		}

		/// <summary>
		/// Constructs a new constraint.
		/// </summary>
		/// <param name="name">The constraint's name. Must be unique.</param>
		///
		/// <param name="term">The term tree the constraint encapsulates.</param>
		/// <param name="context">Solver context</param>
		internal Constraint(SolverContext context, string name, Term term)
		{
			_context = context;
			_name = name;
			_term = term;
		}

		internal Constraint(SolverContext context, string name, Term term, string expression)
		{
			_context = context;
			_name = name;
			_term = term;
			_expression = expression;
		}

		private Constraint(string baseName, SubmodelInstance instance, Constraint source)
		{
			_context = source._context;
			_name = Term.BuildFullName(baseName, source._name);
			_term = source._term;
			_expression = source._expression;
			if (source._path == null)
			{
				_path = new List<SubmodelInstance>();
			}
			else
			{
				_path = new List<SubmodelInstance>(source._path);
			}
			_path.Add(instance);
		}

		internal Constraint Clone(string baseName, SubmodelInstance instance)
		{
			return new Constraint(baseName, instance, this);
		}

		/// <summary>
		/// Check if the constraints is second stage
		/// </summary>
		/// <returns>true iff (Recourse decision participates or random parameter participates)</returns>
		internal bool IsSecondStage()
		{
			return _term.Visit<bool, byte>(new StochasticConstraintVisitor(), 0);
		}
	}
}
