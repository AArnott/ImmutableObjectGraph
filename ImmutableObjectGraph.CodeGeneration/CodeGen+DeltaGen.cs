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
        protected class DeltaGen : FeatureGenerator
        {
            private static readonly IdentifierNameSyntax EnumValueNone = SyntaxFactory.IdentifierName("None");
            private static readonly IdentifierNameSyntax EnumValueType = SyntaxFactory.IdentifierName("Type");
            private static readonly IdentifierNameSyntax EnumValuePositionUnderParent = SyntaxFactory.IdentifierName("PositionUnderParent");
            private static readonly IdentifierNameSyntax EnumValueParent = SyntaxFactory.IdentifierName("Parent");
            private static readonly IdentifierNameSyntax EnumValueAll = SyntaxFactory.IdentifierName("All");
            private static readonly IdentifierNameSyntax DiffGramTypeName = SyntaxFactory.IdentifierName("DiffGram");

            private IdentifierNameSyntax enumTypeName;
            private NameSyntax diffGramTypeSyntax;

            public DeltaGen(CodeGen generator)
                : base(generator)
            {
            }

            public override bool IsApplicable
            {
                get { return this.generator.options.Delta; }
            }

            protected override void GenerateCore()
            {
                if (this.applyTo.IsRecursiveType)
                {
                    this.enumTypeName = SyntaxFactory.IdentifierName(this.applyTo.TypeSymbol.Name + "ChangedProperties");
                    this.diffGramTypeSyntax = SyntaxFactory.QualifiedName(this.applyTo.TypeSyntax, DiffGramTypeName);

                    // Implement IRecursiveDiffingType<RecursiveTypeChangedProperties, RecursiveType.DiffGram>
                    this.baseTypes.Add(SyntaxFactory.SimpleBaseType(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)),
                            SyntaxFactory.GenericName(nameof(ImmutableObjectGraph.IRecursiveDiffingType<uint, uint>))
                                .AddTypeArgumentListArguments(
                                    this.enumTypeName,
                                    this.diffGramTypeSyntax))));

                    this.siblingMembers.Add(this.CreateChangedPropertiesEnum());
                    this.innerMembers.Add(this.CreateDiffGramStruct());
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

                var result = SyntaxFactory.EnumDeclaration(this.enumTypeName.Identifier)
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
                return SyntaxFactory.StructDeclaration(DiffGramTypeName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            }
        }
    }
}
