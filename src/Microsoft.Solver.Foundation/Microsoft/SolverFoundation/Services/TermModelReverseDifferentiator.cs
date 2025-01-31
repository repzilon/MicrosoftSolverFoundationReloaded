using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Uses Reverse Differentiation to symbolically differentiate an ITermModel.
	/// </summary>
	internal class TermModelReverseDifferentiator : TermModelDifferentiator
	{
		private delegate int DifferentiateChildWRTParentDelegate(ITermModel model, int childVid, int parentVid, int[] childOperands);

		private int[][] _dRequestedRowdRowVids;

		private List<GradientEntry>[] _dChilddRowVids;

		private int[] _diffRowsDAGOrder;

		private int _maxNumChildren = 100;

		private int _constOneVid = -1;

		private int _constNegOneVid = -1;

		/// <summary>This method starts the differentiation work. SFS calls this method to differentiate the model. 
		/// The other Differentiate methods just load rows and variables of differentiation. 
		/// </summary>
		/// <param name="model"></param>
		public override void Differentiate(ITermModel model)
		{
			ValidateModel(model);
			modelRowsSorted = model.Indices.ToArray();
			if (!TermModelDifferentiator.IsOrderGuaranteed(model))
			{
				TermModelDifferentiator.EnsureSorted(modelRowsSorted);
			}
			if (differentiatedRows == null)
			{
				SetDifferentiatedRowsFromModel(model);
			}
			if (diffVarsSorted == null)
			{
				SetDiffVarsSortedFromModel(model);
			}
			ValidateModelRows(model, differentiatedRows);
			ValidateModelVars(model, diffVarsSorted);
			_dRequestedRowdRowVids = new int[modelRowsSorted.Length][];
			_dChilddRowVids = new List<GradientEntry>[modelRowsSorted.Length];
			model.AddConstant(Rational.One, out _constOneVid);
			model.AddConstant(-Rational.One, out _constNegOneVid);
			TopologicalSort(model);
			for (int num = _diffRowsDAGOrder.Length - 1; num >= 0; num--)
			{
				int rowIndex = GetRowIndex(modelRowsSorted, _diffRowsDAGOrder[num]);
				DifferentiateRow(model, _diffRowsDAGOrder[num], rowIndex);
			}
			FormTermModelGradientVids();
			Clean();
		}

		private void Clean()
		{
			modelRowsSorted = null;
			_diffRowsDAGOrder = null;
			diffVarsSorted = null;
			_dRequestedRowdRowVids = null;
			_dChilddRowVids = null;
		}

		private bool CheckModelSorted()
		{
			if (modelRowsSorted.Length < 2)
			{
				return true;
			}
			for (int i = 1; i < modelRowsSorted.Length; i++)
			{
				if (modelRowsSorted[i] < modelRowsSorted[i - 1])
				{
					return false;
				}
			}
			return true;
		}

		private void TopologicalSort(ITermModel model)
		{
			byte[] visited = new byte[modelRowsSorted.Length];
			List<int> list = new List<int>(modelRowsSorted.Length);
			int[] array = differentiatedRows;
			foreach (int row in array)
			{
				DFSVisit(row, model, visited, list);
			}
			_diffRowsDAGOrder = Enumerable.ToArray(list);
		}

		private void DFSVisit(int row, ITermModel model, byte[] visited, List<int> rowsInVisitedOrder)
		{
			Stack<int> stack = new Stack<int>();
			int rowIndex = GetRowIndex(modelRowsSorted, row);
			stack.Push(rowIndex);
			while (stack.Count > 0)
			{
				rowIndex = stack.Peek();
				if (model.IsOperation(modelRowsSorted[rowIndex]))
				{
					int[] operands = GetOperands(model, modelRowsSorted[rowIndex]);
					bool flag = false;
					int[] array = operands;
					foreach (int row2 in array)
					{
						int rowIndex2 = GetRowIndex(modelRowsSorted, row2);
						if (visited[rowIndex2] == 0)
						{
							stack.Push(rowIndex2);
						}
						else if (visited[rowIndex2] == 1)
						{
							flag = true;
						}
					}
					if (stack.Peek() != rowIndex)
					{
						continue;
					}
					if (visited[rowIndex] == 0 && flag)
					{
						DifferentiateChildWRTParentDelegate differentiationDelegate = GetDifferentiationDelegate(model, modelRowsSorted[rowIndex]);
						rowsInVisitedOrder.Add(rowIndex);
						_dChilddRowVids[rowIndex] = new List<GradientEntry>(_maxNumChildren);
						for (int j = 0; j < operands.Length; j++)
						{
							int row3 = operands[j];
							int rowIndex3 = GetRowIndex(modelRowsSorted, row3);
							if (visited[rowIndex3] == 1)
							{
								_dChilddRowVids[rowIndex3].Add(new GradientEntry(modelRowsSorted[rowIndex], differentiationDelegate(model, modelRowsSorted[rowIndex], j, operands)));
							}
						}
						visited[rowIndex] = 1;
					}
					else if (!flag)
					{
						visited[rowIndex] = 2;
					}
					stack.Pop();
				}
				else if (model.IsConstant(modelRowsSorted[rowIndex]))
				{
					stack.Pop();
					visited[rowIndex] = 2;
				}
				else
				{
					if (visited[rowIndex] == 0 && GetRowIndex(diffVarsSorted, modelRowsSorted[rowIndex]) >= 0)
					{
						rowsInVisitedOrder.Add(rowIndex);
						visited[rowIndex] = 1;
						_dChilddRowVids[rowIndex] = new List<GradientEntry>(_maxNumChildren);
					}
					else if (GetRowIndex(diffVarsSorted, modelRowsSorted[rowIndex]) < 0)
					{
						visited[rowIndex] = 2;
					}
					stack.Pop();
				}
			}
		}

		private DifferentiateChildWRTParentDelegate GetDifferentiationDelegate(ITermModel model, int rowVid)
		{
			TermModelOperation operation = model.GetOperation(rowVid);
			switch (operation)
			{
			case TermModelOperation.Plus:
				return DiffPlusRow;
			case TermModelOperation.Times:
				return DiffTimesRow;
			case TermModelOperation.Exp:
				return DiffExpRow;
			case TermModelOperation.Sin:
				return DiffSinRow;
			case TermModelOperation.Cos:
				return DiffCosRow;
			case TermModelOperation.Minus:
				return DiffMinusRow;
			case TermModelOperation.Quotient:
				return DiffQuotientRow;
			case TermModelOperation.Identity:
				return DiffIdentityRow;
			case TermModelOperation.Tan:
				return DiffTanRow;
			case TermModelOperation.Log:
				return DiffLogRow;
			case TermModelOperation.Power:
				return DiffPowerRow;
			case TermModelOperation.ArcSin:
				return DiffArcSinRow;
			case TermModelOperation.ArcCos:
				return DiffArcCosRow;
			case TermModelOperation.ArcTan:
				return DiffArcTanRow;
			case TermModelOperation.Sinh:
				return DiffSinhRow;
			case TermModelOperation.Cosh:
				return DiffCoshRow;
			case TermModelOperation.Tanh:
				return DiffTanhRow;
			case TermModelOperation.Log10:
				return DiffLog10Row;
			case TermModelOperation.Sqrt:
				return DiffSqrtRow;
			default:
				throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, Resources.RowNeedsToBeDifferentiatedButIsNotDifferentiable01, new object[2] { rowVid, operation }));
			}
		}

		private int[] GetOperands(ITermModel model, int row)
		{
			int operandCount = model.GetOperandCount(row);
			int[] array = new int[operandCount];
			for (int i = 0; i < operandCount; i++)
			{
				array[i] = model.GetOperand(row, i);
			}
			return array;
		}

		private void DifferentiateRow(ITermModel model, int row, int rowIndex)
		{
			List<GradientEntry> list = _dChilddRowVids[rowIndex];
			int[] array = new int[differentiatedRows.Length];
			for (int i = 0; i < differentiatedRows.Length; i++)
			{
				array[i] = -1;
				if (differentiatedRows[i] == row)
				{
					array[i] = _constOneVid;
					continue;
				}
				for (int j = 0; j < list.Count; j++)
				{
					int rowIndex2 = GetRowIndex(modelRowsSorted, list[j].varVid);
					if (_dRequestedRowdRowVids[rowIndex2][i] != -1)
					{
						int vidNew = list[j].derivRowVid;
						int num = _dRequestedRowdRowVids[rowIndex2][i];
						if (num != _constOneVid)
						{
							model.AddOperation(TermModelOperation.Times, out vidNew, num, list[j].derivRowVid);
						}
						if (array[i] == -1)
						{
							array[i] = vidNew;
							continue;
						}
						int vidNew2 = -1;
						model.AddOperation(TermModelOperation.Plus, out vidNew2, array[i], vidNew);
						array[i] = vidNew2;
					}
				}
			}
			_dRequestedRowdRowVids[rowIndex] = array;
		}

		private int DiffPlusRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return _constOneVid;
		}

		private int DiffTimesRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			if (childOperands.Length == 2)
			{
				switch (operandIndex)
				{
				case 0:
					return childOperands[1];
				case 1:
					return childOperands[0];
				default:
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidArgumentCountForOperator0, new object[1] { "Times" }));
				}
			}
			int[] array = new int[childOperands.Length - 1];
			int num = 0;
			for (int i = 0; i < childOperands.Length; i++)
			{
				int num2 = childOperands[i];
				if (i != operandIndex)
				{
					array[num] = num2;
					num++;
				}
			}
			int vidNew = -1;
			model.AddOperation(TermModelOperation.Times, out vidNew, array);
			return vidNew;
		}

		private int DiffQuotientRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			int vidNew = -1;
			if (operandIndex == 0)
			{
				model.AddOperation(TermModelOperation.Quotient, out vidNew, _constOneVid, childOperands[1]);
			}
			else
			{
				int vidNew2 = -1;
				model.AddOperation(TermModelOperation.Quotient, out vidNew2, _constNegOneVid, childOperands[operandIndex]);
				model.AddOperation(TermModelOperation.Times, out vidNew, childVid, vidNew2);
			}
			return vidNew;
		}

		private int DiffPowerRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			int vidNew = -1;
			if (operandIndex == 0)
			{
				int vidNew2 = -1;
				int vidNew3 = -1;
				model.AddOperation(TermModelOperation.Plus, out vidNew2, childOperands[1], _constNegOneVid);
				model.AddOperation(TermModelOperation.Power, out vidNew3, childOperands[operandIndex], vidNew2);
				model.AddOperation(TermModelOperation.Times, out vidNew, childOperands[1], vidNew3);
			}
			else
			{
				int vidNew4 = -1;
				model.AddOperation(TermModelOperation.Log, out vidNew4, childOperands[0]);
				model.AddOperation(TermModelOperation.Times, out vidNew, childVid, vidNew4);
			}
			return vidNew;
		}

		private int DiffExpRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return childVid;
		}

		private int DiffLogRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			int vidNew = -1;
			model.AddOperation(TermModelOperation.Quotient, out vidNew, _constOneVid, childOperands[operandIndex]);
			return vidNew;
		}

		private int DiffSinRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return DerivativeHelpers.Sin(model, childOperands[operandIndex]);
		}

		private int DiffCosRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return DerivativeHelpers.Cos(model, childOperands[operandIndex]);
		}

		private int DiffTanRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return DerivativeHelpers.Tan(model, childOperands[operandIndex]);
		}

		private int DiffArcSinRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return DerivativeHelpers.ArcSin(model, childOperands[operandIndex]);
		}

		private int DiffArcCosRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return DerivativeHelpers.ArcCos(model, childOperands[operandIndex]);
		}

		private int DiffArcTanRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return DerivativeHelpers.ArcTan(model, childOperands[operandIndex]);
		}

		private int DiffSinhRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return DerivativeHelpers.Sinh(model, childOperands[operandIndex]);
		}

		private int DiffCoshRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return DerivativeHelpers.Cosh(model, childOperands[operandIndex]);
		}

		private int DiffTanhRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return DerivativeHelpers.Tanh(model, childOperands[operandIndex]);
		}

		private int DiffLog10Row(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return DerivativeHelpers.Log10(model, childOperands[operandIndex]);
		}

		private int DiffSqrtRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return DerivativeHelpers.Sqrt(model, childOperands[operandIndex]);
		}

		private int DiffMinusRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return _constNegOneVid;
		}

		private int DiffIdentityRow(ITermModel model, int childVid, int operandIndex, int[] childOperands)
		{
			return _constOneVid;
		}

		private void FormTermModelGradientVids()
		{
			termModelGradientVids = new GradientEntry[differentiatedRows.Length][];
			for (int i = 0; i < differentiatedRows.Length; i++)
			{
				List<GradientEntry> list = new List<GradientEntry>(diffVarsSorted.Length);
				for (int j = 0; j < diffVarsSorted.Length; j++)
				{
					int rowIndex = GetRowIndex(modelRowsSorted, diffVarsSorted[j]);
					int[] array = _dRequestedRowdRowVids[rowIndex];
					if (array != null && array[i] != -1)
					{
						list.Add(new GradientEntry(diffVarsSorted[j], array[i]));
					}
				}
				if (list.Count != 0)
				{
					termModelGradientVids[i] = list.ToArray();
				}
			}
		}
	}
}
