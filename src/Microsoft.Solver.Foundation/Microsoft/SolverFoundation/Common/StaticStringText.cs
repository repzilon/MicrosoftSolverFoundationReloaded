using System.IO;

namespace Microsoft.SolverFoundation.Common
{
	internal class StaticStringText : StaticText
	{
		private readonly string _text;

		public StaticStringText(string path, string text)
			: base(path)
		{
			_text = text;
		}

		public override TextReader GetReader(int ichInit)
		{
			if (ichInit == 0)
			{
				return new StringReader(_text);
			}
			return new StreamReader(_text.Substring(ichInit));
		}
	}
}
