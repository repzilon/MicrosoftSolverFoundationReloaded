using System.Globalization;

namespace Microsoft.SolverFoundation.Common
{
	internal class ErrObj
	{
		public enum ErrorLevel
		{
			Warn1,
			Warn2,
			Warn3,
			Warn4,
			Error,
			Fatal
		}

		private readonly int _id;

		private readonly string _str;

		private readonly ErrorLevel _lev;

		public int Id => _id;

		public string MessageText => _str;

		public ErrorLevel Level => _lev;

		public ErrObj(int id, string str)
			: this(id, str, ErrorLevel.Error)
		{
		}

		public ErrObj(int id, string str, ErrorLevel lev)
		{
			_id = id;
			_str = str;
			_lev = lev;
		}

		public string Format(params object[] args)
		{
			if (args == null || args.Length == 0)
			{
				return _str;
			}
			return string.Format(CultureInfo.InvariantCulture, _str, args);
		}
	}
}
