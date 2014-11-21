namespace ImmutableObjectGraph.CodeGeneration.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
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
                .AddMetadataReference(MetadataReference.CreateFromAssembly(typeof(Optional).Assembly))
                .AddMetadataReference(MetadataReference.CreateFromAssembly(typeof(ImmutableArray).Assembly))
                .AddMetadataReference(MetadataReference.CreateFromAssembly(Assembly.LoadWithPartialName("System.Runtime")));
            var inputDocument = project.AddDocument("input.cs", string.Empty);
            this.inputDocumentId = inputDocument.Id;
            project = inputDocument.Project;
            this.projectId = inputDocument.Project.Id;
            this.solution = project.Solution;
        }

        [Fact]
        public async Task CanCreateImmutableTypeWithNoMembers()
        {
            await this.GenerateFromStreamAsync("CanCreateImmutableTypeWithNoMembers");
        }

        [Fact]
        public async Task FamilyPersonWatch()
        {
            await this.GenerateFromStreamAsync("FamilyPersonWatch");
        }

        [Fact]
        public async Task OneScalarField_HasWithMethod()
        {
            var document = await this.GenerateFromStreamAsync("OneScalarField");
            var semantic = await document.GetSemanticModelAsync();
            var declaredSymbol = semantic.GetDeclarationsInSpan(TextSpan.FromBounds(0, semantic.SyntaxTree.Length), true, CancellationToken.None);
            var declaredMethods = declaredSymbol.Select(s => s.DeclaredSymbol).OfType<IMethodSymbol>();
            Assert.True(declaredMethods.Any(m => m.Name == "With" && m.Parameters.Any(p => p.Name == "seeds")));
        }

        protected async Task<Document> GenerateFromStreamAsync(string testName)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(this.GetType().Namespace + ".TestSources." + testName + ".cs"))
            {
                return await this.GenerateAsync(SourceText.From(stream));
            }
        }

        protected async Task<Document> GenerateAsync(SourceText inputSource)
        {
            var solution = this.solution.WithDocumentText(this.inputDocumentId, inputSource);
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
            }

            public void Report(uint progress, uint total)
            {
            }

            public void Warning(string message, uint line, uint column)
            {
            }
        }
    }
}
