using System;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class Variable
	{
		protected string _dataOut = "";

		protected Domain _dom;

		protected Expression _key;

		protected int _solverId;

		protected int _vid;

		public Expression Key => _key;

		public Domain Domain
		{
			get
			{
				return _dom;
			}
			set
			{
				_dom = value;
			}
		}

		public string DataOut
		{
			get
			{
				return _dataOut;
			}
			set
			{
				_dataOut = value;
			}
		}

		/// <summary>
		/// Id related to this variables on Solver
		/// </summary>
		public int SolverId
		{
			get
			{
				return _solverId;
			}
			set
			{
				_solverId = value;
			}
		}

		public int Id => _vid;

		public Variable(Expression key, Domain dom, int vid)
		{
			_key = key;
			_dom = dom;
			_vid = vid;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <param name="dom"></param>
		/// <param name="vid"></param>
		/// <param name="dataOut">string for data out after solving</param>
		public Variable(Expression key, Domain dom, int vid, string dataOut)
			: this(key, dom, vid)
		{
			_dataOut = dataOut;
		}

		protected void ValidateRs(Expression expr)
		{
			if (expr.Rewrite != _key.Rewrite)
			{
				throw new InvalidOperationException(Resources.ExpressionFromWrongRewriteSystem);
			}
		}

		public bool GetLinearDomain(out bool fInteger, out Rational numLo, out Rational numHi)
		{
			if (_dom.IsNumeric && _dom.ValidValues == null)
			{
				numLo = _dom.MinValue;
				numHi = _dom.MaxValue;
				fInteger = _dom.IntRestricted;
				return true;
			}
			fInteger = false;
			numLo = default(Rational);
			numHi = default(Rational);
			return false;
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", new object[2] { Key, Domain });
		}
	}
}
