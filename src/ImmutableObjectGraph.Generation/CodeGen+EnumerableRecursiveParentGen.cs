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
    using ParentedRecursiveTypeNonGeneric = ParentedRecursiveType<IRecursiveParent<IRecursiveType>, IRecursiveType>;

    public partial class CodeGen
    {
        protected class EnumerableRecursiveParentGen : FeatureGenerator
        {
            internal static readonly IdentifierNameSyntax GetParentMethodName = SyntaxFactory.IdentifierName("GetParent");

            public EnumerableRecursiveParentGen(CodeGen generator)
                : base(generator)
            {
            }

            public override bool IsApplicable
            {
                get { return this.generator.applyToMetaType.IsRecursiveParent; }
            }

            protected override void GenerateCore()
            {
                this.ImplementIEnumerableInterfaces();

                if (this.generator.applyToMetaType.ChildrenAreOrdered)
                {
                    this.ImplementOrderedChildrenInterface();
                }

                if (this.generator.applyToMetaType.ChildrenAreSorted)
                {
                    this.ImplementSortedChildrenInterface();
                }

                this.ImplementRecursiveParentInterface();

                this.innerMembers.Add(this.CreateGetParentMethod());
            }

            private MethodDeclarationSyntax CreateGetParentMethod()
            {
                // public TRecursiveParent GetParent(TRecursiveType descendent)
                var descendentParam = SyntaxFactory.IdentifierName("descendent");
                return SyntaxFactory.MethodDeclaration(this.applyTo.RecursiveParent.TypeSyntax, GetParentMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(SyntaxFactory.Parameter(descendentParam.Identifier).WithType(this.applyTo.RecursiveType.TypeSyntax))
                    .WithBody(SyntaxFactory.Block(
                        // return this.GetParent<TRecursiveParent, TRecursiveType>(descendent);
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                Syntax.ThisDot(
                                    SyntaxFactory.GenericName(nameof(RecursiveTypeExtensions.GetParent))
                                        .AddTypeArgumentListArguments(this.applyTo.RecursiveParent.TypeSyntax, this.applyTo.RecursiveType.TypeSyntax)))
                                .AddArgumentListArguments(SyntaxFactory.Argument(descendentParam)))));
            }

            private void ImplementIEnumerableInterfaces()
            {
                this.baseTypes.Add(SyntaxFactory.SimpleBaseType(Syntax.IEnumerableOf(this.generator.applyToMetaType.RecursiveType.TypeSyntax)));

                // return this.<#=templateType.RecursiveField.NameCamelCase#>.GetEnumerator();
                var body = SyntaxFactory.Block(
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                Syntax.ThisDot(SyntaxFactory.IdentifierName(this.generator.applyToMetaType.RecursiveField.Name)),
                                SyntaxFactory.IdentifierName(nameof(IEnumerable<int>.GetEnumerator))),
                            SyntaxFactory.ArgumentList())));

                // public System.Collections.Generic.IEnumerator<RecursiveType> GetEnumerator()
                this.innerMembers.Add(
                    SyntaxFactory.MethodDeclaration(
                        Syntax.IEnumeratorOf(GetFullyQualifiedSymbolName(this.generator.applyToMetaType.RecursiveField.ElementType)),
                        nameof(IEnumerable<int>.GetEnumerator))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBody(body));

                // System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
                this.innerMembers.Add(
                    SyntaxFactory.MethodDeclaration(
                        Syntax.GetTypeSyntax(typeof(System.Collections.IEnumerator)),
                        nameof(IEnumerable<int>.GetEnumerator))
                    .WithExplicitInterfaceSpecifier(
                        SyntaxFactory.ExplicitInterfaceSpecifier(
                            SyntaxFactory.QualifiedName(
                                SyntaxFactory.QualifiedName(
                                    SyntaxFactory.IdentifierName(nameof(System)),
                                    SyntaxFactory.IdentifierName(nameof(System.Collections))),
                                SyntaxFactory.IdentifierName(nameof(System.Collections.IEnumerable)))))
                    .WithBody(body));
            }

            private void ImplementRecursiveParentInterface()
            {
                var irecursiveParentOfT = CreateIRecursiveParentOfTSyntax(GetFullyQualifiedSymbolName(this.generator.applyToMetaType.RecursiveType.TypeSymbol));
                this.baseTypes.Add(SyntaxFactory.SimpleBaseType(irecursiveParentOfT));

                // this.Children;
                var thisDotChildren = Syntax.ThisDot(SyntaxFactory.IdentifierName(this.generator.applyToMetaType.RecursiveField.Name.ToPascalCase()));

                // System.Collections.Generic.IEnumerable<IRecursiveType> IRecursiveParent.Children
                this.innerMembers.Add(
                    SyntaxFactory.PropertyDeclaration(
                        Syntax.GetTypeSyntax(typeof(IEnumerable<IRecursiveType>)),
                        nameof(IRecursiveParent.Children))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(Syntax.GetTypeSyntax(typeof(IRecursiveParent))))
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(thisDotChildren))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(DebuggerBrowsableNeverAttribute))));

                // public ParentedRecursiveType<TRecursiveParent, TRecursiveType> GetParentedNode(uint identity)
                this.innerMembers.Add(
                    SyntaxFactory.MethodDeclaration(
                        SyntaxFactory.GenericName(nameof(ParentedRecursiveType<IRecursiveParent<IRecursiveType>, IRecursiveType>)).AddTypeArgumentListArguments(
                            this.applyTo.RecursiveParent.TypeSyntax,
                            this.applyTo.RecursiveType.TypeSyntax),
                        nameof(IRecursiveParent.GetParentedNode))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    //.WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(Syntax.GetTypeSyntax(typeof(IRecursiveParent))))
                    .AddParameterListParameters(RequiredIdentityParameter)
                    .WithBody(SyntaxFactory.Block(
                        // return this.GetParentedNode<TRecursiveParent, TRecursiveType>(identity);
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                Syntax.ThisDot(SyntaxFactory.GenericName(nameof(RecursiveTypeExtensions.GetParentedNode)).AddTypeArgumentListArguments(
                                    this.applyTo.RecursiveParent.TypeSyntax,
                                    this.applyTo.RecursiveType.TypeSyntax)))
                                .AddArgumentListArguments(SyntaxFactory.Argument(IdentityParameterName))))));

                // ParentedRecursiveType<IRecursiveParent<IRecursiveType>, IRecursiveType> IRecursiveParent.GetParentedNode(<#= templateType.RequiredIdentityField.TypeName #> identity) {
                var parentedVar = SyntaxFactory.IdentifierName("parented");
                var returnType = Syntax.GetTypeSyntax(typeof(ParentedRecursiveTypeNonGeneric));
                this.innerMembers.Add(
                    SyntaxFactory.MethodDeclaration(
                        returnType,
                        nameof(IRecursiveParent.GetParentedNode))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(Syntax.GetTypeSyntax(typeof(IRecursiveParent))))
                    .AddParameterListParameters(RequiredIdentityParameter)
                    .WithBody(SyntaxFactory.Block(
                        // var parented = this.GetParentedNode(identity);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(parentedVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(Syntax.ThisDot(SyntaxFactory.IdentifierName(nameof(RecursiveTypeExtensions.GetParentedNode))))
                                    .AddArgumentListArguments(SyntaxFactory.Argument(IdentityParameterName)))))),
                        // return new ParentedRecursiveType<IRecursiveParent<IRecursiveType>, IRecursiveType>(parented.Value, parented.Parent);
                        SyntaxFactory.ReturnStatement(SyntaxFactory.ObjectCreationExpression(returnType).AddArgumentListArguments(
                            SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parentedVar, SyntaxFactory.IdentifierName(nameof(ParentedRecursiveTypeNonGeneric.Value)))),
                            SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parentedVar, SyntaxFactory.IdentifierName(nameof(ParentedRecursiveTypeNonGeneric.Parent)))))))));

                ////System.Collections.Generic.IEnumerable<<#= templateType.RecursiveType.TypeName #>> IRecursiveParent<<#= templateType.RecursiveType.TypeName #>>.Children
                ////	=> return this.Children;
                this.innerMembers.Add(
                    SyntaxFactory.PropertyDeclaration(
                        Syntax.IEnumerableOf(this.generator.applyToMetaType.RecursiveType.TypeSyntax),
                        nameof(IRecursiveParent<IRecursiveType>.Children))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(irecursiveParentOfT))
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(thisDotChildren))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(DebuggerBrowsableNeverAttribute))));
            }

            private void ImplementOrderedChildrenInterface()
            {
                // We only need to declare this interface if the children are not sorted,
                // since sorted children merit a derived interface making this redundant.
                if (!this.generator.applyToMetaType.ChildrenAreSorted)
                {
                    this.baseTypes.Add(SyntaxFactory.SimpleBaseType(Syntax.GetTypeSyntax(typeof(IRecursiveParentWithOrderedChildren))));
                }

                // int IRecursiveParentWithOrderedChildren.IndexOf(IRecursiveType value)
                var valueParameterName = SyntaxFactory.IdentifierName("value");
                this.innerMembers.Add(SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                    nameof(IRecursiveParentWithOrderedChildren.IndexOf))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.IdentifierName(nameof(IRecursiveParentWithOrderedChildren))))
                    .AddParameterListParameters(SyntaxFactory.Parameter(valueParameterName.Identifier).WithType(Syntax.GetTypeSyntax(typeof(IRecursiveType))))
                    .WithBody(SyntaxFactory.Block(
                        // return this.Children.IndexOf((<#= templateType.RecursiveType.TypeName #>)value);
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    Syntax.ThisDot(SyntaxFactory.IdentifierName(this.generator.applyToMetaType.RecursiveField.Name.ToPascalCase())),
                                    SyntaxFactory.IdentifierName(nameof(IList<int>.IndexOf))),
                                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.CastExpression(
                                            this.generator.applyToMetaType.RecursiveType.TypeSyntax,
                                            valueParameterName)))))))));

            }

            private void ImplementSortedChildrenInterface()
            {
                this.baseTypes.Add(SyntaxFactory.SimpleBaseType(Syntax.GetTypeSyntax(typeof(IRecursiveParentWithSortedChildren))));

                // int IRecursiveParentWithSortedChildren.Compare(IRecursiveType first, IRecursiveType second)
                var firstParameterName = SyntaxFactory.IdentifierName("first");
                var secondParameterName = SyntaxFactory.IdentifierName("second");
                this.innerMembers.Add(SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                    nameof(IRecursiveParentWithSortedChildren.Compare))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.IdentifierName(nameof(IRecursiveParentWithSortedChildren))))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(firstParameterName.Identifier).WithType(Syntax.GetTypeSyntax(typeof(IRecursiveType))),
                        SyntaxFactory.Parameter(secondParameterName.Identifier).WithType(Syntax.GetTypeSyntax(typeof(IRecursiveType))))
                    .WithBody(SyntaxFactory.Block(
                        // return this.Children.KeyComparer.Compare((<#= templateType.RecursiveType.TypeName #>)first, (<#= templateType.RecursiveType.TypeName #>)second);
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        Syntax.ThisDot(SyntaxFactory.IdentifierName(this.generator.applyToMetaType.RecursiveField.Name.ToPascalCase())),
                                        SyntaxFactory.IdentifierName(nameof(ImmutableSortedSet<int>.KeyComparer))),
                                    SyntaxFactory.IdentifierName(nameof(IComparer<int>.Compare))),
                                SyntaxFactory.ArgumentList(Syntax.JoinSyntaxNodes(SyntaxKind.CommaToken,
                                    SyntaxFactory.Argument(SyntaxFactory.CastExpression(this.generator.applyToMetaType.RecursiveType.TypeSyntax, firstParameterName)),
                                    SyntaxFactory.Argument(SyntaxFactory.CastExpression(this.generator.applyToMetaType.RecursiveType.TypeSyntax, secondParameterName)))))))));
            }
        }
    }
}
