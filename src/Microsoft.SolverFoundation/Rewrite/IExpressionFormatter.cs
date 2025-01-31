using System.Text;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal interface IExpressionFormatter
	{
		void BeginInvocationArgs(StringBuilder sb, Invocation inv);

		void BeginOneArg(StringBuilder sb, Invocation inv);

		void EndOneArg(StringBuilder sb, bool last, Invocation inv);

		void EndInvocationArgs(StringBuilder sb, Invocation inv);

		void BeforeBinaryOperator(StringBuilder sb, Invocation inv);

		void AfterBinaryOperator(StringBuilder sb, Invocation inv);

		bool ShouldForceFullInvocationForm(Invocation inv);
	}
}
