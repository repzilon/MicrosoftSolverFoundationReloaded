using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal abstract class PivotStrategy<Number> : ISimplexPivotInformation
	{
		protected const byte kskipCost = 1;

		protected const byte kskipFlawed = 2;

		protected const byte kskipThresh = 3;

		protected SimplexTask _thd;

		private readonly LogSource _log;

		protected SimplexReducedModel _mod;

		protected int _rowLim;

		protected int _varLim;

		private int _varEnter;

		private SimplexVarValKind _vvkEnter;

		private int _sign;

		protected double _dblApproxCost;

		protected int _ivarKey;

		protected Number _numKey;

		private Number _scale;

		private int _ivarLeave;

		private int _varLeave;

		private SimplexVarValKind _vvkLeave;

		internal virtual SimplexReducedModel Model => _mod;

		protected LogSource Logger => _log;

		public int VarEnter
		{
			get
			{
				return _varEnter;
			}
			protected set
			{
				_varEnter = value;
			}
		}

		public SimplexVarValKind VvkEnter
		{
			get
			{
				return _vvkEnter;
			}
			protected set
			{
				_vvkEnter = value;
			}
		}

		public int VarLeave
		{
			get
			{
				return _varLeave;
			}
			protected set
			{
				_varLeave = value;
			}
		}

		public int IvarLeave
		{
			get
			{
				return _ivarLeave;
			}
			internal set
			{
				_ivarLeave = value;
			}
		}

		public SimplexVarValKind VvkLeave
		{
			get
			{
				return _vvkLeave;
			}
			protected set
			{
				_vvkLeave = value;
			}
		}

		public Number Scale
		{
			get
			{
				return _scale;
			}
			protected set
			{
				_scale = value;
			}
		}

		public int Sign
		{
			get
			{
				return _sign;
			}
			protected set
			{
				_sign = value;
			}
		}

		public Number Determinant => _numKey;

		bool ISimplexPivotInformation.IsDouble => typeof(Number) == typeof(double);

		Rational ISimplexPivotInformation.Scale => GetRationalFromNumber(_scale);

		Rational ISimplexPivotInformation.Determinant => GetRationalFromNumber(_numKey);

		public virtual double ApproxCost => _dblApproxCost;

		protected PivotStrategy(SimplexTask thd)
		{
			_thd = thd;
			_log = _thd.Solver.Logger;
			_mod = _thd.Model;
		}

		public virtual void Init()
		{
			_rowLim = _mod.RowLim;
			_varLim = _mod.VarLim;
		}

		public abstract bool FindNext();

		public void Commit(Number dnumLeaveCost)
		{
			if (_varLeave != _varEnter)
			{
				UpdateCosts(dnumLeaveCost);
			}
		}

		protected virtual void UpdateCosts(Number dnumLeaveCost)
		{
		}

		protected abstract Rational GetRationalFromNumber(Number num);
	}
}
