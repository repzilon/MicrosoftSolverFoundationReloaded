using System;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>An exception that is thrown when an OML model has invalid syntax.
	/// </summary>
	[Serializable]
	public class OmlParseException : MsfException
	{
		/// <summary>Information about where the error occurs.
		/// </summary>
		public OmlParseExceptionLocation Location { get; private set; }

		/// <summary>Information about the reason for the exception.
		/// </summary>
		public OmlParseExceptionReason Reason { get; private set; }

		/// <summary>Create a new instance with the specified message.
		/// </summary>
		public OmlParseException(string strMsg)
			: base(strMsg)
		{
		}

		internal OmlParseException(string strMsg, Exception innerException, SrcPos spos, OmlParseExceptionReason reason)
			: base(strMsg, innerException)
		{
			Location = new OmlParseExceptionLocation(spos);
			Reason = reason;
		}

		/// <summary>Constructor to support serialization.
		/// </summary>
		protected OmlParseException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		internal static OmlParseException Create(ModelClauseException mce)
		{
			if (mce.Expr != null && mce.Expr.PlacementInformation != null)
			{
				PlacementInfo placementInformation = mce.Expr.PlacementInformation;
				placementInformation.Map.MapSpanToPos(placementInformation.Span, out var spos);
				return new OmlParseException(string.Format(CultureInfo.InvariantCulture, Resources.CouldNotParseOMLModel0, new object[1] { mce.Expr }), mce, spos, mce.Reason);
			}
			return new OmlParseException(mce.Message, mce, default(SrcPos), mce.Reason);
		}
	}
}
