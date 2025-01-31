using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.SolverFoundation.Common
{
	internal class StaticText : IText, ITextVersion
	{
		private readonly string _path;

		public virtual ITextVersion Version => this;

		public virtual string Path => _path;

		public StaticText(string path)
		{
			_path = path;
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		public virtual TextReader GetReader(int ichInit)
		{
			Stream stream = new FileStream(_path, FileMode.Open, FileAccess.Read);
			stream.Seek(ichInit, SeekOrigin.Begin);
			return new StreamReader(stream);
		}

		public virtual bool SameStream(ITextVersion tvr)
		{
			return tvr == this;
		}

		public virtual bool SameVersion(ITextVersion tvr)
		{
			return tvr == this;
		}

		public virtual bool MapSpan(ref TextSpan span)
		{
			return span.Version == this;
		}

		public virtual bool MapToSame(ref TextSpan span1, ref TextSpan span2)
		{
			if (span1.Version == this)
			{
				return span2.Version == this;
			}
			return false;
		}
	}
}
