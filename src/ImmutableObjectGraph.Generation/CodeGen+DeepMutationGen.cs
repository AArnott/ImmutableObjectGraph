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
        protected class DeepMutationGen : FeatureGenerator
        {
            private static readonly IdentifierNameSyntax AddDescendentMethodName = SyntaxFactory.IdentifierName("AddDescendent");
            internal static readonly IdentifierNameSyntax ReplaceDescendentMethodName = SyntaxFactory.IdentifierName("ReplaceDescendent");
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
                this.innerMembers.Add(this.CreateRemoveDescendentMethod());
                this.innerMembers.Add(this.CreateReplaceDescendentSameIdentityMethod());
                this.innerMembers.Add(this.CreateReplaceDescendentDifferentIdentityMethod());
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
                                        NoneToken,
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
                                                SyntaxFactory.Argument(SyntaxFactory.NameColon("spineIncludesDeletedElement"), NoneToken, SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))),
                                        SyntaxFactory.IdentifierName(nameof(ImmutableStack<int>.Peek))),
                                    SyntaxFactory.ArgumentList())))
                        ));
            }

            private MethodDeclarationSyntax CreateRemoveDescendentMethod()
            {
                var valueParameter = SyntaxFactory.IdentifierName("value");
                var spineVar = SyntaxFactory.IdentifierName("spine");
                var spineListVar = SyntaxFactory.IdentifierName("spineList");
                var parentVar = SyntaxFactory.IdentifierName("parent");
                var newParentVar = SyntaxFactory.IdentifierName("newParent");
                var newSpineVar = SyntaxFactory.IdentifierName("newSpine");

                // public TemplateType RemoveDescendent(TRecursiveType value) {
                return SyntaxFactory.MethodDeclaration(
                    this.applyTo.TypeSyntax,
                    RemoveDescendentMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(valueParameter.Identifier).WithType(this.applyTo.RecursiveType.TypeSyntax))
                    .WithBody(SyntaxFactory.Block(
                        // var spine = this.GetSpine(value);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType)
                            .AddVariables(SyntaxFactory.VariableDeclarator(spineVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(Syntax.ThisDot(FastSpineGen.GetSpineMethodName)).AddArgumentListArguments(
                                    SyntaxFactory.Argument(valueParameter)))))),
                        // var spineList = spine.ToList();
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType)
                            .AddVariables(SyntaxFactory.VariableDeclarator(spineListVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                Syntax.ToList(spineVar))))),
                        // var parent = (TRecursiveParent)spineList[spineList.Count - 2];
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType)
                            .AddVariables(SyntaxFactory.VariableDeclarator(parentVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.CastExpression(
                                    this.applyTo.RecursiveParent.TypeSyntax,
                                    SyntaxFactory.ElementAccessExpression(
                                        spineListVar,
                                        SyntaxFactory.BracketedArgumentList(
                                            SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.Argument(SyntaxFactory.BinaryExpression(
                                                    SyntaxKind.SubtractExpression,
                                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, spineListVar, SyntaxFactory.IdentifierName(nameof(List<int>.Count))),
                                                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(2)))))))))))),
                        // var newParent = parent.With(children: parent.Children.Remove(spineList[spineList.Count - 1]));
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType)
                            .AddVariables(SyntaxFactory.VariableDeclarator(newParentVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parentVar, WithMethodName))
                                .AddArgumentListArguments(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.NameColon(this.applyTo.RecursiveField.NameAsField),
                                    NoneToken,
                                    SyntaxFactory.InvocationExpression( // parent.Children.Remove(...)
                                        SyntaxFactory.MemberAccessExpression( // parent.Children.Remove
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parentVar, this.applyTo.RecursiveField.NameAsProperty),
                                            SyntaxFactory.IdentifierName(nameof(List<int>.Remove))))
                                        .AddArgumentListArguments(
                                            SyntaxFactory.Argument(SyntaxFactory.ElementAccessExpression( // spineList[spineList.Count - 1]
                                                spineListVar,
                                                SyntaxFactory.BracketedArgumentList(SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument( // spineList.Count - 1
                                                        SyntaxFactory.BinaryExpression(
                                                            SyntaxKind.SubtractExpression,
                                                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, spineListVar, SyntaxFactory.IdentifierName(nameof(List<int>.Count))),
                                                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1))))))))))))))),
                        // var newSpine = System.Collections.Immutable.ImmutableStack.Create<TRecursiveType>(newParent);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType)
                            .AddVariables(SyntaxFactory.VariableDeclarator(newSpineVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(Syntax.CreateImmutableStack(this.applyTo.RecursiveType.TypeSyntax))
                                    .AddArgumentListArguments(SyntaxFactory.Argument(newParentVar)))))),
                        // return (TRecursiveParent)this.ReplaceDescendent(spine, newSpine, spineIncludesDeletedElement: true).Peek();
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.CastExpression(
                                this.applyTo.RecursiveParent.TypeSyntax,
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.InvocationExpression(Syntax.ThisDot(ReplaceDescendentMethodName))
                                            .AddArgumentListArguments(
                                                SyntaxFactory.Argument(spineVar),
                                                SyntaxFactory.Argument(newSpineVar),
                                                SyntaxFactory.Argument(SyntaxFactory.NameColon("spineIncludesDeletedElement"), NoneToken, SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))
                                            ),
                                        SyntaxFactory.IdentifierName(nameof(ImmutableStack<int>.Peek))),
                                    SyntaxFactory.ArgumentList())))));
            }

            private MethodDeclarationSyntax CreateReplaceDescendentSameIdentityMethod()
            {
                var updatedNodeParameter = SyntaxFactory.IdentifierName("updatedNode");
                var spineVar = SyntaxFactory.IdentifierName("spine");

                // public TemplateType ReplaceDescendent(TRecursiveType value) {
                return SyntaxFactory.MethodDeclaration(
                    this.applyTo.TypeSyntax,
                    ReplaceDescendentMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(updatedNodeParameter.Identifier).WithType(this.applyTo.RecursiveType.TypeSyntax))
                    .WithBody(SyntaxFactory.Block(
                        // var spine = this.GetSpine(updatedNode.Identity);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(spineVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(Syntax.ThisDot(FastSpineGen.GetSpineMethodName))
                                    .AddArgumentListArguments(
                                    SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, updatedNodeParameter, SyntaxFactory.IdentifierName(nameof(IRecursiveType.Identity))))))))),
                        // if (spine.IsEmpty) {
                        SyntaxFactory.IfStatement(
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, spineVar, SyntaxFactory.IdentifierName(nameof(ImmutableStack<int>.IsEmpty))),
                            // 	// The descendent was not found.
                            // 	throw new System.ArgumentException("Old value not found");
                            SyntaxFactory.Block(
                                SyntaxFactory.ThrowStatement(
                                    SyntaxFactory.ObjectCreationExpression(Syntax.GetTypeSyntax(typeof(ArgumentException))).AddArgumentListArguments(
                                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("Old value not found."))))))),
                        // return (TemplateType)this.ReplaceDescendent(spine, ImmutableStack.Create(updatedNode), spineIncludesDeletedElement: false).Peek();
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.CastExpression(
                                this.applyTo.TypeSyntax,
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.InvocationExpression(Syntax.ThisDot(ReplaceDescendentMethodName))
                                            .AddArgumentListArguments(
                                                SyntaxFactory.Argument(spineVar),
                                                SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(Syntax.CreateImmutableStack()).AddArgumentListArguments(
                                                    SyntaxFactory.Argument(updatedNodeParameter))),
                                                SyntaxFactory.Argument(SyntaxFactory.NameColon("spineIncludesDeletedElement"), NoneToken, SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                                            ),
                                        SyntaxFactory.IdentifierName(nameof(ImmutableStack<int>.Peek))),
                                    SyntaxFactory.ArgumentList())))));
            }

            private MethodDeclarationSyntax CreateReplaceDescendentDifferentIdentityMethod()
            {
                var currentParameter = SyntaxFactory.IdentifierName("current");
                var replacementParameter = SyntaxFactory.IdentifierName("replacement");
                var spineVar = SyntaxFactory.IdentifierName("spine");

                // public TemplateType ReplaceDescendent(TRecursiveType current, TRecursiveType replacement) {
                return SyntaxFactory.MethodDeclaration(
                    this.applyTo.TypeSyntax,
                    ReplaceDescendentMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(currentParameter.Identifier).WithType(this.applyTo.RecursiveType.TypeSyntax),
                        SyntaxFactory.Parameter(replacementParameter.Identifier).WithType(this.applyTo.RecursiveType.TypeSyntax))
                    .WithBody(SyntaxFactory.Block(
                        // var spine = this.GetSpine(current);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(spineVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(Syntax.ThisDot(FastSpineGen.GetSpineMethodName))
                                    .AddArgumentListArguments(
                                        SyntaxFactory.Argument(currentParameter)))))),
                        // if (spine.IsEmpty) {
                        SyntaxFactory.IfStatement(
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, spineVar, SyntaxFactory.IdentifierName(nameof(ImmutableStack<int>.IsEmpty))),
                            // 	// The descendent was not found.
                            // 	throw new System.ArgumentException("Old value not found");
                            SyntaxFactory.Block(
                                SyntaxFactory.ThrowStatement(
                                    SyntaxFactory.ObjectCreationExpression(Syntax.GetTypeSyntax(typeof(ArgumentException))).AddArgumentListArguments(
                                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("Old value not found."))))))),
                        // return (TemplateType)this.ReplaceDescendent(spine, ImmutableStack.Create(replacement), spineIncludesDeletedElement: false).Peek();
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.CastExpression(
                                this.applyTo.TypeSyntax,
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.InvocationExpression(Syntax.ThisDot(ReplaceDescendentMethodName))
                                            .AddArgumentListArguments(
                                                SyntaxFactory.Argument(spineVar),
                                                SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(Syntax.CreateImmutableStack()).AddArgumentListArguments(
                                                    SyntaxFactory.Argument(replacementParameter))),
                                                SyntaxFactory.Argument(SyntaxFactory.NameColon("spineIncludesDeletedElement"), NoneToken, SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                                            ),
                                        SyntaxFactory.IdentifierName(nameof(ImmutableStack<int>.Peek))),
                                    SyntaxFactory.ArgumentList())))));
            }
        }
    }
}
