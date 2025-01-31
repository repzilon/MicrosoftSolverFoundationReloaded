using System.IO;

namespace Microsoft.SolverFoundation.Common
{
	internal class FileText : StaticText
	{
		private readonly TextReader _reader;

		public FileText(string path, TextReader reader)
			: base(path)
		{
			_reader = reader;
		}

		public override TextReader GetReader(int ichInit)
		{
			return _reader;
		}
	}
}
