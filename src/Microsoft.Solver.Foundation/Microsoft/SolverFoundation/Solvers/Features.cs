using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Class used to extract features of a given problem
	/// </summary>
	internal class Features
	{
		public enum FeatureNames
		{
			NrOfVars,
			NrUserDefinedVars,
			NrOfBoolVars,
			NrOfIntVars,
			NrOfBoolVarsUserDefined,
			NrOfIntVarsUserDefined,
			NrUserDefinedAllVarsRatio,
			NrUserDefinedBoolIntVarsRatio,
			NrAllBoolIntVarsRatio,
			NrOfConstraints,
			VarConstraintRatio,
			UDVarConstraintRatio,
			UDBoolVarConstraintRatio,
			UDIntVarConstraintRatio,
			AverageDomainSize,
			AverageUDDomainSize,
			NrOfInstantiatedVarsRatio,
			VarWithMaxConstraintsRatio,
			AdditionRatio,
			AdditionToConstantRatio,
			EqualityRatio,
			NegationRatio,
			OppositeRatio,
			ClauseRatio,
			IntegerBooleanEquivalenceRatio,
			PseudoBooleanSumRatio,
			ProductByConstantRatio,
			ReifiedEqualityRatio,
			ReifiedLessEqualRatio,
			ReifiedConjunctionRatio,
			ReifiedDisjunctionRatio,
			LessEqualRatio,
			LessStricRatio,
			TernaryConstraintRatio,
			BinaryConstraintRatio,
			UnaryConstraintRatio,
			ImplicationRatio,
			SetMembershipRatio,
			AllDifferentDomainsRatio,
			DomainHist0,
			DomainHist1,
			DomainHist2,
			DomainHist3,
			DomainHist4,
			DomainHist5,
			DomainHist6,
			DomainHist7,
			DomainHist8,
			DomainHist9,
			DomainHistUD0,
			DomainHistUD1,
			DomainHistUD2,
			DomainHistUD3,
			DomainHistUD4,
			DomainHistUD5,
			DomainHistUD6,
			DomainHistUD7,
			DomainHistUD8,
			DomainHistUD9,
			ConstraintHist0,
			ConstraintHist1,
			ConstraintHist2,
			ConstraintHist3,
			ConstraintHist4,
			ConstraintHist5,
			ConstraintHist6,
			ConstraintHist7,
			ConstraintHist8,
			ConstraintHist9,
			ConstraintHistUD0,
			ConstraintHistUD1,
			ConstraintHistUD2,
			ConstraintHistUD3,
			ConstraintHistUD4,
			ConstraintHistUD5,
			ConstraintHistUD6,
			ConstraintHistUD7,
			ConstraintHistUD8,
			ConstraintHistUD9,
			ConstraintHistInt0,
			ConstraintHistInt1,
			ConstraintHistInt2,
			ConstraintHistInt3,
			ConstraintHistInt4,
			ConstraintHistInt5,
			ConstraintHistInt6,
			ConstraintHistInt7,
			ConstraintHistInt8,
			ConstraintHistInt9,
			ConstraintHistBool0,
			ConstraintHistBool1,
			ConstraintHistBool2,
			ConstraintHistBool3,
			ConstraintHistBool4,
			ConstraintHistBool5,
			ConstraintHistBool6,
			ConstraintHistBool7,
			ConstraintHistBool8,
			ConstraintHistBool9
		}

		private const int nHistLength = 10;

		private int nNumberOfFeatures;

		private double[] dFeatureValues;

		protected Problem _problem;

		public double this[FeatureNames n]
		{
			get
			{
				return dFeatureValues[(int)n];
			}
			protected set
			{
				dFeatureValues[(int)n] = value;
			}
		}

		/// <summary>        
		/// Takes as parameter a problem and allocates sufficient memory for the 
		/// the number of features contained in FeatureNames
		/// </summary>
		/// <param name="p"></param>
		public Features(Problem p)
		{
			_problem = p;
			int num = Enum.GetNames(typeof(FeatureNames)).Length;
			nNumberOfFeatures = num;
			dFeatureValues = new double[nNumberOfFeatures];
		}

		/// <summary>
		/// Extract Features of the current Problem and store in feature vector
		/// </summary>
		public void ExtractFeatures()
		{
			this[FeatureNames.NrOfVars] = _problem.DiscreteVariables.Cardinality;
			this[FeatureNames.NrOfConstraints] = _problem.Constraints.Cardinality;
			this[FeatureNames.VarConstraintRatio] = this[FeatureNames.NrOfVars] / this[FeatureNames.NrOfConstraints];
			ComputeVariableRelatedFeatures();
			ComputeSimpleConstraintStatistics();
		}

		/// <summary>
		/// Access/Compute simple statistics on Constraints
		/// * Store in feature array *
		/// </summary>
		public void ComputeSimpleConstraintStatistics()
		{
			foreach (DisolverConstraint item in _problem.Constraints.Enumerate())
			{
				Type type = item.GetType();
				string text = type.ToString();
				string text2 = text.Remove(0, 9);
				string value = text2 + "Ratio";
				if (Enum.IsDefined(typeof(FeatureNames), value))
				{
					FeatureNames n = (FeatureNames)Enum.Parse(typeof(FeatureNames), value);
					this[n] = (double)_problem.ConstraintCountByType[type] / this[FeatureNames.NrOfConstraints];
				}
				else
				{
					Console.WriteLine(Resources.WeAreMissingAnEmumForTheConstraintValueInTheFeatureExtraction, text2);
				}
			}
		}

		/// <summary>
		/// Computes the connectivity of all constraints
		/// (Not yet finished)
		/// </summary>
		public void ComputeConstraintConnectivity()
		{
			int cardinality = _problem.Constraints.Cardinality;
			Set<int>[] array = new Set<int>[cardinality];
			for (int i = 0; i < cardinality; i++)
			{
				array[i] = new Set<int>();
			}
			foreach (DiscreteVariable item in _problem.DiscreteVariables.Enumerate())
			{
				List<int> list = new List<int>();
				foreach (DisolverConstraint item2 in item.EnumerateConstraints())
				{
					list.Add(item2.Index);
				}
				for (int j = 0; j < list.Count; j++)
				{
					for (int k = 0; k < list.Count; k++)
					{
						if (j != k)
						{
							array[j].Add(list[k]);
						}
					}
				}
			}
		}

		/// <summary>
		/// Helper function to add histograms to features
		/// </summary>
		/// <param name="arrHist"></param>
		/// <param name="sHist"></param>
		private void AddHistToFeatures(double[] arrHist, string sHist)
		{
			for (int i = 0; i < 10; i++)
			{
				string value = sHist + i.ToString(CultureInfo.InvariantCulture);
				if (Enum.IsDefined(typeof(FeatureNames), value))
				{
					FeatureNames n = (FeatureNames)Enum.Parse(typeof(FeatureNames), value);
					this[n] = arrHist[i];
				}
			}
		}

		/// <summary>
		/// Compute and set the number of ALL/Userdefined INT/BOOL vars in the current Problem 
		/// This also includes the computation of several ratios
		/// Remark: the computation of the basic value should be performed when constructing the problem
		/// </summary>
		public void ComputeVariableRelatedFeatures()
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			long num5 = 0L;
			long num6 = 0L;
			int num7 = 0;
			int num8 = 0;
			double[] array = new double[10];
			double[] array2 = new double[10];
			double[] array3 = new double[10];
			double[] array4 = new double[10];
			double[] array5 = new double[10];
			double[] array6 = new double[10];
			int num9 = 0;
			int num10 = 0;
			int num11 = 0;
			int num12 = 0;
			for (int i = 0; i < 10; i++)
			{
				array[i] = 0.0;
				array2[i] = 0.0;
				array3[i] = 0.0;
				array4[i] = 0.0;
				array5[i] = 0.0;
				array6[i] = 0.0;
			}
			int[] array7 = new int[(int)this[FeatureNames.NrOfVars]];
			int num13 = 0;
			foreach (DiscreteVariable item in _problem.DiscreteVariables.Enumerate())
			{
				foreach (DisolverConstraint item2 in item.EnumerateConstraints())
				{
					_ = item2;
					array7[item.Index] = array7[item.Index] + 1;
				}
				if (array7[item.Index] > num13)
				{
					num13 = array7[item.Index];
				}
			}
			int num14 = 0;
			foreach (DiscreteVariable item3 in _problem.DiscreteVariables.Enumerate())
			{
				if (item3 is IntegerVariable)
				{
					num2++;
					num5 += (int)item3.DomainSize;
					if (item3.DomainSize > num7)
					{
						num7 = (int)item3.DomainSize;
					}
					if (array7[item3.Index] > num11)
					{
						num11 = array7[item3.Index];
					}
				}
				else if (item3 is BooleanVariable)
				{
					num++;
					if (array7[item3.Index] > num12)
					{
						num12 = array7[item3.Index];
					}
				}
				int num15 = array7[item3.Index];
				if (num15 > num9)
				{
					num9 = num15;
				}
				if (item3.CheckIfInstantiated())
				{
					num14++;
				}
			}
			foreach (DiscreteVariable userDefinedVariable in _problem.UserDefinedVariables)
			{
				if (userDefinedVariable is IntegerVariable)
				{
					num4++;
					num6 += (int)userDefinedVariable.DomainSize;
					if (userDefinedVariable.DomainSize > num8)
					{
						num8 = (int)userDefinedVariable.DomainSize;
					}
				}
				else if (userDefinedVariable is BooleanVariable)
				{
					num3++;
				}
				int num15 = array7[userDefinedVariable.Index];
				if (num15 > num10)
				{
					num10 = num15;
				}
			}
			int num16 = 0;
			foreach (DiscreteVariable item4 in _problem.DiscreteVariables.Enumerate())
			{
				if (item4 is IntegerVariable)
				{
					num16 = ((num7 > 0) ? ((int)Math.Floor((double)item4.DomainSize / (double)num7 * (double)Math.Min(9, num7))) : 0);
					array[num16] += 1.0;
					num16 = ((num11 > 0) ? ((int)Math.Floor((double)array7[item4.Index] / (double)num11 * (double)Math.Min(9, num11))) : 0);
					array5[num16] += 1.0;
				}
				else
				{
					num16 = ((num12 > 0) ? ((int)Math.Floor((double)array7[item4.Index] / (double)num12 * (double)Math.Min(9, num12))) : 0);
					array6[num16] += 1.0;
				}
				num16 = ((num9 > 0) ? ((int)Math.Floor((double)array7[item4.Index] / (double)num9 * (double)Math.Min(9, num9))) : 0);
				array3[num16] += 1.0;
			}
			foreach (DiscreteVariable userDefinedVariable2 in _problem.UserDefinedVariables)
			{
				if (userDefinedVariable2 is IntegerVariable)
				{
					num16 = ((num8 > 0) ? ((int)Math.Floor((double)userDefinedVariable2.DomainSize / (double)num8 * (double)Math.Min(9, num8))) : 0);
					array2[num16] += 1.0;
				}
				if (num10 > 0)
				{
					num16 = (int)Math.Floor((double)array7[userDefinedVariable2.Index] / (double)num10 * (double)Math.Min(9, num10));
				}
				array4[num16] += 1.0;
			}
			this[FeatureNames.NrUserDefinedVars] = num4 + num3;
			this[FeatureNames.NrOfBoolVars] = num;
			this[FeatureNames.NrOfIntVars] = num2;
			this[FeatureNames.NrOfBoolVarsUserDefined] = num3;
			this[FeatureNames.NrOfIntVarsUserDefined] = num4;
			for (int j = 0; j < 10; j++)
			{
				if (num2 > 0)
				{
					array[j] /= num2;
				}
				else
				{
					array[j] = 0.0;
				}
				if (num4 > 0)
				{
					array2[j] /= num4;
				}
				else
				{
					array2[j] = 0.0;
				}
				if (this[FeatureNames.NrOfVars] > 0.0)
				{
					array3[j] /= this[FeatureNames.NrOfVars];
				}
				else
				{
					array3[j] = 0.0;
				}
				if (this[FeatureNames.NrOfBoolVars] > 0.0)
				{
					array4[j] /= this[FeatureNames.NrUserDefinedVars];
				}
				else
				{
					array4[j] = 0.0;
				}
				if (this[FeatureNames.NrOfBoolVars] > 0.0)
				{
					array6[j] /= this[FeatureNames.NrOfBoolVars];
				}
				else
				{
					array6[j] = 0.0;
				}
				if (this[FeatureNames.NrOfIntVars] > 0.0)
				{
					array5[j] /= this[FeatureNames.NrOfIntVars];
				}
				else
				{
					array5[j] = 0.0;
				}
			}
			AddHistToFeatures(array, "DomainHist");
			AddHistToFeatures(array2, "DomainHistUD");
			AddHistToFeatures(array3, "ConstraintHist");
			AddHistToFeatures(array4, "ConstraintHistUD");
			AddHistToFeatures(array5, "ConstraintHistInt");
			AddHistToFeatures(array6, "ConstraintHistBool");
			if (this[FeatureNames.NrOfVars] > 0.0)
			{
				this[FeatureNames.NrUserDefinedAllVarsRatio] = this[FeatureNames.NrUserDefinedVars] / this[FeatureNames.NrOfVars];
			}
			else
			{
				this[FeatureNames.NrUserDefinedAllVarsRatio] = 0.0;
			}
			if (this[FeatureNames.NrOfIntVarsUserDefined] > 0.0)
			{
				this[FeatureNames.NrUserDefinedBoolIntVarsRatio] = this[FeatureNames.NrOfBoolVarsUserDefined] / this[FeatureNames.NrOfIntVarsUserDefined];
			}
			else
			{
				this[FeatureNames.NrUserDefinedBoolIntVarsRatio] = 0.0;
			}
			if (this[FeatureNames.NrOfIntVars] > 0.0)
			{
				this[FeatureNames.NrAllBoolIntVarsRatio] = this[FeatureNames.NrOfBoolVars] / this[FeatureNames.NrOfIntVars];
			}
			else
			{
				this[FeatureNames.NrAllBoolIntVarsRatio] = 0.0;
			}
			if (this[FeatureNames.NrOfConstraints] > 0.0)
			{
				this[FeatureNames.UDVarConstraintRatio] = this[FeatureNames.NrUserDefinedVars] / this[FeatureNames.NrOfConstraints];
			}
			else
			{
				this[FeatureNames.UDVarConstraintRatio] = 0.0;
			}
			if (this[FeatureNames.NrOfConstraints] > 0.0)
			{
				this[FeatureNames.UDBoolVarConstraintRatio] = this[FeatureNames.NrOfBoolVarsUserDefined] / this[FeatureNames.NrOfConstraints];
			}
			else
			{
				this[FeatureNames.UDBoolVarConstraintRatio] = 0.0;
			}
			if (this[FeatureNames.NrOfConstraints] > 0.0)
			{
				this[FeatureNames.UDIntVarConstraintRatio] = this[FeatureNames.NrOfIntVarsUserDefined] / this[FeatureNames.NrOfConstraints];
			}
			else
			{
				this[FeatureNames.UDIntVarConstraintRatio] = 0.0;
			}
			if (num2 > 0)
			{
				this[FeatureNames.AverageDomainSize] = (double)num5 / (double)num2;
			}
			else
			{
				this[FeatureNames.AverageDomainSize] = 0.0;
			}
			if (num4 > 0)
			{
				this[FeatureNames.AverageUDDomainSize] = (double)num6 / (double)num4;
			}
			else
			{
				this[FeatureNames.AverageUDDomainSize] = 0.0;
			}
			if (this[FeatureNames.NrOfConstraints] > 0.0)
			{
				this[FeatureNames.VarWithMaxConstraintsRatio] = (double)num13 / this[FeatureNames.NrOfConstraints];
			}
			else
			{
				this[FeatureNames.VarWithMaxConstraintsRatio] = 0.0;
			}
			if (this[FeatureNames.NrOfVars] > 0.0)
			{
				this[FeatureNames.NrOfInstantiatedVarsRatio] = (double)num14 / this[FeatureNames.NrOfVars];
			}
			else
			{
				this[FeatureNames.NrOfInstantiatedVarsRatio] = 0.0;
			}
		}

		/// <summary>
		/// Get number of features 
		/// </summary>
		public int GetNumberOfFeatures()
		{
			return nNumberOfFeatures;
		}

		/// <summary>
		/// Display collected features on Console
		/// </summary>
		public void DisplayFeatures()
		{
			int num = 0;
			int num2 = 1;
			Console.WriteLine();
			Console.WriteLine(Resources.DisplayFeaturesTotalNumberOfFeatures0, GetNumberOfFeatures());
			Console.WriteLine();
			foreach (FeatureNames value in Enum.GetValues(typeof(FeatureNames)))
			{
				string text = value.ToString();
				if (text.Equals("AdditionRatio"))
				{
					Console.WriteLine();
					Console.WriteLine(Resources.DisplayFeaturesXXXX);
					Console.WriteLine();
					Console.WriteLine(Resources.DisplayFeaturesOccurenceOfConstraintsRatioCountAllConstraints);
					Console.WriteLine();
				}
				else if (text.Equals("DomainHist0"))
				{
					Console.WriteLine();
					Console.WriteLine(Resources.DisplayFeaturesXXXX);
					Console.WriteLine();
					Console.WriteLine(Resources.DisplayFeaturesHistograms);
					Console.WriteLine();
					break;
				}
				Console.Write("{0} {1}            ", value, Math.Round(this[value], 2));
				if (text.Equals("VarWithMaxConstraintsRatio"))
				{
					Console.WriteLine();
					Console.Write(Resources.DisplayFeaturesDensestVariableConnectedTo0OfConstraints, Math.Round(this[value], 2) * 100.0);
				}
				num++;
				if (num % num2 == 0)
				{
					Console.WriteLine();
				}
			}
			Console.WriteLine();
			int num3 = num;
			for (int i = 0; i < 6; i++)
			{
				Console.WriteLine(Resources.DisplayFeaturesXXXX);
				Console.WriteLine();
				switch (i)
				{
				case 0:
					Console.WriteLine(Resources.DisplayFeaturesHistogramOverDomainSizeAndAllVars);
					break;
				case 1:
					Console.WriteLine(Resources.DisplayFeaturesHistogramOverDomainSizeAndUserDefinedVars);
					break;
				case 2:
					Console.WriteLine(Resources.DisplayFeaturesHistogramOverNumberOfConstraintsAndAllVars);
					break;
				case 3:
					Console.WriteLine(Resources.DisplayFeaturesHistogramOverNumberOfConstraintsAndUserDefinedVars);
					break;
				case 4:
					Console.WriteLine(Resources.DisplayFeaturesHistogramOverNumberOfConstraintsAndIntegerVars);
					break;
				case 5:
					Console.WriteLine(Resources.DisplayFeaturesHistogramOverNumberOfConstraintsAndBoolVars);
					break;
				}
				Console.WriteLine();
				for (int num4 = 10; num4 > 0; num4--)
				{
					for (int j = num3; j < num3 + 10; j++)
					{
						if ((double)num4 <= Math.Ceiling(dFeatureValues[j] * 10.0))
						{
							Console.Write("|*");
						}
						else
						{
							Console.Write("| ");
						}
					}
					Console.WriteLine("|");
				}
				Console.WriteLine();
				Console.WriteLine(Resources.DisplayFeaturesSmallLarge);
				Console.WriteLine();
				num3 += 10;
			}
		}
	}
}
