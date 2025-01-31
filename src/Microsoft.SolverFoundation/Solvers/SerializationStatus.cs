using System.Globalization;
using System.IO;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Encapsulates error conditions occuring during serialization.  Success is indicated by the Status Code member being set to OK
	/// </summary>
	public class SerializationStatus
	{
		/// <summary>
		/// Indication of success/failure-code during ConstraintSystem serialization.  OK indicates that the operation was successful.  On error, see the message text for 
		/// further informaiton on the error.
		/// </summary>
		public enum Status
		{
			/// <summary>
			/// Indicates an internal error in the parser -- unhandled conditions.
			/// </summary>
			INTERNALERROR,
			/// <summary>
			/// Indicates a failure when writing to the output stream on Save
			/// </summary>
			WRITEFAILED,
			/// <summary>
			/// Indicates an error duringparsing of a model.  
			/// </summary>
			UNABLETOPARSEMODEL,
			/// <summary>
			/// Inidicates an error when parsing the header block.  
			/// </summary>
			UNABLETOPARSEHEADER,
			/// <summary>
			/// Indicates an error when parsing a unary operator 
			/// </summary>
			UNABLETOPARSEUNARYTERM,
			/// <summary>
			/// Indicates a failure when parsing a term name
			/// </summary>
			UNABLETOPARSETERMNAME,
			/// <summary>
			/// Indicates an error when parsing a domain
			/// </summary>
			UNABLETOPARSEDOMAIN,
			/// <summary>
			/// Indicates an error when parsing a line in the input stream.  
			/// </summary>
			UNABLETOPARSELINE,
			/// <summary>
			/// Indicates an error when parsing a number
			/// </summary>
			UNABLETOPARSENUMBER,
			/// <summary>
			/// Indicates an error when parsing a string.
			/// </summary>
			UNABLETOPARSESTRING,
			/// <summary>
			/// Indicates an error when parsing symbols in a symbol set
			/// </summary>
			NOSYMBOLSINSET,
			/// <summary>
			/// Indicates a problem parsing the integers in an integer set
			/// </summary>
			NOINTSINSET,
			/// <summary>
			/// Indicates an error when parsing a variable line
			/// </summary>
			UNABLETOPARSEVARIABLE,
			/// <summary>
			/// Indicates an error when parsing multiple terms on one line.
			/// </summary>
			UNABLETOPARSEMULTITERM,
			/// <summary>
			/// Indicates an error when parsing a symbol refrence
			/// </summary>
			UNABLETOPARSESYMBOL,
			/// <summary>
			/// Indicates an error when parsing a constraint line
			/// </summary>
			UNABLETOPARSECONSTRAINT,
			/// <summary>
			/// Indicates an error when parsing an M of N term
			/// </summary>
			UNABLETOPARSEMOFN,
			/// <summary>
			/// Indicates an error when parsing a Power term
			/// </summary>
			UNABLETOPARSEPOWER,
			/// <summary>
			/// Indicates an error when parsing an index term
			/// </summary>
			UNABLETOPARSEINDEX,
			/// <summary>
			/// Indicates an error when parsing a symbol reference
			/// </summary>
			UNABLETOPARSESYMBOLREF,
			/// <summary>
			/// Duplicate variable name detected.
			/// </summary>
			DUPLICATEVARNAME,
			/// <summary>
			/// Invalid number of terms in an "implies" operator.
			/// </summary>
			INVALIDNUMTERMSIMPLIES,
			/// <summary>
			/// Unable to parse non-decisive variable definition.
			/// </summary>
			UNABLETOPARSENONDECISIVE,
			/// <summary>
			/// Error parsing non-decisive tokens.
			/// </summary>
			NONDECISIVEPARSE,
			/// <summary>
			/// Successful operation
			/// </summary>
			OK,
			/// <summary>
			/// Failed when parsing IsElementOf term.
			/// </summary>
			UNABLETOPARSEISELEMENTOF
		}

		private SerializationStatus previous;

		private string stringContext;

		private Status code;

		private string message;

		private uint line;

		private string token;

		/// <summary>
		/// Full text of line where a parsing error occurred on load
		/// </summary>
		public string Source => stringContext;

		/// <summary>
		/// Error code which is an enumeration.  Indicates parsing and/or saving failures.  There is a distinct OK value which indicates success.
		/// </summary>
		public Status Code => code;

		/// <summary>
		/// Distinct text per source of failure.  
		/// </summary>
		public string Message => message;

		/// <summary>
		/// This is set to the unrecognized parse token when parsing.
		/// </summary>
		public string Token => token;

		/// <summary>
		/// This is set to the complete line of text when a failure occurs during parsing.
		/// </summary>
		internal uint SourceLine => line;

		/// <summary>
		/// default construction implies everything is OK.
		/// </summary>
		internal SerializationStatus()
		{
			previous = null;
			stringContext = string.Empty;
			code = Status.OK;
			message = string.Empty;
			line = 0u;
			token = string.Empty;
		}

		/// <summary>
		/// Used from the 'save' side of serialization.
		/// </summary>
		/// <param name="cd">enumeration of failure</param>
		/// <param name="errorMessage">unique string for failure</param>
		/// <param name="lineNo">line number for the failure.</param>
		internal SerializationStatus(Status cd, string errorMessage, uint lineNo)
		{
			previous = null;
			code = cd;
			message = errorMessage;
			line = lineNo;
			token = string.Empty;
			previous = null;
			stringContext = string.Empty;
		}

		/// <summary>
		/// Used from the parsing side of serialization
		/// </summary>
		/// <param name="cd">enumeration of the failure</param>
		/// <param name="errorMessage">uniuqe string for failure</param>
		/// <param name="lineNo">line number for the failure</param>
		/// <param name="_token">the parser token where the issue is noticed</param>
		/// <param name="fullText">the full line of text where the issue is noticed</param>
		/// <param name="prev">the previous Status in the stack from the parser.</param>
		internal SerializationStatus(Status cd, string errorMessage, uint lineNo, string _token, string fullText, SerializationStatus prev)
		{
			previous = prev;
			message = errorMessage;
			code = cd;
			line = lineNo;
			token = _token;
			stringContext = fullText;
		}

		/// <summary>
		/// Traces the source of an error the stream.  includes error code, token (if available), line number (or zero if not availble) , and line contents (if available)
		/// </summary>
		/// <param name="writer"></param>
		public void Trace(TextWriter writer)
		{
			for (SerializationStatus serializationStatus = this; serializationStatus != null; serializationStatus = serializationStatus.previous)
			{
				if (serializationStatus.Code != Status.OK)
				{
					writer.WriteLine(string.Format(CultureInfo.InvariantCulture, Resources.ErrorCode0Token3Line12, serializationStatus.code, serializationStatus.line, serializationStatus.message, serializationStatus.Token));
					if (!string.IsNullOrEmpty(serializationStatus.stringContext))
					{
						writer.WriteLine(serializationStatus.stringContext);
					}
					else
					{
						writer.WriteLine();
					}
				}
			}
		}
	}
}
