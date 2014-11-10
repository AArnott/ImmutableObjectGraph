namespace ImmutableObjectGraph.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.ImmutableObjectGraph_SFG;
    using Validation;

    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateImmutableAttribute : CodeGenerationAttribute
    {
        private static readonly TypeSyntax IdentityFieldTypeSyntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword));
        private static readonly TypeSyntax IdentityFieldOptionalTypeSyntax = SyntaxFactory.GenericName(SyntaxFactory.Identifier(nameof(Optional)), SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(IdentityFieldTypeSyntax)));
        private static readonly IdentifierNameSyntax IdentityParameterName = SyntaxFactory.IdentifierName("identity");
        private static readonly ParameterSyntax RequiredIdentityParameter = SyntaxFactory.Parameter(IdentityParameterName.Identifier).WithType(IdentityFieldTypeSyntax);
        private static readonly ParameterSyntax OptionalIdentityParameter = Optional(RequiredIdentityParameter);

        public GenerateImmutableAttribute()
        {
        }

        public override async Task<MemberDeclarationSyntax> GenerateAsync(MemberDeclarationSyntax applyTo, Document document, IProgressAndErrors progress, CancellationToken cancellationToken)
        {
            var inputSemanticModel = await document.GetSemanticModelAsync();
            var classDeclaration = (ClassDeclarationSyntax)applyTo;
            bool isAbstract = classDeclaration.Modifiers.Any(m => m.IsContextualKind(SyntaxKind.AbstractKeyword));

            ValidateInput(classDeclaration, document, progress);

            var fields = GetFields(classDeclaration);
            var members = new List<MemberDeclarationSyntax>();
            members.Add(CreateLastIdentityProducedField());
            members.Add(CreateCtor(classDeclaration, inputSemanticModel));
            members.Add(CreateNewIdentityMethod(classDeclaration, document));

            if (!isAbstract)
            {
                members.Add(CreateCreateMethod(classDeclaration, inputSemanticModel));
                members.Add(CreateWithFactoryMethod(classDeclaration, inputSemanticModel));
                members.Add(CreateDefaultInstanceField(classDeclaration, document));
                members.Add(CreateGetDefaultTemplateMethod(classDeclaration, document));
                members.Add(CreateCreateDefaultTemplatePartialMethod(classDeclaration, document));
                members.Add(CreateTemplateStruct(classDeclaration, document));
            }

            foreach (var field in fields)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    var xmldocComment = field.GetLeadingTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));

                    var property = SyntaxFactory.PropertyDeclaration(field.Declaration.Type, variable.Identifier.ValueText.ToPascalCase())
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                        .WithAccessorList(
                            SyntaxFactory.AccessorList(SyntaxFactory.List(new AccessorDeclarationSyntax[] {
                                SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration,
                                    SyntaxFactory.Block(
                                        SyntaxFactory.ReturnStatement(
                                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(), SyntaxFactory.IdentifierName(variable.Identifier))
                                        ))) })))
                        .WithLeadingTrivia(xmldocComment); // TODO: modify the <summary> to translate "Some description" to "Gets some description."
                    members.Add(property);
                }
            }

            return SyntaxFactory.ClassDeclaration(classDeclaration.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                .WithMembers(SyntaxFactory.List(members));
        }

        private static void ValidateInput(ClassDeclarationSyntax applyTo, Document document, IProgressAndErrors progress)
        {
            foreach (var field in GetFields(applyTo))
            {
                if (!field.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword)))
                {
                    var location = field.GetLocation().GetLineSpan().StartLinePosition;
                    progress.Warning(
                        string.Format(CultureInfo.CurrentCulture, "Field '{0}' should be marked readonly.", field.Declaration.Variables.First().Identifier),
                        (uint)location.Line,
                        (uint)location.Character);
                }
            }
        }

        private static readonly IdentifierNameSyntax DefaultInstanceFieldName = SyntaxFactory.IdentifierName("DefaultInstance");
        private static readonly IdentifierNameSyntax GetDefaultTemplateMethodName = SyntaxFactory.IdentifierName("GetDefaultTemplate");

        private MemberDeclarationSyntax CreateDefaultInstanceField(ClassDeclarationSyntax applyTo, Document document)
        {
            // [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            // private static readonly <#= templateType.TypeName #> DefaultInstance = GetDefaultTemplate();
            var field = SyntaxFactory.FieldDeclaration(
                 SyntaxFactory.VariableDeclaration(
                     SyntaxFactory.IdentifierName(applyTo.Identifier.ValueText),
                     SyntaxFactory.SingletonSeparatedList(
                         SyntaxFactory.VariableDeclarator(DefaultInstanceFieldName.Identifier)
                             .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.InvocationExpression(GetDefaultTemplateMethodName, SyntaxFactory.ArgumentList()))))))
                 .WithModifiers(SyntaxFactory.TokenList(
                     SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                     SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                     SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
                 .WithAttributeLists(SyntaxFactory.SingletonList(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                     SyntaxFactory.Attribute(
                         SyntaxFactory.ParseName(typeof(DebuggerBrowsableAttribute).FullName),
                         SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.AttributeArgument(
                             SyntaxFactory.MemberAccessExpression(
                                 SyntaxKind.SimpleMemberAccessExpression,
                                 SyntaxFactory.ParseName(typeof(DebuggerBrowsableState).FullName),
                                 SyntaxFactory.IdentifierName(nameof(DebuggerBrowsableState.Never)))))))))));
            return field;
        }

        private static readonly IdentifierNameSyntax varType = SyntaxFactory.IdentifierName("var");
        private static readonly IdentifierNameSyntax NestedTemplateTypeName = SyntaxFactory.IdentifierName("Template");
        private static readonly IdentifierNameSyntax CreateDefaultTemplateMethodName = SyntaxFactory.IdentifierName("CreateDefaultTemplate");
        private static readonly IdentifierNameSyntax CreateMethodName = SyntaxFactory.IdentifierName("Create");
        private static readonly IdentifierNameSyntax NewIdentityMethodName = SyntaxFactory.IdentifierName("NewIdentity");
        private static readonly IdentifierNameSyntax WithFactoryMethodName = SyntaxFactory.IdentifierName("WithFactory");

        private MemberDeclarationSyntax CreateCreateDefaultTemplatePartialMethod(ClassDeclarationSyntax applyTo, Document document)
        {
            // /// <summary>Provides defaults for fields.</summary>
            // /// <param name="template">The struct to set default values on.</param>
            // static partial void CreateDefaultTemplate(ref Template template);
            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                CreateDefaultTemplateMethodName.Identifier)
                .WithParameterList(SyntaxFactory.ParameterList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("template"))
                            .WithType(NestedTemplateTypeName)
                            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword))))))
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                    SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private MemberDeclarationSyntax CreateGetDefaultTemplateMethod(ClassDeclarationSyntax applyTo, Document document)
        {
            IdentifierNameSyntax templateVarName = SyntaxFactory.IdentifierName("template");
            var body = SyntaxFactory.Block(
                // var template = new Template();
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        varType,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                templateVarName.Identifier,
                                null,
                                SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(NestedTemplateTypeName, SyntaxFactory.ArgumentList(), null)))))),
                // CreateDefaultTemplate(ref template);
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        CreateDefaultTemplateMethodName,
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.RefKeyword), templateVarName))))),
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.IdentifierName(applyTo.Identifier),
                        SyntaxFactory.ArgumentList(CodeGen.JoinSyntaxNodes(
                            SyntaxKind.CommaToken,
                            ImmutableArray.Create(SyntaxFactory.Argument(SyntaxFactory.DefaultExpression(IdentityFieldTypeSyntax))).AddRange(
                                GetFieldVariables(applyTo).Select(f => SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, templateVarName, SyntaxFactory.IdentifierName(f.Value.Identifier))))))),
                        null)),
                // throw new System.NotImplementedException();
                SyntaxFactory.ThrowStatement(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("System.NotImplementedException"), SyntaxFactory.ArgumentList(), null)));

            return SyntaxFactory.MethodDeclaration(SyntaxFactory.IdentifierName(applyTo.Identifier.ValueText), GetDefaultTemplateMethodName.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(
                     SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                     SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithBody(body);
        }

        private MemberDeclarationSyntax CreateTemplateStruct(ClassDeclarationSyntax applyTo, Document document)
        {
            return SyntaxFactory.StructDeclaration(NestedTemplateTypeName.Identifier)
                .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(
                    GetFields(applyTo).Select(f => f.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword))))))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        }

        private static MemberDeclarationSyntax CreateCtor(ClassDeclarationSyntax applyTo, SemanticModel semanticModel)
        {
            return SyntaxFactory.ConstructorDeclaration(
                applyTo.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)))
                .WithParameterList(CreateParameterList(applyTo, ParameterStyle.Required, semanticModel).PrependParameter(RequiredIdentityParameter))
                .WithBody(SyntaxFactory.Block(
                    // this.someField = someField;
                    GetFieldVariables(applyTo).Select(f => SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            CodeGen.ThisDot(SyntaxFactory.IdentifierName(f.Value.Identifier)),
                            SyntaxFactory.IdentifierName(f.Value.Identifier))))));
        }

        private static MemberDeclarationSyntax CreateWithFactoryMethod(ClassDeclarationSyntax applyTo, SemanticModel semanticModel)
        {
            // (field.IsDefined && field.Value != this.field)
            Func<VariableDeclaratorSyntax, ExpressionSyntax> isChanged = v =>
                SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.LogicalAndExpression,
                        OptionalIsDefined(SyntaxFactory.IdentifierName(v.Identifier)),
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.NotEqualsExpression,
                            OptionalValue(SyntaxFactory.IdentifierName(v.Identifier)),
                            CodeGen.ThisDot(SyntaxFactory.IdentifierName(v.Identifier)))));
            var anyChangesExpression = GetFieldVariables(applyTo).Select(fv => isChanged(fv.Value)).ChainBinaryExpressions(SyntaxKind.LogicalOrExpression);

            // /// <summary>Returns a new instance of this object with any number of properties changed.</summary>
            // private TemplateType WithFactory(...)
            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName(applyTo.Identifier),
                WithFactoryMethodName.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                .WithParameterList(CreateParameterList(applyTo, ParameterStyle.Optional, semanticModel).AddParameters(OptionalIdentityParameter))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.IfStatement(
                        anyChangesExpression,
                        SyntaxFactory.Block(
                            ),
                        SyntaxFactory.ElseClause(SyntaxFactory.Block(
                            SyntaxFactory.ReturnStatement(SyntaxFactory.ThisExpression()))))));
        }

        private static MemberAccessExpressionSyntax OptionalIsDefined(ExpressionSyntax optionalOfTExpression)
        {
            return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, optionalOfTExpression, SyntaxFactory.IdentifierName("IsDefined"));
        }

        private static MemberAccessExpressionSyntax OptionalValue(ExpressionSyntax optionalOfTExpression)
        {
            return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, optionalOfTExpression, SyntaxFactory.IdentifierName("Value"));
        }

        private static MemberDeclarationSyntax CreateCreateMethod(ClassDeclarationSyntax applyTo, SemanticModel semanticModel)
        {
            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName(applyTo.Identifier),
                CreateMethodName.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithParameterList(CreateParameterList(applyTo, ParameterStyle.OptionalOrRequired, semanticModel))
                .WithBody(SyntaxFactory.Block(
                    // var identity = Optional.For(NewIdentity());
                    SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(
                        varType,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(IdentityParameterName.Identifier)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(OptionalFor(SyntaxFactory.InvocationExpression(NewIdentityMethodName, SyntaxFactory.ArgumentList()))))))),
                    // return DefaultInstance.With(...)
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                DefaultInstanceFieldName,
                                WithFactoryMethodName),
                            CreateArgumentList(applyTo, ArgSource.OptionalArgumentOrTemplate, asOptional: OptionalStyle.Always)
                                .AddArguments(SyntaxFactory.Argument(SyntaxFactory.NameColon(IdentityParameterName), SyntaxFactory.Token(SyntaxKind.None), IdentityParameterName))))));
        }

        private static readonly IdentifierNameSyntax LastIdentityProducedFieldName = SyntaxFactory.IdentifierName("lastIdentityProduced");

        private static MemberDeclarationSyntax CreateLastIdentityProducedField()
        {
            // /// <summary>The last identity assigned to a created instance.</summary>
            // private static int lastIdentityProduced;
            return SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(LastIdentityProducedFieldName.Identifier))))
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        }

        private static MemberDeclarationSyntax CreateNewIdentityMethod(ClassDeclarationSyntax applyTo, Document document)
        {
            // protected static <#= templateType.RequiredIdentityField.TypeName #> NewIdentity() {
            //     return (<#= templateType.RequiredIdentityField.TypeName #>)System.Threading.Interlocked.Increment(ref lastIdentityProduced);
            // }
            return SyntaxFactory.MethodDeclaration(
                IdentityFieldTypeSyntax,
                NewIdentityMethodName.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.CastExpression(
                            IdentityFieldTypeSyntax,
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.ParseName("System.Threading.Interlocked.Increment"),
                                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(
                                    null,
                                    SyntaxFactory.Token(SyntaxKind.RefKeyword),
                                    LastIdentityProducedFieldName))))))));
        }

        private static ExpressionSyntax OptionalFor(ExpressionSyntax expression)
        {
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)),
                        SyntaxFactory.IdentifierName(nameof(Optional))),
                    SyntaxFactory.IdentifierName(nameof(Optional.For))),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(expression))));
        }

        private static TypeSyntax OptionalOf(TypeSyntax type)
        {
            return SyntaxFactory.QualifiedName(
                SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)),
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(nameof(Optional)),
                    SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(type))));
        }

        private static IEnumerable<FieldDeclarationSyntax> GetFields(ClassDeclarationSyntax applyTo)
        {
            return applyTo.ChildNodes().OfType<FieldDeclarationSyntax>();
        }

        private static IEnumerable<KeyValuePair<FieldDeclarationSyntax, VariableDeclaratorSyntax>> GetFieldVariables(ClassDeclarationSyntax applyTo)
        {
            foreach (var field in GetFields(applyTo))
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    yield return new KeyValuePair<FieldDeclarationSyntax, VariableDeclaratorSyntax>(field, variable);
                }
            }
        }

        private static ParameterListSyntax CreateParameterList(ClassDeclarationSyntax applyTo, ParameterStyle style, SemanticModel semanticModel)
        {
            Requires.NotNull(applyTo, "applyTo");

            if (style == ParameterStyle.OptionalOrRequired)
            {
                ////fields = SortRequiredFieldsFirst(fields);
            }

            Func<FieldDeclarationSyntax, bool> isOptional = f => style == ParameterStyle.Optional || (style == ParameterStyle.OptionalOrRequired && !IsFieldRequired(f, semanticModel));
            Func<ParameterSyntax, FieldDeclarationSyntax, ParameterSyntax> setTypeAndDefault = (p, f) => isOptional(f)
                ? Optional(p.WithType(f.Declaration.Type))
                : p.WithType(f.Declaration.Type);
            return SyntaxFactory.ParameterList(
                CodeGen.JoinSyntaxNodes(
                    SyntaxKind.CommaToken,
                    GetFieldVariables(applyTo).Select(f => setTypeAndDefault(SyntaxFactory.Parameter(f.Value.Identifier), f.Key))));
        }

        private static ArgumentListSyntax CreateArgumentList(ClassDeclarationSyntax applyTo, ArgSource source = ArgSource.Property, OptionalStyle asOptional = OptionalStyle.None)
        {
            return SyntaxFactory.ArgumentList();
        }

        private static ParameterSyntax Optional(ParameterSyntax parameter)
        {
            return parameter
                .WithType(OptionalOf(parameter.Type))
                .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.DefaultExpression(OptionalOf(parameter.Type))));
        }

        private static bool IsFieldRequired(FieldDeclarationSyntax field, SemanticModel semanticModel)
        {
            var fieldSymbol = semanticModel.GetDeclaredSymbol(field.Declaration.Variables.First());
            return fieldSymbol?.GetAttributes().Any(a => IsOrDerivesFrom<RequiredAttribute>(a.AttributeClass)) ?? false;
        }

        private static bool IsOrDerivesFrom<T>(INamedTypeSymbol type)
        {
            if (type != null)
            {
                if (type.Name == typeof(T).Name)
                {
                    // Don't sweat accuracy too much at this point.
                    return true;
                }

                return IsOrDerivesFrom<T>(type.BaseType);
            }

            return false;
        }

        private enum ParameterStyle
        {
            Required,
            Optional,
            OptionalOrRequired,
        }

        private enum ArgSource
        {
            Property,
            Argument,
            OptionalArgumentOrProperty,
            OptionalArgumentOrTemplate,
            Missing,
        }

        private enum OptionalStyle
        {
            None,
            WhenNotRequired,
            Always,
        }
    }
}
