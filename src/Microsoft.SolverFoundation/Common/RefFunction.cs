namespace Microsoft.SolverFoundation.Common
{
	/// <summary> callback
	/// </summary>
	/// <typeparam name="S"></typeparam>
	/// <typeparam name="R"></typeparam>
	/// <param name="s"></param>
	/// <returns></returns>
	internal delegate R RefFunction<S, R>(ref S s);
	/// <summary> callback
	/// </summary>
	/// <typeparam name="S1"></typeparam>
	/// <typeparam name="S2"></typeparam>
	/// <typeparam name="R"></typeparam>
	/// <param name="s1"></param>
	/// <param name="s2"></param>
	/// <returns></returns>
	internal delegate R RefFunction<S1, S2, R>(ref S1 s1, ref S2 s2);
}
