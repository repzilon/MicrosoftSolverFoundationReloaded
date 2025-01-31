namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Listeners that are annotated with an extra bit of information
	///   that is systematically passed to delegates that subscribe to it
	/// </summary>
	internal class AnnotatedListener<Content>
	{
		public delegate bool Listener(Content c);

		private readonly Content _info;

		private readonly Listener _listen;

		/// <summary>
		///   Generates a listener that, when called, will pass
		///   parameter c to the annotated listener l
		/// </summary>
		public static BasicEvent.Listener Generate(Content c, Listener l)
		{
			AnnotatedListener<Content> @object = new AnnotatedListener<Content>(c, l);
			return @object.DispatchInfo;
		}

		private AnnotatedListener(Content c, Listener l)
		{
			_info = c;
			_listen = l;
		}

		private bool DispatchInfo()
		{
			return _listen(_info);
		}
	}
}
