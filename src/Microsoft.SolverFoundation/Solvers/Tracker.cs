namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Maintains the average of the values recorded 
	/// </summary>
	internal class Tracker
	{
		private double _sum;

		private long _numberOfValues;

		public double Average
		{
			get
			{
				if (_numberOfValues == 0)
				{
					return double.MaxValue;
				}
				return _sum / (double)_numberOfValues;
			}
		}

		public void RecordValue(double v)
		{
			_sum += v;
			_numberOfValues++;
		}
	}
}
