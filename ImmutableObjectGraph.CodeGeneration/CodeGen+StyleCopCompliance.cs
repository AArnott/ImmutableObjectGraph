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

    public partial class CodeGen
    {
        protected class StyleCopCompliance : FeatureGenerator
        {
            public StyleCopCompliance(CodeGen generator) : base(generator)
            {
            }

            public override bool IsApplicable
            {
                get { return true; }
            }

            protected override void GenerateCore()
            {
            }

            public override ClassDeclarationSyntax ProcessApplyToClassDeclaration(ClassDeclarationSyntax applyTo)
            {
                applyTo = base.ProcessApplyToClassDeclaration(applyTo);

                // Sort the members now per StyleCop rules.
                var innerMembers = applyTo.Members.ToList();
                innerMembers.Sort(StyleCop.Sort);
                applyTo = applyTo.WithMembers(SyntaxFactory.List(innerMembers));

                return applyTo;
            }
        }
    }
}
