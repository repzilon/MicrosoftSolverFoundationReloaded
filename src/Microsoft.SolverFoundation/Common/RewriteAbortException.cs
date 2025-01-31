using System.Runtime.Serialization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	internal class RewriteAbortException : MsfException
	{
		public RewriteAbortException()
			: base(Resources.Aborted)
		{
		}

		public RewriteAbortException(string strMessage)
			: base(strMessage)
		{
		}

		protected RewriteAbortException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
