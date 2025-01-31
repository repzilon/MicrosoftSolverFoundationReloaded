using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Uses Forward Differentiation to symbolically differentiate an ITermModel.
	/// </summary>
	internal class TermModelForwardDifferentiator : TermModelDifferentiator
	{
		private delegate void DifferentiateMultiOperandRowDelegate(ITermModel model, int row, int rowIndex, int[] operands, int[] operandDerivs, int rowDerivIndex);

		private GradientEntry[][] _modelRowsGradientVids;

		private int[] _diffRowsDAGOrder;

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
			_modelRowsGradientVids = new GradientEntry[modelRowsSorted.Length][];
			TopologicalSort(model);
			model.AddConstant(Rational.One, out _constOneVid);
			model.AddConstant(-Rational.One, out _constNegOneVid);
			int[] diffRowsDAGOrder = _diffRowsDAGOrder;
			foreach (int row in diffRowsDAGOrder)
			{
				int rowIndex = GetRowIndex(modelRowsSorted, row);
				DifferentiateRow(model, row, rowIndex);
			}
			termModelGradientVids = TrimTermModelGradientVids(_modelRowsGradientVids, modelRowsSorted, differentiatedRows);
			Clean();
		}

		private void Clean()
		{
			modelRowsSorted = null;
			_diffRowsDAGOrder = null;
			diffVarsSorted = null;
			_modelRowsGradientVids = null;
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
					if (stack.Peek() == rowIndex)
					{
						if (visited[rowIndex] == 0 && flag)
						{
							ValidateDifferentiableOperation(model, modelRowsSorted[rowIndex]);
							rowsInVisitedOrder.Add(rowIndex);
							visited[rowIndex] = 1;
						}
						else if (!flag)
						{
							visited[rowIndex] = 2;
						}
						stack.Pop();
					}
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
						_modelRowsGradientVids[rowIndex] = new GradientEntry[1]
						{
							new GradientEntry(modelRowsSorted[rowIndex], -1)
						};
					}
					else if (GetRowIndex(diffVarsSorted, modelRowsSorted[rowIndex]) < 0)
					{
						visited[rowIndex] = 2;
					}
					stack.Pop();
				}
			}
		}

		private void ValidateDifferentiableOperation(ITermModel model, int rowVid)
		{
			TermModelOperation operation = model.GetOperation(rowVid);
			switch (operation)
			{
			case TermModelOperation.Identity:
			case TermModelOperation.Minus:
			case TermModelOperation.Sin:
			case TermModelOperation.Cos:
			case TermModelOperation.Tan:
			case TermModelOperation.ArcCos:
			case TermModelOperation.ArcSin:
			case TermModelOperation.ArcTan:
			case TermModelOperation.Cosh:
			case TermModelOperation.Sinh:
			case TermModelOperation.Tanh:
			case TermModelOperation.Exp:
			case TermModelOperation.Log:
			case TermModelOperation.Log10:
			case TermModelOperation.Sqrt:
			case TermModelOperation.Plus:
			case TermModelOperation.Times:
			case TermModelOperation.Quotient:
			case TermModelOperation.Power:
				return;
			}
			throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, Resources.RowNeedsToBeDifferentiatedButIsNotDifferentiable01, new object[2] { rowVid, operation }));
		}

		private int[] GetRowIndices(int[] rows)
		{
			int[] array = new int[rows.Length];
			for (int i = 0; i < rows.Length; i++)
			{
				array[i] = GetRowIndex(modelRowsSorted, rows[i]);
			}
			return array;
		}

		private Dictionary<int, int[]> OperandDerivatives(int rowIndex, int[] operands)
		{
			int[] rowIndices = GetRowIndices(operands);
			Dictionary<int, int[]> dictionary = new Dictionary<int, int[]>();
			for (int i = 0; i < operands.Length; i++)
			{
				int num = rowIndices[i];
				GradientEntry[] array = _modelRowsGradientVids[num];
				if (array == null)
				{
					continue;
				}
				int num2 = 0;
				int num3 = 0;
				while (num2 < diffVarsSorted.Length && num3 < array.Length)
				{
					if (diffVarsSorted[num2] < array[num3].varVid)
					{
						num2++;
						continue;
					}
					GradientEntry gradientEntry = array[num3];
					if (!dictionary.TryGetValue(gradientEntry.varVid, out var value))
					{
						value = new int[operands.Length];
						for (int j = 0; j < value.Length; j++)
						{
							value[j] = -1;
						}
						dictionary[gradientEntry.varVid] = value;
					}
					value[i] = gradientEntry.derivRowVid;
					num2++;
					num3++;
				}
			}
			_modelRowsGradientVids[rowIndex] = GetRowGradientVids(dictionary);
			return dictionary;
		}

		private static GradientEntry[] GetRowGradientVids(Dictionary<int, int[]> operandDerivs)
		{
			GradientEntry[] array = new GradientEntry[operandDerivs.Count];
			int num = 0;
			foreach (int key in operandDerivs.Keys)
			{
				array[num].varVid = key;
				array[num].derivRowVid = -1;
				num++;
			}
			Array.Sort(array);
			return array;
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
			if (model.IsOperation(row))
			{
				if (model.GetOperandCount(row) > 1)
				{
					int[] operands = GetOperands(model, row);
					Dictionary<int, int[]> dictionary = OperandDerivatives(rowIndex, operands);
					GradientEntry[] array = _modelRowsGradientVids[rowIndex];
					DifferentiateMultiOperandRowDelegate differentiateMultiOperandRowDelegate = GetDifferentiateMultiOperandRowDelegate(model, row);
					for (int i = 0; i < array.Length; i++)
					{
						differentiateMultiOperandRowDelegate(model, row, rowIndex, operands, dictionary[array[i].varVid], i);
					}
					return;
				}
				switch (model.GetOperation(row))
				{
				case TermModelOperation.Exp:
					DiffExpRow(model, row, rowIndex);
					break;
				case TermModelOperation.Sin:
					DiffUnary(model, row, rowIndex, DerivativeHelpers.Sin);
					break;
				case TermModelOperation.Cos:
					DiffUnary(model, row, rowIndex, DerivativeHelpers.Cos);
					break;
				case TermModelOperation.Minus:
					DiffMinusRow(model, row, rowIndex);
					break;
				case TermModelOperation.Identity:
					DiffIdentityRow(model, row, rowIndex);
					break;
				case TermModelOperation.Tan:
					DiffUnary(model, row, rowIndex, DerivativeHelpers.Tan);
					break;
				case TermModelOperation.Log:
					DiffLogRow(model, row, rowIndex);
					break;
				case TermModelOperation.ArcSin:
					DiffUnary(model, row, rowIndex, DerivativeHelpers.ArcSin);
					break;
				case TermModelOperation.ArcCos:
					DiffUnary(model, row, rowIndex, DerivativeHelpers.ArcCos);
					break;
				case TermModelOperation.ArcTan:
					DiffUnary(model, row, rowIndex, DerivativeHelpers.ArcTan);
					break;
				case TermModelOperation.Sinh:
					DiffUnary(model, row, rowIndex, DerivativeHelpers.Sinh);
					break;
				case TermModelOperation.Cosh:
					DiffUnary(model, row, rowIndex, DerivativeHelpers.Cosh);
					break;
				case TermModelOperation.Tanh:
					DiffUnary(model, row, rowIndex, DerivativeHelpers.Tanh);
					break;
				case TermModelOperation.Log10:
					DiffUnary(model, row, rowIndex, DerivativeHelpers.Log10);
					break;
				case TermModelOperation.Sqrt:
					DiffUnary(model, row, rowIndex, DerivativeHelpers.Sqrt);
					break;
				case TermModelOperation.Abs:
				case TermModelOperation.Not:
					break;
				}
			}
			else
			{
				_modelRowsGradientVids[rowIndex][0].derivRowVid = _constOneVid;
			}
		}

		private DifferentiateMultiOperandRowDelegate GetDifferentiateMultiOperandRowDelegate(ITermModel model, int row)
		{
			switch (model.GetOperation(row))
			{
			case TermModelOperation.Plus:
				return DiffPlusRow;
			case TermModelOperation.Times:
				return DiffTimesRow;
			case TermModelOperation.Quotient:
				return DiffQuotientRow;
			case TermModelOperation.Power:
				return DiffPowerRow;
			default:
				return null;
			}
		}

		private void DiffPlusRow(ITermModel model, int row, int rowIndex, int[] operands, int[] operandDerivs, int rowDerivIndex)
		{
			List<int> list = new List<int>(operands.Length);
			for (int i = 0; i < operands.Length; i++)
			{
				if (operandDerivs[i] >= 0)
				{
					list.Add(operandDerivs[i]);
				}
			}
			if (list.Count == 1)
			{
				_modelRowsGradientVids[rowIndex][rowDerivIndex].derivRowVid = list[0];
			}
			else if (list.Count > 1)
			{
				int vidNew = -1;
				model.AddOperation(TermModelOperation.Plus, out vidNew, list.ToArray());
				_modelRowsGradientVids[rowIndex][rowDerivIndex].derivRowVid = vidNew;
			}
		}

		private void DiffTimesRow(ITermModel model, int row, int rowIndex, int[] operands, int[] operandDerivs, int rowDerivIndex)
		{
			int[] array = new int[operands.Length];
			operands.CopyTo(array, 0);
			List<int> list = new List<int>(operands.Length);
			for (int i = 0; i < operands.Length; i++)
			{
				if (operandDerivs[i] >= 0)
				{
					array[i] = operandDerivs[i];
					int vidNew = -1;
					model.AddOperation(TermModelOperation.Times, out vidNew, array);
					array[i] = operands[i];
					list.Add(vidNew);
				}
			}
			if (list.Count == 1)
			{
				_modelRowsGradientVids[rowIndex][rowDerivIndex].derivRowVid = list[0];
			}
			else if (list.Count > 1)
			{
				int vidNew2 = -1;
				model.AddOperation(TermModelOperation.Plus, out vidNew2, list.ToArray());
				_modelRowsGradientVids[rowIndex][rowDerivIndex].derivRowVid = vidNew2;
			}
		}

		private void DiffQuotientRow(ITermModel model, int row, int rowIndex, int[] operands, int[] operandDerivs, int rowDerivIndex)
		{
			if (operandDerivs[0] > -1 && operandDerivs[1] > -1)
			{
				int vidNew = -1;
				int vidNew2 = -1;
				int vidNew3 = -1;
				int vidNew4 = -1;
				int vidNew5 = -1;
				model.AddOperation(TermModelOperation.Quotient, out vidNew2, operandDerivs[0], operands[1]);
				model.AddOperation(TermModelOperation.Quotient, out vidNew3, operandDerivs[1], operands[1]);
				model.AddOperation(TermModelOperation.Minus, out vidNew4, vidNew3);
				model.AddOperation(TermModelOperation.Times, out vidNew5, row, vidNew4);
				model.AddOperation(TermModelOperation.Plus, out vidNew, vidNew2, vidNew5);
				_modelRowsGradientVids[rowIndex][rowDerivIndex].derivRowVid = vidNew;
			}
			else if (operandDerivs[0] > -1)
			{
				int vidNew6 = -1;
				model.AddOperation(TermModelOperation.Quotient, out vidNew6, operandDerivs[0], operands[1]);
				_modelRowsGradientVids[rowIndex][rowDerivIndex].derivRowVid = vidNew6;
			}
			else if (operandDerivs[1] > -1)
			{
				int vidNew7 = -1;
				int vidNew8 = -1;
				int vidNew9 = -1;
				model.AddOperation(TermModelOperation.Quotient, out vidNew8, operandDerivs[1], operands[1]);
				model.AddOperation(TermModelOperation.Minus, out vidNew9, vidNew8);
				model.AddOperation(TermModelOperation.Times, out vidNew7, row, vidNew9);
				_modelRowsGradientVids[rowIndex][rowDerivIndex].derivRowVid = vidNew7;
			}
		}

		private void DiffPowerRow(ITermModel model, int row, int rowIndex, int[] operands, int[] operandDerivs, int rowDerivIndex)
		{
			if (operandDerivs[0] > -1 && operandDerivs[1] > -1)
			{
				int vidNew = -1;
				int vidNew2 = -1;
				int vidNew3 = -1;
				int vidNew4 = -1;
				int vidNew5 = -1;
				int vidNew6 = -1;
				model.AddOperation(TermModelOperation.Log, out vidNew2, operands[0]);
				model.AddOperation(TermModelOperation.Times, out vidNew3, operandDerivs[1], vidNew2);
				model.AddOperation(TermModelOperation.Quotient, out vidNew4, operandDerivs[0], operands[0]);
				model.AddOperation(TermModelOperation.Times, out vidNew5, operands[1], vidNew4);
				model.AddOperation(TermModelOperation.Plus, out vidNew6, vidNew3, vidNew5);
				model.AddOperation(TermModelOperation.Times, out vidNew, row, vidNew6);
				_modelRowsGradientVids[rowIndex][rowDerivIndex].derivRowVid = vidNew;
			}
			else if (operandDerivs[0] > -1)
			{
				int vidNew7 = -1;
				int vidNew8 = -1;
				int vidNew9 = -1;
				int vidNew10 = -1;
				model.AddOperation(TermModelOperation.Plus, out vidNew8, operands[1], _constNegOneVid);
				model.AddOperation(TermModelOperation.Power, out vidNew9, operands[0], vidNew8);
				model.AddOperation(TermModelOperation.Times, out vidNew10, operands[1], vidNew9);
				model.AddOperation(TermModelOperation.Times, out vidNew7, vidNew10, operandDerivs[0]);
				_modelRowsGradientVids[rowIndex][rowDerivIndex].derivRowVid = vidNew7;
			}
			else if (operandDerivs[1] > -1)
			{
				int vidNew11 = -1;
				int vidNew12 = -1;
				int vidNew13 = -1;
				model.AddOperation(TermModelOperation.Log, out vidNew12, operands[0]);
				model.AddOperation(TermModelOperation.Times, out vidNew13, operandDerivs[1], vidNew12);
				model.AddOperation(TermModelOperation.Times, out vidNew11, row, vidNew13);
				_modelRowsGradientVids[rowIndex][rowDerivIndex].derivRowVid = vidNew11;
			}
		}

		private void DiffLogRow(ITermModel model, int row, int rowIndex)
		{
			int operand = model.GetOperand(row, 0);
			int rowIndex2 = GetRowIndex(modelRowsSorted, operand);
			int num = -1;
			int vidNew = -1;
			_modelRowsGradientVids[rowIndex] = new GradientEntry[_modelRowsGradientVids[rowIndex2].Length];
			for (int i = 0; i < _modelRowsGradientVids[rowIndex2].Length; i++)
			{
				num = _modelRowsGradientVids[rowIndex2][i].derivRowVid;
				model.AddOperation(TermModelOperation.Quotient, out vidNew, num, operand);
				_modelRowsGradientVids[rowIndex][i].derivRowVid = vidNew;
				_modelRowsGradientVids[rowIndex][i].varVid = _modelRowsGradientVids[rowIndex2][i].varVid;
			}
		}

		private void DiffExpRow(ITermModel model, int row, int rowIndex)
		{
			int operand = model.GetOperand(row, 0);
			int rowIndex2 = GetRowIndex(modelRowsSorted, operand);
			_modelRowsGradientVids[rowIndex] = new GradientEntry[_modelRowsGradientVids[rowIndex2].Length];
			MultiplyByOperandDeriv(model, rowIndex, rowIndex2, row);
		}

		private void DiffUnary(ITermModel model, int row, int rowIndex, Func<ITermModel, int, int> derivFunc)
		{
			int operand = model.GetOperand(row, 0);
			int rowIndex2 = GetRowIndex(modelRowsSorted, operand);
			int rowToMultiplyVid = derivFunc(model, operand);
			_modelRowsGradientVids[rowIndex] = new GradientEntry[_modelRowsGradientVids[rowIndex2].Length];
			MultiplyByOperandDeriv(model, rowIndex, rowIndex2, rowToMultiplyVid);
		}

		private void MultiplyByOperandDeriv(ITermModel model, int rowIndex, int op1Index, int rowToMultiplyVid)
		{
			int num = -1;
			int vidNew = -1;
			for (int i = 0; i < _modelRowsGradientVids[op1Index].Length; i++)
			{
				num = _modelRowsGradientVids[op1Index][i].derivRowVid;
				model.AddOperation(TermModelOperation.Times, out vidNew, rowToMultiplyVid, num);
				_modelRowsGradientVids[rowIndex][i].derivRowVid = vidNew;
				_modelRowsGradientVids[rowIndex][i].varVid = _modelRowsGradientVids[op1Index][i].varVid;
			}
		}

		private void DiffMinusRow(ITermModel model, int row, int rowIndex)
		{
			int operand = model.GetOperand(row, 0);
			int rowIndex2 = GetRowIndex(modelRowsSorted, operand);
			int num = -1;
			int vidNew = -1;
			_modelRowsGradientVids[rowIndex] = new GradientEntry[_modelRowsGradientVids[rowIndex2].Length];
			for (int i = 0; i < _modelRowsGradientVids[rowIndex2].Length; i++)
			{
				num = _modelRowsGradientVids[rowIndex2][i].derivRowVid;
				model.AddOperation(TermModelOperation.Minus, out vidNew, num);
				_modelRowsGradientVids[rowIndex][i].derivRowVid = vidNew;
				_modelRowsGradientVids[rowIndex][i].varVid = _modelRowsGradientVids[rowIndex2][i].varVid;
			}
		}

		private void DiffIdentityRow(ITermModel model, int row, int rowIndex)
		{
			int operand = model.GetOperand(row, 0);
			int rowIndex2 = GetRowIndex(modelRowsSorted, operand);
			_modelRowsGradientVids[rowIndex] = new GradientEntry[_modelRowsGradientVids[rowIndex2].Length];
			for (int i = 0; i < _modelRowsGradientVids[rowIndex2].Length; i++)
			{
				_modelRowsGradientVids[rowIndex][i].derivRowVid = _modelRowsGradientVids[rowIndex2][i].derivRowVid;
				_modelRowsGradientVids[rowIndex][i].varVid = _modelRowsGradientVids[rowIndex2][i].varVid;
			}
		}

		private GradientEntry[][] TrimTermModelGradientVids(GradientEntry[][] gradVidsToTrim, int[] rowsToTrim, int[] rowsToKeep)
		{
			GradientEntry[][] array = new GradientEntry[rowsToKeep.Length][];
			for (int i = 0; i < rowsToKeep.Length; i++)
			{
				int rowIndex = GetRowIndex(rowsToTrim, rowsToKeep[i]);
				if (gradVidsToTrim[rowIndex] != null)
				{
					array[i] = new GradientEntry[gradVidsToTrim[rowIndex].Length];
					Array.Copy(gradVidsToTrim[rowIndex], array[i], gradVidsToTrim[rowIndex].Length);
				}
			}
			return array;
		}
	}
}
