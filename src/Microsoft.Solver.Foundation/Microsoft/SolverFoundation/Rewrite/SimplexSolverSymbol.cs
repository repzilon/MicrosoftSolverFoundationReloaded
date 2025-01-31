using System;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// Symbol for the Simplex solver. A simplex solver can be created from an MPS file, a model or another simplex solver.
	///
	/// Arguments: A series of MPS symobl, Model symbol or SimplexSolver symbol. If options are used, the symbols are put in a list
	/// as the first element. Further elements are options.
	///
	/// Returns a SimplexSolver symbol.
	/// </summary>
	internal class SimplexSolverSymbol : BaseSolveSymbol
	{
		protected MpsParser _psr;

		public SimplexSolverSymbol(SolveRewriteSystem rs)
			: base(rs, "SimplexSolver")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			SimplexSolverWrapper simplexSolverWrapper = new SimplexSolverWrapper(base.Rewrite);
			for (int i = 0; i < ib.Count; i++)
			{
				Expression expression = ib[i];
				Expression expression2 = expression;
				string strPrefix = null;
				if (expression is Invocation && expression.Head == base.Rewrite.Builtin.List)
				{
					expression2 = expression[0];
					for (int j = 1; j < expression.Arity; j++)
					{
						if (!SolveSymbol.TryParseOption(base.Rewrite, expression[j], out var str, out var exprVal))
						{
							return null;
						}
						string text;
						if ((text = str) != null && text == "Prefix")
						{
							strPrefix = exprVal.ToString();
							continue;
						}
						return null;
					}
				}
				Func<object, Expression> mapper = ((strPrefix != null) ? ((Func<object, Expression>)((object key) => new StringConstant(base.Rewrite, strPrefix + key.ToString()))) : ((Func<object, Expression>)((object key) => (key is Expression expression3) ? expression3 : new StringConstant(base.Rewrite, key.ToString()))));
				bool flag = false;
				if (expression2 is SimplexSolverWrapper simplexSolverWrapper2)
				{
					flag = MergeLinearModels(base.Rewrite, simplexSolverWrapper.Solver, simplexSolverWrapper2.Solver, mapper);
				}
				else if (expression2 is Invocation invocation && invocation.Head == base.Rewrite.SolveSymbols.Mps)
				{
					flag = MergeMps(simplexSolverWrapper, invocation, mapper);
				}
				if (!flag)
				{
					return null;
				}
			}
			return simplexSolverWrapper;
		}

		/// <summary>
		/// Merges an MPS file into a simplex solver.
		/// </summary>
		protected bool MergeMps(SimplexSolverWrapper ssDst, Invocation invSrc, Func<object, Expression> mapper)
		{
			bool val = true;
			if (invSrc.Arity < 1 || invSrc.Arity > 2 || !(invSrc[0] is StringConstant stringConstant) || (invSrc.Arity == 2 && !invSrc[1].GetValue(out val)))
			{
				base.Rewrite.Log(Resources.MpsExpectsAStringTheFileNameOptionallyFollowedByABooleanTrueForFixedFormatFalseForFree);
				return false;
			}
			if (_psr == null)
			{
				_psr = new MpsParser(new MpsLexer(new NormStr.Pool()));
			}
			LinearModel linearModel = _psr.ProcessSource(new StaticText(stringConstant.Value), val);
			if (linearModel == null)
			{
				base.Rewrite.Log(Resources.ParsingMPSFailed0, stringConstant.Value);
				return false;
			}
			return MergeLinearModels(base.Rewrite, ssDst.Solver, linearModel, mapper);
		}

		/// <summary>
		/// Merges two linear models.
		/// The keys for source rows and source variables are sent to the mapper which transforms them into destination keys.
		/// If a destination key already exists, we log a warning and continue merging the models.
		/// The goals are merged in order (first goal with first goal and so on down the list of rows).
		/// </summary>
		protected static bool MergeLinearModels(RewriteSystem rs, ILinearModel modDst, ILinearModel modSrc, Func<object, Expression> mapper)
		{
			if (modDst == modSrc)
			{
				return false;
			}
			foreach (object variableKey in modSrc.VariableKeys)
			{
				Expression expression = mapper(variableKey);
				if (!modDst.AddVariable(expression, out var vid))
				{
					rs.Log(string.Format(CultureInfo.InvariantCulture, Resources.Variable0AlreadyExistsInDestinationModel, new object[1] { expression }));
				}
				else
				{
					CopyVarAttrs(modDst, vid, modSrc, modSrc.GetIndexFromKey(variableKey));
				}
			}
			foreach (object rowKey in modSrc.RowKeys)
			{
				CopyRow(rs, modDst, modSrc, rowKey, mapper);
			}
			int num = -1;
			foreach (ILinearGoal goal in modDst.Goals)
			{
				num = goal.Priority;
			}
			foreach (ILinearGoal goal2 in modSrc.Goals)
			{
				int indexFromKey = modDst.GetIndexFromKey(mapper(goal2.Key));
				ILinearGoal linearGoal = modDst.AddGoal(indexFromKey, ++num, goal2.Minimize);
				linearGoal.Enabled = goal2.Enabled;
			}
			return true;
		}

		protected static void CopyVarAttrs(ILinearModel modDst, int vidDst, ILinearModel modSrc, int vidSrc)
		{
			modSrc.GetBounds(vidSrc, out var numLo, out var numHi);
			modDst.SetBounds(vidDst, numLo, numHi);
			modDst.SetIgnoreBounds(vidDst, modSrc.GetIgnoreBounds(vidSrc));
			modDst.SetBasic(vidDst, modSrc.GetBasic(vidSrc));
			modDst.SetValue(vidDst, modSrc.GetValue(vidSrc));
			modDst.SetIntegrality(vidDst, modSrc.GetIntegrality(vidSrc));
		}

		/// <summary>
		/// Copies a row from the source model into the destination models. 
		/// The row to copy is identified by its key.
		/// </summary>
		protected static int CopyRow(RewriteSystem rewrite, ILinearModel modDst, ILinearModel modSrc, object keySrc, Func<object, Expression> mapper)
		{
			object obj = mapper(keySrc);
			if (!modDst.AddRow(obj, out var vid))
			{
				rewrite.Log(string.Format(CultureInfo.InvariantCulture, Resources.Row0AlreadyExistsInDestinationModel, new object[1] { obj }));
				return vid;
			}
			int indexFromKey = modSrc.GetIndexFromKey(keySrc);
			CopyVarAttrs(modDst, vid, modSrc, indexFromKey);
			foreach (LinearEntry rowEntry in modSrc.GetRowEntries(indexFromKey))
			{
				modDst.SetCoefficient(vid, modDst.GetIndexFromKey(mapper(rowEntry.Key)), rowEntry.Value);
			}
			return vid;
		}
	}
}
