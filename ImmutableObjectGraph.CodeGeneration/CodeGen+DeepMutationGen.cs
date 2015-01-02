namespace ImmutableObjectGraph.CodeGeneration
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
    using Microsoft.ImmutableObjectGraph_SFG;
    using Validation;
    using LookupTableHelper = RecursiveTypeExtensions.LookupTable<IRecursiveType, IRecursiveParentWithLookupTable<IRecursiveType>>;

    public partial class CodeGen
    {
        protected class DeepMutationGen : FeatureGenerator
        {
            private static readonly IdentifierNameSyntax AddDescendentMethodName = SyntaxFactory.IdentifierName("AddDescendent");
            private static readonly IdentifierNameSyntax ReplaceDescendentMethodName = SyntaxFactory.IdentifierName("ReplaceDescendent");
            private static readonly IdentifierNameSyntax RemoveDescendentMethodName = SyntaxFactory.IdentifierName("RemoveDescendent");

            public DeepMutationGen(CodeGen generator)
                  : base(generator)
            {
            }

            public override bool IsApplicable
            {
                get { return this.generator.applyToMetaType.IsRecursive; }
            }

            protected override void GenerateCore()
            {
                this.innerMembers.Add(this.CreateAddDescendentMethod());
            }

            private MethodDeclarationSyntax CreateAddDescendentMethod()
            {
                var valueParameterName = SyntaxFactory.IdentifierName("value");
                var parentParameterName = SyntaxFactory.IdentifierName("parent");
                var spineVar = SyntaxFactory.IdentifierName("spine");
                var newParentVar = SyntaxFactory.IdentifierName("newParent");
                var newSpineVar = SyntaxFactory.IdentifierName("newSpine");

                return SyntaxFactory.MethodDeclaration(
                    GetFullyQualifiedSymbolName(this.generator.applyToSymbol),
                    AddDescendentMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(valueParameterName.Identifier).WithType(this.generator.applyToMetaType.RecursiveType.TypeSyntax),
                        SyntaxFactory.Parameter(parentParameterName.Identifier).WithType(this.generator.applyToMetaType.RecursiveParent.TypeSyntax))
                    .WithBody(SyntaxFactory.Block(
                        // var spine = this.GetSpine(parent);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(spineVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(Syntax.ThisDot(FastSpineGen.GetSpineMethodName))
                                    .AddArgumentListArguments(SyntaxFactory.Argument(parentParameterName)))))),
                        // var newParent = parent.With(children: parent.Children.Add(value));
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(newParentVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        parentParameterName,
                                        WithMethodName))
                                    .AddArgumentListArguments(SyntaxFactory.Argument(
                                        SyntaxFactory.NameColon(this.applyTo.RecursiveField.NameAsField),
                                        SyntaxFactory.Token(SyntaxKind.None),
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    parentParameterName,
                                                    this.applyTo.RecursiveField.NameAsProperty),
                                                SyntaxFactory.IdentifierName(nameof(List<int>.Add))),
                                            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(valueParameterName)))))))))),
                        // var newSpine = System.Collections.Immutable.ImmutableStack.Create(value, newParent);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(newSpineVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(Syntax.CreateImmutableStack())
                                    .AddArgumentListArguments(
                                        SyntaxFactory.Argument(valueParameterName),
                                        SyntaxFactory.Argument(newParentVar)))))),
                        // return (ProjectElementContainer)RecursiveTypeExtensions.ReplaceDescendent(this, spine, newSpine, spineIncludesDeletedElement: false).Peek();
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.CastExpression(
                                this.applyTo.TypeSyntax,
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.QualifiedName(
                                                    SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)),
                                                    SyntaxFactory.IdentifierName(nameof(RecursiveTypeExtensions))),
                                                ReplaceDescendentMethodName))
                                            .AddArgumentListArguments(
                                                SyntaxFactory.Argument(SyntaxFactory.ThisExpression()),
                                                SyntaxFactory.Argument(spineVar),
                                                SyntaxFactory.Argument(newSpineVar),
                                                SyntaxFactory.Argument(SyntaxFactory.NameColon("spineIncludesDeletedElement"), SyntaxFactory.Token(SyntaxKind.None), SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))),
                                        SyntaxFactory.IdentifierName(nameof(ImmutableStack<int>.Peek))),
                                    SyntaxFactory.ArgumentList())))
                        ));
            }
        }
    }
}
