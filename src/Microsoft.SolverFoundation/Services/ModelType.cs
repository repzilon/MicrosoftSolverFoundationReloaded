using System;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Not all the combinations are allow
	/// We need to define each one precisely
	/// </summary>
	[Flags]
	internal enum ModelType
	{
		Unknown = 0,
		Csp = 1,
		Lp = 2,
		/// <summary>
		/// Goal is quadratic
		/// </summary>
		Qp = 4,
		Socp = 8,
		Mip = 0x10,
		Nlp = 0x20,
		Minlp = 0x40,
		Miqp = 0x80,
		/// <summary>
		/// The following are somewhat orthogonal to the others.
		/// </summary>
		Stochastic = 0x100,
		Sos1 = 0x200,
		Sos2 = 0x400,
		Differentiable = 0x800,
		/// <summary>
		/// Indicates that the model has (nontrivial) constraints.
		/// </summary>
		Constrained = 0x1000,
		/// <summary>
		/// Indicates that the model has decisions with finite bounds.
		/// </summary>
		Bounded = 0x2000
	}
}
