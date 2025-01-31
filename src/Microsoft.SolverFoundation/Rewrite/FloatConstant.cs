using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class FloatConstant : Constant<double>
	{
		public override Expression Head => base.Rewrite.Builtin.Float;

		public override bool IsNumericValue => true;

		public FloatConstant(RewriteSystem rs, double dbl)
			: base(rs, dbl)
		{
		}

		public override bool GetValue(out double val)
		{
			val = base.Value;
			return true;
		}

		public override bool GetNumericValue(out Rational val)
		{
			val = base.Value;
			return true;
		}

		public override string ToString()
		{
			double value = base.Value;
			if (!NumberUtils.IsFinite(value))
			{
				return value.ToString(CultureInfo.InvariantCulture);
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(value);
			int num = stringBuilder.Length;
			while (--num >= 0)
			{
				if (stringBuilder[num] == '.' || stringBuilder[num] == 'E')
				{
					return stringBuilder.ToString();
				}
			}
			stringBuilder.Append(".0");
			return stringBuilder.ToString();
		}
	}
}
