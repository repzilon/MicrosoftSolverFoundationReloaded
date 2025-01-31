using System;
using System.Text;
using System.Threading;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Normalized string type, used for string pooling.
	/// </summary>
	public class NormStr
	{
		/// <summary>
		/// a NormStr pool
		/// </summary>
		internal class Pool
		{
			private static Func<string, NormStr, bool> s_fnCmpStr = CmpStr;

			private static Func<StringBuilder, NormStr, bool> s_fnCmpStrBldr = CmpStrBldr;

			private ReaderWriterLock _lock;

			private HashTable<NormStr> _tbl;

			private static bool CmpStr(string str, NormStr nstr)
			{
				return str == nstr._str;
			}

			private static bool CmpStrBldr(StringBuilder sb, NormStr nstr)
			{
				if (nstr._str.Length != sb.Length)
				{
					return false;
				}
				int num = nstr._str.Length;
				while (--num >= 0)
				{
					if (nstr._str[num] != sb[num])
					{
						return false;
					}
				}
				return true;
			}

			/// <summary>
			/// Constructor
			/// </summary>
			public Pool()
			{
				_lock = new ReaderWriterLock();
				_tbl = new HashTable<NormStr>();
			}

			/// <summary>
			/// Make sure the given string has an equivalent NormStr in the pool
			/// and return it.
			/// </summary>
			public NormStr Add(string str)
			{
				return GetCore(str, fAdd: true);
			}

			/// <summary>
			/// If the string has an equivalent NormStr in the pool return it.
			/// </summary>
			public NormStr Get(string str)
			{
				return GetCore(str, fAdd: false);
			}

			/// <summary>
			/// Make sure the given string has an equivalent NormStr in the pool
			/// and return it.
			/// </summary>
			private NormStr GetCore(string str, bool fAdd)
			{
				if (string.IsNullOrEmpty(str))
				{
					return Empty;
				}
				uint hash = Statics.HashString(str);
				_lock.AcquireReaderLock(-1);
				NormStr t;
				int count;
				try
				{
					if (_tbl.Get(str, hash, s_fnCmpStr, out t))
					{
						return t;
					}
					count = _tbl.Count;
				}
				finally
				{
					_lock.ReleaseReaderLock();
				}
				if (!fAdd)
				{
					return null;
				}
				_lock.AcquireWriterLock(-1);
				try
				{
					if (count == _tbl.Count || !_tbl.Get(str, hash, s_fnCmpStr, out t))
					{
						t = new NormStr(hash, str);
						_tbl.Add(hash, t);
					}
					return t;
				}
				finally
				{
					_lock.ReleaseWriterLock();
				}
			}

			/// <summary>
			/// Make sure the given string builder has an equivalent NormStr in the pool
			/// and return it. This method is almost identical to the previous.
			/// Unfortunately, the code can't really be shared.
			/// </summary>
			public NormStr Add(StringBuilder sb)
			{
				if (sb == null || sb.Length == 0)
				{
					return Empty;
				}
				uint hash = Statics.HashStrBldr(sb);
				_lock.AcquireReaderLock(-1);
				NormStr t;
				int count;
				try
				{
					if (_tbl.Get(sb, hash, s_fnCmpStrBldr, out t))
					{
						return t;
					}
					count = _tbl.Count;
				}
				finally
				{
					_lock.ReleaseReaderLock();
				}
				_lock.AcquireWriterLock(-1);
				try
				{
					if (count == _tbl.Count || !_tbl.Get(sb, hash, s_fnCmpStrBldr, out t))
					{
						t = new NormStr(hash, sb.ToString());
						_tbl.Add(hash, t);
					}
					return t;
				}
				finally
				{
					_lock.ReleaseWriterLock();
				}
			}
		}

		private uint _hash;

		private string _str;

		private static readonly NormStr s_nstrEmpty = new NormStr(0u, "");

		/// <summary>
		/// The one and only empty NormStr.
		/// </summary>
		public static NormStr Empty => s_nstrEmpty;

		/// <summary>
		/// Indicates whether the instance represent an empty string.
		/// </summary>
		public bool IsEmpty => this == s_nstrEmpty;

		/// <summary>
		/// NormStr's can only be created by the Pool.
		/// </summary>
		private NormStr(uint hash, string str)
		{
			_hash = hash;
			_str = str;
		}

		/// <summary>Returns the string representation.
		/// </summary>
		/// <returns>A System.String.</returns>
		public override string ToString()
		{
			return _str;
		}

		/// <summary>Serves as a hash function for NormStr.
		/// </summary>
		/// <returns>A hash code for the current NormStr.</returns>
		public override int GetHashCode()
		{
			return (int)_hash;
		}

		/// <summary>Converts to a string object.
		/// </summary>
		/// <param name="name">A NormStr.</param>
		/// <returns>A System.String.</returns>
		public static implicit operator string(NormStr name)
		{
			return name?._str;
		}
	}
}
