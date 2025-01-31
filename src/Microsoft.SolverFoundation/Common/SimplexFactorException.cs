using System.Runtime.Serialization;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Common
{
	internal class SimplexFactorException : MsfException
	{
		private FactorResultFlags _flags;

		public FactorResultFlags Flags => _flags;

		public SimplexFactorException(FactorResultFlags flags)
		{
			_flags = flags;
		}

		protected SimplexFactorException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
