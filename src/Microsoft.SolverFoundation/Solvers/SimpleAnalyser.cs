using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A simple type of analyser for Disolver Terms.
	///   Does top-down syntax-driven analyses
	/// </summary>
	/// <remarks>
	///   Propagates terms that are true because constraints are stated
	///   as Boolean expressions "forced" to true
	///   Also, collects the terms that would potentially benefit from a sparse
	///   (bit-vector) representation. These are the terms that are touched by
	///   a non-convex constraint (i.e. difference, table).
	/// </remarks>
	internal class SimpleAnalyser
	{
		private delegate bool Action(DisolverTerm t);

		private Stack<DisolverTerm> _stack;

		private Dictionary<Type, Action> _switch;

		/// <summary>
		///   Analyses the constraints of the problem, setting some Boolean
		///   Terms to true.
		/// </summary>
		/// <param name="trueterms">
		///   The constraints of the problem; 
		///   i.e. the terms that are forced to true
		/// </param>
		public static void Apply(IEnumerable<DisolverTerm> trueterms)
		{
			SimpleAnalyser simpleAnalyser = new SimpleAnalyser(trueterms);
			simpleAnalyser.Run();
		}

		private SimpleAnalyser(IEnumerable<DisolverTerm> trueterms)
		{
			_stack = new Stack<DisolverTerm>();
			foreach (DisolverBooleanTerm trueterm in trueterms)
			{
				trueterm.SetInitialValue(b: true);
				_stack.Push(trueterm);
			}
			_switch = new Dictionary<Type, Action>();
			_switch.Add(typeof(DisolverNot), (DisolverTerm t) => ProcessNot(t as DisolverNot));
			_switch.Add(typeof(DisolverAnd), (DisolverTerm t) => ProcessAnd(t as DisolverAnd));
			_switch.Add(typeof(DisolverOr), (DisolverTerm t) => ProcessOr(t as DisolverOr));
		}

		/// <summary>
		///   Performs the top-down analysis
		/// </summary>
		private void Run()
		{
			while (_stack.Count != 0)
			{
				DisolverTerm disolverTerm = _stack.Pop();
				if (_switch.TryGetValue(disolverTerm.GetType(), out var value) && value(disolverTerm))
				{
					DisolverTerm[] subTerms = disolverTerm.SubTerms;
					foreach (CspTerm cspTerm in subTerms)
					{
						DisolverTerm item = cspTerm as DisolverTerm;
						_stack.Push(item);
					}
				}
			}
		}

		/// <summary>
		///   For conjunctions we propagate truth downwards
		/// </summary>
		private static bool ProcessAnd(DisolverAnd t)
		{
			if (t.InitialStatus == BooleanVariableState.True)
			{
				DisolverTerm[] subTerms = t.SubTerms;
				for (int i = 0; i < subTerms.Length; i++)
				{
					DisolverBooleanTerm disolverBooleanTerm = (DisolverBooleanTerm)subTerms[i];
					disolverBooleanTerm.SetInitialValue(b: true);
				}
			}
			return true;
		}

		/// <summary>
		///   For disjunction we propagate falsity downwards
		/// </summary>
		private static bool ProcessOr(DisolverOr t)
		{
			if (t.InitialStatus == BooleanVariableState.False)
			{
				DisolverTerm[] subTerms = t.SubTerms;
				for (int i = 0; i < subTerms.Length; i++)
				{
					DisolverBooleanTerm disolverBooleanTerm = (DisolverBooleanTerm)subTerms[i];
					disolverBooleanTerm.SetInitialValue(b: false);
				}
			}
			return true;
		}

		/// <summary>
		///   For negation we propagate downwards the truth value, reverted.
		/// </summary>
		private static bool ProcessNot(DisolverNot t)
		{
			switch (t.InitialStatus)
			{
			case BooleanVariableState.False:
				t.Subterm.SetInitialValue(b: true);
				break;
			case BooleanVariableState.True:
				t.Subterm.SetInitialValue(b: false);
				break;
			}
			return true;
		}
	}
}
