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
        protected class FastSpineGen : FeatureGenerator
        {
            internal static readonly IdentifierNameSyntax GetSpineMethodName = SyntaxFactory.IdentifierName("GetSpine");
            private static readonly IdentifierNameSyntax LookupTableFieldName = SyntaxFactory.IdentifierName("lookupTable");
            private static readonly IdentifierNameSyntax LookupTablePropertyName = SyntaxFactory.IdentifierName("LookupTable");
            private static readonly IdentifierNameSyntax InefficiencyLoadFieldName = SyntaxFactory.IdentifierName("inefficiencyLoad");
            private static readonly IdentifierNameSyntax FindMethodName = SyntaxFactory.IdentifierName(nameof(RecursiveTypeExtensions.Find));

            private readonly TypeSyntax lookupTableType;
            private readonly NameSyntax IRecursiveParentWithChildReplacementType;

            public FastSpineGen(CodeGen generator)
                : base(generator)
            {
                if (this.applyTo.IsRecursive || this.applyTo.IsRecursiveParentOrDerivative)
                {
                    var keyValuePairType = SyntaxFactory.ParseTypeName(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "System.Collections.Generic.KeyValuePair<{1}, {0}>",
                            IdentityFieldTypeSyntax,
                            this.applyTo.RecursiveTypeFromFamily.TypeSyntax));
                    this.lookupTableType = SyntaxFactory.ParseTypeName(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "System.Collections.Immutable.ImmutableDictionary<{0}, {1}>",
                            IdentityFieldTypeSyntax,
                            keyValuePairType));
                    this.IRecursiveParentWithChildReplacementType = SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)),
                        SyntaxFactory.GenericName(nameof(IRecursiveParentWithChildReplacement<IRecursiveType>))
                            .AddTypeArgumentListArguments(this.applyTo.RecursiveType.TypeSyntax));
                }
            }

            public override bool IsApplicable
            {
                get { return true; }
            }

            public override ClassDeclarationSyntax ProcessApplyToClassDeclaration(ClassDeclarationSyntax applyTo)
            {
                applyTo = base.ProcessApplyToClassDeclaration(applyTo);

                if (this.applyTo.IsRecursiveParentOrDerivative)
                {
                    // Add the lookupTable parameter to the constructor's signature.
                    var origCtor = GetMeaningfulConstructor(applyTo);
                    var alteredCtor = origCtor.AddParameterListParameters(SyntaxFactory.Parameter(LookupTableFieldName.Identifier).WithType(Syntax.OptionalOf(this.lookupTableType)));

                    // If this type isn't itself the recursive parent then we derive from it. And we must propagate the value to the chained base type.
                    if (!this.applyTo.IsRecursiveParent)
                    {
                        Assumes.NotNull(alteredCtor.Initializer); // we expect a chained call to the base constructor.
                        alteredCtor = alteredCtor.WithInitializer(
                            alteredCtor.Initializer.AddArgumentListArguments(
                                SyntaxFactory.Argument(SyntaxFactory.NameColon(LookupTableFieldName), NoneToken, LookupTableFieldName)));
                    }

                    // Apply the updated constructor back to the generated type.
                    applyTo = applyTo.ReplaceNode(origCtor, alteredCtor);

                    // Search for invocations of the constructor that we now have to update.
                    var creationInvocations = (
                        from n in applyTo.DescendantNodes()
                        let ctorInvocation = n as ObjectCreationExpressionSyntax
                        let instantiatedTypeName = ctorInvocation?.Type?.ToString()
                        where instantiatedTypeName == this.applyTo.TypeSyntax.ToString() || instantiatedTypeName == this.applyTo.TypeSymbol.Name
                        select ctorInvocation).ToImmutableArray();
                    var chainedInvocations = (
                        from n in applyTo.DescendantNodes()
                        let chained = n as ConstructorInitializerSyntax
                        where chained.IsKind(SyntaxKind.ThisConstructorInitializer) && chained.FirstAncestorOrSelf<ConstructorDeclarationSyntax>().Identifier.ValueText == this.applyTo.TypeSymbol.Name
                        select chained).ToImmutableArray();
                    var invocations = creationInvocations.Concat<CSharpSyntaxNode>(chainedInvocations);
                    var trackedTree = applyTo.TrackNodes(invocations);

                    var recursiveField = this.applyTo.RecursiveParent.RecursiveField;
                    foreach (var ctorInvocation in invocations)
                    {
                        ExpressionSyntax lookupTableValue = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

                        var currentInvocation = trackedTree.GetCurrentNode(ctorInvocation);
                        var currentCreationInvocation = currentInvocation as ObjectCreationExpressionSyntax;
                        var currentChainedInvocation = currentInvocation as ConstructorInitializerSyntax;

                        if (currentCreationInvocation != null)
                        {
                            var containingMethod = currentInvocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
                            if (containingMethod != null)
                            {
                                if (containingMethod.ParameterList.Parameters.Any(p => p.Identifier.ToString() == recursiveField.Name))
                                {
                                    // We're in a method that accepts the recursive field as a parameter.
                                    // The value we want to pass in for the lookup table is:
                                    // (children.IsDefined && children.Value != this.Children) ? default(Optional<ImmutableDictionary<uint, KeyValuePair<RecursiveType, uint>>>) : Optional.For(this.lookupTable);
                                    lookupTableValue = SyntaxFactory.ConditionalExpression(
                                        SyntaxFactory.ParenthesizedExpression(
                                            SyntaxFactory.BinaryExpression(
                                                SyntaxKind.LogicalAndExpression,
                                                Syntax.OptionalIsDefined(recursiveField.NameAsField),
                                                SyntaxFactory.BinaryExpression(
                                                    SyntaxKind.NotEqualsExpression,
                                                    Syntax.OptionalValue(recursiveField.NameAsField),
                                                    Syntax.ThisDot(recursiveField.NameAsProperty)))),
                                        SyntaxFactory.DefaultExpression(Syntax.OptionalOf(this.lookupTableType)),
                                        Syntax.OptionalFor(Syntax.ThisDot(LookupTableFieldName)));
                                }
                            }

                            var alteredInvocation = currentCreationInvocation.AddArgumentListArguments(
                                SyntaxFactory.Argument(SyntaxFactory.NameColon(LookupTableFieldName), NoneToken, lookupTableValue));
                            trackedTree = trackedTree.ReplaceNode(currentInvocation, alteredInvocation);
                        }
                        else
                        {
                            var alteredInvocation = currentChainedInvocation.AddArgumentListArguments(
                                SyntaxFactory.Argument(SyntaxFactory.NameColon(LookupTableFieldName), NoneToken, lookupTableValue));
                            trackedTree = trackedTree.ReplaceNode(currentInvocation, alteredInvocation);
                        }
                    }

                    applyTo = trackedTree;
                }

                return applyTo;
            }

            protected override void GenerateCore()
            {
                if (this.applyTo.IsRecursiveParent)
                {
                    this.baseTypes.Add(SyntaxFactory.SimpleBaseType(this.IRecursiveParentWithChildReplacementType));
                    this.innerMembers.Add(this.CreateReplaceChildMethod());

                    // private readonly uint inefficiencyLoad;
                    var inefficiencyLoadType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword));
                    this.innerMembers.Add(SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(inefficiencyLoadType)
                            .AddVariables(SyntaxFactory.VariableDeclarator(InefficiencyLoadFieldName.Identifier)))
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
                        .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(DebuggerBrowsableNeverAttribute))));

                    var interfaceType = SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)),
                        SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier(nameof(IRecursiveParentWithLookupTable<IRecursiveType>)),
                            SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(this.applyTo.RecursiveType.TypeSyntax))));
                    this.baseTypes.Add(SyntaxFactory.SimpleBaseType(interfaceType));
                    var explicitImplementation = SyntaxFactory.ExplicitInterfaceSpecifier(interfaceType);

                    // uint IRecursiveParentWithLookupTable<TRecursiveType>.InefficiencyLoad => this.ineffiencyLoad;
                    this.innerMembers.Add(
                        SyntaxFactory.PropertyDeclaration(inefficiencyLoadType, nameof(IRecursiveParentWithLookupTable<IRecursiveType>.InefficiencyLoad))
                        .WithExplicitInterfaceSpecifier(explicitImplementation)
                        .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(Syntax.ThisDot(InefficiencyLoadFieldName)))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(DebuggerBrowsableNeverAttribute))));

                    // IReadOnlyCollection<TRecursiveType> IRecursiveParentWithLookupTable<TRecursiveType>.Children => this.recursiveField;
                    this.innerMembers.Add(
                        SyntaxFactory.PropertyDeclaration(
                            Syntax.IReadOnlyCollectionOf(this.applyTo.RecursiveType.TypeSyntax),
                            nameof(IRecursiveParentWithLookupTable<IRecursiveType>.Children))
                        .WithExplicitInterfaceSpecifier(explicitImplementation)
                        .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(Syntax.ThisDot(this.applyTo.RecursiveField.NameAsProperty)))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(DebuggerBrowsableNeverAttribute))));

                    // ImmutableDictionary<IdentityFieldType, KeyValuePair<TRecursiveType, IdentityFieldType>> IRecursiveParentWithLookupTable<TRecursiveType>.LookupTable => this.lookupTable;
                    this.innerMembers.Add(
                        SyntaxFactory.PropertyDeclaration(
                            this.lookupTableType,
                            nameof(IRecursiveParentWithLookupTable<IRecursiveType>.LookupTable))
                        .WithExplicitInterfaceSpecifier(explicitImplementation)
                        .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(Syntax.ThisDot(LookupTablePropertyName)))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(DebuggerBrowsableNeverAttribute))));
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
                        this.lookupTableType,
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

                    // protected System.Collections.Immutable.ImmutableDictionary<System.UInt32, KeyValuePair<FileSystemEntry, System.UInt32>> lookupTable;
                    this.innerMembers.Add(SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(this.lookupTableType)
                            .AddVariables(SyntaxFactory.VariableDeclarator(LookupTableFieldName.Identifier)))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
                        .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(DebuggerBrowsableNeverAttribute))));

                    // public System.Collections.Immutable.ImmutableStack<TRecursiveType> GetSpine(TRecursiveType descendent) {
                    // 	return this.GetSpine<TRecursiveParent, TRecursiveType>(descendent);
                    // }
                    var descendentParameter = SyntaxFactory.IdentifierName("descendent");
                    this.innerMembers.Add(
                        SyntaxFactory.MethodDeclaration(Syntax.ImmutableStackOf(this.applyTo.RecursiveType.TypeSyntax), GetSpineMethodName.Identifier)
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                            .AddParameterListParameters(SyntaxFactory.Parameter(descendentParameter.Identifier).WithType(this.applyTo.RecursiveType.TypeSyntax))
                            .WithBody(SyntaxFactory.Block(
                                SyntaxFactory.ReturnStatement(
                                    SyntaxFactory.InvocationExpression(
                                        Syntax.ThisDot(
                                            SyntaxFactory.GenericName(nameof(RecursiveTypeExtensions.GetSpine))
                                                .AddTypeArgumentListArguments(this.applyTo.RecursiveParent.TypeSyntax, this.applyTo.RecursiveType.TypeSyntax)),
                                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(descendentParameter))))))));

                    // public System.Collections.Immutable.ImmutableStack<TRecursiveType> GetSpine(uint identity) {
                    // 	return this.GetSpine<TRecursiveParent, TRecursiveType>(identity);
                    // }
                    var identityParameter = SyntaxFactory.IdentifierName("identity");
                    this.innerMembers.Add(
                        SyntaxFactory.MethodDeclaration(Syntax.ImmutableStackOf(this.applyTo.RecursiveType.TypeSyntax), GetSpineMethodName.Identifier)
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                            .AddParameterListParameters(SyntaxFactory.Parameter(identityParameter.Identifier).WithType(IdentityFieldTypeSyntax))
                            .WithBody(SyntaxFactory.Block(
                                SyntaxFactory.ReturnStatement(
                                    SyntaxFactory.InvocationExpression(
                                        Syntax.ThisDot(
                                            SyntaxFactory.GenericName(nameof(RecursiveTypeExtensions.GetSpine))
                                                .AddTypeArgumentListArguments(this.applyTo.RecursiveParent.TypeSyntax, this.applyTo.RecursiveType.TypeSyntax)),
                                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(identityParameter))))))));

                    this.innerMembers.Add(this.CreateFindMethod());
                }
            }

            protected MemberDeclarationSyntax CreateReplaceChildMethod()
            {
                var irecursiveParentType = SyntaxFactory.QualifiedName(
                    SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)),
                    SyntaxFactory.GenericName(nameof(IRecursiveParent<IRecursiveType>))
                        .AddTypeArgumentListArguments(this.applyTo.RecursiveType.TypeSyntax));
                var oldSpineParameter = SyntaxFactory.IdentifierName("oldSpine");
                var newSpineParameter = SyntaxFactory.IdentifierName("newSpine");
                var newChildrenVar = SyntaxFactory.IdentifierName("newChildren");
                var newSelfVar = SyntaxFactory.IdentifierName("newSelf");
                var lookupTableLazySentinelVar = SyntaxFactory.IdentifierName("lookupTableLazySentinel");
                Func<ExpressionSyntax, InvocationExpressionSyntax> callPeek = receiver =>
                   SyntaxFactory.InvocationExpression(
                       SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName(nameof(ImmutableStack<int>.Peek))),
                       SyntaxFactory.ArgumentList());
                Func<ExpressionSyntax, InvocationExpressionSyntax> createDeque = stack =>
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.QualifiedName(
                                SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)),
                                SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph.ImmutableDeque))),
                            SyntaxFactory.IdentifierName(nameof(ImmutableDeque.Create))),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(stack))));

                return SyntaxFactory.MethodDeclaration(
                    irecursiveParentType,
                    nameof(IRecursiveParentWithChildReplacement<IRecursiveType>.ReplaceChild))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(IRecursiveParentWithChildReplacementType))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(oldSpineParameter.Identifier).WithType(Syntax.ImmutableStackOf(this.applyTo.RecursiveType.TypeSyntax)),
                        SyntaxFactory.Parameter(newSpineParameter.Identifier).WithType(Syntax.ImmutableStackOf(this.applyTo.RecursiveType.TypeSyntax)))
                    .WithBody(SyntaxFactory.Block(
                        // var newChildren = this.Children.Replace(oldSpine.Peek(), newSpine.Peek());
                        SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(varType)
                                .AddVariables(SyntaxFactory.VariableDeclarator(newChildrenVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            Syntax.ThisDot(this.applyTo.RecursiveField.NameAsProperty),
                                            SyntaxFactory.IdentifierName(nameof(CollectionExtensions.Replace))))
                                        .AddArgumentListArguments(
                                            SyntaxFactory.Argument(callPeek(oldSpineParameter)), // oldSpine.Peek()
                                            SyntaxFactory.Argument(callPeek(newSpineParameter)) // newSpine.Peek()
                                        ))))),
                        // var newSelf = this.With(children: newChildren);
                        SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(varType)
                                .AddVariables(SyntaxFactory.VariableDeclarator(newSelfVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(Syntax.ThisDot(WithMethodName))
                                        .AddArgumentListArguments(
                                            SyntaxFactory.Argument(SyntaxFactory.NameColon(this.applyTo.RecursiveField.NameAsField), NoneToken, newChildrenVar)))))),
                        // var lookupTableLazySentinel = RecursiveTypeExtensions.LookupTable<TRecursiveType, TRecursiveParent>.LazySentinel;
                        SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(varType)
                                .AddVariables(SyntaxFactory.VariableDeclarator(lookupTableLazySentinelVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                    GetLookupTableHelperMember(nameof(LookupTableHelper.LazySentinel)))))),
                        // if (newSelf.lookupTable == lookupTableLazySentinel && this.lookupTable != null && this.lookupTable != lookupTableLazySentinel) {
                        SyntaxFactory.IfStatement(
                            new ExpressionSyntax[] {
                                // newSelf.lookupTable == lookupTableLazySentinel
                                SyntaxFactory.BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, newSelfVar, LookupTableFieldName),
                                    lookupTableLazySentinelVar),
                                // this.lookupTable != null
                                SyntaxFactory.BinaryExpression(
                                    SyntaxKind.NotEqualsExpression,
                                    Syntax.ThisDot(LookupTableFieldName),
                                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                // this.lookupTable != lookupTableLazySentinel
                                SyntaxFactory.BinaryExpression(
                                    SyntaxKind.NotEqualsExpression,
                                    Syntax.ThisDot(LookupTableFieldName),
                                    lookupTableLazySentinelVar)
                            }.ChainBinaryExpressions(SyntaxKind.LogicalAndExpression),
                            SyntaxFactory.Block(
                                // // Our newly mutated self wants a lookup table. If we already have one we can use it,
                                // // but it needs to be fixed up given the newly rewritten spine through our descendents.
                                // newSelf.lookupTable = RecursiveTypeExtensions.LookupTable<TRecursiveType, TRecursiveParent>.Fixup(this, ImmutableDeque.Create(newSpine), ImmutableDeque.Create(oldSpine));
                                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, newSelfVar, LookupTableFieldName),
                                    SyntaxFactory.InvocationExpression(
                                        GetLookupTableHelperMember(nameof(LookupTableHelper.Fixup)))
                                        .AddArgumentListArguments(
                                            SyntaxFactory.Argument(SyntaxFactory.ThisExpression()),
                                            SyntaxFactory.Argument(createDeque(newSpineParameter)),
                                            SyntaxFactory.Argument(createDeque(oldSpineParameter))))),
                                // RecursiveTypeExtensions.LookupTable<TRecursiveType, TRecursiveParent>.ValidateInternalIntegrityDebugOnly(newSelf);
                                SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                                    GetLookupTableHelperMember(nameof(LookupTableHelper.ValidateInternalIntegrityDebugOnly)))
                                    .AddArgumentListArguments(SyntaxFactory.Argument(newSelfVar))))),
                        // return newSelf;
                        SyntaxFactory.ReturnStatement(newSelfVar)));
            }

            protected MethodDeclarationSyntax CreateFindMethod()
            {
                // public TRecursiveType Find(uint identity)
                return SyntaxFactory.MethodDeclaration(this.applyTo.RecursiveType.TypeSyntax, FindMethodName.Identifier)
                    .AddParameterListParameters(RequiredIdentityParameter)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBody(SyntaxFactory.Block(
                        // return this.Find<TRecursiveParent, TRecursiveType>(identity);
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                Syntax.ThisDot(
                                    SyntaxFactory.GenericName(FindMethodName.Identifier).AddTypeArgumentListArguments(
                                        this.applyTo.RecursiveParent.TypeSyntax,
                                        this.applyTo.RecursiveType.TypeSyntax)))
                                .AddArgumentListArguments(SyntaxFactory.Argument(IdentityParameterName)))));
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
