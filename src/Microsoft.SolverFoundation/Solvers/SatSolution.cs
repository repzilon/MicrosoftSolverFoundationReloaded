using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Solutions for SatSolver models
	/// </summary>
	public class SatSolution
	{
		private Literal[] _rglit;

		private int[] _rgilitGuess;

		private int _cvRestart;

		private int _cvBackTrack;

		private int _cclLearned;

		private int _clitLearned;

		private TimeSpan _ts;

		/// <summary>
		/// Get the number of restarts during search
		/// </summary>
		public int RestartCount => _cvRestart;

		/// <summary>
		/// Get the number of backtracks during search
		/// </summary>
		public int BackTrackCount => _cvBackTrack;

		/// <summary>
		/// Get the number of learned clauses during search
		/// </summary>
		public int LearnedClauseCount => _cclLearned;

		/// <summary>
		/// Get the number of learned literals during search
		/// </summary>
		public int LearnedLiteralCount => _clitLearned;

		/// <summary>
		/// Get the amount of time spent in search
		/// </summary>
		public TimeSpan Time => _ts;

		/// <summary>
		/// Get all literals
		/// </summary>
		public IEnumerable<Literal> Literals
		{
			get
			{
				try
				{
					Literal[] rglit = _rglit;
					for (int i = 0; i < rglit.Length; i++)
					{
						yield return rglit[i];
					}
				}
				finally
				{
				}
			}
		}

		/// <summary>
		/// Get all positive literals
		/// </summary>
		public IEnumerable<int> Pos
		{
			get
			{
				try
				{
					Literal[] rglit = _rglit;
					foreach (Literal lit in rglit)
					{
						Literal literal = lit;
						if (literal.Sense)
						{
							Literal literal2 = lit;
							yield return literal2.Var;
						}
					}
				}
				finally
				{
				}
			}
		}

		/// <summary>
		/// Get all negative literals
		/// </summary>
		public IEnumerable<int> Neg
		{
			get
			{
				try
				{
					Literal[] rglit = _rglit;
					foreach (Literal lit in rglit)
					{
						Literal literal = lit;
						if (!literal.Sense)
						{
							Literal literal2 = lit;
							yield return literal2.Var;
						}
					}
				}
				finally
				{
				}
			}
		}

		/// <summary>
		/// Get all choice literals
		/// </summary>
		public IEnumerable<Literal> Guesses
		{
			get
			{
				try
				{
					int[] rgilitGuess = _rgilitGuess;
					foreach (int ilit in rgilitGuess)
					{
						yield return _rglit[ilit];
					}
				}
				finally
				{
				}
			}
		}

		/// <summary>
		/// Get all choice literals that are positive
		/// </summary>
		public IEnumerable<int> PosGuess
		{
			get
			{
				try
				{
					int[] rgilitGuess = _rgilitGuess;
					foreach (int ilit in rgilitGuess)
					{
						Literal lit = _rglit[ilit];
						if (lit.Sense)
						{
							yield return lit.Var;
						}
					}
				}
				finally
				{
				}
			}
		}

		/// <summary>
		/// Get all choice literals that are negative
		/// </summary>
		public IEnumerable<int> NegGuess
		{
			get
			{
				try
				{
					int[] rgilitGuess = _rgilitGuess;
					foreach (int ilit in rgilitGuess)
					{
						Literal lit = _rglit[ilit];
						if (!lit.Sense)
						{
							yield return lit.Var;
						}
					}
				}
				finally
				{
				}
			}
		}

		/// <summary>
		/// Create a solution object
		/// </summary>
		public SatSolution(Literal[] rglit, int[] rgilitGuess, int cvRestart, int cvBackTrack, int cclLearned, int learnedLiteralCount, TimeSpan ts)
		{
			_rglit = rglit;
			_rgilitGuess = rgilitGuess;
			_cvRestart = cvRestart;
			_cvBackTrack = cvBackTrack;
			_cclLearned = cclLearned;
			_clitLearned = learnedLiteralCount;
			_ts = ts;
		}

		/// <summary>
		/// Get the string representation of the solution
		/// </summary>
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = true;
			stringBuilder.Append('{');
			Literal[] rglit = _rglit;
			for (int i = 0; i < rglit.Length; i++)
			{
				Literal literal = rglit[i];
				if (literal.Sense)
				{
					if (!flag)
					{
						stringBuilder.Append(',');
					}
					stringBuilder.Append(literal);
					flag = false;
				}
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}
	}
}
