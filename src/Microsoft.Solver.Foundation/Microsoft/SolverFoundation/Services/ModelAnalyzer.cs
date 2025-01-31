using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	internal class ModelAnalyzer
	{
		/// <summary>
		/// This visitor is used for determine all operations included in the model.
		/// First bits of the BitArray is for existence of each operator. 
		/// Last bit (_isDifferentiableBit)is for IfDifferentiable of the model 
		/// </summary>
		/// <remarks>return value is not in used</remarks>
		private class OperatorCollectingVisitor : ITermVisitor<bool, BitArray>
		{
			private int _isDifferentiableBit;

			internal OperatorCollectingVisitor(int bitArraySize)
			{
				_isDifferentiableBit = bitArraySize - 1;
			}

			public bool Visit(Decision term, BitArray arg)
			{
				return true;
			}

			public bool Visit(RecourseDecision term, BitArray arg)
			{
				return true;
			}

			public bool Visit(Parameter term, BitArray arg)
			{
				return true;
			}

			public bool Visit(RandomParameter term, BitArray arg)
			{
				return true;
			}

			public bool Visit(ConstantTerm term, BitArray arg)
			{
				return true;
			}

			public bool Visit(NamedConstantTerm term, BitArray arg)
			{
				return true;
			}

			public bool Visit(StringConstantTerm term, BitArray arg)
			{
				return true;
			}

			public bool Visit(BoolConstantTerm term, BitArray arg)
			{
				return true;
			}

			public bool Visit(EnumeratedConstantTerm term, BitArray arg)
			{
				return true;
			}

			public bool Visit(IdentityTerm term, BitArray arg)
			{
				return term._input.Visit(this, arg);
			}

			public bool Visit(OperatorTerm term, BitArray arg)
			{
				if (!term.HasStructure(TermStructure.Constant))
				{
					int operation = (int)term.Operation;
					if (!arg.Get(operation))
					{
						arg.Set(operation, value: true);
						arg.Set(_isDifferentiableBit, value: true);
					}
					Term[] inputs = term.Inputs;
					foreach (Term term2 in inputs)
					{
						term2.Visit(this, arg);
					}
				}
				return true;
			}

			public bool Visit(IndexTerm term, BitArray arg)
			{
				return true;
			}

			/// <summary>
			/// Should not get here, as i don't care about itarations so i don't
			/// call it from ForEachTerm and ForEachWhereTerm
			/// </summary>
			public bool Visit(IterationTerm term, BitArray arg)
			{
				throw new NotImplementedException();
			}

			public bool Visit(ForEachTerm term, BitArray arg)
			{
				return term._valueExpression.Visit(this, arg);
			}

			public bool Visit(ForEachWhereTerm term, BitArray arg)
			{
				return term._valueExpression.Visit(this, arg);
			}

			public bool Visit(RowTerm term, BitArray arg)
			{
				return true;
			}

			public bool Visit(ElementOfTerm term, BitArray arg)
			{
				return true;
			}

			public bool Visit(Tuples term, BitArray arg)
			{
				return true;
			}
		}

		private Model _model;

		private ModelType _modelType;

		private BitArray _operators;

		private readonly SolverCapability[] _defaultCapabilities = new SolverCapability[7]
		{
			SolverCapability.LP,
			SolverCapability.MILP,
			SolverCapability.QP,
			SolverCapability.MIQP,
			SolverCapability.CP,
			SolverCapability.NLP,
			SolverCapability.MINLP
		};

		/// <summary>Is model differentiable
		/// </summary>
		/// <remarks>Assme calling to AnalyzeOperators before</remarks>
		internal bool IsDifferentiable
		{
			get
			{
				DebugContracts.NonNull(_operators);
				return _operators.Get(_operators.Count - 1);
			}
		}

		/// <summary>Get all Operators of model
		/// </summary>
		/// <remarks>Assme calling to AnalyzeOperators before</remarks>
		internal IEnumerable<Operator> Operators
		{
			get
			{
				DebugContracts.NonNull(_operators);
				for (int i = 0; i < _operators.Count - 1; i++)
				{
					if (_operators.Get(i))
					{
						yield return (Operator)i;
					}
				}
			}
		}

		internal ModelAnalyzer(Model model)
		{
			DebugContracts.NonNull(model);
			_model = model;
		}

		internal bool SupportsCapability(SolverCapability capability)
		{
			switch (capability)
			{
			case SolverCapability.LP:
				return (_modelType & ModelType.Lp) != 0;
			case SolverCapability.MILP:
				return (_modelType & ModelType.Mip) != 0;
			case SolverCapability.QP:
				return (_modelType & ModelType.Qp) != 0;
			case SolverCapability.MIQP:
				return (_modelType & ModelType.Miqp) != 0;
			case SolverCapability.CP:
				return (_modelType & ModelType.Csp) != 0;
			case SolverCapability.NLP:
				return (_modelType & ModelType.Nlp) != 0;
			case SolverCapability.MINLP:
				return (_modelType & ModelType.Minlp) != 0;
			default:
				return false;
			}
		}

		internal IEnumerable<SolverCapability> GetCapabilities()
		{
			try
			{
				SolverCapability[] defaultCapabilities = _defaultCapabilities;
				foreach (SolverCapability capability in defaultCapabilities)
				{
					if (SupportsCapability(capability))
					{
						yield return capability;
					}
				}
			}
			finally
			{
			}
		}

		internal ModelType GetModelType()
		{
			return _modelType;
		}

		/// <summary>Analyzes model operators
		/// </summary>
		internal void AnalyzeOperators()
		{
			if (_operators != null)
			{
				return;
			}
			int bitArraySize = GetBitArraySize();
			_operators = new BitArray(bitArraySize);
			_operators.Set(bitArraySize - 1, value: true);
			OperatorCollectingVisitor visitor = new OperatorCollectingVisitor(bitArraySize);
			foreach (Constraint item in _model.AllConstraints.Where((Constraint constraint) => constraint.Enabled))
			{
				item._term.Visit(visitor, _operators);
			}
			foreach (Goal item2 in _model.AllGoals.Where((Goal goal) => goal.Enabled))
			{
				item2._term.Visit(visitor, _operators);
			}
		}

		private static int GetBitArraySize()
		{
			int num = 37;
			return num + 2;
		}

		internal void Analyze()
		{
			_modelType = ModelType.Csp | ModelType.Lp | ModelType.Qp | ModelType.Mip | ModelType.Nlp | ModelType.Minlp | ModelType.Miqp | ModelType.Differentiable;
			AnalyzeDecisions();
			AnalyzeConstraints();
			AnalyzeGoals();
			if (_model.IsStochastic)
			{
				if (!_model.IsValidStochastic)
				{
					throw new ModelException(Resources.StochasticNeedRecourseDecisionsAndRandomParameters);
				}
				_modelType |= ModelType.Stochastic;
			}
		}

		private void AnalyzeDecisions()
		{
			foreach (Decision allDecision in _model.AllDecisions)
			{
				if (allDecision._domain.IntRestricted)
				{
					_modelType &= ~(ModelType.Lp | ModelType.Qp | ModelType.Nlp);
					_modelType |= ModelType.Bounded;
				}
				if (allDecision._domain.ValidValues != null)
				{
					_modelType &= ~(ModelType.Lp | ModelType.Qp | ModelType.Mip | ModelType.Miqp);
					_modelType |= ModelType.Bounded;
				}
				if (allDecision._domain.MinValue.IsFinite || allDecision._domain.MaxValue.IsFinite)
				{
					_modelType |= ModelType.Bounded;
				}
			}
			foreach (RecourseDecision allRecourseDecision in _model.AllRecourseDecisions)
			{
				if (allRecourseDecision._domain.IntRestricted)
				{
					_modelType &= ~(ModelType.Lp | ModelType.Qp | ModelType.Nlp);
				}
				else
				{
					_modelType &= ~ModelType.Csp;
				}
				if (allRecourseDecision._domain.ValidValues != null)
				{
					_modelType &= ~(ModelType.Lp | ModelType.Qp | ModelType.Mip | ModelType.Miqp);
				}
			}
		}

		private void AnalyzeGoals()
		{
			foreach (Goal item in _model.AllGoals.Where((Goal goal) => goal.Enabled))
			{
				TermStructure structure = item.Term.Structure;
				if ((structure & (TermStructure.Constant | TermStructure.Linear | TermStructure.Quadratic)) == 0)
				{
					_modelType &= ~(ModelType.Qp | ModelType.Miqp);
				}
				if ((structure & (TermStructure.Constant | TermStructure.Linear)) == 0)
				{
					_modelType &= ~(ModelType.Lp | ModelType.Mip);
				}
				if ((structure & (TermStructure.Constant | TermStructure.Differentiable)) == 0)
				{
					_modelType &= ~ModelType.Differentiable;
				}
				if ((structure & TermStructure.Integer) == 0)
				{
					_modelType &= ~ModelType.Csp;
				}
			}
		}

		private void AnalyzeConstraints()
		{
			foreach (Constraint item in _model.AllConstraints.Where((Constraint constraint) => constraint.Enabled))
			{
				TermStructure structure = item.Term.Structure;
				if ((structure & (TermStructure.Constant | TermStructure.Linear | TermStructure.LinearInequality | TermStructure.LinearConstraint | TermStructure.Sos1 | TermStructure.Sos2)) == 0)
				{
					_modelType &= ~(ModelType.Lp | ModelType.Mip);
				}
				if ((structure & TermStructure.Sos1) == TermStructure.Sos1)
				{
					_modelType |= ModelType.Sos1;
				}
				if ((structure & TermStructure.Sos2) == TermStructure.Sos2)
				{
					_modelType |= ModelType.Sos2;
				}
				if ((structure & (TermStructure.Constant | TermStructure.Linear | TermStructure.LinearInequality | TermStructure.LinearConstraint)) == 0)
				{
					_modelType &= ~(ModelType.Qp | ModelType.Miqp);
				}
				if ((structure & (TermStructure.Constant | TermStructure.Differentiable | TermStructure.DifferentiableConstraint)) == 0)
				{
					_modelType &= ~ModelType.Differentiable;
				}
				if ((structure & TermStructure.Integer) == 0)
				{
					_modelType &= ~ModelType.Csp;
				}
				_modelType |= ModelType.Constrained;
			}
		}
	}
}
