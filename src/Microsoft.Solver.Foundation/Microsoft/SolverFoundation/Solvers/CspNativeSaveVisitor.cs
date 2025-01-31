using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Concrete implementation of the Visitor pattern which produces the CspNative Format for ConstraintSystem models
	/// </summary>
	internal class CspNativeSaveVisitor : IVisitor
	{
		public class TermNameEntry
		{
			private bool _generated;

			private string _name;

			public bool Generated
			{
				get
				{
					return _generated;
				}
				set
				{
					_generated = value;
				}
			}

			public string TermName
			{
				get
				{
					return _name;
				}
				set
				{
					_name = value;
				}
			}

			public TermNameEntry()
			{
				_generated = false;
			}
		}

		/// <summary>
		/// stream writer used when saving.
		/// </summary>
		private TextWriter writer;

		/// <summary>
		/// The ConstraintSystem which we are visiting.
		/// </summary>
		private ConstraintSystem solver;

		/// <summary>
		/// Symbol table for all domains
		/// </summary>
		private Dictionary<CspSolverDomain, TermNameEntry> domainTable = new Dictionary<CspSolverDomain, TermNameEntry>();

		/// <summary>
		/// Used when saving.  The visitor pattern has no facilities for passing the extra pattern.  I has to be part of the 
		/// Visitor concrete object.
		/// </summary>
		private SerializationStatus outError;

		private int domainCounter;

		internal CspNativeSaveVisitor()
		{
			outError = new SerializationStatus();
		}

		private static string GenerateFormattedName(CspVariable v)
		{
			if (v.Key is string)
			{
				return FormatVarName(v.Key as string, generated: false);
			}
			return FormatVarName(string.Format(CultureInfo.InvariantCulture, "{0}{1}", new object[2] { "V_", v.Ordinal }), generated: true);
		}

		private static string GenerateFormattedName(CspSolverTerm t)
		{
			if (t is CspVariable v)
			{
				return GenerateFormattedName(v);
			}
			return FormatTermName(string.Format(CultureInfo.InvariantCulture, "{0}{1}", new object[2] { "T_", t.Ordinal }));
		}

		/// <summary>
		/// Helper which generates a string when a domain is given.  This name will have the "generated format".  It does not
		/// include the quotes.
		/// </summary>
		/// <param name="d"></param>
		/// <returns></returns>
		private TermNameEntry GenerateName(CspSolverDomain d)
		{
			TermNameEntry termNameEntry = new TermNameEntry();
			termNameEntry.Generated = true;
			termNameEntry.TermName = string.Format(CultureInfo.InvariantCulture, "{0}{1}", new object[2]
			{
				"D_",
				domainCounter++
			});
			return termNameEntry;
		}

		/// <summary>
		/// Helper which gteneratees a string when a ConstraintSystem is given.  This name will have the "generated format".
		/// It does not include the quotes.
		/// </summary>
		/// <param name="solv"></param>
		/// <returns></returns>
		private static string GenerateName(ConstraintSystem solv)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}{1}", new object[2] { "S_", 0 });
		}

		/// <summary>
		/// This adds escape characters and quotes to a symbol name as directed.
		/// </summary>
		/// <param name="inputString"></param>
		/// <param name="quoteChar"></param>
		/// <returns></returns>
		internal static string EscapeAndQuoteChars(string inputString, char quoteChar)
		{
			StringBuilder stringBuilder = new StringBuilder();
			int i = 0;
			char[] array = inputString.ToCharArray();
			stringBuilder.Append(quoteChar);
			for (; i < array.Length; i++)
			{
				char c = array[i];
				if (c == '"' || c == '\'' || c == '\\')
				{
					stringBuilder.Append('\\');
				}
				stringBuilder.Append(array[i]);
			}
			stringBuilder.Append(quoteChar);
			return stringBuilder.ToString();
		}

		/// <summary>
		/// For a domain, this will format the domain name in preparation for output.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		internal static string FormatDomainName(TermNameEntry name)
		{
			if (name.Generated)
			{
				return EscapeAndQuoteChars(name.TermName, '\'');
			}
			return EscapeAndQuoteChars(name.TermName, '"');
		}

		/// <summary>
		/// for a variable this will format the name in preparation for output.
		/// </summary>
		/// <param name="strName"></param>
		/// <param name="generated"></param>
		/// <returns></returns>
		internal static string FormatVarName(string strName, bool generated)
		{
			if (generated)
			{
				return EscapeAndQuoteChars(strName, '\'');
			}
			return EscapeAndQuoteChars(strName, '"');
		}

		/// <summary>
		/// Format term this will format the name in preparation for output.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		internal static string FormatTermName(string name)
		{
			return EscapeAndQuoteChars(name, '\'');
		}

		/// <summary>
		/// For a symbol this will format the name in preparation for output
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		internal static string FormatSymbol(string symbol)
		{
			return EscapeAndQuoteChars(symbol, '"');
		}

		internal static string FormatDecimal(int val, int scale)
		{
			double value = (double)val / (double)scale;
			int num = (int)Math.Ceiling(Math.Log10(Math.Abs(value))) + scale.ToString(CultureInfo.InvariantCulture).Length - 1;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("0.");
			for (int i = 0; i < num; i++)
			{
				stringBuilder.Append('#');
			}
			stringBuilder.Append("e+0");
			return value.ToString(stringBuilder.ToString(), CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// This helper method figures out what to do with a particular term.  It figures out the base type for the term
		/// and either outputs the name or the definition.
		/// </summary>
		/// <param name="t"></param>
		/// <param name="definingTerm"></param>
		internal void WriteTermString(ref CspSolverTerm t, bool definingTerm)
		{
			if (t is CspVariable)
			{
				CspVariable cspVariable = t as CspVariable;
				if (cspVariable == solver.True)
				{
					writer.Write("TRUE");
				}
				else if (cspVariable == solver.False)
				{
					writer.Write("FALSE");
				}
				else if (cspVariable.IsConstant)
				{
					switch (cspVariable.Kind)
					{
					case CspDomain.ValueKind.Symbol:
						writer.Write(FormatDomainName(domainTable[cspVariable.Symbols]));
						writer.Write(":");
						writer.Write(FormatSymbol(cspVariable.Key.ToString()));
						break;
					case CspDomain.ValueKind.Integer:
						writer.Write(cspVariable.BaseValueSet.First);
						break;
					case CspDomain.ValueKind.Decimal:
						writer.Write(FormatDecimal(cspVariable.BaseValueSet.First, cspVariable.BaseValueSet.Scale));
						break;
					default:
						throw new ArgumentException(Resources.UnknownKindDetectedInSave);
					}
				}
				else
				{
					writer.Write(GenerateFormattedName(cspVariable));
				}
			}
			else
			{
				writer.Write(GenerateFormattedName(t));
			}
		}

		/// <summary>
		/// If an operator may have multiple terms this is called to output the operator, the name, and the 
		/// references to each term in the multi-term operator.
		/// </summary>
		/// <param name="operatorTerm"></param>
		/// <param name="term"></param>
		internal void VisitMultiTermOperator(string operatorTerm, CspSolverTerm term)
		{
			writer.Write(operatorTerm);
			writer.Write(' ');
			WriteTermString(ref term, definingTerm: true);
			foreach (CspSolverTerm input in term.Inputs)
			{
				writer.Write(' ');
				CspSolverTerm t = input;
				WriteTermString(ref t, definingTerm: false);
			}
			writer.WriteLine();
		}

		/// <summary> IsElementOf operator.
		/// </summary>
		/// <param name="operatorTerm"></param>
		/// <param name="term"></param>
		internal void VisitIsElementOfOperator(string operatorTerm, IsElementOf term)
		{
			if (term == null || term.Width != 1 || term.Domain == null)
			{
				throw new ArgumentNullException(Resources.InvalidIsElementOfDetectedInSave);
			}
			CspSolverDomain domain = term.Domain;
			writer.Write(operatorTerm);
			writer.Write(' ');
			CspSolverTerm t = term;
			WriteTermString(ref t, definingTerm: true);
			writer.Write(' ');
			t = term.Args[0];
			WriteTermString(ref t, definingTerm: false);
			writer.Write(' ');
			TermNameEntry value = null;
			if (!domainTable.TryGetValue(domain, out value))
			{
				throw new ArgumentException(Resources.InvalidIsElementOfDetectedInSave);
			}
			writer.WriteLine(FormatDomainName(value));
		}

		/// <summary>
		/// This is very similar to a multi-term operator except that the M of N structures are more strict.
		/// </summary>
		/// <param name="operatorTerm"></param>
		/// <param name="term"></param>
		internal void VisitMofNTermOperator(string operatorTerm, MOfNConstraint term)
		{
			writer.Write(operatorTerm);
			writer.Write(' ');
			CspSolverTerm t = term;
			WriteTermString(ref t, definingTerm: true);
			writer.Write(' ');
			writer.Write(term.M);
			foreach (CspSolverTerm input in term.Inputs)
			{
				writer.Write(' ');
				t = input;
				WriteTermString(ref t, definingTerm: false);
			}
			writer.WriteLine();
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="lessTerm"></param>
		public void Visit(Less lessTerm)
		{
			VisitMultiTermOperator("LESS", lessTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="greaterTerm"></param>
		public void Visit(Greater greaterTerm)
		{
			VisitMultiTermOperator("GREATER", greaterTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="lessEqualTerm"></param>
		public void Visit(LessEqual lessEqualTerm)
		{
			VisitMultiTermOperator("LESSEQUAL", lessEqualTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="greaterEqualTerm"></param>
		public void Visit(GreaterEqual greaterEqualTerm)
		{
			VisitMultiTermOperator("GREATEREQUAL", greaterEqualTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="equalTerm"></param>
		public void Visit(Equal equalTerm)
		{
			VisitMultiTermOperator("EQUAL", equalTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="unequalTerm"></param>
		public void Visit(Unequal unequalTerm)
		{
			VisitMultiTermOperator("UNEQUAL", unequalTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="impliesTerm"></param>
		public void Visit(BooleanImplies impliesTerm)
		{
			VisitMultiTermOperator("IMPLIES", impliesTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="notTerm"></param>
		public void Visit(BooleanNot notTerm)
		{
			VisitMultiTermOperator("BOOLEANNOT", notTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="andTerm"></param>
		public void Visit(BooleanAnd andTerm)
		{
			VisitMultiTermOperator("BOOLEANAND", andTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="orTerm"></param>
		public void Visit(BooleanOr orTerm)
		{
			VisitMultiTermOperator("BOOLEANOR", orTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="bEqualTerm"></param>
		public void Visit(BooleanEqual bEqualTerm)
		{
			VisitMultiTermOperator("EQUAL", bEqualTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="bUnequalTerm"></param>
		public void Visit(BooleanUnequal bUnequalTerm)
		{
			VisitMultiTermOperator("UNEQUAL", bUnequalTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="productTerm"></param>
		public void Visit(Product productTerm)
		{
			VisitMultiTermOperator("PRODUCT", productTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="absTerm"></param>
		public void Visit(Abs absTerm)
		{
			VisitMultiTermOperator("ABSVALUE", absTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator
		/// </summary>
		/// <param name="sumTerm"></param>
		public void Visit(Sum sumTerm)
		{
			VisitMultiTermOperator("SUM", sumTerm);
		}

		/// <summary>
		/// The work is delegated to the Multi-term operator.  This appears odd except that the format for a single
		/// term is identical, there just happens to only be one of them.
		/// </summary>
		/// <param name="negTerm"></param>
		public void Visit(Negate negTerm)
		{
			VisitMultiTermOperator("NEGATE", negTerm);
		}

		/// <summary>
		/// This writes the Constraint keyword and the terms after.
		/// </summary>
		/// <param name="constraint"></param>
		public void VisitConstraint(ref CspSolverTerm constraint)
		{
			writer.Write("CONSTRAINT");
			writer.Write(' ');
			writer.Write(GenerateFormattedName(constraint));
			writer.WriteLine();
		}

		/// <summary>
		/// This writes the goal keyword and the terms after.
		/// </summary>
		/// <param name="goal"></param>
		public void VisitGoal(ref CspSolverTerm goal)
		{
			writer.Write("GOAL");
			writer.Write(' ');
			writer.Write(GenerateFormattedName(goal));
			writer.WriteLine();
		}

		/// <summary>
		/// This writes the power keyword, name, term name, and integer exponent.
		/// </summary>
		/// <param name="powTerm"></param>
		public void Visit(Power powTerm)
		{
			writer.Write("POWER");
			writer.Write(' ');
			writer.Write(GenerateFormattedName(powTerm));
			foreach (CspSolverTerm input in powTerm.Inputs)
			{
				CspSolverTerm t = input;
				writer.Write(' ');
				WriteTermString(ref t, definingTerm: false);
			}
			writer.Write(' ');
			writer.WriteLine(powTerm.Exponent);
		}

		/// <summary> Delegate to helper for IsElementOf terms
		/// </summary>
		/// <param name="isElementOfTerm"></param>
		public void Visit(IsElementOf isElementOfTerm)
		{
			VisitIsElementOfOperator("ISELEMENTOF", isElementOfTerm);
		}

		/// <summary>
		/// The work is delegated to the helper for M of N terms
		/// </summary>
		/// <param name="exMofNTerm"></param>
		public void Visit(ExactlyMOfN exMofNTerm)
		{
			VisitMofNTermOperator("EXACTLYMOFN", exMofNTerm);
		}

		/// <summary>
		/// The work is delegated to the helper for M of N terms
		/// </summary>
		/// <param name="atMMofNTerm"></param>
		public void Visit(AtMostMOfN atMMofNTerm)
		{
			VisitMofNTermOperator("ATMOSTMOFN", atMMofNTerm);
		}

		/// <summary>
		/// Outputs the intmap term.  
		/// </summary>
		/// <param name="intmapTerm"></param>
		public void Visit(IntMap intmapTerm)
		{
			int num = 0;
			int num2 = intmapTerm.Width - intmapTerm.Dimension;
			bool flag = true;
			writer.Write("INDEX");
			writer.Write(' ');
			writer.Write(GenerateFormattedName(intmapTerm));
			foreach (CspSolverTerm input in intmapTerm.Inputs)
			{
				if (num < num2)
				{
					writer.Write(' ');
					CspSolverTerm t = input;
					WriteTermString(ref t, definingTerm: false);
				}
				else
				{
					if (flag)
					{
						flag = false;
						writer.Write(' ');
						writer.Write("AXES");
					}
					writer.Write(' ');
					CspSolverTerm t2 = input;
					WriteTermString(ref t2, definingTerm: false);
				}
				num++;
			}
			writer.WriteLine();
		}

		/// <summary>
		/// This writes the domain definition regardless of the type of domain.
		/// </summary>
		/// <param name="d"></param>
		public void Visit(CspSolverDomain d)
		{
			outError = new SerializationStatus();
			TermNameEntry value = null;
			if (domainTable.TryGetValue(d, out value))
			{
				return;
			}
			value = GenerateName(d);
			domainTable.Add(d, value);
			if (d is CspSymbolDomain cspSymbolDomain)
			{
				writer.Write("SYMBOLDOMAIN");
				writer.Write(' ');
				writer.Write(FormatDomainName(value));
				writer.Write(' ');
				foreach (int item in cspSymbolDomain.Forward())
				{
					writer.Write(FormatSymbol(cspSymbolDomain.GetSymbol(item)));
					writer.Write(' ');
				}
			}
			else if (d is CspIntervalDomain cspIntervalDomain)
			{
				writer.Write("INTERVAL");
				writer.Write(' ');
				writer.Write(FormatDomainName(value));
				writer.Write(' ');
				if (cspIntervalDomain.Kind == CspDomain.ValueKind.Decimal)
				{
					writer.Write(FormatDecimal(cspIntervalDomain.First, cspIntervalDomain.Scale));
				}
				else
				{
					writer.Write(cspIntervalDomain.First);
				}
				writer.Write(' ');
				if (cspIntervalDomain.Kind == CspDomain.ValueKind.Decimal)
				{
					writer.Write(FormatDecimal(cspIntervalDomain.Last, cspIntervalDomain.Scale));
				}
				else
				{
					writer.Write(cspIntervalDomain.Last);
				}
			}
			else if (d is sEmptyDomain)
			{
				writer.Write("EMPTY");
			}
			else if (d is CspSetDomain cspSetDomain)
			{
				writer.Write("DOMAIN");
				writer.Write(' ');
				writer.Write(FormatDomainName(value));
				writer.Write(' ');
				foreach (int item2 in cspSetDomain.Forward())
				{
					if (cspSetDomain.Kind == CspDomain.ValueKind.Decimal)
					{
						writer.Write(FormatDecimal(item2, cspSetDomain.Scale));
					}
					else
					{
						writer.Write(item2);
					}
					writer.Write(' ');
				}
			}
			else
			{
				outError = new SerializationStatus(SerializationStatus.Status.WRITEFAILED, Resources.UnknownDomainType, 0u);
			}
			writer.WriteLine();
		}

		/// <summary>
		/// Visits the definition of a variable (as opposed to a reference to it).
		/// </summary>
		/// <param name="variable"></param>
		public void VisitDefinition(ref CspVariable variable)
		{
			if (variable is CspCompositeVariable)
			{
				throw new NotImplementedException();
			}
			writer.Write("VARIABLE");
			writer.Write(' ');
			if (variable.TermKind != 0)
			{
				switch (variable.TermKind)
				{
				case CspSolverTerm.TermKinds.CompositeVariable:
					writer.Write("&");
					writer.Write(":");
					break;
				case CspSolverTerm.TermKinds.TemplateVariable:
					writer.Write(":");
					writer.Write(":");
					break;
				}
			}
			writer.Write(GenerateFormattedName(variable));
			writer.Write(' ');
			TermNameEntry value = null;
			domainTable.TryGetValue(variable.Values[0], out value);
			if (variable.IsBoolean)
			{
				writer.WriteLine("BOOLEAN");
			}
			else
			{
				writer.WriteLine(FormatDomainName(value));
			}
		}

		/// <summary>
		/// Visits a reference to a variable usually from within an expression.
		/// </summary>
		/// <param name="variable"></param>
		public void Visit(CspVariable variable)
		{
			if (variable is CspCompositeVariable)
			{
				throw new NotImplementedException();
			}
			writer.Write(GenerateFormattedName(variable));
		}

		/// <summary>
		/// Writes the header block for the required format..
		/// </summary>
		internal void WriteHeaderBlock()
		{
			writer.WriteLine("HEADERSTART");
			writer.Write("MODELNAME");
			writer.Write(' ');
			writer.WriteLine(EscapeAndQuoteChars(GenerateName(solver), '\''));
			writer.Write("MODELTYPE");
			writer.Write(' ');
			writer.WriteLine("CSPNATIVE");
			writer.Write("VERSION");
			writer.Write(' ');
			writer.WriteLine(ConstraintSystem.Version);
			writer.Write("SAVEDATE");
			writer.Write(' ');
			writer.WriteLine(DateTime.Today.ToString(DateTimeFormatInfo.InvariantInfo.UniversalSortableDateTimePattern, CultureInfo.InvariantCulture));
			writer.Write("SCALEFACTOR");
			writer.Write(' ');
			writer.WriteLine(solver.Precision);
			writer.WriteLine("HEADEREND");
		}

		/// <summary>
		/// Main entry point into the serialization code.
		/// </summary>
		/// <param name="cspSolver"></param>
		/// <param name="textWriter"></param>
		/// <returns></returns>
		internal SerializationStatus Save(ConstraintSystem cspSolver, TextWriter textWriter)
		{
			domainCounter = 0;
			writer = textWriter;
			writer.WriteLine("MODELSTART");
			solver = cspSolver;
			WriteHeaderBlock();
			solver.Accept(this);
			writer.WriteLine("MODELEND");
			return outError;
		}
	}
}
