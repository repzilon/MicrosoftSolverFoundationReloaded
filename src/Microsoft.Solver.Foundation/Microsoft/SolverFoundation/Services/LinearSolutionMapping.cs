using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A class for mapping between Sfs model and a ILinearModel
	/// </summary>
	public sealed class LinearSolutionMapping : PluginSolutionMapping
	{
		private readonly ILinearSolver _linearSolver;

		private readonly ILinearSolution _linearSolution;

		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
		private readonly int[,] _recourseDecisionVids;

		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
		private readonly ValueTable<int>[,] _recourseDecisionIndexedVids;

		private readonly Rational[] _goalBoundsAdjustments;

		internal LinearSolutionMapping(Model model, ILinearSolver solver, ILinearSolution solution, int[] decisionVids, ValueTable<int>[] decisionIndexedVids, int[,] recourseDecisionVids, ValueTable<int>[,] recourseDecisionIndexedVids, int[] goalVids, Rational[] goalBoundsAdjustments, Constraint[] rowConstraint, int[] rowSubconstraint, object[][] rowIndexes)
			: base(model, decisionVids, decisionIndexedVids, goalVids, rowConstraint, rowSubconstraint, rowIndexes)
		{
			_linearSolver = solver;
			_linearSolution = solution;
			_recourseDecisionVids = recourseDecisionVids;
			_recourseDecisionIndexedVids = recourseDecisionIndexedVids;
			_goalBoundsAdjustments = goalBoundsAdjustments;
		}

		internal override Rational GetValue(int vidVar)
		{
			return _linearSolver.GetValue(vidVar);
		}

		internal override int GetSolverVariableCount()
		{
			return _linearSolver.VariableCount;
		}

		internal override int GetSolverGoalCount()
		{
			return _linearSolver.GoalCount;
		}

		internal override IEnumerable<int> GetSolverRowIndices()
		{
			return _linearSolver.RowIndices;
		}

		internal override bool IsGoal(int vidRow)
		{
			return _linearSolver.IsGoal(vidRow);
		}

		/// <summary>Calculate and set the recourseDecision results (avg, min, max) 
		/// </summary>
		/// <param name="recourseDecision"></param>
		internal override void CalculateSecondStageResults(RecourseDecision recourseDecision)
		{
			if (recourseDecision._secondStageResults != null || recourseDecision._domain.ValueClass != 0)
			{
				return;
			}
			recourseDecision.ClearSecondStageResults();
			int length = _recourseDecisionVids.GetLength(1);
			int id = recourseDecision._id;
			if (recourseDecision._indexSets.Length > 0)
			{
				ValueTable<int> valueTable = _recourseDecisionIndexedVids[id, 0];
				{
					foreach (object[] key in valueTable.Keys)
					{
						Rational rational = 0;
						Rational rational2 = Rational.NegativeInfinity;
						Rational rational3 = Rational.PositiveInfinity;
						for (int i = 0; i < length; i++)
						{
							if (_recourseDecisionIndexedVids[id, i].TryGetValue(out var value, key))
							{
								Rational value2 = GetValue(value);
								rational += value2 * recourseDecision._secondStageProbabilities[i];
								if (value2 > rational2)
								{
									rational2 = value2;
								}
								if (value2 < rational3)
								{
									rational3 = value2;
								}
							}
						}
						recourseDecision.SetSecondStageResult(new double[3]
						{
							(double)rational,
							(double)rational3,
							(double)rational2
						}, key);
					}
					return;
				}
			}
			Rational rational4 = 0;
			Rational rational5 = Rational.NegativeInfinity;
			Rational rational6 = Rational.PositiveInfinity;
			for (int j = 0; j < length; j++)
			{
				int vidVar = _recourseDecisionVids[id, j];
				Rational value3 = GetValue(vidVar);
				rational4 += value3;
				if (value3 > rational5)
				{
					rational5 = value3;
				}
				if (value3 < rational6)
				{
					rational6 = value3;
				}
			}
			Rational rational7 = rational4 / length;
			recourseDecision.SetSecondStageResult(new double[3]
			{
				(double)rational7,
				(double)rational6,
				(double)rational5
			}, SolutionMapping.EmptyArray);
		}

		/// <summary>This updates the second stage results when using decomposition
		/// Called at the end of every iteration for each scenario 
		/// </summary>
		/// <param name="model"></param>
		/// <param name="probability">probability of the scenario</param>
		internal override void UpdateSecondStageResults(Model model, double probability)
		{
			foreach (RecourseDecision allRecourseDecision in model.AllRecourseDecisions)
			{
				if (allRecourseDecision._indexSets.Length > 0)
				{
					ValueTable<int> valueTable = _recourseDecisionIndexedVids[allRecourseDecision._id, 0];
					foreach (object[] key in valueTable.Keys)
					{
						valueTable.TryGetValue(out var value, key);
						UpdateSecondStageResults(allRecourseDecision, probability, GetValue(value).ToDouble(), key);
					}
				}
				else
				{
					int vidVar = _recourseDecisionVids[allRecourseDecision._id, 0];
					UpdateSecondStageResults(allRecourseDecision, probability, GetValue(vidVar).ToDouble(), SolutionMapping.EmptyArray);
				}
			}
		}

		internal override bool ShouldExtractDecisionValues(SolverQuality quality)
		{
			switch (quality)
			{
			case SolverQuality.Unknown:
				return _linearSolution.Result != LinearResult.Invalid;
			default:
				return false;
			case SolverQuality.Optimal:
			case SolverQuality.Feasible:
				return true;
			}
		}

		private static void UpdateSecondStageResults(RecourseDecision recourseDecision, double probability, double value, object[] indexes)
		{
			double[] array = recourseDecision.GetSecondStageResults(indexes);
			double num = probability * value;
			double num2;
			double num3;
			if (array == null)
			{
				num2 = value;
				num3 = value;
				array = new double[3];
			}
			else
			{
				num2 = Math.Min(value, array[1]);
				num3 = Math.Max(value, array[2]);
			}
			array[0] += num;
			array[1] = num2;
			array[2] = num3;
			recourseDecision.SetSecondStageResult(array, indexes);
		}

		/// <summary>This is called from context when solving stochastic problem 
		/// </summary>
		internal override ValueTable<Rational> GetGoalCoefficient(Decision decision)
		{
			DebugContracts.NonNull(decision);
			ValueSet[] indexSets = PluginSolutionMapping.DecisionValueSets(decision);
			ValueTable<Rational> valueTable = ValueTable<Rational>.Create(null, indexSets);
			foreach (object[] index in GetIndexes(decision))
			{
				int value;
				if (decision._indexSets.Length == 0)
				{
					value = _decisionVids[decision._id];
				}
				else
				{
					if (_decisionIndexedVids == null)
					{
						continue;
					}
					ValueTable<int> valueTable2 = _decisionIndexedVids[decision._id];
					if (valueTable2 == null || !valueTable2.TryGetValue(out value, index))
					{
						continue;
					}
				}
				Rational coefficient = _linearSolver.GetCoefficient(_linearSolver.Goals.First().Index, value);
				valueTable.Add(coefficient, index);
			}
			return valueTable;
		}

		/// <summary>Getting the Sensitivity report from the linear solver
		/// </summary>
		/// <returns></returns>
		internal ILinearSolverSensitivityReport GetSensitivityReport()
		{
			return _linearSolver.GetReport(LinearSolverReportType.Sensitivity) as ILinearSolverSensitivityReport;
		}

		/// <summary>Getting the Infeasibility report from the linear solver
		/// </summary>
		/// <returns></returns>
		internal ILinearSolverInfeasibilityReport GetInfeasibilityReport()
		{
			return _linearSolver.GetReport(LinearSolverReportType.Infeasibility) as ILinearSolverInfeasibilityReport;
		}

		internal IEnumerable<KeyValuePair<string, Rational>> GetAllShadowPrices(ILinearSolverSensitivityReport sensitivityReport)
		{
			foreach (Constraint constraint in base.Model.Constraints)
			{
				IEnumerable<KeyValuePair<string, Rational>> shadowPrices = GetShadowPrices(sensitivityReport, constraint);
				foreach (KeyValuePair<string, Rational> item in shadowPrices)
				{
					yield return item;
				}
			}
		}

		internal IEnumerable<KeyValuePair<string, Rational>> GetShadowPrices(ILinearSolverSensitivityReport sensitivityReport, Constraint constraint)
		{
			IEnumerable<int> enumerable = from vidRow in GetVids(constraint)
				select (vidRow);
			if (!enumerable.Any())
			{
				throw new ArgumentException(Resources.CannotFindTheConstraintInTheModel, "constraint");
			}
			return GetShadowPricesCore(sensitivityReport, enumerable);
		}

		private IEnumerable<KeyValuePair<string, Rational>> GetShadowPricesCore(ILinearSolverSensitivityReport sensitivityReport, IEnumerable<int> vidsRows)
		{
			foreach (int vidRow in vidsRows)
			{
				yield return GetShadowPrice(vidRow, sensitivityReport);
			}
		}

		/// <summary>Get specific row's dual value/shadow price
		/// </summary>
		private KeyValuePair<string, Rational> GetShadowPrice(int vidRow, ILinearSolverSensitivityReport sensitivityReport)
		{
			string rowName = GetRowName(vidRow);
			Rational dualValue = sensitivityReport.GetDualValue(vidRow);
			return new KeyValuePair<string, Rational>(rowName, dualValue);
		}

		internal IEnumerable<KeyValuePair<Decision, ValueTable<LinearSolverSensitivityRange>>> GetGoalCoefficientsSensitivity(ILinearSolverSensitivityReport sensitivityReport, IEnumerable<Decision> decisions)
		{
			foreach (Decision decision in decisions)
			{
				DebugContracts.NonNull(decision);
				ValueSet[] sets = PluginSolutionMapping.DecisionValueSets(decision);
				ValueTable<LinearSolverSensitivityRange> valueTable = ValueTable<LinearSolverSensitivityRange>.Create(null, sets);
				foreach (object[] index in GetIndexes(decision))
				{
					LinearSolverSensitivityRange goalCoefficientSensitivity = GetGoalCoefficientSensitivity(sensitivityReport, decision, index);
					valueTable.Add(goalCoefficientSensitivity, index);
				}
				yield return new KeyValuePair<Decision, ValueTable<LinearSolverSensitivityRange>>(decision, valueTable);
			}
		}

		/// <summary>Get sensitivity ranges for all constraints.
		/// </summary>
		/// <param name="sensitivityReport">An ILinearSolverSensitivityReport.</param>
		internal IEnumerable<KeyValuePair<string, LinearSolverSensitivityRange>> GetAllConstraintBoundsSensitivity(ILinearSolverSensitivityReport sensitivityReport)
		{
			if (_rowConstraint == null)
			{
				yield break;
			}
			foreach (Constraint constraint in base.Model.Constraints)
			{
				IEnumerable<KeyValuePair<string, LinearSolverSensitivityRange>> constraintBounds = GetConstraintBoundsSensitivity(sensitivityReport, constraint);
				foreach (KeyValuePair<string, LinearSolverSensitivityRange> item in constraintBounds)
				{
					yield return item;
				}
			}
		}

		/// <summary>Get sensitivity ranges for the specified constraint.
		/// </summary>
		/// <param name="sensitivityReport">An ILinearSolverSensitivityReport.</param>
		/// <param name="constraint">The constraint of interest.</param>
		internal IEnumerable<KeyValuePair<string, LinearSolverSensitivityRange>> GetConstraintBoundsSensitivity(ILinearSolverSensitivityReport sensitivityReport, Constraint constraint)
		{
			IEnumerable<int> enumerable = from vidRow in GetVids(constraint)
				select (vidRow);
			if (!enumerable.Any())
			{
				throw new ArgumentException(Resources.CannotFindTheConstraintInTheModel, "constraint");
			}
			return GetConstraintBoundsSensitivityCore(sensitivityReport, enumerable);
		}

		/// <summary>Get sensitivity ranges for the specified constraint.
		/// </summary>
		/// <param name="sensitivityReport">An ILinearSolverSensitivityReport.</param>
		/// <param name="vidsRows">Enumeration of vids related to a specific constraint.</param>
		private IEnumerable<KeyValuePair<string, LinearSolverSensitivityRange>> GetConstraintBoundsSensitivityCore(ILinearSolverSensitivityReport sensitivityReport, IEnumerable<int> vidsRows)
		{
			foreach (int vidRow in vidsRows)
			{
				yield return GetRowRange(vidRow, sensitivityReport);
			}
		}

		/// <summary>Get a collection of all vids mapping to a constraint
		/// </summary>
		private IEnumerable<int> GetVids(Constraint constraint)
		{
			foreach (object[] indexes in GetIndexes(constraint))
			{
				foreach (int component in GetComponents(constraint, indexes))
				{
					TryGetVid(constraint, indexes, component, out var vidRow);
					yield return vidRow;
				}
			}
		}

		/// <summary>Get specific row's sensitivity range
		/// </summary>
		private KeyValuePair<string, LinearSolverSensitivityRange> GetRowRange(int vidRow, ILinearSolverSensitivityReport sensitivityReport)
		{
			string rowName = GetRowName(vidRow);
			LinearSolverSensitivityRange variableRange = sensitivityReport.GetVariableRange(vidRow);
			return new KeyValuePair<string, LinearSolverSensitivityRange>(rowName, variableRange);
		}

		internal IEnumerable<string> GetInfeasibleSet(ILinearSolverInfeasibilityReport report)
		{
			IEnumerable<int> irreducibleInfeasibleSet = report.IrreducibleInfeasibleSet;
			if (irreducibleInfeasibleSet == null)
			{
				yield break;
			}
			foreach (int vidRow in irreducibleInfeasibleSet)
			{
				if (_rowConstraint[vidRow] == null)
				{
					yield return "unknown constraint";
				}
				else
				{
					yield return GetRowName(vidRow);
				}
			}
		}

		internal LinearSolverSensitivityRange GetGoalCoefficientSensitivity(ILinearSolverSensitivityReport sensitivityReport, Decision decision, object[] indexes)
		{
			return GetGoalCoefficientSensitivity(sensitivityReport, (IVariable)decision, indexes);
		}

		[SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "indexes")]
		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		[SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "decision")]
		[SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "report")]
		private LinearSolverSensitivityRange GetGoalCoefficientSensitivity(ILinearSolverSensitivityReport report, IVariable decision, object[] indexes)
		{
			if (decision is Decision decision2)
			{
				int vidVar = GetVidVar(decision2, indexes);
				if (vidVar < 0)
				{
					throw new MsfException(string.Format(CultureInfo.InvariantCulture, Resources.NoVidForTheDecision0, new object[1] { decision2.Name }));
				}
				return report.GetObjectiveCoefficientRange(vidVar);
			}
			throw new MsfException(Resources.InternalError);
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
					Rational value = GetValue(vid);
					return value - _goalBoundsAdjustments[goal._id];
				}
				throw new MsfException(string.Format(CultureInfo.InvariantCulture, Resources.NoVidForTheGoal0, new object[1] { goal.Name }));
			}
			throw new MsfException(Resources.InternalError);
		}

		internal override bool IsGoalOptimal(int goalNumber)
		{
			_linearSolution.GetSolvedGoal(goalNumber, out var _, out var _, out var _, out var fOptimal);
			return fOptimal;
		}
	}
}
