using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A solver that uses simple, general-purpose local search strategies.
	/// </summary>
	/// <remarks>
	/// Can be used for discrete and continuous, linear and non-linear,
	/// satisfaction and/or optimization models.
	/// This solver is incomplete: it does not guarantee global optimality. 
	/// </remarks>
	public class HybridLocalSearchSolver : HybridLocalSearchModel, ITermSolver, IRowVariableSolver, ISolver, ITermModel, IRowVariableModel, IGoalModel, INonlinearSolution, ISolverSolution, ISolverProperties, IReportProvider
	{
		private class Solution : INonlinearSolution, ISolverSolution
		{
			public int SolvedGoalCount => Goals.Length;

			public NonlinearResult Result { get; internal set; }

			public IGoal[] Goals { get; internal set; }

			internal double[] Values { get; set; }

			public double GetValue(int vid)
			{
				return Values[vid];
			}

			public double GetSolutionValue(int goalIndex)
			{
				return Values[Goals[goalIndex].Index];
			}

			public void GetSolvedGoal(int igoal, out object key, out int vid, out bool fMinimize, out bool fOptimal)
			{
				IGoal goal = Goals[igoal];
				key = goal.Key;
				vid = goal.Index;
				fMinimize = goal.Minimize;
				fOptimal = false;
			}
		}

		private Solution _solution;

		/// <summary>
		/// Gets the operations supported by the solver.
		/// </summary>
		/// <returns>All the TermModelOperations supported by the solver.</returns>
		public IEnumerable<TermModelOperation> SupportedOperations => (TermModelOperation[])Enum.GetValues(typeof(TermModelOperation));

		/// <summary>Number of goals being solved.
		/// </summary>
		public int SolvedGoalCount => goalList.Count;

		/// <summary>
		/// NonlinearResult.
		/// </summary>
		public NonlinearResult Result
		{
			get
			{
				if (_solution == null)
				{
					return NonlinearResult.Invalid;
				}
				return _solution.Result;
			}
		}

		/// <summary> 
		/// Solve the model using the given parameter instance
		/// </summary>
		public INonlinearSolution Solve(ISolverParameters parameters)
		{
			if (!(parameters is HybridLocalSearchParameters parameters2))
			{
				throw new ArgumentException(Resources.InvalidParams);
			}
			NonlinearResult result = RadiusSearch(parameters2);
			double[] array = new double[_allTerms.Count];
			for (int num = _allTerms.Count - 1; num >= 0; num--)
			{
				EvaluableTerm evaluableTerm = _allTerms[num];
				array[num] = evaluableTerm.ValueAsDouble;
			}
			_solution = new Solution
			{
				Values = array,
				Result = result,
				Goals = goalList.ToArray()
			};
			return _solution;
		}

		/// <summary> Shutdown the solver instance
		/// </summary>
		/// <remarks>Solver needs to dispose any unmanaged memory used upon this call.</remarks>
		public void Shutdown()
		{
			RequestTermination();
		}

		/// <summary>
		/// Return the value for the variable (or optionally row) with the specified vid. 
		/// </summary>
		/// <param name="vid">A variable id.</param>
		/// <returns>The value of the variable as a double.</returns>
		/// <remarks>
		/// This method can always be called with variable vids. Some solvers support row vids as well.
		/// The value may be finite, infinite, or indeterminate depending on the solution status.
		/// </remarks>
		public double GetValue(int vid)
		{
			return GetTerm(vid).ValueAsDouble;
		}

		/// <summary>
		/// Get the objective value of a goal.
		/// </summary>
		/// <param name="goalIndex">A goal id.</param>
		public double GetSolutionValue(int goalIndex)
		{
			int index = goalList[goalIndex].Index;
			return ((INonlinearSolution)this).GetValue(index);
		}

		/// <summary> Get the information for a solved goal.
		/// </summary>
		/// <param name="igoal">The goal index: 0 &lt;= goal index &lt; SolvedGoalCount.</param>
		/// <param name="key">The goal row key.</param>
		/// <param name="vid">The goal row vid.</param>
		/// <param name="fMinimize">Whether the goal is minimization goal.</param>
		/// <param name="fOptimal">Whether the goal is optimal.</param>
		public void GetSolvedGoal(int igoal, out object key, out int vid, out bool fMinimize, out bool fOptimal)
		{
			Goal goal = goalList[igoal];
			key = goal.Key;
			vid = goal.Index;
			fMinimize = goal.Minimize;
			fOptimal = false;
		}

		/// <summary>Set a solver-related property.
		/// </summary>
		/// <param name="propertyName">The name of the property to get.</param>
		/// <param name="vid">An index for the item of interest.</param>
		/// <param name="value">The property value.</param>
		public void SetProperty(string propertyName, int vid, object value)
		{
			throw new InvalidSolverPropertyException();
		}

		/// <summary>Get the value of a property.
		/// </summary>
		/// <param name="propertyName">The name of the property to get.</param>
		/// <param name="vid">An index for the item of interest.</param>
		/// <returns>The property value as a System.Object.</returns>
		public object GetProperty(string propertyName, int vid)
		{
			switch (propertyName)
			{
			case "Violation":
				return base.Violation;
			case "Step":
				return base.Step;
			default:
				throw new InvalidSolverPropertyException(string.Format(CultureInfo.InvariantCulture, Resources.PropertyNameIsNotSupported0, new object[1] { propertyName }), InvalidSolverPropertyReason.InvalidPropertyName);
			}
		}

		/// <summary>Generate a report
		/// </summary>
		/// <param name="context">The SolverContext.</param>
		/// <param name="solution">The Solution.</param>
		/// <param name="solutionMapping">A SolutionMapping instance.</param>
		/// <returns>Report for model solved by CompactQuasiNewtonSolver</returns>
		public Report GetReport(SolverContext context, Microsoft.SolverFoundation.Services.Solution solution, SolutionMapping solutionMapping)
		{
			PluginSolutionMapping pluginSolutionMapping = solutionMapping as PluginSolutionMapping;
			if (pluginSolutionMapping == null && solutionMapping != null)
			{
				throw new ArgumentException(Resources.SolutionMappingArgumentIsNotAPluginSolutionMappingObject, "solutionMapping");
			}
			return new HybridLocalSearchReport(context, this, solution, pluginSolutionMapping);
		}
	}
}
