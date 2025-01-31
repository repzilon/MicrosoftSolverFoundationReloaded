using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Transfers a model to ITermModel.
	/// </summary>
	internal class TermModelTransferrer
	{
		private readonly SolverContext _context;

		private readonly ITermModel _termModel;

		private readonly Model _model;

		private Term[] _variables;

		private Dictionary<TermModelOperation, Func<Term[], Term>> _opToSfs;

		private Dictionary<int, Term> _vidToTerm;

		/// <summary>Create a new instance.
		/// </summary>
		internal TermModelTransferrer(Model model, ITermModel termModel, SolverContext context)
		{
			_model = model;
			_termModel = termModel;
			_context = context;
			_opToSfs = new Dictionary<TermModelOperation, Func<Term[], Term>>(35);
			_vidToTerm = new Dictionary<int, Term>(1 + termModel.RowCount + termModel.VariableCount);
			FillOperationToSfsMap();
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		private void FillOperationToSfsMap()
		{
			_opToSfs[TermModelOperation.Abs] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Abs(t[0]);
			};
			_opToSfs[TermModelOperation.And] = (Term[] t) => Model.And(t);
			_opToSfs[TermModelOperation.ArcCos] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.ArcCos(t[0]);
			};
			_opToSfs[TermModelOperation.ArcSin] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.ArcSin(t[0]);
			};
			_opToSfs[TermModelOperation.ArcTan] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.ArcTan(t[0]);
			};
			_opToSfs[TermModelOperation.Ceiling] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Ceiling(t[0]);
			};
			_opToSfs[TermModelOperation.Cos] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Cos(t[0]);
			};
			_opToSfs[TermModelOperation.Cosh] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Cosh(t[0]);
			};
			_opToSfs[TermModelOperation.Equal] = (Term[] t) => Model.Equal(t);
			_opToSfs[TermModelOperation.Exp] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Exp(t[0]);
			};
			_opToSfs[TermModelOperation.Floor] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Floor(t[0]);
			};
			_opToSfs[TermModelOperation.Greater] = (Term[] t) => Model.Greater(t);
			_opToSfs[TermModelOperation.GreaterEqual] = (Term[] t) => Model.GreaterEqual(t);
			_opToSfs[TermModelOperation.Identity] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Abs(t[0]);
			};
			_opToSfs[TermModelOperation.If] = delegate(Term[] t)
			{
				CheckArgs(t, 3);
				return Model.If(t[0], t[1], t[2]);
			};
			_opToSfs[TermModelOperation.Less] = (Term[] t) => Model.Less(t);
			_opToSfs[TermModelOperation.LessEqual] = (Term[] t) => Model.LessEqual(t);
			_opToSfs[TermModelOperation.Log] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Log(t[0]);
			};
			_opToSfs[TermModelOperation.Log10] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Log10(t[0]);
			};
			_opToSfs[TermModelOperation.Max] = (Term[] t) => Model.Max(t);
			_opToSfs[TermModelOperation.Min] = (Term[] t) => Model.Min(t);
			_opToSfs[TermModelOperation.Minus] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Negate(t[0]);
			};
			_opToSfs[TermModelOperation.Not] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Not(t[0]);
			};
			_opToSfs[TermModelOperation.Or] = (Term[] t) => Model.Or(t);
			_opToSfs[TermModelOperation.Plus] = (Term[] t) => Model.Sum(t);
			_opToSfs[TermModelOperation.Power] = delegate(Term[] t)
			{
				CheckArgs(t, 2);
				return Model.Power(t[0], t[1]);
			};
			_opToSfs[TermModelOperation.Quotient] = delegate(Term[] t)
			{
				CheckArgs(t, 2);
				return Model.Quotient(t[0], t[1]);
			};
			_opToSfs[TermModelOperation.Sin] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Sin(t[0]);
			};
			_opToSfs[TermModelOperation.Sinh] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Sinh(t[0]);
			};
			_opToSfs[TermModelOperation.Sqrt] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Sqrt(t[0]);
			};
			_opToSfs[TermModelOperation.Tan] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Tan(t[0]);
			};
			_opToSfs[TermModelOperation.Tanh] = delegate(Term[] t)
			{
				CheckArgs(t, 1);
				return Model.Tanh(t[0]);
			};
			_opToSfs[TermModelOperation.Times] = (Term[] t) => Model.Product(t);
			_opToSfs[TermModelOperation.Unequal] = (Term[] t) => Model.AllDifferent(t);
		}

		internal void TransferModel()
		{
			_vidToTerm.Clear();
			TransferVariables();
			TransferConstraints();
			TransferGoals();
		}

		private void TransferGoals()
		{
			int num = 0;
			foreach (IGoal goal2 in _termModel.Goals)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				int index = goal2.Index;
				object obj = _termModel.GetKeyFromIndex(index);
				if (obj == null)
				{
					obj = "__g" + num.ToString(CultureInfo.InvariantCulture);
				}
				string name = OmlWriter.ReplaceInvalidChars(obj.ToString()).ToString();
				GoalKind direction = (goal2.Minimize ? GoalKind.Minimize : GoalKind.Maximize);
				Goal goal = _model.AddGoal(name, direction, GetTermForVid(index));
				goal.Order = num++;
			}
		}

		private void TransferConstraints()
		{
			int num = 0;
			foreach (int rowIndex in _termModel.RowIndices)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				if (_termModel.IsConstant(rowIndex))
				{
					continue;
				}
				_termModel.GetBounds(rowIndex, out var lower, out var upper);
				if (lower.IsFinite || upper.IsFinite)
				{
					num++;
					object obj = _termModel.GetKeyFromIndex(rowIndex);
					if (obj == null)
					{
						obj = "__c" + num.ToString(CultureInfo.InvariantCulture);
					}
					string name = OmlWriter.ReplaceInvalidChars(obj.ToString()).ToString();
					Term term = new ConstantTerm(lower);
					Term term2 = new ConstantTerm(upper);
					Term termForVid = GetTermForVid(rowIndex);
					_model.AddConstraint(name, new LessEqualTerm(new Term[3] { term, termForVid, term2 }, TermValueClass.Numeric));
				}
			}
		}

		private void TransferVariables()
		{
			_variables = new Term[_termModel.KeyCount];
			int num = 0;
			foreach (int variableIndex in _termModel.VariableIndices)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				num++;
				_termModel.GetBounds(variableIndex, out var lower, out var upper);
				Domain domain = ((!_termModel.GetIntegrality(variableIndex)) ? Domain.RealRange(lower, upper) : Domain.IntegerRange(lower, upper));
				object obj = _termModel.GetKeyFromIndex(variableIndex);
				if (obj == null)
				{
					obj = "__d" + num.ToString(CultureInfo.InvariantCulture);
				}
				string name = OmlWriter.ReplaceInvalidChars(obj.ToString()).ToString();
				Decision decision = new Decision(domain, name);
				_model.AddDecision(decision);
				_variables[variableIndex] = decision;
			}
		}

		private void CheckArgs(Term[] terms, int count)
		{
			if (terms.Length != count)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.Expected0ArgumentsButSaw1, new object[2] { count, terms.Length }));
			}
		}

		internal Term GetTermForVid(int vid)
		{
			if (_vidToTerm.TryGetValue(vid, out var value))
			{
				return value;
			}
			if (_termModel.IsOperation(vid))
			{
				TermModelOperation operation = _termModel.GetOperation(vid);
				int operandCount = _termModel.GetOperandCount(vid);
				Term[] arg = GetOperandTerms(vid, operandCount).ToArray();
				if (!_opToSfs.ContainsKey(operation))
				{
					throw new NotSupportedException(Resources.UnrecognizedTerm);
				}
				value = _opToSfs[operation](arg);
			}
			else if (_termModel.IsConstant(vid))
			{
				_termModel.GetBounds(vid, out var lower, out var _);
				value = new ConstantTerm(lower);
			}
			else
			{
				if (_termModel.IsRow(vid))
				{
					throw new NotSupportedException(Resources.UnrecognizedTerm);
				}
				value = _variables[vid];
			}
			_vidToTerm[vid] = value;
			return value;
		}

		private IEnumerable<Term> GetOperandTerms(int vid, int operandCount)
		{
			foreach (int operandVid in _termModel.GetOperands(vid))
			{
				yield return GetTermForVid(operandVid);
			}
		}
	}
}
