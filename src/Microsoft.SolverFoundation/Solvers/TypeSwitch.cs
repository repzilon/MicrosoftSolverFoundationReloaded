using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class TypeSwitch
	{
		/// <summary>
		///   Dictionary storing the actions associated
		///   with each type
		/// </summary>
		private Dictionary<Type, Action<object>> _switch;

		/// <summary>
		///   action to apply by default, i.e. if the type 
		///   switch is applied to a type for which there is no match
		/// </summary>
		private readonly Action<object> _default;

		/// <param name="defaultAction">
		///   default handler, called if the switch is applied 
		///   to an object for which there is no dedicated handler
		/// </param>
		public TypeSwitch(Action<object> defaultAction)
		{
			_switch = new Dictionary<Type, Action<object>>();
			_default = defaultAction;
		}

		/// <param name="exceptionWhenNoMatch">
		///   exception that will be thrown if the switch is applied
		///   to an object for which there is dedicated handler
		/// </param>
		public TypeSwitch(Exception exceptionWhenNoMatch)
			: this(delegate
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
		public void Match<InputSubType>(Action<InputSubType> f) where InputSubType : class
		{
			_switch.Add(typeof(InputSubType), delegate(object t)
			{
				f(t as InputSubType);
			});
		}

		/// <summary>
		///   Applies the switch. 
		/// </summary>
		public void Apply(object t)
		{
			if (_switch.TryGetValue(t.GetType(), out var value))
			{
				value(t);
			}
			else
			{
				_default(t);
			}
		}
	}
}
