namespace Microsoft.SolverFoundation.Common
{
	internal class ErrorToken : Token
	{
		private bool _fNested;

		private ErrObj _eid;

		private object[] _args;

		public override bool Nested
		{
			get
			{
				return _fNested;
			}
			internal set
			{
				_fNested = value;
			}
		}

		public string Message => _eid.Format(_args);

		public ErrObj Eid => _eid;

		public object[] Args => _args;

		public ErrorToken(ErrObj eid)
			: this(eid, null)
		{
		}

		public ErrorToken(ErrObj eid, params object[] args)
			: base(TokKind.Error)
		{
			_eid = eid;
			_args = args;
		}
	}
}
