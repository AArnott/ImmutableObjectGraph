using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Validation;

namespace ImmutableObjectGraph.CodeGeneration.Roslyn
{
    public class CodeGenerator : ICodeGenerator
    {
        private readonly GenerateImmutableAttribute attribute;

        public CodeGenerator(GenerateImmutableAttribute attribute)
        {
            Requires.NotNull(attribute, nameof(attribute));

            this.attribute = attribute;
        }

        public Task<IReadOnlyList<MemberDeclarationSyntax>> GenerateAsync(MemberDeclarationSyntax applyTo, Document document, IProgressAndErrors progress, CancellationToken cancellationToken)
        {
            Requires.NotNull(attribute, nameof(attribute));
            Requires.NotNull(applyTo, nameof(applyTo));

            var that = this.attribute;
            var options = new CodeGen.Options
            {
                GenerateBuilder = that.GenerateBuilder,
                Delta = that.Delta,
                DefineInterface = that.DefineInterface,
                DefineRootedStruct = that.DefineRootedStruct,
                DefineWithMethodsPerProperty = that.DefineWithMethodsPerProperty,
            };

            return CodeGen.GenerateAsync((ClassDeclarationSyntax)applyTo, document, progress, options, cancellationToken);
        }
    }
}
