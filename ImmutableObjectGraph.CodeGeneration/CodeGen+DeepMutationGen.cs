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
        protected class DeepMutationGen : FeatureGenerator
        {
            private static readonly IdentifierNameSyntax AddDescendentMethodName = SyntaxFactory.IdentifierName("AddDescendent");
            private static readonly IdentifierNameSyntax ReplaceDescendentMethodName = SyntaxFactory.IdentifierName("ReplaceDescendent");
            private static readonly IdentifierNameSyntax RemoveDescendentMethodName = SyntaxFactory.IdentifierName("RemoveDescendent");

            public DeepMutationGen(CodeGen generator)
                  : base(generator)
            {
            }

            protected override bool IsApplicable
            {
                get { return this.generator.applyToMetaType.IsRecursive; }
            }

            protected override void GenerateCore()
            {
                this.innerMembers.Add(this.CreateAddDescendentMethod());
            }

            private MethodDeclarationSyntax CreateAddDescendentMethod()
            {
                var valueParameterName = SyntaxFactory.IdentifierName("value");
                var parentParameterName = SyntaxFactory.IdentifierName("parent");

                return SyntaxFactory.MethodDeclaration(
                    GetFullyQualifiedSymbolName(this.generator.applyToSymbol),
                    AddDescendentMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(valueParameterName.Identifier).WithType(this.generator.applyToMetaType.RecursiveType.TypeSyntax),
                        SyntaxFactory.Parameter(parentParameterName.Identifier).WithType(this.generator.applyToMetaType.RecursiveParent.TypeSyntax))
                    .WithBody(SyntaxFactory.Block(ThrowNotImplementedException));
            }
        }
    }
}
