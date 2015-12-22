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
        protected class RootedStructGen : FeatureGenerator
        {
            private const string IsDerivedPropertyNameFormat = "Is{0}";
            private const string AsDerivedPropertyNameFormat = "As{0}";
            private const string AsAncestorPropertyNameFormat = "As{0}";
            private static readonly IdentifierNameSyntax ParentPropertyName = SyntaxFactory.IdentifierName("Parent");
            private static readonly IdentifierNameSyntax AsRootPropertyName = SyntaxFactory.IdentifierName("AsRoot");
            private static readonly IdentifierNameSyntax WithRootMethodName = SyntaxFactory.IdentifierName("WithRoot");
            private static readonly IdentifierNameSyntax RootPropertyName = SyntaxFactory.IdentifierName("Root");
            private static readonly IdentifierNameSyntax IsRootPropertyName = SyntaxFactory.IdentifierName("IsRoot");
            private static readonly IdentifierNameSyntax IsDefaultPropertyName = SyntaxFactory.IdentifierName("IsDefault");
            private static readonly IdentifierNameSyntax ThrowIfDefaultMethodName = SyntaxFactory.IdentifierName("ThrowIfDefault");
            private static readonly IdentifierNameSyntax TryFindMethodName = SyntaxFactory.IdentifierName(nameof(RecursiveTypeExtensions.TryFind));
            private static readonly IdentifierNameSyntax NewSpineMethodName = SyntaxFactory.IdentifierName("NewSpine");
            private static readonly IdentifierNameSyntax ChangesSinceMethodName = SyntaxFactory.IdentifierName("ChangesSince");

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
                if (!this.applyTo.RecursiveTypeFromFamily.IsDefault)
                {
                    this.rootedRecursiveType = GetRootedTypeSyntax(this.applyTo.RecursiveTypeFromFamily);
                    this.rootedRecursiveParent = GetRootedTypeSyntax(this.applyTo.RecursiveParent);
                }
            }

            public override bool IsApplicable
            {
                get
                {
                    if (this.generator.options.DefineRootedStruct)
                    {
                        if (this.applyTo.RecursiveParent.IsDefault)
                        {
                            this.generator.ReportDiagnostic(
                                Diagnostics.NotApplicableSetting,
                                this.generator.GetOptionArgumentSyntax(nameof(GenerateImmutableAttribute.DefineRootedStruct)),
                                nameof(GenerateImmutableAttribute.DefineRootedStruct));
                            return false;
                        }

                        return true;
                    }

                    return false;
                }
            }

            protected static IdentifierNameSyntax GetRootedTypeSyntax(MetaType metaType)
            {
                Requires.Argument(!metaType.IsDefault, nameof(metaType), "Undefined type.");

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
                        this.applyTo.RecursiveTypeFromFamily.TypeSyntax,
                        GetRootedTypeSyntax(this.applyTo.RecursiveTypeFromFamily),
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
                        .AddTypeArgumentListArguments(GetRootedTypeSyntax(this.applyTo.RecursiveTypeFromFamily)));
            }

            protected string SetOrList
            {
                get { return (this.applyTo.ChildrenAreOrdered && !this.applyTo.ChildrenAreSorted) ? "List" : "Set"; }
            }

            protected override void GenerateCore()
            {
                this.siblingMembers.Add(this.CreateRootedStruct());

                this.innerMembers.Add(this.CreateWithRootMethod());

                if (this.applyTo.IsRecursiveParentOrDerivative)
                {
                    this.innerMembers.Add(this.CreateAsRootProperty());
                }
            }

            protected MemberDeclarationSyntax CreateAsRootProperty()
            {
                // public RootedRecursiveParent Root { get; }
                var property = SyntaxFactory.PropertyDeclaration(GetRootedTypeSyntax(this.applyTo), AsRootPropertyName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithExpressionBody(
                        SyntaxFactory.ArrowExpressionClause(
                            // => new RootedRecursiveParent(this, this);
                            SyntaxFactory.ObjectCreationExpression(GetRootedTypeSyntax(this.applyTo)).AddArgumentListArguments(
                                SyntaxFactory.Argument(SyntaxFactory.ThisExpression()),
                                SyntaxFactory.Argument(SyntaxFactory.ThisExpression()))))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

                if (!this.applyTo.IsRecursiveParent)
                {
                    property = Syntax.AddNewKeyword(property);
                }

                return property;
            }

            protected MemberDeclarationSyntax CreateWithRootMethod()
            {
                var rootParam = SyntaxFactory.IdentifierName("root");
                var spineVar = SyntaxFactory.IdentifierName("spine");

                // public RootedTemplateType WithRoot(TRecursiveParent root)
                var method = SyntaxFactory.MethodDeclaration(GetRootedTypeSyntax(this.applyTo), WithRootMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(SyntaxFactory.Parameter(rootParam.Identifier).WithType(this.applyTo.RecursiveParent.TypeSyntax))
                    .WithBody(SyntaxFactory.Block(
                        // var spine = root.GetSpine(this);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(spineVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, rootParam, FastSpineGen.GetSpineMethodName))
                                    .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.ThisExpression())))))),
                        // if (spine.IsEmpty)
                        SyntaxFactory.IfStatement(
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, spineVar, SyntaxFactory.IdentifierName(nameof(ImmutableStack<int>.IsEmpty))),
                            SyntaxFactory.Block(
                                // throw new System.ArgumentException("Root does not belong to the same tree.");
                                SyntaxFactory.ThrowStatement(SyntaxFactory.ObjectCreationExpression(Syntax.GetTypeSyntax(typeof(ArgumentException))).AddArgumentListArguments(
                                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("Root does not belong to the same tree."))))))),
                        // return new <#= redType.TypeName #>(this, root);
                        SyntaxFactory.ReturnStatement(SyntaxFactory.ObjectCreationExpression(GetRootedTypeSyntax(this.applyTo)).AddArgumentListArguments(
                            SyntaxFactory.Argument(SyntaxFactory.ThisExpression()),
                            SyntaxFactory.Argument(rootParam)))));

                if (this.applyTo.Ancestors.Any())
                {
                    method = Syntax.AddNewKeyword(method);
                }

                return method;
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
                        this.CreateNewSpineMethod(),
                        this.CreateEqualsObjectMethod(),
                        this.CreateGetHashCodeMethod(),
                        this.CreateEqualsRootedStructMethod(),
                        this.CreateThrowIfDefaultMethod())
                    .AddMembers(this.CreateFields())
                    .AddMembers(this.CreateRootedOperators())
                    .AddMembers(this.CreateIsDerivedProperties())
                    .AddMembers(this.CreateAsDerivedProperties())
                    .AddMembers(this.CreateAsAncestorProperties())
                    .AddMembers(this.CreateFieldAccessorProperties())
                    .AddMembers(this.CreateToTypeMethods())
                    .AddMembers(this.CreateCollectionHelperMethods());

                if (this.rootedRecursiveParent != null)
                {
                    rootedStruct = rootedStruct.AddMembers(this.CreateParentProperty());
                }

                if (this.applyTo.IsRecursive)
                {
                    if (!this.applyTo.TypeSymbol.IsAbstract)
                    {
                        rootedStruct = rootedStruct
                            .AddMembers(
                                this.CreateCreateMethod());
                    }
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

                if (this.generator.options.DefineWithMethodsPerProperty)
                {
                    rootedStruct = rootedStruct.AddMembers(this.CreateWithPropertyMethods());
                }

                if (this.generator.options.Delta)
                {
                    rootedStruct = rootedStruct.AddMembers(this.CreateChangesSinceMethod());
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

                if (this.applyTo.IsRecursiveParentOrDerivative)
                {
                    ctor = ctor.AddBodyStatements(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        Syntax.ThisDot(this.applyTo.RecursiveParent.RecursiveField.NameAsField),
                        SyntaxFactory.DefaultExpression(Syntax.OptionalOf(this.GetRootedCollectionTypeAdapterName())))));
                }

                return ctor;
            }

            protected MemberDeclarationSyntax[] CreateFields()
            {
                var fields = new List<MemberDeclarationSyntax>();

                if (this.applyTo.IsRecursiveParentOrDerivative)
                {
                    var rParam = SyntaxFactory.IdentifierName("r");
                    var uParam = SyntaxFactory.IdentifierName("u");
                    fields.AddRange(new MemberDeclarationSyntax[] {
                        // private static readonly System.Func<RootedRecursiveType, TRecursiveType> toUnrooted = r => r.TemplateType;
                        SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(Syntax.FuncOf(this.rootedRecursiveType, this.applyTo.RecursiveTypeFromFamily.TypeSyntax))
                            .AddVariables(SyntaxFactory.VariableDeclarator(ToUnrootedFieldName.Identifier)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.SimpleLambdaExpression(
                                        SyntaxFactory.Parameter(rParam.Identifier),
                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, rParam, SyntaxFactory.IdentifierName(this.applyTo.RecursiveTypeFromFamily.TypeSymbol.Name)))))))
                            .AddModifiers(
                                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
                        // private static readonly System.Func<TRecursiveType, TRecursiveParent, RootedRecursiveType> toRooted = (u, r) => new RootedRecursiveType(u, r);
                        SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(Syntax.FuncOf(this.applyTo.RecursiveTypeFromFamily.TypeSyntax, this.applyTo.RecursiveParent.TypeSyntax, this.rootedRecursiveType))
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
                                SyntaxFactory.VariableDeclarator(this.applyTo.RecursiveParent.RecursiveField.NameAsField.Identifier)))
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

                // public RootedRecursiveParent Parent
                return SyntaxFactory.PropertyDeclaration(GetRootedTypeSyntax(this.applyTo.RecursiveParent), ParentPropertyName.Identifier)
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
                // public RootedRecursiveParent Root { get; }
                return SyntaxFactory.PropertyDeclaration(GetRootedTypeSyntax(this.applyTo.RecursiveParent), RootPropertyName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(
                            // return this.root != null ? this.root.AsRoot : default(RootedRecursiveParent);
                            SyntaxFactory.ReturnStatement(SyntaxFactory.ConditionalExpression(
                                SyntaxFactory.BinaryExpression(
                                    SyntaxKind.NotEqualsExpression,
                                    Syntax.ThisDot(RootFieldName),
                                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, Syntax.ThisDot(RootFieldName), AsRootPropertyName),
                                SyntaxFactory.DefaultExpression(GetRootedTypeSyntax(this.applyTo.RecursiveParent)))))));
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
                // public bool IsRoot => this.root == this.greenNode;
                return SyntaxFactory.PropertyDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), IsRootPropertyName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(SyntaxFactory.BinaryExpression(
                        SyntaxKind.EqualsExpression,
                        Syntax.ThisDot(RootFieldName),
                        Syntax.ThisDot(GreenNodeFieldName))))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }

            protected PropertyDeclarationSyntax CreateIsDefaultProperty()
            {
                // public bool IsDefault => this.greenNode == null;
                return SyntaxFactory.PropertyDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), IsDefaultPropertyName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.EqualsExpression,
                            Syntax.ThisDot(GreenNodeFieldName),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }

            protected PropertyDeclarationSyntax CreateTemplateTypeProperty()
            {
                return SyntaxFactory.PropertyDeclaration(this.applyTo.TypeSyntax, this.applyTo.TypeSymbol.Name)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                            // => this.greenNode;
                            Syntax.ThisDot(GreenNodeFieldName)))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }

            protected MethodDeclarationSyntax CreateWithMethod()
            {
                var newGreenNodeVar = SyntaxFactory.IdentifierName("newGreenNode");
                var newRootVar = SyntaxFactory.IdentifierName("newRoot");

                return SyntaxFactory.MethodDeclaration(this.typeName, WithMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithParameterList(this.generator.CreateParameterList(this.applyTo.AllFields, ParameterStyle.Optional))
                    .WithBody(SyntaxFactory.Block(
                        // this.ThrowIfDefault();
                        CallThrowIfDefaultMethod,
                        // var newGreenNode = this.greenNode.With(<# WriteArguments(redType.AllFields, ArgSource.Argument); #>);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(newGreenNodeVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, Syntax.ThisDot(GreenNodeFieldName), WithMethodName))
                                    .WithArgumentList(this.generator.CreateArgumentList(this.applyTo.AllFields, ArgSource.Argument)))))),
                        // return this.NewSpine(newGreenNode);
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(Syntax.ThisDot(NewSpineMethodName)).AddArgumentListArguments(
                            SyntaxFactory.Argument(newGreenNodeVar)))));
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
                var greenNodeVar = SyntaxFactory.IdentifierName("greenNode");

                return SyntaxFactory.MethodDeclaration(this.typeName, CreateMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                    .WithParameterList(this.generator.CreateParameterList(this.applyTo.AllFields, ParameterStyle.OptionalOrRequired))
                    .WithBody(SyntaxFactory.Block(
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(greenNodeVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, this.applyTo.TypeSyntax, CreateMethodName))
                                    .WithArgumentList(this.generator.CreateArgumentList(this.applyTo.AllFields, ArgSource.Argument)))))),
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, greenNodeVar, AsRootPropertyName))));
            }

            protected MethodDeclarationSyntax CreateFindMethod()
            {
                // public RootedRecursiveType Find(uint identity)
                return SyntaxFactory.MethodDeclaration(GetRootedTypeSyntax(this.applyTo.RecursiveTypeFromFamily), SyntaxFactory.Identifier(nameof(RecursiveTypeExtensions.Find)))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(IdentityParameterName.Identifier).WithType(IdentityFieldTypeSyntax))
                    .WithBody(SyntaxFactory.Block(
                        CallThrowIfDefaultMethod,
                        SyntaxFactory.ReturnStatement(SyntaxFactory.ObjectCreationExpression(GetRootedTypeSyntax(this.applyTo.RecursiveTypeFromFamily)).AddArgumentListArguments(
                            SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, Syntax.ThisDot(GreenNodeFieldName), SyntaxFactory.IdentifierName(nameof(RecursiveTypeExtensions.Find))))
                                .AddArgumentListArguments(SyntaxFactory.Argument(IdentityParameterName))),
                            SyntaxFactory.Argument(Syntax.ThisDot(RootFieldName))))));
            }

            protected MethodDeclarationSyntax CreateTryFindMethod()
            {
                var greenValueVar = SyntaxFactory.IdentifierName("greenValue");

                // public bool TryFind(uint identity, out RootedRecursiveType value)
                var valueParameter = SyntaxFactory.IdentifierName("value");
                return SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), SyntaxFactory.Identifier(nameof(RecursiveTypeExtensions.TryFind)))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(IdentityParameterName.Identifier).WithType(IdentityFieldTypeSyntax),
                        SyntaxFactory.Parameter(valueParameter.Identifier).WithType(GetRootedTypeSyntax(this.applyTo.RecursiveTypeFromFamily)).AddModifiers(SyntaxFactory.Token(SyntaxKind.OutKeyword)))
                    .WithBody(SyntaxFactory.Block(
                        CallThrowIfDefaultMethod,
                        // TRecursiveType greenValue;
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(this.applyTo.RecursiveTypeFromFamily.TypeSyntax).AddVariables(
                            SyntaxFactory.VariableDeclarator(greenValueVar.Identifier))),
                        // if (this.greenNode.TryFind(identity, out greenValue)) {
                        SyntaxFactory.IfStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, Syntax.ThisDot(GreenNodeFieldName), TryFindMethodName))
                                .AddArgumentListArguments(
                                    SyntaxFactory.Argument(IdentityParameterName),
                                    SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.OutKeyword), greenValueVar)),
                            SyntaxFactory.Block(
                                // value = new <#= redType.RecursiveType.TypeName #>(greenValue, this.root);
                                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    valueParameter,
                                    SyntaxFactory.ObjectCreationExpression(GetRootedTypeSyntax(this.applyTo.RecursiveTypeFromFamily)).AddArgumentListArguments(
                                        SyntaxFactory.Argument(greenValueVar),
                                        SyntaxFactory.Argument(Syntax.ThisDot(RootFieldName))))),
                                // return true;
                                SyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)))),
                        // value = default(<#= redType.RecursiveType.TypeName #>);
                        SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            valueParameter,
                            SyntaxFactory.DefaultExpression(GetRootedTypeSyntax(this.applyTo.RecursiveTypeFromFamily)))),
                        // return false;
                        SyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))));
            }

            protected MethodDeclarationSyntax CreateGetEnumeratorMethod()
            {
                // public IEnumerator<TRootedRecursiveType> GetEnumerator()
                return SyntaxFactory.MethodDeclaration(Syntax.IEnumeratorOf(GetRootedTypeSyntax(this.applyTo.RecursiveTypeFromFamily)), nameof(IEnumerable<int>.GetEnumerator))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBody(SyntaxFactory.Block(
                        // return this.Children.GetEnumerator();
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    Syntax.ThisDot(this.applyTo.RecursiveParent.RecursiveField.NameAsProperty),
                                    SyntaxFactory.IdentifierName(nameof(IEnumerable<int>.GetEnumerator))),
                                SyntaxFactory.ArgumentList()))));
            }

            protected MethodDeclarationSyntax CreateParentedNodeMethod()
            {
                var returnType = Syntax.GetTypeSyntax(typeof(ParentedRecursiveTypeNonGeneric));
                var resultVar = SyntaxFactory.IdentifierName("result");

                // ParentedRecursiveType<IRecursiveParent<IRecursiveType>, IRecursiveType> IRecursiveParent.GetParentedNode(uint identity)
                return SyntaxFactory.MethodDeclaration(
                    returnType,
                    nameof(IRecursiveParent.GetParentedNode))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(Syntax.GetTypeSyntax(typeof(IRecursiveParent))))
                    .AddParameterListParameters(RequiredIdentityParameter)
                    .WithBody(SyntaxFactory.Block(
                        // this.ThrowIfDefault();
                        CallThrowIfDefaultMethod,
                        // var result = this.greenNode.GetParentedNode(identity);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(resultVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, Syntax.ThisDot(GreenNodeFieldName), SyntaxFactory.IdentifierName(nameof(IRecursiveParent.GetParentedNode))))
                                        .AddArgumentListArguments(SyntaxFactory.Argument(IdentityParameterName)))))),
                        // return new ParentedRecursiveType<IRecursiveParent<IRecursiveType>, IRecursiveType>(result.Value, result.Parent);
                        SyntaxFactory.ReturnStatement(SyntaxFactory.ObjectCreationExpression(returnType).AddArgumentListArguments(
                            SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, resultVar, SyntaxFactory.IdentifierName(nameof(ParentedRecursiveTypeNonGeneric.Value)))),
                            SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, resultVar, SyntaxFactory.IdentifierName(nameof(ParentedRecursiveTypeNonGeneric.Parent))))))));
            }

            protected PropertyDeclarationSyntax[] CreateChildrenProperties()
            {
                return new PropertyDeclarationSyntax[] {
                    // IEnumerable<IRecursiveType> IRecursiveParent.Children { get; }
                    SyntaxFactory.PropertyDeclaration(Syntax.IEnumerableOf(Syntax.GetTypeSyntax(typeof(IRecursiveType))), nameof(IRecursiveParent.Children))
                        .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(Syntax.GetTypeSyntax(typeof(IRecursiveParent))))
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(
                                // return System.Linq.Enumerable.Cast<IRecursiveType>(this.Children);
                                SyntaxFactory.ReturnStatement(
                                    Syntax.EnumerableExtension(
                                        SyntaxFactory.GenericName(nameof(Enumerable.Cast))
                                            .AddTypeArgumentListArguments(Syntax.GetTypeSyntax(typeof(IRecursiveType))),
                                        Syntax.ThisDot(this.applyTo.RecursiveParent.RecursiveField.NameAsProperty),
                                        SyntaxFactory.ArgumentList()))))),
                    // IEnumerable<TRootedRecursiveType> IRecursiveParent<TRootedRecursiveType>.Children { get; }
                    SyntaxFactory.PropertyDeclaration(Syntax.IEnumerableOf(this.rootedRecursiveType), nameof(IRecursiveParent<IRecursiveType>.Children))
                        .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(CreateIRecursiveParentOfTSyntax(this.rootedRecursiveType)))
                        // => this.Children;
                        .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(Syntax.ThisDot(this.applyTo.RecursiveParent.RecursiveField.NameAsProperty)))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                };
            }

            protected PropertyDeclarationSyntax[] CreateIsDerivedProperties()
            {
                // public bool IsDerivedType => this.greenNode is TDerivedType;
                return this.applyTo.Descendents.Select(descendent =>
                    SyntaxFactory.PropertyDeclaration(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                        string.Format(CultureInfo.InvariantCulture, IsDerivedPropertyNameFormat, descendent.TypeSymbol.Name))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                            SyntaxFactory.BinaryExpression(
                                SyntaxKind.IsExpression,
                                Syntax.ThisDot(GreenNodeFieldName),
                                descendent.TypeSyntax)))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
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
                return this.applyTo.Ancestors.Select(ancestor =>
                    SyntaxFactory.PropertyDeclaration(
                        GetRootedTypeSyntax(ancestor),
                        string.Format(CultureInfo.InvariantCulture, AsAncestorPropertyNameFormat, ancestor.TypeSymbol.Name))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(
                                // return this.greenNode != null ? new RootedAncestor(this.greenNode, this.root) : default(RootedAncestor);
                                SyntaxFactory.ReturnStatement(SyntaxFactory.ConditionalExpression(
                                    SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, Syntax.ThisDot(GreenNodeFieldName), SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                    SyntaxFactory.ObjectCreationExpression(GetRootedTypeSyntax(ancestor)).AddArgumentListArguments(
                                        SyntaxFactory.Argument(Syntax.ThisDot(GreenNodeFieldName)),
                                        SyntaxFactory.Argument(Syntax.ThisDot(RootFieldName))),
                                    SyntaxFactory.DefaultExpression(GetRootedTypeSyntax(ancestor)))))))
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

            protected MethodDeclarationSyntax CreateNewSpineMethod()
            {
                var leafParam = SyntaxFactory.IdentifierName("leaf");
                var newRootVar = SyntaxFactory.IdentifierName("newRoot");

                // private RootedFileSystemFile NewSpine(FileSystemFile leaf)
                return SyntaxFactory.MethodDeclaration(GetRootedTypeSyntax(this.applyTo), NewSpineMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                    .AddParameterListParameters(SyntaxFactory.Parameter(leafParam.Identifier).WithType(this.applyTo.TypeSyntax))
                    .WithBody(SyntaxFactory.Block(
                        // var newRoot = this.root.ReplaceDescendent(this.greenNode, leaf);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(newRootVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, Syntax.ThisDot(RootFieldName), SyntaxFactory.IdentifierName(nameof(RecursiveTypeExtensions.ReplaceDescendent))))
                                    .AddArgumentListArguments(
                                        SyntaxFactory.Argument(GreenNodeFieldName),
                                        SyntaxFactory.Argument(leafParam)))))),
                        // return new RootedTemplateType(newGreenNode, newRoot);
                        SyntaxFactory.ReturnStatement(SyntaxFactory.ObjectCreationExpression(GetRootedTypeSyntax(this.applyTo)).AddArgumentListArguments(
                            SyntaxFactory.Argument(leafParam),
                            SyntaxFactory.Argument(newRootVar)))
                    ));
            }

            protected MemberDeclarationSyntax[] CreateWithPropertyMethods()
            {
                var valueParam = SyntaxFactory.IdentifierName("value");
                var mutatedLeaf = SyntaxFactory.IdentifierName("mutatedLeaf");

                return this.applyTo.AllFields.Select(field =>
                    SyntaxFactory.MethodDeclaration(GetRootedTypeSyntax(this.applyTo), DefineWithMethodsPerPropertyGen.WithPropertyMethodPrefix + field.NameAsProperty)
                    .AddParameterListParameters(SyntaxFactory.Parameter(valueParam.Identifier).WithType(field.TypeSyntax))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBody(SyntaxFactory.Block(
                        CallThrowIfDefaultMethod,
                        // var mutatedLeaf = this.greenNode.With<#= field.NamePascalCase #>(value);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(mutatedLeaf.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        Syntax.ThisDot(GreenNodeFieldName),
                                        SyntaxFactory.IdentifierName(DefineWithMethodsPerPropertyGen.WithPropertyMethodPrefix + field.NameAsProperty)))
                                    .AddArgumentListArguments(SyntaxFactory.Argument(valueParam)))))),
                        // return this.NewSpine(mutatedLeaf);
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(Syntax.ThisDot(NewSpineMethodName))
                                .AddArgumentListArguments(SyntaxFactory.Argument(mutatedLeaf)))))).ToArray();
            }

            protected MethodDeclarationSyntax[] CreateToTypeMethods()
            {
                var targetTypes = from type in this.applyTo.GetTypeFamily()
                                  where !type.TypeSymbol.IsAbstract && !type.Equals(this.applyTo)
                                  select new { type, CommonAncestor = type.GetFirstCommonAncestor(this.applyTo) };

                var newGreenNodeVar = SyntaxFactory.IdentifierName("newGreenNode");
                var newRootVar = SyntaxFactory.IdentifierName("newRoot");

                return targetTypes.Select(targetType =>
                    SyntaxFactory.MethodDeclaration(
                        GetRootedTypeSyntax(targetType.type),
                        TypeConversionGen.GetToTypeMethodName(targetType.type.TypeSymbol.Name).Identifier)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .WithParameterList(this.generator.CreateParameterList(targetType.type.GetFieldsBeyond(targetType.CommonAncestor), ParameterStyle.OptionalOrRequired))
                        .WithBody(SyntaxFactory.Block(
                            // var newGreenNode = this.greenNode.To<#= targetType.TypeName #>(<# WriteArguments(familyType.GetFieldsBeyond(commonAncestor), ArgSource.Argument); #>);
                            SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                                SyntaxFactory.VariableDeclarator(newGreenNodeVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            Syntax.ThisDot(GreenNodeFieldName),
                                            TypeConversionGen.GetToTypeMethodName(targetType.type.TypeSymbol.Name)))
                                        .WithArgumentList(this.generator.CreateArgumentList(targetType.type.GetFieldsBeyond(targetType.CommonAncestor), ArgSource.Argument)))))),
                            // var newRoot = this.root.ReplaceDescendent(this.greenNode, newGreenNode);
                            SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                                SyntaxFactory.VariableDeclarator(newRootVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            Syntax.ThisDot(RootFieldName),
                                            DeepMutationGen.ReplaceDescendentMethodName))
                                        .AddArgumentListArguments(
                                            SyntaxFactory.Argument(Syntax.ThisDot(GreenNodeFieldName)),
                                            SyntaxFactory.Argument(newGreenNodeVar)))))),
                            // return new <#= familyType.TypeName #>(newGreenNode, newRoot);
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.ObjectCreationExpression(GetRootedTypeSyntax(targetType.type)).AddArgumentListArguments(
                                    SyntaxFactory.Argument(newGreenNodeVar),
                                    SyntaxFactory.Argument(newRootVar)))))).ToArray();
            }

            protected MethodDeclarationSyntax CreateDictionaryHelperMethod(MetaField field, IdentifierNameSyntax methodName, bool includeValue)
            {
                var methodParameters = SyntaxFactory.ParameterList().AddParameters(
                    SyntaxFactory.Parameter(CollectionHelpersGen.KeyParameterName.Identifier).WithType(GetFullyQualifiedSymbolName(field.ElementKeyType)));
                var collectionMethodArguments = SyntaxFactory.ArgumentList().AddArguments(
                    SyntaxFactory.Argument(CollectionHelpersGen.KeyParameterName));
                if (includeValue)
                {
                    methodParameters = methodParameters.AddParameters(
                        SyntaxFactory.Parameter(CollectionHelpersGen.ValueParameterName.Identifier).WithType(GetFullyQualifiedSymbolName(field.ElementValueType)));
                    collectionMethodArguments = collectionMethodArguments.AddArguments(
                        SyntaxFactory.Argument(CollectionHelpersGen.ValueParameterName));
                }

                // this.greenNode.MethodName(key, value)
                ExpressionSyntax modifiedGreenNode = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        Syntax.ThisDot(GreenNodeFieldName),
                        methodName),
                    collectionMethodArguments);

                var method = SyntaxFactory.MethodDeclaration(
                    GetRootedTypeSyntax(this.applyTo),
                    methodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithParameterList(methodParameters)
                    .WithBody(SyntaxFactory.Block(
                        CallThrowIfDefaultMethod,
                        // return this.NewSpine(<modifiedGreenNode>);
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                Syntax.ThisDot(NewSpineMethodName),
                                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        modifiedGreenNode)))))));

                return method;
            }

            protected MethodDeclarationSyntax CreateCollectionHelperMethodStarter(MetaField field, bool isPlural, string verb)
            {
                var distinguisher = field.Distinguisher;
                string suffix = distinguisher?.CollectionModifierMethodSuffix;
                string plural = suffix != null ? (this.generator.PluralService.Singularize(field.Name.ToPascalCase()) + this.generator.PluralService.Pluralize(suffix)) : field.Name.ToPascalCase();
                string singular = this.generator.PluralService.Singularize(field.Name.ToPascalCase()) + suffix;
                string term = isPlural ? plural : singular;
                var mutatedLeafVar = SyntaxFactory.IdentifierName("mutatedLeaf");
                var parameterName = isPlural ? CollectionHelpersGen.ValuesParameterName : CollectionHelpersGen.ValueParameterName;

                var method = SyntaxFactory.MethodDeclaration(
                    GetRootedTypeSyntax(this.applyTo),
                    SyntaxFactory.Identifier(verb + term))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBody(SyntaxFactory.Block(
                        CallThrowIfDefaultMethod,
                        // var mutatedLeaf = this.greenNode.Verb<#= plural #>(values);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(mutatedLeafVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        Syntax.ThisDot(GreenNodeFieldName),
                                        SyntaxFactory.IdentifierName(verb + term))).AddArgumentListArguments(
                                            SyntaxFactory.Argument(parameterName)))))),
                        // return this.NewSpine(mutatedLeaf);
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(Syntax.ThisDot(NewSpineMethodName)).AddArgumentListArguments(
                                SyntaxFactory.Argument(mutatedLeafVar)))));

                method = isPlural
                    ? method.WithParameterList(CollectionHelpersGen.CreateParamsElementArrayParameters(field))
                    : method.AddParameterListParameters(SyntaxFactory.Parameter(parameterName.Identifier).WithType(field.ElementTypeSyntax));

                return method;
            }

            protected MemberDeclarationSyntax[] CreateCollectionHelperMethods()
            {
                if (!this.generator.options.DefineWithMethodsPerProperty)
                {
                    return Array.Empty<MemberDeclarationSyntax>();
                }

                var valueParam = CollectionHelpersGen.ValueParameterName;
                var valuesParam = CollectionHelpersGen.ValuesParameterName;
                var mutatedLeafVar = SyntaxFactory.IdentifierName("mutatedLeaf");
                var methods = new List<MemberDeclarationSyntax>();

                // Compare to the CollectionHelpersGen.GenerateCore method.
                foreach (var field in this.applyTo.AllFields)
                {
                    var distinguisher = field.Distinguisher;
                    string suffix = distinguisher != null ? distinguisher.CollectionModifierMethodSuffix : null;
                    string plural = suffix != null ? (this.generator.PluralService.Singularize(field.Name.ToPascalCase()) + this.generator.PluralService.Pluralize(suffix)) : field.Name.ToPascalCase();
                    string singular = this.generator.PluralService.Singularize(field.Name.ToPascalCase()) + suffix;

                    if (field.IsCollection)
                    {
                        // With[Plural] methods
                        var paramsArrayMethod = this.CreateCollectionHelperMethodStarter(field, true, "With");
                        methods.Add(paramsArrayMethod);
                        methods.Add(CollectionHelpersGen.CreateIEnumerableFromParamsArrayMethod(field, paramsArrayMethod));

                        // Add[Plural] methods
                        paramsArrayMethod = this.CreateCollectionHelperMethodStarter(field, true, "Add");
                        methods.Add(paramsArrayMethod);
                        methods.Add(CollectionHelpersGen.CreateIEnumerableFromParamsArrayMethod(field, paramsArrayMethod));

                        // Add[Singular] method
                        if (field.IsRecursiveCollection)
                        {
                            var newParentVar = SyntaxFactory.IdentifierName("newParent");
                            var newChildVar = SyntaxFactory.IdentifierName("newChild");
                            var returnType = SyntaxFactory.GenericName(nameof(ParentedRecursiveType<IRecursiveParent<IRecursiveType>, IRecursiveType>)).AddTypeArgumentListArguments(
                                GetRootedTypeSyntax(this.applyTo),
                                GetRootedTypeSyntax(this.applyTo.RecursiveTypeFromFamily));

                            // public ParentedRecursiveType<RootedTemplateType, RootedRecursiveType> AddChild(FileSystemEntry value)
                            methods.Add(SyntaxFactory.MethodDeclaration(
                                returnType,
                                SyntaxFactory.Identifier("Add" + singular))
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                .AddParameterListParameters(SyntaxFactory.Parameter(valueParam.Identifier).WithType(this.applyTo.RecursiveTypeFromFamily.TypeSyntax))
                                .WithBody(SyntaxFactory.Block(
                                    CallThrowIfDefaultMethod,
                                    // var mutatedLeaf = this.greenNode.AddChild(value);
                                    SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                                        SyntaxFactory.VariableDeclarator(mutatedLeafVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, Syntax.ThisDot(GreenNodeFieldName), SyntaxFactory.IdentifierName("Add" + singular)))
                                                .AddArgumentListArguments(SyntaxFactory.Argument(valueParam)))))),
                                     // var newParent = this.NewSpine(mutatedLeaf);
                                     SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                                         SyntaxFactory.VariableDeclarator(newParentVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                             SyntaxFactory.InvocationExpression(Syntax.ThisDot(NewSpineMethodName)).AddArgumentListArguments(
                                                 SyntaxFactory.Argument(mutatedLeafVar)))))),
                                     // var newChild = new RootedRecursiveType(value, newParent.root);
                                     SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                                         SyntaxFactory.VariableDeclarator(newChildVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                             SyntaxFactory.ObjectCreationExpression(GetRootedTypeSyntax(this.applyTo.RecursiveTypeFromFamily)).AddArgumentListArguments(
                                                 SyntaxFactory.Argument(valueParam),
                                                 SyntaxFactory.Argument(
                                                     SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        newParentVar,
                                                        RootFieldName))))))),
                                     // return new ParentedRecursiveType<RootedTemplateType, RootedRecursiveType>(newChild, newParent);
                                     SyntaxFactory.ReturnStatement(
                                         SyntaxFactory.ObjectCreationExpression(returnType).AddArgumentListArguments(
                                             SyntaxFactory.Argument(newChildVar),
                                             SyntaxFactory.Argument(newParentVar))))));
                        }
                        else
                        {
                            // public RootedTemplateType AddChild(<#= field.ElementTypeName #> value)
                            methods.Add(this.CreateCollectionHelperMethodStarter(field, false, "Add"));
                        }

                        // Remove[Plural] methods
                        paramsArrayMethod = this.CreateCollectionHelperMethodStarter(field, true, "Remove");
                        methods.Add(paramsArrayMethod);
                        methods.Add(CollectionHelpersGen.CreateIEnumerableFromParamsArrayMethod(field, paramsArrayMethod));

                        methods.Add(SyntaxFactory.MethodDeclaration(
                            GetRootedTypeSyntax(this.applyTo),
                            SyntaxFactory.Identifier("Remove" + plural))
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                            .WithParameterList(SyntaxFactory.ParameterList())
                            .WithBody(SyntaxFactory.Block(
                                CallThrowIfDefaultMethod,
                                // var mutatedLeaf = this.greenNode.RemoveAttributes();
                                SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                                    SyntaxFactory.VariableDeclarator(mutatedLeafVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                Syntax.ThisDot(GreenNodeFieldName),
                                                SyntaxFactory.IdentifierName("Remove" + plural)),
                                            SyntaxFactory.ArgumentList()))))),
                                // return this.NewSpine(mutatedLeaf);
                                SyntaxFactory.ReturnStatement(
                                    SyntaxFactory.InvocationExpression(Syntax.ThisDot(NewSpineMethodName)).AddArgumentListArguments(
                                        SyntaxFactory.Argument(mutatedLeafVar))))));

                        // Remove[Singular] method
                        methods.Add(this.CreateCollectionHelperMethodStarter(field, false, "Remove"));
                    }
                    else if (field.IsDictionary)
                    {
                        // public RootedTemplateType Add[Singular](TKey key, TValue value)
                        methods.Add(this.CreateDictionaryHelperMethod(
                            field,
                            SyntaxFactory.IdentifierName("Add" + singular),
                            true));

                        // public RootedTemplateType Remove[Singular](TKey key)
                        methods.Add(this.CreateDictionaryHelperMethod(
                            field,
                            SyntaxFactory.IdentifierName("Remove" + singular),
                            false));

                        // public RootedTemplateType Set[Singular](TKey key, TValue value)
                        methods.Add(this.CreateDictionaryHelperMethod(
                            field,
                            SyntaxFactory.IdentifierName("Set" + singular),
                            true));
                    }
                }

                return methods.ToArray();
            }

            protected MethodDeclarationSyntax CreateChangesSinceMethod()
            {
                var priorVersionParam = SyntaxFactory.IdentifierName("priorVersion");
                var diffTypeName = this.generator.mergedFeatures.OfType<DeltaGen>().Single().diffGramTypeSyntax;

                // public IReadOnlyList<<#= diffTypeName #>> ChangesSince(RootedTemplateType priorVersion) {
                return SyntaxFactory.MethodDeclaration(
                    Syntax.IReadOnlyListOf(diffTypeName),
                    ChangesSinceMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(priorVersionParam.Identifier).WithType(GetRootedTypeSyntax(this.applyTo)))
                    .WithBody(SyntaxFactory.Block(
                        CallThrowIfDefaultMethod,
                        // return this.greenNode.ChangesSince(priorVersion.<#= templateType.GreenType.TypeName #>);
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    Syntax.ThisDot(GreenNodeFieldName),
                                    SyntaxFactory.IdentifierName(nameof(RecursiveTypeExtensions.ChangesSince)))).AddArgumentListArguments(
                                SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    priorVersionParam,
                                    SyntaxFactory.IdentifierName(this.applyTo.TypeSymbol.Name)))))));
            }
        }
    }
}
