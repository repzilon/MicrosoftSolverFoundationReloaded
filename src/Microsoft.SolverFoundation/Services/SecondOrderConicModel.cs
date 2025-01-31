using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Encapsulates a second order conic optimization problem.
	/// </summary>
	/// <remarks>
	/// Second order conic (SOCP) models are distinguished from linear models by
	/// the use of conic constraints.  Cones come in two types: quadratic and rotated
	/// quadratic.
	/// </remarks>
	public class SecondOrderConicModel : LinearModel, ISecondOrderConicModel, ILinearModel
	{
		/// <summary> Map from cone index to cone info.
		/// </summary>
		private readonly Dictionary<int, SecondOrderCone> _mpcidcone;

		/// <summary>Return the number of cones. 
		/// </summary>
		public int ConeCount => _mpcidcone.Count;

		/// <summary>Return the cone collection of this model. 
		/// </summary>
		public IEnumerable<ISecondOrderCone> Cones
		{
			get
			{
				foreach (KeyValuePair<int, SecondOrderCone> pair in _mpcidcone)
				{
					SecondOrderConicModel secondOrderConicModel = this;
					KeyValuePair<int, SecondOrderCone> keyValuePair = pair;
					if (!secondOrderConicModel.IsRowRemoved(keyValuePair.Key))
					{
						KeyValuePair<int, SecondOrderCone> keyValuePair2 = pair;
						yield return keyValuePair2.Value;
					}
				}
			}
		}

		/// <summary> Indicates whether the model contains any second order cones.
		/// </summary>
		public virtual bool IsSocpModel => _mpcidcone.Count > 0;

		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="comparer">The IEqualityComparer used to map from key to row/variable (optional).</param>
		public SecondOrderConicModel(IEqualityComparer<object> comparer)
			: base(comparer)
		{
			_mpcidcone = new Dictionary<int, SecondOrderCone>();
		}

		/// <summary> Add a reference row for a second order cone. Each cone has one reference row.
		/// </summary>
		/// <param name="key">A second order cone key</param>
		/// <param name="coneType">Second order cone type</param>
		/// <param name="vidRow">the vid of the reference row</param>
		/// <returns></returns>
		public bool AddRow(object key, SecondOrderConeType coneType, out int vidRow)
		{
			if (AddRow(key, out vidRow))
			{
				_mpcidcone[vidRow] = new SecondOrderCone(key, vidRow, coneType);
				return true;
			}
			vidRow = -1;
			return false;
		}

		/// <summary> Specifies a primary variable for a cone.  
		/// </summary>
		/// <param name="vidRow">The reference row for the cone.</param>
		/// <param name="vid">The vid of the variable.</param>
		/// <returns></returns>
		/// <remarks>
		/// Quadratic cones have one primary variable.  SetPrimaryConic must be called twice for rotated quadratic cones
		/// because they have two primary variables.
		/// </remarks>
		public virtual bool SetPrimaryConic(int vidRow, int vid)
		{
			ValidateConeVid(vidRow);
			_mpcidcone[vidRow].SetPrimary(vid);
			return true;
		}

		/// <summary>Gets cone information given a reference row vid.
		/// </summary>
		public virtual bool TryGetConeFromIndex(int vidRow, out ISecondOrderCone cone)
		{
			SecondOrderCone value;
			bool result = _mpcidcone.TryGetValue(vidRow, out value);
			cone = value;
			return result;
		}

		/// <summary>Adds a new conic row.
		/// </summary>
		public virtual bool AddRow(object key, int vidCone, SecondOrderConeRowType rowType, out int vidRow)
		{
			ValidateConeVid(vidCone);
			ValidateSecondOrderConeRowType(rowType);
			if (AddRow(key, out vidRow))
			{
				_mpcidcone[vidCone].AddVid(vidRow, rowType == SecondOrderConeRowType.PrimaryConic);
				SetFlag(vidRow, VidFlags.Conic);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Set the coefficient of the A matrix in the linear model. If num is zero, the entry is removed. 
		/// </summary>
		/// <param name="vidRow">a row id </param>
		/// <param name="vidVar">a column/variable id</param>
		/// <param name="num">a value</param>
		public override void SetCoefficient(int vidRow, int vidVar, Rational num)
		{
			if (_mpcidcone.ContainsKey(vidRow))
			{
				throw new NotSupportedException(Resources.OperationNotSupportedOnConicRows);
			}
			base.SetCoefficient(vidRow, vidVar, num);
		}

		/// <summary>Indicates whether a row is a conic row.
		/// </summary>
		public virtual bool IsConicRow(int vidRow)
		{
			ValidateVid(vidRow);
			return HasFlag(vidRow, VidFlags.Conic);
		}

		/// <summary> Return the row count for the specified cone.
		/// </summary>
		public int GetConicRowCount(int vidRow)
		{
			ValidateConeVid(vidRow);
			return _mpcidcone[vidRow].VidCount;
		}

		/// <summary> Return the rows for the specified cone.
		/// </summary>
		public IEnumerable<int> GetConicRowIndexes(int vidRow)
		{
			ValidateConeVid(vidRow);
			return _mpcidcone[vidRow].Vids;
		}

		/// <summary> Inject the given SecondOrderConicModel into this model, removing all previous information.
		/// </summary>
		public virtual void LoadSecondOrderConicModel(ISecondOrderConicModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException("model", Resources.ModelCouldNotBeNull);
			}
			if (model == this)
			{
				throw new InvalidOperationException(Resources.LoadLinearModelPassedThis);
			}
			LoadLinearModel(model);
			_mpcidcone.Clear();
			foreach (ISecondOrderCone cone in model.Cones)
			{
				SecondOrderCone secondOrderCone = new SecondOrderCone(cone.Key, cone.Index, cone.ConeType);
				foreach (int vid in ((SecondOrderCone)cone).Vids)
				{
					secondOrderCone.AddVid(vid, isPrimary: false);
					SetFlag(vid, VidFlags.Conic);
				}
				secondOrderCone.SetPrimary(secondOrderCone.PrimaryVid2);
				secondOrderCone.SetPrimary(secondOrderCone.PrimaryVid1);
				_mpcidcone[cone.Index] = secondOrderCone;
			}
		}

		/// <summary> Validates a SecondOrderConeRowType.
		/// </summary>
		/// <param name="rowType">SecondOrderConeRowType.</param>
		internal virtual void ValidateSecondOrderConeRowType(SecondOrderConeRowType rowType)
		{
			if (rowType != SecondOrderConeRowType.PrimaryConic && rowType != 0)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownConeId0, new object[1] { rowType }));
			}
		}

		/// <summary> Validates a cone VID.
		/// </summary>
		/// <param name="vidCone">A VID.</param>
		internal virtual void ValidateConeVid(int vidCone)
		{
			if (!_mpcidcone.ContainsKey(vidCone))
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownConeId0, new object[1] { vidCone }));
			}
		}
	}
}
