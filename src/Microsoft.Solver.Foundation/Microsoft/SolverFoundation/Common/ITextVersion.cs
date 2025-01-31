namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Used to identify a version of a text stream and map between different
	/// versions of the same text stream. Doesn't guarantee access to the actual text.
	/// </summary>
	internal interface ITextVersion
	{
		/// <summary> The path.
		/// </summary>
		string Path { get; }

		/// <summary> Indicates whether the ITextVersion refers to the same underlying stream as another ITextVersion.
		/// </summary>
		bool SameStream(ITextVersion tvr);

		/// <summary> Indicates whether two ITextVersions have the same version.
		/// </summary>
		bool SameVersion(ITextVersion tvr);

		/// <summary> MapSpan.
		/// </summary>
		bool MapSpan(ref TextSpan span);

		/// <summary> MapToSame.
		/// </summary>
		bool MapToSame(ref TextSpan span1, ref TextSpan span2);
	}
}
