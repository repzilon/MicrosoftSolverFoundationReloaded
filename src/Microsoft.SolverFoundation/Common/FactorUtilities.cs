namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Declare enums and extension methods for them
	/// </summary>
	internal static class FactorUtilities
	{
		/// <summary> Does the variable meet the requirements to be allowed in the Normal section?
		/// </summary>
		public static bool MayBeNormal(this VarSpecial x)
		{
			if (x != 0 && x != VarSpecial.QDiagonal)
			{
				return x == (VarSpecial)5;
			}
			return true;
		}
	}
}
