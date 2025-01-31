using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Abstract base class for all Integer-result constraints
	/// </summary>
	internal abstract class CspFunction : CspSolverTerm
	{
		internal const uint _flagPushed = 1u;

		internal const uint _flagWatch = 2u;

		protected CspSolverTerm[] _inputs;

		protected int[] _scales;

		internal uint _flags;

		private List<int> ScaleBuffer => base.InnerSolver._scaleBuffer;

		/// <summary>
		/// Get the value kind of this function
		/// </summary>
		public override CspDomain.ValueKind Kind
		{
			get
			{
				if (OutputScale != 1)
				{
					return CspDomain.ValueKind.Decimal;
				}
				return CspDomain.ValueKind.Integer;
			}
		}

		internal int OutputScaleId => _scales.Length - 1;

		internal override int OutputScale => _scales[OutputScaleId];

		internal abstract string Name { get; }

		/// <summary> The count of inputs on this CspFunction.
		/// </summary>
		internal sealed override int Width
		{
			get
			{
				if (_inputs != null)
				{
					return _inputs.Length;
				}
				return 0;
			}
		}

		internal override bool IsTrue
		{
			get
			{
				throw new InvalidOperationException(Resources.InvalidIsTrueCall + ToString());
			}
		}

		internal sealed override CspSolverTerm[] Args => _inputs;

		internal override CspSolverDomain BaseValueSet
		{
			get
			{
				if (_values[0] == ConstraintSystem.DFinite && _values.Count > 1)
				{
					return _values[1];
				}
				return _values[0];
			}
		}

		/// <summary> This is for protected internal use only
		/// </summary>
		internal CspFunction(ConstraintSystem solver, CspSolverDomain domain, CspSolverTerm[] inputs)
			: base(solver, domain, TermKinds.Function, ComputeDepth(inputs))
		{
			_inputs = inputs;
			if (_inputs != null)
			{
				CspSolverTerm[] inputs2 = _inputs;
				foreach (CspSolverTerm cspSolverTerm in inputs2)
				{
					cspSolverTerm.Dependents.Add(this);
				}
			}
		}

		/// <summary>
		///   the depth of a function is 1 + max of the depth of the inputs
		/// </summary>
		private static int ComputeDepth(CspSolverTerm[] inputs)
		{
			if (inputs == null || inputs.Length == 0)
			{
				return 0;
			}
			IEnumerable<int> source = inputs.Select((CspSolverTerm t) => t.Depth);
			return source.Max() + 1;
		}

		/// <summary> Abstract base class for all Integer-result constraints
		/// </summary>
		internal CspFunction(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: this(solver, ConstraintSystem.DFinite, inputs)
		{
		}

		private int ComputeScale(int id)
		{
			int num = 0;
			if (id != OutputScaleId)
			{
				return _scales[id] / _inputs[id].OutputScale;
			}
			return 1;
		}

		private static int ScaleUpValue(int value, int scale)
		{
			if (value == 1073741823 || value == -1073741823)
			{
				return value;
			}
			long num = value * scale;
			if (num >= 1073741823 || num <= -1073741823)
			{
				throw new ArgumentOutOfRangeException(Resources.DecimalValueOutOfRange);
			}
			return (int)num;
		}

		private static int ScaleDownValue(int value, int scale)
		{
			if (value >= 1073741823)
			{
				return 1073741823;
			}
			if (value <= -1073741823)
			{
				return -1073741823;
			}
			return value / scale;
		}

		/// <summary>
		/// Scale a value at the input scale to the output scale.
		/// </summary>
		/// <param name="valueAtInputScale"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		protected int ScaleToOutput(int valueAtInputScale, int id)
		{
			return ScaleUpValue(valueAtInputScale, ComputeScale(id));
		}

		/// <summary>
		/// Scale a Domain at the input scale to the output scale.
		/// </summary>
		protected CspSolverDomain ScaleToOutput(CspSolverDomain valuesAtInputScale, int id)
		{
			int num = ComputeScale(id);
			if (num == 1)
			{
				return valuesAtInputScale;
			}
			CspSolverDomain result = null;
			if (valuesAtInputScale is CspIntervalDomain cspIntervalDomain)
			{
				result = (CspSolverDomain)base.InnerSolver.CreateIntegerInterval(ScaleUpValue(cspIntervalDomain.First, num), ScaleUpValue(cspIntervalDomain.Last, num));
			}
			else if (valuesAtInputScale is CspSetDomain cspSetDomain)
			{
				int[] set = cspSetDomain.Set;
				int[] array = new int[set.Length];
				for (int i = 0; i < set.Length; i++)
				{
					array[i] = ScaleUpValue(set[i], num);
				}
				result = (CspSolverDomain)base.InnerSolver.CreateIntegerSet(array);
			}
			return result;
		}

		/// <summary>
		/// Scale an array of values at the input scale to the output scale.
		/// </summary>
		/// <param name="valuesAtInputScale"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		protected int[] ScaleToOutput(int[] valuesAtInputScale, int id)
		{
			int num = ComputeScale(id);
			if (num == 1)
			{
				return valuesAtInputScale;
			}
			int[] array = new int[valuesAtInputScale.Length];
			for (int i = 0; i < valuesAtInputScale.Length; i++)
			{
				array[i] = ScaleUpValue(valuesAtInputScale[i], num);
			}
			return array;
		}

		/// <summary>
		/// Scale a value at the output scale to the input scale.
		/// </summary>
		protected int ScaleToInput(int valueAtOutputScale, int id)
		{
			return ScaleDownValue(valueAtOutputScale, ComputeScale(id));
		}

		/// <summary>
		/// Scale a value at the output scale to the input scale.
		/// </summary>
		protected CspSolverDomain ScaleToInput(CspSolverDomain valuesAtOutputScale, int id)
		{
			CspSolverDomain result = null;
			if (valuesAtOutputScale is CspIntervalDomain cspIntervalDomain)
			{
				int num = ComputeScale(id);
				if (num == 1)
				{
					return valuesAtOutputScale;
				}
				if (cspIntervalDomain.First <= -1073741823 && cspIntervalDomain.Last >= 1073741823)
				{
					result = (CspSolverDomain)base.InnerSolver.CreateIntegerInterval(-1073741823, 1073741823);
				}
				else
				{
					ScaleBuffer.Clear();
					foreach (int item in cspIntervalDomain.Forward())
					{
						if (item % num == 0)
						{
							ScaleBuffer.Add(item / num);
						}
					}
					result = (CspSolverDomain)base.InnerSolver.CreateIntegerSet(ScaleBuffer.ToArray());
				}
			}
			else if (valuesAtOutputScale is CspSetDomain cspSetDomain)
			{
				result = (CspSolverDomain)base.InnerSolver.CreateIntegerSet(ScaleToInput(cspSetDomain.Set, id));
			}
			return result;
		}

		/// <summary>
		/// Scale an array of values at the output scale to the input scale.
		/// </summary>
		protected int[] ScaleToInput(int[] valuesAtOutputScale, int id)
		{
			int num = ComputeScale(id);
			if (num == 1)
			{
				return valuesAtOutputScale;
			}
			ScaleBuffer.Clear();
			for (int i = 0; i < valuesAtOutputScale.Length; i++)
			{
				if (valuesAtOutputScale[i] % num == 0)
				{
					ScaleBuffer.Add(valuesAtOutputScale[i] / num);
				}
			}
			return ScaleBuffer.ToArray();
		}

		protected void ExcludeNonBooleans(params CspSolverTerm[] inputs)
		{
			foreach (CspSolverTerm cspSolverTerm in inputs)
			{
				if (!cspSolverTerm.IsBoolean)
				{
					throw new InvalidOperationException(Resources.NonBooleanInputs + ToString());
				}
			}
		}

		protected void ExcludeSymbols(params CspSolverTerm[] inputs)
		{
			foreach (CspSolverTerm cspSolverTerm in inputs)
			{
				if (cspSolverTerm.Kind == CspDomain.ValueKind.Symbol)
				{
					throw new InvalidOperationException(Resources.StringDomainNotSupported + ToString());
				}
			}
		}

		protected static CspSymbolDomain AllowConsistentSymbols(CspSolverTerm[] inputs, int from, int count)
		{
			if (inputs.Length <= from)
			{
				return null;
			}
			CspSymbolDomain symbols = inputs[from].Symbols;
			int num = from + count;
			for (int i = from + 1; i < inputs.Length && i < num; i++)
			{
				if (symbols != inputs[i].Symbols)
				{
					throw new InvalidOperationException(Resources.StringDomainIncompatible + inputs[i].ToString());
				}
			}
			return symbols;
		}

		protected void InitProductScales()
		{
			if (_inputs != null)
			{
				_scales = new int[_inputs.Length + 1];
				double num = 1.0;
				for (int i = 0; i < _inputs.Length; i++)
				{
					_scales[i] = _inputs[i].OutputScale;
					num *= (double)_scales[i];
				}
				if (num < 0.0 || num > 10000.0)
				{
					throw new InvalidOperationException(Resources.InvalidDecimalPrecision);
				}
				_scales[_scales.Length - 1] = (int)Math.Round(num);
				ConstraintSystem.ValidatePrecision(_scales[_scales.Length - 1]);
			}
		}

		protected void InitPowerScales(int exponent)
		{
			if (_inputs != null)
			{
				_scales = new int[_inputs.Length + 1];
				for (int i = 0; i < _inputs.Length; i++)
				{
					_scales[i] = _inputs[i].OutputScale;
				}
				double num = Math.Pow(_scales[0], exponent);
				if (num < 0.0 || num > 10000.0)
				{
					throw new InvalidOperationException(Resources.InvalidDecimalPrecision);
				}
				_scales[_scales.Length - 1] = (int)Math.Round(num);
				ConstraintSystem.ValidatePrecision(_scales[_scales.Length - 1]);
			}
		}

		protected void InitMaximalScales()
		{
			if (_inputs == null)
			{
				return;
			}
			_scales = new int[_inputs.Length + 1];
			int num = 0;
			for (int i = 0; i < _inputs.Length; i++)
			{
				if (_inputs[i].OutputScale > num)
				{
					num = _inputs[i].OutputScale;
				}
			}
			for (int j = 0; j < _scales.Length; j++)
			{
				_scales[j] = num;
			}
		}

		protected void InitUnitScales()
		{
			if (_inputs == null)
			{
				return;
			}
			_scales = new int[_inputs.Length + 1];
			int num = 0;
			for (int i = 0; i < _inputs.Length; i++)
			{
				if (_inputs[i].OutputScale > num)
				{
					num = _inputs[i].OutputScale;
				}
				_scales[i] = _inputs[i].OutputScale;
			}
			_scales[_inputs.Length] = num;
		}

		internal abstract CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs);

		internal abstract CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs);

		/// <summary> String representation of this function
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}({1}): FiniteValue{{{2}}}", new object[3]
			{
				Name,
				base.Ordinal,
				base.FiniteValue.AppendTo("", 9)
			});
		}

		/// <summary> Enumerates all values of a range first..last-1
		///           in a random order
		/// </summary>
		/// <remarks> The order is weakly random: we start from a random point
		///           and enumerate circularly
		/// </remarks>
		/// <param name="first">first value of the range, inclusive</param>
		/// <param name="last">last value of the range, exclusive</param>
		/// <param name="prng">a Pseudo-Random Number Generator</param>
		internal static IEnumerable<int> RandomlyEnumerateValuesInRange(int first, int last, Random prng)
		{
			int initialPos = prng.Next(first, last);
			for (int i = initialPos; i < last; i++)
			{
				yield return i;
			}
			for (int j = first; j < initialPos; j++)
			{
				yield return j;
			}
		}

		/// <summary> Enumerates all inputs but in a random order
		/// </summary>
		/// <param name="prng">A Pseudo-Random Number Generator</param>
		internal IEnumerable<CspSolverTerm> RandomlyEnumerateSubterms(Random prng)
		{
			foreach (int i in RandomlyEnumerateValuesInRange(0, _inputs.Length, prng))
			{
				yield return _inputs[i];
			}
		}

		/// <summary> Enumerates the values of a Domain in a random order
		/// </summary>
		/// <param name="dom">a Domain</param>
		/// <param name="prng">A Pseudo-Random Number Generator</param>
		internal static IEnumerable<int> RandomlyEnumerateValues(CspSolverDomain dom, Random prng)
		{
			foreach (int i in RandomlyEnumerateValuesInRange(0, dom.Count, prng))
			{
				yield return dom[i];
			}
		}

		/// <summary> Picks an input at random; preferably one whose domain
		///           is non-constant
		/// </summary>
		/// <param name="prng">A Pseudo-Random Number Generator</param>
		internal CspSolverTerm PickInput(Random prng)
		{
			CspSolverTerm result = _inputs[0];
			foreach (CspSolverTerm item in RandomlyEnumerateSubterms(prng))
			{
				if (item.BaseValueSet.Count > 1)
				{
					result = item;
					break;
				}
			}
			return result;
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping,
		///           together with a suggestion of value for this subterm
		/// </summary>
		/// <remarks> This is a default overload for any function term; 
		///           It simply picks one input at random.
		///           Redefine for every class where something more clever can be done
		/// </remarks>
		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			Random randomSource = ls.RandomSource;
			CspSolverTerm cspSolverTerm = PickInput(randomSource);
			int value = cspSolverTerm.BaseValueSet.Pick(randomSource);
			return new KeyValuePair<CspSolverTerm, int>(cspSolverTerm, value);
		}
	}
}
