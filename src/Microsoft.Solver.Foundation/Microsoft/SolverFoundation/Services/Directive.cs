using System.ComponentModel;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A Directive represents an instruction to the SFS and/or the solver.
	/// </summary>
	public class Directive
	{
		private const int _defultTimeLimit = -1;

		private const int _defultWaitLimit = 4000;

		private int _timeLimitMilliseconds;

		private int _waitLimitMilliseconds;

		private int _goalCount;

		private Arithmetic _arithmetic;

		/// <summary>Time limit in milliseconds. If negative, no limit.
		/// </summary>
		[Category("Termination")]
		[Description("Time limit in milliseconds. If negative, no limit.")]
		public int TimeLimit
		{
			get
			{
				return _timeLimitMilliseconds;
			}
			set
			{
				_timeLimitMilliseconds = value;
			}
		}

		/// <summary>Time to wait for a result after timeout is reached, in milliseconds. If negative, no limit.
		/// </summary>
		[Description("Time to wait for a result after timeout is reached, in milliseconds. If negative, no limit.")]
		[Category("Termination")]
		public int WaitLimit
		{
			get
			{
				return _waitLimitMilliseconds;
			}
			set
			{
				_waitLimitMilliseconds = value;
			}
		}

		/// <summary>Maximum number of goals to use, if multiple goals are supported. 
		/// Disabled goals are considered in this count. For example, if you have 3 goals, which from them the second is disabled. 
		/// Setting the MaximumGoalCount to 2 will make the first goal the only goal to be considered.
		/// </summary>
		[Description("Maximum number of goals to use, if multiple goals are supported. If negative, no limit.")]
		public int MaximumGoalCount
		{
			get
			{
				return _goalCount;
			}
			set
			{
				_goalCount = value;
			}
		}

		/// <summary>Numerical accuracy to use during solve.
		/// </summary>
		public Arithmetic Arithmetic
		{
			get
			{
				return _arithmetic;
			}
			set
			{
				_arithmetic = value;
			}
		}

		/// <summary>Create a new instance with default values.
		/// </summary>
		public Directive()
		{
			_timeLimitMilliseconds = -1;
			_waitLimitMilliseconds = 4000;
			_goalCount = -1;
			_arithmetic = Arithmetic.Default;
		}
	}
}
