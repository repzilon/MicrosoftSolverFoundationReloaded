using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Representation of the deductions made by propagation and of the
	///   causality between them, inspired by what the implication graphs used
	///   by SAT solvers. Allows conflict analysis.
	/// </summary>
	internal class ImplicationGraph
	{
		/// <summary>
		///   delegates called when a var is iterated over
		///   by conflict analysis
		/// </summary>
		public delegate void VarIteratedOver(DiscreteVariable x);

		/// <summary>
		///   delegates called at the end of conflict analysis;
		///   receives the explanation of conflict
		/// </summary>
		public delegate void ExplanationFound(VariableGroup g);

		/// <summary>
		///   delegates called when constraint is part of conflict analysis
		/// </summary>
		public delegate void ConstraintInvolvedInConflict(DisolverConstraint cstr);

		public struct Node
		{
			public DiscreteVariable Var;

			public VariableGroup Predecessors;

			public DisolverConstraint ConflictConstraint;

			public Interval Range;

			public Node(DiscreteVariable x, Cause g, Interval range)
			{
				Var = x;
				Predecessors = g.Signature;
				ConflictConstraint = g.Constraint;
				Range = range;
			}
		}

		private const int MaxSize = 100000;

		/// <summary>
		///  Only the last level of the implication graph is actually
		///  constructed. This is done on demand (by recmputation)
		///  when the conflict is effectively detected
		/// </summary>
		private List<Node> _graph;

		private Problem _problem;

		private LookupMap<DiscreteVariable, int> _occurrenceCount;

		private SubSet<DiscreteVariable> _conflictSet;

		private event VarIteratedOver _varIteratedOver;

		private event ExplanationFound _explanationFound;

		private event ConstraintInvolvedInConflict _involvedConstraint;

		public ImplicationGraph(Problem p)
		{
			_problem = p;
			IndexedCollection<DiscreteVariable> discreteVariables = _problem.DiscreteVariables;
			_occurrenceCount = new LookupMap<DiscreteVariable, int>(discreteVariables);
			_conflictSet = new SubSet<DiscreteVariable>(discreteVariables);
			_graph = new List<Node>();
			foreach (DiscreteVariable item in _problem.DiscreteVariables.Enumerate())
			{
				_occurrenceCount[item] = 0;
			}
		}

		/// <summary>
		///   Code that is passed to this method will be called
		///   for every variable that is met during the conflict analysis 
		/// </summary>
		public void SubscribeToVarIteratedOver(VarIteratedOver f)
		{
			_varIteratedOver += f;
		}

		/// <summary>
		///   Code that is passed to this method will be called
		///   every time conflict analysis terminates and computes an explanation
		/// </summary>
		public void SubscribeToExplanation(ExplanationFound e)
		{
			_explanationFound += e;
		}

		/// <summary>
		///   Code that is passed to this method will be called
		///   for every constraint that is met during the conflict analysis 
		/// </summary>
		public void SubscribeToConstraintInvolvedInConflict(ConstraintInvolvedInConflict c)
		{
			_involvedConstraint += c;
		}

		/// <summary>
		///   Main conflict analysis method:
		///   Computes a unit implication point and updates the weights
		///   accordingly
		/// </summary>
		public ConflictDiagnostic AnalyseConflictUIP(TreeSearchAlgorithm algo)
		{
			_problem.SubscribeToVariableModification(AddVariableModification);
			algo.Recompute();
			_problem.UnsubscribeToVariableModification(AddVariableModification);
			if (_graph.Count == 100000)
			{
				_graph.Clear();
				return new ConflictDiagnostic(status: false, Cause.Decision, Interval.Empty());
			}
			int count = _graph.Count;
			if (count == 0)
			{
				return new ConflictDiagnostic(status: true, Cause.RootLevelDecision, Interval.Empty());
			}
			for (int i = 0; i < count; i++)
			{
				DiscreteVariable var = _graph[i].Var;
				_occurrenceCount[var] += 1;
			}
			DiscreteVariable var2 = _graph[count - 1].Var;
			_conflictSet.Add(var2);
			if (this._varIteratedOver != null)
			{
				this._varIteratedOver(var2);
			}
			if (this._involvedConstraint != null)
			{
				DisolverConstraint conflictConstraint = _graph[count - 1].ConflictConstraint;
				if (conflictConstraint != null)
				{
					this._involvedConstraint(conflictConstraint);
				}
			}
			for (int num = count - 1; num > 0; num--)
			{
				DiscreteVariable var3 = _graph[num].Var;
				VariableGroup predecessors = _graph[num].Predecessors;
				if (_conflictSet.Contains(var3))
				{
					foreach (DiscreteVariable variable in predecessors.GetVariables())
					{
						if (predecessors == VariableGroup.EmptyGroup())
						{
							continue;
						}
						_conflictSet.Add(variable);
						if (this._varIteratedOver != null)
						{
							this._varIteratedOver(variable);
						}
						if (this._involvedConstraint != null)
						{
							DisolverConstraint conflictConstraint2 = _graph[num].ConflictConstraint;
							if (conflictConstraint2 != null)
							{
								this._involvedConstraint(conflictConstraint2);
							}
						}
					}
				}
			}
			DiscreteVariable var4 = _graph[0].Var;
			Interval range = _graph[0].Range;
			int num2 = 0;
			foreach (DiscreteVariable item in _conflictSet.Enumerate())
			{
				if (!item.IsInInitialState())
				{
					num2++;
				}
			}
			int num3 = num2 + ((!_conflictSet.Contains(var4)) ? 1 : 0);
			DiscreteVariable[] array = new DiscreteVariable[num3];
			array[0] = var4;
			int num4 = 1;
			foreach (DiscreteVariable item2 in _conflictSet.Enumerate())
			{
				if (item2 != var4 && !item2.IsInInitialState())
				{
					array[num4] = item2;
					num4++;
				}
			}
			_conflictSet.Clear();
			_graph.Clear();
			VariableGroup g = new VariableGroup(array);
			if (this._explanationFound != null)
			{
				this._explanationFound(g);
			}
			return new ConflictDiagnostic(status: true, new Cause(null, g), range);
		}

		/// <summary>
		///   Specifies that a new deduction has happened at the current level:
		///   variable x was reduced because of c
		/// </summary>
		public void AddVariableModification(VariableModification c)
		{
			if (c.Reason.Signature == VariableModification.RootLevelDeduction)
			{
				return;
			}
			DiscreteVariable var = c.Var;
			Interval range;
			if (var is IntegerVariable integerVariable)
			{
				range = new Interval(integerVariable.LowerBound, integerVariable.UpperBound);
			}
			else
			{
				BooleanVariable booleanVariable = var as BooleanVariable;
				switch (booleanVariable.Status)
				{
				case BooleanVariableState.True:
					range = new Interval(1L, 1L);
					break;
				case BooleanVariableState.False:
					range = new Interval(0L, 0L);
					break;
				default:
					range = new Interval(0L, 1L);
					break;
				}
			}
			if (_graph.Count < 100000)
			{
				_graph.Add(new Node(var, c.Reason, range));
			}
		}

		[Conditional("DEBUG")]
		private void CheckZero()
		{
			foreach (DiscreteVariable item in _problem.DiscreteVariables.Enumerate())
			{
				_ = item;
			}
		}
	}
}
