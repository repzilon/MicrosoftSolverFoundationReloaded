using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> A Goal encapsulates a term and its role as a goal in the model.
	/// </summary>
	public sealed class Goal : IVariable
	{
		internal GoalKind _direction;

		internal string _name;

		private bool _enabled = true;

		private SolverContext _context;

		internal int _id;

		internal Term _term;

		internal List<SubmodelInstance> _path;

		/// <summary>
		/// The current value of the goal. Unspecified if the model has not been solved.
		/// Because the model may be transformed prior to solving, we need to be careful that the goal gets the right value.
		/// </summary>
		private Rational _value;

		internal string _expression;

		/// <summary>
		/// Direction of goal (minimization or maximization)
		/// </summary>
		public GoalKind Kind => _direction;

		/// <summary>
		/// The name of the goal.
		/// </summary>
		public string Name
		{
			[DebuggerStepThrough]
			get
			{
				return _name;
			}
		}

		/// <summary>The goal expression as a System.String.
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
		/// Whether to enable the goal.
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
		/// The order of the goal (lower numbers mean higher priority).
		/// </summary>
		public int Order { get; set; }

		/// <summary>
		/// GoalKind.Minimize or GoalKind.Maximize
		/// </summary>
		internal GoalKind Direction
		{
			[DebuggerStepThrough]
			get
			{
				return _direction;
			}
		}

		internal Term Term
		{
			[DebuggerStepThrough]
			get
			{
				return _term;
			}
		}

		/// <summary>
		/// Construct a new goal object
		/// </summary>
		/// <param name="name">The name of the goal. Must be unique within a model.</param>
		/// <param name="direction">GoalKind.Minimize or GoalKind.Maximize</param>
		///
		/// <param name="term">The term encapsulated by the goal</param>
		/// <param name="context">Solver context</param>
		internal Goal(SolverContext context, string name, GoalKind direction, Term term)
		{
			_context = context;
			_name = name;
			_direction = direction;
			_term = term;
			_value = ((direction == GoalKind.Maximize) ? double.NegativeInfinity : double.PositiveInfinity);
		}

		internal Goal(SolverContext context, string name, GoalKind direction, Term term, string expression)
		{
			_context = context;
			_name = name;
			_direction = direction;
			_term = term;
			_expression = expression;
			_value = ((direction == GoalKind.Maximize) ? double.NegativeInfinity : double.PositiveInfinity);
		}

		private Goal(string baseName, SubmodelInstance instance, Goal source)
		{
			_context = source._context;
			_name = Term.BuildFullName(baseName, source._name);
			_direction = source._direction;
			_term = source._term;
			if (source._path == null)
			{
				_path = new List<SubmodelInstance>();
			}
			else
			{
				_path = new List<SubmodelInstance>(source._path);
			}
			_path.Add(instance);
			_value = source._value;
		}

		/// <summary>
		/// Save the value of the goal. The value given should be the value as specified by the goal's expression.
		/// </summary>
		/// <param name="value">The value</param>
		/// <param name="indexes">An empty array</param>
		void IVariable.SetValue(Rational value, object[] indexes)
		{
			_value = value;
		}

		/// <summary>
		/// Value of the goal as an integer
		/// </summary>
		public int ToInt32()
		{
			if ((int)_value != _value)
			{
				throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture, Resources.CannotRepresentAsAnInteger, new object[1] { _value }));
			}
			return (int)_value;
		}

		/// <summary>
		/// Value of the goal as a double
		/// </summary>
		public double ToDouble()
		{
			return (double)_value;
		}

		internal Goal Clone(string baseName, SubmodelInstance instance)
		{
			return new Goal(baseName, instance, this);
		}

		/// <summary>
		/// Gets a string representation of the value of this goal.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return _value.ToString();
		}

		internal bool IsValidStochastic()
		{
			StochasticGoalComponents stochasticGoalComponents = _term.Visit<StochasticGoalComponents, byte>(new StochasticGoalVisitor(), 0);
			return (stochasticGoalComponents & StochasticGoalComponents.RandomParameterTimesDecision) != StochasticGoalComponents.RandomParameterTimesDecision;
		}
	}
}
