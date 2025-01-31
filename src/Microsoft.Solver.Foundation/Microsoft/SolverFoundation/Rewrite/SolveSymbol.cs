using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// Solves a model.
	///
	/// Arguments: the model to solve, a series of optional options. 
	/// </summary>
	internal class SolveSymbol : BaseSolveSymbol
	{
		protected enum ModelType
		{
			Lp,
			Qp,
			Mip,
			Csp
		}

		private HashSet<string> _globalParamters;

		public SolveSymbol(SolveRewriteSystem rs)
			: base(rs, "Solve")
		{
		}

		protected SolveSymbol(SolveRewriteSystem rs, string name)
			: base(rs, name)
		{
		}

		protected SolveSymbol(SolveRewriteSystem rs, SymbolScope scope, string name)
			: base(rs, scope, name)
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			Dictionary<string, Expression> dictionary = null;
			if (ib.Count == 0)
			{
				return null;
			}
			if (ib.Count > 1)
			{
				dictionary = new Dictionary<string, Expression>();
				for (int i = 1; i < ib.Count; i++)
				{
					if (!TryParseOption(base.Rewrite, ib[i], out var str, out var exprVal))
					{
						base.Rewrite.Log(Resources.ImproperOption0, ib[i]);
						return null;
					}
					dictionary[str] = exprVal;
				}
			}
			_globalParamters = new HashSet<string>();
			_globalParamters.Add("AllowNonUsefulParam");
			_globalParamters.Add("CheckingOption");
			_globalParamters.Add("Solver");
			_globalParamters.Add("GetSensitivityReport");
			_globalParamters.Add("MaxTime");
			_globalParamters.Add("MaxIterations");
			return SolveWithOptions(ib[0], dictionary);
		}

		protected virtual Expression SolveWithOptions(Expression expr, Dictionary<string, Expression> mpstrexprOpt)
		{
			if (expr.Head == base.Rewrite.Builtin.Model)
			{
				return SolveModel((Invocation)expr, mpstrexprOpt);
			}
			return null;
		}

		protected virtual Expression SolveModel(Invocation invModel, Dictionary<string, Expression> mpstrexprOpt)
		{
			if (!ConcreteModel.ParseModel(invModel, out var mod))
			{
				return null;
			}
			bool flag = false;
			if (mpstrexprOpt != null && mpstrexprOpt.TryGetValue("CheckingOption", out var value) && (!value.GetValue(out string val) || val != "True"))
			{
				flag = true;
			}
			SolverContext solverContext = new SolverContext();
			Model sfsModel = solverContext.CreateModel();
			if (!mod.TryGetSfsModel(sfsModel, solverContext, out var sfsMap, out var strError, out var exprError))
			{
				base.Rewrite.Log("SFS can't process the model: '{0}', '{1}'", strError, exprError);
				return null;
			}
			if (flag)
			{
				return null;
			}
			return SolveWithSfs(sfsModel, sfsMap, mpstrexprOpt, solverContext);
		}

		private static string GetLineInformation(Expression expr)
		{
			if (expr != null && expr.PlacementInformation != null)
			{
				expr.PlacementInformation.Map.MapSpanToPos(expr.PlacementInformation.Span, out var spos);
				return string.Format(CultureInfo.InvariantCulture, "({0},{1})-({2},{3})", spos.lineMin, spos.colMin, spos.lineLim, spos.colLim);
			}
			return string.Empty;
		}

		/// <summary>
		/// look up in the dictinary with the prefix of the model
		/// </summary>
		/// <param name="key"></param>
		/// <param name="mpstrexprOpt"></param>
		/// <param name="model"></param>
		/// <returns>if the key exists return the value, otherwise empty string</returns>
		private static string GetValueForModelType(string key, Dictionary<string, Expression> mpstrexprOpt, ModelType model)
		{
			string key2 = model.ToString() + "_" + key;
			string key3 = model.ToString().ToUpper() + "_" + key;
			if (mpstrexprOpt != null && (mpstrexprOpt.TryGetValue(key2, out var value) || mpstrexprOpt.TryGetValue(key3, out value)) && value.GetValue(out string val))
			{
				return val;
			}
			return string.Empty;
		}

		/// <summary>
		/// Check the initial of parameter to see if it is relevant to model
		/// if yes, it cuts the string to the clean parameter
		/// </summary>
		/// <param name="parameter"></param>
		/// <param name="models"></param>
		/// <returns></returns>
		private bool IsRelevantForModelType(ref string parameter, params ModelType[] models)
		{
			if (_globalParamters.Contains(parameter))
			{
				return true;
			}
			foreach (ModelType modelType in models)
			{
				string text = modelType.ToString() + "_";
				if (parameter.StartsWith(text, ignoreCase: true, CultureInfo.InvariantCulture))
				{
					parameter = parameter.Substring(text.Length);
					return true;
				}
			}
			return false;
		}

		protected Expression SolveWithSfs(Model sfsModel, Dictionary<Expression, Term> sfsMap, Dictionary<string, Expression> mpstrexprOpt, SolverContext context)
		{
			Solution solution = context.Solve();
			List<Expression> list = new List<Expression>();
			foreach (KeyValuePair<Expression, Term> item in sfsMap)
			{
				Expression key = item.Key;
				Term value = item.Value;
				if (value is Decision decision)
				{
					if (decision._domain.ValueClass == TermValueClass.Numeric)
					{
						list.Add(base.Rewrite.Builtin.Rule.Invoke(key, RationalConstant.Create(base.Rewrite, decision.GetDouble())));
					}
					else if (decision._domain.ValueClass == TermValueClass.Enumerated)
					{
						list.Add(base.Rewrite.Builtin.Rule.Invoke(key, new StringConstant(base.Rewrite, decision.GetString())));
					}
				}
			}
			return base.Rewrite.Builtin.List.Invoke(base.Rewrite.Builtin.List.Invoke(list.ToArray()), new StringConstant(base.Rewrite, solution.Quality.ToString()));
		}

		/// <summary>
		/// Parses options given in the form of rules OptionName -&gt; Value. If only the OptionName is specified, 
		/// the Value is assumed to be True.
		/// </summary>
		internal static bool TryParseOption(RewriteSystem rs, Expression expr, out string str, out Expression exprVal)
		{
			if (expr.GetValue(out str))
			{
				exprVal = rs.Builtin.Boolean.True;
				return true;
			}
			if (expr.Head == rs.Builtin.Rule && expr.Arity == 2 && expr[0].GetValue(out str))
			{
				exprVal = expr[1];
				return true;
			}
			exprVal = null;
			return false;
		}
	}
}
