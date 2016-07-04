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
    using RecursiveDiffingTypeHelper = IRecursiveDiffingType<object, object>;

    public partial class CodeGen
    {
        protected class DeltaGen : FeatureGenerator
        {
            private static readonly IdentifierNameSyntax EnumValueNone = SyntaxFactory.IdentifierName("None");
            private static readonly IdentifierNameSyntax EnumValueType = SyntaxFactory.IdentifierName("Type");
            private static readonly IdentifierNameSyntax EnumValuePositionUnderParent = SyntaxFactory.IdentifierName("PositionUnderParent");
            private static readonly IdentifierNameSyntax EnumValueParent = SyntaxFactory.IdentifierName("Parent");
            private static readonly IdentifierNameSyntax EnumValueAll = SyntaxFactory.IdentifierName("All");
            private static readonly IdentifierNameSyntax DiffGramTypeName = SyntaxFactory.IdentifierName("DiffGram");
            private static readonly IdentifierNameSyntax DiffGramChangeMethodName = SyntaxFactory.IdentifierName("Change");
            private static readonly IdentifierNameSyntax DiffGramAddMethodName = SyntaxFactory.IdentifierName("Add");
            private static readonly IdentifierNameSyntax DiffGramRemoveMethodName = SyntaxFactory.IdentifierName("Remove");
            private static readonly IdentifierNameSyntax DiffPropertiesMethodName = SyntaxFactory.IdentifierName(nameof(RecursiveDiffingTypeHelper.DiffProperties));

            private static readonly IdentifierNameSyntax DiffGramBeforePropertyName = SyntaxFactory.IdentifierName("Before");
            private static readonly IdentifierNameSyntax DiffGramAfterPropertyName = SyntaxFactory.IdentifierName("After");
            private static readonly IdentifierNameSyntax DiffGramKindPropertyName = SyntaxFactory.IdentifierName("Kind");
            private static readonly IdentifierNameSyntax DiffGramChangesPropertyName = SyntaxFactory.IdentifierName("Changes");

            private static readonly IdentifierNameSyntax ComparersClassName = SyntaxFactory.IdentifierName("Comparers");
            private static readonly IdentifierNameSyntax ComparersByValuePropertyName = SyntaxFactory.IdentifierName("ByValue");
            private static readonly IdentifierNameSyntax ComparersByValueWithDescendentsPropertyName = SyntaxFactory.IdentifierName("ByValueWithDescendents");

            private IdentifierNameSyntax changedPropertiesEnumTypeName;
            internal NameSyntax diffGramTypeSyntax;
            private QualifiedNameSyntax recursiveDiffingType;

            public DeltaGen(CodeGen generator)
                : base(generator)
            {
                var recursiveType = this.applyTo.RecursiveTypeFromFamily;
                if (!recursiveType.IsDefault)
                {
                    this.changedPropertiesEnumTypeName = SyntaxFactory.IdentifierName(recursiveType.TypeSymbol.Name + "ChangedProperties");
                    this.diffGramTypeSyntax = SyntaxFactory.QualifiedName(recursiveType.TypeSyntax, DiffGramTypeName);
                    this.recursiveDiffingType = SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)),
                        SyntaxFactory.GenericName(nameof(ImmutableObjectGraph.IRecursiveDiffingType<uint, uint>))
                            .AddTypeArgumentListArguments(
                                this.changedPropertiesEnumTypeName,
                                this.diffGramTypeSyntax));
                }
            }

            public override bool IsApplicable
            {
                get { return this.generator.options.Delta; }
            }

            protected override void GenerateCore()
            {
                if (this.applyTo.IsRecursiveType)
                {
                    // Implement IRecursiveDiffingType<RecursiveTypeChangedProperties, RecursiveType.DiffGram>
                    this.baseTypes.Add(SyntaxFactory.SimpleBaseType(this.recursiveDiffingType));

                    this.siblingMembers.Add(this.CreateChangedPropertiesEnum());
                    this.innerMembers.Add(this.CreateDiffGramStruct());
                    this.innerMembers.Add(this.CreateComparersClass());
                    this.innerMembers.AddRange(this.CreateBoilerplateEnumProperties());
                    this.innerMembers.Add(this.CreateDiffPropertiesExplicitMethod());
                    this.innerMembers.Add(this.CreateChangeMethod());
                    this.innerMembers.Add(this.CreateAddMethod());
                    this.innerMembers.Add(this.CreateRemoveMethod());
                    this.innerMembers.Add(this.CreateEqualsMethod());
                    this.innerMembers.Add(this.CreateUnionMethod());
                    this.innerMembers.Add(this.CreateDiffPropertiesMethod());
                }
                else if (this.applyTo.IsDerivedFromRecursiveType)
                {
                    var additionalFields = this.applyTo.LocalFields.Where(f => !f.IsRecursiveCollection);
                    if (additionalFields.Any())
                    {
                        this.innerMembers.Add(this.CreateDiffPropertiesOverrideMethod());
                    }
                }
            }

            protected EnumDeclarationSyntax CreateChangedPropertiesEnum()
            {
                var fields = this.generator.applyToMetaType.Concat(this.generator.applyToMetaType.Descendents)
                    .SelectMany(t => t.LocalFields)
                    .Where(f => !f.IsRecursiveCollection)
                    .GroupBy(f => f.Name.ToPascalCase());
                int fieldsCount = 4 + fields.Count();
                int counter = 3;
                var fieldEnumValues = new List<EnumMemberDeclarationSyntax>();
                foreach (var field in fields)
                {
                    fieldEnumValues.Add(
                        SyntaxFactory.EnumMemberDeclaration(
                            SyntaxFactory.List<AttributeListSyntax>(),
                            SyntaxFactory.Identifier(field.Key),
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(string.Format(CultureInfo.InvariantCulture, "0x{0:x}", (ulong)Math.Pow(2, counter)), counter)))));
                    counter++;
                }

                var allEnumValue = SyntaxFactory.EnumMemberDeclaration(
                        SyntaxFactory.List<AttributeListSyntax>(),
                        EnumValueAll.Identifier,
                        SyntaxFactory.EqualsValueClause(
                            Syntax.ChainBinaryExpressions(
                                new[] { EnumValueType, EnumValuePositionUnderParent, EnumValueParent }.Concat(
                                fields.Select(f => SyntaxFactory.IdentifierName(f.Key))),
                                SyntaxKind.BitwiseOrExpression)));

                TypeSyntax enumBaseType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(
                    fieldsCount > 32
                        ? SyntaxKind.ULongKeyword
                        : SyntaxKind.UIntKeyword));

                var result = SyntaxFactory.EnumDeclaration(this.changedPropertiesEnumTypeName.Identifier)
                    .AddModifiers(GetModifiersForAccessibility(this.generator.applyToSymbol))
                    .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(enumBaseType))))
                    .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(
                        SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Flags"))))))
                    .AddMembers(
                        SyntaxFactory.EnumMemberDeclaration(SyntaxFactory.List<AttributeListSyntax>(), EnumValueNone.Identifier, SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0x0", 0x0)))),
                        SyntaxFactory.EnumMemberDeclaration(SyntaxFactory.List<AttributeListSyntax>(), EnumValueType.Identifier, SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0x1", 0x1)))),
                        SyntaxFactory.EnumMemberDeclaration(SyntaxFactory.List<AttributeListSyntax>(), EnumValuePositionUnderParent.Identifier, SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0x2", 0x2)))),
                        SyntaxFactory.EnumMemberDeclaration(SyntaxFactory.List<AttributeListSyntax>(), EnumValueParent.Identifier, SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0x4", 0x4)))))
                    .AddMembers(fieldEnumValues.ToArray())
                    .AddMembers(allEnumValue);
                return result;
            }

            protected StructDeclarationSyntax CreateDiffGramStruct()
            {
                var beforeParam = SyntaxFactory.IdentifierName("before");
                var afterParam = SyntaxFactory.IdentifierName("after");
                var kindParam = SyntaxFactory.IdentifierName("kind");
                var changesParam = SyntaxFactory.IdentifierName("changes");
                var valueParam = SyntaxFactory.IdentifierName("value");

                Func<TypeSyntax, SyntaxToken, PropertyDeclarationSyntax> createAutoProperty = (type, name) =>
                    SyntaxFactory.PropertyDeclaration(type, name)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));

                return SyntaxFactory.StructDeclaration(DiffGramTypeName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddMembers(
                        // private DiffGram(TemplateType before, TemplateType after, ChangeKind kind, ChangedProperties changes) : this()
                        SyntaxFactory.ConstructorDeclaration(DiffGramTypeName.Identifier)
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                            .AddParameterListParameters(
                                SyntaxFactory.Parameter(beforeParam.Identifier).WithType(this.applyTo.TypeSyntax),
                                SyntaxFactory.Parameter(afterParam.Identifier).WithType(this.applyTo.TypeSyntax),
                                SyntaxFactory.Parameter(kindParam.Identifier).WithType(Syntax.GetTypeSyntax(typeof(ChangeKind))),
                                SyntaxFactory.Parameter(changesParam.Identifier).WithType(this.changedPropertiesEnumTypeName))
                            .WithInitializer(SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer, SyntaxFactory.ArgumentList()))
                            .WithBody(SyntaxFactory.Block(
                                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, Syntax.ThisDot(DiffGramBeforePropertyName), beforeParam)),
                                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, Syntax.ThisDot(DiffGramAfterPropertyName), afterParam)),
                                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, Syntax.ThisDot(DiffGramKindPropertyName), kindParam)),
                                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, Syntax.ThisDot(DiffGramChangesPropertyName), changesParam)))),

                        // public static DiffGram Change(<#= templateType.TypeName #> before, <#= templateType.TypeName #> after, <#= enumTypeName #> changes)
                        SyntaxFactory.MethodDeclaration(this.diffGramTypeSyntax, DiffGramChangeMethodName.Identifier)
                            .AddModifiers(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                            .AddParameterListParameters(
                                SyntaxFactory.Parameter(beforeParam.Identifier).WithType(this.applyTo.TypeSyntax),
                                SyntaxFactory.Parameter(afterParam.Identifier).WithType(this.applyTo.TypeSyntax),
                                SyntaxFactory.Parameter(changesParam.Identifier).WithType(this.changedPropertiesEnumTypeName))
                            .WithBody(SyntaxFactory.Block(
                                // return new DiffGram(before, after, ChangeKind.Replaced, changes);
                                SyntaxFactory.ReturnStatement(
                                    SyntaxFactory.ObjectCreationExpression(this.diffGramTypeSyntax).AddArgumentListArguments(
                                        SyntaxFactory.Argument(beforeParam),
                                        SyntaxFactory.Argument(afterParam),
                                        SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, Syntax.GetTypeSyntax(typeof(ChangeKind)), SyntaxFactory.IdentifierName(nameof(ChangeKind.Replaced)))),
                                        SyntaxFactory.Argument(changesParam))))),

                        // public static DiffGram Add(<#= templateType.TypeName #> value) {
                        SyntaxFactory.MethodDeclaration(this.diffGramTypeSyntax, DiffGramAddMethodName.Identifier)
                            .AddModifiers(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                            .AddParameterListParameters(SyntaxFactory.Parameter(valueParam.Identifier).WithType(this.applyTo.TypeSyntax))
                            .WithBody(SyntaxFactory.Block(
                                // return new DiffGram(null, value, ChangeKind.Added, default(<#= enumTypeName #>));
                                SyntaxFactory.ReturnStatement(
                                    SyntaxFactory.ObjectCreationExpression(this.diffGramTypeSyntax).AddArgumentListArguments(
                                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                        SyntaxFactory.Argument(valueParam),
                                        SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, Syntax.GetTypeSyntax(typeof(ChangeKind)), SyntaxFactory.IdentifierName(nameof(ChangeKind.Added)))),
                                        SyntaxFactory.Argument(SyntaxFactory.DefaultExpression(this.changedPropertiesEnumTypeName)))))),

                        // public static DiffGram Remove(<#= templateType.TypeName #> value) {
                        SyntaxFactory.MethodDeclaration(this.diffGramTypeSyntax, DiffGramRemoveMethodName.Identifier)
                            .AddModifiers(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                            .AddParameterListParameters(SyntaxFactory.Parameter(valueParam.Identifier).WithType(this.applyTo.TypeSyntax))
                            .WithBody(SyntaxFactory.Block(
                                // return new DiffGram(value, null, ChangeKind.Removed, default(<#= enumTypeName #>));
                                SyntaxFactory.ReturnStatement(
                                    SyntaxFactory.ObjectCreationExpression(this.diffGramTypeSyntax).AddArgumentListArguments(
                                        SyntaxFactory.Argument(valueParam),
                                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                        SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, Syntax.GetTypeSyntax(typeof(ChangeKind)), SyntaxFactory.IdentifierName(nameof(ChangeKind.Removed)))),
                                        SyntaxFactory.Argument(SyntaxFactory.DefaultExpression(this.changedPropertiesEnumTypeName)))))),

                        // public <#= templateType.TypeName #> Before { get; private set; }
                        createAutoProperty(this.applyTo.TypeSyntax, DiffGramBeforePropertyName.Identifier),

                        // public <#= templateType.TypeName #> After { get; private set; }
                        createAutoProperty(this.applyTo.TypeSyntax, DiffGramAfterPropertyName.Identifier),

                        // public ChangeKind Kind { get; private set; }
                        createAutoProperty(Syntax.GetTypeSyntax(typeof(ChangeKind)), DiffGramKindPropertyName.Identifier),

                        // public <#= enumTypeName #> Changes { get; private set; }
                        createAutoProperty(this.changedPropertiesEnumTypeName, DiffGramChangesPropertyName.Identifier),

                        // public uint Identity { get; }
                        SyntaxFactory.PropertyDeclaration(IdentityFieldTypeSyntax, IdentityPropertyName.Identifier)
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                            .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                                SyntaxKind.GetAccessorDeclaration,
                                SyntaxFactory.Block(
                                    // return (this.Before ?? this.After).<#= templateType.RequiredIdentityField.NamePascalCase #>;
                                    SyntaxFactory.ReturnStatement(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.ParenthesizedExpression(
                                                SyntaxFactory.BinaryExpression(
                                                    SyntaxKind.CoalesceExpression,
                                                    Syntax.ThisDot(DiffGramBeforePropertyName),
                                                    Syntax.ThisDot(DiffGramAfterPropertyName))),
                                            IdentityPropertyName)))))
                    )
                    .AddMembers(
                        this.applyTo.AllFields.Where(f => !f.IsRecursiveCollection).Select(field =>
                            // public bool Is<#=field.NamePascalCase#>Changed {
                            SyntaxFactory.PropertyDeclaration(
                                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                                "Is" + field.NameAsProperty + "Changed")
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration,
                                    SyntaxFactory.Block(
                                        // return (this.Changes & <#= enumTypeName #>.<#= field.NamePascalCase #>) != <#= enumTypeName #>.None;
                                        SyntaxFactory.ReturnStatement(
                                            SyntaxFactory.BinaryExpression(
                                                SyntaxKind.NotEqualsExpression,
                                                SyntaxFactory.ParenthesizedExpression(
                                                    SyntaxFactory.BinaryExpression(
                                                        SyntaxKind.BitwiseAndExpression,
                                                        Syntax.ThisDot(DiffGramChangesPropertyName),
                                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, this.changedPropertiesEnumTypeName, field.NameAsProperty))),
                                                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, this.changedPropertiesEnumTypeName, EnumValueNone))))))
                        ).ToArray<MemberDeclarationSyntax>());
            }

            protected PropertyDeclarationSyntax[] CreateBoilerplateEnumProperties()
            {
                Func<string, IdentifierNameSyntax, PropertyDeclarationSyntax> createProperty = (propertyName, enumValueName) =>
                    // <#= templateType.TypeName #>ChangedProperties IRecursiveDiffingType<<#= templateType.TypeName #>ChangedProperties, TemplateType.DiffGram>.ParentProperty {
                    SyntaxFactory.PropertyDeclaration(this.changedPropertiesEnumTypeName, propertyName)
                        .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(this.recursiveDiffingType))
                        .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                                // <#= templateType.TypeName #>ChangedProperties.Parent;
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    this.changedPropertiesEnumTypeName,
                                    enumValueName)))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(DebuggerBrowsableNeverAttribute)));

                return new PropertyDeclarationSyntax[] {
                    createProperty(nameof(RecursiveDiffingTypeHelper.ParentProperty), EnumValueParent),
                    createProperty(nameof(RecursiveDiffingTypeHelper.PositionUnderParentProperty), EnumValuePositionUnderParent),
                };
            }

            protected MethodDeclarationSyntax CreateDiffPropertiesExplicitMethod()
            {
                var otherParam = SyntaxFactory.IdentifierName("other");

                // <#= templateType.TypeName #>ChangedProperties IRecursiveDiffingType<<#= templateType.TypeName #>ChangedProperties, <#= templateType.TypeName #>.DiffGram>.DiffProperties(IRecursiveType other) {
                return SyntaxFactory.MethodDeclaration(this.changedPropertiesEnumTypeName, nameof(RecursiveDiffingTypeHelper.DiffProperties))
                    .AddParameterListParameters(SyntaxFactory.Parameter(otherParam.Identifier).WithType(Syntax.GetTypeSyntax(typeof(IRecursiveType))))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(this.recursiveDiffingType))
                    .WithBody(SyntaxFactory.Block(
                        // return this.DiffProperties((<#= templateType.TypeName #>)other);
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(Syntax.ThisDot(DiffPropertiesMethodName)).AddArgumentListArguments(
                                SyntaxFactory.Argument(SyntaxFactory.CastExpression(this.applyTo.TypeSyntax, otherParam))))));
            }

            protected MethodDeclarationSyntax CreateChangeMethod()
            {
                var beforeParam = SyntaxFactory.IdentifierName("before");
                var afterParam = SyntaxFactory.IdentifierName("after");
                var diffParam = SyntaxFactory.IdentifierName("diff");

                // <#= templateType.TypeName #>.DiffGram IRecursiveDiffingType<<#= templateType.TypeName #>ChangedProperties, <#= templateType.TypeName #>.DiffGram>
                //     .Change(IRecursiveType before, IRecursiveType after, <#= templateType.TypeName #>ChangedProperties diff) {
                return SyntaxFactory.MethodDeclaration(this.diffGramTypeSyntax, nameof(RecursiveDiffingTypeHelper.Change))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(this.recursiveDiffingType))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(beforeParam.Identifier).WithType(Syntax.GetTypeSyntax(typeof(IRecursiveType))),
                        SyntaxFactory.Parameter(afterParam.Identifier).WithType(Syntax.GetTypeSyntax(typeof(IRecursiveType))),
                        SyntaxFactory.Parameter(diffParam.Identifier).WithType(this.changedPropertiesEnumTypeName))
                    .WithBody(SyntaxFactory.Block(
                        // return DiffGram.Change((<#= templateType.TypeName #>)before, (<#= templateType.TypeName #>)after, diff);
                        SyntaxFactory.ReturnStatement(SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, this.diffGramTypeSyntax, DiffGramChangeMethodName))
                            .AddArgumentListArguments(
                                SyntaxFactory.Argument(SyntaxFactory.CastExpression(this.applyTo.TypeSyntax, beforeParam)),
                                SyntaxFactory.Argument(SyntaxFactory.CastExpression(this.applyTo.TypeSyntax, afterParam)),
                                SyntaxFactory.Argument(diffParam)))));
            }

            protected MethodDeclarationSyntax CreateAddMethod()
            {
                var afterParam = SyntaxFactory.IdentifierName("after");

                // <#= templateType.TypeName #>.DiffGram IRecursiveDiffingType<<#= templateType.TypeName #>ChangedProperties, <#= templateType.TypeName #>.DiffGram>
                //     .Add(IRecursiveType after) {
                return SyntaxFactory.MethodDeclaration(this.diffGramTypeSyntax, nameof(RecursiveDiffingTypeHelper.Add))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(this.recursiveDiffingType))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(afterParam.Identifier).WithType(Syntax.GetTypeSyntax(typeof(IRecursiveType))))
                    .WithBody(SyntaxFactory.Block(
                        // return DiffGram.Add((<#= templateType.TypeName #>)after);
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, this.diffGramTypeSyntax, DiffGramAddMethodName))
                                .AddArgumentListArguments(
                                    SyntaxFactory.Argument(SyntaxFactory.CastExpression(this.applyTo.TypeSyntax, afterParam))))));
            }

            protected MethodDeclarationSyntax CreateRemoveMethod()
            {
                var beforeParam = SyntaxFactory.IdentifierName("before");

                // <#= templateType.TypeName #>.DiffGram IRecursiveDiffingType<<#= templateType.TypeName #>ChangedProperties, <#= templateType.TypeName #>.DiffGram>
                //     .Remove(IRecursiveType before) {
                return SyntaxFactory.MethodDeclaration(this.diffGramTypeSyntax, nameof(RecursiveDiffingTypeHelper.Remove))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(this.recursiveDiffingType))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(beforeParam.Identifier).WithType(Syntax.GetTypeSyntax(typeof(IRecursiveType))))
                    .WithBody(SyntaxFactory.Block(
                        // return DiffGram.Remove((<#= templateType.TypeName #>)before);
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, this.diffGramTypeSyntax, DiffGramRemoveMethodName))
                                .AddArgumentListArguments(
                                    SyntaxFactory.Argument(SyntaxFactory.CastExpression(this.applyTo.TypeSyntax, beforeParam))))));
            }

            protected MethodDeclarationSyntax CreateEqualsMethod()
            {
                var firstParam = SyntaxFactory.IdentifierName("first");
                var secondParam = SyntaxFactory.IdentifierName("second");

                // bool IRecursiveDiffingType<<#= templateType.TypeName #>ChangedProperties, <#= templateType.TypeName #>.DiffGram>
                //      .Equals(<#= templateType.TypeName #>ChangedProperties first, <#= templateType.TypeName #>ChangedProperties second) {
                return SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                    nameof(RecursiveDiffingTypeHelper.Equals))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(this.recursiveDiffingType))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(firstParam.Identifier).WithType(this.changedPropertiesEnumTypeName),
                        SyntaxFactory.Parameter(secondParam.Identifier).WithType(this.changedPropertiesEnumTypeName))
                    .WithBody(SyntaxFactory.Block(
                        // return first == second;
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, firstParam, secondParam))));
            }

            protected MethodDeclarationSyntax CreateUnionMethod()
            {
                var firstParam = SyntaxFactory.IdentifierName("first");
                var secondParam = SyntaxFactory.IdentifierName("second");

                // <#= templateType.TypeName #>ChangedProperties IRecursiveDiffingType<<#= templateType.TypeName #>ChangedProperties, <#= templateType.TypeName #>.DiffGram>
                //    .Union(<#= templateType.TypeName #>ChangedProperties first, <#= templateType.TypeName #>ChangedProperties second) {
                return SyntaxFactory.MethodDeclaration(this.changedPropertiesEnumTypeName, nameof(RecursiveDiffingTypeHelper.Union))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(this.recursiveDiffingType))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(firstParam.Identifier).WithType(this.changedPropertiesEnumTypeName),
                        SyntaxFactory.Parameter(secondParam.Identifier).WithType(this.changedPropertiesEnumTypeName))
                    .WithBody(SyntaxFactory.Block(
                        // return first | second;
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseOrExpression, firstParam, secondParam))));
            }

            protected MethodDeclarationSyntax CreateDiffPropertiesMethod()
            {
                var otherParam = SyntaxFactory.IdentifierName("other");
                var propertiesChangedVar = SyntaxFactory.IdentifierName("propertiesChanged");
                var additionalFieldsVar = SyntaxFactory.IdentifierName("additionalFields");

                // protected virtual <#= enumTypeName #> DiffProperties(<#= templateType.TypeName #> other) {
                return SyntaxFactory.MethodDeclaration(this.changedPropertiesEnumTypeName, DiffPropertiesMethodName.Identifier)
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                        SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(otherParam.Identifier).WithType(this.applyTo.TypeSyntax))
                    .WithBody(SyntaxFactory.Block(
                        // 	if (other == null) { throw new System.ArgumentNullException("other"); }
                        Syntax.RequiresNotNull(otherParam),
                        // var propertiesChanged = <#= enumTypeName #>.None;
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(propertiesChangedVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, this.changedPropertiesEnumTypeName, EnumValueNone))))),
                        // if (this != other) {
                        SyntaxFactory.IfStatement(
                            SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, SyntaxFactory.ThisExpression(), otherParam),
                            SyntaxFactory.Block(
                                // if (!this.GetType().IsEquivalentTo(other.GetType())) {
                                SyntaxFactory.IfStatement(
                                    SyntaxFactory.PrefixUnaryExpression(
                                        SyntaxKind.LogicalNotExpression,
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.InvocationExpression(
                                                    Syntax.ThisDot(SyntaxFactory.IdentifierName(nameof(GetType))),
                                                    SyntaxFactory.ArgumentList()),
                                                SyntaxFactory.IdentifierName(nameof(Type.IsEquivalentTo)))).AddArgumentListArguments(
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.InvocationExpression(
                                                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, otherParam, SyntaxFactory.IdentifierName(nameof(GetType))),
                                                            SyntaxFactory.ArgumentList())))),
                                    // propertiesChanged |= <#= enumTypeName #>.Type;
                                    SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                                        SyntaxKind.OrAssignmentExpression,
                                        propertiesChangedVar,
                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, this.changedPropertiesEnumTypeName, EnumValueType)))))
                                .AddStatements(this.applyTo.LocalFields.Where(f => !f.IsRecursiveCollection).Select(field =>
                                    this.CreateIfPropertyChangedBlock(field, otherParam, propertiesChangedVar)).ToArray<StatementSyntax>())),

                        // return propertiesChanged;
                        SyntaxFactory.ReturnStatement(propertiesChangedVar)));
            }

            protected ClassDeclarationSyntax CreateComparersClass()
            {
                Func<TypeSyntax, SyntaxToken, ExpressionSyntax, PropertyDeclarationSyntax> defineProperty = (type, name, returnExpression) =>
                    SyntaxFactory.PropertyDeclaration(Syntax.IEqualityComparerOf(type), name)
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                            SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                        .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(SyntaxFactory.ReturnStatement(returnExpression))));
                // ImmutableObjectGraph.Comparers.ByValue<<#= templateType.TypeName #>ChangedProperties, DiffGram>
                var byValueChangedPropertiesDiffGramMethod =
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)), SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph.Comparers))),
                        SyntaxFactory.GenericName(nameof(Comparers.ByValue))
                            .AddTypeArgumentListArguments(this.changedPropertiesEnumTypeName, this.diffGramTypeSyntax));

                // public static class Comparers
                return SyntaxFactory.ClassDeclaration(ComparersClassName.Identifier)
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                    .AddMembers(
                        // /// <summary>Gets an equatable comparer that considers only the persistent identity of a pair of values.</summary>
                        // public static System.Collections.Generic.IEqualityComparer<<#= templateType.TypeName #>> <#= templateType.RequiredIdentityField.NamePascalCase #> {
                        //     get { return ImmutableObjectGraph.Comparers.Identity; }
                        // }
                        defineProperty(
                            this.applyTo.TypeSyntax,
                            IdentityPropertyName.Identifier,
                            SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)), SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph.Comparers))),
                                    SyntaxFactory.IdentifierName(nameof(Comparers.Identity)))),

                        // /// <summary>Gets an equatable comparer that compares all properties between two instances.</summary>
                        // public static System.Collections.Generic.IEqualityComparer<<#= templateType.TypeName #>> ByValue {
                        //     get { return ImmutableObjectGraph.Comparers.ByValue<<#= templateType.TypeName #>ChangedProperties, DiffGram>(deep: false); }
                        // }
                        defineProperty(
                            this.applyTo.TypeSyntax,
                            ComparersByValuePropertyName.Identifier,
                            SyntaxFactory.InvocationExpression(byValueChangedPropertiesDiffGramMethod).AddArgumentListArguments(
                                SyntaxFactory.Argument(SyntaxFactory.NameColon("deep"), NoneToken, SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))),

                        // /// <summary>Gets an equatable comparer that considers all properties between two instances and their children.</summary>
                        // public static System.Collections.Generic.IEqualityComparer<<#= templateType.TypeName #>> ByValueWithDescendents {
                        //     get { return ImmutableObjectGraph.Comparers.ByValue<<#= templateType.TypeName #>ChangedProperties, DiffGram>(deep: true); }
                        // }
                        defineProperty(
                            this.applyTo.TypeSyntax,
                            ComparersByValueWithDescendentsPropertyName.Identifier,
                            SyntaxFactory.InvocationExpression(byValueChangedPropertiesDiffGramMethod).AddArgumentListArguments(
                                SyntaxFactory.Argument(SyntaxFactory.NameColon("deep"), NoneToken, SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))))

                        //// internal static IEqualityComparer<ParentedRecursiveType<RecursiveParent, RecursiveTypeFromFamily> Parented<#= templateType.TypeName #><#= templateType.RequiredIdentityField.NamePascalCase #> {
                        ////     get { return ImmutableObjectGraph.Comparers.Parented<<#= templateType.RecursiveParent.TypeName #>, <#= templateType.TypeName #>>(); }
                        //// }
                        //defineProperty(
                            
                        //    this.recursiveDiffingType
                        //    ).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                    );
            }

            private IfStatementSyntax CreateIfPropertyChangedBlock(MetaField field, IdentifierNameSyntax otherParam, IdentifierNameSyntax propertiesChangedVar)
            {
                // if (this.Property != other.Property) {
                return
                    SyntaxFactory.IfStatement(
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.NotEqualsExpression,
                            Syntax.ThisDot(field.NameAsProperty),
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, otherParam, field.NameAsProperty)),
                        // propertiesChanged |= <#= enumTypeName #>.<#= field.NamePascalCase #>;
                        SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                            SyntaxKind.OrAssignmentExpression,
                            propertiesChangedVar,
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, this.changedPropertiesEnumTypeName, field.NameAsProperty))));
            }

            protected MethodDeclarationSyntax CreateDiffPropertiesOverrideMethod()
            {
                var otherParam = SyntaxFactory.IdentifierName("other");
                var propertiesChangedVar = SyntaxFactory.IdentifierName("propertiesChanged");
                var otherTypedVar = SyntaxFactory.IdentifierName("other" + this.applyTo.TypeSymbol.Name);

                // protected override <#= enumTypeName #> DiffProperties(<#= recursiveType.TypeName #> other) {
                return SyntaxFactory.MethodDeclaration(this.changedPropertiesEnumTypeName, DiffPropertiesMethodName.Identifier)
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                        SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(otherParam.Identifier).WithType(this.applyTo.RecursiveTypeFromFamily.TypeSyntax))
                    .WithBody(SyntaxFactory.Block(
                        // var propertiesChanged = base.DiffProperties(other);
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(propertiesChangedVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.BaseExpression(), DiffPropertiesMethodName))
                                    .AddArgumentListArguments(SyntaxFactory.Argument(otherParam)))))),
                        // var other<#= templateType.TypeName #> = other as <#= templateType.TypeName #>;
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(varType).AddVariables(
                            SyntaxFactory.VariableDeclarator(otherTypedVar.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, otherParam, this.applyTo.TypeSyntax))))),
                        // if (other<#= templateType.TypeName #> != null && other != this) {
                        SyntaxFactory.IfStatement(
                            SyntaxFactory.BinaryExpression(
                                SyntaxKind.LogicalAndExpression,
                                SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, otherTypedVar, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, otherParam, SyntaxFactory.ThisExpression())),
                            SyntaxFactory.Block(
                                this.applyTo.LocalFields.Where(f => !f.IsRecursiveCollection).Select(field =>
                                    CreateIfPropertyChangedBlock(field, otherTypedVar, propertiesChangedVar)).ToArray<StatementSyntax>())),
                        // return propertiesChanged;
                        SyntaxFactory.ReturnStatement(propertiesChangedVar)));
            }
        }
    }
}
