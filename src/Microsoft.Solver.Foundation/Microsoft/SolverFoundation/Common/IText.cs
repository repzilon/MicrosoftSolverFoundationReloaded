using System.IO;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> A TextVersionImpl that also has access to the actual text.
	/// </summary>
	internal interface IText
	{
		/// <summary> Get the version.
		/// </summary>
		ITextVersion Version { get; }

		/// <summary> Get a TextReader.
		/// </summary>
		TextReader GetReader(int ichMin);
	}
}
