using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Main class for lazy evaluation. This is used when we need numerical
	///   functions (typically sums) of a large number of inputs, and that need
	///   to be called very often. Instead of recomputing the function everytime
	///   we just propagate the things that were modified, allowing to obtain the
	///   updated result in sub-linear time. This is particularly good when the
	///   expression includes a large sum (updates are in constant time); for
	///   other constructs large like min/max the update takes logarithmic time.
	/// </summary>
	/// <remarks>
	///   We use a factory design for the generation of lazy expressions, which 
	///   is more natural. Another advantage is that we should (but have not yet)
	///   cache terms that are created twice, allowing a more efficient
	///   representation of the expression.
	/// </remarks>
	internal class LazyEvaluator
	{
		/// <summary>
		///   A commutative and associative binary operation
		/// </summary>
		private delegate LazyExpression CommutativeAssociativeOperation(LazyExpression e1, LazyExpression e2);

		/// <summary>
		///   When we enqueue a Lazy function we also record the value
		///   it has before it was modified; so the queue contains pairs
		///   function / old value.
		/// </summary>
		private struct Info
		{
			public LazyExpression fun;

			public double oldValue;

			public Info(LazyExpression f, double v)
			{
				fun = f;
				oldValue = v;
			}
		}

		private PriorityQueue<Info> _queue;

		private List<LazyExpression> _registeredFunctions;

		private Dictionary<double, LazyValue> _createdConstants;

		/// <summary>
		///   New scheduler; 
		///   Schedulable events must then be registered to it
		/// </summary>
		public LazyEvaluator()
		{
			_queue = new PriorityQueue<Info>(3);
			_registeredFunctions = new List<LazyExpression>();
			_createdConstants = new Dictionary<double, LazyValue>();
		}

		/// <summary>
		///   Recomputes the value of all lazy functions registered to the
		///   evaluator.
		/// </summary>
		public void Recompute()
		{
			while (!_queue.IsEmpty())
			{
				Info info = _queue.Dequeue();
				LazyExpression fun = info.fun;
				double change = fun.Value - info.oldValue;
				fun.DispatchChange(change);
			}
		}

		/// <summary>
		///   Returns a lazy expression that has the specified initial value.
		///   This value can be modified by accessing its Value property.
		/// </summary>
		public LazyValue Atom(double val)
		{
			return new LazyValue(this, val);
		}

		/// <summary>
		///   Returns a lazy expression that has the specified initial value.
		///   The expression is not meant to be modified
		/// </summary>
		public LazyExpression Constant(double val)
		{
			if (!_createdConstants.TryGetValue(val, out var value))
			{
				value = new LazyValue(this, val);
				_createdConstants.Add(val, value);
			}
			return value;
		}

		/// <summary>
		///   returns a lazy expression representing the sum
		///   of two lazy expressions
		/// </summary>
		public LazyExpression Sum(LazyExpression e1, LazyExpression e2)
		{
			List<LazyExpression> list = new List<LazyExpression>();
			list.Add(e1);
			list.Add(e2);
			return new LazySum(this, list);
		}

		/// <summary>
		///   returns a lazy expression representing the sum
		///   of a lazy expression and a constant
		/// </summary>
		public LazyExpression Sum(LazyExpression e1, double cst)
		{
			return Sum(e1, Constant(cst));
		}

		/// <summary>
		///   returns a lazy expression representing the difference
		///   between two lazy expressions
		/// </summary>
		public LazyExpression Minus(LazyExpression e1, LazyExpression e2)
		{
			return new LazyMinus(this, e1, e2);
		}

		/// <summary>
		///   returns a lazy expression representing the opposite
		///   of a lazy expression
		/// </summary>
		public LazyExpression Minus(LazyExpression e)
		{
			return new LazyOpposite(this, e);
		}

		/// <summary>
		///   returns a lazy expression representing the product
		///   of two lazy expressions
		/// </summary>
		public LazyExpression Product(LazyExpression e1, LazyExpression e2)
		{
			return new LazyProduct(this, e1, e2);
		}

		/// <summary>
		///   returns a lazy expression representing the product
		///   of a lazy expression and a constant
		/// </summary>
		public LazyExpression Product(LazyExpression e1, double cst)
		{
			return new LazyProductByConstant(this, e1, cst);
		}

		/// <summary>
		///   returns a lazy expression representing the quotient
		///   of two lazy expressions
		/// </summary>
		public LazyExpression Division(LazyExpression e1, LazyExpression e2)
		{
			return new LazyDivision(this, e1, e2);
		}

		/// <summary>
		///   returns a lazy expression representing the quotient
		///   of a lazy expression and a constant
		/// </summary>
		public LazyExpression Division(LazyExpression e1, double cst)
		{
			return new LazyDivisionByConstant(this, e1, cst);
		}

		/// <summary>
		///   returns a lazy expression representing the (natural,
		///   base E) logarithm of a lazy expression
		/// </summary>
		public LazyExpression Log(LazyExpression e)
		{
			return new LazyLog(this, e);
		}

		/// <summary>
		///   returns a lazy expression representing the (natural,
		///   base E) exponent of a lazy expression
		/// </summary>
		public LazyExpression Exp(LazyExpression e)
		{
			return new LazyExp(this, e);
		}

		/// <summary>
		///   returns a lazy expression representing the square
		///   of a lazy expression
		/// </summary>
		public LazyExpression Square(LazyExpression e)
		{
			return new LazySquare(this, e);
		}

		/// <summary>
		///   returns a lazy expression representing the square root
		///   of a lazy expression
		/// </summary>
		public LazyExpression Sqrt(LazyExpression e)
		{
			return new LazyRoot(this, e);
		}

		/// <summary>
		///   returns a lazy expression representing the inverse
		///   of a lazy expression x (i.e. 1/x)
		/// </summary>
		public LazyExpression Inverse(LazyExpression e)
		{
			return new LazyInverse(this, e);
		}

		/// <summary>
		///   returns a lazy expression representing the max
		///   of two lazy expressions
		/// </summary>
		public LazyExpression Min(LazyExpression e1, LazyExpression e2)
		{
			return new LazyMin(this, e1, e2);
		}

		/// <summary>
		///   returns a lazy expression representing the max
		///   of two lazy expressions
		/// </summary>
		public LazyExpression Max(LazyExpression e1, LazyExpression e2)
		{
			return new LazyMax(this, e1, e2);
		}

		/// <summary>
		///   Returns a lazy expression representing the max of a list of lazy
		///   expressions (the expression will be balanced for optimal evaluation)
		/// </summary>
		public LazyExpression Max(IEnumerable<LazyExpression> args)
		{
			List<LazyExpression> list = new List<LazyExpression>();
			foreach (LazyExpression arg in args)
			{
				list.Add(arg);
			}
			return ConstructBalancedExpression(Max, list);
		}

		/// <summary>
		///   Returns a lazy expression representing the min of a list of lazy
		///   expressions (the expression will be balanced for optimal evaluation)
		/// </summary>
		public LazyExpression Min(IEnumerable<LazyExpression> args)
		{
			List<LazyExpression> list = new List<LazyExpression>();
			foreach (LazyExpression arg in args)
			{
				list.Add(arg);
			}
			return ConstructBalancedExpression(Min, list);
		}

		/// <summary>
		///   Returns a lazy expression representing the sum of a list
		///   of lazy expressions
		/// </summary>
		public LazyExpression Sum(IEnumerable<LazyExpression> args)
		{
			List<LazyExpression> list = new List<LazyExpression>();
			foreach (LazyExpression arg in args)
			{
				list.Add(arg);
			}
			return new LazySum(this, list);
		}

		/// <summary>
		///   Returns a lazy expression representing the average of a list
		///   of lazy expressions
		/// </summary>
		public LazyExpression Average(IEnumerable<LazyExpression> args)
		{
			List<LazyExpression> list = new List<LazyExpression>();
			foreach (LazyExpression arg in args)
			{
				list.Add(arg);
			}
			LazyExpression lazyExpression = new LazySum(this, list);
			return lazyExpression / list.Count;
		}

		/// <summary>
		///   True if queue empty, i.e. nothing is scheduled.
		/// </summary>
		public bool IsEmpty()
		{
			return _queue.IsEmpty();
		}

		/// <summary>
		///   Signals to the scheduler that a new function depends upon it.
		/// </summary>
		internal void Register(LazyExpression f)
		{
			_registeredFunctions.Add(f);
			if (f.Depth >= _queue.NumberOfPriorities)
			{
				_queue.NumberOfPriorities = f.Depth + 1;
			}
		}

		/// <summary>
		///   Signals to the scheduler that part of a function is modified 
		///   and will require re-computation
		/// </summary>
		public void Reschedule(LazyExpression f, double oldValue)
		{
			_queue.Enqueue(new Info(f, oldValue), f.Depth);
		}

		/// <summary>
		///   Creates an expression that is binary balanced. Allows 
		///   lazy re-evaluation to take place in log time when the 
		///   function is well-chosen.
		/// </summary>
		/// <param name="op">the binary operator</param>
		/// <param name="l">the argument list</param>
		private static LazyExpression ConstructBalancedExpression(CommutativeAssociativeOperation op, List<LazyExpression> l)
		{
			return ConstructBalancedExpression(op, l, 0, l.Count - 1);
		}

		private static LazyExpression ConstructBalancedExpression(CommutativeAssociativeOperation op, List<LazyExpression> l, int begin, int end)
		{
			if (begin == end)
			{
				return l[begin];
			}
			int num = begin + (end - begin) / 2;
			int begin2 = num + 1;
			return op(ConstructBalancedExpression(op, l, begin, num), ConstructBalancedExpression(op, l, begin2, end));
		}
	}
}
