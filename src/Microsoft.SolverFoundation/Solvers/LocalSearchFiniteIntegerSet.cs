using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Domain containing a finite number of integer values
	/// </summary>
	internal sealed class LocalSearchFiniteIntegerSet : LocalSearchDomain
	{
		private long[] _orderedValues;

		public override long Cardinality => _orderedValues.Length;

		public override bool IsDiscrete => true;

		public override double Lower => _orderedValues[0];

		public override double Upper => _orderedValues[_orderedValues.Length - 1];

		public LocalSearchFiniteIntegerSet(long[] values)
		{
			if (LocalSearchDomain.SortedUniqueIncreasing(values))
			{
				_orderedValues = values;
				return;
			}
			_orderedValues = values.Distinct().ToArray();
			Array.Sort(_orderedValues);
		}

		public override double Sample(Random prng)
		{
			return _orderedValues[prng.Next(_orderedValues.Length)];
		}

		public override double PickNeighbour(Random prng, double currentValue, double distance)
		{
			int num = 0;
			double num2;
			while (true)
			{
				num2 = Sample(prng);
				if (num2 != currentValue || num >= 10)
				{
					break;
				}
				num++;
			}
			return num2;
		}

		public override bool Contains(double val)
		{
			int num = (int)val;
			if ((double)num == val)
			{
				return Array.BinarySearch(_orderedValues, num) >= 0;
			}
			return false;
		}

		protected override IEnumerable<double> Enumerate()
		{
			return ((IEnumerable<long>)_orderedValues).Select((Func<long, double>)((long x) => x));
		}
	}
}
