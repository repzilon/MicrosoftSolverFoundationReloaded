using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A numerical (real-valued) expression whose value can be re-evaluated
	///   in a lazy (i.e. incremental) way on demand by calling the lazy
	///   evaluator it connects to.
	///   Delegates can also subscribe to the exspression in order to be
	///   notified every time its value changes.
	/// </summary>
	internal abstract class LazyExpression
	{
		/// <summary>
		///   Delegates for the methods that need to be called when the
		///   value of a lazy expression is changed.
		/// </summary>
		/// <param name="arg">expression that has changed</param>
		/// <param name="change">
		///   difference between the current value of the expression
		///   and its value when the last re-evaluation took place.
		/// </param>
		public delegate void Listener(LazyExpression arg, double change);

		private double _value;

		private bool _enqueued;

		private readonly int _depth;

		private readonly LazyEvaluator _evaluator;

		protected static List<LazyExpression> _emptyList = new List<LazyExpression>();

		/// <summary>
		///   Get the value of the expression. 
		///   Derived classes can also set this value, in which case the change
		///   is signaled to the evaluator.
		/// </summary>
		public double Value
		{
			get
			{
				return _value;
			}
			protected set
			{
				if (value != _value)
				{
					if (!_enqueued)
					{
						_evaluator.Reschedule(this, _value);
						_enqueued = true;
					}
					_value = value;
				}
			}
		}

		/// <summary>
		///   Depth of the expression (atoms have depth 0, an expression's depth
		///   is otherwise one unit more than its deepest child
		/// </summary>
		public int Depth => _depth;

		/// <summary>
		///   True if the function is scheduled for re-evaluation
		/// </summary>
		public bool IsScheduled => _enqueued;

		private event Listener _event;

		/// <summary>
		///   Construction of a Lazy Expression
		/// </summary>
		/// <param name="s">evaluator the expression depends on</param>
		/// <param name="initialValue">initial value</param>
		/// <param name="args">sub-expressions of the expression</param>
		protected LazyExpression(LazyEvaluator s, double initialValue, IEnumerable<LazyExpression> args)
		{
			_depth = ComputeMaxDepth(s, args) + 1;
			_evaluator = s;
			_value = initialValue;
			s.Register(this);
		}

		/// <summary>
		///   Signals to the expression that a delegate should be called
		///   whenever its value is modified.
		/// </summary>
		public void Subscribe(Listener l)
		{
			_event += l;
		}

		/// <summary>
		///   Notifies all listeners that have subscribed to the expression
		///   that its value is modified.
		/// </summary>
		internal void DispatchChange(double change)
		{
			_enqueued = false;
			if (this._event != null && change != 0.0)
			{
				this._event(this, change);
			}
		}

		/// <summary>
		///   get the evaluator to which the expression is registered
		/// </summary>
		protected internal LazyEvaluator GetEvaluator()
		{
			return _evaluator;
		}

		/// <summary>
		///   Initialization method (also checks a few pre-conditions)
		/// </summary>
		private static int ComputeMaxDepth(LazyEvaluator s, IEnumerable<LazyExpression> args)
		{
			int num = -1;
			if (args != null)
			{
				foreach (LazyExpression arg in args)
				{
					num = Math.Max(num, arg._depth);
				}
			}
			return num;
		}

		/// <summary>
		///   returns a lazy expression representing the sum
		///   of two lazy expressions
		/// </summary>
		public static LazyExpression operator +(LazyExpression e1, LazyExpression e2)
		{
			return e1.GetEvaluator().Sum(e1, e2);
		}

		/// <summary>
		///   returns a lazy expression representing the sum
		///   of a lazy expression and a constant
		/// </summary>
		public static LazyExpression operator +(LazyExpression e, double cst)
		{
			return e.GetEvaluator().Sum(e, cst);
		}

		/// <summary>
		///   returns a lazy expression representing the sum
		///   of a lazy expression and a constant
		/// </summary>
		public static LazyExpression operator +(double cst, LazyExpression e)
		{
			return e.GetEvaluator().Sum(e, cst);
		}

		/// <summary>
		///   returns a lazy expression representing the difference
		///   between two lazy expressions
		/// </summary>
		public static LazyExpression operator -(LazyExpression e1, LazyExpression e2)
		{
			return e1.GetEvaluator().Minus(e1, e2);
		}

		/// <summary>
		///   returns a lazy expression representing the difference
		///   between a lazy expression and a constant
		/// </summary>
		public static LazyExpression operator -(LazyExpression e1, double e2)
		{
			return e1.GetEvaluator().Sum(e1, 0.0 - e2);
		}

		/// <summary>
		///   returns a lazy expression representing the opposite
		///   of a lazy expression (i.e. -x)
		/// </summary>
		public static LazyExpression operator -(LazyExpression e)
		{
			return e.GetEvaluator().Minus(e);
		}

		/// <summary>
		///   returns a lazy expression representing the product
		///   of two lazy expressions
		/// </summary>
		public static LazyExpression operator *(LazyExpression e1, LazyExpression e2)
		{
			return e1.GetEvaluator().Product(e1, e2);
		}

		/// <summary>
		///   returns a lazy expression representing the product
		///   of a lazy expression and a constant
		/// </summary>
		public static LazyExpression operator *(LazyExpression e1, double e2)
		{
			return e1.GetEvaluator().Product(e1, e2);
		}

		/// <summary>
		///   returns a lazy expression representing the product
		///   of a lazy expression and a constant
		/// </summary>
		public static LazyExpression operator *(double e2, LazyExpression e1)
		{
			return e1.GetEvaluator().Product(e1, e2);
		}

		/// <summary>
		///   returns a lazy expression representing the division
		///   of a lazy expression by another LazyExpression
		/// </summary>
		public static LazyExpression operator /(LazyExpression e1, LazyExpression e2)
		{
			return e1.GetEvaluator().Division(e1, e2);
		}

		/// <summary>
		///   returns a lazy expression representing the division
		///   of a lazy expression by another LazyExpression
		/// </summary>
		public static LazyExpression operator /(LazyExpression e1, double e2)
		{
			return e1.GetEvaluator().Division(e1, e2);
		}
	}
}
