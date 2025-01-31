using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Second order cone information.
	/// </summary>
	public class SecondOrderCone : ISecondOrderCone
	{
		private readonly int _index;

		private readonly SecondOrderConeType _coneType;

		private readonly object _key;

		private readonly HashSet<int> _mpSocpEntry;

		/// <summary> The primary conic variable.  For a quadratic cone it is x1 where
		/// x1 &gt;= || x2 ||, x1 &gt; 0.
		/// </summary>
		/// <remarks>
		/// -1 means there is no primary conic variable - the cone is empty.
		/// </remarks>
		public int PrimaryVid1 { get; set; }

		/// <summary> The secondary conic variable.  For a rotated quadratic cone it is x2 where
		/// x1 x2 &gt;= || x3 ||, x1, s2 &gt; 0.
		/// </summary>
		/// <remarks>
		/// -1 means either the cone is not rotated, or there are fewer than 2 conic vars.
		/// </remarks>
		public int PrimaryVid2 { get; set; }

		/// <summary> The VIDs that belong to this cone.
		/// </summary>
		public IEnumerable<int> Vids => _mpSocpEntry.AsEnumerable();

		/// <summary> The number of VIDs that belong to this cone.
		/// </summary>
		public int VidCount => _mpSocpEntry.Count;

		/// <summary> The second order cone type.
		/// </summary>
		public SecondOrderConeType ConeType => _coneType;

		/// <summary> The cone index (vid) of this cone.
		/// </summary>
		public int Index => _index;

		/// <summary> The cone key.
		/// </summary>
		public object Key => _key;

		/// <summary>Create a new instance.
		/// </summary>
		public SecondOrderCone(object key, int index, SecondOrderConeType coneType)
		{
			_key = key;
			_index = index;
			_coneType = coneType;
			_mpSocpEntry = new HashSet<int>();
			PrimaryVid1 = (PrimaryVid2 = -1);
		}

		/// <summary>Add a vid to the cone.
		/// </summary>
		/// <param name="vid">A row variable index.</param>
		/// <param name="isPrimary">Specifies if the var is a primary conic variable.</param>
		internal bool AddVid(int vid, bool isPrimary)
		{
			bool flag = _mpSocpEntry.Contains(vid);
			if (!flag)
			{
				_mpSocpEntry.Add(vid);
			}
			if (isPrimary)
			{
				SetPrimary(vid);
			}
			return !flag;
		}

		/// <summary>Check if a vid belongs to the cone.
		/// </summary>
		/// <param name="vid">A row variable index.</param>
		/// <returns>Returns true if the vid belongs to the cone.</returns>
		public bool ContainsVid(int vid)
		{
			return _mpSocpEntry.Contains(vid);
		}

		/// <summary>Remove a vid from the cone.
		/// </summary>
		/// <param name="vid">A row variable index.</param>
		internal void RemoveVid(int vid)
		{
			TryRemovePrimary(vid);
			_mpSocpEntry.Remove(vid);
		}

		internal void SetPrimary(int vid)
		{
			if (PrimaryVid1 < 0 || PrimaryVid1 == vid)
			{
				PrimaryVid1 = vid;
			}
			else if (PrimaryVid2 < 0 || PrimaryVid2 == vid)
			{
				PrimaryVid2 = vid;
			}
			else
			{
				PrimaryVid2 = vid;
			}
		}

		private bool TryRemovePrimary(int vid)
		{
			if (vid == PrimaryVid1)
			{
				PrimaryVid1 = PrimaryVid2;
				PrimaryVid2 = -1;
				return true;
			}
			if (vid == PrimaryVid2)
			{
				PrimaryVid2 = -1;
				return true;
			}
			return false;
		}

		/// <summary> The string representation of the cone.
		/// </summary>
		public override string ToString()
		{
			return string.Concat("[ ", _coneType, ", size = ", _mpSocpEntry.Count, " ]");
		}
	}
}
