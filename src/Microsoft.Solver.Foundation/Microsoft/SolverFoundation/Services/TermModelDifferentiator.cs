using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Abstract class specifying behavior of ITermModelDifferentiators. 
	/// </summary>
	internal abstract class TermModelDifferentiator
	{
		/// <summary>Holds the derivative vids for requested differentiations
		/// </summary>
		protected GradientEntry[][] termModelGradientVids;

		/// <summary>SORTED vids of rows requested for differentiation. Same order as rows in termModelGradientVids.
		/// </summary>
		protected int[] differentiatedRows;

		/// <summary>Sorted vids of all rows (including variables) in the model.
		/// </summary>
		protected int[] modelRowsSorted;

		/// <summary>Sorted vids of variables with respect to which we need to differentiate.
		/// </summary>
		protected int[] diffVarsSorted;

		/// <summary>Differentiates an ITermModel.
		/// </summary>
		/// <param name="model">The ITermModel to differentiate.</param>
		/// <remarks>Adds rows that represent derivatives of row rowVid with respect to each variable. 
		/// Additionally fills in a (private) data structure that specifies the vids of these new rows.
		/// </remarks>
		public abstract void Differentiate(ITermModel model);

		/// <summary>
		/// Differentiates an ITermModel.
		/// </summary>
		/// <param name="model">The ITermModel to differentiate.</param>
		/// <param name="rowVid">The vids to differentiate.</param>
		/// <remarks>Adds rows that represent derivatives of row rowVid with respect to each variable. 
		/// Additionally fills in a (private) data structure that specifies the vids of these new rows.
		/// </remarks>
		public virtual void Differentiate(ITermModel model, int rowVid)
		{
			ValidateModel(model);
			SetDiffVarsSortedFromModel(model);
			differentiatedRows = new int[1] { rowVid };
			Differentiate(model);
		}

		/// <summary>Differentiates an ITermModel.
		/// </summary>
		/// <param name="model">The ITermModel to differentiate.</param>
		/// <param name="rowVids">The vids to differentiate.</param>
		/// <remarks>Adds rows that represent derivatives of rows in rowVids with respect to each variable. 
		/// Additionally fills in a (private) data structure that specifies the vids of these new rows. 
		/// </remarks>
		public virtual void Differentiate(ITermModel model, IEnumerable<int> rowVids)
		{
			ValidateModel(model);
			SetDiffVarsSortedFromModel(model);
			differentiatedRows = rowVids.ToArray();
			EnsureSorted(differentiatedRows);
			Differentiate(model);
		}

		/// <summary>Differentiates an ITermModel.
		/// </summary>
		/// <param name="model">The ITermModel to differentiate.</param>
		/// <param name="rowVids">The vids to differentiate.</param>
		/// <param name="varVid">The vid of the variable with respect to which to differentiate.</param>
		/// <remarks>Adds rows that represent derivatives of rows in rowVids with respect to variable indexed by varVid. 
		/// Additionally fills in a (private) data structure that specifies the vids of these new rows. 
		/// </remarks>
		public virtual void Differentiate(ITermModel model, IEnumerable<int> rowVids, int varVid)
		{
			ValidateModel(model);
			diffVarsSorted = new int[1] { varVid };
			differentiatedRows = rowVids.ToArray();
			EnsureSorted(differentiatedRows);
			Differentiate(model);
		}

		/// <summary>Differentiates an ITermModel. 
		/// </summary>
		/// <param name="model">The ITermModel to differentiate.</param>
		/// <param name="rowVids">The vids to differentiate.</param>
		/// <param name="varVids">The vids of variables with respect to which to differentiate.</param>
		/// <remarks>Adds rows that represent derivatives of rows in rowVids with respect to variable indexed by varVid. 
		/// Additionally fills in a (private) data structure that specifies the vids of these new rows. 
		/// </remarks>
		public virtual void Differentiate(ITermModel model, IEnumerable<int> rowVids, IEnumerable<int> varVids)
		{
			ValidateModel(model);
			diffVarsSorted = varVids.ToArray();
			EnsureSorted(diffVarsSorted);
			differentiatedRows = rowVids.ToArray();
			EnsureSorted(differentiatedRows);
			Differentiate(model);
		}

		protected void SetDifferentiatedRowsFromModel(ITermModel model)
		{
			differentiatedRows = FindGoalConstraintRows(model, modelRowsSorted);
			if (!IsOrderGuaranteed(model))
			{
				EnsureSorted(differentiatedRows);
			}
		}

		protected void SetDiffVarsSortedFromModel(ITermModel model)
		{
			diffVarsSorted = model.VariableIndices.ToArray();
			if (!IsOrderGuaranteed(model))
			{
				EnsureSorted(diffVarsSorted);
			}
		}

		protected void ValidateModel(ITermModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(string.Format(CultureInfo.InvariantCulture, Resources.XIsNull0, new object[1] { "model" }));
			}
		}

		/// <summary>Verify a model has actually been differentiated.
		/// </summary>
		protected void VerifyDifferentiated()
		{
			if (differentiatedRows == null)
			{
				throw new InvalidOperationException(Resources.ModelHasNotBeenDifferentiated);
			}
		}

		protected static void EnsureSorted(int[] array)
		{
			if (!IsSorted(array))
			{
				Array.Sort(array);
			}
		}

		protected static bool IsSorted(int[] array)
		{
			if (array.Length < 2)
			{
				return true;
			}
			for (int i = 1; i < array.Length; i++)
			{
				if (array[i] < array[i - 1])
				{
					return false;
				}
			}
			return true;
		}

		/// <summary> Validates the input rows are row vids.
		/// </summary>
		/// <param name="model">An ITermModel.</param>
		/// <param name="rowVidsToValidate">rows whose presense in model we validate</param>
		protected void ValidateModelRows(ITermModel model, int[] rowVidsToValidate)
		{
			DebugContracts.NonNull(rowVidsToValidate);
			foreach (int num in rowVidsToValidate)
			{
				if (GetRowIndex(modelRowsSorted, num) < 0)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownRow, new object[1] { num }));
				}
			}
		}

		/// <summary> To be called after is sorted so that binary search can be used. 
		/// </summary>
		/// <param name="model">model</param>
		/// <param name="varVidsToValidate">rows whose presense in model we validate</param>
		protected void ValidateModelVars(ITermModel model, int[] varVidsToValidate)
		{
			DebugContracts.NonNull(varVidsToValidate);
			foreach (int num in varVidsToValidate)
			{
				if (GetRowIndex(modelRowsSorted, num) < 0 || model.IsRow(num))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { num }));
				}
			}
		}

		protected int GetRowIndex(int[] rowArr, int row)
		{
			return Array.BinarySearch(rowArr, row);
		}

		/// <summary>Check if model is known to have rows and variables for differentiation in order.
		/// </summary>
		/// <param name="model">The model.</param>
		/// <returns>True if order is guaranteed, false otherwise.</returns>
		protected static bool IsOrderGuaranteed(ITermModel model)
		{
			SimplifiedTermModelWrapper simplifiedTermModelWrapper = model as SimplifiedTermModelWrapper;
			if (!(model is TermModel))
			{
				if (simplifiedTermModelWrapper != null)
				{
					return simplifiedTermModelWrapper._model is TermModel;
				}
				return false;
			}
			return true;
		}

		protected static int[] FindGoalConstraintRows(ITermModel model, int[] modelRowsSorted)
		{
			List<int> list = new List<int>(modelRowsSorted.Length);
			foreach (int num in modelRowsSorted)
			{
				if (model.IsGoal(num))
				{
					list.Add(num);
					continue;
				}
				model.GetBounds(num, out var lower, out var upper);
				if ((!(lower == Rational.NegativeInfinity) || !(upper == Rational.PositiveInfinity)) && model.IsRow(num) && !model.IsConstant(num))
				{
					list.Add(num);
				}
			}
			return list.ToArray();
		}

		/// <summary>Gets an IEnumerable of rows which were differentiated.
		/// </summary>
		/// <returns>The row vids with nonzero derivatives.</returns>
		public IEnumerable<int> GetDifferentiatedRows()
		{
			VerifyDifferentiated();
			for (int i = 0; i < differentiatedRows.Length; i++)
			{
				yield return differentiatedRows[i];
			}
		}

		/// <summary>Gets an IEnumerable of variable vids for those variables with respect to which the given row was differentiated.
		/// ArgumentException will be thrown if the vid is not listed in GetDifferentiatedRows().
		/// An empty result will be returned if the given row has no non-zero derivatives.
		/// <param name="rowVid">The vid of the differentiated row.</param>
		/// </summary>
		/// <returns>Variable vids with respect to which nonzero derivatives were obtained.</returns>
		public IEnumerable<int> GetDerivativeVariables(int rowVid)
		{
			VerifyDifferentiated();
			int i = Array.BinarySearch(differentiatedRows, rowVid);
			if (i < 0)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownRow, new object[1] { rowVid }));
			}
			if (termModelGradientVids[i] == null)
			{
				yield break;
			}
			try
			{
				GradientEntry[] array = termModelGradientVids[i];
				for (int j = 0; j < array.Length; j++)
				{
					GradientEntry e = array[j];
					yield return e.varVid;
				}
			}
			finally
			{
			}
		}

		/// <summary>Gets GradientEntry tuples which represent the variable of differentiation vid and derivative vid for a given row.
		/// ArgumentException will be thrown if the vid is not listed in GetDifferentiatedRows().
		/// An empty result will be returned if the given row has no non-zero derivatives.
		/// </summary>
		/// <param name="rowVid">The vid of the differentiated row.</param>
		/// <returns>GradientEntries for the nonzero gradient entries</returns>
		public IEnumerable<GradientEntry> GetRowGradientEntries(int rowVid)
		{
			VerifyDifferentiated();
			int i = Array.BinarySearch(differentiatedRows, rowVid);
			if (i < 0)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownRow, new object[1] { rowVid }));
			}
			if (termModelGradientVids[i] == null)
			{
				yield break;
			}
			try
			{
				GradientEntry[] array = termModelGradientVids[i];
				for (int j = 0; j < array.Length; j++)
				{
					yield return array[j];
				}
			}
			finally
			{
			}
		}

		/// <summary>Gets the row index in ITermModel type which represents the derivative of a specified row wrt to a specified variable. 
		/// Throws an ArgumentException if the given row has no non-zero derivatives. Throws an ArgumentException if no derivative exists for the variable.
		/// </summary>
		/// <param name="rowVid">The row whose gradient component is to be retrieved.</param>
		/// <param name="varVid">The vid for the variable the row for the derivative with respect to which is to be gotten.</param>
		/// <returns> row indices into extended ITermModel type</returns>
		public int GetRowGradientEntry(int rowVid, int varVid)
		{
			VerifyDifferentiated();
			int num = Array.BinarySearch(differentiatedRows, rowVid);
			if (num < 0)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownRow, new object[1] { rowVid }));
			}
			if (termModelGradientVids[num] == null)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.NoDerivativeExists01, new object[2] { rowVid, varVid }));
			}
			GradientEntry[] array = termModelGradientVids[num];
			for (int i = 0; i < array.Length; i++)
			{
				GradientEntry gradientEntry = array[i];
				if (gradientEntry.varVid == varVid)
				{
					return gradientEntry.derivRowVid;
				}
			}
			throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.NoDerivativeExists01, new object[2] { rowVid, varVid }));
		}
	}
}
