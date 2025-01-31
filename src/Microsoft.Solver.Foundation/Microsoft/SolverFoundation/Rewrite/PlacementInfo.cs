using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// Gives information about the original placement of the expression 
	/// in the file
	/// </summary>
	internal sealed class PlacementInfo
	{
		private readonly LineMapper _map;

		private readonly TextSpan _span;

		public TextSpan Span => _span;

		public LineMapper Map => _map;

		private PlacementInfo()
		{
		}

		public PlacementInfo(LineMapper map, TextSpan span)
		{
			_map = map;
			_span = span;
		}
	}
}
