namespace ImmutableObjectGraph.Generation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeGeneration.Roslyn;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Validation;

    public class CodeGenerator : ICodeGenerator
    {
        private readonly AttributeData attributeData;
        private readonly ImmutableDictionary<string, TypedConstant> data;

        public CodeGenerator(AttributeData attributeData)
        {
            Requires.NotNull(attributeData, nameof(attributeData));

            this.attributeData = attributeData;
            this.data = this.attributeData.NamedArguments.ToImmutableDictionary(kv => kv.Key, kv => kv.Value);
        }

        public Task<IReadOnlyList<MemberDeclarationSyntax>> GenerateAsync(MemberDeclarationSyntax applyTo, Document document, IProgress<Diagnostic> progress, CancellationToken cancellationToken)
        {
            Requires.NotNull(applyTo, nameof(applyTo));

            var options = new CodeGen.Options(this.attributeData)
            {
                GenerateBuilder = this.GetBoolData(nameof(GenerateImmutableAttribute.GenerateBuilder)),
                Delta = this.GetBoolData(nameof(GenerateImmutableAttribute.Delta)),
                DefineInterface = this.GetBoolData(nameof(GenerateImmutableAttribute.DefineInterface)),
                DefineRootedStruct = this.GetBoolData(nameof(GenerateImmutableAttribute.DefineRootedStruct)),
                DefineWithMethodsPerProperty = this.GetBoolData(nameof(GenerateImmutableAttribute.DefineWithMethodsPerProperty)),
            };

            return CodeGen.GenerateAsync((ClassDeclarationSyntax)applyTo, document, progress, options, cancellationToken);
        }

        private bool GetBoolData(string name)
        {
            return (bool)(this.data.GetValueOrDefault(name).Value ?? false);
        }
    }
}
