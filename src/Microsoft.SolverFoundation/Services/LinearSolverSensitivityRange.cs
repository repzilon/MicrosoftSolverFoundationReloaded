using System.Globalization;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> sensitivity report range  
	/// </summary>
	public struct LinearSolverSensitivityRange
	{
		/// <summary> lower bound 
		/// </summary>
		public Rational Lower;

		/// <summary> current value  
		/// </summary>
		public Rational Current;

		/// <summary> upper bound
		/// </summary>
		public Rational Upper;

		/// <summary> Compare whether the values of two LinearSolverSensitivityRange are equal
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj != null)
			{
				LinearSolverSensitivityRange? linearSolverSensitivityRange;
				LinearSolverSensitivityRange? linearSolverSensitivityRange2 = (linearSolverSensitivityRange = obj as LinearSolverSensitivityRange?);
				if (linearSolverSensitivityRange2.HasValue)
				{
					if (Lower == linearSolverSensitivityRange.Value.Lower && Current == linearSolverSensitivityRange.Value.Current)
					{
						return Upper == linearSolverSensitivityRange.Value.Upper;
					}
					return false;
				}
			}
			return false;
		}

		/// <summary> Compare whether the values of two LinearSolverSensitivityRange are equal
		/// </summary>
		public static bool operator ==(LinearSolverSensitivityRange lssr1, LinearSolverSensitivityRange lssr2)
		{
			return lssr1.Equals(lssr2);
		}

		/// <summary> Compare whether the values of two LinearSolverSensitivityRange are not equal
		/// </summary>
		public static bool operator !=(LinearSolverSensitivityRange lssr1, LinearSolverSensitivityRange lssr2)
		{
			return !lssr1.Equals(lssr2);
		}

		/// <summary> Return the hashcode of this LinearSolverSensitivityRange
		/// </summary>
		public override int GetHashCode()
		{
			return Statics.CombineHash(Statics.CombineHash(Lower.GetHashCode(), Current.GetHashCode()), Upper.GetHashCode());
		}

		/// <summary> Return the string represntation as (current [lower, upper])
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0} [{1}, {2}]", new object[3]
			{
				Current.ToDouble(),
				Lower.ToDouble(),
				Upper.ToDouble()
			});
		}
	}
}
