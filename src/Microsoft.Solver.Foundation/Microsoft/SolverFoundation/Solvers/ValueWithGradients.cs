using System.Diagnostics;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A value that has both increasing and decreasing gradients, 
	/// i.e. we have an (over-)estimate of how much the value
	/// can be decreased/increased, together with hints w.r.t. which
	/// variables should be modified to obtain this variation
	/// </summary>
	[DebuggerDisplay("Value {Value}, dec by {DecGradient}, inc by {IncGradient}")]
	internal struct ValueWithGradients
	{
		private int _value;

		private int _decGradient;

		private CspVariable _decVariable;

		private int _incGradient;

		private CspVariable _incVariable;

		/// <summary>
		/// Current value 
		/// </summary>
		public int Value => _value;

		/// <summary>
		/// Decreasing gradient (always negative or zero):
		/// by how much may the value decrease? 
		/// </summary>
		public int DecGradient => _decGradient;

		/// <summary>
		/// Increasing gradient (always positive or zero):
		/// bu how much can the value increase?
		/// </summary>
		public int IncGradient => _incGradient;

		/// <summary>
		/// Hint attached to the decreasing gradient:
		/// which variable should we re-assign to decrease the value?
		/// (may be null e.g. for constant terms)
		/// </summary>
		public CspVariable DecVariable => _decVariable;

		/// <summary>
		/// Hint attached to the increasing gradient:
		/// which variable should we re-assign to icrease the value?
		/// (may be null if increasing gradient is 0)
		/// </summary>
		public CspVariable IncVariable => _incVariable;

		/// <summary>
		/// A value that has both increasing and decreasing gradients
		/// </summary>
		///
		/// <param name="val">
		/// Current value
		/// </param>
		/// <param name="dec">
		/// Decreasing gradient (always negative or zero):
		/// by how much may the value decrease? 
		/// </param>
		/// <param name="decvar">
		/// Hint attached to the decreasing gradient:
		/// which variable should we re-assign to decrease the value?
		/// (may be null only iff the decreasing gradient is 0)
		/// </param>
		/// <param name="inc">
		/// Increasing gradient (always positive or zero):
		/// by how much can the value increase?
		/// </param>
		/// <param name="incvar">
		/// Hint attached to the increasing gradient:
		/// which variable should we re-assign to icrease the value?
		/// (may be null only if the increasing gradient is 0)
		/// </param>
		public ValueWithGradients(int val, int dec, CspVariable decvar, int inc, CspVariable incvar)
		{
			_value = val;
			_decGradient = dec;
			_decVariable = decvar;
			_incGradient = inc;
			_incVariable = incvar;
		}

		/// <summary>
		/// A value which cannot be increased or decreased: 
		/// both gradients are null
		/// </summary>
		public ValueWithGradients(int val)
			: this(val, 0, null, 0, null)
		{
		}

		/// <summary>
		/// Implicit conversion of constants: a constant is a 
		/// value that cannot be increased or decreased (gradients are 0)
		/// </summary>
		public static implicit operator ValueWithGradients(int val)
		{
			return new ValueWithGradients(val);
		}

		[Conditional("DEBUG")]
		private void CheckInvariants()
		{
		}

		/// <summary>
		/// Modify the gradient by informing that: 
		/// re-assigning hint variable we may reach the value newval
		/// </summary>
		/// <remarks>
		/// Some gradients are more easily computed when we initialize
		/// to null gradients and do a number of expansions. 
		/// </remarks>
		internal void Expand(CspVariable hint, int newval)
		{
			int num = newval - _value;
			if (num < _decGradient)
			{
				_decGradient = num;
				_decVariable = hint;
			}
			if (num > _incGradient)
			{
				_incGradient = num;
				_incVariable = hint;
			}
		}

		/// <summary>
		/// Modifify the gradient by informing that:
		/// re-assigning the hint variable may reach the value newval 
		/// where newval is guaranteed to be LessEqual to current value
		/// </summary>
		internal void ExpandNegative(CspVariable hint, int newval)
		{
			int num = newval - _value;
			if (num < _decGradient)
			{
				_decGradient = num;
				_decVariable = hint;
			}
		}
	}
}
