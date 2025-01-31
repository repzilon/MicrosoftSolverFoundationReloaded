namespace Microsoft.SolverFoundation.Solvers
{
	internal struct SortedUniqueIntSet
	{
		internal int[] Set;

		internal void Union(int[] more)
		{
			if (Set == null)
			{
				Set = more;
			}
			else
			{
				if (more == null)
				{
					return;
				}
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				while (num3 < more.Length && num < Set.Length)
				{
					if (Set[num] < more[num3])
					{
						num++;
					}
					else if (Set[num] != more[num3++])
					{
						num2++;
					}
					else
					{
						num++;
					}
				}
				num2 += more.Length - num3;
				if (0 >= num2)
				{
					return;
				}
				int[] array = new int[Set.Length + num2];
				num = 0;
				num3 = 0;
				int num4 = 0;
				while (num3 < more.Length && num < Set.Length)
				{
					if (Set[num] <= more[num3])
					{
						if (Set[num] == more[num3])
						{
							num3++;
						}
						array[num4++] = Set[num++];
					}
					else
					{
						array[num4++] = more[num3++];
					}
				}
				while (num < Set.Length)
				{
					array[num4++] = Set[num++];
				}
				while (num3 < more.Length)
				{
					array[num4++] = more[num3++];
				}
				Set = array;
			}
		}

		internal void Initialize(int first, int last)
		{
			if (last >= first)
			{
				Set = new int[last - first + 1];
				int num = first;
				int num2 = 0;
				while (num <= last)
				{
					Set[num2++] = num++;
				}
			}
		}

		internal void Union(int first, int last)
		{
			if (Set == null)
			{
				Initialize(first, last);
			}
			else
			{
				if (first > last)
				{
					return;
				}
				int i = 0;
				int num = 0;
				int j = first;
				for (; i < Set.Length && Set[i] < first; i++)
				{
				}
				for (; j <= last; j++)
				{
					if (i >= Set.Length)
					{
						break;
					}
					if (Set[i] != j)
					{
						num++;
					}
					if (Set[i] <= j)
					{
						i++;
					}
				}
				num += last + 1 - j;
				if (0 < num)
				{
					int[] array = new int[Set.Length + num];
					i = 0;
					j = first;
					int num2 = 0;
					while (i < Set.Length && Set[i] < first)
					{
						array[num2++] = Set[i++];
					}
					while (j <= last)
					{
						array[num2++] = j++;
					}
					for (; i < Set.Length && Set[i] <= last; i++)
					{
					}
					while (i < Set.Length)
					{
						array[num2++] = Set[i++];
					}
					Set = array;
				}
			}
		}

		internal void Union(CspSolverDomain dom)
		{
			if (dom is CspSetDomain cspSetDomain)
			{
				Union(cspSetDomain.Set);
			}
			else
			{
				Union(dom.First, dom.Last);
			}
		}
	}
}
