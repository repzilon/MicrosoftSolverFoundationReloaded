namespace Microsoft.SolverFoundation.Rewrite
{
	internal enum Precedence : byte
	{
		None,
		Atom,
		Invocation,
		Unary,
		Power,
		Times,
		Plus,
		Compare,
		And,
		AndAlso,
		Xor,
		Or,
		Implies,
		OrElse,
		Condition,
		Rule,
		Replace,
		Function,
		Assign,
		Then,
		Lim
	}
}
