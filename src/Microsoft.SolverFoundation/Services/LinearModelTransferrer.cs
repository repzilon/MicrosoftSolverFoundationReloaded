using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	internal class LinearModelTransferrer
	{
		private readonly SolverContext _context;

		private readonly ILinearModel _linearModel;

		private readonly Model _model;

		private readonly SmpsParser _smpsParser;

		private bool _secondStageStarted;

		private Term[] _variables;

		private TermStructure _structure;

		/// <summary>
		///
		/// </summary>
		/// <param name="model"></param>
		/// <param name="linearModel"></param>
		/// <param name="context"></param>
		/// <param name="smpsParser">null for NON SMPS file</param>
		internal LinearModelTransferrer(Model model, ILinearModel linearModel, SolverContext context, SmpsParser smpsParser)
		{
			_model = model;
			_linearModel = linearModel;
			_context = context;
			_smpsParser = smpsParser;
		}

		internal void TransferModel()
		{
			TransferVariables();
			AnalyzeStructure();
			TransferConstraints();
			TransferGoals();
		}

		private void AnalyzeStructure()
		{
			TermStructure termStructure = TermStructure.Linear | TermStructure.Quadratic;
			for (int i = 0; i < _variables.Length; i++)
			{
				if ((object)_variables[i] != null)
				{
					termStructure &= _variables[i].Structure;
				}
			}
			_structure = termStructure;
		}

		private void TransferGoals()
		{
			int num = 0;
			foreach (ILinearGoal goal3 in _linearModel.Goals)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				int index = goal3.Index;
				Term goal = ((_smpsParser == null) ? GetLinearSumTerm(index) : GetFullLinearSumTerm(index));
				if (_linearModel.IsQuadraticModel)
				{
					foreach (QuadraticEntry rowQuadraticEntry in _linearModel.GetRowQuadraticEntries(index))
					{
						if (_context._abortFlag)
						{
							throw new MsfException(Resources.Aborted);
						}
						int index2 = rowQuadraticEntry.Index1;
						int index3 = rowQuadraticEntry.Index2;
						Rational value = rowQuadraticEntry.Value;
						goal += new ConstantTerm(value) * _variables[index2] * _variables[index3];
					}
				}
				object keyFromIndex = _linearModel.GetKeyFromIndex(index);
				string name = OmlWriter.ReplaceInvalidChars(keyFromIndex.ToString()).ToString();
				GoalKind direction = (goal3.Minimize ? GoalKind.Minimize : GoalKind.Maximize);
				Goal goal2 = _model.AddGoal(name, direction, goal);
				goal2.Order = num++;
			}
		}

		private void TransferConstraints()
		{
			HashSet<int> hashSet = null;
			HashSet<int> hashSet2 = null;
			if (_linearModel is SimplexSolver simplexSolver)
			{
				if (simplexSolver._sos1Rows != null)
				{
					hashSet = new HashSet<int>(simplexSolver._sos1Rows);
				}
				if (simplexSolver._sos2Rows != null)
				{
					hashSet2 = new HashSet<int>(simplexSolver._sos2Rows);
				}
			}
			foreach (int rowIndex in _linearModel.RowIndices)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				if (_linearModel.IsGoal(rowIndex))
				{
					if ((hashSet != null && hashSet.Contains(rowIndex)) || (hashSet2 != null && hashSet2.Contains(rowIndex)))
					{
						throw new NotSupportedException();
					}
					continue;
				}
				object keyFromIndex = _linearModel.GetKeyFromIndex(rowIndex);
				string name = OmlWriter.ReplaceInvalidChars(keyFromIndex.ToString()).ToString();
				_linearModel.GetBounds(rowIndex, out var numLo, out var numHi);
				Term term = new ConstantTerm(numLo);
				Term term2 = new ConstantTerm(numHi);
				Term term3;
				if (_smpsParser != null)
				{
					term3 = GetFullLinearSumTerm(rowIndex);
					if (TryGetStochasticBound(rowIndex, out var parameter))
					{
						if (_linearModel.GetRowEntryCount(rowIndex) == 1)
						{
							throw new NotSupportedException();
						}
						if (numLo.IsFinite)
						{
							term = parameter;
						}
						if (numHi.IsFinite)
						{
							term2 = parameter;
						}
					}
				}
				else
				{
					term3 = GetLinearSumTerm(rowIndex);
				}
				if (hashSet != null && hashSet.Contains(rowIndex))
				{
					_model.AddConstraint(name, new Sos1RowTerm(new Term[3] { term, term3, term2 }, TermValueClass.Numeric));
				}
				else if (hashSet2 != null && hashSet2.Contains(rowIndex))
				{
					_model.AddConstraint(name, new Sos2RowTerm(new Term[3] { term, term3, term2 }, TermValueClass.Numeric));
				}
				else
				{
					_model.AddConstraint(name, new LessEqualTerm(new Term[3] { term, term3, term2 }, TermValueClass.Numeric));
				}
			}
		}

		private bool TryGetStochasticBound(int vid, out RandomParameter parameter)
		{
			parameter = null;
			DebugContracts.NonNull(_smpsParser);
			object keyFromIndex = _linearModel.GetKeyFromIndex(vid);
			if (_smpsParser.RandomParametersSubstitution.ContainsKey(keyFromIndex) && _smpsParser.RandomParametersSubstitution[keyFromIndex].ContainsKey(keyFromIndex))
			{
				parameter = CreateRandomParameter(_smpsParser.RandomParametersSubstitution[keyFromIndex][keyFromIndex], string.Concat(keyFromIndex, "RHS"));
				return true;
			}
			return false;
		}

		private bool IsRecourseDecision(object key)
		{
			DebugContracts.NonNull(_smpsParser);
			if (_smpsParser.PeriodsInfo._implicit)
			{
				if (_secondStageStarted)
				{
					return true;
				}
				if (_smpsParser.PeriodsInfo._isSecondStage.ContainsKey(key))
				{
					_secondStageStarted = _smpsParser.PeriodsInfo._isSecondStage[key];
				}
				return _secondStageStarted;
			}
			throw new NotImplementedException();
		}

		private void TransferVariables()
		{
			_variables = new Term[_linearModel.KeyCount];
			foreach (int variableIndex in _linearModel.VariableIndices)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				_linearModel.GetBounds(variableIndex, out var numLo, out var numHi);
				Domain domain = ((!_linearModel.GetIntegrality(variableIndex)) ? Domain.RealRange(numLo, numHi) : Domain.IntegerRange(numLo, numHi));
				object keyFromIndex = _linearModel.GetKeyFromIndex(variableIndex);
				string name = OmlWriter.ReplaceInvalidChars(keyFromIndex.ToString()).ToString();
				if (_smpsParser != null && IsRecourseDecision(keyFromIndex))
				{
					RecourseDecision recourseDecision = new RecourseDecision(domain, name);
					_model.AddDecision(recourseDecision);
					_variables[variableIndex] = recourseDecision;
				}
				else
				{
					Decision decision = new Decision(domain, name);
					_model.AddDecision(decision);
					_variables[variableIndex] = decision;
				}
			}
		}

		private Term GetLinearSumTerm(int vid)
		{
			return new RowTerm(_linearModel, _variables, vid, _structure);
		}

		/// <summary>
		/// This one actually gets the line as a sum and will be used just for stochastic
		/// as for large MPS its memory usage is too big
		/// </summary>
		/// <param name="vid"></param>
		/// <returns></returns>
		private Term GetFullLinearSumTerm(int vid)
		{
			Term term = null;
			object keyFromIndex = _linearModel.GetKeyFromIndex(vid);
			DebugContracts.NonNull(_smpsParser);
			foreach (LinearEntry rowEntry in _linearModel.GetRowEntries(vid))
			{
				Term term2 = _variables[rowEntry.Index];
				object keyFromIndex2 = _linearModel.GetKeyFromIndex(rowEntry.Index);
				Term term3;
				if (_smpsParser.RandomParametersSubstitution.ContainsKey(keyFromIndex) && _smpsParser.RandomParametersSubstitution[keyFromIndex].TryGetValue(keyFromIndex2, out var value))
				{
					string name = string.Concat(keyFromIndex, keyFromIndex2.ToString());
					RandomParameter randomParameter = CreateRandomParameter(value, name);
					term3 = randomParameter * term2;
				}
				else
				{
					term3 = new ConstantTerm(rowEntry.Value) * term2;
				}
				if ((object)term == null)
				{
					term = term3;
				}
				else
				{
					term += term3;
				}
			}
			return term ?? ((Term)0.0);
		}

		private RandomParameter CreateRandomParameter(SmpsParser.RandomParameterData randomData, string name)
		{
			try
			{
				switch (randomData._type)
				{
				case SmpsParser.RandomParameterType.Discrete:
					switch (randomData._modification)
					{
					default:
						throw new NotImplementedException();
					case SmpsParser.RandomParameterModification.Replace:
					{
						RandomParameter randomParameter = new ScenariosParameter(name, randomData._scenarios);
						_model.AddParameter(randomParameter);
						return randomParameter;
					}
					}
				default:
					throw new NotImplementedException();
				}
			}
			catch (ModelException innerException)
			{
				throw new MsfException(Resources.CouldNotParseSMPSModel, innerException);
			}
			catch (ArgumentException innerException2)
			{
				throw new MsfException(Resources.CouldNotParseSMPSModel, innerException2);
			}
		}
	}
}
