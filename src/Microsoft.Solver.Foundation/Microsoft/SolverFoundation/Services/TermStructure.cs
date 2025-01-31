using System;

namespace Microsoft.SolverFoundation.Services
{
	[Flags]
	internal enum TermStructure
	{
		/// <summary>
		/// No flags
		/// </summary>
		None = 0,
		/// <summary>
		/// Constant after all parameters and iteration terms are fixed
		/// </summary>
		Constant = 1,
		/// <summary>
		/// Linear in decisions (e.g. "x + 2")
		/// A term with this flag is valid as a goal in linear or quadratic models, and (if Boolean) as a constraint in linear or quadratic models.
		/// </summary>
		Linear = 2,
		/// <summary>
		/// Quadratic in decisions (e.g. "x * y + 2")
		/// A term with this flag is valid as a goal in quadratic models.
		/// </summary>
		Quadratic = 4,
		/// <summary>
		/// Linear inequality (e.g. "x + 2 == 5")
		/// A term with this flag is valid as the parameter of an SOS2 constraint.
		/// </summary>
		LinearInequality = 8,
		/// <summary>
		/// AND of linear inequalities and/or Boolean constants (e.g. "(x + 2 == 5) &amp; (x + y &lt;= 10)")
		/// A term with this flag is valid as a constraint in linear or quadratic models.
		/// </summary>
		LinearConstraint = 0x10,
		/// <summary>
		/// Uses only integer operations and constants, including all descendants
		/// A term with this flag is valid as a goal or constraint in CP models.
		/// </summary>
		Integer = 0x20,
		/// <summary>
		/// Foreach
		/// </summary>
		Multivalue = 0x40,
		/// <summary>
		/// Sos1
		/// </summary>
		Sos1 = 0x80,
		/// <summary>
		/// Sos2
		/// </summary>
		Sos2 = 0x100,
		/// <summary>
		/// Differentiable function
		/// A term with this flag is valid as a goal in differentiable nonlinear models, and (if Boolean) as a constraint in the same.
		/// </summary>
		Differentiable = 0x200,
		/// <summary>
		/// AND of differentiable functions and/or Boolean constants
		/// A term with this flag is valid as a constraint in differentiable nonlinear models.
		/// </summary>
		DifferentiableConstraint = 0x400
	}
}
