namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Generic class used to make backtrackable ints, strings, bools, 
	///   whatever. A Backtrackable (say) int is connected to a Trail of ints,
	///   so that any save operation on the trail allows to later change back
	///   to the previous state of all the backtrackable ints connected to it.
	/// </summary>
	/// <remarks>
	///   The only way to modify something backtrackable is by assigning it.
	///   For instance: if we use a backtrackable struct (say a Backtrackable 
	///   pair of ints, call it P) it cannot be modified field by field 
	///   (p.Value.First = 3), it has to be completely assigned 
	///   (p.Value = new PairIntInt(3, p.Value.Second).
	/// </remarks>
	internal sealed class Backtrackable<Content>
	{
		internal Content _currentValue;

		internal int _depthOfLastSave;

		internal readonly Trail<Content> _trail;

		/// <summary>
		///   Access to / modification of the Content stored.
		///   The modification will be backtrackable.
		/// </summary>
		/// <remarks>
		///   One design choice for "set" would be to test whether the assigned
		///   value is equal to the current one and, if so, to avoid saving. 
		///   We don't because we don't want to constrain the Content type.
		///   The user is, however, encouraged to test equality before assigning,
		///   as this can prevent saving a non-modified backtrackable object.
		/// </remarks>
		public Content Value
		{
			get
			{
				return _currentValue;
			}
			set
			{
				int depth = _trail.Depth;
				if (_depthOfLastSave != depth)
				{
					_trail.RecordChange(this);
					_depthOfLastSave = depth;
				}
				_currentValue = value;
			}
		}

		/// <summary>
		///   Construct a Backtrackable item, connected to a trail of the
		///   corresponding type. Initial value needs be provided.
		/// </summary>
		public Backtrackable(Trail<Content> trail, Content init)
		{
			_currentValue = init;
			_trail = trail;
			_depthOfLastSave = -1;
			trail.Register(this);
		}
	}
}
