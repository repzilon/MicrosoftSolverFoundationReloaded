using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A class for mapping between Sfs model and a ConstraintSystem
	/// </summary>
	public sealed class CspSolutionMapping : SolutionMapping
	{
		private readonly ConstraintSystem _solver;

		private readonly ConstraintSolverSolution _solution;

		private Dictionary<IVariable, ValueTable<int>> _keyFromDecisionAndIndex;

		private Dictionary<IVariable, int> _keyFromDecisionWithoutIndex;

		internal CspSolutionMapping(Model model, ConstraintSystem solver, ConstraintSolverSolution solution, Dictionary<IVariable, ValueTable<int>> keyFromDecisionAndIndex, Dictionary<IVariable, int> keyFromDecisionWithoutIndex)
			: base(model)
		{
			_keyFromDecisionAndIndex = keyFromDecisionAndIndex;
			_keyFromDecisionWithoutIndex = keyFromDecisionWithoutIndex;
			_solver = solver;
			_solution = solution;
		}

		/// <summary>Gets a vid out of a decision
		/// </summary>
		/// <param name="decision">A decision</param>
		/// <param name="indexes">Indexes related to the decision</param>
		/// <param name="vid">vid related to the decision</param>
		/// <returns>true if the decision has mapping to a vid, false otherwise</returns>
		/// <exception cref="T:System.ArgumentNullException">decision and indexes must not be null</exception>
		public override bool TryGetVid(Decision decision, object[] indexes, out int vid)
		{
			if ((object)decision == null)
			{
				throw new ArgumentNullException("decision");
			}
			if (indexes == null)
			{
				throw new ArgumentNullException("indexes");
			}
			vid = KeyForVariable(decision, indexes);
			return true;
		}

		/// <summary>Get vid out of constraint, indexes and part
		/// </summary>
		/// <remarks>First call to TryGetVid of constraint or GetIndexes ( of constraint) or GetComponents is expensive</remarks> 
		/// <param name="constraint">A constraint</param>
		/// <param name="indexes">Indexes related to the constraint</param>
		/// <param name="component">component of the constraint</param>
		/// <param name="vid">vid related to the constraint</param>
		/// <returns>true if constraint has mapping to a vid, false otherwise</returns>
		public override bool TryGetVid(Constraint constraint, object[] indexes, int component, out int vid)
		{
			throw new NotSupportedException();
		}

		/// <summary>Gets vid out of a Goal
		/// </summary>
		/// <param name="goal">A goal</param>
		/// <param name="vid">vid related to the goal</param>
		/// <returns>true if the goal has mapping to a vid, false otherwise</returns>
		public override bool TryGetVid(Goal goal, out int vid)
		{
			throw new NotSupportedException();
		}

		/// <summary>Gets a decision out of vid
		/// </summary>
		/// <param name="vid">A vid</param>
		/// <param name="decision">The decision mapped to the vid</param>
		/// <param name="indexes">Indexes related to the decision</param>
		/// <returns>true if the vid has mapping to a decision, false otherwise</returns>
		public override bool TryGetDecision(int vid, out Decision decision, out object[] indexes)
		{
			throw new NotSupportedException();
		}

		/// <summary>Gets a constraint with indexes and component from vid
		/// </summary>
		/// <param name="vid">vid of a row</param>
		/// <param name="constraint">The constraint mapped to the vid</param>
		/// <param name="indexes">Indexes related to the constraint</param>
		/// <param name="component">component of the constraint.
		/// When single constraint has multiple part 
		/// (e.g. "5 &gt;= x &gt;= y &gt;= 9") there might be different row for each part. component is zero based.</param>
		/// <returns>true if the vid has mapping to a Constraint, false otherwise</returns>
		public override bool TryGetConstraint(int vid, out Constraint constraint, out object[] indexes, out int component)
		{
			throw new NotSupportedException();
		}

		/// <summary>Gets goal out of a vid
		/// </summary>
		/// <param name="vid">A vid</param>
		/// <param name="goal">The goal mapped to the vid</param>
		/// <returns>true if the vid has mapping to a goal, false otherwise</returns>
		public override bool TryGetGoal(int vid, out Goal goal)
		{
			throw new NotSupportedException();
		}

		/// <summary>Gets indexes of a constraint
		/// </summary>
		/// <remarks>First call to TryGetVid of constraint or GetIndexes ( of constraint) or GetComponents is expensive</remarks>
		/// <param name="constraint">A constraint</param>
		/// <returns>A collection of indexes related to the constraint</returns>
		public override IEnumerable<object[]> GetIndexes(Constraint constraint)
		{
			throw new NotSupportedException();
		}

		/// <summary>Get all indexes of a decision
		/// </summary>
		/// <param name="decision">The decision</param>
		/// <returns>Enumaration of all indexes</returns>
		/// <exception cref="T:System.ArgumentNullException">decision must not be null</exception>
		public override IEnumerable<object[]> GetIndexes(Decision decision)
		{
			if ((object)decision == null)
			{
				throw new ArgumentNullException("decision");
			}
			return IndexesOfDecision(decision);
		}

		/// <summary>Get parts of constraint with its indexes
		/// </summary>
		/// <remarks>First call to TryGetVid of constraint or GetIndexes ( of constraint) or GetComponents is expensive</remarks>
		/// <param name="constraint">A Constraint</param>
		/// <param name="indexes">Indexes related to the constraint</param>
		/// <returns>A collection of parts related to the constraint</returns>
		public override IEnumerable<int> GetComponents(Constraint constraint, object[] indexes)
		{
			throw new NotSupportedException();
		}

		internal override void CalculateSecondStageResults(RecourseDecision recourseDecision)
		{
			throw new NotSupportedException();
		}

		internal override ValueTable<Rational> GetGoalCoefficient(Decision decision)
		{
			throw new NotSupportedException();
		}

		internal override void UpdateSecondStageResults(Model model, double probability)
		{
			throw new NotSupportedException();
		}

		internal override bool IsGoalOptimal(int goalNumber)
		{
			throw new NotSupportedException();
		}

		internal override Rational GetValue(IVariable decision, object[] indexes)
		{
			int num = KeyForVariable(decision, indexes);
			if (!_solver.TryGetVariableFromKey(num, out var term))
			{
				throw new MsfException(Resources.InternalError);
			}
			int integerValue = _solution.GetIntegerValue(term);
			return integerValue;
		}

		/// <summary>Get the next solution.
		/// </summary>
		/// <remarks>This method does not register the solution.</remarks>
		internal override SolverQuality GetNext(Model model, Func<bool> newQueryAbort)
		{
			model.ClearDecisionValues();
			_solution.GetNext(newQueryAbort);
			SolverQuality solverQuality;
			switch (_solution.Quality)
			{
			case ConstraintSolverSolution.SolutionQuality.Feasible:
				solverQuality = SolverQuality.Feasible;
				break;
			case ConstraintSolverSolution.SolutionQuality.Infeasible:
				solverQuality = SolverQuality.Infeasible;
				break;
			case ConstraintSolverSolution.SolutionQuality.Optimal:
				solverQuality = SolverQuality.Optimal;
				break;
			default:
				solverQuality = SolverQuality.Unknown;
				break;
			}
			if (!_solution.HasFoundSolution)
			{
				solverQuality = ((_solution.IsInterrupted || _solution.SolverParams.Algorithm == ConstraintSolverParams.CspSearchAlgorithm.LocalSearch) ? SolverQuality.Unknown : SolverQuality.Infeasible);
			}
			if (ShouldExtractDecisionValues(solverQuality))
			{
				model.ExtractDecisionValues(this);
			}
			return solverQuality;
		}

		private IEnumerable<object[]> IndexesOfDecision(IVariable decision)
		{
			if (_keyFromDecisionWithoutIndex.ContainsKey(decision))
			{
				yield return SolutionMapping.EmptyArray;
			}
			if (!_keyFromDecisionAndIndex.ContainsKey(decision))
			{
				yield break;
			}
			foreach (object[] key in _keyFromDecisionAndIndex[decision].Keys)
			{
				yield return key;
			}
		}

		private int KeyForVariable(IVariable decision, object[] indexes)
		{
			if (_keyFromDecisionWithoutIndex.TryGetValue(decision, out var value))
			{
				return value;
			}
			if (!_keyFromDecisionAndIndex[decision].TryGetValue(out value, indexes))
			{
				throw new MsfException(Resources.InternalError);
			}
			return value;
		}
	}
}
