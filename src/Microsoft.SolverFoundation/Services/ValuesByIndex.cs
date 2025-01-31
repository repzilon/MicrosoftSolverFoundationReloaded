using System;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Allow access to solver values using INonlinearModel indexes.
	/// </summary>
	/// <remarks>
	/// Use the indexer to get and set for the specified row or variable index. For example,
	/// <code>
	/// ValuesByIndex v;
	/// int x;
	/// // (code to retrieve v and x)
	/// Console.WriteLine("The value for vid {0} is {1}", x, v[x]);
	/// </code>
	/// </remarks>
	public class ValuesByIndex
	{
		private double[] _solverValues;

		private int[] _indexToSolver;

		/// <summary>Array of the solver values (to be used by derived classes).
		/// </summary>
		protected double[] SolverValues
		{
			get
			{
				return _solverValues;
			}
			set
			{
				_solverValues = value;
			}
		}

		/// <summary>Get or set a value given the index (vid).
		/// </summary>
		public virtual double this[int index]
		{
			get
			{
				return _solverValues[_indexToSolver[index]];
			}
			set
			{
				_solverValues[_indexToSolver[index]] = value;
			}
		}

		/// <summary>
		/// Creates ValuesByIndex instance. As no array of mapping is given, the inheritor is responsible to 
		/// override the indexer and implement it.
		/// </summary>
		protected ValuesByIndex()
		{
		}

		/// <summary>
		/// Creates a new instance using the specified mapping.
		/// </summary>
		/// <param name="indexToSolver">index to solver array mapping</param>
		public ValuesByIndex(int[] indexToSolver)
		{
			if (indexToSolver == null)
			{
				throw new ArgumentNullException("indexToSolver");
			}
			_indexToSolver = indexToSolver;
		}

		/// <summary>
		/// Creates a new instance using the specified mapping and values.
		/// </summary>
		/// <param name="indexToSolver">Index to solver array mapping.</param>
		/// <param name="solverValues">Array of the solver values.</param>
		/// <remarks>If the same ValuesByIndex is used for each iteration, 
		/// then the one-argument constructor should be used instead.</remarks>
		public ValuesByIndex(int[] indexToSolver, double[] solverValues)
		{
			if (indexToSolver == null)
			{
				throw new ArgumentNullException("indexToSolver");
			}
			if (solverValues == null)
			{
				throw new ArgumentNullException("solverValues");
			}
			_indexToSolver = indexToSolver;
			_solverValues = solverValues;
		}
	}
}
