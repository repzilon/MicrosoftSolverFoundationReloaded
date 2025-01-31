using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>This is the middle layer between results in SFS decisions, goals and constraints to 
	/// the solver terms/vids.
	/// </summary>
	public abstract class SolutionMapping
	{
		private static readonly object[] _emptyArray = new object[0];

		private readonly Model _model;

		/// <summary>One dimensional Empty array. Use it whenever you need an empty array instead of allocating one. 
		/// </summary>
		protected static object[] EmptyArray => _emptyArray;

		/// <summary>Sfs Model which was solved
		/// </summary>
		protected Model Model => _model;

		/// <summary>Constructor for SolutionMapping class
		/// </summary>
		/// <param name="model"></param>
		protected SolutionMapping(Model model)
		{
			_model = model;
		}

		/// <summary>Gets a vid out of a decision
		/// </summary>
		/// <param name="decision">A decision</param>
		/// <param name="indexes">Indexes related to the decision</param>
		/// <param name="vid">vid related to the decision</param>
		/// <returns>true if the decision has mapping to a vid, false otherwise</returns>
		public abstract bool TryGetVid(Decision decision, object[] indexes, out int vid);

		/// <summary>Gets vid out of a Goal
		/// </summary>
		/// <param name="goal">A goal</param>
		/// <param name="vid">vid related to the goal</param>
		/// <returns>true if the goal has mapping to a vid, false otherwise</returns>
		public abstract bool TryGetVid(Goal goal, out int vid);

		/// <summary>Get vid out of constraint, indexes and part
		/// </summary>
		/// <remarks>First call to TryGetVid of constraint or GetIndexes ( of constraint) or GetComponents is expensive</remarks> 
		/// <param name="constraint">A constraint</param>
		/// <param name="indexes">Indexes related to the constraint</param>
		/// <param name="component">component of the constraint</param>
		/// <param name="vid">vid related to the constraint</param>
		/// <returns>true if constraint has mapping to a vid, false otherwise</returns>
		public abstract bool TryGetVid(Constraint constraint, object[] indexes, int component, out int vid);

		/// <summary>Gets a decision out of vid
		/// </summary>
		/// <param name="vid">A vid</param>
		/// <param name="decision">The decision mapped to the vid</param>
		/// <param name="indexes">Indexes related to the decision</param>
		/// <returns>true if the vid has mapping to a decision, false otherwise</returns>
		public abstract bool TryGetDecision(int vid, out Decision decision, out object[] indexes);

		/// <summary>Gets a constraint with indexes and component from vid
		/// </summary>
		/// <param name="vid">vid of a row</param>
		/// <param name="constraint">The constraint mapped to the vid</param>
		/// <param name="indexes">Indexes related to the constraint</param>
		/// <param name="component">component of the constraint.
		/// When single constraint has multiple part 
		/// (e.g. "5 &gt;= x &gt;= y &gt;= 9") there might be different row for each part. component is zero based.</param>
		/// <returns>true if the vid has mapping to a Constraint, false otherwise</returns>
		public abstract bool TryGetConstraint(int vid, out Constraint constraint, out object[] indexes, out int component);

		/// <summary>Gets goal out of a vid
		/// </summary>
		/// <param name="vid">A vid</param>
		/// <param name="goal">The goal mapped to the vid</param>
		/// <returns>true if the vid has mapping to a goal, false otherwise</returns>
		public abstract bool TryGetGoal(int vid, out Goal goal);

		/// <summary>Gets a value of a decision with specific indexes
		/// </summary>
		/// <param name="decision">A Decision</param>
		/// <param name="indexes">Indexes related to the Decision</param>
		/// <param name="value">The value related to the Decision </param>
		/// <returns>true if the decision has mapping to a vid, false otherwise</returns>
		/// <exception cref="T:System.ArgumentNullException">decision and indexes must not be null</exception>
		public bool TryGetValue(Decision decision, object[] indexes, out Rational value)
		{
			if ((object)decision == null)
			{
				throw new ArgumentNullException("decision");
			}
			if (indexes == null)
			{
				throw new ArgumentNullException("indexes");
			}
			value = Rational.Indeterminate;
			try
			{
				value = GetValue(decision, indexes);
				return true;
			}
			catch (MsfException)
			{
				return false;
			}
		}

		/// <summary>Gets a value of a decision with specific indexes
		/// </summary>
		/// <param name="goal">A Goal</param>
		/// <param name="value">The value related to the Goal </param>
		/// <returns>true if the goal has mapping to a vid, false otherwise</returns>
		/// <exception cref="T:System.ArgumentNullException">goal must not be null </exception>
		public bool TryGetValue(Goal goal, out Rational value)
		{
			if (goal == null)
			{
				throw new ArgumentNullException("goal");
			}
			value = Rational.Indeterminate;
			try
			{
				value = GetValue(goal, EmptyArray);
				return true;
			}
			catch (MsfException)
			{
				return false;
			}
		}

		/// <summary>Get all indexes of a decision
		/// </summary>
		/// <param name="decision">The decision</param>
		/// <returns>Enumaration of all indexes</returns>
		public abstract IEnumerable<object[]> GetIndexes(Decision decision);

		/// <summary>Get parts of constraint with its indexes
		/// </summary>
		/// <remarks>First call to TryGetVid of constraint or GetIndexes ( of constraint) or GetComponents is expensive</remarks>
		/// <param name="constraint">A Constraint</param>
		/// <param name="indexes">Indexes related to the constraint</param>
		/// <returns>A collection of parts related to the constraint</returns>
		public abstract IEnumerable<int> GetComponents(Constraint constraint, object[] indexes);

		/// <summary>Gets indexes of a constraint
		/// </summary>
		/// <remarks>First call to TryGetVid of constraint or GetIndexes ( of constraint) or GetComponents is expensive</remarks>
		/// <param name="constraint">A constraint</param>
		/// <returns>A collection of indexes related to the constraint</returns>
		public abstract IEnumerable<object[]> GetIndexes(Constraint constraint);

		internal void ExtractDecisionValues(Model model, Directive directive)
		{
			foreach (RecourseDecision allRecourseDecision in model.AllRecourseDecisions)
			{
				CalculateSecondStageResults(allRecourseDecision);
			}
			ExtractDecisionsWithAllIndexes(model.AllDecisions);
			int maximumGoalCount = directive.MaximumGoalCount;
			int num = 0;
			foreach (Goal item in model.AllGoals.OrderBy((Goal goal) => goal.Order))
			{
				if (maximumGoalCount != num)
				{
					num++;
					if (item.Enabled)
					{
						ExtractDecision(item, EmptyArray);
					}
					continue;
				}
				break;
			}
		}

		private void ExtractDecision(IVariable decision, object[] indexes)
		{
			Rational value = GetValue(decision, indexes);
			decision.SetValue(value, indexes);
		}

		private void ExtractDecisionsWithAllIndexes(IEnumerable<Decision> decisions)
		{
			foreach (Decision decision in decisions)
			{
				foreach (object[] index in GetIndexes(decision))
				{
					ExtractDecision(decision, index);
				}
			}
		}

		internal abstract void CalculateSecondStageResults(RecourseDecision recourseDecision);

		/// <summary>This is called from context when solving stochastic problem 
		/// </summary>
		internal abstract ValueTable<Rational> GetGoalCoefficient(Decision decision);

		/// <summary>This updates the second stage results when using decomposition
		/// Called at the end of every iteration for each scenario 
		/// </summary>
		/// <param name="model"></param>
		/// <param name="probability">probability of the scenario</param>
		internal abstract void UpdateSecondStageResults(Model model, double probability);

		/// <summary>Get the next solution
		/// </summary>
		/// <remarks>used internaly for CSP</remarks>
		internal abstract SolverQuality GetNext(Model model, Func<bool> newTimer);

		internal abstract bool IsGoalOptimal(int goalNumber);

		internal abstract Rational GetValue(IVariable decision, object[] indexes);

		internal virtual bool ShouldExtractDecisionValues(SolverQuality quality)
		{
			if (quality != 0)
			{
				return quality == SolverQuality.Feasible;
			}
			return true;
		}
	}
}
