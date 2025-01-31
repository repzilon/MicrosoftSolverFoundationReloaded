using System;
using System.Diagnostics;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// Params which should be used temporarily from OML
	/// </summary>
	internal class OmlTimedSimplexParams : SimplexSolverParams
	{
		private int _csecMax;

		private Stopwatch _sw;

		private Action<string> _messageLog;

		public int MaxSeconds
		{
			get
			{
				return _csecMax;
			}
			set
			{
				_csecMax = value;
			}
		}

		public Action<string> MessageLog
		{
			get
			{
				return _messageLog;
			}
			set
			{
				_messageLog = value;
			}
		}

		public OmlTimedSimplexParams()
			: this((Func<bool>)null)
		{
		}

		public OmlTimedSimplexParams(Func<bool> fnQueryAbort)
			: base(fnQueryAbort)
		{
			_csecMax = int.MaxValue;
			_sw = new Stopwatch();
		}

		public OmlTimedSimplexParams(OmlTimedSimplexParams prm)
			: base(prm)
		{
			_csecMax = prm._csecMax;
			_sw = new Stopwatch();
		}

		public override bool ShouldAbort(ILinearSimplexStatistics stat)
		{
			if (_sw.Elapsed.TotalSeconds > (double)_csecMax)
			{
				if (MessageLog != null)
				{
					MessageLog(Resources.MaximumTimeExceeded);
				}
				return true;
			}
			if (stat != null && stat.PivotCount > MaxPivotCount)
			{
				if (MessageLog != null)
				{
					MessageLog(Resources.MaximumIterationsExceeded);
				}
				return true;
			}
			return base.ShouldAbort(stat);
		}

		/// <summary>
		/// Reset any state that changed. The TimedParams is about to be re-used.
		/// </summary>
		public void Reset()
		{
			_sw.Stop();
			_sw.Reset();
		}

		public override bool NotifyStartSolve(int threadIndex)
		{
			if (!_sw.IsRunning)
			{
				_sw.Start();
			}
			return base.NotifyStartSolve(threadIndex);
		}

		public override bool NotifyStartFactorization(int threadIndex, ILinearSimplexStatistics stat, bool fDouble)
		{
			if (!_sw.IsRunning)
			{
				_sw.Start();
			}
			return base.NotifyStartFactorization(threadIndex, stat, fDouble);
		}
	}
}
