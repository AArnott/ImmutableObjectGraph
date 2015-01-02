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
        protected class InterfacesGen : FeatureGenerator
        {
            public InterfacesGen(CodeGen generator)
                : base(generator)
            {
            }

            protected override bool IsApplicable
            {
                get { return this.generator.options.DefineInterface; }
            }

            protected override void GenerateCore()
            {
                var iface = SyntaxFactory.InterfaceDeclaration(
                    "I" + this.generator.applyTo.Identifier.Text)
                    .AddModifiers(GetModifiersForAccessibility(this.generator.applyToSymbol))
                    .WithMembers(
                        SyntaxFactory.List<MemberDeclarationSyntax>(
                            from field in this.generator.applyToMetaType.LocalFields
                            select SyntaxFactory.PropertyDeclaration(
                                GetFullyQualifiedSymbolName(field.Type),
                                field.Name.ToPascalCase())
                                .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(
                                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))))));
                if (this.generator.applyToMetaType.HasAncestor)
                {
                    iface = iface.WithBaseList(SyntaxFactory.BaseList(
                        SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(
                            SyntaxFactory.IdentifierName("I" + this.generator.applyToMetaType.Ancestor.TypeSymbol.Name)))));
                }

                this.siblingMembers.Add(iface);
            }

            protected override void PostProcessCore()
            {
                var applyToPrimaryType = this.generator.outerMembers.OfType<ClassDeclarationSyntax>()
                    .First(c => c.Identifier.Text == this.generator.applyTo.Identifier.Text);
                var updatedPrimaryType = applyToPrimaryType.WithBaseList(
                    (applyToPrimaryType.BaseList ?? SyntaxFactory.BaseList()).AddTypes(SyntaxFactory.SimpleBaseType(
                        SyntaxFactory.IdentifierName("I" + this.generator.applyTo.Identifier.Text))));

                this.generator.outerMembers.Remove(applyToPrimaryType);
                this.generator.outerMembers.Add(updatedPrimaryType);
            }
        }
    }
}
