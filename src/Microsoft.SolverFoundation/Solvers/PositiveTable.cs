using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between N integer variables imposing that the tuple of
	///   values assigned to these variables be equal to one of the lines of 
	///   a table. The relation is in other words directly expressed  in
	///   intension, as a list of allowed tuples.
	/// </summary>
	/// <remarks>
	///   This is a general version aimed at working in any condition, even
	///   if the variables have no sparse representation and the domains are
	///   unreasonable. (essentially this is AC4)
	/// </remarks>
	internal class PositiveTable : NaryConstraint<IntegerVariable>
	{
		/// <summary>
		///   struct representing info attached to each var/value pair.
		///   The count is the number of supports for each variable/value pair. 
		///   When this reaches 0 that signals that the value should be removed
		///   from the variable The Boolean variable is the one representing 
		///   the truth value of the pair.
		/// </summary>
		private struct Info
		{
			public Backtrackable<long> _count;

			public BooleanVariable _booleanTrigger;

			public Info(Backtrackable<long> c, BooleanVariable b)
			{
				_count = c;
				_booleanTrigger = b;
			}
		}

		/// <summary>
		///   For each line and each column we directly keep the counter
		///   and the boolean variable associated to the 
		///   corresponding variable/value pair.
		/// </summary>
		private readonly Info[][] _table;

		/// <summary>
		///   for each line l of the table _validLines.Contains(l) is true
		///   iff the line is valid (false if some variable contradicts it)
		/// </summary>
		private FiniteDomain _validLines;

		public PositiveTable(Problem p, IntegerVariable[] vars, int[][] table)
			: base(p, vars)
		{
			int num = vars.Length;
			int num2 = table.Length;
			_validLines = new FiniteDomain(_problem.DomainTrail, new ConvexDomain(0L, table.Length - 1));
			_table = new Info[num2][];
			for (int i = 0; i < num2; i++)
			{
				_table[i] = new Info[num];
			}
			CompilerToProblem compiler = p.Compiler;
			AnnotatedListener<int>.Listener l = RemoveLine;
			Dictionary<long, Info> dictionary = new Dictionary<long, Info>();
			for (int j = 0; j < num; j++)
			{
				IntegerVariable x = vars[j];
				dictionary.Clear();
				for (int k = 0; k < num2; k++)
				{
					long num3 = table[k][j];
					if (!dictionary.TryGetValue(num3, out var value))
					{
						value = (dictionary[num3] = new Info(new Backtrackable<long>(_problem.IntegerTrail, 0L), compiler.GetEquality(x, num3)));
					}
					value._count.Value = value._count.Value + 1;
					_table[k][j] = value;
					value._booleanTrigger.SubscribeToFalse(AnnotatedListener<int>.Generate(k, l));
				}
			}
		}

		/// <summary>
		///   Computes the effects of the removal of a line (this decreases
		///   the number of supports of some var/value pairs and this this
		///   counter reaches 0 may provoke their removal).
		/// </summary>
		/// <param name="line">index of the line</param>
		/// <returns>false iff failure was detected</returns>
		private bool RemoveLine(int line)
		{
			return RemoveLineBis(line, -1);
		}

		/// <summary>
		///   Computes the effects of the removal of a line (this decreases
		///   the number of supports of some var/value pairs and this this
		///   counter reaches 0 may provoke their removal).
		/// </summary>
		/// <param name="line">index of the line</param>
		/// <param name="columnToSkip">
		///   index of a column that can just be ignored. 
		///   Set to -1 if no colum should be ignored
		/// </param>
		/// <returns>false iff failure was detected</returns>
		private bool RemoveLineBis(int line, int columnToSkip)
		{
			if (!_validLines.Contains(line))
			{
				return true;
			}
			_validLines.Remove(line);
			Info[] array = _table[line];
			for (int num = array.Length - 1; num >= 0; num--)
			{
				if (num != columnToSkip)
				{
					Info info = array[num];
					Backtrackable<long> count = info._count;
					BooleanVariable booleanTrigger = info._booleanTrigger;
					long num2 = count.Value - 1;
					if (num2 == 0 && !booleanTrigger.ImposeValueFalse(base.Cause))
					{
						return false;
					}
					count.Value = num2;
				}
			}
			return true;
		}
	}
}
