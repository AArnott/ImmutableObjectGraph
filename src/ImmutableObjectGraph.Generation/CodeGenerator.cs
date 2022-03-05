namespace ImmutableObjectGraph.Generation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Validation;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    [Generator]
    public class CodeGenerator : IIncrementalGenerator
    {
        private const string GenerateImmutableAttributeMetadataName = "ImmutableObjectGraph.Generation.GenerateImmutableAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classes = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, token) => node is ClassDeclarationSyntax @class && @class.AttributeLists.Count > 0,
                transform: (ctx, token) => GetSemanticTargetForGeneration(ctx, token))
                .Where(c => c != null);

            var compilationAndClasses = context.CompilationProvider.Combine(classes.Collect());

            context.RegisterSourceOutput(compilationAndClasses, (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static ClassDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken token)
        {
            var classSyntax = (ClassDeclarationSyntax)context.Node;

            foreach (var attributeListSyntax in classSyntax.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    var attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax, token).Symbol as IMethodSymbol;
                    if (attributeSymbol is null)
                        continue;

                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName == GenerateImmutableAttributeMetadataName)
                        return classSyntax;
                }
            }

            return null;
        }

        private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
                return;

            var attributeSymbol = compilation.GetTypeByMetadataName(GenerateImmutableAttributeMetadataName)
                ?? throw new InvalidOperationException("Symbol not found: " + GenerateImmutableAttributeMetadataName);

            foreach (var syntax in classes.Distinct())
            {
                var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
                var classSymbol = semanticModel.GetDeclaredSymbol(syntax);
                var attributeData = classSymbol?.GetAttributes().FirstOrDefault(d => d.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);
                var data = attributeData.NamedArguments.ToImmutableDictionary(kv => kv.Key, kv => kv.Value);
                var options = new CodeGen.Options(attributeData)
                {
                    GenerateBuilder = GetBoolData(data, nameof(GenerateImmutableAttribute.GenerateBuilder)),
                    Delta = GetBoolData(data, nameof(GenerateImmutableAttribute.Delta)),
                    DefineInterface = GetBoolData(data, nameof(GenerateImmutableAttribute.DefineInterface)),
                    DefineRootedStruct = GetBoolData(data, nameof(GenerateImmutableAttribute.DefineRootedStruct)),
                    DefineWithMethodsPerProperty = GetBoolData(data, nameof(GenerateImmutableAttribute.DefineWithMethodsPerProperty)),
                };

                var compilationUnit = CreateSource(context, syntax, semanticModel, classSymbol, options);
                var sourceText = SyntaxTree(compilationUnit, encoding: Encoding.UTF8).GetText();
                context.AddSource($"{syntax.Identifier}.g.cs", sourceText);
            }
        }

        private static bool GetBoolData(ImmutableDictionary<string, TypedConstant> data, string name)
        {
            return (bool)(data.GetValueOrDefault(name).Value ?? false);
        }

        private static CompilationUnitSyntax CreateSource(SourceProductionContext context, ClassDeclarationSyntax declarationSyntax, SemanticModel semanticModel, INamedTypeSymbol typeSymbol, CodeGen.Options options)
        {
            var root = declarationSyntax.SyntaxTree.GetCompilationUnitRoot();
            var declaredUsings = root.Usings;
            foreach (var ns in root.Members.OfType<NamespaceDeclarationSyntax>())
                declaredUsings = declaredUsings.AddRange(ns.Usings);

            var usings = List(declaredUsings);

            var generatedMembers = CodeGen.GenerateAsync(declarationSyntax, semanticModel, progress: new Progress(context), options, context.CancellationToken).Result;

            var containingType = typeSymbol;
            while (containingType.ContainingType != null)
            {
                containingType = containingType.ContainingType;

                generatedMembers = SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration(containingType.Name)
                    .WithModifiers(TokenList(Token(SyntaxKind.PartialKeyword)))
                    .WithMembers(generatedMembers));
            }

            if (!containingType.ContainingNamespace.IsGlobalNamespace)
            {
                generatedMembers = SingletonList<MemberDeclarationSyntax>(
                    NamespaceDeclaration(
                        ParseName(typeSymbol.ContainingNamespace.ToDisplayString()))
                    .WithMembers(generatedMembers));
            }

            return
               CompilationUnit()
               .WithUsings(usings)
               .WithMembers(generatedMembers)
               .NormalizeWhitespace();
        }

        private class Progress : IProgress<Diagnostic>
        {
            private readonly SourceProductionContext context;

            public Progress(SourceProductionContext context)
            {
                this.context = context;
            }

            public void Report(Diagnostic value)
            {
                context.ReportDiagnostic(value);
            }
        }
    }
}
