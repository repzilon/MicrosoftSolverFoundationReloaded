using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Filters out tokens based on a predicate.
	/// </summary>
	internal static class TokenFilter
	{
		/// <summary> The one and only entry point. Filters out tokens based on a predicate.
		/// Note that the "predicate" may also transform the token or otherwise act on it.
		/// </summary>
		/// <param name="rgtok">The source token stream.</param>
		/// <param name="pred">The delegate to invoke on each token. If it returns true, the token is yielded.</param>
		/// <returns>The filtered token stream.</returns>
		public static IEnumerable<Token> Filter(IEnumerable<Token> rgtok, RefFunction<Token, bool> pred)
		{
			foreach (Token tok in rgtok)
			{
				Token tokCur = tok;
				if (pred(ref tokCur))
				{
					yield return tokCur;
				}
			}
		}
	}
}
