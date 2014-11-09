namespace ImmutableObjectGraph.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.ImmutableObjectGraph_SFG;

    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateImmutableAttribute : CodeGenerationAttribute
    {
        private string positionalArg;

        public GenerateImmutableAttribute()
        {
        }

        public GenerateImmutableAttribute(string positionalArg)
        {
            this.positionalArg = positionalArg;
        }

        public string NamedArg { get; set; }

        public override MemberDeclarationSyntax Generate(MemberDeclarationSyntax applyTo, Document document)
        {
            var classDeclaration = (ClassDeclarationSyntax)applyTo;
            bool isAbstract = classDeclaration.Modifiers.Any(m => m.IsContextualKind(SyntaxKind.AbstractKeyword));

            var fields = applyTo.ChildNodes().OfType<FieldDeclarationSyntax>();
            var members = new List<MemberDeclarationSyntax>();

            if (!isAbstract)
            {
                members.Add(CreateDefaultInstanceField(classDeclaration, document));
                members.Add(CreateGetDefaultTemplateMethod(classDeclaration, document));
            }

            foreach (var field in fields)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    var property = SyntaxFactory.PropertyDeclaration(field.Declaration.Type, variable.Identifier.ValueText.ToPascalCase())
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                        .WithAccessorList(
                            SyntaxFactory.AccessorList(SyntaxFactory.List(new AccessorDeclarationSyntax[] {
                                SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration,
                                    SyntaxFactory.Block(
                                        SyntaxFactory.ReturnStatement(
                                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(), SyntaxFactory.IdentifierName(variable.Identifier))
                                        ))) })));
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

        private MemberDeclarationSyntax CreateGetDefaultTemplateMethod(ClassDeclarationSyntax applyTo, Document document)
        {
            return SyntaxFactory.MethodDeclaration(SyntaxFactory.IdentifierName(applyTo.Identifier.ValueText), GetDefaultTemplateMethodName.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(
                     SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                     SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ThrowStatement(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("System.NotImplementedException"), SyntaxFactory.ArgumentList(), null))));
        }
    }
}
