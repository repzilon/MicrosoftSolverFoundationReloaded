using System.Text;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class DefaultExpressionFormatter : IExpressionFormatter
	{
		public void BeginInvocationArgs(StringBuilder sb, Invocation inv)
		{
		}

		public void BeginOneArg(StringBuilder sb, Invocation inv)
		{
		}

		public void EndOneArg(StringBuilder sb, bool last, Invocation inv)
		{
		}

		public void EndInvocationArgs(StringBuilder sb, Invocation inv)
		{
		}

		public void BeforeBinaryOperator(StringBuilder sb, Invocation inv)
		{
		}

		public void AfterBinaryOperator(StringBuilder sb, Invocation inv)
		{
		}

		public bool ShouldForceFullInvocationForm(Invocation inv)
		{
			return false;
		}
	}
}
