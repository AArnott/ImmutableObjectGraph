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
            private readonly IdentifierNameSyntax name;

            public RootedStructGen(CodeGen generator)
                : base(generator)
            {
                this.name = SyntaxFactory.IdentifierName("Rooted" + this.applyTo.TypeSymbol.Name);
            }

            public override bool IsApplicable
            {
                get { return this.generator.options.DefineRootedStruct; }
            }

            protected override void GenerateCore()
            {
                this.siblingMembers.Add(this.CreateRootedStruct());
            }

            protected StructDeclarationSyntax CreateRootedStruct()
            {
                var rootedStruct = SyntaxFactory.StructDeclaration(this.name.Identifier)
                    .AddModifiers(GetModifiersForAccessibility(this.applyTo.TypeSymbol))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                    .AddMembers(
                        this.CreateRootedConstructor(),
                        this.CreateParentProperty(),
                        this.CreateRootProperty(),
                        this.CreateIdentityProperty(),
                        this.CreateIsRootProperty(),
                        this.CreateIsDefaultProperty(),
                        this.CreateTemplateTypeProperty(),
                        this.CreateWithMethod(),
                        this.CreateEqualsObjectMethod(),
                        this.CreateGetHashCodeMethod(),
                        this.CreateEqualsRootedStructMethod())
                    .AddMembers(this.CreateRootedOperators())
                    .AddMembers(this.CreateIsDerivedProperties())
                    .AddMembers(this.CreateAsDerivedProperties())
                    .AddMembers(this.CreateAsAncestorProperties())
                    .AddMembers(this.CreateFieldAccessorProperties());

                if (this.applyTo.IsRecursive)
                {
                    if (!this.applyTo.TypeSymbol.IsAbstract)
                    {
                        rootedStruct = rootedStruct
                            .AddMembers(
                                this.CreateCreateMethod());
                    }

                    rootedStruct = rootedStruct.AddMembers(
                        this.CreateFindMethod(),
                        this.CreateTryFindMethod(),
                        this.CreateGetEnumeratorMethod());
                }

                return rootedStruct;
            }

            protected ConstructorDeclarationSyntax CreateRootedConstructor()
            {
                // internal RootedTemplateType(TemplateType templateType, TRecursiveParent root) {
                var templateTypeParam = SyntaxFactory.IdentifierName(this.applyTo.TypeSymbol.Name.ToCamelCase());
                var rootParam = SyntaxFactory.IdentifierName("root");
                var ctor = SyntaxFactory.ConstructorDeclaration(this.name.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(templateTypeParam.Identifier).WithType(this.applyTo.TypeSyntax),
                        SyntaxFactory.Parameter(rootParam.Identifier).WithType(this.applyTo.RecursiveParent.TypeSyntax))
                    .WithBody(SyntaxFactory.Block(ThrowNotImplementedException));

                return ctor;
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
                        .AddParameterListParameters(SyntaxFactory.Parameter(rootedParameter.Identifier).WithType(this.name))
                        .WithBody(SyntaxFactory.Block(ThrowNotImplementedException)),
                    // public static bool operator ==(RootedTemplateType that, RootedTemplateType other)
                    SyntaxFactory.OperatorDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), SyntaxFactory.Token(SyntaxKind.EqualsEqualsToken))
                        .AddModifiers(publicStaticModifiers)
                        .AddParameterListParameters(
                            SyntaxFactory.Parameter(thatParameter.Identifier).WithType(this.name),
                            SyntaxFactory.Parameter(otherParameter.Identifier).WithType(this.name))
                        .WithBody(SyntaxFactory.Block(ThrowNotImplementedException)),
                    // public static bool operator !=(RootedTemplateType that, RootedTemplateType other)
                    SyntaxFactory.OperatorDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), SyntaxFactory.Token(SyntaxKind.ExclamationEqualsToken))
                        .AddModifiers(publicStaticModifiers)
                        .AddParameterListParameters(
                            SyntaxFactory.Parameter(thatParameter.Identifier).WithType(this.name),
                            SyntaxFactory.Parameter(otherParameter.Identifier).WithType(this.name))
                        .WithBody(SyntaxFactory.Block(ThrowNotImplementedException)),
                };
            }

            protected PropertyDeclarationSyntax CreateParentProperty()
            {
                return SyntaxFactory.PropertyDeclaration(this.applyTo.RecursiveParent.TypeSyntax, ParentPropertyName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(ThrowNotImplementedException)));
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
                        SyntaxFactory.Block(ThrowNotImplementedException)));
            }

            protected PropertyDeclarationSyntax CreateIsRootProperty()
            {
                return SyntaxFactory.PropertyDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), IsRootPropertyName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(ThrowNotImplementedException)));
            }

            protected PropertyDeclarationSyntax CreateIsDefaultProperty()
            {
                return SyntaxFactory.PropertyDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), IsDefaultPropertyName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(ThrowNotImplementedException)));
            }

            protected PropertyDeclarationSyntax CreateTemplateTypeProperty()
            {
                return SyntaxFactory.PropertyDeclaration(this.applyTo.TypeSyntax, this.applyTo.TypeSymbol.Name)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(ThrowNotImplementedException)));
            }

            protected MethodDeclarationSyntax CreateWithMethod()
            {
                return SyntaxFactory.MethodDeclaration(this.name, WithMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithParameterList(this.generator.CreateParameterList(this.applyTo.AllFields, ParameterStyle.Optional))
                    .WithBody(SyntaxFactory.Block(ThrowNotImplementedException));
            }

            protected MethodDeclarationSyntax CreateEqualsObjectMethod()
            {
                // public override bool Equals(object obj)
                var objParam = SyntaxFactory.IdentifierName("obj");
                return SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), nameof(object.Equals))
                   .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
                   .AddParameterListParameters(SyntaxFactory.Parameter(objParam.Identifier).WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))))
                   .WithBody(SyntaxFactory.Block(ThrowNotImplementedException));
            }

            protected MethodDeclarationSyntax CreateGetHashCodeMethod()
            {
                // public override int GetHashCode()
                return SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)), nameof(object.GetHashCode))
                   .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
                   .WithBody(SyntaxFactory.Block(ThrowNotImplementedException));
            }

            protected MethodDeclarationSyntax CreateEqualsRootedStructMethod()
            {
                // public bool Equals(RootedTemplateType other)
                var otherParam = SyntaxFactory.IdentifierName("other");
                return SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), nameof(IEquatable<object>.Equals))
                   .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                   .AddParameterListParameters(SyntaxFactory.Parameter(otherParam.Identifier).WithType(this.name))
                   .WithBody(SyntaxFactory.Block(ThrowNotImplementedException));
            }

            protected MethodDeclarationSyntax CreateCreateMethod()
            {
                return SyntaxFactory.MethodDeclaration(this.name, CreateMethodName.Identifier)
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
                return this.applyTo.Descendents.Select(descendent =>
                    SyntaxFactory.PropertyDeclaration(
                        descendent.TypeSyntax,
                        string.Format(CultureInfo.InvariantCulture, AsDerivedPropertyNameFormat, descendent.TypeSymbol.Name))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(ThrowNotImplementedException)))
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
                        field.TypeSyntax,
                        field.NameAsProperty.Identifier)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(ThrowNotImplementedException)))
                    ).ToArray();
            }
        }
    }
}
