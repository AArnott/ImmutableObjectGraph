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
        protected class RootedStructGen : FeatureGenerator
        {
            private const string IsDerivedPropertyNameFormat = "Is{0}";
            private const string AsDerivedPropertyNameFormat = "As{0}";
            private const string AsAncestorPropertyNameFormat = "As{0}";
            private static readonly IdentifierNameSyntax ParentPropertyName = SyntaxFactory.IdentifierName("Parent");
            private static readonly IdentifierNameSyntax RootPropertyName = SyntaxFactory.IdentifierName("Root");
            private static readonly IdentifierNameSyntax IsRootPropertyName = SyntaxFactory.IdentifierName("IsRoot");
            private static readonly IdentifierNameSyntax IsDefaultPropertyName = SyntaxFactory.IdentifierName("IsDefault");
            private static readonly IdentifierNameSyntax ThrowIfDefaultMethodName = SyntaxFactory.IdentifierName("ThrowIfDefault");

            private static readonly IdentifierNameSyntax GreenNodeFieldName = SyntaxFactory.IdentifierName("greenNode");
            private static readonly IdentifierNameSyntax RootFieldName = SyntaxFactory.IdentifierName("root");
            private static readonly IdentifierNameSyntax ToRootedFieldName = SyntaxFactory.IdentifierName("toRooted");
            private static readonly IdentifierNameSyntax ToUnrootedFieldName = SyntaxFactory.IdentifierName("toUnrooted");

            private static readonly StatementSyntax CallThrowIfDefaultMethod = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    Syntax.ThisDot(ThrowIfDefaultMethodName),
                    SyntaxFactory.ArgumentList()));
            private readonly IdentifierNameSyntax typeName;
            private readonly IdentifierNameSyntax rootedRecursiveType;
            private readonly IdentifierNameSyntax rootedRecursiveParent;

            public RootedStructGen(CodeGen generator)
                : base(generator)
            {
                this.typeName = GetRootedTypeSyntax(this.applyTo);
                if (!this.applyTo.RecursiveType.IsDefault)
                {
                    this.rootedRecursiveType = GetRootedTypeSyntax(this.applyTo.RecursiveType);
                    this.rootedRecursiveParent = GetRootedTypeSyntax(this.applyTo.RecursiveParent);
                }
            }

            public override bool IsApplicable
            {
                get { return this.generator.options.DefineRootedStruct; }
            }

            protected static IdentifierNameSyntax GetRootedTypeSyntax(MetaType metaType)
            {
                return SyntaxFactory.IdentifierName("Rooted" + metaType.TypeSymbol.Name);
            }

            protected NameSyntax GetRootedCollectionTypeAdapterName()
            {
                string collectionType = string.Format(CultureInfo.InvariantCulture, "Immutable{0}RootAdapter", this.SetOrList);
                return SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)),
                        SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph.Adapters))),
                    SyntaxFactory.GenericName(collectionType).AddTypeArgumentListArguments(
                        this.applyTo.RecursiveType.TypeSyntax,
                        GetRootedTypeSyntax(this.applyTo.RecursiveType),
                        this.applyTo.RecursiveParent.TypeSyntax));
            }

            protected NameSyntax GetRootedCollectionTypePropertyType()
            {
                return SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.IdentifierName(nameof(System)),
                            SyntaxFactory.IdentifierName(nameof(System.Collections))),
                        SyntaxFactory.IdentifierName(nameof(System.Collections.Immutable))),
                    SyntaxFactory.GenericName("IImmutable" + this.SetOrList)
                        .AddTypeArgumentListArguments(GetRootedTypeSyntax(this.applyTo.RecursiveType)));
            }

            protected string SetOrList
            {
                get { return (this.applyTo.ChildrenAreOrdered && !this.applyTo.ChildrenAreSorted) ? "List" : "Set"; }
            }

            protected override void GenerateCore()
            {
                this.siblingMembers.Add(this.CreateRootedStruct());
            }

            protected StructDeclarationSyntax CreateRootedStruct()
            {
                var rootedStruct = SyntaxFactory.StructDeclaration(this.typeName.Identifier)
                    .AddModifiers(GetModifiersForAccessibility(this.applyTo.TypeSymbol))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                    .AddBaseListTypes(
                        SyntaxFactory.SimpleBaseType(Syntax.IEquatableOf(this.typeName)),
                        SyntaxFactory.SimpleBaseType(Syntax.GetTypeSyntax(typeof(IRecursiveType))))
                    .AddMembers(
                        this.CreateRootedConstructor(),
                        this.CreateRootProperty(),
                        this.CreateIdentityProperty(),
                        this.CreateIsDefaultProperty(),
                        this.CreateTemplateTypeProperty(),
                        this.CreateWithMethod(),
                        this.CreateEqualsObjectMethod(),
                        this.CreateGetHashCodeMethod(),
                        this.CreateEqualsRootedStructMethod(),
                        this.CreateThrowIfDefaultMethod())
                    .AddMembers(this.CreateFields())
                    .AddMembers(this.CreateRootedOperators())
                    .AddMembers(this.CreateIsDerivedProperties())
                    .AddMembers(this.CreateAsDerivedProperties())
                    .AddMembers(this.CreateAsAncestorProperties())
                    .AddMembers(this.CreateFieldAccessorProperties());

                if (this.applyTo.IsRecursive)
                {
                    rootedStruct = rootedStruct
                        .AddMembers(
                            this.CreateParentProperty());

                    if (!this.applyTo.TypeSymbol.IsAbstract)
                    {
                        rootedStruct = rootedStruct
                            .AddMembers(
                                this.CreateCreateMethod());
                    }

                    if (this.applyTo.IsRecursiveParentOrDerivative)
                    {
                        rootedStruct = rootedStruct
                            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(CreateIRecursiveParentOfTSyntax(this.rootedRecursiveType)))
                            .AddMembers(
                                this.CreateIsRootProperty(),
                                this.CreateFindMethod(),
                                this.CreateTryFindMethod(),
                                this.CreateGetEnumeratorMethod(),
                                this.CreateParentedNodeMethod())
                            .AddMembers(this.CreateChildrenProperties());
                    }
                }

                return rootedStruct;
            }

            protected ConstructorDeclarationSyntax CreateRootedConstructor()
            {
                // internal RootedTemplateType(TemplateType templateType, TRecursiveParent root) {
                var templateTypeParam = SyntaxFactory.IdentifierName(this.applyTo.TypeSymbol.Name.ToCamelCase());
                var rootParam = SyntaxFactory.IdentifierName("root");
                var ctor = SyntaxFactory.ConstructorDeclaration(this.typeName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(templateTypeParam.Identifier).WithType(this.applyTo.TypeSyntax),
                        SyntaxFactory.Parameter(rootParam.Identifier).WithType(this.applyTo.RecursiveParent.TypeSyntax))
                    .WithBody(SyntaxFactory.Block(
                        // this.greenNode = templateType;
                        SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            Syntax.ThisDot(GreenNodeFieldName),
                            templateTypeParam)),
                        // this.root = root;
                        SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            Syntax.ThisDot(RootFieldName),
                            rootParam))));

                if (this.applyTo.IsRecursiveParent)
                {
                    ctor = ctor.AddBodyStatements(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        Syntax.ThisDot(this.applyTo.RecursiveField.NameAsField),
                        SyntaxFactory.DefaultExpression(Syntax.OptionalOf(this.GetRootedCollectionTypeAdapterName())))));
                }

                return ctor;
            }

            protected MemberDeclarationSyntax[] CreateFields()
            {
                var fields = new List<MemberDeclarationSyntax>();

                if (this.applyTo.IsRecursiveParent)
                {
                    var rParam = SyntaxFactory.IdentifierName("r");
                    var uParam = SyntaxFactory.IdentifierName("u");
                    fields.AddRange(new MemberDeclarationSyntax[] {
                        // private static readonly System.Func<RootedRecursiveType, TRecursiveType> toUnrooted = r => r.TemplateType;
                        SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(Syntax.FuncOf(this.rootedRecursiveType, this.applyTo.RecursiveType.TypeSyntax))
                            .AddVariables(SyntaxFactory.VariableDeclarator(ToUnrootedFieldName.Identifier)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.SimpleLambdaExpression(
                                        SyntaxFactory.Parameter(rParam.Identifier),
                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, rParam, SyntaxFactory.IdentifierName(this.applyTo.RecursiveType.TypeSymbol.Name)))))))
                            .AddModifiers(
                                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
                        // private static readonly System.Func<TRecursiveType, TRecursiveParent, RootedRecursiveType> toRooted = (u, r) => new RootedRecursiveType(u, r);
                        SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(Syntax.FuncOf(this.applyTo.RecursiveType.TypeSyntax, this.applyTo.RecursiveParent.TypeSyntax, this.rootedRecursiveType))
                            .AddVariables(SyntaxFactory.VariableDeclarator(ToRootedFieldName.Identifier)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.ParenthesizedLambdaExpression(
                                        SyntaxFactory.ParameterList(Syntax.JoinSyntaxNodes(SyntaxKind.CommaToken,
                                            SyntaxFactory.Parameter(uParam.Identifier),
                                            SyntaxFactory.Parameter(rParam.Identifier))),
                                        SyntaxFactory.ObjectCreationExpression(this.rootedRecursiveType).AddArgumentListArguments(
                                            SyntaxFactory.Argument(uParam),
                                            SyntaxFactory.Argument(rParam)))))))
                            .AddModifiers(
                                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
                        // private Optional<Adapters.ImmutableSetRootAdapter<TRecursiveType, RootedRecursiveType, TRecursiveParent>> children;
                        SyntaxFactory.FieldDeclaration(
                            SyntaxFactory.VariableDeclaration(Syntax.OptionalOf(this.GetRootedCollectionTypeAdapterName())).AddVariables(
                                SyntaxFactory.VariableDeclarator(this.applyTo.RecursiveField.NameAsField.Identifier)))
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)),
                    });
                }

                fields.AddRange(new MemberDeclarationSyntax[] {
                    // private readonly TemplateType greenNode;
                    SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(this.applyTo.TypeSyntax)
                        .AddVariables(SyntaxFactory.VariableDeclarator(GreenNodeFieldName.Identifier)))
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
                    // private readonly TRecursiveParent root;
                    SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(this.applyTo.RecursiveParent.TypeSyntax)
                        .AddVariables(SyntaxFactory.VariableDeclarator(RootFieldName.Identifier)))
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
                });

                return fields.ToArray();
            }

            protected MemberDeclarationSyntax[] CreateRootedOperators()
            {
                var publicStaticModifiers = new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword) };
                var rootedParameter = SyntaxFactory.IdentifierName("rooted");
                var thatParameter = SyntaxFactory.IdentifierName("that");
                var otherParameter = SyntaxFactory.IdentifierName("other");

                return new MemberDeclarationSyntax[] {
                    // public static implicit operator TemplateType(RootedTemplateType rooted)
                    SyntaxFactory.ConversionOperatorDeclaration(SyntaxFactory.Token(SyntaxKind.ImplicitKeyword), this.applyTo.TypeSyntax)
                        .AddModifiers(publicStaticModifiers)
                        .AddParameterListParameters(SyntaxFactory.Parameter(rootedParameter.Identifier).WithType(this.typeName))
                        .WithBody(SyntaxFactory.Block(
                            // return rooted.FileSystemDirectory;
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    rootedParameter,
                                    SyntaxFactory.IdentifierName(this.applyTo.TypeSymbol.Name))))),
                    // public static bool operator ==(RootedTemplateType that, RootedTemplateType other)
                    SyntaxFactory.OperatorDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), SyntaxFactory.Token(SyntaxKind.EqualsEqualsToken))
                        .AddModifiers(publicStaticModifiers)
                        .AddParameterListParameters(
                            SyntaxFactory.Parameter(thatParameter.Identifier).WithType(this.typeName),
                            SyntaxFactory.Parameter(otherParameter.Identifier).WithType(this.typeName))
                        .WithBody(SyntaxFactory.Block(
                            // return that.greenNode == other.greenNode && that.root == other.root;
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.BinaryExpression(
                                    SyntaxKind.LogicalAndExpression,
                                    SyntaxFactory.BinaryExpression(
                                        SyntaxKind.EqualsExpression,
                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, thatParameter, GreenNodeFieldName),
                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, otherParameter, GreenNodeFieldName)),
                                    SyntaxFactory.BinaryExpression(
                                        SyntaxKind.EqualsExpression,
                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, thatParameter, RootFieldName),
                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, otherParameter, RootFieldName)))))),
                    // public static bool operator !=(RootedTemplateType that, RootedTemplateType other)
                    SyntaxFactory.OperatorDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), SyntaxFactory.Token(SyntaxKind.ExclamationEqualsToken))
                        .AddModifiers(publicStaticModifiers)
                        .AddParameterListParameters(
                            SyntaxFactory.Parameter(thatParameter.Identifier).WithType(this.typeName),
                            SyntaxFactory.Parameter(otherParameter.Identifier).WithType(this.typeName))
                        .WithBody(SyntaxFactory.Block(
                            // return !(that == other);
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.PrefixUnaryExpression(
                                    SyntaxKind.LogicalNotExpression,
                                    SyntaxFactory.ParenthesizedExpression(
                                        SyntaxFactory.BinaryExpression(
                                            SyntaxKind.EqualsExpression,
                                            thatParameter,
                                            otherParameter)))))),
                };
            }

            protected PropertyDeclarationSyntax CreateParentProperty()
            {
                var greenParentVar = SyntaxFactory.IdentifierName("greenParent");
                return SyntaxFactory.PropertyDeclaration(this.applyTo.RecursiveParent.TypeSyntax, ParentPropertyName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(
                            // this.ThrowIfDefault();
                            CallThrowIfDefaultMethod,
                            // var greenParent = this.root.GetParent(this.greenNode);
                            SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                                SyntaxFactory.VariableDeclarator(greenParentVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            Syntax.ThisDot(RootFieldName),
                                            EnumerableRecursiveParentGen.GetParentMethodName)).AddArgumentListArguments(
                                        SyntaxFactory.Argument(Syntax.ThisDot(GreenNodeFieldName))))))),
                            // return greenParent != null ? new RootedRecursiveParent(greenParent, this.root) : default(RootedRecursiveParent);
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.ConditionalExpression(
                                    SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, greenParentVar, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                    SyntaxFactory.ObjectCreationExpression(this.rootedRecursiveParent).AddArgumentListArguments(
                                        SyntaxFactory.Argument(greenParentVar),
                                        SyntaxFactory.Argument(Syntax.ThisDot(RootFieldName))),
                                    SyntaxFactory.DefaultExpression(this.rootedRecursiveParent))))));
            }

            protected PropertyDeclarationSyntax CreateRootProperty()
            {
                return SyntaxFactory.PropertyDeclaration(this.applyTo.RecursiveParent.TypeSyntax, RootPropertyName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(ThrowNotImplementedException)));
            }

            protected PropertyDeclarationSyntax CreateIdentityProperty()
            {
                return SyntaxFactory.PropertyDeclaration(IdentityFieldTypeSyntax, IdentityPropertyName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(
                            // this.ThrowIfDefault();
                            CallThrowIfDefaultMethod,
                            // return this.greenNode.Identity;
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, Syntax.ThisDot(GreenNodeFieldName), IdentityPropertyName)))));
            }

            protected PropertyDeclarationSyntax CreateIsRootProperty()
            {
                // public bool IsRoot { get; }
                return SyntaxFactory.PropertyDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), IsRootPropertyName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(
                            // return this.root == this.greenNode;
                            SyntaxFactory.ReturnStatement(SyntaxFactory.BinaryExpression(
                                SyntaxKind.EqualsExpression,
                                Syntax.ThisDot(RootFieldName),
                                Syntax.ThisDot(GreenNodeFieldName))))));
            }

            protected PropertyDeclarationSyntax CreateIsDefaultProperty()
            {
                // public bool IsDefault { get; }
                return SyntaxFactory.PropertyDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), IsDefaultPropertyName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(
                            // return this.greenNode == null;
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    Syntax.ThisDot(GreenNodeFieldName),
                                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))))));
            }

            protected PropertyDeclarationSyntax CreateTemplateTypeProperty()
            {
                return SyntaxFactory.PropertyDeclaration(this.applyTo.TypeSyntax, this.applyTo.TypeSymbol.Name)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(
                            // return this.greenNode;
                            SyntaxFactory.ReturnStatement(Syntax.ThisDot(GreenNodeFieldName)))));
            }

            protected MethodDeclarationSyntax CreateWithMethod()
            {
                return SyntaxFactory.MethodDeclaration(this.typeName, WithMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithParameterList(this.generator.CreateParameterList(this.applyTo.AllFields, ParameterStyle.Optional))
                    .WithBody(SyntaxFactory.Block(ThrowNotImplementedException));
            }

            protected MethodDeclarationSyntax CreateEqualsObjectMethod()
            {
                // public override bool Equals(object obj)
                var objParam = SyntaxFactory.IdentifierName("obj");
                var otherVar = SyntaxFactory.IdentifierName("other");
                return SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), nameof(object.Equals))
                   .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
                   .AddParameterListParameters(SyntaxFactory.Parameter(objParam.Identifier).WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))))
                   .WithBody(SyntaxFactory.Block(
                       // if (obj is RootedTemplateType) {
                       SyntaxFactory.IfStatement(
                           SyntaxFactory.BinaryExpression(
                               SyntaxKind.IsExpression,
                               objParam,
                               this.typeName),
                           SyntaxFactory.Block(
                               // var other = (RootedTemplateType)obj;
                               SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                                   SyntaxFactory.VariableDeclarator(otherVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                       SyntaxFactory.CastExpression(this.typeName, objParam))))),
                               // return this.Equals(other);
                               SyntaxFactory.ReturnStatement(
                                   SyntaxFactory.InvocationExpression(Syntax.ThisDot(SyntaxFactory.IdentifierName(nameof(IEquatable<int>.Equals))))
                                    .AddArgumentListArguments(SyntaxFactory.Argument(otherVar))))),
                       // return false;
                       SyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))));
            }

            protected MethodDeclarationSyntax CreateGetHashCodeMethod()
            {
                // public override int GetHashCode()
                return SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)), nameof(object.GetHashCode))
                   .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
                   .WithBody(SyntaxFactory.Block(
                       // return this.greenNode?.GetHashCode() ?? 0;
                       SyntaxFactory.ReturnStatement(SyntaxFactory.BinaryExpression(
                           SyntaxKind.CoalesceExpression,
                           SyntaxFactory.InvocationExpression(
                               SyntaxFactory.ConditionalAccessExpression(
                                   Syntax.ThisDot(GreenNodeFieldName),
                                   SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName(nameof(GetHashCode)))),
                               SyntaxFactory.ArgumentList()),
                           SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))))));
            }

            protected MethodDeclarationSyntax CreateEqualsRootedStructMethod()
            {
                // public bool Equals(RootedTemplateType other)
                var otherParam = SyntaxFactory.IdentifierName("other");
                return SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), nameof(IEquatable<object>.Equals))
                   .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                   .AddParameterListParameters(SyntaxFactory.Parameter(otherParam.Identifier).WithType(this.typeName))
                   .WithBody(SyntaxFactory.Block(
                       // return this.greenNode == other.greenNode && this.root == other.root;
                       SyntaxFactory.ReturnStatement(
                           SyntaxFactory.BinaryExpression(
                               SyntaxKind.LogicalAndExpression,
                               SyntaxFactory.BinaryExpression(
                                   SyntaxKind.EqualsExpression,
                                   Syntax.ThisDot(GreenNodeFieldName),
                                   SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, otherParam, GreenNodeFieldName)),
                               SyntaxFactory.BinaryExpression(
                                   SyntaxKind.EqualsExpression,
                                   Syntax.ThisDot(RootFieldName),
                                   SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, otherParam, RootFieldName))))));
            }

            protected MethodDeclarationSyntax CreateThrowIfDefaultMethod()
            {
                // private void ThrowIfDefault()
                return SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    ThrowIfDefaultMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                    .WithBody(SyntaxFactory.Block(
                        // if (this.greenNode == null) {
                        SyntaxFactory.IfStatement(
                            SyntaxFactory.BinaryExpression(
                                SyntaxKind.EqualsExpression,
                                Syntax.ThisDot(GreenNodeFieldName),
                                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        SyntaxFactory.Block(
                            // throw new InvalidOperationException();
                            SyntaxFactory.ThrowStatement(SyntaxFactory.ObjectCreationExpression(
                                Syntax.GetTypeSyntax(typeof(InvalidOperationException)),
                                SyntaxFactory.ArgumentList(),
                                null))))));
            }

            protected MethodDeclarationSyntax CreateCreateMethod()
            {
                return SyntaxFactory.MethodDeclaration(this.typeName, CreateMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithParameterList(this.generator.CreateParameterList(this.applyTo.AllFields, ParameterStyle.OptionalOrRequired))
                    .WithBody(SyntaxFactory.Block(ThrowNotImplementedException));
            }

            protected MethodDeclarationSyntax CreateFindMethod()
            {
                // public TRecursiveType Find(uint identity)
                return SyntaxFactory.MethodDeclaration(this.applyTo.RecursiveType.TypeSyntax, SyntaxFactory.Identifier(nameof(RecursiveTypeExtensions.Find)))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(IdentityParameterName.Identifier).WithType(IdentityFieldTypeSyntax))
                    .WithBody(SyntaxFactory.Block(ThrowNotImplementedException));
            }

            protected MethodDeclarationSyntax CreateTryFindMethod()
            {
                // public bool TryFind(uint identity, out TRecursiveType value)
                var valueParameter = SyntaxFactory.IdentifierName("value");
                return SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), SyntaxFactory.Identifier(nameof(RecursiveTypeExtensions.TryFind)))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(IdentityParameterName.Identifier).WithType(IdentityFieldTypeSyntax),
                        SyntaxFactory.Parameter(valueParameter.Identifier).WithType(this.applyTo.RecursiveType.TypeSyntax).AddModifiers(SyntaxFactory.Token(SyntaxKind.OutKeyword)))
                    .WithBody(SyntaxFactory.Block(ThrowNotImplementedException));
            }

            protected MethodDeclarationSyntax CreateGetEnumeratorMethod()
            {
                // public IEnumerator<TRecursiveType> GetEnumerator()
                return SyntaxFactory.MethodDeclaration(Syntax.IEnumeratorOf(this.applyTo.RecursiveType.TypeSyntax), nameof(IEnumerable<int>.GetEnumerator))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBody(SyntaxFactory.Block(ThrowNotImplementedException));
            }

            protected MethodDeclarationSyntax CreateParentedNodeMethod()
            {
                // ParentedRecursiveType<IRecursiveParent<IRecursiveType>, IRecursiveType> IRecursiveParent.GetParentedNode(uint identity)
                return SyntaxFactory.MethodDeclaration(
                    Syntax.GetTypeSyntax(typeof(ParentedRecursiveType<IRecursiveParent<IRecursiveType>, IRecursiveType>)),
                    nameof(IRecursiveParent.GetParentedNode))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(Syntax.GetTypeSyntax(typeof(IRecursiveParent))))
                    .AddParameterListParameters(RequiredIdentityParameter)
                    .WithBody(SyntaxFactory.Block(ThrowNotImplementedException));
            }

            protected PropertyDeclarationSyntax[] CreateChildrenProperties()
            {
                return new PropertyDeclarationSyntax[] {
                    // IEnumerable<IRecursiveType> IRecursiveParent.Children { get; }
                    SyntaxFactory.PropertyDeclaration(Syntax.IEnumerableOf(Syntax.GetTypeSyntax(typeof(IRecursiveType))), nameof(IRecursiveParent.Children))
                        .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(Syntax.GetTypeSyntax(typeof(IRecursiveParent))))
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(ThrowNotImplementedException))),
                    // IEnumerable<TRootedRecursiveType> IRecursiveParent<TRootedRecursiveType>.Children { get; }
                    SyntaxFactory.PropertyDeclaration(Syntax.IEnumerableOf(this.rootedRecursiveType), nameof(IRecursiveParent<IRecursiveType>.Children))
                        .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(CreateIRecursiveParentOfTSyntax(this.rootedRecursiveType)))
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(ThrowNotImplementedException))),
                };
            }

            protected PropertyDeclarationSyntax[] CreateIsDerivedProperties()
            {
                return this.applyTo.Descendents.Select(descendent =>
                    SyntaxFactory.PropertyDeclaration(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                        string.Format(CultureInfo.InvariantCulture, IsDerivedPropertyNameFormat, descendent.TypeSymbol.Name))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(ThrowNotImplementedException)))
                    ).ToArray();
            }

            protected PropertyDeclarationSyntax[] CreateAsDerivedProperties()
            {
                var downcastVar = SyntaxFactory.IdentifierName("downcast");
                return this.applyTo.Descendents.Select(descendent =>
                    // public RootedDerivedType AsDerivedType { get; }
                    SyntaxFactory.PropertyDeclaration(
                        GetRootedTypeSyntax(descendent),
                        string.Format(CultureInfo.InvariantCulture, AsDerivedPropertyNameFormat, descendent.TypeSymbol.Name))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(
                                // var downcast = this.greenNode as <#=greenDescendent.TypeName#>;
                                SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                                    SyntaxFactory.VariableDeclarator(downcastVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, Syntax.ThisDot(GreenNodeFieldName), descendent.TypeSyntax))))),
                                // return downcast != null ? new RootedDerivedType(downcast, this.root) : default(RootedDerivedType);
                                SyntaxFactory.ReturnStatement(
                                    SyntaxFactory.ConditionalExpression(
                                        SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, downcastVar, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                        SyntaxFactory.ObjectCreationExpression(GetRootedTypeSyntax(descendent)).AddArgumentListArguments(
                                            SyntaxFactory.Argument(downcastVar),
                                            SyntaxFactory.Argument(Syntax.ThisDot(RootFieldName))),
                                        SyntaxFactory.DefaultExpression(GetRootedTypeSyntax(descendent)))))))
                    ).ToArray();
            }

            protected PropertyDeclarationSyntax[] CreateAsAncestorProperties()
            {
                return this.applyTo.Ancestors.Select(descendent =>
                    SyntaxFactory.PropertyDeclaration(
                        descendent.TypeSyntax,
                        string.Format(CultureInfo.InvariantCulture, AsAncestorPropertyNameFormat, descendent.TypeSymbol.Name))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(ThrowNotImplementedException)))
                    ).ToArray();
            }

            protected PropertyDeclarationSyntax[] CreateFieldAccessorProperties()
            {
                return this.applyTo.AllFields.Select(field =>
                    SyntaxFactory.PropertyDeclaration(
                        field.IsRecursiveCollection ? this.GetRootedCollectionTypePropertyType() : field.TypeSyntax,
                        field.NameAsProperty.Identifier)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            field.IsRecursiveCollection
                                ? SyntaxFactory.Block(
                                    SyntaxFactory.IfStatement(
                                        SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, Syntax.OptionalIsDefined(Syntax.ThisDot(field.NameAsField))),
                                        SyntaxFactory.Block(
                                            // this.ThrowIfDefault();
                                            CallThrowIfDefaultMethod,
                                            // this.<#= field.NameCamelCase #> = Optional.For(Adapter.Create(this.greenNode.<#= field.NamePascalCase #>, toRooted, toUnrooted, this.root));
                                            SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                                                SyntaxKind.SimpleAssignmentExpression,
                                                Syntax.ThisDot(field.NameAsField),
                                                Syntax.OptionalFor(
                                                    SyntaxFactory.InvocationExpression(
                                                        SyntaxFactory.MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)), SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph.Adapter))),
                                                            SyntaxFactory.IdentifierName(nameof(Adapter.Create))))
                                                        .AddArgumentListArguments(
                                                            SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, Syntax.ThisDot(GreenNodeFieldName), field.NameAsProperty)),
                                                            SyntaxFactory.Argument(ToRootedFieldName),
                                                            SyntaxFactory.Argument(ToUnrootedFieldName),
                                                            SyntaxFactory.Argument(Syntax.ThisDot(RootFieldName)))))))),
                                    SyntaxFactory.ReturnStatement(
                                        Syntax.OptionalValue(Syntax.ThisDot(field.NameAsField))))
                                : SyntaxFactory.Block(
                                    // this.ThrowIfDefault();
                                    CallThrowIfDefaultMethod,
                                    // return this.greenNode.SomeProperty;
                                    SyntaxFactory.ReturnStatement(
                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, Syntax.ThisDot(GreenNodeFieldName), field.NameAsProperty)))))
                    ).ToArray();
            }
        }
    }
}
