using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> TermModel is a default implementation of ITermModel.
	/// TermModel can be inherited by a plug-in solver class.
	/// </summary>
	public class TermModel : RowVariableGoalModel, ITermModel, IRowVariableModel, IGoalModel
	{
		internal struct Var
		{
			public bool isConstant;

			public Rational[] possibleValues;

			public TermModelOperation op;

			public int operandStart;

			public int operandCount;
		}

		private const int defaultVarsArraySize = 20;

		private const int defaultOperandCountArraySize = 40;

		internal Var[] _vars;

		internal int[] _operands;

		private int _operandCount;

		internal int MaxVid => _vidLim - 1;

		/// <summary>Create a new instance.
		/// </summary>
		public TermModel(IEqualityComparer<object> comparer)
			: base(comparer)
		{
			_vars = new Var[20];
			_operands = new int[40];
			_mpvidgoal = new Dictionary<int, Goal>();
		}

		internal override int AllocVid()
		{
			int result = base.AllocVid();
			EnsureArraySize(ref _vars, _vidLim);
			return result;
		}

		/// <summary>Adds an operation row to the model.
		/// </summary>
		public bool AddOperation(TermModelOperation op, out int vidNew, int vid1)
		{
			base.AddRow(null, out vidNew);
			int operandCount = _operandCount;
			_operandCount++;
			EnsureArraySize(ref _operands, _operandCount);
			_operands[operandCount] = vid1;
			ref Var reference = ref _vars[vidNew];
			reference = new Var
			{
				isConstant = false,
				op = op,
				operandStart = operandCount,
				operandCount = 1
			};
			return true;
		}

		/// <summary>Adds an operation row to the model.
		/// </summary>
		public bool AddOperation(TermModelOperation op, out int vidNew, int vid1, int vid2)
		{
			base.AddRow(null, out vidNew);
			int operandCount = _operandCount;
			_operandCount += 2;
			EnsureArraySize(ref _operands, _operandCount);
			_operands[operandCount] = vid1;
			_operands[operandCount + 1] = vid2;
			ref Var reference = ref _vars[vidNew];
			reference = new Var
			{
				isConstant = false,
				op = op,
				operandStart = operandCount,
				operandCount = 2
			};
			return true;
		}

		/// <summary>Adds an operation row to the model.
		/// </summary>
		public bool AddOperation(TermModelOperation op, out int vidNew, int vid1, int vid2, int vid3)
		{
			base.AddRow(null, out vidNew);
			int operandCount = _operandCount;
			_operandCount += 3;
			EnsureArraySize(ref _operands, _operandCount);
			_operands[operandCount] = vid1;
			_operands[operandCount + 1] = vid2;
			_operands[operandCount + 2] = vid3;
			ref Var reference = ref _vars[vidNew];
			reference = new Var
			{
				isConstant = false,
				op = op,
				operandStart = operandCount,
				operandCount = 3
			};
			return true;
		}

		/// <summary>Adds an operation row to the model.
		/// </summary>
		/// <remarks>
		/// This overload is supported for the following TermModelOperation values:
		/// And, Equal, Greater, GreaterEqual, Less, LessEqual, Max, Min, Or, Plus, Times, Unequal.
		/// </remarks>
		public bool AddOperation(TermModelOperation op, out int vidNew, params int[] vids)
		{
			base.AddRow(null, out vidNew);
			int operandCount = _operandCount;
			_operandCount += vids.Length;
			EnsureArraySize(ref _operands, _operandCount);
			for (int i = 0; i < vids.Length; i++)
			{
				_operands[operandCount + i] = vids[i];
			}
			ref Var reference = ref _vars[vidNew];
			reference = new Var
			{
				isConstant = false,
				op = op,
				operandStart = operandCount,
				operandCount = vids.Length
			};
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <param name="vid"></param>
		/// <param name="lower"></param>
		/// <param name="upper"></param>
		/// <param name="isInteger"></param>
		/// <returns></returns>
		public bool AddVariable(object key, out int vid, Rational lower, Rational upper, bool isInteger)
		{
			if (!base.AddVariable(key, out vid))
			{
				return false;
			}
			_mpvidnumLo[vid] = lower;
			_mpvidnumHi[vid] = upper;
			AssignVidFlag(vid, VidFlags.Integer, isInteger, ref m_cvidInt);
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <param name="vid"></param>
		/// <param name="possibleValues"></param>
		/// <returns></returns>
		public bool AddVariable(object key, out int vid, IEnumerable<Rational> possibleValues)
		{
			if (!base.AddVariable(key, out vid))
			{
				return false;
			}
			Rational[] array = possibleValues.ToArray();
			Array.Sort(array);
			if (array.Length == 0)
			{
				throw new NotSupportedException();
			}
			_vars[vid].possibleValues = array;
			ref Rational reference = ref _mpvidnumLo[vid];
			reference = array[0];
			ref Rational reference2 = ref _mpvidnumHi[vid];
			reference2 = array[array.Length - 1];
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="value"></param>
		/// <param name="vid"></param>
		/// <returns></returns>
		public bool AddConstant(Rational value, out int vid)
		{
			base.AddRow(null, out vid);
			ref Rational reference = ref _mpvidnumLo[vid];
			ref Rational reference2 = ref _mpvidnumHi[vid];
			reference = (reference2 = (_mpvidnum[vid] = value));
			_vars[vid].isConstant = true;
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="vid"></param>
		/// <returns></returns>
		public bool IsOperation(int vid)
		{
			ValidateVid(vid);
			if (_mpvidvi[vid].IsRow)
			{
				return !_vars[vid].isConstant;
			}
			return false;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="vid"></param>
		/// <returns></returns>
		public bool IsConstant(int vid)
		{
			ValidateVid(vid);
			if (_mpvidvi[vid].IsRow)
			{
				return _vars[vid].isConstant;
			}
			return false;
		}

		/// <summary>Get the operation for the specified row.
		/// </summary>
		/// <param name="vid"></param>
		/// <returns>The TermModelOperation.</returns>
		public TermModelOperation GetOperation(int vid)
		{
			ValidateVid(vid);
			if (!IsOperation(vid))
			{
				throw new NotSupportedException();
			}
			return _vars[vid].op;
		}

		/// <summary>Get the operand count for the specified row.
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <returns>The operand count.</returns>
		public int GetOperandCount(int vid)
		{
			ValidateVid(vid);
			if (!IsOperation(vid))
			{
				throw new NotSupportedException();
			}
			return _vars[vid].operandCount;
		}

		/// <summary>Get the operands for the specified row.
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <returns>An IEnumerable containing the operand vids.</returns>
		public IEnumerable<int> GetOperands(int vid)
		{
			ValidateVid(vid);
			int operandStart = _vars[vid].operandStart;
			for (int i = 0; i < _vars[vid].operandCount; i++)
			{
				yield return _operands[operandStart + i];
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="vid"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public int GetOperand(int vid, int index)
		{
			ValidateVid(vid);
			if (!IsOperation(vid))
			{
				throw new NotSupportedException();
			}
			if (index < 0 || index > _vars[vid].operandCount)
			{
				throw new NotSupportedException();
			}
			return _operands[_vars[vid].operandStart + index];
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <param name="vid"></param>
		/// <returns></returns>
		public override bool AddRow(object key, out int vid)
		{
			throw new NotSupportedException();
		}

		/// <summary>Set the bounds for a vid.</summary>
		/// <remarks>
		/// Logically, a vid may have an upper bound of Infinity and/or a lower bound of -Infinity. 
		/// Specifying any other non-finite values for bounds should be avoided. 
		/// If a vid has a lower bound that is greater than its upper bound, the model is automatically infeasible, 
		/// and ArgumentException is thrown.  
		/// </remarks>
		/// <param name="vid">A vid.</param>
		/// <param name="lower">The lower bound.</param>
		/// <param name="upper">The upper bound.</param>
		public override void SetBounds(int vid, Rational lower, Rational upper)
		{
			ValidateVid(vid);
			if (_vars[vid].isConstant)
			{
				throw new NotSupportedException();
			}
			base.SetBounds(vid, lower, upper);
		}

		/// <summary>Set or adjust the lower bound of the vid. 
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <param name="lower">The lower bound.</param>
		public override void SetLowerBound(int vid, Rational lower)
		{
			ValidateVid(vid);
			if (_vars[vid].isConstant)
			{
				throw new NotSupportedException();
			}
			base.SetLowerBound(vid, lower);
		}

		/// <summary>Set or adjust the upper bound of the vid. 
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <param name="upper">The upper bound.</param>
		public override void SetUpperBound(int vid, Rational upper)
		{
			ValidateVid(vid);
			if (_vars[vid].isConstant)
			{
				throw new NotSupportedException();
			}
			base.SetUpperBound(vid, upper);
		}

		/// <summary>Sets the default value for a vid.
		/// </summary>
		/// <remarks>
		/// The default value for a vid is Indeterminate. An IRowVariableModel can be used to represent not just a model, 
		/// but also a current state for the model’s (user and row) variables. 
		/// The state associates with each vid a current value represented as a Rational. 
		/// This state may be used as a starting point when solving, and may be updated by a solve attempt. 
		/// Some solvers may ignore this initial state for rows and even for variables.
		/// </remarks>
		/// <param name="vid">A vid.</param>
		/// <param name="value">The default value for the variable.</param>    
		public override void SetValue(int vid, Rational value)
		{
			ValidateVid(vid);
			if (_vars[vid].isConstant)
			{
				throw new NotSupportedException();
			}
			base.SetValue(vid, value);
		}

		internal string Dump()
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < _vidLim; i++)
			{
				stringBuilder.AppendFormat("{0,4}$ ", i);
				if (!IsRow(i))
				{
					if (_vars[i].possibleValues != null)
					{
						stringBuilder.Append("VA");
					}
					else if (GetIntegrality(i))
					{
						stringBuilder.Append("VI");
					}
					else
					{
						stringBuilder.Append("VR");
					}
				}
				else if (_vars[i].isConstant)
				{
					stringBuilder.Append("RC");
				}
				else
				{
					stringBuilder.Append("RO");
				}
				if (!_mpvidnum[i].IsIndeterminate)
				{
					stringBuilder.AppendFormat(" {0}", _mpvidnum[i]);
				}
				if (!IsConstant(i) && (_mpvidnumLo[i] != Rational.NegativeInfinity || _mpvidnumHi[i] != Rational.PositiveInfinity))
				{
					stringBuilder.AppendFormat(" [{0} {1}]", _mpvidnumLo[i], _mpvidnumHi[i]);
				}
				if (IsGoal(i))
				{
					stringBuilder.Append(" !");
				}
				stringBuilder.Append("\t");
				if (!IsRow(i))
				{
					if (_vars[i].possibleValues != null)
					{
						stringBuilder.AppendFormat("({2})", i, _mpvidnum[i], _vars[i].possibleValues);
					}
				}
				else if (!_vars[i].isConstant)
				{
					stringBuilder.AppendFormat("{0}\t", _vars[i].op);
					for (int j = 0; j < _vars[i].operandCount; j++)
					{
						stringBuilder.AppendFormat("{0,4}$ ", _operands[_vars[i].operandStart + j]);
					}
				}
				stringBuilder.AppendLine();
			}
			return stringBuilder.ToString();
		}
	}
}
