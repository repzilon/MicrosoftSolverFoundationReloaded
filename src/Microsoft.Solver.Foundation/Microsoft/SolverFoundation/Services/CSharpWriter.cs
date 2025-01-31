using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CSharp;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	internal sealed class CSharpWriter : ITermVisitor<CodeExpression, object>, IDisposable
	{
		private readonly Dictionary<Decision, CodeExpression> decisionFields = new Dictionary<Decision, CodeExpression>();

		private readonly Dictionary<Parameter, CodeExpression> parameterFields = new Dictionary<Parameter, CodeExpression>();

		private readonly Dictionary<RecourseDecision, CodeExpression> recourseDecisionFields = new Dictionary<RecourseDecision, CodeExpression>();

		private readonly Dictionary<RandomParameter, CodeExpression> randomParameterFields = new Dictionary<RandomParameter, CodeExpression>();

		private readonly Dictionary<Domain, CodeExpression> domainFields = new Dictionary<Domain, CodeExpression>();

		private readonly Dictionary<Tuples, CodeExpression> tuplesFields = new Dictionary<Tuples, CodeExpression>();

		private readonly Dictionary<Set, CodeExpression> setFields = new Dictionary<Set, CodeExpression>();

		private readonly Dictionary<Term, CodeExpression> iterationFields = new Dictionary<Term, CodeExpression>();

		private CodeTypeDeclaration omlClass;

		private CodeMemberMethod omlConstructor;

		private CodeExpression modelExpr;

		private int nextLambda = 1;

		private readonly CodeTypeReferenceExpression modelTypeExpr;

		private readonly CodeTypeReferenceExpression domainTypeExpr;

		private readonly CSharpCodeProvider provider = new CSharpCodeProvider();

		private readonly CodeGeneratorOptions providerOptions = new CodeGeneratorOptions();

		private readonly bool _useFullyQualifiedTypeNames;

		private Dictionary<Type, CodeTypeReference> _typeToRef = new Dictionary<Type, CodeTypeReference>();

		public CSharpWriter()
			: this(useFullyQualifiedTypeNames: false)
		{
		}

		public CSharpWriter(bool useFullyQualifiedTypeNames)
		{
			_useFullyQualifiedTypeNames = useFullyQualifiedTypeNames;
			Type typeFromHandle = typeof(Model);
			Type typeFromHandle2 = typeof(Domain);
			if (useFullyQualifiedTypeNames)
			{
				modelTypeExpr = new CodeTypeReferenceExpression(typeFromHandle);
				domainTypeExpr = new CodeTypeReferenceExpression(typeFromHandle2);
			}
			else
			{
				modelTypeExpr = new CodeTypeReferenceExpression(typeFromHandle.Name);
				domainTypeExpr = new CodeTypeReferenceExpression(typeFromHandle2.Name);
			}
		}

		private CodeTypeReference GetCodeTypeReference(Type type)
		{
			if (!_typeToRef.TryGetValue(type, out var value))
			{
				value = ((!_useFullyQualifiedTypeNames) ? new CodeTypeReference(type.Name) : new CodeTypeReference(type));
				_typeToRef[type] = value;
			}
			return value;
		}

		internal void Write(Model model, TextWriter output)
		{
			CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
			CodeNamespace codeNamespace = new CodeNamespace("OmlExport");
			codeCompileUnit.Namespaces.Add(codeNamespace);
			AddImports(codeNamespace);
			omlClass = new CodeTypeDeclaration(GetClassName(model));
			codeNamespace.Types.Add(omlClass);
			omlConstructor = new CodeConstructor();
			omlClass.Members.Add(omlConstructor);
			omlConstructor.Attributes = MemberAttributes.Public;
			modelExpr = AddPropertyWithBackingField("Model", "model", GetCodeTypeReference(typeof(Model)));
			CodeExpression codeExpression = AddPropertyWithBackingField("Context", "context", GetCodeTypeReference(typeof(SolverContext)));
			omlConstructor.Statements.Add(new CodeAssignStatement(codeExpression, new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(GetCodeTypeReference(typeof(SolverContext))), "GetContext")));
			omlConstructor.Statements.Add(new CodeAssignStatement(modelExpr, new CodeMethodInvokeExpression(codeExpression, "CreateModel")));
			omlConstructor.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(modelExpr, "Name"), new CodePrimitiveExpression(model.Name)));
			AddDomains(model);
			AddTuples(model);
			AddParameters(model);
			AddRandomParameters(model);
			AddDecisions(model);
			AddRecourseDecisions(model);
			foreach (Constraint constraint in model.Constraints)
			{
				string name = constraint._name;
				Term term = constraint.Term;
				CodeExpression codeExpression2 = new CodePrimitiveExpression(name);
				CodeExpression codeExpression3 = ConvertTerm(term);
				CodeStatement value = new CodeExpressionStatement(new CodeMethodInvokeExpression(modelExpr, "AddConstraint", codeExpression2, codeExpression3));
				omlConstructor.Statements.Add(Comment("Add Constraint for " + name));
				omlConstructor.Statements.Add(value);
			}
			foreach (Goal goal in model.Goals)
			{
				AddGoal(goal);
			}
			string resultClassName = omlClass.Name + "Result";
			CodeMemberMethod value2 = CreateSolveMethod(model, resultClassName);
			omlClass.Members.Add(value2);
			provider.GenerateCodeFromCompileUnit(codeCompileUnit, output, providerOptions);
		}

		private static void AddImports(CodeNamespace omlNamespace)
		{
			omlNamespace.Imports.Add(new CodeNamespaceImport("System"));
			omlNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
			omlNamespace.Imports.Add(new CodeNamespaceImport("Microsoft.SolverFoundation.Services"));
		}

		private void AddGoal(Goal goal)
		{
			string name = goal.Name;
			Term term = goal.Term;
			GoalKind direction = goal.Direction;
			CodeExpression codeExpression = new CodePrimitiveExpression(name);
			CodeExpression codeExpression2 = ConvertTerm(term);
			CodeTypeReferenceExpression targetObject = new CodeTypeReferenceExpression(GetCodeTypeReference(typeof(GoalKind)));
			CodeExpression codeExpression3 = ((direction != GoalKind.Minimize) ? new CodePropertyReferenceExpression(targetObject, "Maximize") : new CodePropertyReferenceExpression(targetObject, "Minimize"));
			CodeStatement value = new CodeExpressionStatement(new CodeMethodInvokeExpression(modelExpr, "AddGoal", codeExpression, codeExpression3, codeExpression2));
			omlConstructor.Statements.Add(Comment("Add Goal for " + name));
			omlConstructor.Statements.Add(value);
		}

		private CodeMemberMethod CreateSolveMethod(Model model, string resultClassName)
		{
			CodeMemberMethod codeMemberMethod = new CodeMemberMethod();
			codeMemberMethod.Name = "Solve";
			codeMemberMethod.ReturnType = new CodeTypeReference(typeof(void));
			codeMemberMethod.Attributes = MemberAttributes.Public;
			foreach (Parameter parameter in model.Parameters)
			{
				CodeParameterDeclarationExpression codeParameterDeclarationExpression = new CodeParameterDeclarationExpression(GetSolvePropertyType(parameter), FieldName(parameter.Name));
				codeMemberMethod.Parameters.Add(codeParameterDeclarationExpression);
				CodeMethodInvokeExpression value = new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), FieldName(parameter.Name)), "SetBinding", new CodeArgumentReferenceExpression(codeParameterDeclarationExpression.Name));
				codeMemberMethod.Statements.Add(value);
			}
			CodeMethodInvokeExpression value2 = new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "context"), "Solve");
			codeMemberMethod.Statements.Add(value2);
			return codeMemberMethod;
		}

		private static Type GetClrType(Parameter parameter)
		{
			Domain domain = parameter._domain;
			return (!domain.IsNumeric) ? typeof(object) : (domain.IntRestricted ? typeof(int) : typeof(double));
		}

		private CodeTypeReference GetSolvePropertyType(Parameter parameter)
		{
			int count = parameter.IndexSets.Count;
			Type clrType = GetClrType(parameter);
			return GetSolvePropertyTypeCore(GetCodeTypeReference(clrType), count);
		}

		private CodeTypeReference GetSolvePropertyTypeCore(CodeTypeReference type, int indexCount)
		{
			if (indexCount > 2)
			{
				type = new CodeTypeReference("IEnumerable", new CodeTypeReference(GetCodeTypeReference(typeof(object)), 1));
			}
			else
			{
				for (int i = 0; i < indexCount; i++)
				{
					type = new CodeTypeReference("IEnumerable", type);
				}
			}
			return type;
		}

		private static string GetClassName(Model model)
		{
			if (!string.IsNullOrEmpty(model.Name))
			{
				return model.Name;
			}
			return "ExportedModel";
		}

		private CodeExpression ConvertTerm(Term term)
		{
			return term.Visit(this, null);
		}

		public CodeExpression Visit(ElementOfTerm term, object dummy)
		{
			CodeExpression[] array = new CodeExpression[term._tuple.Length];
			for (int i = 0; i < term._tuple.Length; i++)
			{
				array[i] = ConvertTerm(term._tuple[i]);
			}
			CodeExpression codeExpression = tuplesFields[term._tupleList];
			return new CodeMethodInvokeExpression(modelTypeExpr, "Equal", new CodeArrayCreateExpression(typeof(Term), array), codeExpression);
		}

		public CodeExpression Visit(RowTerm term, object dummy)
		{
			throw new NotImplementedException();
		}

		public CodeExpression Visit(ForEachWhereTerm term, object dummy)
		{
			CodeExpression codeExpression = SetExpr(term._set);
			Term iterator = term._iterator;
			Term valueExpression = term._valueExpression;
			Term condExpression = term._condExpression;
			CodeExpression codeExpression2 = GenerateLambda(iterator, valueExpression, term._set);
			CodeExpression codeExpression3 = GenerateLambda(iterator, condExpression, term._set);
			return new CodeMethodInvokeExpression(modelTypeExpr, "ForEachWhere", codeExpression, codeExpression2, codeExpression3);
		}

		public CodeExpression Visit(ForEachTerm term, object dummy)
		{
			CodeExpression codeExpression = SetExpr(term._set);
			Term iterator = term._iterator;
			Term valueExpression = term._valueExpression;
			CodeExpression codeExpression2 = GenerateLambda(iterator, valueExpression, term._set);
			return new CodeMethodInvokeExpression(modelTypeExpr, "ForEach", codeExpression, codeExpression2);
		}

		public CodeExpression Visit(IterationTerm term, object dummy)
		{
			return iterationFields[term];
		}

		public CodeExpression Visit(IndexTerm term, object dummy)
		{
			CodeExpression[] array = new CodeExpression[term._inputs.Length];
			for (int i = 0; i < term._inputs.Length; i++)
			{
				array[i] = ConvertTerm(term._inputs[i]);
			}
			CodeExpression targetObject = ConvertTerm((Term)term._table);
			return new CodeArrayIndexerExpression(targetObject, array);
		}

		public CodeExpression Visit(OperatorTerm term, object dummy)
		{
			CodeExpression[] array = new CodeExpression[term.Inputs.Length];
			bool flag = false;
			for (int i = 0; i < term.Inputs.Length; i++)
			{
				array[i] = ConvertTerm(term.Inputs[i]);
				if (term.Inputs[i] is ForEachTerm)
				{
					flag = true;
				}
			}
			switch (term.Operation)
			{
			case Operator.Plus:
				if (array.Length == 2 && !flag)
				{
					if (term.Inputs[1] is MinusTerm minusTerm)
					{
						CodeExpression right = ConvertTerm(minusTerm.Inputs[0]);
						return new CodeBinaryOperatorExpression(array[0], CodeBinaryOperatorType.Subtract, right);
					}
					return new CodeBinaryOperatorExpression(array[0], CodeBinaryOperatorType.Add, array[1]);
				}
				return new CodeMethodInvokeExpression(modelTypeExpr, "Sum", array);
			case Operator.Minus:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Negate", array);
			case Operator.Times:
				if (array.Length == 2 && !flag)
				{
					return new CodeBinaryOperatorExpression(array[0], CodeBinaryOperatorType.Multiply, array[1]);
				}
				return new CodeMethodInvokeExpression(modelTypeExpr, "Product", array);
			case Operator.Equal:
				if (array.Length == 2 && !flag)
				{
					return new CodeBinaryOperatorExpression(array[0], CodeBinaryOperatorType.IdentityEquality, array[1]);
				}
				return new CodeMethodInvokeExpression(modelTypeExpr, "Equal", array);
			case Operator.Unequal:
				if (array.Length == 2 && !flag)
				{
					return new CodeBinaryOperatorExpression(array[0], CodeBinaryOperatorType.IdentityInequality, array[1]);
				}
				return new CodeMethodInvokeExpression(modelTypeExpr, "AllDifferent", array);
			case Operator.Greater:
				if (array.Length == 2 && !flag)
				{
					return new CodeBinaryOperatorExpression(array[0], CodeBinaryOperatorType.GreaterThan, array[1]);
				}
				return new CodeMethodInvokeExpression(modelTypeExpr, "Greater", array);
			case Operator.GreaterEqual:
				if (array.Length == 2 && !flag)
				{
					return new CodeBinaryOperatorExpression(array[0], CodeBinaryOperatorType.GreaterThanOrEqual, array[1]);
				}
				return new CodeMethodInvokeExpression(modelTypeExpr, "GreaterEqual", array);
			case Operator.Less:
				if (array.Length == 2 && !flag)
				{
					return new CodeBinaryOperatorExpression(array[0], CodeBinaryOperatorType.LessThan, array[1]);
				}
				return new CodeMethodInvokeExpression(modelTypeExpr, "Less", array);
			case Operator.LessEqual:
				if (array.Length == 2 && !flag)
				{
					return new CodeBinaryOperatorExpression(array[0], CodeBinaryOperatorType.LessThanOrEqual, array[1]);
				}
				return new CodeMethodInvokeExpression(modelTypeExpr, "LessEqual", array);
			case Operator.Quotient:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Quotient", array);
			case Operator.Power:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Power", array);
			case Operator.Abs:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Abs", array);
			case Operator.And:
				if (array.Length == 2 && !flag)
				{
					return new CodeBinaryOperatorExpression(array[0], CodeBinaryOperatorType.BitwiseAnd, array[1]);
				}
				return new CodeMethodInvokeExpression(modelTypeExpr, "And", array);
			case Operator.Or:
				if (array.Length == 2 && !flag)
				{
					return new CodeBinaryOperatorExpression(array[0], CodeBinaryOperatorType.BitwiseOr, array[1]);
				}
				return new CodeMethodInvokeExpression(modelTypeExpr, "Or", array);
			case Operator.Not:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Not", array);
			case Operator.Max:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Max", array);
			case Operator.Min:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Min", array);
			case Operator.Cos:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Cos", array);
			case Operator.Sin:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Sin", array);
			case Operator.Tan:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Tan", array);
			case Operator.ArcCos:
				return new CodeMethodInvokeExpression(modelTypeExpr, "ArcCos", array);
			case Operator.ArcSin:
				return new CodeMethodInvokeExpression(modelTypeExpr, "ArcSin", array);
			case Operator.ArcTan:
				return new CodeMethodInvokeExpression(modelTypeExpr, "ArcTan", array);
			case Operator.Cosh:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Cosh", array);
			case Operator.Sinh:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Sinh", array);
			case Operator.Tanh:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Tanh", array);
			case Operator.Exp:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Exp", array);
			case Operator.Log:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Log", array);
			case Operator.Log10:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Log10", array);
			case Operator.Sqrt:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Sqrt", array);
			case Operator.Ceiling:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Ceiling", array);
			case Operator.Floor:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Floor", array);
			case Operator.If:
				return new CodeMethodInvokeExpression(modelTypeExpr, "If", array);
			case Operator.Sos1:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Sos1", array);
			case Operator.Sos1Row:
				throw new NotImplementedException();
			case Operator.Sos2:
				return new CodeMethodInvokeExpression(modelTypeExpr, "Sos2", array);
			case Operator.Sos2Row:
				throw new NotImplementedException();
			default:
				throw new MsfException(Resources.InternalError);
			}
		}

		public CodeExpression Visit(IdentityTerm term, object dummy)
		{
			return ConvertTerm(term._input);
		}

		public CodeExpression Visit(EnumeratedConstantTerm term, object dummy)
		{
			return new CodePrimitiveExpression(term.EnumeratedDomain.EnumeratedNames[(int)term._value]);
		}

		public CodeExpression Visit(BoolConstantTerm term, object dummy)
		{
			if (!(term._value != 0))
			{
				return new CodePrimitiveExpression(false);
			}
			return new CodePrimitiveExpression(true);
		}

		public CodeExpression Visit(StringConstantTerm term, object dummy)
		{
			return new CodePrimitiveExpression(term._value);
		}

		public CodeExpression Visit(NamedConstantTerm term, object dummy)
		{
			throw new NotImplementedException();
		}

		public CodeExpression Visit(ConstantTerm term, object dummy)
		{
			return RationalExpr(term._value);
		}

		public CodeExpression Visit(RandomParameter term, object dummy)
		{
			return randomParameterFields[term];
		}

		public CodeExpression Visit(Parameter term, object dummy)
		{
			return parameterFields[term];
		}

		public CodeExpression Visit(RecourseDecision term, object dummy)
		{
			return recourseDecisionFields[term];
		}

		public CodeExpression Visit(Decision term, object dummy)
		{
			return decisionFields[term];
		}

		public CodeExpression Visit(Tuples term, object dummy)
		{
			throw new NotImplementedException();
		}

		private CodeExpression GenerateLambda(Term iterator, Term valueExpression, Set set)
		{
			string lambdaArgName = GetLambdaArgName(set);
			CodeVariableReferenceExpression value = new CodeVariableReferenceExpression(lambdaArgName);
			iterationFields[iterator] = value;
			CodeExpression expression = ConvertTerm(valueExpression);
			string text = null;
			using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
			{
				provider.GenerateCodeFromExpression(expression, stringWriter, providerOptions);
				text = stringWriter.ToString();
			}
			string value2 = "(" + lambdaArgName + ") => " + text;
			return new CodeSnippetExpression(value2);
		}

		private string GetLambdaArgName(Set set)
		{
			string text = "lambdaArg";
			if (set != null)
			{
				string name = set.Name;
				if (name.Length > 0 && char.IsLetter(name[0]))
				{
					return char.ToLowerInvariant(name[0]) + "_" + nextLambda++;
				}
			}
			return text + nextLambda++;
		}

		private void AddRecourseDecisions(Model model)
		{
			CodeTypeReference codeTypeReference = GetCodeTypeReference(typeof(RecourseDecision));
			foreach (RecourseDecision recourseDecision in model.RecourseDecisions)
			{
				CodeExpression value = AddDecision(codeTypeReference, recourseDecision._name, recourseDecision._indexSets, recourseDecision._domain);
				recourseDecisionFields[recourseDecision] = value;
			}
		}

		private void AddDecisions(Model model)
		{
			CodeTypeReference codeTypeReference = GetCodeTypeReference(typeof(Decision));
			foreach (Decision decision in model.Decisions)
			{
				CodeExpression value = AddDecision(codeTypeReference, decision._name, decision._indexSets, decision._domain);
				decisionFields[decision] = value;
			}
		}

		private CodeExpression AddDecision(CodeTypeReference decisionType, string name, Set[] indexSets, Domain domain)
		{
			string fieldName = FieldName(name);
			CodeExpression codeExpression = AddPropertyWithBackingField(name, fieldName, decisionType);
			CodeExpression[] array = new CodeExpression[indexSets.Length + 2];
			array[0] = DomainExpr(domain);
			array[1] = new CodePrimitiveExpression(name);
			for (int i = 0; i < indexSets.Length; i++)
			{
				Set expr = indexSets[i];
				CodeExpression codeExpression2 = SetExpr(expr);
				array[i + 2] = codeExpression2;
			}
			CodeAssignStatement value = new CodeAssignStatement(codeExpression, new CodeObjectCreateExpression(decisionType, array));
			CodeMethodInvokeExpression value2 = new CodeMethodInvokeExpression(modelExpr, "AddDecision", codeExpression);
			omlConstructor.Statements.Add(Comment(string.Format(CultureInfo.InvariantCulture, "Create a {0} for {1}", new object[2] { decisionType.BaseType, name })));
			omlConstructor.Statements.Add(value);
			omlConstructor.Statements.Add(value2);
			return codeExpression;
		}

		private void AddRandomParameters(Model model)
		{
			foreach (RandomParameter randomParameter in model.RandomParameters)
			{
				string name = randomParameter._name;
				string fieldName = FieldName(name);
				Type type = randomParameter.GetType();
				CodeTypeReference codeTypeReference = GetCodeTypeReference(type);
				CodeExpression codeExpression = AddPropertyWithBackingField(name, fieldName, codeTypeReference);
				randomParameterFields[randomParameter] = codeExpression;
				int num = randomParameter._indexSets.Length + 1;
				bool flag = !randomParameter.NeedsBind && randomParameter.Binding == null;
				if (flag)
				{
					num += GetNumberOfArguments(randomParameter);
				}
				CodeExpression[] array = new CodeExpression[num];
				array[0] = new CodePrimitiveExpression(randomParameter._name);
				if (flag)
				{
					FillArgumentsForRandomParameterCtr(randomParameter, type, array);
				}
				int num2 = num - randomParameter._indexSets.Length;
				for (int i = 0; i < randomParameter._indexSets.Length; i++)
				{
					Set expr = randomParameter._indexSets[i];
					CodeExpression codeExpression2 = SetExpr(expr);
					array[i + num2] = codeExpression2;
				}
				CodeAssignStatement value = new CodeAssignStatement(codeExpression, new CodeObjectCreateExpression(codeTypeReference, array));
				CodeMethodInvokeExpression value2 = new CodeMethodInvokeExpression(modelExpr, "AddParameter", codeExpression);
				omlConstructor.Statements.Add(Comment(string.Format(CultureInfo.InvariantCulture, "Create a {0} for {1}", new object[2] { codeTypeReference.BaseType, randomParameter._name })));
				omlConstructor.Statements.Add(value);
				omlConstructor.Statements.Add(value2);
			}
		}

		private int GetNumberOfArguments(RandomParameter randomParameter)
		{
			if (randomParameter is UniformDistributionParameter || randomParameter is DiscreteUniformDistributionParameter || randomParameter is LogNormalDistributionParameter || randomParameter is NormalDistributionParameter || randomParameter is BinomialDistributionParameter)
			{
				return 2;
			}
			if (randomParameter is ExponentialDistributionParameter || randomParameter is GeometricDistributionParameter || randomParameter is ScenariosParameter)
			{
				return 1;
			}
			throw new NotImplementedException();
		}

		private void FillArgumentsForRandomParameterCtr(RandomParameter randomParameter, Type randomType, CodeExpression[] createStatementParameters)
		{
			UnivariateDistribution distribution = randomParameter.ValueTable.Values.First().Distribution;
			if (randomType == typeof(UniformDistributionParameter))
			{
				ContinuousUniformUnivariateDistribution continuousUniformUnivariateDistribution = distribution as ContinuousUniformUnivariateDistribution;
				createStatementParameters[1] = new CodePrimitiveExpression(continuousUniformUnivariateDistribution.LowerBound);
				createStatementParameters[2] = new CodePrimitiveExpression(continuousUniformUnivariateDistribution.UpperBound);
				return;
			}
			if (randomType == typeof(DiscreteUniformDistributionParameter))
			{
				DiscreteUniformUnivariateDistribution discreteUniformUnivariateDistribution = distribution as DiscreteUniformUnivariateDistribution;
				createStatementParameters[1] = new CodePrimitiveExpression(discreteUniformUnivariateDistribution.LowerBound);
				createStatementParameters[2] = new CodePrimitiveExpression(discreteUniformUnivariateDistribution.UpperBound);
				return;
			}
			if (randomType == typeof(LogNormalDistributionParameter))
			{
				LogNormalUnivariateDistribution logNormalUnivariateDistribution = distribution as LogNormalUnivariateDistribution;
				createStatementParameters[1] = new CodePrimitiveExpression(logNormalUnivariateDistribution.MeanLog);
				createStatementParameters[2] = new CodePrimitiveExpression(logNormalUnivariateDistribution.StdLog);
				return;
			}
			if (randomType == typeof(NormalDistributionParameter))
			{
				NormalUnivariateDistribution normalUnivariateDistribution = distribution as NormalUnivariateDistribution;
				createStatementParameters[1] = new CodePrimitiveExpression(normalUnivariateDistribution.Mean);
				createStatementParameters[2] = new CodePrimitiveExpression(normalUnivariateDistribution.StandardDeviation);
				return;
			}
			if (randomType == typeof(BinomialDistributionParameter))
			{
				BinomialUnivariateDistribution binomialUnivariateDistribution = distribution as BinomialUnivariateDistribution;
				createStatementParameters[1] = new CodePrimitiveExpression(binomialUnivariateDistribution.NumberOfTrials);
				createStatementParameters[2] = new CodePrimitiveExpression(binomialUnivariateDistribution.SuccessProbability);
				return;
			}
			if (randomType == typeof(ExponentialDistributionParameter))
			{
				ExponentialUnivariateDistribution exponentialUnivariateDistribution = distribution as ExponentialUnivariateDistribution;
				createStatementParameters[1] = new CodePrimitiveExpression(exponentialUnivariateDistribution.Rate);
				return;
			}
			if (randomType == typeof(GeometricDistributionParameter))
			{
				GeometricUnivariateDistribution geometricUnivariateDistribution = distribution as GeometricUnivariateDistribution;
				createStatementParameters[1] = new CodePrimitiveExpression(geometricUnivariateDistribution.SuccessProbability);
				return;
			}
			if (randomType == typeof(ScenariosParameter))
			{
				IEnumerable<Scenario> scenarios = randomParameter.ValueTable.Values.First().Scenarios;
				CodeExpression[] array = new CodeExpression[randomParameter.ValueTable.Values.First().ScenariosCount];
				int num = 0;
				Type typeFromHandle = typeof(Scenario);
				foreach (Scenario item in scenarios)
				{
					array[num] = new CodeObjectCreateExpression(typeFromHandle, new CodePrimitiveExpression(item.Probability.ToDouble()), new CodePrimitiveExpression(item.Value.ToDouble()));
					num++;
				}
				createStatementParameters[1] = new CodeArrayCreateExpression(typeFromHandle, array);
				return;
			}
			throw new NotImplementedException();
		}

		private void AddDomains(Model model)
		{
			HashSet<Domain> hashSet = new HashSet<Domain>();
			foreach (Parameter parameter in model.Parameters)
			{
				CollectEnumeratedDomain(hashSet, parameter._domain);
				Set[] indexSets = parameter._indexSets;
				foreach (Set set in indexSets)
				{
					CollectEnumeratedDomain(hashSet, set._domain);
				}
			}
			foreach (RandomParameter randomParameter in model.RandomParameters)
			{
				CollectEnumeratedDomain(hashSet, randomParameter._domain);
				Set[] indexSets2 = randomParameter._indexSets;
				foreach (Set set2 in indexSets2)
				{
					CollectEnumeratedDomain(hashSet, set2._domain);
				}
			}
			foreach (Decision decision in model.Decisions)
			{
				CollectEnumeratedDomain(hashSet, decision._domain);
				Set[] indexSets3 = decision._indexSets;
				foreach (Set set3 in indexSets3)
				{
					CollectEnumeratedDomain(hashSet, set3._domain);
				}
			}
			foreach (RecourseDecision recourseDecision in model.RecourseDecisions)
			{
				CollectEnumeratedDomain(hashSet, recourseDecision._domain);
				Set[] indexSets4 = recourseDecision._indexSets;
				foreach (Set set4 in indexSets4)
				{
					CollectEnumeratedDomain(hashSet, set4._domain);
				}
			}
			foreach (Tuples tuple in model.Tuples)
			{
				Domain[] domains = tuple._domains;
				foreach (Domain domain in domains)
				{
					if (domain.EnumeratedNames != null)
					{
						hashSet.Add(domain);
					}
				}
			}
			int num = 1;
			foreach (Domain item in hashSet)
			{
				string text = item.Name ?? ("enumDomain" + num++);
				string fieldName = FieldName(text);
				CodeExpression codeExpression = AddPropertyWithBackingField(text, fieldName, GetCodeTypeReference(typeof(Domain)));
				domainFields[item] = codeExpression;
				string[] enumeratedNames = item.EnumeratedNames;
				CodeExpression[] array = new CodeExpression[enumeratedNames.Length];
				for (int n = 0; n < enumeratedNames.Length; n++)
				{
					array[n] = new CodePrimitiveExpression(enumeratedNames[n]);
				}
				CodeAssignStatement value = new CodeAssignStatement(codeExpression, new CodeMethodInvokeExpression(domainTypeExpr, "Enum", array));
				omlConstructor.Statements.Add(Comment("Create a Domain for " + text));
				omlConstructor.Statements.Add(value);
				if (item.Name != null)
				{
					CodeAssignStatement value2 = new CodeAssignStatement(new CodeFieldReferenceExpression(codeExpression, "Name"), new CodePrimitiveExpression(text));
					omlConstructor.Statements.Add(value2);
				}
			}
		}

		private static void CollectEnumeratedDomain(HashSet<Domain> enumDomains, Domain d)
		{
			if (d.EnumeratedNames != null)
			{
				enumDomains.Add(d);
			}
		}

		private void AddTuples(Model model)
		{
			CodeTypeReference codeTypeReference = GetCodeTypeReference(typeof(Tuples));
			foreach (Tuples tuple in model.Tuples)
			{
				string name = tuple.Name;
				string fieldName = FieldName(name);
				CodeExpression codeExpression = AddPropertyWithBackingField(name, fieldName, codeTypeReference);
				tuplesFields[tuple] = codeExpression;
				CodeAssignStatement value = new CodeAssignStatement(codeExpression, new CodeObjectCreateExpression(codeTypeReference, new CodePrimitiveExpression(name), new CodeArrayCreateExpression(typeof(Domain), tuple._domains.Select((Domain d) => DomainExpr(d)).ToArray())));
				CodeMethodInvokeExpression value2 = new CodeMethodInvokeExpression(modelExpr, "AddTuples", codeExpression);
				omlConstructor.Statements.Add(Comment("Create a Tuples for " + name));
				omlConstructor.Statements.Add(value);
				omlConstructor.Statements.Add(value2);
			}
		}

		private void AddParameters(Model model)
		{
			CodeTypeReference codeTypeReference = GetCodeTypeReference(typeof(Parameter));
			foreach (Parameter parameter in model.Parameters)
			{
				string name = parameter._name;
				string fieldName = FieldName(name);
				CodeExpression codeExpression = AddPropertyWithBackingField(name, fieldName, codeTypeReference);
				parameterFields[parameter] = codeExpression;
				CodeExpression[] array = new CodeExpression[parameter._indexSets.Length + 2];
				array[0] = DomainExpr(parameter._domain);
				array[1] = new CodePrimitiveExpression(parameter._name);
				for (int i = 0; i < parameter._indexSets.Length; i++)
				{
					Set expr = parameter._indexSets[i];
					CodeExpression codeExpression2 = SetExpr(expr);
					array[i + 2] = codeExpression2;
				}
				CodeAssignStatement value = new CodeAssignStatement(codeExpression, new CodeObjectCreateExpression(codeTypeReference, array));
				CodeMethodInvokeExpression value2 = new CodeMethodInvokeExpression(modelExpr, "AddParameter", codeExpression);
				omlConstructor.Statements.Add(Comment("Create a Parameter for " + parameter._name));
				omlConstructor.Statements.Add(value);
				omlConstructor.Statements.Add(value2);
			}
		}

		private CodeExpression SetExpr(Set set)
		{
			if (setFields.TryGetValue(set, out var value))
			{
				return value;
			}
			if (set.IsConstant)
			{
				if (set._fixedValues == null)
				{
					CodeExpression codeExpression = ConvertTerm(set.FixedStart);
					CodeExpression codeExpression2 = ConvertTerm(set.FixedLimit);
					CodeExpression codeExpression3 = ConvertTerm(set.FixedStep);
					return new CodeObjectCreateExpression(GetCodeTypeReference(typeof(Set)), codeExpression, codeExpression2, codeExpression3);
				}
				CodeExpression[] initializers = set._fixedValues.Select((Term t) => ConvertTerm(t)).ToArray();
				return new CodeObjectCreateExpression(GetCodeTypeReference(typeof(Set)), new CodeArrayCreateExpression(GetCodeTypeReference(typeof(Term)), initializers));
			}
			string name = set.Name;
			string fieldName = FieldName(name);
			CodeExpression codeExpression5 = (setFields[set] = AddPropertyWithBackingField(name, fieldName, GetCodeTypeReference(typeof(Set))));
			value = codeExpression5;
			CodeAssignStatement value2 = new CodeAssignStatement(value, new CodeObjectCreateExpression(GetCodeTypeReference(typeof(Set)), DomainExpr(set._domain), new CodePrimitiveExpression(name)));
			omlConstructor.Statements.Add(Comment("Create a Set for " + name));
			omlConstructor.Statements.Add(value2);
			return value;
		}

		private static CodeExpression RationalExpr(Rational rat)
		{
			return new CodePrimitiveExpression((double)rat);
		}

		private CodeExpression DomainExpr(Domain domain)
		{
			if (domain is AnyDomain)
			{
				return new CodePropertyReferenceExpression(domainTypeExpr, "Any");
			}
			if (domain is BooleanDomain)
			{
				return new CodePropertyReferenceExpression(domainTypeExpr, "Boolean");
			}
			if (domain is NumericRangeDomain)
			{
				if (domain.IntRestricted)
				{
					if (domain.MinValue == Rational.NegativeInfinity && domain.MaxValue == Rational.PositiveInfinity)
					{
						return new CodePropertyReferenceExpression(domainTypeExpr, "Integer");
					}
					if (domain.MinValue == 0 && domain.MaxValue == Rational.PositiveInfinity)
					{
						return new CodePropertyReferenceExpression(domainTypeExpr, "IntegerNonnegative");
					}
					if (domain.ValidValues == null)
					{
						return new CodeMethodInvokeExpression(domainTypeExpr, "IntegerRange", RationalExpr(domain.MinValue), RationalExpr(domain.MaxValue));
					}
				}
				else
				{
					if (domain.MinValue == Rational.NegativeInfinity && domain.MaxValue == Rational.PositiveInfinity)
					{
						return new CodePropertyReferenceExpression(domainTypeExpr, "Real");
					}
					if (domain.MinValue == 0 && domain.MaxValue == Rational.PositiveInfinity)
					{
						return new CodePropertyReferenceExpression(domainTypeExpr, "RealNonnegative");
					}
					if (domain.ValidValues == null)
					{
						return new CodeMethodInvokeExpression(domainTypeExpr, "RealRange", RationalExpr(domain.MinValue), RationalExpr(domain.MaxValue));
					}
				}
			}
			if (domain is EnumDomain)
			{
				return domainFields[domain];
			}
			throw new NotImplementedException();
		}

		private static string FieldName(string name)
		{
			if (char.IsUpper(name[0]))
			{
				StringBuilder stringBuilder = new StringBuilder(name);
				stringBuilder[0] = char.ToLower(name[0], CultureInfo.InvariantCulture);
				return stringBuilder.ToString();
			}
			return "_" + name;
		}

		private static CodeCommentStatement Comment(string comment)
		{
			return new CodeCommentStatement(comment);
		}

		private CodeExpression AddPropertyWithBackingField(string propertyName, string fieldName, CodeTypeReference type)
		{
			return AddPropertyWithBackingField(omlClass, propertyName, fieldName, type);
		}

		private static CodeExpression AddPropertyWithBackingField(CodeTypeDeclaration omlClass, string propertyName, string fieldName, CodeTypeReference type)
		{
			CodeMemberField codeMemberField = new CodeMemberField();
			omlClass.Members.Add(codeMemberField);
			codeMemberField.Name = fieldName;
			codeMemberField.Type = type;
			codeMemberField.Attributes = MemberAttributes.Private;
			CodeMemberProperty codeMemberProperty = new CodeMemberProperty();
			omlClass.Members.Add(codeMemberProperty);
			codeMemberProperty.Name = propertyName;
			codeMemberProperty.Type = type;
			codeMemberProperty.Attributes = MemberAttributes.Public;
			codeMemberProperty.HasGet = true;
			CodeFieldReferenceExpression expression = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName);
			CodeMethodReturnStatement value = new CodeMethodReturnStatement(expression);
			codeMemberProperty.GetStatements.Add(value);
			return new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				provider.Dispose();
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
