using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Class used to implement simple pattern matching based
	///   on the type of the input. Using it we can specify which
	///   transformation (a delegate of signature T -&gt; output)
	///   is applied to objects of each and every type
	/// </summary>
	internal class TypeMatching<Output>
	{
		/// <summary>
		///   Dictionary storing the actions associated
		///   with each type
		/// </summary>
		private Dictionary<Type, Func<object, Output>> _switch;

		/// <summary>
		///   action to apply by default, i.e. if the type 
		///   switch is applied to a type for which there is no match
		/// </summary>
		private readonly Func<object, Output> _default;

		/// <summary>
		///   Syntactical sugar for Apply;
		///   applies the switch to the object
		/// </summary>
		public Output this[object t] => Apply(t);

		/// <param name="defaultAction">
		///   default handler, called if the switch is applied 
		///   to an object for which there is no dedicated handler
		/// </param>
		public TypeMatching(Func<object, Output> defaultAction)
		{
			_switch = new Dictionary<Type, Func<object, Output>>();
			_default = defaultAction;
		}

		/// <param name="defaultOutput">v
		///   default value, returned if the switch is applied 
		///   to an object for which there is no dedicated handler
		/// </param>
		public TypeMatching(Output defaultOutput)
			: this((Func<object, Output>)((object whatever) => defaultOutput))
		{
		}

		/// <param name="exceptionWhenNoMatch">
		///   exception that will be thrown if the switch is applied
		///   to an object for which there is dedicated handler
		/// </param>
		public TypeMatching(Exception exceptionWhenNoMatch)
			: this((Func<object, Output>)delegate
			{
				throw exceptionWhenNoMatch;
			})
		{
		}

		/// <summary>
		///   Specifies what action is applied to translate a term of the
		///   given concrete type T. The action has to be a delegate of
		///   taking an argument of type T and returning the correct type
		/// </summary>
		public void Match<InputSubType>(Func<InputSubType, Output> f) where InputSubType : class
		{
			_switch.Add(typeof(InputSubType), (object t) => f(t as InputSubType));
		}

		/// <summary>
		///   Applies the switch to an object 
		/// </summary>
		public Output Apply(object t)
		{
			if (_switch.TryGetValue(t.GetType(), out var value))
			{
				return value(t);
			}
			return _default(t);
		}
	}
}
