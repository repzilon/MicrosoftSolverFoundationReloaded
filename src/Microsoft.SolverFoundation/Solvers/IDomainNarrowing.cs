using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A user variable interface
	/// </summary>
	internal interface IDomainNarrowing : INotifyPropertyChanged
	{
		/// <summary>
		/// Return the current computed array of feasible values of this user var.
		/// </summary>
		[SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		object[] FeasibleValues { get; set; }

		/// <summary>
		/// Fix the value of this user var.
		/// </summary>
		/// <param name="val"></param>
		void Fix(object val);

		/// <summary>
		/// Unfix this user var.
		/// </summary>
		void Unfix();

		/// <summary>
		/// Test if domain narrowing is finished for this user var.
		/// </summary>
		/// <returns></returns>
		bool IsFinished();

		/// <summary>
		/// Test if the domain of feasible values of this user var is a singleton.
		/// </summary>
		/// <returns></returns>
		bool IsSingleton();

		/// <summary>
		/// Is the user variable just been fixed
		/// </summary>
		/// <returns></returns>
		bool IsFixing();
	}
}
