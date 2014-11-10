namespace ImmutableObjectGraph.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
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
        private static readonly IdentifierNameSyntax IdentityParameterName = SyntaxFactory.IdentifierName("identity");

        public GenerateImmutableAttribute()
        {
        }

        public override async Task<MemberDeclarationSyntax> GenerateAsync(MemberDeclarationSyntax applyTo, Document document, CancellationToken cancellationToken)
        {
            var inputSemanticModel = await document.GetSemanticModelAsync();
            var classDeclaration = (ClassDeclarationSyntax)applyTo;
            bool isAbstract = classDeclaration.Modifiers.Any(m => m.IsContextualKind(SyntaxKind.AbstractKeyword));

            var fields = GetFields(classDeclaration);
            var members = new List<MemberDeclarationSyntax>();
            members.Add(CreateCtor(classDeclaration, document));

            if (!isAbstract)
            {
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

        private static MemberDeclarationSyntax CreateCtor(ClassDeclarationSyntax applyTo, Document document)
        {
            return SyntaxFactory.ConstructorDeclaration(
                applyTo.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)))
                .WithParameterList(CreateParameterList(applyTo, ParameterStyle.Required).PrependParameter(
                    SyntaxFactory.Parameter(IdentityParameterName.Identifier).WithType(IdentityFieldTypeSyntax)))
                .WithBody(SyntaxFactory.Block(
                    // this.someField = someField;
                    GetFieldVariables(applyTo).Select(f => SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            CodeGen.ThisDot(SyntaxFactory.IdentifierName(f.Value.Identifier)),
                            SyntaxFactory.IdentifierName(f.Value.Identifier))))));
        }

        private static IEnumerable<FieldDeclarationSyntax> GetFields(ClassDeclarationSyntax applyTo)
        {
            return applyTo.ChildNodes().OfType<FieldDeclarationSyntax>();
        }

        private static IEnumerable<KeyValuePair<VariableDeclarationSyntax, VariableDeclaratorSyntax>> GetFieldVariables(ClassDeclarationSyntax applyTo)
        {
            foreach (var field in GetFields(applyTo))
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    yield return new KeyValuePair<VariableDeclarationSyntax, VariableDeclaratorSyntax>(field.Declaration, variable);
                }
            }
        }

        private static ParameterListSyntax CreateParameterList(ClassDeclarationSyntax applyTo, ParameterStyle style)
        {
            Requires.NotNull(applyTo, "applyTo");

            return SyntaxFactory.ParameterList(
                CodeGen.JoinSyntaxNodes(
                    SyntaxKind.CommaToken,
                    GetFieldVariables(applyTo).Select(f => SyntaxFactory.Parameter(f.Value.Identifier).WithType(f.Key.Type))));
        }

        private enum ParameterStyle
        {
            Required,
            Optional,
            OptionalOrRequired,
        }
    }
}
