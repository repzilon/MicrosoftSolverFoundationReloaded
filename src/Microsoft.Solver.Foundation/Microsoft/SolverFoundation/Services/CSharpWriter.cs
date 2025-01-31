using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.SolverFoundation.Services
{
    internal sealed class CSharpWriter : ITermVisitor<ExpressionSyntax, object>, IDisposable
    {
        private readonly Dictionary<Decision, ExpressionSyntax> decisionFields = new Dictionary<Decision, ExpressionSyntax>();
        private readonly Dictionary<Parameter, ExpressionSyntax> parameterFields = new Dictionary<Parameter, ExpressionSyntax>();
        private readonly Dictionary<RecourseDecision, ExpressionSyntax> recourseDecisionFields = new Dictionary<RecourseDecision, ExpressionSyntax>();
        private readonly Dictionary<RandomParameter, ExpressionSyntax> randomParameterFields = new Dictionary<RandomParameter, ExpressionSyntax>();
        private readonly Dictionary<Domain, ExpressionSyntax> domainFields = new Dictionary<Domain, ExpressionSyntax>();
        private readonly Dictionary<Tuples, ExpressionSyntax> tuplesFields = new Dictionary<Tuples, ExpressionSyntax>();
        private readonly Dictionary<Set, ExpressionSyntax> setFields = new Dictionary<Set, ExpressionSyntax>();
        private readonly Dictionary<Term, ExpressionSyntax> iterationFields = new Dictionary<Term, ExpressionSyntax>();

        private ClassDeclarationSyntax omlClass;
        private ConstructorDeclarationSyntax omlConstructor;
        private ExpressionSyntax modelExpr;
        private int nextLambda = 1;

        private readonly bool _useFullyQualifiedTypeNames;
        private Dictionary<Type, TypeSyntax> _typeToRef = new Dictionary<Type, TypeSyntax>();


        public CSharpWriter() : this(useFullyQualifiedTypeNames: false) { }

        public CSharpWriter(bool useFullyQualifiedTypeNames)
        {
            _useFullyQualifiedTypeNames = useFullyQualifiedTypeNames;
        }

        private TypeSyntax GetTypeSyntax(Type type)
        {
            if (!_typeToRef.TryGetValue(type, out var value))
            {
                value = SyntaxFactory.ParseTypeName(_useFullyQualifiedTypeNames ? type.FullName : type.Name);
                _typeToRef[type] = value;
            }
            return value;
        }

        internal void Write(Model model, TextWriter output)
        {
            var namespaceDecl = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("OmlExport"));
            var usingDirectives = new List<UsingDirectiveSyntax>
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.SolverFoundation.Services"))
            };

            namespaceDecl = namespaceDecl.AddUsings(usingDirectives.ToArray());

            omlClass = SyntaxFactory.ClassDeclaration(GetClassName(model))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            omlConstructor = SyntaxFactory.ConstructorDeclaration(omlClass.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            var statements = new List<StatementSyntax>();
            modelExpr = AddPropertyWithBackingField("Model", "model", GetTypeSyntax(typeof(Model)));

            var contextVar = AddPropertyWithBackingField("Context", "context", GetTypeSyntax(typeof(SolverContext)));
            statements.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, contextVar, SyntaxFactory.IdentifierName("SolverContext.GetContext()"))));
            statements.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, modelExpr, SyntaxFactory.IdentifierName("context.CreateModel()"))));

            omlConstructor = omlConstructor.AddBodyStatements(statements.ToArray());
            omlClass = omlClass.AddMembers(omlConstructor);

            var codeCompileUnit = SyntaxFactory.CompilationUnit().AddUsings(usingDirectives.ToArray()).AddMembers(namespaceDecl);
            var formattedCode = codeCompileUnit.NormalizeWhitespace().ToFullString();
            output.Write(formattedCode);
        }

        private static string GetClassName(Model model)
        {
            return !string.IsNullOrEmpty(model.Name) ? model.Name : "ExportedModel";
        }

        // Add property and backing field to class
        //private static ExpressionSyntax AddPropertyWithBackingField(string propertyName, string fieldName, TypeSyntax type)
        //{
        //    var propertyDeclaration = SyntaxFactory.PropertyDeclaration(type, propertyName)
        //        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
        //        .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
        //        {
        //            SyntaxFactory.AccessorDeclaration(SyntaxFactory.Token(SyntaxKind.GetKeyword))
        //        })));

        //    return SyntaxFactory.IdentifierName(fieldName);
        //}

        private ExpressionSyntax AddPropertyWithBackingField(string propertyName, string fieldName, TypeSyntax type)
        {
            var fieldDecl = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(type)
                .AddVariables(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(fieldName))));

            var propertyDecl = SyntaxFactory.PropertyDeclaration(type, SyntaxFactory.Identifier(propertyName))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithAccessorList(SyntaxFactory.AccessorList(
                    SyntaxFactory.List(new AccessorDeclarationSyntax[] {
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    })));

            omlClass = omlClass.AddMembers(fieldDecl, propertyDecl);
            return SyntaxFactory.IdentifierName(fieldName);
        }

        public ExpressionSyntax Visit(ElementOfTerm term, object dummy)
        {
            var tupleExpressions = new List<ExpressionSyntax>();
            foreach (var item in term._tuple)
            {
                tupleExpressions.Add(ConvertTerm(item));
            }
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, modelExpr, SyntaxFactory.IdentifierName("Equal")))
                .AddArgumentListArguments(SyntaxFactory.Argument(
                    SyntaxFactory.ArrayCreationExpression(
                        SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName("Term"))
                        .AddRankSpecifiers(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression()))))
                    .WithInitializer(SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SeparatedList(tupleExpressions)))));
        }

        public ExpressionSyntax Visit(IdentityTerm term, object dummy)
        {
            return ConvertTerm(term._input);
        }

        public ExpressionSyntax Visit(EnumeratedConstantTerm term, object dummy)
        {
            var enumeratedName = term.EnumeratedDomain.EnumeratedNames[(int)term._value];
            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(enumeratedName));
        }

        public ExpressionSyntax Visit(BoolConstantTerm term, object dummy)
        {
            return SyntaxFactory.LiteralExpression(term._value != 0 ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
        }

        public ExpressionSyntax Visit(NamedConstantTerm term, object dummy)
        {
            // Implement logic or throw NotImplementedException if necessary
            throw new NotImplementedException();
        }

        public ExpressionSyntax Visit(ConstantTerm term, object dummy)
        {
            return RationalExpr(term._value);
        }

        public ExpressionSyntax Visit(RandomParameter term, object dummy)
        {
            if (randomParameterFields.TryGetValue(term, out var expr))
            {
                return expr;
            }
            // Fallback logic if needed, e.g., create a new expression
            return SyntaxFactory.IdentifierName("randomParameter");
        }

        public ExpressionSyntax Visit(Parameter term, object dummy)
        {
            if (parameterFields.TryGetValue(term, out var expr))
            {
                return expr;
            }
            // Fallback logic if needed
            return SyntaxFactory.IdentifierName("parameter");
        }

        public ExpressionSyntax Visit(RecourseDecision term, object dummy)
        {
            if (recourseDecisionFields.TryGetValue(term, out var expr))
            {
                return expr;
            }
            // Fallback logic if needed
            return SyntaxFactory.IdentifierName("recourseDecision");
        }

        public ExpressionSyntax Visit(Decision term, object dummy)
        {
            if (decisionFields.TryGetValue(term, out var expr))
            {
                return expr;
            }
            // Fallback logic if needed
            return SyntaxFactory.IdentifierName("decision");
        }

        public ExpressionSyntax Visit(Tuples term, object dummy)
        {
            // Implement logic for Tuples or throw NotImplementedException
            throw new NotImplementedException();
        }

        private ExpressionSyntax ConvertTerm(object input)
        {
            // Convert term logic here
            return SyntaxFactory.IdentifierName(input.ToString());
        }

        private ExpressionSyntax RationalExpr(object value)
        {
            // Implement Rational expression handling
            return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value.ToString()));
        }

        private ExpressionSyntax GenerateLambda(Term iterator, Term valueExpression, Set set)
        {
            string lambdaArgName = GetLambdaArgName(set);
            var value = SyntaxFactory.IdentifierName(lambdaArgName);
            iterationFields[iterator] = value;
            var expression = ConvertTerm(valueExpression);
            var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            var formattedExpression = expression.NormalizeWhitespace().ToFullString();
            var lambdaExpression = SyntaxFactory.ParenthesizedLambdaExpression(
                SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Parameter(SyntaxFactory.Identifier(lambdaArgName)))),
                expression);
            return lambdaExpression;
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
            var codeTypeReference = GetCodeTypeReference(typeof(RecourseDecision));
            foreach (var recourseDecision in model.RecourseDecisions)
            {
                var value = AddDecision(codeTypeReference, recourseDecision._name, recourseDecision._indexSets, recourseDecision._domain);
                recourseDecisionFields[recourseDecision] = value;
            }
        }

        private void AddDecisions(Model model)
        {
            var codeTypeReference = GetCodeTypeReference(typeof(Decision));
            foreach (var decision in model.Decisions)
            {
                var value = AddDecision(codeTypeReference, decision._name, decision._indexSets, decision._domain);
                decisionFields[decision] = value;
            }
        }

        private ExpressionSyntax AddDecision(TypeSyntax decisionType, string name, Set[] indexSets, Domain domain)
        {
            string fieldName = FieldName(name);
            var fieldReference = AddPropertyWithBackingField(name, fieldName, decisionType);
            var arguments = new List<ExpressionSyntax>
            {
                DomainExpr(domain),
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(name))
            };
            arguments.AddRange(indexSets.Select(set => SetExpr(set)));

            var constructorCall = SyntaxFactory.ObjectCreationExpression((TypeSyntax)decisionType)
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments.Select(arg => SyntaxFactory.Argument(arg)))));

            var assignmentStatement = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, fieldReference, constructorCall);
            omlConstructor = omlConstructor.AddBodyStatements(SyntaxFactory.ExpressionStatement(assignmentStatement));
            return fieldReference;
        }

        private void AddRandomParameters(Model model)
        {
            foreach (var randomParameter in model.RandomParameters)
            {
                string name = randomParameter._name;
                string fieldName = FieldName(name);
                Type type = randomParameter.GetType();
                var typeRef = GetCodeTypeReference(type);
                var fieldReference = AddPropertyWithBackingField(name, fieldName, typeRef);
                randomParameterFields[randomParameter] = fieldReference;

                // Build argument list for random parameter
                var arguments = new List<ExpressionSyntax> { SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(randomParameter._name)) };
                if (!randomParameter.NeedsBind && randomParameter.Binding == null)
                {
                    FillArgumentsForRandomParameterCtr(randomParameter, type, arguments);
                }
                foreach (var set in randomParameter._indexSets)
                {
                    arguments.Add(SetExpr(set));
                }

                var constructorCall = SyntaxFactory.ObjectCreationExpression(typeRef)
                    .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments.Select(arg => SyntaxFactory.Argument(arg)))));
                var assignmentStatement = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, fieldReference, constructorCall);
                omlConstructor = omlConstructor.AddBodyStatements(SyntaxFactory.ExpressionStatement(assignmentStatement));
            }
        }

        private static void FillArgumentsForRandomParameterCtr(RandomParameter randomParameter, Type randomType, List<ExpressionSyntax> createStatementParameters)
        {
            // Fill arguments for different types of random parameter distributions.
            // Specific logic for each type of distribution can be added here.
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release resources
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // Helper method to convert terms to Roslyn expressions
        private ExpressionSyntax ConvertTerm(Term term)
        {
            throw new NotImplementedException();
        }

        // Helper method to get type reference
        private static TypeSyntax GetCodeTypeReference(Type type)
        {
            return SyntaxFactory.ParseTypeName(type.FullName);
        }

        // Helper method to get field name
        private static string FieldName(string name)
        {
            if (char.IsUpper(name[0]))
            {
                var stringBuilder = new StringBuilder(name);
                stringBuilder[0] = char.ToLower(name[0], CultureInfo.InvariantCulture);
                return stringBuilder.ToString();
            }
            return "_" + name;
        }

        // Helper method to create domain expression
        private ExpressionSyntax DomainExpr(Domain domain)
        {
            throw new NotImplementedException();
        }

        // Helper method to create set expression
        private ExpressionSyntax SetExpr(Set set)
        {
            throw new NotImplementedException();
        }

        public ExpressionSyntax Visit(StringConstantTerm term, object arg)
        {
            throw new NotImplementedException();
        }

        public ExpressionSyntax Visit(OperatorTerm term, object arg)
        {
            throw new NotImplementedException();
        }

        public ExpressionSyntax Visit(IndexTerm term, object arg)
        {
            throw new NotImplementedException();
        }

        public ExpressionSyntax Visit(IterationTerm term, object arg)
        {
            throw new NotImplementedException();
        }

        public ExpressionSyntax Visit(ForEachTerm term, object arg)
        {
            throw new NotImplementedException();
        }

        public ExpressionSyntax Visit(ForEachWhereTerm term, object arg)
        {
            throw new NotImplementedException();
        }

        public ExpressionSyntax Visit(RowTerm term, object arg)
        {
            throw new NotImplementedException();
        }
    }
}
