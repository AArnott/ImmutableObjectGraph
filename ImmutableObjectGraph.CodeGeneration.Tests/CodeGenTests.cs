namespace ImmutableObjectGraph.CodeGeneration.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.MSBuild;
    using Microsoft.CodeAnalysis.Text;
    using Microsoft.ImmutableObjectGraph_SFG;
    using Xunit;

    public class CodeGenTests
    {
        protected Solution solution;
        protected ProjectId projectId;
        protected DocumentId inputDocumentId;

        public CodeGenTests()
        {
            var workspace = new CustomWorkspace();
            var project = workspace.CurrentSolution.AddProject("test", "test", "C#")
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReference(MetadataReference.CreateFromAssembly(typeof(string).Assembly))
                .AddMetadataReference(MetadataReference.CreateFromAssembly(typeof(GenerateImmutableAttribute).Assembly))
                .AddMetadataReference(MetadataReference.CreateFromAssembly(typeof(CodeGenerationAttribute).Assembly))
                .AddMetadataReference(MetadataReference.CreateFromAssembly(typeof(Optional).Assembly));
            var inputDocument = project.AddDocument("input.cs", string.Empty);
            this.inputDocumentId = inputDocument.Id;
            project = inputDocument.Project;
            this.projectId = inputDocument.Project.Id;
            this.solution = project.Solution;
        }

        [Fact]
        public async Task Empty_NoBuildBreaks()
        {
            Document result = await this.GenerateAsync(@"
[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class Empty { }
");
        }

        protected async Task<Document> GenerateAsync(string inputSource)
        {
            var solution = this.solution.WithDocumentText(this.inputDocumentId, SourceText.From(inputSource));
            var inputDocument = solution.GetDocument(this.inputDocumentId);
            var outputDocument = await DocumentTransform.TransformAsync(inputDocument, new MockProgress());

            // Make sure there are no compile errors.
            var compilation = await outputDocument.Project.GetCompilationAsync();
            var diagnostics = compilation.GetDiagnostics();
            var errors = from diagnostic in diagnostics
                         where diagnostic.Severity >= DiagnosticSeverity.Error
                         select diagnostic;

            Console.WriteLine(await outputDocument.GetTextAsync());

            foreach (var error in errors)
            {
                Console.WriteLine(error);
            }

            Assert.Empty(errors);

            return outputDocument;
        }

        private class MockProgress : IProgressAndErrors
        {
            public void Error(string message, uint line, uint column)
            {
                throw new NotImplementedException();
            }

            public void Report(uint progress, uint total)
            {
                throw new NotImplementedException();
            }

            public void Warning(string message, uint line, uint column)
            {
                throw new NotImplementedException();
            }
        }
    }
}
