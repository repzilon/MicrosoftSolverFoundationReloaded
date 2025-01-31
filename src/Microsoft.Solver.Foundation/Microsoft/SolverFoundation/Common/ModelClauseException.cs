using System.Runtime.Serialization;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Common
{
	internal class ModelClauseException : ModelException
	{
		private Expression _expr;

		public Expression Expr => _expr;

		public OmlParseExceptionReason Reason { get; private set; }

		internal ModelClauseException(string strMsg, Expression expr, OmlParseExceptionReason reason = OmlParseExceptionReason.NotSpecified)
			: base(strMsg)
		{
			_expr = expr;
			Reason = reason;
		}

		protected ModelClauseException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
