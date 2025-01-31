using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Class for SatSolver
	/// </summary>
	public class SatSolver
	{
		internal enum VarVal
		{
			False = -1,
			Undefined,
			True
		}

		internal struct WatchList<T>
		{
			private int _cv;

			private T[] _rgv;

			public int Count
			{
				get
				{
					return _cv;
				}
				set
				{
					_cv = value;
				}
			}

			public T[] Elements => _rgv;

			public void Add(T cl)
			{
				if (_rgv == null)
				{
					_rgv = new T[8];
				}
				else if (_cv == _rgv.Length)
				{
					Array.Resize(ref _rgv, _rgv.Length * 2);
				}
				_rgv[_cv++] = cl;
			}
		}

		private const double kactVarDecay = 1.0526315789473684;

		private SatSolverParams _prm;

		private Random _rand;

		private List<Literal[]> _rgclLearned;

		private List<Literal[]> _rgclSolution;

		private int _clitLearned;

		private int _cclRaw;

		private int _varLim;

		private VarVal[] _mplitval;

		private int[] _mpvarlev;

		private Literal[][] _mpvarclReason;

		private Literal[] _mpvarlitReason;

		private WatchList<Literal>[] _mplitwlBinary;

		private WatchList<Literal[]>[] _mplitwlGeneral;

		private List<Literal> _rglitAssign;

		private List<int> _mplevilitAssign;

		private int _ilitPropogate;

		private double[] _mpvaract;

		private int[] _mplitccl;

		private double _dactVar;

		private double _actVarDecay;

		private int _cvBackTrack;

		private IndexedHeap _heap;

		private SatSolution _solPrev;

		private Stopwatch _sw;

		private bool[] _mpvarfSeen;

		private List<Literal> _rglitLearned;

		internal int LevelCur => _mplevilitAssign.Count;

		/// <summary>
		/// This one uses heuristics to decide which boolean value to try first.
		/// </summary>
		public static IEnumerable<SatSolution> Solve(SatSolverParams prm, int varLim, IEnumerable<Literal[]> rgcl)
		{
			return SolveCore(prm, varLim, rgcl);
		}

		internal static IEnumerable<SatSolution> SolveCore(SatSolverParams prm, int varLim, IEnumerable<Literal[]> rgcl)
		{
			SatSolver satSolver = new SatSolver();
			if (!satSolver.Init(prm, varLim, rgcl))
			{
				return Statics.EmptyIter<SatSolution>();
			}
			return satSolver.GetSolutions();
		}

		/// <summary>
		/// Initializes the data structures to represent the given set of clauses.
		/// </summary>
		protected bool Init(SatSolverParams prm, int varLim, IEnumerable<Literal[]> rgcl)
		{
			_rand = new Random();
			_rgclLearned = new List<Literal[]>();
			_rgclSolution = new List<Literal[]>();
			_clitLearned = 0;
			_prm = prm;
			_varLim = varLim;
			_mplitval = new VarVal[varLim * 2];
			_mpvarlev = new int[varLim];
			_mpvarclReason = new Literal[varLim][];
			_mpvarlitReason = new Literal[varLim];
			_mplitwlBinary = new WatchList<Literal>[varLim * 2];
			_mplitwlGeneral = new WatchList<Literal[]>[varLim * 2];
			_rglitAssign = new List<Literal>();
			_mplevilitAssign = new List<int>();
			_ilitPropogate = 0;
			_mpvaract = new double[varLim];
			_mplitccl = new int[varLim * 2];
			_dactVar = 1.0;
			_actVarDecay = 1.0526315789473684;
			_cvBackTrack = 0;
			_heap = new IndexedHeap(varLim, (int var1, int var2) => _mpvaract[var1] < _mpvaract[var2]);
			_mpvarfSeen = new bool[varLim];
			_rglitLearned = new List<Literal>();
			_sw = new Stopwatch();
			foreach (Literal[] item in rgcl)
			{
				if (!AddMainClause(item) || _prm.Abort)
				{
					return false;
				}
			}
			Literal[] array = PropogateUnitClauses();
			if (array != null)
			{
				return false;
			}
			InitVarActivity();
			int num = _mpvaract.Length;
			while (--num >= 0)
			{
				if (_mpvaract[num] > 0.0)
				{
					_heap.Add(num);
				}
			}
			return true;
		}

		internal void AddToWatchLists(Literal[] cl)
		{
			if (cl.Length > 2)
			{
				_mplitwlGeneral[(~cl[0]).Id].Add(cl);
				_mplitwlGeneral[(~cl[1]).Id].Add(cl);
			}
			else
			{
				_mplitwlBinary[(~cl[0]).Id].Add(cl[1]);
				_mplitwlBinary[(~cl[1]).Id].Add(cl[0]);
			}
		}

		internal bool AddMainClause(Literal[] cl)
		{
			_cclRaw++;
			if (cl.Length == 0)
			{
				return false;
			}
			if (cl.Length == 1)
			{
				return EnqueueLit(cl[0], null, Literal.Nil);
			}
			if (cl.Length > 2)
			{
				cl = (Literal[])cl.Clone();
				AddToWatchLists(cl);
				AdjustVarActivity(cl);
			}
			else
			{
				AddToWatchLists(cl);
			}
			return true;
		}

		internal void AdjustVarActivity(Literal[] cl)
		{
			for (int i = 0; i < cl.Length; i++)
			{
				Literal literal = cl[i];
				_mplitccl[literal.Id]++;
				_mpvaract[literal.Var] += _dactVar;
			}
		}

		internal void InitVarActivity()
		{
			int num = _mplitwlBinary.Length;
			while (--num >= 0)
			{
				int count = _mplitwlBinary[num ^ 1].Count;
				_mplitccl[num] += count;
				_mpvaract[num >> 1] += (double)count * _dactVar;
			}
		}

		internal void BumpActivity(int var)
		{
			_mpvaract[var] += _dactVar;
			if (_mpvaract[var] >= 1E+100)
			{
				RescaleVarActivity();
			}
			if (_heap.InHeap(var))
			{
				_heap.MoveUp(var);
			}
		}

		internal void RescaleVarActivity()
		{
			for (int i = 0; i < _varLim; i++)
			{
				_mpvaract[i] *= 1E-100;
			}
			_dactVar *= 1E-100;
		}

		internal void DecayVarActivity()
		{
			_dactVar *= _actVarDecay;
		}

		/// <summary>
		/// Backtrack to the given level
		/// </summary>
		internal void UndoToLevel(int lev)
		{
			int num = _mplevilitAssign[lev];
			while (_rglitAssign.Count > num)
			{
				UndoAssign();
			}
			Statics.TrimList(_mplevilitAssign, lev);
		}

		internal bool ResetFromPreviousSolution()
		{
			if (LevelCur == 0)
			{
				return false;
			}
			Literal[] array = new Literal[LevelCur];
			for (int i = 0; i < LevelCur; i++)
			{
				ref Literal reference = ref array[i];
				reference = ~_rglitAssign[_mplevilitAssign[i]];
			}
			UndoToLevel(0);
			if (array.Length == 1)
			{
				EnqueueLit(array[0], null, Literal.Nil);
			}
			else
			{
				AdjustVarActivity(array);
				AddToWatchLists(array);
				_rgclSolution.Add(array);
			}
			return true;
		}

		internal IEnumerable<SatSolution> GetSolutions()
		{
			while (true)
			{
				SatSolution nextSolution;
				SatSolution sol = (nextSolution = GetNextSolution());
				if (nextSolution != null)
				{
					yield return sol;
					continue;
				}
				break;
			}
		}

		internal SatSolution GetNextSolution()
		{
			if (_solPrev != null && !ResetFromPreviousSolution())
			{
				return null;
			}
			int num = _prm.BackTrackCountLimit;
			int num2 = 0;
			_sw.Stop();
			_sw.Reset();
			_sw.Start();
			while (true)
			{
				if (_prm.Abort)
				{
					_solPrev = null;
					break;
				}
				if (Search(num, num2, out _solPrev))
				{
					break;
				}
				num += Math.Max(5, num / 10);
				num2++;
			}
			_sw.Stop();
			return _solPrev;
		}

		internal Literal ChooseNext()
		{
			Literal literal;
			if (_rand.NextDouble() < _prm.RandVarProb)
			{
				literal = new Literal(_rand.Next(2 * _varLim));
				if (GetLitVal(literal) == VarVal.Undefined)
				{
					if (_prm.Biased)
					{
						return new Literal(literal.Var, _prm.InitialGuess);
					}
					return literal;
				}
			}
			do
			{
				if (_heap.Count == 0)
				{
					return Literal.Nil;
				}
				literal = new Literal(_heap.Pop(), fSense: false);
			}
			while (GetLitVal(literal) != 0);
			if (_prm.Biased)
			{
				return new Literal(literal.Var, _prm.InitialGuess);
			}
			Literal result = ~literal;
			int num = _mplitccl[literal.Id] - _mplitccl[result.Id];
			if (num == 0 || _rand.NextDouble() < _prm.RandSenseProb)
			{
				if (_rand.Next(2) != 0)
				{
					return result;
				}
				return literal;
			}
			if (num <= 0)
			{
				return result;
			}
			return literal;
		}

		internal bool Search(int cvBackTrackMax, int cvRestart, out SatSolution sol)
		{
			_cvBackTrack = 0;
			while (true)
			{
				Literal[] array = PropogateUnitClauses();
				if (array == null)
				{
					Literal lit = ChooseNext();
					if (lit.IsNil)
					{
						_sw.Stop();
						sol = new SatSolution(_rglitAssign.ToArray(), _mplevilitAssign.ToArray(), cvRestart, _cvBackTrack, _rgclLearned.Count, _clitLearned, _sw.Elapsed);
						return true;
					}
					_mplevilitAssign.Add(_rglitAssign.Count);
					EnqueueLit(lit, null, Literal.Nil);
					continue;
				}
				if (LevelCur == 0)
				{
					sol = null;
					return true;
				}
				Analyze(array, out var clOut, out var ilitMaxLev);
				_cvBackTrack++;
				if (ilitMaxLev >= 0)
				{
					UndoToLevel(_mpvarlev[clOut[ilitMaxLev].Var]);
				}
				else
				{
					UndoToLevel(0);
				}
				if (clOut.Length > 1)
				{
					Literal literal = clOut[ilitMaxLev];
					ref Literal reference = ref clOut[ilitMaxLev];
					reference = clOut[1];
					clOut[1] = literal;
					_rgclLearned.Add(clOut);
					_clitLearned += clOut.Length;
					AddToWatchLists(clOut);
					DecayVarActivity();
				}
				EnqueueLit(clOut[0], clOut, Literal.Nil);
				if (++_cvBackTrack >= cvBackTrackMax || _prm.Abort)
				{
					break;
				}
			}
			if (LevelCur > 0)
			{
				UndoToLevel(0);
			}
			sol = null;
			return false;
		}

		internal Literal[] PropogateUnitClauses()
		{
			while (_ilitPropogate < _rglitAssign.Count)
			{
				Literal literal = _rglitAssign[_ilitPropogate++];
				Literal literal2 = ~literal;
				int count = _mplitwlBinary[literal.Id].Count;
				Literal[] elements = _mplitwlBinary[literal.Id].Elements;
				int num = count;
				while (--num >= 0)
				{
					if (!EnqueueLit(elements[num], null, literal2))
					{
						return new Literal[2]
						{
							elements[num],
							literal2
						};
					}
				}
				int i = 0;
				int num2 = 0;
				int count2 = _mplitwlGeneral[literal.Id].Count;
				Literal[][] elements2 = _mplitwlGeneral[literal.Id].Elements;
				for (; i < count2; i++)
				{
					Literal[] array = elements2[i];
					if (literal2 == array[0])
					{
						ref Literal reference = ref array[0];
						reference = array[1];
						array[1] = literal2;
					}
					if (_mplitval[array[0].Id] != VarVal.True)
					{
						int j;
						for (j = 2; j < array.Length && _mplitval[array[j].Id] == VarVal.False; j++)
						{
						}
						if (j < array.Length)
						{
							ref Literal reference2 = ref array[1];
							reference2 = array[j];
							array[j] = literal2;
							_mplitwlGeneral[(~array[1]).Id].Add(array);
							continue;
						}
						if (!EnqueueLit(array[0], array, Literal.Nil))
						{
							if (num2 < i)
							{
								while (i < count2)
								{
									elements2[num2++] = elements2[i++];
								}
								_mplitwlGeneral[literal.Id].Count = num2;
							}
							return array;
						}
					}
					if (num2 < i)
					{
						elements2[num2] = elements2[i];
					}
					num2++;
				}
				_mplitwlGeneral[literal.Id].Count = num2;
			}
			return null;
		}

		internal void Analyze(Literal[] clReason, out Literal[] clOut, out int ilitMaxLev)
		{
			_rglitLearned.Clear();
			Literal[] array = null;
			int num = 0;
			Literal literal = Literal.Nil;
			int num2 = _rglitAssign.Count;
			int num3 = 0;
			ilitMaxLev = -1;
			_rglitLearned.Add(Literal.Nil);
			while (true)
			{
				for (int i = 0; i < clReason.Length; i++)
				{
					Literal item = clReason[i];
					if (item.Id == literal.Id)
					{
						continue;
					}
					int var = item.Var;
					int num4;
					if (_mpvarfSeen[var] || (num4 = _mpvarlev[var]) <= 0)
					{
						continue;
					}
					BumpActivity(var);
					_mpvarfSeen[var] = true;
					if (num4 == LevelCur)
					{
						num++;
						continue;
					}
					if (num3 < num4)
					{
						num3 = num4;
						ilitMaxLev = _rglitLearned.Count;
					}
					_rglitLearned.Add(item);
				}
				while (!_mpvarfSeen[_rglitAssign[--num2].Var])
				{
				}
				literal = _rglitAssign[num2];
				_mpvarfSeen[literal.Var] = false;
				if (--num <= 0)
				{
					break;
				}
				clReason = _mpvarclReason[literal.Var];
				if (clReason == null)
				{
					if (array == null)
					{
						array = new Literal[2];
					}
					array[0] = literal;
					ref Literal reference = ref array[1];
					reference = _mpvarlitReason[literal.Var];
					clReason = array;
				}
			}
			_rglitLearned[0] = ~literal;
			clOut = _rglitLearned.ToArray();
			Literal[] array2 = clOut;
			foreach (Literal literal2 in array2)
			{
				_mpvarfSeen[literal2.Var] = false;
			}
		}

		internal void UndoAssign()
		{
			Literal lit = Statics.PopList(_rglitAssign);
			SetLitVal(lit, VarVal.Undefined);
			int var = lit.Var;
			if (!_heap.InHeap(var))
			{
				_heap.Add(var);
			}
			if (_ilitPropogate > _rglitAssign.Count)
			{
				_ilitPropogate = _rglitAssign.Count;
			}
		}

		internal VarVal GetLitVal(Literal lit)
		{
			return _mplitval[lit.Id];
		}

		internal void SetLitVal(Literal lit, VarVal val)
		{
			_mplitval[lit.Id] = val;
			_mplitval[lit.Id ^ 1] = (VarVal)(-(sbyte)val);
		}

		internal bool EnqueueLit(Literal lit, Literal[] clFrom, Literal litFrom)
		{
			switch (GetLitVal(lit))
			{
			case VarVal.True:
				return true;
			case VarVal.False:
				return false;
			default:
			{
				SetLitVal(lit, VarVal.True);
				int var = lit.Var;
				_mpvarlev[var] = LevelCur;
				_mpvarclReason[var] = clFrom;
				_mpvarlitReason[var] = litFrom;
				_rglitAssign.Add(lit);
				return true;
			}
			}
		}
	}
}
