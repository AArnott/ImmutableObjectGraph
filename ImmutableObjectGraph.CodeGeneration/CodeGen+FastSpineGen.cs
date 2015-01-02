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
        protected class FastSpineGen : FeatureGenerator
        {
            private static readonly IdentifierNameSyntax LookupTableFieldName = SyntaxFactory.IdentifierName("lookupTable");
            private static readonly IdentifierNameSyntax LookupTablePropertyName = SyntaxFactory.IdentifierName("LookupTable");
            private static readonly IdentifierNameSyntax InefficiencyLoadFieldName = SyntaxFactory.IdentifierName("inefficiencyLoad");

            public FastSpineGen(CodeGen generator)
                : base(generator)
            {
            }

            public override bool IsApplicable
            {
                get { return true; }
            }

            protected override void GenerateCore()
            {
                var keyValuePairType = SyntaxFactory.ParseTypeName(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "System.Collections.Generic.KeyValuePair<{1}, {0}>",
                        IdentityFieldTypeSyntax,
                        this.applyTo.RecursiveType.TypeSyntax));
                var lookupTableType = SyntaxFactory.ParseTypeName(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "System.Collections.Immutable.ImmutableDictionary<{0}, {1}>",
                        IdentityFieldTypeSyntax,
                        keyValuePairType));

                if (this.applyTo.IsRecursiveParent)
                {
                    // private readonly uint inefficiencyLoad;
                    var inefficiencyLoadType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword));
                    this.innerMembers.Add(SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(inefficiencyLoadType)
                            .AddVariables(SyntaxFactory.VariableDeclarator(InefficiencyLoadFieldName.Identifier)))
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));

                    var interfaceType = SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)),
                        SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier(nameof(IRecursiveParentWithLookupTable<IRecursiveType>)),
                            SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(this.applyTo.RecursiveType.TypeSyntax))));
                    this.baseTypes.Add(SyntaxFactory.SimpleBaseType(interfaceType));
                    var explicitImplementation = SyntaxFactory.ExplicitInterfaceSpecifier(interfaceType);

                    // uint IRecursiveParentWithLookupTable<TRecursiveType>.InefficiencyLoad { get; }
                    this.innerMembers.Add(
                        SyntaxFactory.PropertyDeclaration(inefficiencyLoadType, nameof(IRecursiveParentWithLookupTable<IRecursiveType>.InefficiencyLoad))
                        .WithExplicitInterfaceSpecifier(explicitImplementation)
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(SyntaxFactory.ReturnStatement(Syntax.ThisDot(InefficiencyLoadFieldName))))));

                    // IReadOnlyCollection<TRecursiveType> IRecursiveParentWithLookupTable<TRecursiveType>.Children { get; }
                    this.innerMembers.Add(
                        SyntaxFactory.PropertyDeclaration(
                            Syntax.IReadOnlyCollectionOf(this.applyTo.RecursiveType.TypeSyntax),
                            nameof(IRecursiveParentWithLookupTable<IRecursiveType>.Children))
                        .WithExplicitInterfaceSpecifier(explicitImplementation)
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(SyntaxFactory.ReturnStatement(Syntax.ThisDot(this.applyTo.RecursiveField.NameAsProperty))))));

                    // ImmutableDictionary<IdentityFieldType, KeyValuePair<TRecursiveType, IdentityFieldType>> IRecursiveParentWithLookupTable<TRecursiveType>.LookupTable { get; }
                    this.innerMembers.Add(
                        SyntaxFactory.PropertyDeclaration(
                            lookupTableType,
                            nameof(IRecursiveParentWithLookupTable<IRecursiveType>.LookupTable))
                        .WithExplicitInterfaceSpecifier(explicitImplementation)
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(SyntaxFactory.ReturnStatement(Syntax.ThisDot(LookupTablePropertyName))))));
                }

                if (this.applyTo.IsRecursive)
                {
                    var lookupInitResultVarName = SyntaxFactory.IdentifierName("lookupInitResult");
                    this.additionalCtorStatements.AddRange(new StatementSyntax[] {
                        // var lookupInitResult = ImmutableObjectGraph.RecursiveTypeExtensions.LookupTable<TRecursiveType, TRecursiveParent>.Initialize(this, lookupTable);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(
                            varType,
                            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(lookupInitResultVarName.Identifier)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                        GetLookupTableHelperMember(nameof(LookupTableHelper.Initialize)),
                                        SyntaxFactory.ArgumentList(Syntax.JoinSyntaxNodes(
                                            SyntaxKind.CommaToken,
                                            SyntaxFactory.Argument(SyntaxFactory.ThisExpression()),
                                            SyntaxFactory.Argument(LookupTableFieldName))))))))),
                        // this.inefficiencyLoad = lookupInitResult.InefficiencyLoad;
                        SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            Syntax.ThisDot(InefficiencyLoadFieldName),
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, lookupInitResultVarName, SyntaxFactory.IdentifierName(nameof(LookupTableHelper.InitializeLookupResult.InefficiencyLoad))))),
                        // this.lookupTable = lookupInitResult.LookupTable;
                        SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            Syntax.ThisDot(LookupTableFieldName),
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, lookupInitResultVarName, SyntaxFactory.IdentifierName(nameof(LookupTableHelper.InitializeLookupResult.LookupTable)))))
                    });

                    this.innerMembers.Add(SyntaxFactory.PropertyDeclaration(
                        lookupTableType,
                        LookupTablePropertyName.Identifier)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(
                                SyntaxFactory.IfStatement(
                                    SyntaxFactory.BinaryExpression(
                                        SyntaxKind.EqualsExpression,
                                        Syntax.ThisDot(LookupTableFieldName),
                                        GetLookupTableHelperMember(nameof(LookupTableHelper.LazySentinel))),
                                    SyntaxFactory.Block(
                                        SyntaxFactory.ExpressionStatement(
                                            SyntaxFactory.AssignmentExpression(
                                                SyntaxKind.SimpleAssignmentExpression,
                                                Syntax.ThisDot(LookupTableFieldName),
                                                SyntaxFactory.InvocationExpression(
                                                    GetLookupTableHelperMember(nameof(LookupTableHelper.CreateLookupTable)),
                                                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Argument(SyntaxFactory.ThisExpression())))))))),
                                SyntaxFactory.ReturnStatement(Syntax.ThisDot(LookupTableFieldName))))));

                    // private System.Collections.Immutable.ImmutableDictionary<System.UInt32, KeyValuePair<FileSystemEntry, System.UInt32>> lookupTable;
                    this.innerMembers.Add(SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(lookupTableType)
                            .AddVariables(SyntaxFactory.VariableDeclarator(LookupTableFieldName.Identifier)))
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }
            }

            protected MethodDeclarationSyntax CreateInitializeLookupMethod()
            {
                throw new NotImplementedException();
            }

            protected MemberAccessExpressionSyntax GetLookupTableHelperMember(string memberName)
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    GetLookupTableHelperType(),
                    SyntaxFactory.IdentifierName(memberName));
            }

            protected TypeSyntax GetLookupTableHelperType()
            {
                // ImmutableObjectGraph.RecursiveTypeExtensions.LookupTable<TRecursiveType, TRecursiveParent>
                return SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)),
                        SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph.RecursiveTypeExtensions))),
                    SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier(nameof(RecursiveTypeExtensions.LookupTable<IRecursiveType, IRecursiveParentWithLookupTable<IRecursiveType>>)),
                        SyntaxFactory.TypeArgumentList(Syntax.JoinSyntaxNodes<TypeSyntax>(
                            SyntaxKind.CommaToken,
                            this.applyTo.RecursiveType.TypeSyntax,
                            this.applyTo.RecursiveParent.TypeSyntax))));
            }
        }
    }
}
