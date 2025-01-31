using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;

namespace Microsoft.SolverFoundation.Common
{
	internal class CompressedFileText : StaticText
	{
		private string _path;

		public CompressedFileText(string path)
			: base(path)
		{
			_path = path;
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		public override TextReader GetReader(int ichInit)
		{
			Stream stream = new GZipStream(new FileStream(_path, FileMode.Open, FileAccess.Read), CompressionMode.Decompress);
			return new StreamReader(stream);
		}
	}
}
