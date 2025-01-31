namespace Microsoft.SolverFoundation.Common
{
	/// <summary>Possible reasons for OmlParseException.
	/// </summary>
	public enum OmlParseExceptionReason
	{
		/// <summary>Unspecified reason.
		/// </summary>
		NotSpecified,
		/// <summary>Duplicate name.
		/// </summary>
		DuplicateName,
		/// <summary>The expression could not be converted into a term.
		/// </summary>
		ExpressionCannotBeConvertedIntoTerm,
		/// <summary>Invalid annotation.
		/// </summary>
		InvalidAnnotation,
		/// <summary>The wrong number of arguments was provided to an operator.
		/// </summary>
		InvalidArgumentCount,
		/// <summary>The argument type is invalid or does not match.
		/// </summary>
		InvalidArgumentType,
		/// <summary>Invalid data binding.
		/// </summary>
		InvalidDataBinding,
		/// <summary>Invalid decision.
		/// </summary>
		InvalidDecision,
		/// <summary>Invalid domain specification.
		/// </summary>
		InvalidDomain,
		/// <summary>Invalid condition in a Foreach or ForeachWhere.
		/// </summary>
		InvalidFilterCondition,
		/// <summary>Invalid goal.
		/// </summary>
		InvalidGoal,
		/// <summary>An invalid identifier was specified.
		/// </summary>
		InvalidName,
		/// <summary>Invalid iterator expression.
		/// </summary>
		InvalidIterator,
		/// <summary>Invalid index.
		/// </summary>
		InvalidIndex,
		/// <summary>Invalid index count.
		/// </summary>
		InvalidIndexCount,
		/// <summary>An invalid label was specified.
		/// </summary>
		InvalidLabel,
		/// <summary>Invalid parameter.
		/// </summary>
		InvalidParameter,
		/// <summary>Invalid SOS.
		/// </summary>
		InvalidSos,
		/// <summary>Invalid set.
		/// </summary>
		InvalidSet,
		/// <summary>Invalid tuples specification.
		/// </summary>
		InvalidTuples,
		/// <summary>Am element in a submodel section is invalid.
		/// </summary>
		SubmodelError,
		/// <summary>An unexpected term was encountered.
		/// </summary>
		UnexpectedTerm
	}
}
