namespace ImmutableObjectGraph.Generation.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using global::CodeGeneration.Roslyn;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.MSBuild;
    using Microsoft.CodeAnalysis.Text;
    using Validation;
    using Xunit;
    using Xunit.Abstractions;

    public class CodeGenTests
    {
        protected Solution solution;
        protected ProjectId projectId;
        protected DocumentId inputDocumentId;

        private readonly ITestOutputHelper logger;

        public CodeGenTests(ITestOutputHelper logger)
        {
            Requires.NotNull(logger, nameof(logger));

            this.logger = logger;
            var workspace = new AdhocWorkspace();
            var project = workspace.CurrentSolution.AddProject("test", "test", "C#")
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(GetReferences("Profile78"))
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(GenerateImmutableAttribute).Assembly.Location))
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(CodeGenerationAttributeAttribute).Assembly.Location))
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(Optional).Assembly.Location))
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location));
            var inputDocument = project.AddDocument("input.cs", string.Empty);
            this.inputDocumentId = inputDocument.Id;
            project = inputDocument.Project;
            this.projectId = inputDocument.Project.Id;
            this.solution = project.Solution;
        }

        [Fact]
        public async Task UsingImmutableObjectGraph_Compiles()
        {
            await this.GenerateFromStreamAsync("UsingImmutableObjectGraph");
        }

        [Fact]
        public async Task NoFieldsAndNoFieldsDerived_HasCreateMethod()
        {
            var result = await this.GenerateFromStreamAsync("NoFieldsAndNoFieldsDerived");
            Assert.Equal(2, result.DeclaredMethods.Count(m => m.Name == "Create" && m.Parameters.Length == 0 && m.IsStatic));
        }

        [Fact]
        public async Task NoFieldsAndOneScalarFieldDerived_HasCreateMethod()
        {
            var result = await this.GenerateFromStreamAsync("NoFieldsAndOneScalarFieldDerived");
            Assert.Equal(1, result.DeclaredMethods.Count(m => m.ContainingType.Name == "Empty" && m.Name == "Create" && m.Parameters.Length == 0 && m.IsStatic));
            Assert.Equal(1, result.DeclaredMethods.Count(m => m.ContainingType.Name == "NotSoEmptyDerived" && m.Name == "Create" && m.Parameters.Length == 1 && m.IsStatic));
        }

        [Fact]
        public async Task OneScalarFieldAndEmptyDerived_HasCreateMethod()
        {
            var result = await this.GenerateFromStreamAsync("OneScalarFieldAndEmptyDerived");
            Assert.Equal(2, result.DeclaredMethods.Count(m => m.Name == "Create" && m.Parameters.Length == 1 && m.IsStatic));
        }

        [Fact]
        public async Task OneScalarField_HasWithMethod()
        {
            var result = await this.GenerateFromStreamAsync("OneScalarField");
            Assert.True(result.DeclaredMethods.Any(m => m.Name == "With" && m.Parameters.Single().Name == "seeds" && !m.IsStatic));
        }

        [Fact]
        public async Task OneScalarField_HasCreateMethod()
        {
            var result = await this.GenerateFromStreamAsync("OneScalarField");
            Assert.True(result.DeclaredMethods.Any(m => m.Name == "Create" && m.Parameters.Single().Name == "seeds"));
        }

        [Fact]
        public async Task OneScalarFieldWithBuilder_HasToBuilderMethod()
        {
            var result = await this.GenerateFromStreamAsync("OneScalarFieldWithBuilder");
            Assert.True(result.DeclaredMethods.Any(m => m.Name == "ToBuilder" && m.Parameters.Length == 0 && !m.IsStatic));
        }

        [Fact]
        public async Task OneScalarFieldWithBuilder_HasCreateBuilderMethod()
        {
            var result = await this.GenerateFromStreamAsync("OneScalarFieldWithBuilder");
            Assert.True(result.DeclaredMethods.Any(m => m.Name == "CreateBuilder" && m.Parameters.Length == 0 && m.IsStatic));
        }

        [Fact]
        public async Task OneScalarFieldWithBuilder_BuilderHasMutableProperties()
        {
            var result = await this.GenerateFromStreamAsync("OneScalarFieldWithBuilder");
            Assert.True(result.DeclaredProperties.Any(p => p.ContainingType?.Name == "Builder" && p.Name == "Seeds" && p.SetMethod != null && p.GetMethod != null));
        }

        [Fact]
        public async Task OneScalarFieldWithBuilder_BuilderHasToImmutableMethod()
        {
            var result = await this.GenerateFromStreamAsync("OneScalarFieldWithBuilder");
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType?.Name == "Builder" && m.Name == "ToImmutable" && m.Parameters.Length == 0 && !m.IsStatic));
        }

        [Fact]
        public async Task ClassDerivesFromAnotherWithFields_DerivedCreateParametersIncludeBaseFields()
        {
            var result = await this.GenerateFromStreamAsync("ClassDerivesFromAnotherWithFields");
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType?.Name == "Fruit" && m.Name == "Create" && m.Parameters.Length == 1));
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType?.Name == "Apple" && m.Name == "Create" && m.Parameters.Length == 2));
        }

        [Fact]
        public async Task ClassDerivesFromAnotherWithFields_DerivedWithParametersIncludeBaseFields()
        {
            var result = await this.GenerateFromStreamAsync("ClassDerivesFromAnotherWithFields");
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType?.Name == "Fruit" && m.Name == "With" && m.Parameters.Length == 1));
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType?.Name == "Apple" && m.Name == "With" && m.Parameters.Length == 2));
        }

        [Fact]
        public async Task ClassDerivesFromAnotherWithFields_DerivedWithCoreParametersIncludeBaseFields()
        {
            var result = await this.GenerateFromStreamAsync("ClassDerivesFromAnotherWithFields");
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType?.Name == "Fruit" && m.Name == "WithCore" && m.Parameters.Length == 1 && m.IsVirtual));
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType?.Name == "Apple" && m.Name == "WithCore" && m.Parameters.Length == 1 && m.IsOverride));
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType?.Name == "Apple" && m.Name == "WithCore" && m.Parameters.Length == 2 && m.IsVirtual));
        }

        [Fact]
        public async Task ClassDerivesFromAnotherWithFieldsAndBuilder_BuildersReflectTypeRelationship()
        {
            var result = await this.GenerateFromStreamAsync("ClassDerivesFromAnotherWithFieldsAndBuilder");
            var fruitBuilder = result.DeclaredTypes.Single(t => t.Name == "Builder" && t.ContainingType.Name == "Fruit");
            Assert.Same(fruitBuilder, result.DeclaredTypes.Single(t => t.Name == "Builder" && t.ContainingType.Name == "Apple").BaseType);
        }

        [Fact]
        public async Task AbstractNonEmptyWithDerivedEmpty_HasCreateOnlyInNonAbstractClass()
        {
            var result = await this.GenerateFromStreamAsync("AbstractNonEmptyWithDerivedEmpty");
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType.Name == "EmptyDerivedFromAbstract" && m.Name == "Create" && m.Parameters.Single().Name == "oneField"));
            Assert.False(result.DeclaredMethods.Any(m => m.ContainingType.Name == "AbstractNonEmpty" && m.Name == "Create"));
        }

        [Fact]
        public async Task AbstractNonEmptyWithDerivedEmpty_HasValidateMethodOnBothTypes()
        {
            var result = await this.GenerateFromStreamAsync("AbstractNonEmptyWithDerivedEmpty");
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType.Name == "EmptyDerivedFromAbstract" && m.Name == "Validate"));
            Assert.False(result.DeclaredMethods.Any(m => m.ContainingType.Name == "AbstractNonEmpty" && m.Name == "Validate"));
        }

        [Fact]
        public async Task AbstractNonEmptyWithDerivedEmpty_HasWithMethodOnBothTypes()
        {
            var result = await this.GenerateFromStreamAsync("AbstractNonEmptyWithDerivedEmpty");
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType.Name == "AbstractNonEmpty" && m.Name == "With" && m.Parameters.Single().Name == "oneField"));
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType.Name == "AbstractNonEmpty" && m.Name == "With" && m.Parameters.Single().Name == "oneField"));
        }

        [Fact]
        public async Task AbstractNonEmptyWithDerivedEmpty_HasWithCoreMethodOnBothTypes()
        {
            var result = await this.GenerateFromStreamAsync("AbstractNonEmptyWithDerivedEmpty");
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType.Name == "EmptyDerivedFromAbstract" && m.Name == "WithCore" && m.Parameters.Single().Name == "oneField"));
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType.Name == "AbstractNonEmpty" && m.Name == "WithCore" && m.Parameters.Single().Name == "oneField"));
        }

        [Fact]
        public async Task AbstractNonEmptyWithDerivedEmpty_HasWithFactoryMethodOnConcreteTypeOnly()
        {
            var result = await this.GenerateFromStreamAsync("AbstractNonEmptyWithDerivedEmpty");
            Assert.True(result.DeclaredMethods.Any(m => m.ContainingType.Name == "EmptyDerivedFromAbstract" && m.Name == "WithFactory" && m.Parameters.Length == 2));
            Assert.False(result.DeclaredMethods.Any(m => m.ContainingType.Name == "AbstractNonEmpty" && m.Name == "WithFactory" && m.Parameters.Length == 2));
        }

        [Fact]
        public async Task IgnoreField_Compiles()
        {
            var result = await this.GenerateFromStreamAsync("IgnoreField");
        }

        [Fact]
        public async Task DefineRootedStruct_NotApplicable()
        {
            var result = await this.GenerateFromStreamAsync("DefineRootedStruct_NotApplicable");
            var warning = result.GeneratorDiagnostics.Single();
            Assert.Equal(Diagnostics.NotApplicableSetting, warning.Id);

            var location = warning.Location.GetLineSpan();
            Assert.Equal(9, location.StartLinePosition.Line);
            Assert.Equal(23, location.StartLinePosition.Character);
            Assert.Equal(9, location.EndLinePosition.Line);
            Assert.Equal(48, location.EndLinePosition.Character);
        }

        [Fact]
        public async Task RootedStruct_Without_WithMethodsPerProperty()
        {
            var result = await this.GenerateFromStreamAsync("RootedStruct_Without_WithMethodsPerProperty");
        }

        [Fact]
        public async Task OneImmutableFieldToAnotherWithOneScalarField_Compiles()
        {
            var result = await this.GenerateFromStreamAsync("OneImmutableFieldToAnotherWithOneScalarField");

        }

        [Fact]
        public async Task HierarchyLevels_Compiles()
        {
            await this.GenerateFromStreamAsync("HierarchyLevels");
        }

        [Fact]
        public async Task AlmostRecursive_Compiles()
        {
            await this.GenerateFromStreamAsync("AlmostRecursive");
        }

        protected async Task<GenerationResult> GenerateFromStreamAsync(string testName)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(this.GetType().Namespace + ".TestSources." + testName + ".cs"))
            {
                var result = await this.GenerateAsync(SourceText.From(stream));

                Assert.Empty(result.CompilationDiagnostics.Where(
                    d => !d.IsSuppressed && d.Severity != DiagnosticSeverity.Hidden));

                return result;
            }
        }

        protected async Task<GenerationResult> GenerateAsync(SourceText inputSource)
        {
            var solution = this.solution.WithDocumentText(this.inputDocumentId, inputSource);
            var inputDocument = solution.GetDocument(this.inputDocumentId);
            var generatorDiagnostics = new List<Diagnostic>();
            var progress = new SynchronousProgress<Diagnostic>(generatorDiagnostics.Add);
            var outputDocument = await DocumentTransform.TransformAsync(inputDocument, progress);

            // Make sure the result compiles without errors or warnings.
            var compilation = await outputDocument.Project.GetCompilationAsync();
            var compilationDiagnostics = compilation.GetDiagnostics();

            SourceText outputDocumentText = await outputDocument.GetTextAsync();
            this.logger.WriteLine("{0}", outputDocumentText);

            // Verify all line endings are consistent (otherwise VS can bug the heck out of the user if they have the generated file open).
            string firstLineEnding = null;
            foreach (var line in outputDocumentText.Lines)
            {
                string actualNewLine = line.Text.GetSubText(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak)).ToString();
                if (firstLineEnding == null)
                {
                    firstLineEnding = actualNewLine;
                }
                else if (actualNewLine != firstLineEnding && actualNewLine.Length > 0)
                {
                    string expected = EscapeLineEndingCharacters(firstLineEnding);
                    string actual = EscapeLineEndingCharacters(actualNewLine);
                    Assert.True(false, $"Expected line ending characters '{expected}' but found '{actual}' on line {line.LineNumber + 1}.\nContent: {line}");
                }
            }

            var semanticModel = await outputDocument.GetSemanticModelAsync();
            var result = new GenerationResult(outputDocument, semanticModel, generatorDiagnostics, compilationDiagnostics);

            foreach (var diagnostic in generatorDiagnostics)
            {
                this.logger.WriteLine(diagnostic.ToString());
            }

            foreach (var diagnostic in result.CompilationDiagnostics)
            {
                this.logger.WriteLine(diagnostic.ToString());
            }

            return result;
        }

        private static string EscapeLineEndingCharacters(string whitespace)
        {
            Requires.NotNull(whitespace, nameof(whitespace));
            var builder = new StringBuilder(whitespace.Length * 2);
            foreach (char ch in whitespace)
            {
                switch (ch)
                {
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
            }

            return builder.ToString();
        }

        private static IEnumerable<MetadataReference> GetReferences(string profile)
        {
            string profileDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Reference Assemblies\Microsoft\Framework\.NETPortable\v4.5\Profile", profile);
            foreach (string assembly in Directory.GetFiles(profileDirectory, "*.dll"))
            {
                yield return MetadataReference.CreateFromFile(assembly);
            }
        }

        protected class GenerationResult
        {
            public GenerationResult(
                Document document,
                SemanticModel semanticModel,
                IReadOnlyList<Diagnostic> generatorDiagnostics,
                IReadOnlyList<Diagnostic> compilationDiagnostics)
            {
                this.Document = document;
                this.SemanticModel = semanticModel;
                this.Declarations = CSharpDeclarationComputer.GetDeclarationsInSpan(semanticModel, TextSpan.FromBounds(0, semanticModel.SyntaxTree.Length), true, CancellationToken.None);
                this.GeneratorDiagnostics = generatorDiagnostics;
                this.CompilationDiagnostics = compilationDiagnostics;
            }

            public Document Document { get; private set; }

            public SemanticModel SemanticModel { get; private set; }

            internal ImmutableArray<DeclarationInfo> Declarations { get; private set; }

            public IEnumerable<ISymbol> DeclaredSymbols
            {
                get { return this.Declarations.Select(d => d.DeclaredSymbol); }
            }

            public IEnumerable<IMethodSymbol> DeclaredMethods
            {
                get { return this.DeclaredSymbols.OfType<IMethodSymbol>(); }
            }

            public IEnumerable<IPropertySymbol> DeclaredProperties
            {
                get { return this.DeclaredSymbols.OfType<IPropertySymbol>(); }
            }

            public IEnumerable<INamedTypeSymbol> DeclaredTypes
            {
                get { return this.DeclaredSymbols.OfType<INamedTypeSymbol>(); }
            }

            public IReadOnlyList<Diagnostic> GeneratorDiagnostics { get; }

            public IReadOnlyList<Diagnostic> CompilationDiagnostics { get; }
        }

        private class SynchronousProgress<T> : IProgress<T>
        {
            private readonly Action<T> action;

            public SynchronousProgress(Action<T> action)
            {
                Requires.NotNull(action, nameof(action));

                this.action = action;
            }

            public void Report(T value)
            {
                this.action(value);
            }
        }
    }
}
