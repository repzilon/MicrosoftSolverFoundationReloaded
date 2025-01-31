using System.Runtime.InteropServices;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Internal tokens for serialization.  These are all the sytax tokens in the format.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal struct NativeFormatTokens
	{
		internal const string MODELSTART = "MODELSTART";

		internal const string MODELEND = "MODELEND";

		internal const string HEADERSTART = "HEADERSTART";

		internal const string HEADEREND = "HEADEREND";

		internal const string MODELTYPE = "MODELTYPE";

		internal const string CSPNATIVE = "CSPNATIVE";

		internal const string DECLARATIONSTART = "DECLARATIONSTART";

		internal const string DECLARATIONEND = "DECLARATIONEND";

		internal const string MODELNAME = "MODELNAME";

		internal const string SOLVERTYPE = "SOLVERTYPE";

		internal const string VERSION = "VERSION";

		internal const string M5 = "M5";

		internal const string SAVEDATE = "SAVEDATE";

		internal const string SCALEFACTOR = "SCALEFACTOR";

		internal const string VARIABLE = "VARIABLE";

		internal const string CONSTRAINT = "CONSTRAINT";

		internal const string GOAL = "GOAL";

		internal const string SOLVERPREFIX = "S_";

		internal const string DOMAINPREFIX = "D_";

		internal const string VARPREFIX = "V_";

		internal const string TERMPREFIX = "T_";

		internal const string LESS = "LESS";

		internal const string LESSEQUAL = "LESSEQUAL";

		internal const string GREATER = "GREATER";

		internal const string GREATEREQUAL = "GREATEREQUAL";

		internal const string UNEQUAL = "UNEQUAL";

		internal const string EQUAL = "EQUAL";

		internal const string SUM = "SUM";

		internal const string POWER = "POWER";

		internal const string PRODUCT = "PRODUCT";

		internal const string NEGATE = "NEGATE";

		internal const string ABSVALUE = "ABSVALUE";

		internal const string IMPLIES = "IMPLIES";

		internal const string BOOLEANOR = "BOOLEANOR";

		internal const string BOOLEANAND = "BOOLEANAND";

		internal const string BOOLEANNOT = "BOOLEANNOT";

		internal const string BOOLEANEQUAL = "BOOLEANEQUAL";

		internal const string BOOLEANUNEQUAL = "BOOLEANUNEQUAL";

		internal const string ISELEMENTOF = "ISELEMENTOF";

		internal const string EXACTLYMOFN = "EXACTLYMOFN";

		internal const string ATMOSTMOFN = "ATMOSTMOFN";

		internal const string INDEX = "INDEX";

		internal const string AXES = "AXES";

		internal const string TRUE = "TRUE";

		internal const string FALSE = "FALSE";

		internal const string DECIMALSEPARATOR = ".";

		internal const string INTEGERDOMAIN = "DOMAIN";

		internal const string INTERVALDOMAIN = "INTERVAL";

		internal const string DECIMALINTERVAL = "DECIMALINTERVAL";

		internal const string SYMBOLDOMAIN = "SYMBOLDOMAIN";

		internal const string BOOLEANDOMAIN = "BOOLEAN";

		internal const string EMPTY = "EMPTY";

		internal const string COLON = ":";

		internal const string STAR = "*";

		internal const string AMP = "&";

		internal const char SINGLEQUOTE = '\'';

		internal const char DOUBLEQUOTE = '"';

		internal const char SPACE = ' ';

		internal const char ESCAPECHAR = '\\';
	}
}
