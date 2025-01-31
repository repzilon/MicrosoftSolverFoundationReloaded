using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A single cutting plane row
	/// </summary>
	internal sealed class RowCut
	{
		private VectorDouble _cut;

		private double _lowerRowBound;

		private double _upperRowBound;

		public VectorDouble Row => _cut;

		public double LowerRowBound => _lowerRowBound;

		public double UpperRowBound => _upperRowBound;

		public RowCut(VectorDouble row, double lowerRowBound, double upperRowBound)
		{
			_cut = row;
			_lowerRowBound = lowerRowBound;
			_upperRowBound = upperRowBound;
		}

		public RowCut(VectorRational row, Rational lowerRowBound, Rational upperRowBound)
		{
			_cut = new VectorDouble(row.RcCount, row.EntryCount);
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(row);
			while (iter.IsValid)
			{
				double value = iter.Value.ToDouble();
				_cut.SetCoef(iter.Rc, value);
				iter.Advance();
			}
			_lowerRowBound = lowerRowBound.ToDouble();
			_upperRowBound = upperRowBound.ToDouble();
		}

		public override string ToString()
		{
			if (_cut == null)
			{
				return base.ToString();
			}
			StringBuilder stringBuilder = new StringBuilder(LowerRowBound.ToString(CultureInfo.InvariantCulture));
			stringBuilder.Append(" <= ");
			stringBuilder.Append(_cut.ToString());
			stringBuilder.Append(" <= ");
			stringBuilder.Append(UpperRowBound.ToString(CultureInfo.InvariantCulture));
			return stringBuilder.ToString();
		}
	}
}
