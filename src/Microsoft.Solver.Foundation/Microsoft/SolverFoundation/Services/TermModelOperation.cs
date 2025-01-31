namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Operation types for ITermModels.
	/// </summary>
	public enum TermModelOperation
	{
		/// <summary>
		/// No operation. Unary.
		/// </summary>
		Identity,
		/// <summary>
		/// Unary negation.
		/// </summary>
		Minus,
		/// <summary>
		/// Absolute value. Unary.
		/// </summary>
		Abs,
		/// <summary>
		/// Logical inversion. 0 if input is 1, 1 if input is 0. Undefined otherwise. Unary.
		/// </summary>
		Not,
		/// <summary>
		/// Sine. Unary.
		/// </summary>
		Sin,
		/// <summary>
		/// Cosine. Unary.
		/// </summary>
		Cos,
		/// <summary>
		/// Tangent. Unary.
		/// </summary>
		Tan,
		/// <summary>
		/// Arccosine. Unary.
		/// </summary>
		ArcCos,
		/// <summary>
		/// Arcsine. Unary.
		/// </summary>
		ArcSin,
		/// <summary>
		/// Arctangent. Unary.
		/// </summary>
		ArcTan,
		/// <summary>
		/// Hyperbolic cosine. Unary.
		/// </summary>
		Cosh,
		/// <summary>
		/// Hyperbolic sine. Unary.
		/// </summary>
		Sinh,
		/// <summary>
		/// Hyperbolic tangent. Unary.
		/// </summary>
		Tanh,
		/// <summary>
		/// Exponent (base e). Unary.
		/// </summary>
		Exp,
		/// <summary>
		/// Logarithm (base e). Unary.
		/// </summary>
		Log,
		/// <summary>
		/// Logarithm (base 10). Unary.
		/// </summary>
		Log10,
		/// <summary>
		/// Square root. Unary.
		/// </summary>
		Sqrt,
		/// <summary>
		/// Addition. Binary.
		/// </summary>
		Plus,
		/// <summary>
		/// Multiplication. Binary.
		/// </summary>
		Times,
		/// <summary>
		/// Division. Binary.
		/// </summary>
		Quotient,
		/// <summary>
		/// Exponentiation. Binary.
		/// </summary>
		Power,
		/// <summary>
		/// Maximum. Binary.
		/// </summary>
		Max,
		/// <summary>
		/// Minimum. Binary.
		/// </summary>
		Min,
		/// <summary>
		/// Logical and. 1 if both inputs are 1, 0 if either input is 0, undefined otherwise. Binary.
		/// </summary>
		And,
		/// <summary>
		/// Logical or. 1 if either input is 1, 0 if both inputs are 0, undefined otherwise. Binary.
		/// </summary>
		Or,
		/// <summary>
		/// Equality. 1 if the inputs are equal, 0 otherwise. Binary.
		/// </summary>
		Equal,
		/// <summary>
		/// Inequality. 1 if the inputs are unequal, 0 otherwise. Binary.
		/// </summary>
		Unequal,
		/// <summary>
		/// Greater-than comparison. 1 if the first input is strictly larger, 0 otherwise. Binary.
		/// </summary>
		Greater,
		/// <summary>
		/// Less-than comparison. 1 if the second input is strictly larger, 0 otherwise. Binary.
		/// </summary>
		Less,
		/// <summary>
		/// Greater-than comparison. 1 if the first input is larger or equal, 0 otherwise. Binary.
		/// </summary>
		GreaterEqual,
		/// <summary>
		/// Less-than comparison. 1 if the second input is larger or equal. 0 otherwise. Binary.
		/// </summary>
		LessEqual,
		/// <summary>
		/// If. If the first input is 1, the second input. If the first input is 0, the third input. If the first input is anything else, undefined. Trinary.
		/// </summary>
		If,
		/// <summary>
		/// Ceiling. Unary.
		/// </summary>
		Ceiling,
		/// <summary>
		/// Floor. Unary.
		/// </summary>
		Floor,
		/// <summary>
		/// Function. N-ary.
		/// </summary>
		Function
	}
}
