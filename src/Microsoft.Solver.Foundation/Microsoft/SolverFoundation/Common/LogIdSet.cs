using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Represents a set of logging ids.
	/// Currently this only supports id values from 0 to 63, but will likely be extended
	/// to support arbitrary non-negative ids.
	/// </summary>
	public struct LogIdSet
	{
		private ulong _bits;

		/// <summary>
		/// no logging 
		/// </summary>
		public static LogIdSet None => default(LogIdSet);

		/// <summary>
		/// log all 
		/// </summary>
		public static LogIdSet All => new LogIdSet(ulong.MaxValue);

		/// <summary>
		/// Gets whether the LogIdSet is empty.
		/// </summary>
		public bool IsEmpty => _bits == 0;

		private LogIdSet(ulong bits)
		{
			_bits = bits;
		}

		private static bool IsIdValid(int id)
		{
			return (id & -64) == 0;
		}

		private static ulong Bit(int id)
		{
			return (ulong)(1L << id);
		}

		/// <summary>
		/// Set log id
		/// </summary>
		/// <param name="id"></param>
		public LogIdSet(int id)
		{
			if (!IsIdValid(id))
			{
				throw new ArgumentOutOfRangeException(Resources.InvalidLogEventId);
			}
			_bits = Bit(id);
		}

		/// <summary>
		/// Set logging range 
		/// </summary>
		/// <param name="id1"></param>
		/// <param name="id2"></param>
		public LogIdSet(int id1, int id2)
		{
			if (!IsIdValid(id1) || !IsIdValid(id2))
			{
				throw new ArgumentOutOfRangeException(Resources.InvalidLogEventId);
			}
			_bits = Bit(id1) | Bit(id2);
		}

		/// <summary>
		/// Set logging range
		/// </summary>
		/// <param name="rgid"></param>
		public LogIdSet(params int[] rgid)
		{
			_bits = 0uL;
			if (rgid == null)
			{
				return;
			}
			int num = rgid.Length;
			while (--num >= 0)
			{
				if (!IsIdValid(rgid[num]))
				{
					throw new ArgumentOutOfRangeException(Resources.InvalidLogEventId);
				}
				_bits |= Bit(rgid[num]);
			}
		}

		/// <summary>
		/// Computes the set-wise complement of the LogIdSet.
		/// </summary>
		/// <param name="ids">a set of log ids</param>
		/// <returns>a log id set </returns>
		public static LogIdSet operator ~(LogIdSet ids)
		{
			return new LogIdSet(~ids._bits);
		}

		/// <summary>
		/// Computes the set-wise union of the LogIdSets.
		/// </summary>
		public static LogIdSet operator |(LogIdSet ids1, LogIdSet ids2)
		{
			return new LogIdSet(ids1._bits | ids2._bits);
		}

		/// <summary>
		/// Computes the set-wise symmetric difference of the LogIdSets.
		/// </summary>
		public static LogIdSet operator ^(LogIdSet ids1, LogIdSet ids2)
		{
			return new LogIdSet(ids1._bits ^ ids2._bits);
		}

		/// <summary>
		/// Computes the set-wise difference of the LogIdSets.
		/// </summary>
		public static LogIdSet operator /(LogIdSet ids1, LogIdSet ids2)
		{
			return new LogIdSet(ids1._bits & ~ids2._bits);
		}

		/// <summary>
		/// Computes the set-wise intersection of the LogIdSets.
		/// </summary>
		public static LogIdSet operator &(LogIdSet ids1, LogIdSet ids2)
		{
			return new LogIdSet(ids1._bits & ids2._bits);
		}

		/// <summary>
		/// union two log id sets
		/// </summary>
		/// <param name="ids"></param>
		/// <returns></returns>
		public LogIdSet Union(LogIdSet ids)
		{
			return new LogIdSet(_bits | ids._bits);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="ids"></param>
		/// <returns></returns>
		internal LogIdSet SymmetricDifference(LogIdSet ids)
		{
			return new LogIdSet(_bits ^ ids._bits);
		}

		internal LogIdSet Difference(LogIdSet ids)
		{
			return new LogIdSet(_bits & ~ids._bits);
		}

		internal LogIdSet Intersection(LogIdSet ids)
		{
			return new LogIdSet(_bits & ids._bits);
		}

		/// <summary>
		/// Check whether id is in the set 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool Contains(int id)
		{
			return (_bits & Bit(id)) != 0;
		}

		/// <summary>
		/// Check if a subset 
		/// </summary>
		/// <param name="set">a subset</param>
		/// <returns>true if it is a subset. otherwise false</returns>
		public bool ContainsAny(LogIdSet set)
		{
			return (_bits & set._bits) != 0;
		}

		/// <summary>
		/// Add the log id to the set 
		/// </summary>
		/// <param name="id">a log id</param>
		/// <returns>a new log set</returns>
		public LogIdSet Add(int id)
		{
			if (!IsIdValid(id))
			{
				throw new ArgumentOutOfRangeException(Resources.InvalidLogEventId);
			}
			return new LogIdSet(_bits | Bit(id));
		}

		/// <summary>
		/// Remove the log id from the set
		/// </summary>
		/// <param name="id">a log id</param>
		/// <returns>a new log set</returns>
		public LogIdSet Remove(int id)
		{
			if (!IsIdValid(id))
			{
				throw new ArgumentOutOfRangeException(Resources.InvalidLogEventId);
			}
			return new LogIdSet(_bits & ~Bit(id));
		}

		/// <summary>
		/// Add a log id to the set
		/// </summary>
		/// <param name="ids">a log set id</param>
		/// <param name="id">a log id</param>
		/// <returns></returns>
		public static LogIdSet operator +(LogIdSet ids, int id)
		{
			return ids.Add(id);
		}

		/// <summary>
		/// Remove a log id from the set 
		/// </summary>
		/// <param name="ids">a log set</param>
		/// <param name="id">a log id</param>
		/// <returns></returns>
		public static LogIdSet operator -(LogIdSet ids, int id)
		{
			return ids.Remove(id);
		}

		/// <summary> Compare whether two LogIdSets are equal
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj == null || !(obj is LogIdSet))
			{
				return false;
			}
			return _bits == ((LogIdSet)obj)._bits;
		}

		/// <summary> Compare whether two LogIdSets are value equal
		/// </summary>
		public static bool operator ==(LogIdSet ids1, LogIdSet ids2)
		{
			return ids1.Equals(ids2);
		}

		/// <summary> Compare whether two LogIdSets are not value equal
		/// </summary>
		public static bool operator !=(LogIdSet ids1, LogIdSet ids2)
		{
			return !ids1.Equals(ids2);
		}

		/// <summary> Return the hashcode of this LogIdSet
		/// </summary>
		public override int GetHashCode()
		{
			return _bits.GetHashCode();
		}
	}
}
