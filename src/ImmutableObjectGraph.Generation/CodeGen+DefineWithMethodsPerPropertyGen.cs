namespace ImmutableObjectGraph.Generation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Data.Entity.Design.PluralizationServices;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;
    using Validation;
    using LookupTableHelper = RecursiveTypeExtensions.LookupTable<IRecursiveType, IRecursiveParentWithLookupTable<IRecursiveType>>;

    public partial class CodeGen
    {
        protected class DefineWithMethodsPerPropertyGen : FeatureGenerator
        {
            internal const string WithPropertyMethodPrefix = "With";

            public DefineWithMethodsPerPropertyGen(CodeGen generator)
                : base(generator)
            {
            }

            public override bool IsApplicable
            {
                get { return this.generator.options.DefineWithMethodsPerProperty; }
            }

            protected override void GenerateCore()
            {
                var valueParameterName = SyntaxFactory.IdentifierName("value");

                foreach (var field in this.generator.applyToMetaType.LocalFields)
                {
                    var withPropertyMethod = SyntaxFactory.MethodDeclaration(
                        GetFullyQualifiedSymbolName(this.generator.applyToSymbol),
                        WithPropertyMethodPrefix + field.Name.ToPascalCase())
                        .WithAdditionalAnnotations()
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddParameterListParameters(
                            SyntaxFactory.Parameter(valueParameterName.Identifier)
                                .WithType(GetFullyQualifiedSymbolName(field.Type)))
                        .WithBody(SyntaxFactory.Block(
                            SyntaxFactory.IfStatement(
                                SyntaxFactory.BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    valueParameterName,
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(), field.NameAsField)),
                                SyntaxFactory.Block(
                                    SyntaxFactory.ReturnStatement(SyntaxFactory.ThisExpression()))),
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.ThisExpression(),
                                        WithMethodName),
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.NameColon(field.Name),
                                                NoneToken,
                                                Syntax.OptionalFor(valueParameterName))))))));

                    this.innerMembers.Add(withPropertyMethod);
                }

                foreach (var field in this.generator.applyToMetaType.InheritedFields)
                {
                    string withMethodName = WithPropertyMethodPrefix + field.Name.ToPascalCase();
                    var withPropertyMethod = SyntaxFactory.MethodDeclaration(
                        GetFullyQualifiedSymbolName(this.generator.applyToSymbol),
                        withMethodName)
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.NewKeyword),
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddParameterListParameters(
                            SyntaxFactory.Parameter(valueParameterName.Identifier)
                                .WithType(GetFullyQualifiedSymbolName(field.Type)))
                        .WithBody(SyntaxFactory.Block(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.CastExpression(
                                    GetFullyQualifiedSymbolName(this.generator.applyToSymbol),
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.BaseExpression(),
                                            SyntaxFactory.IdentifierName(withMethodName)),
                                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(valueParameterName))))))));

                    this.innerMembers.Add(withPropertyMethod);
                }
            }
        }
    }
}
