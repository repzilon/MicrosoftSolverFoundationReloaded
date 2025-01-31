using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Microsoft.SolverFoundation.Properties
{
	/// <summary>
	///   A strongly-typed resource class, for looking up localized strings, etc.
	/// </summary>
	[CompilerGenerated]
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
	[DebuggerNonUserCode]
	internal class Resources
	{
		private static ResourceManager resourceMan;

		private static CultureInfo resourceCulture;

		/// <summary>
		///   Returns the cached ResourceManager instance used by this class.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (object.ReferenceEquals(resourceMan, null))
				{
					ResourceManager resourceManager = new ResourceManager("Microsoft.SolverFoundation.Solvers.Properties.Resources", typeof(Resources).Assembly);
					resourceMan = resourceManager;
				}
				return resourceMan;
			}
		}

		/// <summary>
		///   Overrides the current thread's CurrentUICulture property for all
		///   resource lookups using this strongly typed resource class.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return resourceCulture;
			}
			set
			{
				resourceCulture = value;
			}
		}

		/// <summary>
		///   Looks up a localized string similar to Aborted.
		/// </summary>
		internal static string Aborted => ResourceManager.GetString("Aborted", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to A bound constraint is neither a upper-bound constraint nor a lower-bound constraint.
		/// </summary>
		internal static string ABoundConstraintIsNeitherAUpperBoundConstraintNorALowerBoundConstraint => ResourceManager.GetString("ABoundConstraintIsNeitherAUpperBoundConstraintNorALowerBoundConstraint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Accepted contracted: {0}..
		/// </summary>
		internal static string AcceptedContractedCount0 => ResourceManager.GetString("AcceptedContractedCount0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Accepted expanded: {0}..
		/// </summary>
		internal static string AcceptedExpandedCount0 => ResourceManager.GetString("AcceptedExpandedCount0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Accepted reflected: {0}..
		/// </summary>
		internal static string AcceptedReflectedCount0 => ResourceManager.GetString("AcceptedReflectedCount0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Actual size of matrix is too big (more than {1}).
		/// </summary>
		internal static string ActualSizeOfMatrixTooBig0 => ResourceManager.GetString("ActualSizeOfMatrixTooBig0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to AddChange {0} {1}.
		/// </summary>
		internal static string AddChange01 => ResourceManager.GetString("AddChange01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Trying to add constant False as a constraint. The model is infeasible.
		/// </summary>
		internal static string AddingFalseAsConstraint => ResourceManager.GetString("AddingFalseAsConstraint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to After-fill non-zeros {0}.
		/// </summary>
		internal static string AfterFillNonZeros0 => ResourceManager.GetString("AfterFillNonZeros0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to All bindable parameters should have a binding clause in the Data Binding section..
		/// </summary>
		internal static string AllBindableParametersShouldHaveBindClause => ResourceManager.GetString("AllBindableParametersShouldHaveBindClause", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to All variables should be active for the row..
		/// </summary>
		internal static string AllVariablesShouldBeActiveForTheRow => ResourceManager.GetString("AllVariablesShouldBeActiveForTheRow", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solve is in progress .
		/// </summary>
		internal static string AlreadySolving => ResourceManager.GetString("AlreadySolving", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Argument {0} to index operation is of an invalid type.
		/// </summary>
		internal static string Argument0ToIndexOperationIsOfAnInvalidType => ResourceManager.GetString("Argument0ToIndexOperationIsOfAnInvalidType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Argument is not a tuple.
		/// </summary>
		internal static string ArgumentIsNotATuple => ResourceManager.GetString("ArgumentIsNotATuple", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to At least one argument expected.
		/// </summary>
		internal static string AtLeast1ArgumentExpected => ResourceManager.GetString("AtLeast1ArgumentExpected", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to AtMostMofN expects Boolean Terms.
		/// </summary>
		internal static string AtMostMofNExpectsBooleanTerms => ResourceManager.GetString("AtMostMofNExpectsBooleanTerms", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Attributes for symbol '{0}' are locked..
		/// </summary>
		internal static string AttributesForSymbolAreLocked => ResourceManager.GetString("AttributesForSymbolAreLocked", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to   average depth / estimated tree size : {0} {1}.
		/// </summary>
		internal static string AverageDepthEstimatedTreeSize01 => ResourceManager.GetString("AverageDepthEstimatedTreeSize01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Bad annotation.
		/// </summary>
		internal static string BadAnnotation => ResourceManager.GetString("BadAnnotation", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Bad constraint.
		/// </summary>
		internal static string BadConstraint => ResourceManager.GetString("BadConstraint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Bad iterator.
		/// </summary>
		internal static string BadIterator => ResourceManager.GetString("BadIterator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Bad iterator {0}.
		/// </summary>
		internal static string BadIterator0 => ResourceManager.GetString("BadIterator0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Bad Kind.
		/// </summary>
		internal static string BadKind => ResourceManager.GetString("BadKind", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Bad mark.
		/// </summary>
		internal static string BadMark => ResourceManager.GetString("BadMark", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The name '{0}' contains invalid characters. Names must begin with a letter or underscore, and contain only letters, numbers, and underscores..
		/// </summary>
		internal static string BadOMLName => ResourceManager.GetString("BadOMLName", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Bad rule.
		/// </summary>
		internal static string BadRule => ResourceManager.GetString("BadRule", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Bad TextVersion in new-line token.
		/// </summary>
		internal static string BadTextVersionInNewLineToken => ResourceManager.GetString("BadTextVersionInNewLineToken", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Bad ValueKind.
		/// </summary>
		internal static string BadValueKind => ResourceManager.GetString("BadValueKind", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Bind data clauses should have parameter name and input string.
		/// </summary>
		internal static string BinddataClausesShouldHaveParameterAndInput => ResourceManager.GetString("BinddataClausesShouldHaveParameterAndInput", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot bind data to {0} from {1}. {1} either represents multiple cells or an empty cell..
		/// </summary>
		internal static string BindDataInErrorEmptyCellFormat => ResourceManager.GetString("BindDataInErrorEmptyCellFormat", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot bind data to {0}, please specify cell ranges..
		/// </summary>
		internal static string BindDataInErrorMissingCellFormat => ResourceManager.GetString("BindDataInErrorMissingCellFormat", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Boolean functions cannot be used as goals. Consider changing to Integer..
		/// </summary>
		internal static string BooleanGoal => ResourceManager.GetString("BooleanGoal", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Bounded vid was neither a row nor a variable..
		/// </summary>
		internal static string BoundedVidWasNeitherRowNorVar => ResourceManager.GetString("BoundedVidWasNeitherRowNorVar", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Bounds for discrete uniform distribution need to be integers.
		/// </summary>
		internal static string BoundsForDiscreteUniformDistriutionNeedsToBeIntegers => ResourceManager.GetString("BoundsForDiscreteUniformDistriutionNeedsToBeIntegers", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Minimum or maximum boundaries are not numeric.
		/// </summary>
		internal static string BundariesAreNotNumeric => ResourceManager.GetString("BundariesAreNotNumeric", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} called on non-type.
		/// </summary>
		internal static string CalledOnNonType => ResourceManager.GetString("CalledOnNonType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} called on &lt;null&gt;.
		/// </summary>
		internal static string CalledOnNull => ResourceManager.GetString("CalledOnNull", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot add cut: {0} to the model.
		/// </summary>
		internal static string CannotAddCutXToModel0 => ResourceManager.GetString("CannotAddCutXToModel0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot bind read-only data table for output.
		/// </summary>
		internal static string CannotBindReadOnlyDataTableForOutput => ResourceManager.GetString("CannotBindReadOnlyDataTableForOutput", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Given Term cannot be cloned.
		/// </summary>
		internal static string CannotCloneTerm => ResourceManager.GetString("CannotCloneTerm", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot create decision binding on decision '{0}' because it is not added to any model.
		/// </summary>
		internal static string CannotCreateDecisionBindingOnDecisionNotAddedToAModel => ResourceManager.GetString("CannotCreateDecisionBindingOnDecisionNotAddedToAModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot create decision binding on decision '{0}' because it is not added to the root model.
		/// </summary>
		internal static string CannotCreateDecisionBindingOnDecisionNotAddedToRootModel => ResourceManager.GetString("CannotCreateDecisionBindingOnDecisionNotAddedToRootModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot enumerate an infinite number of scenarios.
		/// </summary>
		internal static string CannotEnumerateAnInfiniteNumberOfScenarios => ResourceManager.GetString("CannotEnumerateAnInfiniteNumberOfScenarios", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot enumerate scenarios for binomial value. Probability of a scenario with {0} successful trials is insufficiently large..
		/// </summary>
		internal static string CannotEnumerateScenariosForBinomialValueProbabilityOfAScenarioWithSuccessfulTrialsIsInsufficientlyLarge => ResourceManager.GetString("CannotEnumerateScenariosForBinomialValueProbabilityOfAScenarioWithSuccessfulTrialsIsInsufficientlyLarge", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot find domain.
		/// </summary>
		internal static string CanNotFindDomain => ResourceManager.GetString("CanNotFindDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot find indexes for decision {0}..
		/// </summary>
		internal static string CannotFindIndexesForDecision0 => ResourceManager.GetString("CannotFindIndexesForDecision0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot find the constraint in the model..
		/// </summary>
		internal static string CannotFindTheConstraintInTheModel => ResourceManager.GetString("CannotFindTheConstraintInTheModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot find the decision in the model: {0}.  Make sure that the decision has been added to the model and that it is used in at least one goal or constraint..
		/// </summary>
		internal static string CannotFindTheDecisionAndIndexesInTheModel0 => ResourceManager.GetString("CannotFindTheDecisionAndIndexesInTheModel0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot find virtual path extension from WCF host..
		/// </summary>
		internal static string CannotFindVirtualPathExtensionFromWCFHost => ResourceManager.GetString("CannotFindVirtualPathExtensionFromWCFHost", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot import constant parameters..
		/// </summary>
		internal static string CannotImportConstantParameter => ResourceManager.GetString("CannotImportConstantParameter", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot import Foreach decisions..
		/// </summary>
		internal static string CannotImportForeachDecision => ResourceManager.GetString("CannotImportForeachDecision", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot import non-constant iterators..
		/// </summary>
		internal static string CannotImportNonConstantIterator => ResourceManager.GetString("CannotImportNonConstantIterator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot import non-Set iterators..
		/// </summary>
		internal static string CannotImportNonSetIterator => ResourceManager.GetString("CannotImportNonSetIterator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot index non-Decisions..
		/// </summary>
		internal static string CannotIndexNonDecision => ResourceManager.GetString("CannotIndexNonDecision", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot remove {0} because it is being used..
		/// </summary>
		internal static string CannotRemove0BecauseItIsUsed => ResourceManager.GetString("CannotRemove0BecauseItIsUsed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot represent {0} as an integer.
		/// </summary>
		internal static string CannotRepresentAsAnInteger => ResourceManager.GetString("CannotRepresentAsAnInteger", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot reuse decision {0} in a different model.
		/// </summary>
		internal static string CannotReuseDecision0InADifferentModel => ResourceManager.GetString("CannotReuseDecision0InADifferentModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot reuse parameter {0} in a different model..
		/// </summary>
		internal static string CannotReuseParameter0InADifferentModel => ResourceManager.GetString("CannotReuseParameter0InADifferentModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot reuse set {0} in a different model..
		/// </summary>
		internal static string CannotReuseSet0InADifferentModel => ResourceManager.GetString("CannotReuseSet0InADifferentModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot set bounds on a quadratic goal..
		/// </summary>
		internal static string CannotSetBoundsOnAQuadraticGoal => ResourceManager.GetString("CannotSetBoundsOnAQuadraticGoal", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot use IntegerSolver to solve a non-integer model..
		/// </summary>
		internal static string CannotSolve => ResourceManager.GetString("CannotSolve", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot solve because there is no active model..
		/// </summary>
		internal static string CannotSolveBecauseThereIsNoActiveModel => ResourceManager.GetString("CannotSolveBecauseThereIsNoActiveModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot specify Domain.Any for a parameter..
		/// </summary>
		internal static string CanNotSpecifyDomainAnyToParameter => ResourceManager.GetString("CanNotSpecifyDomainAnyToParameter", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot use 'Identical' ('===') or Set ('=') in constraints - try using 'Equal' ('==')..
		/// </summary>
		internal static string CannotUseIdenticalOrSet => ResourceManager.GetString("CannotUseIdenticalOrSet", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot use this term. Only Decisions, Parameters and Constraints can be used.
		/// </summary>
		internal static string CannotUseThisTerm => ResourceManager.GetString("CannotUseThisTerm", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot use the term ('{0}'). Only Decisions, Parameters and Constraints can be used.
		/// </summary>
		internal static string CannotUseThisTerm0 => ResourceManager.GetString("CannotUseThisTerm0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot change coefficients for row variables.
		/// </summary>
		internal static string CanTChangeCoefficientsForRowVariables => ResourceManager.GetString("CanTChangeCoefficientsForRowVariables", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot mix expressions from different RewriteSystems.
		/// </summary>
		internal static string CanTMixExpressionFromDifferentRewriteSystems => ResourceManager.GetString("CanTMixExpressionFromDifferentRewriteSystems", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid term to clone.
		/// </summary>
		internal static string CloneInvalidTerm => ResourceManager.GetString("CloneInvalidTerm", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot create IntegerSolver instance because the model uses an unsupported Domain.
		/// </summary>
		internal static string CloneUnsupportedDomain => ResourceManager.GetString("CloneUnsupportedDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ClrObjectWrapper exception: {0}.
		/// </summary>
		internal static string ClrObjectWrapperException0 => ResourceManager.GetString("ClrObjectWrapperException0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Coefficient for variable in a SOS row cannot be zero.
		/// </summary>
		internal static string CoefficientForVariableInASOSRowCannotBeZero => ResourceManager.GetString("CoefficientForVariableInASOSRowCannotBeZero", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ColumnCompressedSparseMatrix does not support element insertion.
		/// </summary>
		internal static string ColumnCompressedSparseMatrixDoesNotSupportElementInsertion => ResourceManager.GetString("ColumnCompressedSparseMatrixDoesNotSupportElementInsertion", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to column indexes did not sort correctly.
		/// </summary>
		internal static string ColumnIndexesDidNotSortCorrectly => ResourceManager.GetString("ColumnIndexesDidNotSortCorrectly", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Comparisons should have two or more arguments.
		/// </summary>
		internal static string ComparisonsShouldHave2Arguments => ResourceManager.GetString("ComparisonsShouldHave2Arguments", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Complete.
		/// </summary>
		internal static string Complete => ResourceManager.GetString("Complete", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to CspComposite does not support this constraint .
		/// </summary>
		internal static string CompositeConstraintNotSupported => ResourceManager.GetString("CompositeConstraintNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The composite domains of the input of this function are not compatible .
		/// </summary>
		internal static string CompositeDomainIncompatible => ResourceManager.GetString("CompositeDomainIncompatible", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The function does not support composite variables as input..
		/// </summary>
		internal static string CompositeDomainNotSupported => ResourceManager.GetString("CompositeDomainNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Field has been added to this composite .
		/// </summary>
		internal static string CompositeDuplicateFields => ResourceManager.GetString("CompositeDuplicateFields", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Arity of a field in the composite cannot be less than or equal to 0 .
		/// </summary>
		internal static string CompositeFieldArityZero => ResourceManager.GetString("CompositeFieldArityZero", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Composite cannot be changed since variables of this composite have been created .
		/// </summary>
		internal static string CompositeFroze => ResourceManager.GetString("CompositeFroze", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The index to the field of the composite is out of range .
		/// </summary>
		internal static string CompositeIndexOutOfRange => ResourceManager.GetString("CompositeIndexOutOfRange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Specified field does not exist in this composite .
		/// </summary>
		internal static string CompositeInvalidFieldReference => ResourceManager.GetString("CompositeInvalidFieldReference", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Terms created in a CspComposite must use other terms created in the same CspComposite as inputs.
		/// </summary>
		internal static string CompositeUnknownInputs => ResourceManager.GetString("CompositeUnknownInputs", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot use GetValue to retrieve value of a composite variable. Need to retrieve the values from its fields manually .
		/// </summary>
		internal static string CompositeVarGetValueNotSupported => ResourceManager.GetString("CompositeVarGetValueNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to 
		///             Computational progress of HSD solver:
		///             .
		/// </summary>
		internal static string ComputationalProgressOfHSDSolver => ResourceManager.GetString("ComputationalProgressOfHSDSolver", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number of Bool vars:              {0,-10} .
		/// </summary>
		internal static string ComputeStatisticsBoolVars010 => ResourceManager.GetString("ComputeStatisticsBoolVars010", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number of constraints:            {0,-10}
		///             .
		/// </summary>
		internal static string ComputeStatisticsConstraints010 => ResourceManager.GetString("ComputeStatisticsConstraints010", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Delay since start:       {0}ms
		///             .
		/// </summary>
		internal static string ComputeStatisticsDelaySinceStart0Ms => ResourceManager.GetString("ComputeStatisticsDelaySinceStart0Ms", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number of Int vars:               {0,-10} .
		/// </summary>
		internal static string ComputeStatisticsIntVars010 => ResourceManager.GetString("ComputeStatisticsIntVars010", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to (user defined: {0,8})
		///             .
		/// </summary>
		internal static string ComputeStatisticsUserdefined08 => ResourceManager.GetString("ComputeStatisticsUserdefined08", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Condition of ForEach is not constant.
		/// </summary>
		internal static string ConditionOfForEachIsNotConstant => ResourceManager.GetString("ConditionOfForEachIsNotConstant", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cone type {0} is not supported..
		/// </summary>
		internal static string ConeType0NotSupported => ResourceManager.GetString("ConeType0NotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Constant value is not an integer or a Boolean.
		/// </summary>
		internal static string ConstantValueIsNotAnInteger => ResourceManager.GetString("ConstantValueIsNotAnInteger", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Constant values may not be redefined..
		/// </summary>
		internal static string ConstantValuesMayNotBeRedefined => ResourceManager.GetString("ConstantValuesMayNotBeRedefined", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Constraint label already exists..
		/// </summary>
		internal static string ConstraintLabelAlreadyExists => ResourceManager.GetString("ConstraintLabelAlreadyExists", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Could not find stochastic core file or directory {0}..
		/// </summary>
		internal static string CouldNotFindStochasticCoreFileOrDirectory0 => ResourceManager.GetString("CouldNotFindStochasticCoreFileOrDirectory0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Could not get double value.
		/// </summary>
		internal static string CouldNotGetDoubleValue => ResourceManager.GetString("CouldNotGetDoubleValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Failed to parse MPS model..
		/// </summary>
		internal static string CouldNotParseMPSModel => ResourceManager.GetString("CouldNotParseMPSModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Failed to parse OML model. Expression: {0}..
		/// </summary>
		internal static string CouldNotParseOMLModel0 => ResourceManager.GetString("CouldNotParseOMLModel0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Failed to parse SMPS model..
		/// </summary>
		internal static string CouldNotParseSMPSModel => ResourceManager.GetString("CouldNotParseSMPSModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot print sensitivity value.
		/// </summary>
		internal static string CouldNotPrintSensitivityValue => ResourceManager.GetString("CouldNotPrintSensitivityValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to OML could not find a solver for the model.
		/// </summary>
		internal static string CouldntFindASolverForTheModel => ResourceManager.GetString("CouldntFindASolverForTheModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ----------------------------------------------------------------------------.
		/// </summary>
		internal static string CqnDashes => ResourceManager.GetString("CqnDashes", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to CompactQuasiNewtonSolver requires both FunctionEvaluator and GradientEvaluator to be specified before calling solve..
		/// </summary>
		internal static string CqnSolverRequiresBothEvaluatorsToBeSpecifiedBeforeCallingSolve => ResourceManager.GetString("CqnSolverRequiresBothEvaluatorsToBeSpecifiedBeforeCallingSolve", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0,0:000}               {1,0:0.0000e+00}   {2,0:0000}                    {3}.
		/// </summary>
		internal static string CqnTraceIteration0123 => ResourceManager.GetString("CqnTraceIteration0123", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Iteration count   New value    Evaluation call count   Termination criterion.
		/// </summary>
		internal static string CqnTraceIterationHeader => ResourceManager.GetString("CqnTraceIterationHeader", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Starting          {0,0:0.0000e+00}   {1,0:0000}                    {2}.
		/// </summary>
		internal static string CqnTraceStarting012 => ResourceManager.GetString("CqnTraceStarting012", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to CSP solver cannot process the model: '{0}', '{1}'.
		/// </summary>
		internal static string CSPSolverCantProcessTheModel01 => ResourceManager.GetString("CSPSolverCantProcessTheModel01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to CSP solver does not support non-integer values.
		/// </summary>
		internal static string CSPSolverDoesNotSupportNonIntegerValues => ResourceManager.GetString("CSPSolverDoesNotSupportNonIntegerValues", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Indexed Tuples can only be used for random Parameters..
		/// </summary>
		internal static string CurrentlyOnlyTuplesWhichIsUsedAsARandomParameterSupportsIndexing => ResourceManager.GetString("CurrentlyOnlyTuplesWhichIsUsedAsARandomParameterSupportsIndexing", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot perform cut generation in a multi-threaded environment .
		/// </summary>
		internal static string CutGenException => ResourceManager.GetString("CutGenException", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Lower: {0}, Upper: {1}, Entry count: {2}.
		/// </summary>
		internal static string CutLowerUpperEntryCount012 => ResourceManager.GetString("CutLowerUpperEntryCount012", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cycle! {0} {1}.
		/// </summary>
		internal static string Cycle01 => ResourceManager.GetString("Cycle01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Data binding failed for parameter {0} because {2} is not a valid index for set {1}..
		/// </summary>
		internal static string DataBindingFailedForParameter012 => ResourceManager.GetString("DataBindingFailedForParameter012", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Decimal value out of range .
		/// </summary>
		internal static string DecimalValueOutOfRange => ResourceManager.GetString("DecimalValueOutOfRange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Decisions cannot be disabled.
		/// </summary>
		internal static string DecisionsCannotBeDisabled => ResourceManager.GetString("DecisionsCannotBeDisabled", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Decomposition.
		/// </summary>
		internal static string Decomposition => ResourceManager.GetString("Decomposition", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Decomposition cannot be applied because the master model is unbounded.
		/// </summary>
		internal static string DecompositionCannotBeApplied => ResourceManager.GetString("DecompositionCannotBeApplied", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Decomposition cannot be applied as there is a decision which participates only in second stage constraints, and has an infinite bound..
		/// </summary>
		internal static string DecompositionCannotBeAppliedInfiniteBound => ResourceManager.GetString("DecompositionCannotBeAppliedInfiniteBound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Decomposition Gap: {0}.
		/// </summary>
		internal static string DecompositionGap0 => ResourceManager.GetString("DecompositionGap0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Decomposition Iterations: {0}.
		/// </summary>
		internal static string DecompositionIterations => ResourceManager.GetString("DecompositionIterations", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to defaultValue should be a definite number.
		/// </summary>
		internal static string DefaultValueShouldBeDefiniteNumber => ResourceManager.GetString("DefaultValueShouldBeDefiniteNumber", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to A gradient calculation function must be specified before calling Solve..
		/// </summary>
		internal static string DelegateOfFunctionIsNeeded => ResourceManager.GetString("DelegateOfFunctionIsNeeded", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Deterministic Equivalent.
		/// </summary>
		internal static string DeterministicEquivalent => ResourceManager.GetString("DeterministicEquivalent", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Dimensions = .
		/// </summary>
		internal static string Dimensions => ResourceManager.GetString("Dimensions", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Dimensions = {0}.
		/// </summary>
		internal static string Dimensions0 => ResourceManager.GetString("Dimensions0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Given directive {0} cannot locate a solver that has the capability to solve the model.
		/// </summary>
		internal static string DirectiveCannotLocateSolver => ResourceManager.GetString("DirectiveCannotLocateSolver", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to At least one directive is required.
		/// </summary>
		internal static string DirectiveRequired => ResourceManager.GetString("DirectiveRequired", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected Term kind.
		/// </summary>
		internal static string DisolverBooleanTermExpected => ResourceManager.GetString("DisolverBooleanTermExpected", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to IntegerSolver can't process the model: '{0}', '{1}'.
		/// </summary>
		internal static string DisolverCantProcessTheModel => ResourceManager.GetString("DisolverCantProcessTheModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected Term kind.
		/// </summary>
		internal static string DisolverIntegerTermExpected => ResourceManager.GetString("DisolverIntegerTermExpected", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected Term kind.
		/// </summary>
		internal static string DisolverTermExpected => ResourceManager.GetString("DisolverTermExpected", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to AdditionRatio.
		/// </summary>
		internal static string DisplayFeaturesAdditionRatio => ResourceManager.GetString("DisplayFeaturesAdditionRatio", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to -&gt; Densest variable connected to {0}% of constraints..
		/// </summary>
		internal static string DisplayFeaturesDensestVariableConnectedTo0OfConstraints => ResourceManager.GetString("DisplayFeaturesDensestVariableConnectedTo0OfConstraints", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Histogram over domain size and all vars:.
		/// </summary>
		internal static string DisplayFeaturesHistogramOverDomainSizeAndAllVars => ResourceManager.GetString("DisplayFeaturesHistogramOverDomainSizeAndAllVars", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Histogram over domain size and user-defined vars:.
		/// </summary>
		internal static string DisplayFeaturesHistogramOverDomainSizeAndUserDefinedVars => ResourceManager.GetString("DisplayFeaturesHistogramOverDomainSizeAndUserDefinedVars", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Histogram over number of constraints and all vars:.
		/// </summary>
		internal static string DisplayFeaturesHistogramOverNumberOfConstraintsAndAllVars => ResourceManager.GetString("DisplayFeaturesHistogramOverNumberOfConstraintsAndAllVars", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Histogram over number of constraints and bool vars:.
		/// </summary>
		internal static string DisplayFeaturesHistogramOverNumberOfConstraintsAndBoolVars => ResourceManager.GetString("DisplayFeaturesHistogramOverNumberOfConstraintsAndBoolVars", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Histogram over number of constraints and integer vars:.
		/// </summary>
		internal static string DisplayFeaturesHistogramOverNumberOfConstraintsAndIntegerVars => ResourceManager.GetString("DisplayFeaturesHistogramOverNumberOfConstraintsAndIntegerVars", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Histogram over number of constraints and user-defined vars:.
		/// </summary>
		internal static string DisplayFeaturesHistogramOverNumberOfConstraintsAndUserDefinedVars => ResourceManager.GetString("DisplayFeaturesHistogramOverNumberOfConstraintsAndUserDefinedVars", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Histograms.
		/// </summary>
		internal static string DisplayFeaturesHistograms => ResourceManager.GetString("DisplayFeaturesHistograms", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Occurrence of Constraints (Ratio [= Count/AllConstraints]).
		/// </summary>
		internal static string DisplayFeaturesOccurenceOfConstraintsRatioCountAllConstraints => ResourceManager.GetString("DisplayFeaturesOccurenceOfConstraintsRatioCountAllConstraints", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Small ----------- Large.
		/// </summary>
		internal static string DisplayFeaturesSmallLarge => ResourceManager.GetString("DisplayFeaturesSmallLarge", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Total number of features: {0}.
		/// </summary>
		internal static string DisplayFeaturesTotalNumberOfFeatures0 => ResourceManager.GetString("DisplayFeaturesTotalNumberOfFeatures0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX.
		/// </summary>
		internal static string DisplayFeaturesXXXX => ResourceManager.GetString("DisplayFeaturesXXXX", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} with its current parameters cannot be sampled with LatinHypercube method.
		/// </summary>
		internal static string Distribution0WithItsCurrentParametersCannotBeSampledWithLatinHypercubeMethod => ResourceManager.GetString("Distribution0WithItsCurrentParametersCannotBeSampledWithLatinHypercubeMethod", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to This {0} does not belong to the matrix.
		/// </summary>
		internal static string DoesNotBelongToMatrix0 => ResourceManager.GetString("DoesNotBelongToMatrix0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} does not have a numeric value..
		/// </summary>
		internal static string DoesNotHaveANumericValue => ResourceManager.GetString("DoesNotHaveANumericValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} does not yet have a value..
		/// </summary>
		internal static string DoesNotYetHaveAValue => ResourceManager.GetString("DoesNotYetHaveAValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} domain can not have boundaries.
		/// </summary>
		internal static string DomainCanNotHaveBundaries0 => ResourceManager.GetString("DomainCanNotHaveBundaries0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Index is out of the domain .
		/// </summary>
		internal static string DomainIndexOutOfRange => ResourceManager.GetString("DomainIndexOutOfRange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Domains section is not allowed in submodels..
		/// </summary>
		internal static string DomainsSectionIsNotAllowedInSubmodels => ResourceManager.GetString("DomainsSectionIsNotAllowedInSubmodels", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Domains section must contain a domain and a name.
		/// </summary>
		internal static string DomainsSectionMustContainADomainAndAName => ResourceManager.GetString("DomainsSectionMustContainADomainAndAName", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Values in the domain are not ordered distinct, or out of range .
		/// </summary>
		internal static string DomainValueOutOfRange => ResourceManager.GetString("DomainValueOutOfRange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Duplicated set.
		/// </summary>
		internal static string DuplicatedSet => ResourceManager.GetString("DuplicatedSet", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Duplicated entries detected in data bound to {0}.
		/// </summary>
		internal static string DuplicateEntriesDetectedInDataBoundTo0 => ResourceManager.GetString("DuplicateEntriesDetectedInDataBoundTo0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to A variable of the same key already exists .
		/// </summary>
		internal static string DuplicateKey => ResourceManager.GetString("DuplicateKey", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Duplicate name {0}.
		/// </summary>
		internal static string DuplicateName0 => ResourceManager.GetString("DuplicateName0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Duplicate Parameter.
		/// </summary>
		internal static string DuplicateParameter => ResourceManager.GetString("DuplicateParameter", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Duplicate row names: '{0}'.
		/// </summary>
		internal static string DuplicateRowNames => ResourceManager.GetString("DuplicateRowNames", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Duplicate TokKind for TokKindEnum value.
		/// </summary>
		internal static string DuplicateTokKindForTokKindEnumValue => ResourceManager.GetString("DuplicateTokKindForTokKindEnumValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Duplicate Decision.
		/// </summary>
		internal static string DuplicateVariable => ResourceManager.GetString("DuplicateVariable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}' is a duplicate Decision.
		/// </summary>
		internal static string DuplicateVariable0 => ResourceManager.GetString("DuplicateVariable0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Each element in the list must be a Tuple.
		/// </summary>
		internal static string EachElementInTheListMustBeATuple => ResourceManager.GetString("EachElementInTheListMustBeATuple", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Each operation must have at least one input.
		/// </summary>
		internal static string EachOperationMustHaveAtLeastOneInput => ResourceManager.GetString("EachOperationMustHaveAtLeastOneInput", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Each scenario should be initiated with Probability and Value.
		/// </summary>
		internal static string EachScenarioShouldHaveProbabilityAndValue => ResourceManager.GetString("EachScenarioShouldHaveProbabilityAndValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ElementOf must have exactly two arguments.
		/// </summary>
		internal static string ElementOfMustHaveExactlyTwoArguments => ResourceManager.GetString("ElementOfMustHaveExactlyTwoArguments", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ElementOf only allows integer and enumerated domains..
		/// </summary>
		internal static string ElementOfOnlyAllowsIntegerAndEnumeratedDomains => ResourceManager.GetString("ElementOfOnlyAllowsIntegerAndEnumeratedDomains", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuples in ElementOf are not bound to any data source.
		/// </summary>
		internal static string ElementOfTuplesAreUnbound => ResourceManager.GetString("ElementOfTuplesAreUnbound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Empty argument list in Equal.
		/// </summary>
		internal static string EmptyArgListInEqual => ResourceManager.GetString("EmptyArgListInEqual", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Empty argument list in Product.
		/// </summary>
		internal static string EmptyArgListInProduct => ResourceManager.GetString("EmptyArgListInProduct", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Empty argument list in Unequal.
		/// </summary>
		internal static string EmptyArgListInUnequal => ResourceManager.GetString("EmptyArgListInUnequal", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The heap is empty .
		/// </summary>
		internal static string EmptyHeap => ResourceManager.GetString("EmptyHeap", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Empty INDEP section.
		/// </summary>
		internal static string EmptyINDEPSection => ResourceManager.GetString("EmptyINDEPSection", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Enum domain cannot contain duplicate value '{0}'.
		/// </summary>
		internal static string EnumDomainCannotContainDuplicatedValues0 => ResourceManager.GetString("EnumDomainCannotContainDuplicatedValues0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Enum domain cannot contain null or empty string values.
		/// </summary>
		internal static string EnumDomainCannotContainNullOrEmptyStringValue => ResourceManager.GetString("EnumDomainCannotContainNullOrEmptyStringValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Enum domain must have at least one element.
		/// </summary>
		internal static string EnumDomainMustHaveAtLeastOneElement => ResourceManager.GetString("EnumDomainMustHaveAtLeastOneElement", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to EnumDomain(TAnEnum) requires Enumeration Type as argument.
		/// </summary>
		internal static string EnumDomainRequiresEnumerationTypeAsArgument => ResourceManager.GetString("EnumDomainRequiresEnumerationTypeAsArgument", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Enumerated domains are not supported in OML.
		/// </summary>
		internal static string EnumeratedDomainsAreNotSupportedInOML => ResourceManager.GetString("EnumeratedDomainsAreNotSupportedInOML", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot enumerate elements in an empty domain .
		/// </summary>
		internal static string EnumerateEmptyDomain => ResourceManager.GetString("EnumerateEmptyDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to EnumerateInterimSolutions in ConstraintSolverParams must be enabled for local search.
		/// </summary>
		internal static string EnumerateInterimSolutionsMustBeEnabledForLocalSearch => ResourceManager.GetString("EnumerateInterimSolutionsMustBeEnabledForLocalSearch", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Empty character literal.
		/// </summary>
		internal static string ErrObjEmptyCharacterLiteral => ResourceManager.GetString("ErrObjEmptyCharacterLiteral", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to End-of-file found, '*/' expected.
		/// </summary>
		internal static string ErrObjEndOfFileFoundExpected => ResourceManager.GetString("ErrObjEndOfFileFoundExpected", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expected character '{0}'.
		/// </summary>
		internal static string ErrObjExpectedCharacter0 => ResourceManager.GetString("ErrObjExpectedCharacter0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Floating-point constant is outside the range of type '{0}'.
		/// </summary>
		internal static string ErrObjFloatingPointConstantIsOutsideTheRangeOfType0 => ResourceManager.GetString("ErrObjFloatingPointConstantIsOutsideTheRangeOfType0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Integral constant is too large.
		/// </summary>
		internal static string ErrObjIntegralConstantIsTooLarge => ResourceManager.GetString("ErrObjIntegralConstantIsTooLarge", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Keyword, identifier, or string expected after verbatim specifier: @.
		/// </summary>
		internal static string ErrObjKeywordIdentifierOrStringExpectedAfterVerbatimSpecifier => ResourceManager.GetString("ErrObjKeywordIdentifierOrStringExpectedAfterVerbatimSpecifier", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Newline in constant.
		/// </summary>
		internal static string ErrObjNewlineInConstant => ResourceManager.GetString("ErrObjNewlineInConstant", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number past column {0}.
		/// </summary>
		internal static string ErrObjNumberPastColumn0 => ResourceManager.GetString("ErrObjNumberPastColumn0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Too many characters in character literal.
		/// </summary>
		internal static string ErrObjTooManyCharactersInCharacterLiteral => ResourceManager.GetString("ErrObjTooManyCharactersInCharacterLiteral", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected '{0}'.
		/// </summary>
		internal static string ErrObjUnexpected0 => ResourceManager.GetString("ErrObjUnexpected0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected character '{0}'.
		/// </summary>
		internal static string ErrObjUnexpectedCharacter0 => ResourceManager.GetString("ErrObjUnexpectedCharacter0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unrecognized escape sequence.
		/// </summary>
		internal static string ErrObjUnrecognizedEscapeSequence => ResourceManager.GetString("ErrObjUnrecognizedEscapeSequence", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unterminated string literal.
		/// </summary>
		internal static string ErrObjUnterminatedStringLiteral => ResourceManager.GetString("ErrObjUnterminatedStringLiteral", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Error {0}({1},{2})-({3},{4}): {5}.
		/// </summary>
		internal static string Error012345 => ResourceManager.GetString("Error012345", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Error code {0} :token {3}:line {1}: {2}.
		/// </summary>
		internal static string ErrorCode0Token3Line12 => ResourceManager.GetString("ErrorCode0Token3Line12", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Error filling solver parameters with directive..
		/// </summary>
		internal static string ErrorFillingSolverParametersWithDirective => ResourceManager.GetString("ErrorFillingSolverParametersWithDirective", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to An error occurred when solving the basis .
		/// </summary>
		internal static string ErrorInBasisSolve => ResourceManager.GetString("ErrorInBasisSolve", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Error in basis solve operation! {0} {1} {2}.
		/// </summary>
		internal static string ErrorInBasisSolveOperation012 => ResourceManager.GetString("ErrorInBasisSolveOperation012", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Error in sampling. Result needs to be a 32 bit integer, but it is not. successProbability argument of the distribution is probably too small.
		/// </summary>
		internal static string ErrorSamplingResultNeedsToBeInteger => ResourceManager.GetString("ErrorSamplingResultNeedsToBeInteger", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Error token.
		/// </summary>
		internal static string ErrorToken => ResourceManager.GetString("ErrorToken", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to EV: {0}.
		/// </summary>
		internal static string Ev0 => ResourceManager.GetString("Ev0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Evaluation call count: {0}..
		/// </summary>
		internal static string EvaluationCallCount0 => ResourceManager.GetString("EvaluationCallCount0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to EVPI: {0}.
		/// </summary>
		internal static string Evpi0 => ResourceManager.GetString("Evpi0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ExactlyMofN expects Boolean Terms.
		/// </summary>
		internal static string ExactlyMofNExpectsBooleanTerms => ResourceManager.GetString("ExactlyMofNExpectsBooleanTerms", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Exceeded iteration limit of {0}.
		/// </summary>
		internal static string ExceededIterationLimit => ResourceManager.GetString("ExceededIterationLimit", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Model size exceeds evaluation limit - please see About dialog for more information..
		/// </summary>
		internal static string ExcelModelTooLarge => ResourceManager.GetString("ExcelModelTooLarge", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} Exception: {1}.
		/// </summary>
		internal static string Exception1 => ResourceManager.GetString("Exception1", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Exception thrown from Cholesky thread.
		/// </summary>
		internal static string ExceptionThrownFromCholeskyThread => ResourceManager.GetString("ExceptionThrownFromCholeskyThread", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expect a list of integers or an Integers range.
		/// </summary>
		internal static string ExpectAListOfIntegersOrAnIntegersRange => ResourceManager.GetString("ExpectAListOfIntegersOrAnIntegersRange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expected '{0}'.
		/// </summary>
		internal static string Expected0 => ResourceManager.GetString("Expected0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expected {0} arguments but received {1}.
		/// </summary>
		internal static string Expected0ArgumentsButSaw1 => ResourceManager.GetString("Expected0ArgumentsButSaw1", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expected 'Parameters', 'Decisions', 'Goals', or 'Constraints'.
		/// </summary>
		internal static string ExpectedAllowedSectionSymbols => ResourceManager.GetString("ExpectedAllowedSectionSymbols", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expected Identifier.
		/// </summary>
		internal static string ExpectedIdentifier => ResourceManager.GetString("ExpectedIdentifier", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expected Model.
		/// </summary>
		internal static string ExpectedModel => ResourceManager.GetString("ExpectedModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expected numeric value.
		/// </summary>
		internal static string ExpectedNumericValue => ResourceManager.GetString("ExpectedNumericValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expected Tuples.
		/// </summary>
		internal static string ExpectedTuples => ResourceManager.GetString("ExpectedTuples", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expecting {0} indexes.
		/// </summary>
		internal static string ExpectingIndexes0 => ResourceManager.GetString("ExpectingIndexes0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} expects exactly {1} indexes but {2} were provided.
		/// </summary>
		internal static string ExpectsExactly1IndexesBut2WereProvided => ResourceManager.GetString("ExpectsExactly1IndexesBut2WereProvided", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expired evaluation copy.
		/// </summary>
		internal static string ExpiredEvaluationCopy => ResourceManager.GetString("ExpiredEvaluationCopy", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expression cannot be converted to Term.
		/// </summary>
		internal static string ExpressionCannotBeConvertedToTerm => ResourceManager.GetString("ExpressionCannotBeConvertedToTerm", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expression from wrong RewriteSystem!.
		/// </summary>
		internal static string ExpressionFromWrongRewriteSystem => ResourceManager.GetString("ExpressionFromWrongRewriteSystem", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expression term could not be resolved.
		/// </summary>
		internal static string ExpressionTermCouldNotBeResolved => ResourceManager.GetString("ExpressionTermCouldNotBeResolved", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Factoring failed. Substituted {0} slack variables..
		/// </summary>
		internal static string FactoringFailedSubstituted0SlackVariables => ResourceManager.GetString("FactoringFailedSubstituted0SlackVariables", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Fatal error.
		/// </summary>
		internal static string FatalError => ResourceManager.GetString("FatalError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to FilteredSum not yet implemented.
		/// </summary>
		internal static string FilteredSumNotYetImplemented => ResourceManager.GetString("FilteredSumNotYetImplemented", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Finishing point =.
		/// </summary>
		internal static string FinishingPoint => ResourceManager.GetString("FinishingPoint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Finishing value =.
		/// </summary>
		internal static string FinishingValue => ResourceManager.GetString("FinishingValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} finishing value =.
		/// </summary>
		internal static string FinishingValue0 => ResourceManager.GetString("FinishingValue0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to FiniteSolve needs a Model invocation.
		/// </summary>
		internal static string FiniteSolveNeedsAModelInvocation => ResourceManager.GetString("FiniteSolveNeedsAModelInvocation", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to First argument for binomial distribution must be integer.
		/// </summary>
		internal static string FirstArgumentForBinomialDistributionMustBeInteger => ResourceManager.GetString("FirstArgumentForBinomialDistributionMustBeInteger", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to First argument of TupleMember is not a tuple.
		/// </summary>
		internal static string FirstArgumentOfTupleMemberIsNotATuple => ResourceManager.GetString("FirstArgumentOfTupleMemberIsNotATuple", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Foreach cannot be used as a goal.
		/// </summary>
		internal static string ForeachCannotBeUsedAsAGoal => ResourceManager.GetString("ForeachCannotBeUsedAsAGoal", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Function '{0}[]' not understood in expression.
		/// </summary>
		internal static string FunctionNotUnderstoodInExpression => ResourceManager.GetString("FunctionNotUnderstoodInExpression", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Syntax error in Foreach statement.
		/// </summary>
		internal static string GeneralWrongForeach => ResourceManager.GetString("GeneralWrongForeach", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to GetPowerSetListBaseline is called on a non-set/non-list variable.
		/// </summary>
		internal static string GetPowerSetListBaselineIsCalledOnANonSetNonListVar => ResourceManager.GetString("GetPowerSetListBaselineIsCalledOnANonSetNonListVar", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Goal cannot be a constant.
		/// </summary>
		internal static string GoalCannotBeAConstant => ResourceManager.GetString("GoalCannotBeAConstant", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Goal must be an integer function.
		/// </summary>
		internal static string GoalMustBeAnIntegerFunction => ResourceManager.GetString("GoalMustBeAnIntegerFunction", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Goals cannot contain Decisions multiplied by RandomParameters.
		/// </summary>
		internal static string GoalsCannotContainDecisionsMultipliedByRandomParameters => ResourceManager.GetString("GoalsCannotContainDecisionsMultipliedByRandomParameters", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot add goals to CspComposite .
		/// </summary>
		internal static string GoalsNotSupportedComposite => ResourceManager.GetString("GoalsNotSupportedComposite", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Goals section can contains only Minimize or Maximize clauses.
		/// </summary>
		internal static string GoalsSectionCanContainsOnlyMinimizeOrMaximize => ResourceManager.GetString("GoalsSectionCanContainsOnlyMinimizeOrMaximize", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Goals section can contains only Minimize or Maximize clauses, and not {0}.
		/// </summary>
		internal static string GoalsSectionCanContainsOnlyMinimizeOrMaximizeAndNot0 => ResourceManager.GetString("GoalsSectionCanContainsOnlyMinimizeOrMaximizeAndNot0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Goal was not Minimize or Maximize..
		/// </summary>
		internal static string GoalWasNotMinimizeOrMaximize => ResourceManager.GetString("GoalWasNotMinimizeOrMaximize", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Goal function cannot have an empty domain.
		/// </summary>
		internal static string GoalWithEmptyDomain => ResourceManager.GetString("GoalWithEmptyDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} has multiple values..
		/// </summary>
		internal static string HasMultipleValues => ResourceManager.GetString("HasMultipleValues", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} has to be an Invocation.
		/// </summary>
		internal static string HasToBeInvocation0 => ResourceManager.GetString("HasToBeInvocation0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to This solver does not support quadratic programming..
		/// </summary>
		internal static string HSDDoesNotHandleQuadratic => ResourceManager.GetString("HSDDoesNotHandleQuadratic", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Identifier expected.
		/// </summary>
		internal static string IdentifierExpected => ResourceManager.GetString("IdentifierExpected", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Model is ill-formed and cannot be saved to OML.
		/// </summary>
		internal static string IllFormedModelCannotBeSaved => ResourceManager.GetString("IllFormedModelCannotBeSaved", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Ill-formed rule.
		/// </summary>
		internal static string IllFormedRule => ResourceManager.GetString("IllFormedRule", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid option: '{0}'.
		/// </summary>
		internal static string ImproperOption0 => ResourceManager.GetString("ImproperOption0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The input Terms do not belong to the same CspModel instance .
		/// </summary>
		internal static string IncompatibleInputTerms => ResourceManager.GetString("IncompatibleInputTerms", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The matrices have incompatible sizes .
		/// </summary>
		internal static string IncompatibleMatrixSize => ResourceManager.GetString("IncompatibleMatrixSize", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The vectors have incompatible sizes .
		/// </summary>
		internal static string IncompatibleVectorSize => ResourceManager.GetString("IncompatibleVectorSize", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to In constraint takes two argument and the second argument must be either a list or a tuple.
		/// </summary>
		internal static string InConstraintTakesTwoArgumentAndTheSecondArgumentMustBeEitherAListOrATuple => ResourceManager.GetString("InConstraintTakesTwoArgumentAndTheSecondArgumentMustBeEitherAListOrATuple", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Index [{0}] cannot be found.
		/// </summary>
		internal static string IndexCanNotBeFound0 => ResourceManager.GetString("IndexCanNotBeFound0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to One of the indexes in the two dimensional Index constraint is out of range .
		/// </summary>
		internal static string IndexConstraintOutOfRange => ResourceManager.GetString("IndexConstraintOutOfRange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Indexed decisions may not be probed.
		/// </summary>
		internal static string IndexedDecisionsMayNotBeProbed => ResourceManager.GetString("IndexedDecisionsMayNotBeProbed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Indexed decision used without index.
		/// </summary>
		internal static string IndexedDecisionUsedWithoutIndex => ResourceManager.GetString("IndexedDecisionUsedWithoutIndex", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Internal Error: should be no way that the ixVar can have a value not in the index set .
		/// </summary>
		internal static string IndexError => ResourceManager.GetString("IndexError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Indexes [{0}] cannot be found.
		/// </summary>
		internal static string IndexesCanNotBeFound0 => ResourceManager.GetString("IndexesCanNotBeFound0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Index is not constant.
		/// </summary>
		internal static string IndexIsNotConstant => ResourceManager.GetString("IndexIsNotConstant", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Index out of range.
		/// </summary>
		internal static string IndexOutOfRange => ResourceManager.GetString("IndexOutOfRange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Index variable ranges do not match array shape.
		/// </summary>
		internal static string IndexVariableRangesDoNotMatchArrayShape => ResourceManager.GetString("IndexVariableRangesDoNotMatchArrayShape", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} for parameter {1}.
		/// </summary>
		internal static string IndexWrongForParameter01 => ResourceManager.GetString("IndexWrongForParameter01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to InfeasiblePrimal due to UnboundedPrimal.
		/// </summary>
		internal static string InfeasiblePrimalDueToUnboundedPrimal => ResourceManager.GetString("InfeasiblePrimalDueToUnboundedPrimal", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Initial non-zeros A[{0}].
		/// </summary>
		internal static string InitialNonZerosA0 => ResourceManager.GetString("InitialNonZerosA0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Initial non-zeros A[{0}] Q[{1}].
		/// </summary>
		internal static string InitialNonZerosA0Q1 => ResourceManager.GetString("InitialNonZerosA0Q1", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to InOr constraint takes at least one argument.
		/// </summary>
		internal static string InOrConstraintTakesAtLeastOneArgument => ResourceManager.GetString("InOrConstraintTakesAtLeastOneArgument", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Inputs {0} and {1} have different symbol domains..
		/// </summary>
		internal static string Inputs0And1HaveDifferentSymbolDomains => ResourceManager.GetString("Inputs0And1HaveDifferentSymbolDomains", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Integer variables are not supported.
		/// </summary>
		internal static string IntegerVariablesAreNotSupported => ResourceManager.GetString("IntegerVariablesAreNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The integrality setting may not be redefined..
		/// </summary>
		internal static string IntegralityMayNotBeRedefined => ResourceManager.GetString("IntegralityMayNotBeRedefined", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} is registered with the {1} interface but that interface is incompatible with {2}.
		/// </summary>
		internal static string InterfaceIncompatibleWithProblem => ResourceManager.GetString("InterfaceIncompatibleWithProblem", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Interior Point: after reduction, this model contains no variables.
		/// </summary>
		internal static string InteriorPointAfterReductionThisModelContainsNoVariables => ResourceManager.GetString("InteriorPointAfterReductionThisModelContainsNoVariables", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Interior Point cannot load a model with no variables.
		/// </summary>
		internal static string InteriorPointCannotLoadAModelWithNoVariables => ResourceManager.GetString("InteriorPointCannotLoadAModelWithNoVariables", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Interior Point currently requires a goal.
		/// </summary>
		internal static string InteriorPointCurrentlyRequiresAGoal => ResourceManager.GetString("InteriorPointCurrentlyRequiresAGoal", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Interior point solver can't process the model: '{0}', '{1}'.
		/// </summary>
		internal static string InteriorPointSolverCanTProcessTheModel01 => ResourceManager.GetString("InteriorPointSolverCanTProcessTheModel01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to InteriorPointSolver does not support integer variables.
		/// </summary>
		internal static string InteriorPointSolverDoesNotSupportIntegerVariables => ResourceManager.GetString("InteriorPointSolverDoesNotSupportIntegerVariables", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Internal error.
		/// </summary>
		internal static string InternalError => ResourceManager.GetString("InternalError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Internal error: Bad set element.
		/// </summary>
		internal static string InternalErrorBadSetElement => ResourceManager.GetString("InternalErrorBadSetElement", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Interrupted: exit RepeatPivots {0}.
		/// </summary>
		internal static string InterruptedExitRepeatPivots0 => ResourceManager.GetString("InterruptedExitRepeatPivots0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Index Terms in Index constraints must be Integer Terms.
		/// </summary>
		internal static string IntMapNonIntegerIndex => ResourceManager.GetString("IntMapNonIntegerIndex", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid argument count for operator {0}..
		/// </summary>
		internal static string InvalidArgumentCountForOperator0 => ResourceManager.GetString("InvalidArgumentCountForOperator0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid arguments for binomial distribution.
		/// </summary>
		internal static string InvalidArgumentsForBinomialDistribution => ResourceManager.GetString("InvalidArgumentsForBinomialDistribution", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Distribution was passed an invalid numeric value..
		/// </summary>
		internal static string InvalidArgumentsForDistribution => ResourceManager.GetString("InvalidArgumentsForDistribution", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid arguments for exponential distribution.
		/// </summary>
		internal static string InvalidArgumentsForExponentialDistribution => ResourceManager.GetString("InvalidArgumentsForExponentialDistribution", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid arguments for geometric distribution.
		/// </summary>
		internal static string InvalidArgumentsForGeometricDistribution => ResourceManager.GetString("InvalidArgumentsForGeometricDistribution", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid arguments for log-normal distribution.
		/// </summary>
		internal static string InvalidArgumentsForLognormalDistribution => ResourceManager.GetString("InvalidArgumentsForLognormalDistribution", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid arguments for normal distribution.
		/// </summary>
		internal static string InvalidArgumentsForNormalDistribution => ResourceManager.GetString("InvalidArgumentsForNormalDistribution", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid arguments for scenarios distribution.
		/// </summary>
		internal static string InvalidArgumentsForScenariosDistribution => ResourceManager.GetString("InvalidArgumentsForScenariosDistribution", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid arguments for uniform distribution.
		/// </summary>
		internal static string InvalidArgumentsForUniformDistribution => ResourceManager.GetString("InvalidArgumentsForUniformDistribution", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The baseline set is not an integer set.
		/// </summary>
		internal static string InvalidBaselineSet => ResourceManager.GetString("InvalidBaselineSet", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid bounds.
		/// </summary>
		internal static string InvalidBounds => ResourceManager.GetString("InvalidBounds", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Column variable is not a CspVariable with a symbol domain but the table constraint rows have symbol values .
		/// </summary>
		internal static string InvalidColumnVar => ResourceManager.GetString("InvalidColumnVar", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invoked operations on composite variables are invalid.
		/// </summary>
		internal static string InvalidCompositeVariableOperation => ResourceManager.GetString("InvalidCompositeVariableOperation", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid cone type: {0}.
		/// </summary>
		internal static string InvalidConeType0 => ResourceManager.GetString("InvalidConeType0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid constraint encountered.
		/// </summary>
		internal static string InvalidConstraintEncountered => ResourceManager.GetString("InvalidConstraintEncountered", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid CSECTION format.
		/// </summary>
		internal static string InvalidCSectionFormat => ResourceManager.GetString("InvalidCSectionFormat", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Precision for a decimal domain must be 1, 10, 100, 1000, or 10000 .
		/// </summary>
		internal static string InvalidDecimalPrecision => ResourceManager.GetString("InvalidDecimalPrecision", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid Decision definition.
		/// </summary>
		internal static string InvalidDecisionDefinition => ResourceManager.GetString("InvalidDecisionDefinition", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid domain.
		/// </summary>
		internal static string InvalidDomain => ResourceManager.GetString("InvalidDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid domain for CP decision.
		/// </summary>
		internal static string InvalidDomainForFDVariable => ResourceManager.GetString("InvalidDomainForFDVariable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid domain for LP decision.
		/// </summary>
		internal static string InvalidDomainForLinearVariable => ResourceManager.GetString("InvalidDomainForLinearVariable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid domain for SAT decision.
		/// </summary>
		internal static string InvalidDomainForSATVariable => ResourceManager.GetString("InvalidDomainForSATVariable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid operand for Factorial .
		/// </summary>
		internal static string InvalidFactorialOperand => ResourceManager.GetString("InvalidFactorialOperand", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid Finite constraint.
		/// </summary>
		internal static string InvalidFiniteConstraint => ResourceManager.GetString("InvalidFiniteConstraint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid Finite goal.
		/// </summary>
		internal static string InvalidFiniteGoal => ResourceManager.GetString("InvalidFiniteGoal", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Internal Error: Force only applies to Boolean Terms .
		/// </summary>
		internal static string InvalidForceOperation => ResourceManager.GetString("InvalidForceOperation", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid form for Sos1 constraint.
		/// </summary>
		internal static string InvalidFormForSos1Constraint => ResourceManager.GetString("InvalidFormForSos1Constraint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid form for Sos2 constraint.
		/// </summary>
		internal static string InvalidFormForSos2Constraint => ResourceManager.GetString("InvalidFormForSos2Constraint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid index {0} for set..
		/// </summary>
		internal static string InvalidIndexForSet0 => ResourceManager.GetString("InvalidIndexForSet0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid interval..
		/// </summary>
		internal static string InvalidInterval => ResourceManager.GetString("InvalidInterval", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The lowerbound of the interval is greater than the upperbound .
		/// </summary>
		internal static string InvalidIntervalDomainDefinition => ResourceManager.GetString("InvalidIntervalDomainDefinition", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid IsElementOf detected in save.
		/// </summary>
		internal static string InvalidIsElementOfDetectedInSave => ResourceManager.GetString("InvalidIsElementOfDetectedInSave", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot call IsTrue on this Term .
		/// </summary>
		internal static string InvalidIsTrueCall => ResourceManager.GetString("InvalidIsTrueCall", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot modify an existing key .
		/// </summary>
		internal static string InvalidKeyChange => ResourceManager.GetString("InvalidKeyChange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid linear constraint.
		/// </summary>
		internal static string InvalidLinearConstraint => ResourceManager.GetString("InvalidLinearConstraint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid linear model: '{0}', '{1}'.
		/// </summary>
		internal static string InvalidLinearModel => ResourceManager.GetString("InvalidLinearModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid linear or quadratic term.
		/// </summary>
		internal static string InvalidLinearOrQuadraticTerm => ResourceManager.GetString("InvalidLinearOrQuadraticTerm", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid linear term.
		/// </summary>
		internal static string InvalidLinearTerm => ResourceManager.GetString("InvalidLinearTerm", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid list {0}..
		/// </summary>
		internal static string InvalidList0 => ResourceManager.GetString("InvalidList0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to A log event ID must be between 0 and 63 .
		/// </summary>
		internal static string InvalidLogEventId => ResourceManager.GetString("InvalidLogEventId", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid matrix dimensions..
		/// </summary>
		internal static string InvalidMatrixDimensions => ResourceManager.GetString("InvalidMatrixDimensions", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid model for solver: '{0}', '{1}'.
		/// </summary>
		internal static string InvalidModelForSolver01 => ResourceManager.GetString("InvalidModelForSolver01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to invalid number.
		/// </summary>
		internal static string InvalidNumber => ResourceManager.GetString("InvalidNumber", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid option for finite solver: '{0}'.
		/// </summary>
		internal static string InvalidOptionForFiniteSolver0 => ResourceManager.GetString("InvalidOptionForFiniteSolver0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid option for linear solver: '{0}.
		/// </summary>
		internal static string InvalidOptionForLinearSolver0 => ResourceManager.GetString("InvalidOptionForLinearSolver0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The parameter list cannot be null or empty .
		/// </summary>
		internal static string InvalidParams => ResourceManager.GetString("InvalidParams", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The permutation is invalid .
		/// </summary>
		internal static string InvalidPermutation => ResourceManager.GetString("InvalidPermutation", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid row id..
		/// </summary>
		internal static string InvalidRowId => ResourceManager.GetString("InvalidRowId", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid row list or row tuple.
		/// </summary>
		internal static string InvalidRowListOrRowTuple => ResourceManager.GetString("InvalidRowListOrRowTuple", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid sampling method {0}..
		/// </summary>
		internal static string InvalidSamplingMethod0 => ResourceManager.GetString("InvalidSamplingMethod0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid SAT constraint.
		/// </summary>
		internal static string InvalidSATConstraint => ResourceManager.GetString("InvalidSATConstraint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid SAT literal.
		/// </summary>
		internal static string InvalidSATLiteral => ResourceManager.GetString("InvalidSATLiteral", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid SAT objective.
		/// </summary>
		internal static string InvalidSATObjective => ResourceManager.GetString("InvalidSATObjective", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The given values for constructing a set either are unordered or have duplicate values .
		/// </summary>
		internal static string InvalidSetDomainDefinition => ResourceManager.GetString("InvalidSetDomainDefinition", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The solver instance is invalid .
		/// </summary>
		internal static string InvalidSolverInstance => ResourceManager.GetString("InvalidSolverInstance", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid symbol constant: this symbol does not exist in the given symbol domain .
		/// </summary>
		internal static string InvalidStringConstant => ResourceManager.GetString("InvalidStringConstant", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The given symbols for constructing a symbol set have duplicate symbols .
		/// </summary>
		internal static string InvalidSymbolDomainDefinition => ResourceManager.GetString("InvalidSymbolDomainDefinition", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to All Decisions involved in an expression must be added to the same model with AddDecision.
		/// </summary>
		internal static string InvalidTermDecisionNotInModel => ResourceManager.GetString("InvalidTermDecisionNotInModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0}: {1}.
		/// </summary>
		internal static string InvalidTermExceptionMessage => ResourceManager.GetString("InvalidTermExceptionMessage", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to All Terms, Decisions, and Parameters involved in an expression must belong to the same model.
		/// </summary>
		internal static string InvalidTermNotInModel => ResourceManager.GetString("InvalidTermNotInModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to All Parameters involved in an expression must be added to the same model with AddParameter.
		/// </summary>
		internal static string InvalidTermParameterNotInModel => ResourceManager.GetString("InvalidTermParameterNotInModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid value for 'Directive' option: '{0}'.
		/// </summary>
		internal static string InvalidValueForDirectiveOption0 => ResourceManager.GetString("InvalidValueForDirectiveOption0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid value for linear solver option {0}: '{1}'.
		/// </summary>
		internal static string InvalidValueForLinearSolverOption01 => ResourceManager.GetString("InvalidValueForLinearSolverOption01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid value for option '{0}'.
		/// </summary>
		internal static string InvalidValueForOption0 => ResourceManager.GetString("InvalidValueForOption0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid value for 'Solver' option: '{0}'.
		/// </summary>
		internal static string InvalidValueForSolverOption0 => ResourceManager.GetString("InvalidValueForSolverOption0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The type of the value is unknown .
		/// </summary>
		internal static string InvalidValueType => ResourceManager.GetString("InvalidValueType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid variable.
		/// </summary>
		internal static string InvalidVariable => ResourceManager.GetString("InvalidVariable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}' is a invalid variable..
		/// </summary>
		internal static string InvalidVariable0 => ResourceManager.GetString("InvalidVariable0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid variable id..
		/// </summary>
		internal static string InvalidVariableId => ResourceManager.GetString("InvalidVariableId", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid Decisions clause.
		/// </summary>
		internal static string InvalidVariablesClause => ResourceManager.GetString("InvalidVariablesClause", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invocation can not be used as a Set declaration.
		/// </summary>
		internal static string InvocationCannotBeUsedAsSetDeclaration => ResourceManager.GetString("InvocationCannotBeUsedAsSetDeclaration", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to IPM core solution time = {0:F2}.
		/// </summary>
		internal static string IpmCoreSolutionTime0 => ResourceManager.GetString("IpmCoreSolutionTime0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to IPM goal count has been reduced to 1.
		/// </summary>
		internal static string IPMGoalCountHasBeenReducedTo1 => ResourceManager.GetString("IPMGoalCountHasBeenReducedTo1", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Interior Point Method (IPM) solver currently supports a single goal only. 
		///             Please change the model so that it contains one goal, or use the Simplex solver..
		/// </summary>
		internal static string IPMSolverSupportOnlySingleGoal => ResourceManager.GetString("IPMSolverSupportOnlySingleGoal", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}' is not a symbol.
		/// </summary>
		internal static string IsNotASymbol => ResourceManager.GetString("IsNotASymbol", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} is not a valid iteration term.
		/// </summary>
		internal static string IsNotAValidIterationTerm => ResourceManager.GetString("IsNotAValidIterationTerm", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} is not a valid Set.
		/// </summary>
		internal static string IsNotAValidSet => ResourceManager.GetString("IsNotAValidSet", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Input {0} is not Boolean..
		/// </summary>
		internal static string IsNotBoolean => ResourceManager.GetString("IsNotBoolean", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}' is not declared as Tuples.
		/// </summary>
		internal static string IsNotDeclaredAsTuples => ResourceManager.GetString("IsNotDeclaredAsTuples", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Input {0} is not numeric..
		/// </summary>
		internal static string IsNotNumeric => ResourceManager.GetString("IsNotNumeric", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Input {0} is not a pair..
		/// </summary>
		internal static string IsNotPair => ResourceManager.GetString("IsNotPair", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} is not symbolic.
		/// </summary>
		internal static string IsNotSymbolic => ResourceManager.GetString("IsNotSymbolic", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Iteration count: {0}..
		/// </summary>
		internal static string IterationCount0 => ResourceManager.GetString("IterationCount0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Iteration limit exceeded in ReplaceRepeated[{0},{1},{2}].
		/// </summary>
		internal static string IterationLimitExceededInReplaceRepeated => ResourceManager.GetString("IterationLimitExceededInReplaceRepeated", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to  k     rp     rd     gap      k/t     mu   gamma C alpha       cx       time.
		/// </summary>
		internal static string KRpRdGapKtMuGammaCAlphaCxTime => ResourceManager.GetString("KRpRdGapKtMuGammaCAlphaCxTime", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to  k      rp         rd         rg         k/t        mu    gamma       cx.
		/// </summary>
		internal static string KRpRdRgKTMuGammaCx => ResourceManager.GetString("KRpRdRgKTMuGammaCx", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Large error in basic value: {0}.
		/// </summary>
		internal static string LargeErrorInBasicValue0 => ResourceManager.GetString("LargeErrorInBasicValue0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Length of {0} should be equal to sum of lengths of {1} and {2}.
		/// </summary>
		internal static string LenghtShouldEqualToSumOfLengths012 => ResourceManager.GetString("LenghtShouldEqualToSumOfLengths012", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Length of {0} can't be zero.
		/// </summary>
		internal static string LengthCanNotBeZero0 => ResourceManager.GetString("LengthCanNotBeZero0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Limits: NonzeroLimit = {1}, MipVariableLimit = {2}, MipRowLimit = {3}, MipNonzeroLimit = {4}, CspTermLimit = {5}, Expiration = {6}..
		/// </summary>
		internal static string LicenseFormat0123456 => ResourceManager.GetString("LicenseFormat0123456", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to List contains a non string value.
		/// </summary>
		internal static string ListContainsANonStringValue => ResourceManager.GetString("ListContainsANonStringValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to List contains a non numeric value.
		/// </summary>
		internal static string ListContainsNonNumericValue => ResourceManager.GetString("ListContainsNonNumericValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to List did not contain Rules.
		/// </summary>
		internal static string ListDidNotContainRules => ResourceManager.GetString("ListDidNotContainRules", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to List expected to be entirely symbols.
		/// </summary>
		internal static string ListExpectedToBeEntirelySymbols => ResourceManager.GetString("ListExpectedToBeEntirelySymbols", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to List must have at least one element.
		/// </summary>
		internal static string ListMustHaveAtLeastOneElement => ResourceManager.GetString("ListMustHaveAtLeastOneElement", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Lists cannot mix symbols with literals.
		/// </summary>
		internal static string ListsCannotMixSymbolsWithLiterals => ResourceManager.GetString("ListsCannotMixSymbolsWithLiterals", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Loading of MPS files containing special ordered sets is not supported..
		/// </summary>
		internal static string LoadingOfMPSFilesContainingSpecialOrderedSetsIsNotSupported => ResourceManager.GetString("LoadingOfMPSFilesContainingSpecialOrderedSetsIsNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to LoadLinearModel passed 'this'!.
		/// </summary>
		internal static string LoadLinearModelPassedThis => ResourceManager.GetString("LoadLinearModelPassedThis", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Computational Progress of HSD Solver:.
		/// </summary>
		internal static string LogComputationalProgressOfHSDSolver => ResourceManager.GetString("LogComputationalProgressOfHSDSolver", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Logical Symbol {0} could not be made a Decision.
		/// </summary>
		internal static string LogicalSymbolCouldNotBeMadeAVariable => ResourceManager.GetString("LogicalSymbolCouldNotBeMadeAVariable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Lower bound cannot be larger than upper bound.
		/// </summary>
		internal static string LowerBoundCannotBeLargerThanUpperBound => ResourceManager.GetString("LowerBoundCannotBeLargerThanUpperBound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to lower triangular coordinate seen.
		/// </summary>
		internal static string LowerTriangularCoordinateSeen => ResourceManager.GetString("LowerTriangularCoordinateSeen", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Map (array indexing) size limit of {0} elements exceeded.
		/// </summary>
		internal static string MapArrayIndexingSizeLimitExceeded => ResourceManager.GetString("MapArrayIndexingSizeLimitExceeded", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Mapping of stochastic model is not supported.
		/// </summary>
		internal static string MappingOfStochasticModelIsNotSupported => ResourceManager.GetString("MappingOfStochasticModelIsNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Matrix dimensions are not valid..
		/// </summary>
		internal static string MatrixDimensionsAreNotValid => ResourceManager.GetString("MatrixDimensionsAreNotValid", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Matrix dimensions do not match..
		/// </summary>
		internal static string MatrixDimensionsDoNotMatch => ResourceManager.GetString("MatrixDimensionsDoNotMatch", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Matrix is not positive definite..
		/// </summary>
		internal static string MatrixIsNotPositiveDefinite => ResourceManager.GetString("MatrixIsNotPositiveDefinite", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Matrix is not square..
		/// </summary>
		internal static string MatrixIsNotSquare => ResourceManager.GetString("MatrixIsNotSquare", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Matrix is not symmetric..
		/// </summary>
		internal static string MatrixIsNotSymmetric => ResourceManager.GetString("MatrixIsNotSymmetric", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Matrix is rank deficient..
		/// </summary>
		internal static string MatrixIsRankDeficient => ResourceManager.GetString("MatrixIsRankDeficient", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Matrix is singular.
		/// </summary>
		internal static string MatrixIsSingular => ResourceManager.GetString("MatrixIsSingular", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Matrix row dimensions must agree..
		/// </summary>
		internal static string MatrixRowDimensionsMustAgree => ResourceManager.GetString("MatrixRowDimensionsMustAgree", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Maximize problem.
		/// </summary>
		internal static string MaximizeProblem => ResourceManager.GetString("MaximizeProblem", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Maximum iterations exceeded.
		/// </summary>
		internal static string MaximumIterationsExceeded => ResourceManager.GetString("MaximumIterationsExceeded", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Maximum number of iterations should be positive.
		/// </summary>
		internal static string MaximumNumberOfIterationsShouldBePositive => ResourceManager.GetString("MaximumNumberOfIterationsShouldBePositive", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Maximum time exceeded.
		/// </summary>
		internal static string MaximumTimeExceeded => ResourceManager.GetString("MaximumTimeExceeded", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Microsoft Solver Foundation {0} {1} Edition.
		/// </summary>
		internal static string MicrosoftSolverFoundationVersion01 => ResourceManager.GetString("MicrosoftSolverFoundationVersion01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Minimize problem.
		/// </summary>
		internal static string MinimizeProblem => ResourceManager.GetString("MinimizeProblem", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Minimum bound can not be bigger than maximum bound.
		/// </summary>
		internal static string MinimumBundaryBiggerThanMaximumBundary => ResourceManager.GetString("MinimumBundaryBiggerThanMaximumBundary", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to After rounding, the minimum bound {0} is bigger than the maximum bound {1}.
		/// </summary>
		internal static string MinimumBundaryBiggerThanMaximumBundaryAfterRound01 => ResourceManager.GetString("MinimumBundaryBiggerThanMaximumBundaryAfterRound01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to minimum degree key unmatched.
		/// </summary>
		internal static string MinimumDegreeKeyUnmatched => ResourceManager.GetString("MinimumDegreeKeyUnmatched", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Missing parameter value: {0}({1}).
		/// </summary>
		internal static string MissingParameterValue => ResourceManager.GetString("MissingParameterValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Mixed boolean and integer types unexpected in argument list.
		/// </summary>
		internal static string MixedBooleanAndIntegerTypesUnexpectedInArgumentList => ResourceManager.GetString("MixedBooleanAndIntegerTypesUnexpectedInArgumentList", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Model cannot be analyzed.
		/// </summary>
		internal static string ModelCannotBeAnalyzed => ResourceManager.GetString("ModelCannotBeAnalyzed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to model could not be null.
		/// </summary>
		internal static string ModelCouldNotBeNull => ResourceManager.GetString("ModelCouldNotBeNull", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Model is infeasible or unknown. Cannot access ConstraintSolverSolution.GetValue.
		/// </summary>
		internal static string ModelHasNoSolution => ResourceManager.GetString("ModelHasNoSolution", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The operation is invalid because the model has not been differentiated..
		/// </summary>
		internal static string ModelHasNotBeenDifferentiated => ResourceManager.GetString("ModelHasNotBeenDifferentiated", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Model is not stochastic, but a stochastic directive was used.
		/// </summary>
		internal static string ModelIsNotStochasticButStochasticDirectiveWasUsed => ResourceManager.GetString("ModelIsNotStochasticButStochasticDirectiveWasUsed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Model is stochastic, but a non-stochastic directive was used.
		/// </summary>
		internal static string ModelIsStochasticButNonStochasticDirectiveWasUsed => ResourceManager.GetString("ModelIsStochasticButNonStochasticDirectiveWasUsed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The model is not convex..
		/// </summary>
		internal static string ModelNotConvex => ResourceManager.GetString("ModelNotConvex", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Model is not solved yet. Cannot retrieve decisions' values.
		/// </summary>
		internal static string ModelNotSolved => ResourceManager.GetString("ModelNotSolved", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Model reduction: {0} unbounded rows removed, {1} constant or unbounded slack variables removed.
		/// </summary>
		internal static string ModelReduction0UnboundedRowsRemoved1ConstantOrUnboundedSlackVariablesRemoved => ResourceManager.GetString("ModelReduction0UnboundedRowsRemoved1ConstantOrUnboundedSlackVariablesRemoved", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Models cannot be shared across SolverContext instances. Please use the SolverContext instance that created the Model..
		/// </summary>
		internal static string ModelsCannotBeSharedAcrossSolverContextInstances => ResourceManager.GetString("ModelsCannotBeSharedAcrossSolverContextInstances", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Model should contain at least one 'Decisions' section.
		/// </summary>
		internal static string ModelShouldContainAtLeastOneDecisionsSection => ResourceManager.GetString("ModelShouldContainAtLeastOneDecisionsSection", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Model should not be edited while enumerating rows or variables..
		/// </summary>
		internal static string ModelShouldNotBeEditedWhileEnumeratingRowsOrVariables => ResourceManager.GetString("ModelShouldNotBeEditedWhileEnumeratingRowsOrVariables", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Model should start with \"Model[\" and end with \"]\" symbol.
		/// </summary>
		internal static string ModelShouldStartWithModelAndEndWithSymbol => ResourceManager.GetString("ModelShouldStartWithModelAndEndWithSymbol", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Model size limit has been exceeded for this version of the product. Please contact Microsoft Corporation for licensing options..
		/// </summary>
		internal static string ModelSizeLimitHasBeenExceed => ResourceManager.GetString("ModelSizeLimitHasBeenExceed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Given model is too large and cannot be handled.
		/// </summary>
		internal static string ModelTooLarge => ResourceManager.GetString("ModelTooLarge", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Mps expects a string (the file name), optionally followed by a boolean (True for fixed format, False for free)..
		/// </summary>
		internal static string MpsExpectsAStringTheFileNameOptionallyFollowedByABooleanTrueForFixedFormatFalseForFree => ResourceManager.GetString("MpsExpectsAStringTheFileNameOptionallyFollowedByABooleanTrueForFixedFormatFalseForFree", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0}ms
		///             .
		/// </summary>
		internal static string Ms => ResourceManager.GetString("Ms", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Multicorrector: ineffective.
		/// </summary>
		internal static string MulticorrectorIneffective => ResourceManager.GetString("MulticorrectorIneffective", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to multiple occurrence of a symbol in SymbolSet.
		/// </summary>
		internal static string MultipleOccurrenceOfASymbolInSymbolSet => ResourceManager.GetString("MultipleOccurrenceOfASymbolInSymbolSet", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} must apply to a single argument.
		/// </summary>
		internal static string MustApplyToASingleArgument => ResourceManager.GetString("MustApplyToASingleArgument", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} must apply to two arguments.
		/// </summary>
		internal static string MustApplyToTwoArguments => ResourceManager.GetString("MustApplyToTwoArguments", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Must be a list of integer values.
		/// </summary>
		internal static string MustBeAListOfIntegerValues => ResourceManager.GetString("MustBeAListOfIntegerValues", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Must be a tuple of integer values that are within Finite Solver's integer range.
		/// </summary>
		internal static string MustBeATupleOfIntegerValuesThatAreWithinFiniteSolverSIntegerRange => ResourceManager.GetString("MustBeATupleOfIntegerValuesThatAreWithinFiniteSolverSIntegerRange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} must have a single value..
		/// </summary>
		internal static string MustHaveASingleValue => ResourceManager.GetString("MustHaveASingleValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} must have at least one element.
		/// </summary>
		internal static string MustHaveAtLeastOneElement => ResourceManager.GetString("MustHaveAtLeastOneElement", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} must have at least one value.
		/// </summary>
		internal static string MustHaveAtLeastOneValue => ResourceManager.GetString("MustHaveAtLeastOneValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Name found in Model was not presented as a Decision.
		/// </summary>
		internal static string NameFoundInModelWasNotPresentedAsAVariable => ResourceManager.GetString("NameFoundInModelWasNotPresentedAsAVariable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Name is already used as a domain.
		/// </summary>
		internal static string NameIsAlreadyUsedAsADomain => ResourceManager.GetString("NameIsAlreadyUsedAsADomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} needs a simplex solver..
		/// </summary>
		internal static string NeedsASimplexSolver => ResourceManager.GetString("NeedsASimplexSolver", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} needs a simplex solver, followed by any number of rules mapping {row, var} to value..
		/// </summary>
		internal static string NeedsASimplexSolverFollowedByAnyNumberOfRulesMappingRowVarToValue => ResourceManager.GetString("NeedsASimplexSolverFollowedByAnyNumberOfRulesMappingRowVarToValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} needs a simplex solver, followed by any number of rules mapping variables/labels to values..
		/// </summary>
		internal static string NeedsASimplexSolverFollowedByAnyNumberOfRulesMappingVariablesLabelsToValues => ResourceManager.GetString("NeedsASimplexSolverFollowedByAnyNumberOfRulesMappingVariablesLabelsToValues", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} needs a simplex solver, optionally followed by {label, var} pairs..
		/// </summary>
		internal static string NeedsASimplexSolverOptionallyFollowedByLabelVarPairs => ResourceManager.GetString("NeedsASimplexSolverOptionallyFollowedByLabelVarPairs", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} needs a simplex solver, optionally followed by statistic names..
		/// </summary>
		internal static string NeedsASimplexSolverOptionallyFollowedByStatisticNames => ResourceManager.GetString("NeedsASimplexSolverOptionallyFollowedByStatisticNames", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} needs a simplex solver, optionally followed by variables/labels..
		/// </summary>
		internal static string NeedsASimplexSolverOptionallyFollowedByVariablesLabels => ResourceManager.GetString("NeedsASimplexSolverOptionallyFollowedByVariablesLabels", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} needs a solver with sensitivity information..
		/// </summary>
		internal static string NeedsASolverWithSensitivityInformation => ResourceManager.GetString("NeedsASolverWithSensitivityInformation", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} needs at least 2 arguments.
		/// </summary>
		internal static string NeedsAtLeast2Arguments => ResourceManager.GetString("NeedsAtLeast2Arguments", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to needs to use dual for sensitivity report.
		/// </summary>
		internal static string NeedsToUseDualForSensitivityRepor => ResourceManager.GetString("NeedsToUseDualForSensitivityRepor", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Negation should have 1 argument.
		/// </summary>
		internal static string NegationShouldHave1Argument => ResourceManager.GetString("NegationShouldHave1Argument", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Negative index in {0}[{1}].
		/// </summary>
		internal static string NegativeIndex01 => ResourceManager.GetString("NegativeIndex01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot take negative power in ConstraintSolver.Power.
		/// </summary>
		internal static string NegativePower => ResourceManager.GetString("NegativePower", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to NelderMeadSolver requires the FunctionEvaluator property to be specified before calling solve..
		/// </summary>
		internal static string NmSolverRequiresEvaluatorToBeSpecifiedBeforeCallingSolve => ResourceManager.GetString("NmSolverRequiresEvaluatorToBeSpecifiedBeforeCallingSolve", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to There is no arithmetic object for this number type .
		/// </summary>
		internal static string NoArithmeticObjectForType => ResourceManager.GetString("NoArithmeticObjectForType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The model cannot be changed before Solve completes .
		/// </summary>
		internal static string NoChangesBeforeSolveComplete => ResourceManager.GetString("NoChangesBeforeSolveComplete", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to No cone type specified..
		/// </summary>
		internal static string NoConeTypeSpecified => ResourceManager.GetString("NoConeTypeSpecified", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to No data binding set on {0}.
		/// </summary>
		internal static string NoDataBindingSetOn0 => ResourceManager.GetString("NoDataBindingSetOn0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to No derivative exists in row {0} for variable {1}..
		/// </summary>
		internal static string NoDerivativeExists01 => ResourceManager.GetString("NoDerivativeExists01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0,-10} nodes searched in {1}ms.
		/// </summary>
		internal static string NodesSearchedIn1Ms => ResourceManager.GetString("NodesSearchedIn1Ms", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to No Domain.ValueKind specified.
		/// </summary>
		internal static string NoDomainValueKindSpecified => ResourceManager.GetString("NoDomainValueKindSpecified", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to none.
		/// </summary>
		internal static string NoExpiration => ResourceManager.GetString("NoExpiration", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to A constraint must be Boolean .
		/// </summary>
		internal static string NonBooleanConstraints => ResourceManager.GetString("NonBooleanConstraints", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Non-boolean expression attempted as constraint.
		/// </summary>
		internal static string NonBooleanExpressionAttemptedAsConstraint => ResourceManager.GetString("NonBooleanExpressionAttemptedAsConstraint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Non-Boolean term appears where a Boolean term is expected.
		/// </summary>
		internal static string NonBooleanInputs => ResourceManager.GetString("NonBooleanInputs", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Non-boolean variable attempted as constraint.
		/// </summary>
		internal static string NonBooleanVariableAttemptedAsConstraint => ResourceManager.GetString("NonBooleanVariableAttemptedAsConstraint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The field method is only valid for composite variables .
		/// </summary>
		internal static string NonCompositeFieldAccess => ResourceManager.GetString("NonCompositeFieldAccess", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to None of the supplied directives was valid for the model.
		/// </summary>
		internal static string NoneOfTheSuppliedDirectivesWasValidForTheModel => ResourceManager.GetString("NoneOfTheSuppliedDirectivesWasValidForTheModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot add new fields to CspPowerSet or CspPowerList composite.
		/// </summary>
		internal static string NoNewFieldsForSetListComposites => ResourceManager.GetString("NoNewFieldsForSetListComposites", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to non-integer domains in CSP problem is not yet supported in OML.
		/// </summary>
		internal static string NonIntegerDomainsInCSPProblemIsNotYetSupportedInOML => ResourceManager.GetString("NonIntegerDomainsInCSPProblemIsNotYetSupportedInOML", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Nonlinear term.
		/// </summary>
		internal static string NonlinearTerm => ResourceManager.GetString("NonlinearTerm", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Nonlinear value.
		/// </summary>
		internal static string NonlinearValue => ResourceManager.GetString("NonlinearValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to no report type.
		/// </summary>
		internal static string NoReportType => ResourceManager.GetString("NoReportType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to No solver could be found that can accept the model given the model type and directive(s).
		/// </summary>
		internal static string NoSolverFound => ResourceManager.GetString("NoSolverFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to No solver found a solution within the time limit..
		/// </summary>
		internal static string NoSolverFoundASolutionWithinTheTimeLimit => ResourceManager.GetString("NoSolverFoundASolutionWithinTheTimeLimit", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {1} cannot solve {0} models. Remove this directive and use a directive that supports the {0} capability..
		/// </summary>
		internal static string NoSolverWithCapabilityForDirectiveWithType01 => ResourceManager.GetString("NoSolverWithCapabilityForDirectiveWithType01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to No such set.
		/// </summary>
		internal static string NoSuchSet => ResourceManager.GetString("NoSuchSet", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Not a linear operation.
		/// </summary>
		internal static string NotALinearOperation => ResourceManager.GetString("NotALinearOperation", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to not a row.
		/// </summary>
		internal static string NotARow => ResourceManager.GetString("NotARow", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot convert the Term to Boolean Term .
		/// </summary>
		internal static string NotBoolean => ResourceManager.GetString("NotBoolean", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The model is not a LP/MILP/QP model. Make sure the model does not contain any nonlinear constraints or strict inequalities, or use a directive that supports this model type..
		/// </summary>
		internal static string NotLpMilpQpModel => ResourceManager.GetString("NotLpMilpQpModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Not supported random type.
		/// </summary>
		internal static string NotSupportedRandomType => ResourceManager.GetString("NotSupportedRandomType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to not triangular.
		/// </summary>
		internal static string NotTriangular => ResourceManager.GetString("NotTriangular", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Not valid index (bigger than number of {0}) in {1}[{2}].
		/// </summary>
		internal static string NotValidIndex012 => ResourceManager.GetString("NotValidIndex012", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to not yet implemented.
		/// </summary>
		internal static string NotYetImplemented => ResourceManager.GetString("NotYetImplemented", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to No vid for the decision {0}.
		/// </summary>
		internal static string NoVidForTheDecision0 => ResourceManager.GetString("NoVidForTheDecision0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to No vid for the goal {0}.
		/// </summary>
		internal static string NoVidForTheGoal0 => ResourceManager.GetString("NoVidForTheGoal0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot add null constraints..
		/// </summary>
		internal static string NullConstraints => ResourceManager.GetString("NullConstraints", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The domain for a decision cannot be null..
		/// </summary>
		internal static string NullDomain => ResourceManager.GetString("NullDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot add null goals..
		/// </summary>
		internal static string NullGoals => ResourceManager.GetString("NullGoals", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The input cannot be null .
		/// </summary>
		internal static string NullInput => ResourceManager.GetString("NullInput", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number not integer, or out of range.
		/// </summary>
		internal static string NumberNotIntegerOrOutOfRange => ResourceManager.GetString("NumberNotIntegerOrOutOfRange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number of elements in tuple does not match.
		/// </summary>
		internal static string NumberOfElementsInTupleDoesNotMatch => ResourceManager.GetString("NumberOfElementsInTupleDoesNotMatch", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number of evaluation calls: {0}.
		/// </summary>
		internal static string NumberOfEvaluationCalls0 => ResourceManager.GetString("NumberOfEvaluationCalls0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number of iterations performed: {0}.
		/// </summary>
		internal static string NumberOfIterationsPerformed0 => ResourceManager.GetString("NumberOfIterationsPerformed0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number of iterations to remember should be positive.
		/// </summary>
		internal static string NumberOfIterationsToRememberShouldBePositive => ResourceManager.GetString("NumberOfIterationsToRememberShouldBePositive", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number of parameters for Decision is wrong.
		/// </summary>
		internal static string NumberOfParametersForDecisionIsWrong => ResourceManager.GetString("NumberOfParametersForDecisionIsWrong", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number of parameters for Decision '{0}' is wrong.
		/// </summary>
		internal static string NumberOfParametersForDecisionIsWrong0 => ResourceManager.GetString("NumberOfParametersForDecisionIsWrong0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number of variables should be positive.
		/// </summary>
		internal static string NumberOfVariablesShouldBePositive => ResourceManager.GetString("NumberOfVariablesShouldBePositive", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Objective label already exists..
		/// </summary>
		internal static string ObjectiveLabelAlreadyExists => ResourceManager.GetString("ObjectiveLabelAlreadyExists", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number of re-initialization: {0}.
		/// </summary>
		internal static string OfReInitialization0 => ResourceManager.GetString("OfReInitialization0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number of re-initialization due to cuts: {0}.
		/// </summary>
		internal static string OfReInitializationDueToCuts0 => ResourceManager.GetString("OfReInitializationDueToCuts0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Number of re-initialization due to nodes: {0}.
		/// </summary>
		internal static string OfReInitializationDueToNodes0 => ResourceManager.GetString("OfReInitializationDueToNodes0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Decision.
		/// </summary>
		internal static string OmlDecision => ResourceManager.GetString("OmlDecision", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Decisions can not be assigned.
		/// </summary>
		internal static string OmlInvalidAssignedDecision => ResourceManager.GetString("OmlInvalidAssignedDecision", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Set parameters cannot be assigned values..
		/// </summary>
		internal static string OmlInvalidAssignmentToSetsParameter => ResourceManager.GetString("OmlInvalidAssignmentToSetsParameter", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The operator '{0}' is not supported in OML..
		/// </summary>
		internal static string OmlInvalidBadBuiltinOperator => ResourceManager.GetString("OmlInvalidBadBuiltinOperator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The domain '{0}' is not recognized..
		/// </summary>
		internal static string OmlInvalidBadDomain => ResourceManager.GetString("OmlInvalidBadDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid domain format..
		/// </summary>
		internal static string OmlInvalidBadDomainBounds => ResourceManager.GetString("OmlInvalidBadDomainBounds", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Domain bound is not of the correct type..
		/// </summary>
		internal static string OmlInvalidBadDomainBoundType => ResourceManager.GetString("OmlInvalidBadDomainBoundType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Domain bound '{0}' is not of the correct type..
		/// </summary>
		internal static string OmlInvalidBadDomainBoundType0 => ResourceManager.GetString("OmlInvalidBadDomainBoundType0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The expression '{0}' is not recognized as a value or builtin operator..
		/// </summary>
		internal static string OmlInvalidBadExpr => ResourceManager.GetString("OmlInvalidBadExpr", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Incorrect iterator format for '{0}'..
		/// </summary>
		internal static string OmlInvalidBadIterator => ResourceManager.GetString("OmlInvalidBadIterator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid use of '{0}'..
		/// </summary>
		internal static string OmlInvalidBadSetSpecifier => ResourceManager.GetString("OmlInvalidBadSetSpecifier", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expected a Tuple here..
		/// </summary>
		internal static string OmlInvalidBadTuple => ResourceManager.GetString("OmlInvalidBadTuple", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expected a Tuple here. '{0}' is not a Tuple..
		/// </summary>
		internal static string OmlInvalidBadTuple0 => ResourceManager.GetString("OmlInvalidBadTuple0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Builtin symbol '{0}' cannot be used as an iterator..
		/// </summary>
		internal static string OmlInvalidBuiltinUsedAsIterator => ResourceManager.GetString("OmlInvalidBuiltinUsedAsIterator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only Boolean values are allowed as constraints..
		/// </summary>
		internal static string OmlInvalidConstraintNotBoolean => ResourceManager.GetString("OmlInvalidConstraintNotBoolean", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}' Constraint is wrong. Only Boolean values are allowed as constraints..
		/// </summary>
		internal static string OmlInvalidConstraintNotBoolean0 => ResourceManager.GetString("OmlInvalidConstraintNotBoolean0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Domain bounds may not depend on decision values..
		/// </summary>
		internal static string OmlInvalidDecisionInDomainBound => ResourceManager.GetString("OmlInvalidDecisionInDomainBound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Domain bound '{0}' may not depend on decision values..
		/// </summary>
		internal static string OmlInvalidDecisionInDomainBound0 => ResourceManager.GetString("OmlInvalidDecisionInDomainBound0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to A condition for '{0}' may not be dependent on decision values..
		/// </summary>
		internal static string OmlInvalidDecisionInFilter => ResourceManager.GetString("OmlInvalidDecisionInFilter", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Values used in an iterator cannot depend on decision values..
		/// </summary>
		internal static string OmlInvalidDecisionInIteratorList => ResourceManager.GetString("OmlInvalidDecisionInIteratorList", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Value '{0}' is used in an iterator. Values used in an iterator cannot depend on decision values..
		/// </summary>
		internal static string OmlInvalidDecisionInIteratorList0 => ResourceManager.GetString("OmlInvalidDecisionInIteratorList0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Index for '{0}' may not be used without indexing..
		/// </summary>
		internal static string OmlInvalidDeclaredIndexIsTable => ResourceManager.GetString("OmlInvalidDeclaredIndexIsTable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Index for '{0}' may not depend on a parameter value..
		/// </summary>
		internal static string OmlInvalidDeclaredIndexUsesData => ResourceManager.GetString("OmlInvalidDeclaredIndexUsesData", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Index for '{0}' may not depend on a decision value..
		/// </summary>
		internal static string OmlInvalidDeclaredIndexUsesDecision => ResourceManager.GetString("OmlInvalidDeclaredIndexUsesDecision", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Index for '{0}' must be single-valued..
		/// </summary>
		internal static string OmlInvalidDeclaredIndexUsesForeach => ResourceManager.GetString("OmlInvalidDeclaredIndexUsesForeach", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Builtin symbol '{0}' cannot be redefined..
		/// </summary>
		internal static string OmlInvalidDefiningBuiltin => ResourceManager.GetString("OmlInvalidDefiningBuiltin", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Builtin symbol '{0}' cannot be used as a set..
		/// </summary>
		internal static string OmlInvalidDefiningBuiltinAsSet => ResourceManager.GetString("OmlInvalidDefiningBuiltinAsSet", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Symbol {0} has been defined. Cannot redefine it in the current Model[] section..
		/// </summary>
		internal static string OmlInvalidDefiningDuplicatedSymbols => ResourceManager.GetString("OmlInvalidDefiningDuplicatedSymbols", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Each list in a Tuples assignment must contain the same number of elements..
		/// </summary>
		internal static string OmlInvalidEachListMustContainSameNumberOfElements => ResourceManager.GetString("OmlInvalidEachListMustContainSameNumberOfElements", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The operator '{0}' requires at least {1} argument(s)..
		/// </summary>
		internal static string OmlInvalidFewerThanNeededArgumentCount01 => ResourceManager.GetString("OmlInvalidFewerThanNeededArgumentCount01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to A condition for '{0}' must be a Boolean value..
		/// </summary>
		internal static string OmlInvalidFilterIsNotBoolean => ResourceManager.GetString("OmlInvalidFilterIsNotBoolean", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Domain bound must be single-valued..
		/// </summary>
		internal static string OmlInvalidForeachInDomainBound => ResourceManager.GetString("OmlInvalidForeachInDomainBound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Domain bound '{0}' must be single-valued..
		/// </summary>
		internal static string OmlInvalidForeachInDomainBound0 => ResourceManager.GetString("OmlInvalidForeachInDomainBound0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to A condition for '{0}' must be single-valued..
		/// </summary>
		internal static string OmlInvalidForeachInFilter => ResourceManager.GetString("OmlInvalidForeachInFilter", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Values used as iterator bounds must be single-valued..
		/// </summary>
		internal static string OmlInvalidForeachInIterator => ResourceManager.GetString("OmlInvalidForeachInIterator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}' is not allowed in a Parameters section.
		/// </summary>
		internal static string OmlInvalidForeachInParameter => ResourceManager.GetString("OmlInvalidForeachInParameter", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The operator '{0}' only accepts singled-value arguments..
		/// </summary>
		internal static string OmlInvalidForeachNotAllowedInOperator => ResourceManager.GetString("OmlInvalidForeachNotAllowedInOperator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only numeric values are allowed as goals..
		/// </summary>
		internal static string OmlInvalidGoalNotNumeric => ResourceManager.GetString("OmlInvalidGoalNotNumeric", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}' Goal is wrong. Only numeric values are allowed as goals..
		/// </summary>
		internal static string OmlInvalidGoalNotNumeric0 => ResourceManager.GetString("OmlInvalidGoalNotNumeric0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The operator '{0}' must have all arguments of the same type..
		/// </summary>
		internal static string OmlInvalidIncompatibleArgument => ResourceManager.GetString("OmlInvalidIncompatibleArgument", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Arguments for indexed random parameters must be specified through data binding..
		/// </summary>
		internal static string OmlInvalidIndexedRandomParameters => ResourceManager.GetString("OmlInvalidIndexedRandomParameters", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Set parameters cannot be indexed..
		/// </summary>
		internal static string OmlInvalidIndexedSetsParameter => ResourceManager.GetString("OmlInvalidIndexedSetsParameter", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Value used as index must be single-valued..
		/// </summary>
		internal static string OmlInvalidIndexingByForeach => ResourceManager.GetString("OmlInvalidIndexingByForeach", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Value '{0}' is used as index. Value used as index must be single-valued..
		/// </summary>
		internal static string OmlInvalidIndexingByForeach0 => ResourceManager.GetString("OmlInvalidIndexingByForeach0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Value used as index is not a valid type..
		/// </summary>
		internal static string OmlInvalidIndexingByTable => ResourceManager.GetString("OmlInvalidIndexingByTable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Value '{0}' is used as index. Value used as index is not a valid type..
		/// </summary>
		internal static string OmlInvalidIndexingByTable0 => ResourceManager.GetString("OmlInvalidIndexingByTable0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The value {0} cannot be indexed..
		/// </summary>
		internal static string OmlInvalidIndexOfNonTable0 => ResourceManager.GetString("OmlInvalidIndexOfNonTable0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid assignment format..
		/// </summary>
		internal static string OmlInvalidInvalidAssignmentFormat => ResourceManager.GetString("OmlInvalidInvalidAssignmentFormat", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid element..
		/// </summary>
		internal static string OmlInvalidInvalidElement => ResourceManager.GetString("OmlInvalidInvalidElement", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}' element is invalid..
		/// </summary>
		internal static string OmlInvalidInvalidElement0 => ResourceManager.GetString("OmlInvalidInvalidElement0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Sos1 constraint must contain a sum of products.
		/// </summary>
		internal static string OmlInvalidInvalidSos1 => ResourceManager.GetString("OmlInvalidInvalidSos1", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Sos2 must contain a reference constraint..
		/// </summary>
		internal static string OmlInvalidInvalidSos2 => ResourceManager.GetString("OmlInvalidInvalidSos2", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Sos2 must contain a reference constraint as an equality..
		/// </summary>
		internal static string OmlInvalidInvalidSos2NeedEquality => ResourceManager.GetString("OmlInvalidInvalidSos2NeedEquality", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Sos2 must contain a reference constraint as an equality with exactly two arguments..
		/// </summary>
		internal static string OmlInvalidInvalidSos2NeedEqualityWithTwoArguments => ResourceManager.GetString("OmlInvalidInvalidSos2NeedEqualityWithTwoArguments", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid label to goal or constraint.
		/// </summary>
		internal static string OmlInvalidLabel => ResourceManager.GetString("OmlInvalidLabel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only assignment parameters are allowed inside 'Foreach' or 'ForeachWhere'..
		/// </summary>
		internal static string OmlInvalidNonAssignmentParameterInForeach => ResourceManager.GetString("OmlInvalidNonAssignmentParameterInForeach", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Logical or relational operators only accept Boolean arguments..
		/// </summary>
		internal static string OmlInvalidNonBooleanArgument => ResourceManager.GetString("OmlInvalidNonBooleanArgument", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Arithmetic operators only accept numeric arguments..
		/// </summary>
		internal static string OmlInvalidNonNumericArgument => ResourceManager.GetString("OmlInvalidNonNumericArgument", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Values used as iterator bounds must be numeric..
		/// </summary>
		internal static string OmlInvalidNonNumericInIterator => ResourceManager.GetString("OmlInvalidNonNumericInIterator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Domain bounds may not depend on late-bound data..
		/// </summary>
		internal static string OmlInvalidParameterInDomainBound => ResourceManager.GetString("OmlInvalidParameterInDomainBound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Domain bound '{0}' may not depend on late-bound data..
		/// </summary>
		internal static string OmlInvalidParameterInDomainBound0 => ResourceManager.GetString("OmlInvalidParameterInDomainBound0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Parameter '{0}' must be either assigned a value ('=') or indexed('[]')..
		/// </summary>
		internal static string OmlInvalidParameterNotAssignmentOrIndex0 => ResourceManager.GetString("OmlInvalidParameterNotAssignmentOrIndex0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid recourse decision name..
		/// </summary>
		internal static string OmlInvalidRecourseDecision => ResourceManager.GetString("OmlInvalidRecourseDecision", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The symbol '{0}' is already used as an iterator in an enclosing scope..
		/// </summary>
		internal static string OmlInvalidRedefiningIterator => ResourceManager.GetString("OmlInvalidRedefiningIterator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to All Parameters sections should start with a domain and all Decisions sections should start with a domain or a submodel..
		/// </summary>
		internal static string OmlInvalidSectionShouldStartWithDomain => ResourceManager.GetString("OmlInvalidSectionShouldStartWithDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}' is not a domain nor a submodel. All Parameters sections should start with a domain and all Decisions sections should start with a domain or a submodel..
		/// </summary>
		internal static string OmlInvalidSectionShouldStartWithDomain0 => ResourceManager.GetString("OmlInvalidSectionShouldStartWithDomain0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}' is not allowed outside the Parameters section..
		/// </summary>
		internal static string OmlInvalidSetsOutsideParametersSection => ResourceManager.GetString("OmlInvalidSetsOutsideParametersSection", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The expression was too large to validate..
		/// </summary>
		internal static string OmlInvalidStackOverflow => ResourceManager.GetString("OmlInvalidStackOverflow", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Submodel must be of the form: name -&gt; Model[...], where name is a single non-quoted symbol.
		/// </summary>
		internal static string OmlInvalidSubmodelClause => ResourceManager.GetString("OmlInvalidSubmodelClause", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Submodel, goal and constraint names cannot be a predefined symbol..
		/// </summary>
		internal static string OmlInvalidSubmodelGoalConstraintName => ResourceManager.GetString("OmlInvalidSubmodelGoalConstraintName", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown or duplicated submodel instance symbol.
		/// </summary>
		internal static string OmlInvalidSubmodelInstanceSymbolUnknownOrDuplicated => ResourceManager.GetString("OmlInvalidSubmodelInstanceSymbolUnknownOrDuplicated", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Submodel name has been taken.
		/// </summary>
		internal static string OmlInvalidSubmodelNameHasBeenTaken => ResourceManager.GetString("OmlInvalidSubmodelNameHasBeenTaken", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Submodel name is also declared as a named domain.
		/// </summary>
		internal static string OmlInvalidSubmodelNameIsAlsoDeclaredAsANamedDomain => ResourceManager.GetString("OmlInvalidSubmodelNameIsAlsoDeclaredAsANamedDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The symbol '{0}' is already used as a decision or parameter..
		/// </summary>
		internal static string OmlInvalidSymbolRedefined => ResourceManager.GetString("OmlInvalidSymbolRedefined", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The symbol '{0}' is already used as a set..
		/// </summary>
		internal static string OmlInvalidSymbolRedefinedAsSet => ResourceManager.GetString("OmlInvalidSymbolRedefinedAsSet", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The operator '{0}' cannot accept the term without indexing..
		/// </summary>
		internal static string OmlInvalidTableArgument => ResourceManager.GetString("OmlInvalidTableArgument", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to This term cannot be used in an iterator without indexing..
		/// </summary>
		internal static string OmlInvalidTableInIteratorList => ResourceManager.GetString("OmlInvalidTableInIteratorList", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The term '{0}' cannot be used in an iterator without indexing..
		/// </summary>
		internal static string OmlInvalidTableInIteratorList0 => ResourceManager.GetString("OmlInvalidTableInIteratorList0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuple element may not depend on a decision value..
		/// </summary>
		internal static string OmlInvalidTupleContainsDecision => ResourceManager.GetString("OmlInvalidTupleContainsDecision", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuple element '{0}' may not depend on a decision value..
		/// </summary>
		internal static string OmlInvalidTupleContainsDecision0 => ResourceManager.GetString("OmlInvalidTupleContainsDecision0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuple element must be single-valued..
		/// </summary>
		internal static string OmlInvalidTupleElementContainsForeach => ResourceManager.GetString("OmlInvalidTupleElementContainsForeach", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuple element '{0}' must be single-valued..
		/// </summary>
		internal static string OmlInvalidTupleElementContainsForeach0 => ResourceManager.GetString("OmlInvalidTupleElementContainsForeach0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuple element is not numeric..
		/// </summary>
		internal static string OmlInvalidTupleElementNotNumber => ResourceManager.GetString("OmlInvalidTupleElementNotNumber", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuple element '{0}' is not numeric..
		/// </summary>
		internal static string OmlInvalidTupleElementNotNumber0 => ResourceManager.GetString("OmlInvalidTupleElementNotNumber0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to For a Tuple to appear outside a table constraint, the first column must be of type Probability..
		/// </summary>
		internal static string OmlInvalidTupleOutsideTableConstraint => ResourceManager.GetString("OmlInvalidTupleOutsideTableConstraint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuples can only be assigned to a list of lists..
		/// </summary>
		internal static string OmlInvalidTuplesCanOnlyBeAssignedToListOfLists => ResourceManager.GetString("OmlInvalidTuplesCanOnlyBeAssignedToListOfLists", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expected a Tuple with {0} arguments..
		/// </summary>
		internal static string OmlInvalidTupleWrongArity => ResourceManager.GetString("OmlInvalidTupleWrongArity", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The operator '{0}' requires exactly {1} argument(s)..
		/// </summary>
		internal static string OmlInvalidWrongArgumentCount => ResourceManager.GetString("OmlInvalidWrongArgumentCount", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The value assigned to '{0}' has the wrong type..
		/// </summary>
		internal static string OmlInvalidWrongTypeInAssignment => ResourceManager.GetString("OmlInvalidWrongTypeInAssignment", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Parameter.
		/// </summary>
		internal static string OmlParameter => ResourceManager.GetString("OmlParameter", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to &lt;{0}, {1}&gt; - &lt;{2}, {3}&gt;.
		/// </summary>
		internal static string OmlParseExceptionLocationFormat0123 => ResourceManager.GetString("OmlParseExceptionLocationFormat0123", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Recourse sections must contain exactly one decision.
		/// </summary>
		internal static string OmlRecourseMustContainAtLeastOneDecision => ResourceManager.GetString("OmlRecourseMustContainAtLeastOneDecision", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}' symbol has been used for a decision and cannot be used for a parameter..
		/// </summary>
		internal static string OmlSymbolUsedForDecision0 => ResourceManager.GetString("OmlSymbolUsedForDecision0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}' symbol has been used for different type of {1}..
		/// </summary>
		internal static string OmlSymbolUsedForDifferentType01 => ResourceManager.GetString("OmlSymbolUsedForDifferentType01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}' symbol has been used for a parameter and cannot be used for a decision..
		/// </summary>
		internal static string OmlSymbolUsedForParameter0 => ResourceManager.GetString("OmlSymbolUsedForParameter0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to One, and one only address allowed.
		/// </summary>
		internal static string OneOnlyAddressAllowed => ResourceManager.GetString("OneOnlyAddressAllowed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to One or more solvers were not aborted within the wait limit specified in the directive. If this problem persists, increase Directive.WaitLimit..
		/// </summary>
		internal static string OneOrMoreSolversWereNotAbortedWithinWaitLimitSpecified => ResourceManager.GetString("OneOrMoreSolversWereNotAbortedWithinWaitLimitSpecified", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} only applies to a List.
		/// </summary>
		internal static string OnlyAppliesToAList => ResourceManager.GetString("OnlyAppliesToAList", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only bind data clauses allowed in Input section.
		/// </summary>
		internal static string OnlyBinddataClausesAllowed => ResourceManager.GetString("OnlyBinddataClausesAllowed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only constant values are allowed for the iteration set in Foreach/FilteredForeach.
		/// </summary>
		internal static string OnlyConstantValuesAreAllowedForTheIterationSetInForeachFilteredForeach => ResourceManager.GetString("OnlyConstantValuesAreAllowedForTheIterationSetInForeachFilteredForeach", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only enumerated domains can be bound to string values..
		/// </summary>
		internal static string OnlyEnumeratedDomainsSupportStringBinding => ResourceManager.GetString("OnlyEnumeratedDomainsSupportStringBinding", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only integer and enum decisions may be probed.
		/// </summary>
		internal static string OnlyIntegerAndEnumDecisionsMayBeProbed => ResourceManager.GetString("OnlyIntegerAndEnumDecisionsMayBeProbed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only interval domains are allowed for simplex.
		/// </summary>
		internal static string OnlyIntervalDomainsAreAllowedForSimplex => ResourceManager.GetString("OnlyIntervalDomainsAreAllowedForSimplex", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only numeric decisions can be indexed..
		/// </summary>
		internal static string OnlyNumericDecisionsCanBeIndexed => ResourceManager.GetString("OnlyNumericDecisionsCanBeIndexed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only numeric parameters can be indexed..
		/// </summary>
		internal static string OnlyNumericParametersCanBeIndexed => ResourceManager.GetString("OnlyNumericParametersCanBeIndexed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only one domain can be specify for Sets section.
		/// </summary>
		internal static string OnlyOneDomainForSetsSection => ResourceManager.GetString("OnlyOneDomainForSetsSection", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only one goal is supported with Compact Quasi Newton solver..
		/// </summary>
		internal static string OnlyOneGoalIsSupportedWithCQNSolver => ResourceManager.GetString("OnlyOneGoalIsSupportedWithCQNSolver", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Can not have more that one Input section.
		/// </summary>
		internal static string OnlyOneInputsection => ResourceManager.GetString("OnlyOneInputsection", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only one row is supported for Compact Quasi Newton model..
		/// </summary>
		internal static string OnlyOneRowIsSupportedForCQNModel => ResourceManager.GetString("OnlyOneRowIsSupportedForCQNModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only parameters declared on a Parameters section can be bound.
		/// </summary>
		internal static string OnlyParametersDeclaredOnParametersSectionCanBeBound => ResourceManager.GetString("OnlyParametersDeclaredOnParametersSectionCanBeBound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only REPLACE, ADD and MULTIPLY are supported.
		/// </summary>
		internal static string OnlyREPLACEADDAndMULTIPLYAreSupported => ResourceManager.GetString("OnlyREPLACEADDAndMULTIPLYAreSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only two-stage problems are supported.
		/// </summary>
		internal static string OnlyTwoStageProblemsAreSupported => ResourceManager.GetString("OnlyTwoStageProblemsAreSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to This method is only valid for variables..
		/// </summary>
		internal static string OnlyValidForVariables => ResourceManager.GetString("OnlyValidForVariables", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to This method is only valid when called on decisions with {0} indexes..
		/// </summary>
		internal static string OnlyValidWhenCalledOnDecisionsWith0Indexes => ResourceManager.GetString("OnlyValidWhenCalledOnDecisionsWith0Indexes", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to This method is only valid when called on parameters with {0} indexes..
		/// </summary>
		internal static string OnlyValidWhenCalledOnParametersWith0Indexes => ResourceManager.GetString("OnlyValidWhenCalledOnParametersWith0Indexes", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only variables, not rows, may be used within a goal row.
		/// </summary>
		internal static string OnlyVariablesNotRowsMayBeUsedWithinAGoalRow => ResourceManager.GetString("OnlyVariablesNotRowsMayBeUsedWithinAGoalRow", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to This operation is not supported on second order conic rows..
		/// </summary>
		internal static string OperationNotSupportedOnConicRows => ResourceManager.GetString("OperationNotSupportedOnConicRows", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Operator was not defined.
		/// </summary>
		internal static string OperatorNotDefined => ResourceManager.GetString("OperatorNotDefined", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to optimal: exit RepeatPivots {0}.
		/// </summary>
		internal static string OptimalExitRepeatPivots0 => ResourceManager.GetString("OptimalExitRepeatPivots0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Or, .
		/// </summary>
		internal static string OrComma => ResourceManager.GetString("OrComma", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Output data binding format not supported for {0}: Call to Select not found..
		/// </summary>
		internal static string OutputDataBindingFormatNotSupportedFor0CallToSelectNotFound => ResourceManager.GetString("OutputDataBindingFormatNotSupportedFor0CallToSelectNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Output data binding format not supported for {0}: Property reference not found..
		/// </summary>
		internal static string OutputDataBindingFormatNotSupportedFor0PropertyReferenceNotFound => ResourceManager.GetString("OutputDataBindingFormatNotSupportedFor0PropertyReferenceNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Output data binding format not supported for {0}: Unable to call Select on {1}.
		/// </summary>
		internal static string OutputDataBindingFormatNotSupportedFor0UnableToCallSelectOn1 => ResourceManager.GetString("OutputDataBindingFormatNotSupportedFor0UnableToCallSelectOn1", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Output data binding format not supported for {0}: Unrecognized arguments to Select..
		/// </summary>
		internal static string OutputDataBindingFormatNotSupportedFor0UnrecognizedArgumentsToSelect => ResourceManager.GetString("OutputDataBindingFormatNotSupportedFor0UnrecognizedArgumentsToSelect", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Output of non-linear models or linear models with strict inequalities are not supported..
		/// </summary>
		internal static string OutputOfNonLinearModelsIsNotSupported => ResourceManager.GetString("OutputOfNonLinearModelsIsNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Overflow: '{0}'.
		/// </summary>
		internal static string Overflow => ResourceManager.GetString("Overflow", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Parameter {0} is not bound to a data source.
		/// </summary>
		internal static string Parameter0IsNotBoundToADataSource => ResourceManager.GetString("Parameter0IsNotBoundToADataSource", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Parameters cannot be disabled.
		/// </summary>
		internal static string ParametersCannotBeDisabled => ResourceManager.GetString("ParametersCannotBeDisabled", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to A parameter may only be assigned a constant within the correct domain and range.
		/// </summary>
		internal static string ParameterShouldBeAssignConstantOfItsDomain => ResourceManager.GetString("ParameterShouldBeAssignConstantOfItsDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Parameter should have indexes.
		/// </summary>
		internal static string ParameterShouldHaveIndexes => ResourceManager.GetString("ParameterShouldHaveIndexes", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to A parameter must use Sets or be assigned a constant value.
		/// </summary>
		internal static string ParameterShouldUseSetsOrBeAssignedConstant => ResourceManager.GetString("ParameterShouldUseSetsOrBeAssignedConstant", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Parameters with non-numeric data are not supported..
		/// </summary>
		internal static string ParametersWithNonNumericDataAreNotSupported => ResourceManager.GetString("ParametersWithNonNumericDataAreNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}'.
		/// </summary>
		internal static string ParsingModelFailed0 => ResourceManager.GetString("ParsingModelFailed0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected term: '{1}', '{0}'.
		/// </summary>
		internal static string ParsingModelFailed01 => ResourceManager.GetString("ParsingModelFailed01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Parsing MPS failed: '{0}'.
		/// </summary>
		internal static string ParsingMPSFailed0 => ResourceManager.GetString("ParsingMPSFailed0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Phase 1 Optimal.
		/// </summary>
		internal static string Phase1Optimal => ResourceManager.GetString("Phase1Optimal", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Please request the Interior Point Method (IPM) solver to handle quadratic objectives..
		/// </summary>
		internal static string PleaseRequestInteriorPointIpmToHandleQuadraticObjectives => ResourceManager.GetString("PleaseRequestInteriorPointIpmToHandleQuadraticObjectives", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Accessing a PluginSolverCollection instance that has not been initialized..
		/// </summary>
		internal static string PluginSolverCollectionUninitialized => ResourceManager.GetString("PluginSolverCollectionUninitialized", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid solver assembly in MsfConfig section..
		/// </summary>
		internal static string PluginSolverConfigInvalidAssembly => ResourceManager.GetString("PluginSolverConfigInvalidAssembly", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid solver capability in MsfConfig section..
		/// </summary>
		internal static string PluginSolverConfigInvalidCapability => ResourceManager.GetString("PluginSolverConfigInvalidCapability", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid solver directive class name in MsfConfig section..
		/// </summary>
		internal static string PluginSolverConfigInvalidDirectiveClass => ResourceManager.GetString("PluginSolverConfigInvalidDirectiveClass", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid element in MsfConfig section..
		/// </summary>
		internal static string PluginSolverConfigInvalidElement => ResourceManager.GetString("PluginSolverConfigInvalidElement", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid MsfPluginSolvers section in MsfConfig section..
		/// </summary>
		internal static string PluginSolverConfigInvalidMsfPluginSolversSection => ResourceManager.GetString("PluginSolverConfigInvalidMsfPluginSolversSection", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid solver name in MsfConfig section..
		/// </summary>
		internal static string PluginSolverConfigInvalidName => ResourceManager.GetString("PluginSolverConfigInvalidName", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid solver parameter class name in MsfConfig section..
		/// </summary>
		internal static string PluginSolverConfigInvalidParameterClass => ResourceManager.GetString("PluginSolverConfigInvalidParameterClass", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid solver class name in MsfConfig section..
		/// </summary>
		internal static string PluginSolverConfigInvalidSolverClass => ResourceManager.GetString("PluginSolverConfigInvalidSolverClass", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Null element in MsfConfig section..
		/// </summary>
		internal static string PluginSolverConfigNullElement => ResourceManager.GetString("PluginSolverConfigNullElement", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Duplicated solver registration in &lt;MsfPluginSolvers&gt;. The same combination of capability and solverclass can appear in this section at most once.
		/// </summary>
		internal static string PluginSolverDuplicatedRegistration => ResourceManager.GetString("PluginSolverDuplicatedRegistration", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Microsoft Solver Foundation plugin solver configuration exception..
		/// </summary>
		internal static string PluginSolverError => ResourceManager.GetString("PluginSolverError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Plugin solvers cannot be hosted in this execution environment..
		/// </summary>
		internal static string PluginSolverHostEnvironmentNotSupported => ResourceManager.GetString("PluginSolverHostEnvironmentNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solver {0} is registered twice with the same capability..
		/// </summary>
		internal static string PluginSolverInconsistentRegistration => ResourceManager.GetString("PluginSolverInconsistentRegistration", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Post-feasibility strategy {0} cannot be used with a model that does not have goals..
		/// </summary>
		internal static string PostFeasibilityStrategy0CannotBeUsedWithAModelThatDoesNotHaveGoals => ResourceManager.GetString("PostFeasibilityStrategy0CannotBeUsedWithAModelThatDoesNotHaveGoals", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Power is not constant.
		/// </summary>
		internal static string PowerIsNotConstant => ResourceManager.GetString("PowerIsNotConstant", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Power is not integer.
		/// </summary>
		internal static string PowerIsNotInteger => ResourceManager.GetString("PowerIsNotInteger", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Power is outside of allowed bounds.
		/// </summary>
		internal static string PowerIsOutsideOfAllowedBounds => ResourceManager.GetString("PowerIsOutsideOfAllowedBounds", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Predictor: loop ={0,-3}, mu {1,8:G5}, gap {2,6:G3}.
		/// </summary>
		internal static string PredictorLoopMuGap => ResourceManager.GetString("PredictorLoopMuGap", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Pre-feasibility strategy {0} cannot be used with a model that does not have goals..
		/// </summary>
		internal static string PreFeasibilityStrategy0CannotBeUsedWithAModelThatDoesNotHaveGoals => ResourceManager.GetString("PreFeasibilityStrategy0CannotBeUsedWithAModelThatDoesNotHaveGoals", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Preprocessing....
		/// </summary>
		internal static string Preprocessing => ResourceManager.GetString("Preprocessing", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to  done.
		/// </summary>
		internal static string PreprocessingDone => ResourceManager.GetString("PreprocessingDone", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Preprocessing time:      {0}ms.
		/// </summary>
		internal static string PreprocessingTime0Ms => ResourceManager.GetString("PreprocessingTime0Ms", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Probabilities should sum up to 1.
		/// </summary>
		internal static string ProbabilitiesShouldSumupToOne => ResourceManager.GetString("ProbabilitiesShouldSumupToOne", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Probability should be more than 0 and less or equal to 1.
		/// </summary>
		internal static string ProbabilityShouldBeMoreThanZeroAndLessOrEqualToOne => ResourceManager.GetString("ProbabilityShouldBeMoreThanZeroAndLessOrEqualToOne", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Foreach statements over indexed sets are not allowed in Decision section, use the declaration alone..
		/// </summary>
		internal static string ProhibitedForeachInDecisions => ResourceManager.GetString("ProhibitedForeachInDecisions", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Foreach statements over indexed sets are not allowed in Parameters section, use the declaration alone..
		/// </summary>
		internal static string ProhibitedForeachInParameters => ResourceManager.GetString("ProhibitedForeachInParameters", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Property '{0}' can only be accessed by Solving event handlers..
		/// </summary>
		internal static string Property0CanOnlyBeAccessedBySolvingEventHandlers => ResourceManager.GetString("Property0CanOnlyBeAccessedBySolvingEventHandlers", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Property name {0} is not supported..
		/// </summary>
		internal static string PropertyNameIsNotSupported0 => ResourceManager.GetString("PropertyNameIsNotSupported0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The property {0} requires a Decision object, but none was supplied. Accessing Decision properties in a Solving event handler is not supported..
		/// </summary>
		internal static string PropertyRequiresADecision0 => ResourceManager.GetString("PropertyRequiresADecision0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Punctuator '{0}' already mapped!.
		/// </summary>
		internal static string PunctuatorAlreadyMapped => ResourceManager.GetString("PunctuatorAlreadyMapped", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Pure quadratic goal but unknown whether minimizing or maximizing..
		/// </summary>
		internal static string PureQuadraticGoalButUnknownWhetherMinimizeOrMaximize => ResourceManager.GetString("PureQuadraticGoalButUnknownWhetherMinimizeOrMaximize", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Quadratic models should not reference non-variables..
		/// </summary>
		internal static string QpModelShouldNotReferenceNonVariables => ResourceManager.GetString("QpModelShouldNotReferenceNonVariables", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Quadratic models only support a single goal row. Can't change to a different goal row..
		/// </summary>
		internal static string QuadraticModelOnlySupportsASingleGoalRowCanTChangeToADifferentGoalRow => ResourceManager.GetString("QuadraticModelOnlySupportsASingleGoalRowCanTChangeToADifferentGoalRow", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The distribution information for this random parameter has already been specified..
		/// </summary>
		internal static string RandomParameterHasAllreadyFilledWithDistributionDetails => ResourceManager.GetString("RandomParameterHasAllreadyFilledWithDistributionDetails", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Random Seed: {0}.
		/// </summary>
		internal static string RandomSeed0 => ResourceManager.GetString("RandomSeed0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Range of domain should have minimum value and maximum value or not having them both.
		/// </summary>
		internal static string RangeOfDomainWrongNumberOfBoundaries => ResourceManager.GetString("RangeOfDomainWrongNumberOfBoundaries", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Recursion limit exceeded: {0}.
		/// </summary>
		internal static string RecursionLimitExceeded => ResourceManager.GetString("RecursionLimitExceeded", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} Reducing error.
		/// </summary>
		internal static string ReducingError => ResourceManager.GetString("ReducingError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Rejected contracted: {0}..
		/// </summary>
		internal static string RejectedContractedCount0 => ResourceManager.GetString("RejectedContractedCount0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Rejected expanded: {0}..
		/// </summary>
		internal static string RejectedExpandedCount0 => ResourceManager.GetString("RejectedExpandedCount0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} Removed entering candidate: {1}, {2}.
		/// </summary>
		internal static string RemovedEnteringCandidate12 => ResourceManager.GetString("RemovedEnteringCandidate12", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} Repaired singular basis.
		/// </summary>
		internal static string RepairedSingularBasis => ResourceManager.GetString("RepairedSingularBasis", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Replacing {0} with {1} in basis.
		/// </summary>
		internal static string Replacing0With1InBasis => ResourceManager.GetString("Replacing0With1InBasis", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Capabilities Applied: {0}.
		/// </summary>
		internal static string ReportCapability => ResourceManager.GetString("ReportCapability", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ===Solver Foundation Service Report===.
		/// </summary>
		internal static string ReportHeaderReportOverview => ResourceManager.GetString("ReportHeaderReportOverview", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ===Solution Details===.
		/// </summary>
		internal static string ReportHeaderSolutionDetails => ResourceManager.GetString("ReportHeaderSolutionDetails", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ===Solver Execution Details===.
		/// </summary>
		internal static string ReportHeaderSolverExecutionDetails => ResourceManager.GetString("ReportHeaderSolverExecutionDetails", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Reporting is only supported for LP, QP, MILP, CP, and UCNLP models..
		/// </summary>
		internal static string ReportingIsOnlySupportedForSimplexIPMAndCSPModels => ResourceManager.GetString("ReportingIsOnlySupportedForSimplexIPMAndCSPModels", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Reporting not implemented for SolverKind {0}.
		/// </summary>
		internal static string ReportingNotImplementedForSolverKind0 => ResourceManager.GetString("ReportingNotImplementedForSolverKind0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Algorithm: {0}.
		/// </summary>
		internal static string ReportLineAlgorithm => ResourceManager.GetString("ReportLineAlgorithm", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Backtrack Count: {0}.
		/// </summary>
		internal static string ReportLineBacktrackCount => ResourceManager.GetString("ReportLineBacktrackCount", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Basis: {0}.
		/// </summary>
		internal static string ReportLineBasis => ResourceManager.GetString("ReportLineBasis", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Branches: {0}.
		/// </summary>
		internal static string ReportLineBranches => ResourceManager.GetString("ReportLineBranches", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Constraint Bounds:.
		/// </summary>
		internal static string ReportLineConstraintBounds => ResourceManager.GetString("ReportLineConstraintBounds", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Date: {0}.
		/// </summary>
		internal static string ReportLineDatetime0 => ResourceManager.GetString("ReportLineDatetime0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Decisions:.
		/// </summary>
		internal static string ReportLineDecisions => ResourceManager.GetString("ReportLineDecisions", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Degenerate Pivots: {0} ({1:F2} %).
		/// </summary>
		internal static string ReportLineDegeneratePivots => ResourceManager.GetString("ReportLineDegeneratePivots", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Directives:.
		/// </summary>
		internal static string ReportLineDirectives => ResourceManager.GetString("ReportLineDirectives", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Eliminated Slack Variables: {0}.
		/// </summary>
		internal static string ReportLineEliminatedSlackVariables => ResourceManager.GetString("ReportLineEliminatedSlackVariables", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Factorings: {0} + {1}.
		/// </summary>
		internal static string ReportLineFactorings => ResourceManager.GetString("ReportLineFactorings", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Goal Coefficients:.
		/// </summary>
		internal static string ReportLineGoalCoefficients => ResourceManager.GetString("ReportLineGoalCoefficients", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Goals:.
		/// </summary>
		internal static string ReportLineGoals => ResourceManager.GetString("ReportLineGoals", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Infeasible set:.
		/// </summary>
		internal static string ReportLineInfeasibleSet => ResourceManager.GetString("ReportLineInfeasibleSet", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Iterations: {0}.
		/// </summary>
		internal static string ReportLineIterations => ResourceManager.GetString("ReportLineIterations", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Model Name: {0}.
		/// </summary>
		internal static string ReportLineModelName => ResourceManager.GetString("ReportLineModelName", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Move Selection: {0}.
		/// </summary>
		internal static string ReportLineMoveSelection => ResourceManager.GetString("ReportLineMoveSelection", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Nonzeros: {0}.
		/// </summary>
		internal static string ReportLineNonzeros => ResourceManager.GetString("ReportLineNonzeros", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Arithmetic: Double.
		/// </summary>
		internal static string ReportLineNumericFormatDouble => ResourceManager.GetString("ReportLineNumericFormatDouble", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Arithmetic: Exact.
		/// </summary>
		internal static string ReportLineNumericFormatExact => ResourceManager.GetString("ReportLineNumericFormatExact", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Arithmetic: Hybrid.
		/// </summary>
		internal static string ReportLineNumericFormatHybrid => ResourceManager.GetString("ReportLineNumericFormatHybrid", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Phase 1 Pivots: {0} + {1}.
		/// </summary>
		internal static string ReportLinePhase1Pivots => ResourceManager.GetString("ReportLinePhase1Pivots", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Phase 2 Pivots: {0} + {1}.
		/// </summary>
		internal static string ReportLinePhase2Pivots => ResourceManager.GetString("ReportLinePhase2Pivots", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Pivot Count: {0}.
		/// </summary>
		internal static string ReportLinePivotCount => ResourceManager.GetString("ReportLinePivotCount", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Pricing (double): {0}.
		/// </summary>
		internal static string ReportLinePricingDouble => ResourceManager.GetString("ReportLinePricingDouble", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Pricing (exact): {0}.
		/// </summary>
		internal static string ReportLinePricingExact => ResourceManager.GetString("ReportLinePricingExact", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Rows: {0} -&gt; {1}.
		/// </summary>
		internal static string ReportLineRows => ResourceManager.GetString("ReportLineRows", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Shadow Pricing:.
		/// </summary>
		internal static string ReportLineShadowPricing => ResourceManager.GetString("ReportLineShadowPricing", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solve Completion Status: {0}.
		/// </summary>
		internal static string ReportLineSolveCompletionStatus => ResourceManager.GetString("ReportLineSolveCompletionStatus", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solver Selected: {0}.
		/// </summary>
		internal static string ReportLineSolverSelected => ResourceManager.GetString("ReportLineSolverSelected", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solve Time (ms): {0}.
		/// </summary>
		internal static string ReportLineSolveTimeMs => ResourceManager.GetString("ReportLineSolveTimeMs", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Total Time (ms): {0}.
		/// </summary>
		internal static string ReportLineTotalTimeMs => ResourceManager.GetString("ReportLineTotalTimeMs", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Value Selection: {0}.
		/// </summary>
		internal static string ReportLineValueSelection => ResourceManager.GetString("ReportLineValueSelection", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Variables: {0} -&gt; {1} + {2}.
		/// </summary>
		internal static string ReportLineVariables => ResourceManager.GetString("ReportLineVariables", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Variables: {0} -&gt; {1}.
		/// </summary>
		internal static string ReportLineVariables01 => ResourceManager.GetString("ReportLineVariables01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Variable Selection: {0}.
		/// </summary>
		internal static string ReportLineVariableSelection => ResourceManager.GetString("ReportLineVariableSelection", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Version: {0}.
		/// </summary>
		internal static string ReportLineVersion0 => ResourceManager.GetString("ReportLineVersion0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to rescue....
		/// </summary>
		internal static string Rescue => ResourceManager.GetString("Rescue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to result = {0}.
		/// </summary>
		internal static string Result0 => ResourceManager.GetString("Result0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The result cannot be converted to Int32. Either the successProbability argument of the distribution is too small, or probability argument is too big..
		/// </summary>
		internal static string ResultNeedsToBeInteger => ResourceManager.GetString("ResultNeedsToBeInteger", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to OML returned a solution in an unknown format.
		/// </summary>
		internal static string ReturnedASolutionInAnUnknownFormat => ResourceManager.GetString("ReturnedASolutionInAnUnknownFormat", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Row '{0}' already exists in destination model..
		/// </summary>
		internal static string Row0AlreadyExistsInDestinationModel => ResourceManager.GetString("Row0AlreadyExistsInDestinationModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to row indexes did not sort correctly.
		/// </summary>
		internal static string RowIndexesDidNotSortCorrectly => ResourceManager.GetString("RowIndexesDidNotSortCorrectly", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to row {0} is unbounded.
		/// </summary>
		internal static string RowIsUnbounded => ResourceManager.GetString("RowIsUnbounded", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Row {0} needs to be differentiated but {1} is not a differentiable operation..
		/// </summary>
		internal static string RowNeedsToBeDifferentiatedButIsNotDifferentiable01 => ResourceManager.GetString("RowNeedsToBeDifferentiatedButIsNotDifferentiable01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to rule application failed.
		/// </summary>
		internal static string RuleApplicationFailed => ResourceManager.GetString("RuleApplicationFailed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Rule ('-&gt;') should have two parameters.
		/// </summary>
		internal static string RuleShouldHaveTwoParameters => ResourceManager.GetString("RuleShouldHaveTwoParameters", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Detected multiple bindings on a single parameter. Please make sure parameters are bound only once..
		/// </summary>
		internal static string SameParametersBoundMoreThanOnce => ResourceManager.GetString("SameParametersBoundMoreThanOnce", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Sampled.
		/// </summary>
		internal static string Sampled => ResourceManager.GetString("Sampled", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Sampling Method: {0}.
		/// </summary>
		internal static string SampleMethod0 => ResourceManager.GetString("SampleMethod0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Sample Count: {0}.
		/// </summary>
		internal static string SamplesCount0 => ResourceManager.GetString("SamplesCount0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Sampling/getting Quantile from the Binomial distribution failed.
		/// </summary>
		internal static string SamplingGettingQuantileFromTheBinomialDistributionFailed => ResourceManager.GetString("SamplingGettingQuantileFromTheBinomialDistributionFailed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SAT solver can't process the model: '{0}', '{1}'.
		/// </summary>
		internal static string SATSolverCantProcessTheModel01 => ResourceManager.GetString("SATSolverCantProcessTheModel01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Scenarios Count: {0}.
		/// </summary>
		internal static string ScenariosCount0 => ResourceManager.GetString("ScenariosCount0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Scenarios must include one element.
		/// </summary>
		internal static string ScenariosMustIncludeOneElement => ResourceManager.GetString("ScenariosMustIncludeOneElement", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SearchFirst list elements must be pre-declared Variables.
		/// </summary>
		internal static string SearchFirstListElementsMustBePreDeclaredVariables => ResourceManager.GetString("SearchFirstListElementsMustBePreDeclaredVariables", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SearchFirst contains Terms not belonging to this Solver .
		/// </summary>
		internal static string SearchFirstTermError => ResourceManager.GetString("SearchFirstTermError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} second arg must be a constant.
		/// </summary>
		internal static string SecondArgMustBeAConstant => ResourceManager.GetString("SecondArgMustBeAConstant", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} second arg must be nonnegative.
		/// </summary>
		internal static string SecondArgMustBeNonnegative => ResourceManager.GetString("SecondArgMustBeNonnegative", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The second stage approximation is better that the actual second stage problem result..
		/// </summary>
		internal static string SecondStageApproximationIsBetterThanSecondStageResult => ResourceManager.GetString("SecondStageApproximationIsBetterThanSecondStageResult", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Second stage decisions (Average [Min, Max]):.
		/// </summary>
		internal static string SecondStageDecisionsValuesAverageMinimalMaximal => ResourceManager.GetString("SecondStageDecisionsValuesAverageMinimalMaximal", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to '{0}' is valid for conic models only..
		/// </summary>
		internal static string SectionIsValidForConicModelsOnly0 => ResourceManager.GetString("SectionIsValidForConicModelsOnly0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SetBinding may not be called on a Set with fixed values..
		/// </summary>
		internal static string SetBindingMayNotBeCalledOnASetWithFixedValues => ResourceManager.GetString("SetBindingMayNotBeCalledOnASetWithFixedValues", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The ElementAt operator cannot take an empty constant list variable.
		/// </summary>
		internal static string SetListEmptyListUsedInElementAt => ResourceManager.GetString("SetListEmptyListUsedInElementAt", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The value kind of the domains are incompatible for the set/list operator.
		/// </summary>
		internal static string SetListIncompatibleDomainKind => ResourceManager.GetString("SetListIncompatibleDomainKind", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The index Term in ElementAt operator must have an interval integer domain from 0 to the maximal length of lists in listVar - 1 .
		/// </summary>
		internal static string SetListIncompatibleIndex => ResourceManager.GetString("SetListIncompatibleIndex", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to A composite variable (set/list variable) appears where a scalar variable is expected .
		/// </summary>
		internal static string SetListNonscalorElement => ResourceManager.GetString("SetListNonscalorElement", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The index Term in ElementAt operator must be an integer variable .
		/// </summary>
		internal static string SetListNonscalorIndex => ResourceManager.GetString("SetListNonscalorIndex", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The baseline set of the power-set/power-list variable is null .
		/// </summary>
		internal static string SetListNullBaseline => ResourceManager.GetString("SetListNullBaseline", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The argument to the set/list operator is null .
		/// </summary>
		internal static string SetListNullInput => ResourceManager.GetString("SetListNullInput", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The result Term in FirstOccurrence and LastOccurrence must be an Integer Term .
		/// </summary>
		internal static string SetListOccurrenceResultNotInteger => ResourceManager.GetString("SetListOccurrenceResultNotInteger", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot do Union, Intersection, or Difference on Symbol set variables with different baselines .
		/// </summary>
		internal static string SetListSymbolSetVarNotAllowed => ResourceManager.GetString("SetListSymbolSetVarNotAllowed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The set/list variable's baseline domain cannot be empty .
		/// </summary>
		internal static string SetListVarEmptyDomain => ResourceManager.GetString("SetListVarEmptyDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The argument variable's domain must be CspPowerSet or CspPowerList in set/list operator .
		/// </summary>
		internal static string SetListWrongDomain => ResourceManager.GetString("SetListWrongDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The constant subset must be an array of ordered unique elements .
		/// </summary>
		internal static string SetListWrongSubset => ResourceManager.GetString("SetListWrongSubset", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Sets of Enum values are not supported. Use Domain.Any instead..
		/// </summary>
		internal static string SetsOfEnumValuesAreNotSupportedUseDomainAnyInstead => ResourceManager.GetString("SetsOfEnumValuesAreNotSupportedUseDomainAnyInstead", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot create instance of an empty Submodel '{0}'.
		/// </summary>
		internal static string SfsSubmodelCannotCreateInstanceOfEmptySubmodel => ResourceManager.GetString("SfsSubmodelCannotCreateInstanceOfEmptySubmodel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Submodel '{0}' is removed. Cannot instantiate it any more.
		/// </summary>
		internal static string SfsSubmodelCannotInstantiateRemovedSubmodel => ResourceManager.GetString("SfsSubmodelCannotInstantiateRemovedSubmodel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Root model cannot be instantiated.
		/// </summary>
		internal static string SfsSubmodelCannotInstantiateRootModel => ResourceManager.GetString("SfsSubmodelCannotInstantiateRootModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The submodel '{0}' is already instantiated and can no longer be modified..
		/// </summary>
		internal static string SfsSubmodelCannotModifyModels => ResourceManager.GetString("SfsSubmodelCannotModifyModels", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Submodel '{0}' is already instantiated and cannot be removed..
		/// </summary>
		internal static string SfsSubmodelCannotRemoveInstantiatedSubmodel => ResourceManager.GetString("SfsSubmodelCannotRemoveInstantiatedSubmodel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Submodel nesting exceeds the limit of {0}..
		/// </summary>
		internal static string SfsSubmodelExceedNestingLimit => ResourceManager.GetString("SfsSubmodelExceedNestingLimit", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Decision member not found in the SubmodelDecision.
		/// </summary>
		internal static string SfsSubmodelInstanceNotFound => ResourceManager.GetString("SfsSubmodelInstanceNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SubmodelDecision member not found in the SubmodelDecision.
		/// </summary>
		internal static string SfsSubmodelSubmodelInstanceNotFound => ResourceManager.GetString("SfsSubmodelSubmodelInstanceNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Simplex solver can't process the model: '{0}', '{1}'.
		/// </summary>
		internal static string SimplexSolverCantProcessTheModel01 => ResourceManager.GetString("SimplexSolverCantProcessTheModel01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SimplexSolver symbol is missing.
		/// </summary>
		internal static string SimplexSolverSymbolIsMissing => ResourceManager.GetString("SimplexSolverSymbolIsMissing", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Singular basis!.
		/// </summary>
		internal static string SingularBasis => ResourceManager.GetString("SingularBasis", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Size of sub matrix is fault.
		/// </summary>
		internal static string SizeOfSubmatrixIsFault => ResourceManager.GetString("SizeOfSubmatrixIsFault", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The sizes of {0} and {1} do not match..
		/// </summary>
		internal static string SizesDoNotMatch => ResourceManager.GetString("SizesDoNotMatch", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SMPS can only be parsed using a file or directory path.
		/// </summary>
		internal static string SMPSCanOnlyBeParsedUsingFileOrDirectoryPath => ResourceManager.GetString("SMPSCanOnlyBeParsedUsingFileOrDirectoryPath", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solution accepted: alg={0}, res={1}, vals={2}, fMip={3}.
		/// </summary>
		internal static string SolutionAcceptedAlg0Res1Vals2FMip3 => ResourceManager.GetString("SolutionAcceptedAlg0Res1Vals2FMip3", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The solution is out of date..
		/// </summary>
		internal static string SolutionIsOutOfDate => ResourceManager.GetString("SolutionIsOutOfDate", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to solutionMapping argument is not a PluginSolutionMapping object..
		/// </summary>
		internal static string SolutionMappingArgumentIsNotAPluginSolutionMappingObject => ResourceManager.GetString("SolutionMappingArgumentIsNotAPluginSolutionMappingObject", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The solutionMapping argument is not a LinearSolutionMapping object..
		/// </summary>
		internal static string SolutionMappingIsNotALinearSolutionMapping => ResourceManager.GetString("SolutionMappingIsNotALinearSolutionMapping", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solution quality is: {0}.
		/// </summary>
		internal static string SolutionQualityIs0 => ResourceManager.GetString("SolutionQualityIs0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solution rejected: alg={0}, res={1}, vals={2}, fMip={3}.
		/// </summary>
		internal static string SolutionRejectedAlg0Res1Vals2FMip3 => ResourceManager.GetString("SolutionRejectedAlg0Res1Vals2FMip3", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solve method needed to be called before checking for solution details.
		/// </summary>
		internal static string SolveMethodNeededToBeCalledBeforeCheckingSolution => ResourceManager.GetString("SolveMethodNeededToBeCalledBeforeCheckingSolution", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ISolverEnvironment parameter cannot be null when calling PluginSolverCollection.GetSolvers or PluginSolverCollection.GetDefaultSolver.
		/// </summary>
		internal static string SolverContextCannotBeNull => ResourceManager.GetString("SolverContextCannotBeNull", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} is registered with the {1} interface but does not support it.
		/// </summary>
		internal static string SolverDoesNotImplementInterface => ResourceManager.GetString("SolverDoesNotImplementInterface", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} does not support bounded variables. Remove this directive and use a directive that supports bounded variables..
		/// </summary>
		internal static string SolverDoesNotSupportBoundedVariables => ResourceManager.GetString("SolverDoesNotSupportBoundedVariables", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} does not support constrained models. Remove this directive and use a directive that supports constrained models..
		/// </summary>
		internal static string SolverDoesNotSupportConstrainedModels => ResourceManager.GetString("SolverDoesNotSupportConstrainedModels", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solver does not support getting or setting properties.
		/// </summary>
		internal static string SolverDoesNotSupportGettingOrSettingProperties => ResourceManager.GetString("SolverDoesNotSupportGettingOrSettingProperties", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} does not support non-differentiable models. Remove this directive and use a directive that supports non-differentiable models..
		/// </summary>
		internal static string SolverDoesNotSupportNonDifferentiableModels => ResourceManager.GetString("SolverDoesNotSupportNonDifferentiableModels", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} does not support the operation {1}. Remove this directive and use a directive that supports {1}..
		/// </summary>
		internal static string SolverDoesNotSupportOperation => ResourceManager.GetString("SolverDoesNotSupportOperation", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The solver interface {0} does not exist or is not supported.
		/// </summary>
		internal static string SolverInterfaceNotSupported => ResourceManager.GetString("SolverInterfaceNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solver must be ConstraintSystem.
		/// </summary>
		internal static string SolverMustBeConstraintSystem => ResourceManager.GetString("SolverMustBeConstraintSystem", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solver must be SimplexSolver.
		/// </summary>
		internal static string SolverMustBeSimplexSolver => ResourceManager.GetString("SolverMustBeSimplexSolver", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solver must implement IInteriorPointStatistics.
		/// </summary>
		internal static string SolverMustImplementIInteriorPointStatistics => ResourceManager.GetString("SolverMustImplementIInteriorPointStatistics", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solver must implement ILinearModel.
		/// </summary>
		internal static string SolverMustImplementILinearModel => ResourceManager.GetString("SolverMustImplementILinearModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solver must implement ILinearSimplexStatistics.
		/// </summary>
		internal static string SolverMustImplementILinearSimplexStatistics => ResourceManager.GetString("SolverMustImplementILinearSimplexStatistics", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to 'Solver' option must be 'Simplex': '{0}'.
		/// </summary>
		internal static string SolverOptionMustBeSimplex0 => ResourceManager.GetString("SolverOptionMustBeSimplex0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Simplex must receive at least one SimplexSolverParams object..
		/// </summary>
		internal static string SolverParamtersCouldNotBeEmpty => ResourceManager.GetString("SolverParamtersCouldNotBeEmpty", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The solver requires FunctionEvaluator to be specified before calling solve..
		/// </summary>
		internal static string SolverRequiresFunctionEvaluatorToBeSpecifiedBeforeCallingSolve => ResourceManager.GetString("SolverRequiresFunctionEvaluatorToBeSpecifiedBeforeCallingSolve", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solver has been Reset. Cannot continue solving for the next solution..
		/// </summary>
		internal static string SolverResetDuringSolve => ResourceManager.GetString("SolverResetDuringSolve", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solver solution quality: {0}..
		/// </summary>
		internal static string SolverSolutionQuality0 => ResourceManager.GetString("SolverSolutionQuality0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Solving Method: {0}.
		/// </summary>
		internal static string SolvingMethod0 => ResourceManager.GetString("SolvingMethod0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SOS1 constraints are not supported in OML.
		/// </summary>
		internal static string SOS1ConstraintsAreNotSupportedInOML => ResourceManager.GetString("SOS1ConstraintsAreNotSupportedInOML", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SOS2 constraints are not supported in OML.
		/// </summary>
		internal static string SOS2ConstraintsAreNotSupportedInOML => ResourceManager.GetString("SOS2ConstraintsAreNotSupportedInOML", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Right-hand clause in Sos2 must contain at least two decisions..
		/// </summary>
		internal static string Sos2NeedsAtLeastTwoDecisions => ResourceManager.GetString("Sos2NeedsAtLeastTwoDecisions", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only steepest edge pricing is supported with the SOS2 models.
		/// </summary>
		internal static string SOS2NotSupported => ResourceManager.GetString("SOS2NotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Miss SOS reference row name.
		/// </summary>
		internal static string SOSRowNameMissing => ResourceManager.GetString("SOSRowNameMissing", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Source model contains duplicate keys.
		/// </summary>
		internal static string SourceModelContainsDuplicateKeys => ResourceManager.GetString("SourceModelContainsDuplicateKeys", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Spans not compatible.
		/// </summary>
		internal static string SpansNotCompatible => ResourceManager.GetString("SpansNotCompatible", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SparseDomain with incorrect indexes.
		/// </summary>
		internal static string SparseDomainWithIncorrectIndexes => ResourceManager.GetString("SparseDomainWithIncorrectIndexes", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Starting point = .
		/// </summary>
		internal static string StartingPoint => ResourceManager.GetString("StartingPoint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Starting point dimensionality is not the same as the objective function.
		/// </summary>
		internal static string StartingPointDimensionalityDiffersFromObjectiveFunction => ResourceManager.GetString("StartingPointDimensionalityDiffersFromObjectiveFunction", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The starting point should be a numeric value..
		/// </summary>
		internal static string StartingPointShouldBeNumber => ResourceManager.GetString("StartingPointShouldBeNumber", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Values for starting point should be numbers.
		/// </summary>
		internal static string StartingPointShouldBeNumbers => ResourceManager.GetString("StartingPointShouldBeNumbers", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Statistical Lower Bound: {0} +- {1}.
		/// </summary>
		internal static string StatisticalLowerBound01 => ResourceManager.GetString("StatisticalLowerBound01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Statistical Upper Bound: {0} +- {1}.
		/// </summary>
		internal static string StatisticalUpperBound01 => ResourceManager.GetString("StatisticalUpperBound01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Step count: {0}..
		/// </summary>
		internal static string StepCount0 => ResourceManager.GetString("StepCount0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Stochastic core file should have one of the following suffixes: {0}.
		/// </summary>
		internal static string StochasticCoreFileShouldHaveOneOfThoseSuffixes0 => ResourceManager.GetString("StochasticCoreFileShouldHaveOneOfThoseSuffixes0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} stochastic file cannot be found.
		/// </summary>
		internal static string StochasticFileCannotBeFound0 => ResourceManager.GetString("StochasticFileCannotBeFound0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Stochastic Measures:.
		/// </summary>
		internal static string StochasticMeasures => ResourceManager.GetString("StochasticMeasures", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Multiple goals are not allowed in stochastic models..
		/// </summary>
		internal static string StochasticModelCannotContainMoreThanOneGoal => ResourceManager.GetString("StochasticModelCannotContainMoreThanOneGoal", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SOS2 constraints are not allowed in stochastic models..
		/// </summary>
		internal static string StochasticModelCannotHaveSOS2Constraints => ResourceManager.GetString("StochasticModelCannotHaveSOS2Constraints", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Stochastic models can only be saved in OML format..
		/// </summary>
		internal static string StochasticModelCanOnlyBeSavedToAnOMLFile => ResourceManager.GetString("StochasticModelCanOnlyBeSavedToAnOMLFile", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Stochastic models must contain a goal..
		/// </summary>
		internal static string StochasticModelMustContainAGoal => ResourceManager.GetString("StochasticModelMustContainAGoal", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Stochastic models must be linear. Verify there are no nonlinear terms in the goal or constraints..
		/// </summary>
		internal static string StochasticModelsMustBeLinear => ResourceManager.GetString("StochasticModelsMustBeLinear", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Stochastic models need to have both recourse decisions and random parameters.
		/// </summary>
		internal static string StochasticNeedRecourseDecisionsAndRandomParameters => ResourceManager.GetString("StochasticNeedRecourseDecisionsAndRandomParameters", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Strategy : {0} ({1}).
		/// </summary>
		internal static string Strategy01 => ResourceManager.GetString("Strategy01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Incompatible symbol domain .
		/// </summary>
		internal static string StringDomainIncompatible => ResourceManager.GetString("StringDomainIncompatible", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to This function does not support inputs with symbol domains .
		/// </summary>
		internal static string StringDomainNotSupported => ResourceManager.GetString("StringDomainNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to String is not a member of enumerated domain.
		/// </summary>
		internal static string StringIsNotAMemberOfEnumeratedDomain => ResourceManager.GetString("StringIsNotAMemberOfEnumeratedDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Sum of all probabilities should not exceed 1.
		/// </summary>
		internal static string SumOfAllProbabilitiesShouldNotExceedOne => ResourceManager.GetString("SumOfAllProbabilitiesShouldNotExceedOne", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Symbol already has a scope.
		/// </summary>
		internal static string SymbolAlreadyHasAScope => ResourceManager.GetString("SymbolAlreadyHasAScope", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Symbolic factorization time = {0:F2} sec, type = {1}.
		/// </summary>
		internal static string SymbolicFactorizationTime0SecType1 => ResourceManager.GetString("SymbolicFactorizationTime0SecType1", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {3}: columns = {0}, nonzeros = {1}, sp = {2:F3}.
		/// </summary>
		internal static string SymbolicFactorNonzeros0 => ResourceManager.GetString("SymbolicFactorNonzeros0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expression contains the reserved symbol {0}.
		/// </summary>
		internal static string SymbolIsReserved0 => ResourceManager.GetString("SymbolIsReserved0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Symbol "{0}"not found in the domain of {1}.
		/// </summary>
		internal static string SymbolNotFoundInDomain => ResourceManager.GetString("SymbolNotFoundInDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SymbolScope already bound to an expression.
		/// </summary>
		internal static string SymbolScopeAlreadyBoundToAnExpression => ResourceManager.GetString("SymbolScopeAlreadyBoundToAnExpression", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Symbol {0} with domain {1} could not be made a Variable.
		/// </summary>
		internal static string SymbolWithDomainCouldNotBeMadeAVariable => ResourceManager.GetString("SymbolWithDomainCouldNotBeMadeAVariable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SymmetricSparseMatrix does not support element insertion.
		/// </summary>
		internal static string SymmetricSparseMatrixDoesNotSupportElementInsertion => ResourceManager.GetString("SymmetricSparseMatrixDoesNotSupportElementInsertion", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Syntax error: {0}, {1}, {2}.
		/// </summary>
		internal static string SyntaxError012 => ResourceManager.GetString("SyntaxError012", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to system error cannot schedule tasks.
		/// </summary>
		internal static string SystemErrorCannotScheduleTasks => ResourceManager.GetString("SystemErrorCannotScheduleTasks", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Each row array of a table must have the same dimension as the column variable array .
		/// </summary>
		internal static string TableMismachedDimension => ResourceManager.GetString("TableMismachedDimension", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Task creation failed.
		/// </summary>
		internal static string TaskCreationFailed => ResourceManager.GetString("TaskCreationFailed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Term '{0}' has been added to model '{1}' and cannot be added to other models.
		/// </summary>
		internal static string TermHasBeenAddedToAModel01 => ResourceManager.GetString("TermHasBeenAddedToAModel01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The arity of the tuple must be the same as the arity of the variable tuple.
		/// </summary>
		internal static string TheArityOfTheTupleMustBeTheSameAsTheArityOfTheVariableTuple => ResourceManager.GetString("TheArityOfTheTupleMustBeTheSameAsTheArityOfTheVariableTuple", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The arity of tuples in the list must be the same as the arity of the variable tuple.
		/// </summary>
		internal static string TheArityOfTuplesInTheListMustBeTheSameAsTheArityOfTheVariableTuple => ResourceManager.GetString("TheArityOfTuplesInTheListMustBeTheSameAsTheArityOfTheVariableTuple", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The cut is invalid. {0}.
		/// </summary>
		internal static string TheCutIsInvalid0 => ResourceManager.GetString("TheCutIsInvalid0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The first argument to ElementOf must be a list.
		/// </summary>
		internal static string TheFirstArgumentToElementOfMustBeAList => ResourceManager.GetString("TheFirstArgumentToElementOfMustBeAList", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The goal could not be added to the model..
		/// </summary>
		internal static string TheGoalCouldNotBeAddedToTheModel => ResourceManager.GetString("TheGoalCouldNotBeAddedToTheModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The lower bound and the upper bound must be finite numbers..
		/// </summary>
		internal static string TheLowerBoundAndTheUpperBoundMustBeFiniteNumbers => ResourceManager.GetString("TheLowerBoundAndTheUpperBoundMustBeFiniteNumbers", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The mean must be a finite number and the standard deviation must be a non-negative number..
		/// </summary>
		internal static string TheMeanMustBeAFiniteNumberStandardDeviationMustBeANonNegativeNumber => ResourceManager.GetString("TheMeanMustBeAFiniteNumberStandardDeviationMustBeANonNegativeNumber", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The mean of the logarithm of the distribution must be a finite number and the standard deviation of the logarithm of the distribution must be a non-negative number..
		/// </summary>
		internal static string TheMeanOfLogMustBeAFiniteNumberStandardDeviationOfLogMustBeANonNegativeNumber => ResourceManager.GetString("TheMeanOfLogMustBeAFiniteNumberStandardDeviationOfLogMustBeANonNegativeNumber", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The method or operation is not implemented..
		/// </summary>
		internal static string TheMethodOrOperationIsNotImplemented => ResourceManager.GetString("TheMethodOrOperationIsNotImplemented", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The model is empty.
		/// </summary>
		internal static string TheModelIsEmpty => ResourceManager.GetString("TheModelIsEmpty", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The model must contain a goal..
		/// </summary>
		internal static string TheModelMustContainAGoal => ResourceManager.GetString("TheModelMustContainAGoal", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The number of index properties must match the number of index sets..
		/// </summary>
		internal static string TheNumberOfIndexPropertiesMustMatchTheNumberOfIndexSets => ResourceManager.GetString("TheNumberOfIndexPropertiesMustMatchTheNumberOfIndexSets", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The number of value properties must match the number of elements in the tuple..
		/// </summary>
		internal static string TheNumberOfValuePropertiesMustMatchTheNumberOfElementsInTheTuple => ResourceManager.GetString("TheNumberOfValuePropertiesMustMatchTheNumberOfElementsInTheTuple", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The property or field '{0}' was not found..
		/// </summary>
		internal static string ThePropertyOrField0WasNotFound => ResourceManager.GetString("ThePropertyOrField0WasNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The property or field '{0}' was not found on type '{1}'..
		/// </summary>
		internal static string ThePropertyOrField0WasNotFoundOnType1 => ResourceManager.GetString("ThePropertyOrField0WasNotFoundOnType1", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The rate must be a finite positive number..
		/// </summary>
		internal static string TheRateMustBeAFinitePositiveNumber => ResourceManager.GetString("TheRateMustBeAFinitePositiveNumber", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to There is already an active model in the context..
		/// </summary>
		internal static string ThereIsAlreadyAnActiveModelInTheContext => ResourceManager.GetString("ThereIsAlreadyAnActiveModelInTheContext", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to There is no solution to propagate.
		/// </summary>
		internal static string ThereIsNoSolutionToPropagate => ResourceManager.GetString("ThereIsNoSolutionToPropagate", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to There should be at least one scenario.
		/// </summary>
		internal static string ThereShouldBeAtLeastOneScenario => ResourceManager.GetString("ThereShouldBeAtLeastOneScenario", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The row does not sum to 0. .
		/// </summary>
		internal static string TheRowDoesNotSumTo0 => ResourceManager.GetString("TheRowDoesNotSumTo0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The solver does not support Sos1.
		/// </summary>
		internal static string TheSolverDoesNotSupportSos1 => ResourceManager.GetString("TheSolverDoesNotSupportSos1", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The solver does not support Sos2.
		/// </summary>
		internal static string TheSolverDoesNotSupportSos2 => ResourceManager.GetString("TheSolverDoesNotSupportSos2", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The solver(s) threw an exception while solving the model.
		/// </summary>
		internal static string TheSolverSThrewAnExceptionWhileSolvingTheModel => ResourceManager.GetString("TheSolverSThrewAnExceptionWhileSolvingTheModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The success probability must be a non-zero probability..
		/// </summary>
		internal static string TheSuccessProbabilityMustBeANonZeroProbability => ResourceManager.GetString("TheSuccessProbabilityMustBeANonZeroProbability", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The success probability must be a probability and the number of trials must be a positive number..
		/// </summary>
		internal static string TheSuccessProbabilityMustBeAProbabilityAndTheNumberOfTrialsMustBeAPositiveNumber => ResourceManager.GetString("TheSuccessProbabilityMustBeAProbabilityAndTheNumberOfTrialsMustBeAPositiveNumber", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The value {0} is not an allowable {1} value..
		/// </summary>
		internal static string TheValue0IsNotAnAllowable1Value => ResourceManager.GetString("TheValue0IsNotAnAllowable1Value", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The value "{0}" was not present in the enumerated domain..
		/// </summary>
		internal static string TheValue0WasNotPresentInTheEnumeratedDomain => ResourceManager.GetString("TheValue0WasNotPresentInTheEnumeratedDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The Term does not express a domain..
		/// </summary>
		internal static string ThisCanNotExpressDomain => ResourceManager.GetString("ThisCanNotExpressDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to This solver does not support setting a property while solving..
		/// </summary>
		internal static string ThisSolverDoesNotSupportSettingAPropertyWhileSolving => ResourceManager.GetString("ThisSolverDoesNotSupportSettingAPropertyWhileSolving", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to This version of the product has expired. Please contact Microsoft Corporation for licensing options..
		/// </summary>
		internal static string ThisVersionOfTheProductHasExpiredPleaseContactMicrosoftCorporationForLicensingOptions => ResourceManager.GetString("ThisVersionOfTheProductHasExpiredPleaseContactMicrosoftCorporationForLicensingOptions", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Interrupted due to time-out or user's request.
		/// </summary>
		internal static string Timeout => ResourceManager.GetString("Timeout", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Time to first Solution : .
		/// </summary>
		internal static string TimeToFirstSolution => ResourceManager.GetString("TimeToFirstSolution", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tokens not in same version.
		/// </summary>
		internal static string TokensNotInSameVersion => ResourceManager.GetString("TokensNotInSameVersion", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tolerance difference: {0}..
		/// </summary>
		internal static string ToleranceDifference0 => ResourceManager.GetString("ToleranceDifference0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tolerance is too low.
		/// </summary>
		internal static string ToleranceTooLow => ResourceManager.GetString("ToleranceTooLow", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ({0} per sec)
		///             .
		/// </summary>
		internal static string TreeSearchStatsPerSec => ResourceManager.GetString("TreeSearchStatsPerSec", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Total nb events:         {0,-10} .
		/// </summary>
		internal static string TreeSearchStatsTotalNbEvents010 => ResourceManager.GetString("TreeSearchStatsTotalNbEvents010", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Total nb nodes:          {0,-10} .
		/// </summary>
		internal static string TreeSearchStatsTotalNbNodes010 => ResourceManager.GetString("TreeSearchStatsTotalNbNodes010", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Total time:              {0}ms
		///             .
		/// </summary>
		internal static string TreeSearchStatsTotalTime0Ms => ResourceManager.GetString("TreeSearchStatsTotalTime0Ms", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to TryGet should not return false.
		/// </summary>
		internal static string TryGetShouldNotReturnFalse => ResourceManager.GetString("TryGetShouldNotReturnFalse", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuple data {0} does not belong to the domain {1} specified in the Tuples definition.
		/// </summary>
		internal static string TupleDataDoesNotBelongToDomainSpecifiedIn01 => ResourceManager.GetString("TupleDataDoesNotBelongToDomainSpecifiedIn01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuples {0} is not bound to a data source.
		/// </summary>
		internal static string Tuples0IsNotBoundToADataSource => ResourceManager.GetString("Tuples0IsNotBoundToADataSource", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuples defined as a Random Parameter cannot be used in Table Constraint.
		/// </summary>
		internal static string TuplesDefinedAsARandomParameterCannotBeUsedInTableConstraint => ResourceManager.GetString("TuplesDefinedAsARandomParameterCannotBeUsedInTableConstraint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuples must be added to the model before being used.
		/// </summary>
		internal static string TuplesMustBeAddedToTheModelBeforeBeingUsed => ResourceManager.GetString("TuplesMustBeAddedToTheModelBeforeBeingUsed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuples must include at least one element.
		/// </summary>
		internal static string TuplesMustIncludeAtLeastOneElement => ResourceManager.GetString("TuplesMustIncludeAtLeastOneElement", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuples parameter data must be a list of lists.
		/// </summary>
		internal static string TuplesParameterDataMustBeAListOfLists => ResourceManager.GetString("TuplesParameterDataMustBeAListOfLists", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tuples parameters cannot be indexed.
		/// </summary>
		internal static string TuplesParametersCannotBeIndexed => ResourceManager.GetString("TuplesParametersCannotBeIndexed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Both {0} and {1} are null.
		/// </summary>
		internal static string TwoAreNull01 => ResourceManager.GetString("TwoAreNull01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Stochastic Solution Type: {0}.
		/// </summary>
		internal static string TypeOfStochasticSolution0 => ResourceManager.GetString("TypeOfStochasticSolution0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Undefined submodel instance.
		/// </summary>
		internal static string UndefinedSubmodelInstance => ResourceManager.GetString("UndefinedSubmodelInstance", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to unexpected column kind.
		/// </summary>
		internal static string UnexpectedColumnKind => ResourceManager.GetString("UnexpectedColumnKind", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected 'INTEND' marker.
		/// </summary>
		internal static string UnexpectedINTENDMarker => ResourceManager.GetString("UnexpectedINTENDMarker", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected term ‘{0}’. Expected: ‘{1}’.
		/// </summary>
		internal static string UnExpectedNeedToBe01 => ResourceManager.GetString("UnExpectedNeedToBe01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected term ‘{0}’. Expected: ‘{1}’ or '{2}'.
		/// </summary>
		internal static string UnexpectedNeedToBe012 => ResourceManager.GetString("UnexpectedNeedToBe012", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected term ‘{0}’. Expected: ‘{1}’ or end of file.
		/// </summary>
		internal static string UnexpectedNeedToBe01orEndOfFile => ResourceManager.GetString("UnexpectedNeedToBe01orEndOfFile", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected term ‘{0}’..
		/// </summary>
		internal static string UnexpectedTerm0 => ResourceManager.GetString("UnexpectedTerm0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected term '{0}' after '{1}'..
		/// </summary>
		internal static string UnexpectedTermAfterTerm01 => ResourceManager.GetString("UnexpectedTermAfterTerm01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown cone id: {0}.
		/// </summary>
		internal static string UnknownConeId0 => ResourceManager.GetString("UnknownConeId0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown data value {0}.
		/// </summary>
		internal static string UnknownDataValue0 => ResourceManager.GetString("UnknownDataValue0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown domain encountered.
		/// </summary>
		internal static string UnknownDomainEncountered => ResourceManager.GetString("UnknownDomainEncountered", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Domain type is unknown .
		/// </summary>
		internal static string UnknownDomainType => ResourceManager.GetString("UnknownDomainType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown kind detected in save.
		/// </summary>
		internal static string UnknownKindDetectedInSave => ResourceManager.GetString("UnknownKindDetectedInSave", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown marker: {0}.
		/// </summary>
		internal static string UnknownMarker => ResourceManager.GetString("UnknownMarker", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The modeler inside a Term is unknown .
		/// </summary>
		internal static string UnknownModelerType => ResourceManager.GetString("UnknownModelerType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown operation id: {0}.
		/// </summary>
		internal static string UnknownOperation0 => ResourceManager.GetString("UnknownOperation0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot load the plug-in solver assembly {0}..
		/// </summary>
		internal static string UnknownPluginAssembly => ResourceManager.GetString("UnknownPluginAssembly", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot find the plug-in solver parameter class {0}.
		/// </summary>
		internal static string UnknownPluginSolverParameterType => ResourceManager.GetString("UnknownPluginSolverParameterType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot find the plug-in solver class {0} or it does not implement {1} interface.
		/// </summary>
		internal static string UnknownPluginSolverType => ResourceManager.GetString("UnknownPluginSolverType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown quality {0}.
		/// </summary>
		internal static string UnknownQuality => ResourceManager.GetString("UnknownQuality", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown row: '{0}'.
		/// </summary>
		internal static string UnknownRow => ResourceManager.GetString("UnknownRow", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown row variable index: {0}.
		/// </summary>
		internal static string UnknownRowVariableIndex => ResourceManager.GetString("UnknownRowVariableIndex", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Symbol does not belong to the symbol domain .
		/// </summary>
		internal static string UnknownString => ResourceManager.GetString("UnknownString", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown supported MPS field: QUADOBJ.
		/// </summary>
		internal static string UnknownSupportedMPSFieldQUADOBJ => ResourceManager.GetString("UnknownSupportedMPSFieldQUADOBJ", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The variable given is unknown.
		/// </summary>
		internal static string UnknownVariable => ResourceManager.GetString("UnknownVariable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown variable: {0}.
		/// </summary>
		internal static string UnknownVariable0 => ResourceManager.GetString("UnknownVariable0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown variable id: {0}.
		/// </summary>
		internal static string UnknownVariableId => ResourceManager.GetString("UnknownVariableId", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The Term has unknown type .
		/// </summary>
		internal static string UnknowTermType => ResourceManager.GetString("UnknowTermType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unmatched sizes of lists in SumProduct.
		/// </summary>
		internal static string UnmatchedSizesOfListsInSumProduct => ResourceManager.GetString("UnmatchedSizesOfListsInSumProduct", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Internal Error: unnecessary change on the variable .
		/// </summary>
		internal static string UnnecessaryChange => ResourceManager.GetString("UnnecessaryChange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unrecognized constraint programming solver term.
		/// </summary>
		internal static string UnrecognizedCspTerm => ResourceManager.GetString("UnrecognizedCspTerm", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unrecognized term.
		/// </summary>
		internal static string UnrecognizedTerm => ResourceManager.GetString("UnrecognizedTerm", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The input to an unsigned long operator must be either an integer variable or an unsigned long variable .
		/// </summary>
		internal static string UnsignedLongOperatorInputInvalid => ResourceManager.GetString("UnsignedLongOperatorInputInvalid", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The given Term is not an unsigned long variable .
		/// </summary>
		internal static string UnsignedLongVarInvalid => ResourceManager.GetString("UnsignedLongVarInvalid", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unsupported model type.
		/// </summary>
		internal static string UnsupportedModelType => ResourceManager.GetString("UnsupportedModelType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to upper triangular coordinate seen.
		/// </summary>
		internal static string UpperTriangularCoordinateSeen => ResourceManager.GetString("UpperTriangularCoordinateSeen", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Value in ElementOf is not an integer..
		/// </summary>
		internal static string ValueInElementOfIsNotAnInteger => ResourceManager.GetString("ValueInElementOfIsNotAnInteger", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Value is not linear or quadratic.
		/// </summary>
		internal static string ValueIsNotLinearOrQuadratic => ResourceManager.GetString("ValueIsNotLinearOrQuadratic", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Value set cannot be empty.
		/// </summary>
		internal static string ValueSetCannotBeEmpty => ResourceManager.GetString("ValueSetCannotBeEmpty", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Values for symbol '{0}' are locked..
		/// </summary>
		internal static string ValuesForSymbolAreLocked => ResourceManager.GetString("ValuesForSymbolAreLocked", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Variable '{0}' already exists in destination model..
		/// </summary>
		internal static string Variable0AlreadyExistsInDestinationModel => ResourceManager.GetString("Variable0AlreadyExistsInDestinationModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Variable already belongs to another cone..
		/// </summary>
		internal static string VariableAlreadyBelongsToAnotherCone => ResourceManager.GetString("VariableAlreadyBelongsToAnotherCone", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Variable domain does not contain correct integer values.
		/// </summary>
		internal static string VariableDomainDoesNotContainCorrectIntegerValues => ResourceManager.GetString("VariableDomainDoesNotContainCorrectIntegerValues", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Variable domain exceeds CspSolver integer domain.
		/// </summary>
		internal static string VariableDomainExceedsCspSolverIntegerDomain => ResourceManager.GetString("VariableDomainExceedsCspSolverIntegerDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Variable has already been added to the model: {0}.
		/// </summary>
		internal static string VariableHasAlreadyBeenAddedToTheModel => ResourceManager.GetString("VariableHasAlreadyBeenAddedToTheModel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Variable has empty value set.
		/// </summary>
		internal static string VariableHasEmptyValueSet => ResourceManager.GetString("VariableHasEmptyValueSet", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Variable must be added to solver before use.
		/// </summary>
		internal static string VariableMustBeAddedToSolverBeforeUse => ResourceManager.GetString("VariableMustBeAddedToSolverBeforeUse", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Variable must be added to solver before use in constraint.
		/// </summary>
		internal static string VariableMustBeAddedToSolverBeforeUseInConstraint => ResourceManager.GetString("VariableMustBeAddedToSolverBeforeUseInConstraint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Variable must be based on a user symbol.
		/// </summary>
		internal static string VariableMustBeBasedOnAUserSymbol => ResourceManager.GetString("VariableMustBeBasedOnAUserSymbol", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to variable not found in solution.
		/// </summary>
		internal static string VariableNotFoundInSolution => ResourceManager.GetString("VariableNotFoundInSolution", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Variable seen with unrecognized Domain.
		/// </summary>
		internal static string VariableSeenWithUnrecognizedDomain => ResourceManager.GetString("VariableSeenWithUnrecognizedDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Variable {0} should be 0 but is {1}..
		/// </summary>
		internal static string VariableShouldBe0ButIs => ResourceManager.GetString("VariableShouldBe0ButIs", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Enterprise.
		/// </summary>
		internal static string VersionEnterprise => ResourceManager.GetString("VersionEnterprise", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Evaluation.
		/// </summary>
		internal static string VersionEvaluation => ResourceManager.GetString("VersionEvaluation", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Express.
		/// </summary>
		internal static string VersionExpress => ResourceManager.GetString("VersionExpress", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Standard.
		/// </summary>
		internal static string VersionStandard => ResourceManager.GetString("VersionStandard", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Violation: {0}.
		/// </summary>
		internal static string Violation0 => ResourceManager.GetString("Violation0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to VSS: {0}.
		/// </summary>
		internal static string Vss0 => ResourceManager.GetString("Vss0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} Wandered to dual infeasible: {1}.
		/// </summary>
		internal static string WanderedToDualInfeasible1 => ResourceManager.GetString("WanderedToDualInfeasible1", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to  pivot count: {0} Method Wandered to infeasible: {1}.
		/// </summary>
		internal static string WanderedToInfeasible1 => ResourceManager.GetString("WanderedToInfeasible1", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown feature name.
		/// </summary>
		internal static string WeAreMissingAnEmumForTheConstraintValueInTheFeatureExtraction => ResourceManager.GetString("WeAreMissingAnEmumForTheConstraintValueInTheFeatureExtraction", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to WriteMps needs a Model or simplex solver, optionally followed by a boolean indicating fixed (True) or free (False)..
		/// </summary>
		internal static string WriteMpsNeedsAModelOrSimplexSolverOptionallyFollowedByABooleanIndicatingFixedTrueOrFreeFalse => ResourceManager.GetString("WriteMpsNeedsAModelOrSimplexSolverOptionallyFollowedByABooleanIndicatingFixedTrueOrFreeFalse", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to wrong argument: power expects exponent &gt;= 1.
		/// </summary>
		internal static string WrongArgumentPowerExpectsExponent1 => ResourceManager.GetString("WrongArgumentPowerExpectsExponent1", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Wrong basic variable value! {0} {1} {2}.
		/// </summary>
		internal static string WrongBasicVariableValue012 => ResourceManager.GetString("WrongBasicVariableValue012", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Wrong decision declaration in Foreach statement.
		/// </summary>
		internal static string WrongDecisionDeclarationInForeach => ResourceManager.GetString("WrongDecisionDeclarationInForeach", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Can not deal with that error type.
		/// </summary>
		internal static string WrongErrorType => ResourceManager.GetString("WrongErrorType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Wrong line in INDEP section.
		/// </summary>
		internal static string WrongLineInINDEPSection => ResourceManager.GetString("WrongLineInINDEPSection", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Wrong line in PERIODS section.
		/// </summary>
		internal static string WrongLineInPERIODSSection => ResourceManager.GetString("WrongLineInPERIODSSection", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to MIP Solver currently has to use hybrid or exact numerics.
		/// </summary>
		internal static string WrongMIPNumerics => ResourceManager.GetString("WrongMIPNumerics", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} random numbers are expected as input, but {1} were supplied.
		/// </summary>
		internal static string WrongNumberOfRandomNumbers01 => ResourceManager.GetString("WrongNumberOfRandomNumbers01", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Wrong parameter assign clause.
		/// </summary>
		internal static string WrongParameterAssignClause => ResourceManager.GetString("WrongParameterAssignClause", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Wrong parameter assign in Foreach statement.
		/// </summary>
		internal static string WrongParameterAssignInForeachStatement => ResourceManager.GetString("WrongParameterAssignInForeachStatement", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Wrong syntax for domain. Try to use DomainSymbol[min, max] instead..
		/// </summary>
		internal static string WrongSyntaxForDomain => ResourceManager.GetString("WrongSyntaxForDomain", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Wrong syntax for parameter. Try to use param[set1, set2, ...] instead..
		/// </summary>
		internal static string WrongSyntaxParameterTwoInvocation => ResourceManager.GetString("WrongSyntaxParameterTwoInvocation", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Wrong variable identifier {0} (keys should be non-null and unique).
		/// </summary>
		internal static string WrongVariableIdentifier0KeysShouldBeNonNullAndUnique => ResourceManager.GetString("WrongVariableIdentifier0KeysShouldBeNonNullAndUnique", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Given variable's kind is different from the version of the ConstraintSolverSolution.GetValue method called.
		/// </summary>
		internal static string WrongVariableType => ResourceManager.GetString("WrongVariableType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} cannot be null..
		/// </summary>
		internal static string XIsNull0 => ResourceManager.GetString("XIsNull0", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} is less than {1}.
		/// </summary>
		internal static string XLessThanY01 => ResourceManager.GetString("XLessThanY01", resourceCulture);

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal Resources()
		{
		}
	}
}
