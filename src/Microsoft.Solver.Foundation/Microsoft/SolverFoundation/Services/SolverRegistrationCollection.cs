using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Plug-in solver registration information.
	/// </summary>
	/// <remarks>
	/// SolverRegistrationCollection stores information about registered third-party solvers.
	/// This information is accessed using the SolverContext.RegisteredSolvers property. A registration
	/// entry contains information such as the name of the plug-in assembly and the solver capability 
	/// and interface it supports. This information is stored in the SolverRegistration class.
	/// A single plug-in solver may have multiple SolverRegistration entries, one for each capability
	/// supported by the solver. Entries are added to the collection automatically when a configuration
	/// file is associated with the application, or by calling the Add method.
	/// </remarks>
	public class SolverRegistrationCollection : ICollection<SolverRegistration>, IEnumerable<SolverRegistration>, IEnumerable
	{
		private readonly PluginSolverCollection _solvers;

		/// <summary>The number of registration entries.
		/// </summary>
		public int Count => _solvers.Count;

		/// <summary>Returns a value that indicates whether the collection is read only.
		/// </summary>
		public bool IsReadOnly => false;

		/// <summary>Creates a new instance.
		/// </summary>
		/// <param name="solvers">A PluginSolverCollection.</param>
		private SolverRegistrationCollection(PluginSolverCollection solvers)
		{
			DebugContracts.NonNull(solvers);
			_solvers = solvers;
		}

		/// <summary>Creates a new instance.
		/// </summary>
		internal static SolverRegistrationCollection Create()
		{
			PluginSolverCollection solvers = PluginSolverCollection.CreatePluginSolverCollection();
			return new SolverRegistrationCollection(solvers);
		}

		/// <summary>Adds a new solver registration entry.
		/// </summary>
		/// <param name="item">The solver registration information.</param>
		public void Add(SolverRegistration item)
		{
			_solvers.AddPluginSolver(item);
		}

		/// <summary>Clears the collection (not supported).
		/// </summary>
		/// <exception cref="T:System.NotSupportedException" />
		void ICollection<SolverRegistration>.Clear()
		{
			throw new NotSupportedException();
		}

		/// <summary>Determine if a registration entry exists.
		/// </summary>
		/// <param name="item">The solver registration information.</param>
		/// <returns>Returns true if the solver has been registered.</returns>
		public bool Contains(SolverRegistration item)
		{
			return _solvers.Contains(item);
		}

		/// <summary>Copy registration entries to an Array.
		/// </summary>
		/// <param name="array">The destination array.</param>
		/// <param name="arrayIndex">The start index.</param>
		public void CopyTo(SolverRegistration[] array, int arrayIndex)
		{
			_solvers.CopyTo(array, arrayIndex);
		}

		/// <summary>Removes a registration entry (not supported).
		/// </summary>
		/// <param name="item">The solver registration information.</param>
		/// <returns>A boolean value indicating whether the item was removed.</returns>
		/// <exception cref="T:System.NotSupportedException" />
		bool ICollection<SolverRegistration>.Remove(SolverRegistration item)
		{
			throw new NotSupportedException();
		}

		/// <summary>Get an enumerator for the collection.
		/// </summary>
		/// <returns>An IEnumerator.</returns>
		public IEnumerator<SolverRegistration> GetEnumerator()
		{
			return _solvers.GetSolverRegistrations().GetEnumerator();
		}

		/// <summary>Get an enumerator for the collection.
		/// </summary>
		/// <returns>An IEnumerator.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _solvers.GetSolverRegistrations().GetEnumerator();
		}

		/// <summary>Return the Type of the directive associated with a registration entry.
		/// </summary>
		/// <returns>A Type object for the directive, or null if not found.</returns>
		public Type GetDirectiveType(SolverRegistration registration)
		{
			return _solvers.GetDirectiveType(registration);
		}

		/// <summary> Return the default registered ISolver instance with the given capability (could be null)
		/// </summary>
		internal IEnumerable<Tuple<ISolver, Type>> GetSolvers(SolverCapability cap, bool isModelStochastic, Directive dir, ISolverEnvironment context, SolverCapabilityFlags flags)
		{
			return _solvers.GetSolvers(cap, isModelStochastic, dir, context, flags);
		}

		/// <summary>
		/// Return the parameter object for the given solver.
		/// </summary>
		internal ISolverParameters GetSolverParams(SolverCapability cap, Type solverInterface, ISolver solver, Directive directive)
		{
			return _solvers.GetSolverParams(cap, solverInterface, solver, directive);
		}
	}
}
