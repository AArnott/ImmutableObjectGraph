namespace ImmutableObjectGraph.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.ImmutableObjectGraph_SFG;
    using Validation;

    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateImmutableAttribute : CodeGenerationAttribute
    {
        public GenerateImmutableAttribute()
        {
        }

        public bool GenerateBuilder { get; set; }

        public bool Delta { get; set; }

        public bool DefineInterface { get; set; }

        public override Task<IReadOnlyList<MemberDeclarationSyntax>> GenerateAsync(MemberDeclarationSyntax applyTo, Document document, IProgressAndErrors progress, CancellationToken cancellationToken)
        {
            var options = new CodeGen.Options
            {
                GenerateBuilder = this.GenerateBuilder,
                Delta = this.Delta,
                DefineInterface = this.DefineInterface,
            };

            return CodeGen.GenerateAsync((ClassDeclarationSyntax)applyTo, document, progress, options, cancellationToken);
        }
    }
}
