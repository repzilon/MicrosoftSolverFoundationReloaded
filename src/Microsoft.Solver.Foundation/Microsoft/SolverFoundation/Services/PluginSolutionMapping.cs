using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// The base class for solution mappings for linear, nonlinear, and term-based plugin solvers.
	/// </summary>
	public abstract class PluginSolutionMapping : SolutionMapping
	{
		internal readonly int[] _decisionVids;

		internal readonly ValueTable<int>[] _decisionIndexedVids;

		internal readonly int[] _goalVids;

		internal readonly Constraint[] _rowConstraint;

		internal readonly int[] _rowSubconstraint;

		internal readonly object[][] _rowIndexes;

		internal Dictionary<int, KeyValuePair<Decision, object[]>> _vidToDecisionMapping;

		internal Dictionary<int, Goal> _vidToGoalMapping;

		internal Dictionary<Constraint, ValueTable<int[]>> _constraintToVidMapping;

		private readonly object[][] _oneEmptyIndexes = new object[1][] { SolutionMapping.EmptyArray };

		internal PluginSolutionMapping(Model model, int[] decisionVids, ValueTable<int>[] decisionIndexedVids, int[] goalVids, Constraint[] rowConstraint, int[] rowSubconstraint, object[][] rowIndexes)
			: base(model)
		{
			_decisionVids = decisionVids;
			_decisionIndexedVids = decisionIndexedVids;
			_goalVids = goalVids;
			_rowConstraint = rowConstraint;
			_rowSubconstraint = rowSubconstraint;
			_rowIndexes = rowIndexes;
		}

		internal abstract Rational GetValue(int vidVar);

		internal abstract int GetSolverVariableCount();

		internal abstract int GetSolverGoalCount();

		internal abstract IEnumerable<int> GetSolverRowIndices();

		internal abstract bool IsGoal(int vidRow);

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
			int id = decision._id;
			if (id < 0)
			{
				throw new MsfException(Resources.InternalError);
			}
			if (decision._indexSets.Length > 0)
			{
				if (_decisionIndexedVids == null)
				{
					return new object[0][];
				}
				ValueTable<int> valueTable = _decisionIndexedVids[id];
				if (valueTable != null)
				{
					return valueTable.Keys;
				}
				return new object[0][];
			}
			int num = _decisionVids[id];
			if (num >= 0)
			{
				return _oneEmptyIndexes;
			}
			return new object[0][];
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
			vid = -1;
			if (base.Model.IsStochastic)
			{
				return false;
			}
			vid = GetVidVar(decision, indexes);
			return vid >= 0;
		}

		/// <summary>Get vid out of constraint, indexes and part
		/// </summary>
		/// <remarks>First call to TryGetVid of constraint or GetIndexes ( of constraint) or GetComponents is expensive</remarks>
		/// <param name="constraint">A constraint</param>
		/// <param name="indexes">Indexes related to the constraint</param>
		/// <param name="component">component of the constraint</param>
		/// <param name="vid">vid related to the constraint</param>
		/// <returns>true if constraint has mapping to a vid, false otherwise</returns>
		/// <exception cref="T:System.ArgumentNullException">constraint and indexes must not be null</exception>
		public override bool TryGetVid(Constraint constraint, object[] indexes, int component, out int vid)
		{
			if (constraint == null)
			{
				throw new ArgumentNullException("constraint");
			}
			if (indexes == null)
			{
				throw new ArgumentNullException("indexes");
			}
			vid = -1;
			if (base.Model.IsStochastic)
			{
				return false;
			}
			EnsureConstraintToVidMappingInitiated();
			ValueTable<int[]> value = null;
			if (!_constraintToVidMapping.TryGetValue(constraint, out value))
			{
				return false;
			}
			if (!value.TryGetValue(out var value2, indexes))
			{
				return false;
			}
			if (component >= value2.Length)
			{
				return false;
			}
			vid = value2[component];
			return true;
		}

		/// <summary>Gets vid out of a Goal
		/// </summary>
		/// <param name="goal">A goal</param>
		/// <param name="vid">vid related to the goal</param>
		/// <returns>true if the goal has mapping to a vid, false otherwise</returns>
		/// <exception cref="T:System.ArgumentNullException">goal must not be null </exception>
		public override bool TryGetVid(Goal goal, out int vid)
		{
			if (goal == null)
			{
				throw new ArgumentNullException("goal");
			}
			vid = -1;
			if (base.Model.IsStochastic)
			{
				return false;
			}
			vid = GetVid(goal);
			return vid >= 0;
		}

		/// <summary>Gets a decision out of vid
		/// </summary>
		/// <remarks>Caller must not change the indexes array</remarks>
		/// <param name="vid">A vid</param>
		/// <param name="decision">The decision mapped to the vid</param>
		/// <param name="indexes">Indexes related to the decision</param>
		/// <returns>true if the vid has mapping to a decision, false otherwise</returns>
		public override bool TryGetDecision(int vid, out Decision decision, out object[] indexes)
		{
			decision = null;
			indexes = null;
			if (base.Model.IsStochastic)
			{
				return false;
			}
			EnsureVidToDecisionMappingInitiated();
			if (!_vidToDecisionMapping.TryGetValue(vid, out var value))
			{
				return false;
			}
			decision = value.Key;
			indexes = value.Value;
			return true;
		}

		/// <summary>Gets a constraint with indexes and component from vid
		/// </summary>
		/// <remarks>Caller must not change the indexes array</remarks>
		/// <param name="vid">vid of a row</param>
		/// <param name="constraint">The constraint mapped to the vid</param>
		/// <param name="indexes">Indexes related to the constraint</param>
		/// <param name="component">component of the constraint.
		/// When single constraint has multiple part 
		/// (e.g. "5 &gt;= x &gt;= y &gt;= 9") there might be different row for each part. component is zero based.</param>
		/// <returns>true if the vid has mapping to a Constraint, false otherwise</returns>
		public override bool TryGetConstraint(int vid, out Constraint constraint, out object[] indexes, out int component)
		{
			indexes = SolutionMapping.EmptyArray;
			component = 0;
			constraint = null;
			if (base.Model.IsStochastic)
			{
				return false;
			}
			if (!IsValidRowVid(vid))
			{
				return false;
			}
			bool flag = _rowIndexes != null && _rowIndexes[vid] != null && _rowIndexes[vid].Length > 0;
			bool flag2 = _rowSubconstraint != null && _rowSubconstraint[vid] > 0;
			constraint = _rowConstraint[vid];
			if (constraint == null)
			{
				return false;
			}
			if (flag)
			{
				indexes = _rowIndexes[vid];
			}
			if (flag2)
			{
				component = _rowSubconstraint[vid];
			}
			return true;
		}

		private bool IsValidRowVid(int vid)
		{
			if (vid < 0)
			{
				return false;
			}
			if (_rowIndexes != null && _rowIndexes.Length <= vid)
			{
				return false;
			}
			if (_rowSubconstraint != null && _rowSubconstraint.Length <= vid)
			{
				return false;
			}
			if (_rowConstraint.Length <= vid)
			{
				return false;
			}
			return true;
		}

		/// <summary>Gets goal out of a vid
		/// </summary>
		/// <param name="vid">A vid</param>
		/// <param name="goal">The goal mapped to the vid</param>
		/// <returns>true if the vid has mapping to a goal, false otherwise</returns>
		public override bool TryGetGoal(int vid, out Goal goal)
		{
			goal = null;
			if (base.Model.IsStochastic)
			{
				return false;
			}
			EnsureVidToGoalMappingInitiated();
			return _vidToGoalMapping.TryGetValue(vid, out goal);
		}

		/// <summary>Gets indexes of a constraint
		/// </summary>
		/// <remarks>First call to TryGetVid of constraint or GetIndexes ( of constraint) or GetComponents is expensive</remarks>
		/// <param name="constraint">A constraint</param>
		/// <returns>A collection of indexes related to the constraint</returns>
		/// <exception cref="T:System.ArgumentNullException">constraint must not be null </exception>
		/// <exception cref="T:System.NotSupportedException">Mapping of stochastic model is not supported</exception>
		public override IEnumerable<object[]> GetIndexes(Constraint constraint)
		{
			if (constraint == null)
			{
				throw new ArgumentNullException("constraint");
			}
			if (base.Model.IsStochastic)
			{
				throw new NotSupportedException(Resources.MappingOfStochasticModelIsNotSupported);
			}
			EnsureConstraintToVidMappingInitiated();
			ValueTable<int[]> value = null;
			if (!_constraintToVidMapping.TryGetValue(constraint, out value))
			{
				return new object[0][];
			}
			return value.Keys;
		}

		/// <summary>Get parts of constraint with its indexes
		/// </summary>
		/// <remarks>First call to TryGetVid of constraint or GetIndexes ( of constraint) or GetComponents is expensive</remarks>
		/// <param name="constraint">A Constraint</param>
		/// <param name="indexes">Indexes related to the constraint</param>
		/// <returns>A collection of parts related to the constraint</returns>
		/// <exception cref="T:System.NotSupportedException">Mapping of stochastic model is not supported</exception>
		/// <exception cref="T:System.ArgumentNullException">constraint and indexes must not be null </exception>
		public override IEnumerable<int> GetComponents(Constraint constraint, object[] indexes)
		{
			if (constraint == null)
			{
				throw new ArgumentNullException("constraint");
			}
			if (indexes == null)
			{
				throw new ArgumentNullException("indexes");
			}
			if (base.Model.IsStochastic)
			{
				throw new NotSupportedException(Resources.MappingOfStochasticModelIsNotSupported);
			}
			return GetComponentsCore(constraint, indexes);
		}

		/// <summary>Get parts of constraint with its indexes
		/// </summary>
		/// <remarks>First call to TryGetVid of constraint or GetIndexes ( of constraint) or GetComponents is expensive</remarks>
		/// <param name="constraint">A Constraint</param>
		/// <param name="indexes">Indexes related to the constraint</param>
		/// <returns>A collection of parts related to the constraint</returns>
		private IEnumerable<int> GetComponentsCore(Constraint constraint, object[] indexes)
		{
			EnsureConstraintToVidMappingInitiated();
			ValueTable<int[]> indexesToPartsToVids = null;
			if (!_constraintToVidMapping.TryGetValue(constraint, out indexesToPartsToVids))
			{
				yield break;
			}
			int[] partsVids = null;
			if (indexesToPartsToVids.TryGetValue(out partsVids, indexes))
			{
				for (int i = 0; i < partsVids.Length; i++)
				{
					yield return i;
				}
			}
		}

		/// <summary>Calculate and set the recourseDecision results (avg, min, max) 
		/// </summary>
		/// <param name="recourseDecision"></param>
		internal override void CalculateSecondStageResults(RecourseDecision recourseDecision)
		{
			throw new NotSupportedException();
		}

		/// <summary>This updates the second stage results when using decomposition
		/// Called at the end of every iteration for each scenario 
		/// </summary>
		/// <param name="model"></param>
		/// <param name="probability">probability of the scenario</param>
		internal override void UpdateSecondStageResults(Model model, double probability)
		{
			throw new NotSupportedException();
		}

		internal int GetVid(Goal goal)
		{
			return _goalVids[goal._id];
		}

		/// <summary>This is called from context when solving stochastic problem 
		/// </summary>
		internal override ValueTable<Rational> GetGoalCoefficient(Decision decision)
		{
			throw new NotSupportedException();
		}

		internal string GetRowName(int vidRow)
		{
			bool flag = _rowIndexes != null && _rowIndexes[vidRow] != null && _rowIndexes[vidRow].Length > 0;
			bool flag2 = _rowSubconstraint != null && _rowSubconstraint[vidRow] > 0;
			if (!flag && !flag2)
			{
				return _rowConstraint[vidRow].Name;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(_rowConstraint[vidRow].Name);
			if (flag)
			{
				stringBuilder.Append(Statics.JoinArrayToString(_rowIndexes[vidRow]));
			}
			if (flag2)
			{
				stringBuilder.Append("#");
				stringBuilder.Append(_rowSubconstraint[vidRow]);
			}
			return stringBuilder.ToString();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="decision"></param>
		/// <param name="indexes"></param>
		/// <returns>vid if success and -1 if fails</returns>
		internal int GetVidVar(Decision decision, object[] indexes)
		{
			int id = decision._id;
			if (id < 0)
			{
				throw new MsfException(Resources.InternalError);
			}
			if (decision._indexSets.Length == 0 && _decisionVids != null && id < _decisionVids.Length)
			{
				int num = _decisionVids[id];
				if (num >= 0)
				{
					return num;
				}
			}
			else
			{
				ValueTable<int> valueTable = _decisionIndexedVids[id];
				if (valueTable != null && indexes.Length == decision._indexSets.Length && valueTable.TryGetValue(out var value, indexes))
				{
					return value;
				}
			}
			return -1;
		}

		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		[SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "report")]
		[SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "decision")]
		[SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "indexes")]
		private LinearSolverSensitivityRange GetGoalCoefficientSensitivity(ILinearSolverSensitivityReport report, IVariable decision, object[] indexes)
		{
			throw new NotSupportedException();
		}

		internal override Rational GetValue(IVariable decision, object[] indexes)
		{
			if (decision is Decision decision2)
			{
				int vidVar = GetVidVar(decision2, indexes);
				if (vidVar < 0)
				{
					throw new MsfException(string.Format(CultureInfo.InvariantCulture, Resources.NoVidForTheDecision0, new object[1] { decision2.Name }));
				}
				return GetValue(vidVar);
			}
			if (decision is Goal goal)
			{
				int vid = GetVid(goal);
				if (vid >= 0)
				{
					return GetValue(vid);
				}
				throw new MsfException(string.Format(CultureInfo.InvariantCulture, Resources.NoVidForTheGoal0, new object[1] { goal.Name }));
			}
			throw new MsfException(Resources.InternalError);
		}

		internal static ValueSet[] DecisionValueSets(Decision decision)
		{
			ValueSet[] array = new ValueSet[decision._indexSets.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = decision._indexSets[i].ValueSet;
			}
			return array;
		}

		internal override SolverQuality GetNext(Model model, Func<bool> newTimer)
		{
			return SolverQuality.Infeasible;
		}

		/// <summary>If _vidToDecisionMapping was not created, creates it
		/// </summary>
		private void EnsureVidToDecisionMappingInitiated()
		{
			if (_vidToDecisionMapping != null)
			{
				return;
			}
			_vidToDecisionMapping = new Dictionary<int, KeyValuePair<Decision, object[]>>(GetSolverVariableCount());
			foreach (Decision decision in base.Model.Decisions)
			{
				foreach (object[] index in GetIndexes(decision))
				{
					if (TryGetVid(decision, index, out var vid))
					{
						_vidToDecisionMapping[vid] = new KeyValuePair<Decision, object[]>(decision, index);
					}
				}
			}
		}

		/// <summary>If _vidToGoalMapping was not created, creates it
		/// </summary>
		private void EnsureVidToGoalMappingInitiated()
		{
			if (_vidToGoalMapping != null)
			{
				return;
			}
			_vidToGoalMapping = new Dictionary<int, Goal>(GetSolverGoalCount());
			foreach (Goal goal in base.Model.Goals)
			{
				if (TryGetVid(goal, out var vid))
				{
					_vidToGoalMapping[vid] = goal;
				}
			}
		}

		/// <summary>If _constraintToVidMapping was not created, creates it
		/// </summary>
		private void EnsureConstraintToVidMappingInitiated()
		{
			if (_constraintToVidMapping != null)
			{
				return;
			}
			Dictionary<Constraint, Dictionary<object[], Dictionary<int, int>>> dictionary = new Dictionary<Constraint, Dictionary<object[], Dictionary<int, int>>>(base.Model.Constraints.Count());
			foreach (int solverRowIndex in GetSolverRowIndices())
			{
				if (IsGoal(solverRowIndex))
				{
					continue;
				}
				Constraint constraint = _rowConstraint[solverRowIndex];
				int key = 0;
				if (constraint == null)
				{
					continue;
				}
				object[] array = SolutionMapping.EmptyArray;
				bool flag = _rowIndexes != null && _rowIndexes[solverRowIndex] != null && _rowIndexes[solverRowIndex].Length > 0;
				bool flag2 = _rowSubconstraint != null && _rowSubconstraint[solverRowIndex] > 0;
				if (flag)
				{
					array = _rowIndexes[solverRowIndex];
				}
				if (flag2)
				{
					key = _rowSubconstraint[solverRowIndex];
				}
				Dictionary<object[], Dictionary<int, int>> value = null;
				if (!dictionary.TryGetValue(constraint, out value))
				{
					ValueSet[] array2 = new ValueSet[array.Length];
					for (int i = 0; i < array.Length; i++)
					{
						array2[i] = null;
					}
					value = (dictionary[constraint] = new Dictionary<object[], Dictionary<int, int>>());
				}
				Dictionary<int, int> value2 = null;
				if (!value.TryGetValue(array, out value2))
				{
					value2 = new Dictionary<int, int>();
					value.Add(array, value2);
				}
				value2[key] = solverRowIndex;
			}
			_constraintToVidMapping = new Dictionary<Constraint, ValueTable<int[]>>(dictionary.Count);
			foreach (KeyValuePair<Constraint, Dictionary<object[], Dictionary<int, int>>> item in dictionary)
			{
				ValueSet[] indexSets = new ValueSet[item.Value.First().Key.Length];
				ValueTable<int[]> valueTable = ValueTable<int[]>.Create(null, indexSets);
				foreach (KeyValuePair<object[], Dictionary<int, int>> item2 in item.Value)
				{
					int count = item2.Value.Count;
					int[] array3 = new int[count];
					for (int j = 0; j < count; j++)
					{
						array3[j] = item2.Value[j];
					}
					valueTable.Add(array3, item2.Key);
				}
				_constraintToVidMapping.Add(item.Key, valueTable);
			}
		}
	}
}
