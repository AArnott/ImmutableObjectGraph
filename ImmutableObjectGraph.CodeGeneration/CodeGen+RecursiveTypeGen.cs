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
        protected class RecursiveTypeGen : FeatureGenerator
        {
            public RecursiveTypeGen(CodeGen generator)
                : base(generator)
            {
            }

            public override bool IsApplicable
            {
                get { return this.generator.applyToMetaType.IsRecursiveType; }
            }

            protected override void GenerateCore()
            {
                this.baseTypes.Add(SyntaxFactory.SimpleBaseType(Syntax.GetTypeSyntax(typeof(IRecursiveType))));

                ////<#= templateType.RequiredIdentityField.TypeName #> IRecursiveType.Identity {
                ////	get { return this.Identity; }
                ////}
                this.innerMembers.Add(SyntaxFactory.PropertyDeclaration(
                    IdentityFieldTypeSyntax,
                    nameof(IRecursiveType.Identity))
                    .WithExplicitInterfaceSpecifier(
                        SyntaxFactory.ExplicitInterfaceSpecifier(Syntax.GetTypeSyntax(typeof(IRecursiveType))))
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(SyntaxFactory.ReturnStatement(Syntax.ThisDot(IdentityPropertyName))))));
            }
        }
    }
}
