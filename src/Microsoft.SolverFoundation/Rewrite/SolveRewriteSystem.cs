using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class SolveRewriteSystem : RewriteSystem
	{
		public readonly BuiltinSolveSymbols SolveSymbols;

		public readonly Action<string, IEnumerable<KeyValuePair<string, Rational>>> ExcelOutputBindingDel;

		/// <summary>
		///
		/// </summary>
		/// <param name="excelBindParamDel">delegate with takes ValueTable, address as string(for the excel table/range) and array of keys 
		/// which is used for table binding</param>
		/// <param name="excelBindOutDel">M5 symtax</param>
		public SolveRewriteSystem(Action<ValueTableAdapter, string, string[]> excelBindParamDel, Action<string, IEnumerable<KeyValuePair<string, Rational>>> excelBindOutDel)
			: base(excelBindParamDel)
		{
			SolveSymbols = new BuiltinSolveSymbols(this);
			ExcelOutputBindingDel = excelBindOutDel;
		}

		public SolveRewriteSystem(Action<string, IEnumerable<KeyValuePair<string, Rational>>> excelBindOutDel)
		{
			SolveSymbols = new BuiltinSolveSymbols(this);
			ExcelOutputBindingDel = excelBindOutDel;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="excelBindParamDel">delegate with takes ValueTable, address as string(for the excel table/range) and array of keys 
		/// which is used for table binding</param>
		public SolveRewriteSystem(Action<ValueTableAdapter, string, string[]> excelBindParamDel)
			: base(excelBindParamDel)
		{
			SolveSymbols = new BuiltinSolveSymbols(this);
		}

		public SolveRewriteSystem()
		{
			SolveSymbols = new BuiltinSolveSymbols(this);
		}
	}
}
