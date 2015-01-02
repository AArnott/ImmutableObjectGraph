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
        private static readonly TypeSyntax IdentityFieldTypeSyntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword));
        private static readonly TypeSyntax IdentityFieldOptionalTypeSyntax = SyntaxFactory.GenericName(SyntaxFactory.Identifier(nameof(Optional)), SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(IdentityFieldTypeSyntax)));
        private static readonly IdentifierNameSyntax IdentityParameterName = SyntaxFactory.IdentifierName("identity");
        private static readonly IdentifierNameSyntax IdentityPropertyName = SyntaxFactory.IdentifierName("Identity");
        private static readonly ParameterSyntax RequiredIdentityParameter = SyntaxFactory.Parameter(IdentityParameterName.Identifier).WithType(IdentityFieldTypeSyntax);
        private static readonly ParameterSyntax OptionalIdentityParameter = Syntax.Optional(RequiredIdentityParameter);
        private static readonly ArgumentSyntax OptionalIdentityArgument = SyntaxFactory.Argument(SyntaxFactory.NameColon(IdentityParameterName), SyntaxFactory.Token(SyntaxKind.None), IdentityParameterName);
        private static readonly ArgumentSyntax RequiredIdentityArgumentFromProperty = SyntaxFactory.Argument(SyntaxFactory.NameColon(IdentityParameterName), SyntaxFactory.Token(SyntaxKind.None), Syntax.ThisDot(IdentityPropertyName));
        private static readonly IdentifierNameSyntax DefaultInstanceFieldName = SyntaxFactory.IdentifierName("DefaultInstance");
        private static readonly IdentifierNameSyntax GetDefaultTemplateMethodName = SyntaxFactory.IdentifierName("GetDefaultTemplate");
        private static readonly IdentifierNameSyntax varType = SyntaxFactory.IdentifierName("var");
        private static readonly IdentifierNameSyntax NestedTemplateTypeName = SyntaxFactory.IdentifierName("Template");
        private static readonly IdentifierNameSyntax CreateDefaultTemplateMethodName = SyntaxFactory.IdentifierName("CreateDefaultTemplate");
        private static readonly IdentifierNameSyntax CreateMethodName = SyntaxFactory.IdentifierName("Create");
        private static readonly IdentifierNameSyntax NewIdentityMethodName = SyntaxFactory.IdentifierName("NewIdentity");
        private static readonly IdentifierNameSyntax WithFactoryMethodName = SyntaxFactory.IdentifierName("WithFactory");
        private static readonly IdentifierNameSyntax WithMethodName = SyntaxFactory.IdentifierName("With");
        private static readonly IdentifierNameSyntax WithCoreMethodName = SyntaxFactory.IdentifierName("WithCore");
        private static readonly IdentifierNameSyntax LastIdentityProducedFieldName = SyntaxFactory.IdentifierName("lastIdentityProduced");
        private static readonly IdentifierNameSyntax ValidateMethodName = SyntaxFactory.IdentifierName("Validate");
        private static readonly IdentifierNameSyntax SkipValidationParameterName = SyntaxFactory.IdentifierName("skipValidation");
        private static readonly AttributeSyntax DebuggerBrowsableNeverAttribute = SyntaxFactory.Attribute(
            SyntaxFactory.ParseName(typeof(DebuggerBrowsableAttribute).FullName),
            SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.AttributeArgument(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ParseName(typeof(DebuggerBrowsableState).FullName),
                    SyntaxFactory.IdentifierName(nameof(DebuggerBrowsableState.Never)))))));
        private static readonly ThrowStatementSyntax ThrowNotImplementedException = SyntaxFactory.ThrowStatement(
            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(typeof(NotImplementedException).FullName), SyntaxFactory.ArgumentList(), null));
        private static readonly ArgumentSyntax DoNotSkipValidationArgument = SyntaxFactory.Argument(SyntaxFactory.NameColon(SkipValidationParameterName), SyntaxFactory.Token(SyntaxKind.None), SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));

        private readonly ClassDeclarationSyntax applyTo;
        private readonly Document document;
        private readonly IProgressAndErrors progress;
        private readonly Options options;
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// The members injected into the primary generated type.
        /// </summary>
        private readonly List<MemberDeclarationSyntax> innerMembers = new List<MemberDeclarationSyntax>();

        /// <summary>
        /// The members injected into the generated document, including the primary generated type.
        /// </summary>
        private readonly List<MemberDeclarationSyntax> outerMembers = new List<MemberDeclarationSyntax>();

        /// <summary>
        /// Statements to append to the instance constructor.
        /// </summary>
        private readonly List<StatementSyntax> additionalCtorStatements = new List<StatementSyntax>();

        private SemanticModel semanticModel;
        private INamedTypeSymbol applyToSymbol;
        private ImmutableArray<DeclarationInfo> inputDeclarations;
        private MetaType applyToMetaType;
        private bool isAbstract;
        private TypeSyntax applyToTypeName;
        private List<FeatureGenerator> mergedFeatures = new List<FeatureGenerator>();

        private CodeGen(ClassDeclarationSyntax applyTo, Document document, IProgressAndErrors progress, Options options, CancellationToken cancellationToken)
        {
            this.applyTo = applyTo;
            this.document = document;
            this.progress = progress;
            this.options = options ?? new Options();
            this.cancellationToken = cancellationToken;

            this.PluralService = PluralizationService.CreateService(CultureInfo.CurrentCulture);
        }

        public PluralizationService PluralService { get; set; }

        public static async Task<IReadOnlyList<MemberDeclarationSyntax>> GenerateAsync(ClassDeclarationSyntax applyTo, Document document, IProgressAndErrors progress, Options options, CancellationToken cancellationToken)
        {
            Requires.NotNull(applyTo, "applyTo");
            Requires.NotNull(document, "document");
            Requires.NotNull(progress, "progress");

            var instance = new CodeGen(applyTo, document, progress, options, cancellationToken);
            return await instance.GenerateAsync();
        }

        private void MergeFeature(FeatureGenerator featureGenerator)
        {
            if (featureGenerator.IsApplicable)
            {
                var featureResults = featureGenerator.Generate();
                this.innerMembers.AddRange(featureResults.MembersOfGeneratedType);
                this.outerMembers.AddRange(featureResults.SiblingsOfGeneratedType);

                if (!featureResults.AdditionalCtorStatements.IsDefault)
                {
                    this.additionalCtorStatements.AddRange(featureResults.AdditionalCtorStatements);
                }

                this.mergedFeatures.Add(featureGenerator);
            }
        }

        private async Task<IReadOnlyList<MemberDeclarationSyntax>> GenerateAsync()
        {
            this.semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            this.isAbstract = applyTo.Modifiers.Any(m => m.IsContextualKind(SyntaxKind.AbstractKeyword));
            this.applyToTypeName = SyntaxFactory.IdentifierName(this.applyTo.Identifier);

            this.inputDeclarations = this.semanticModel.GetDeclarationsInSpan(TextSpan.FromBounds(0, this.semanticModel.SyntaxTree.Length), true, this.cancellationToken);
            this.applyToSymbol = this.semanticModel.GetDeclaredSymbol(this.applyTo, this.cancellationToken);
            this.applyToMetaType = new MetaType(this, this.applyToSymbol);

            ValidateInput();

            this.MergeFeature(new EnumerableRecursiveParentGen(this));
            this.MergeFeature(new RecursiveTypeGen(this));

            if (!this.applyToMetaType.HasAncestor)
            {
                this.innerMembers.Add(CreateLastIdentityProducedField());
                this.innerMembers.Add(CreateIdentityField());
                this.innerMembers.Add(CreateIdentityProperty());
                this.innerMembers.Add(CreateNewIdentityMethod());
            }

            this.innerMembers.AddRange(CreateWithCoreMethods());

            if (!isAbstract)
            {
                this.innerMembers.Add(CreateCreateMethod());
                if (this.applyToMetaType.AllFields.Any())
                {
                    this.innerMembers.Add(CreateWithFactoryMethod());
                }

                this.innerMembers.Add(CreateDefaultInstanceField());
                this.innerMembers.Add(CreateGetDefaultTemplateMethod());
                this.innerMembers.Add(CreateCreateDefaultTemplatePartialMethod());
                this.innerMembers.Add(CreateTemplateStruct());
                this.innerMembers.Add(CreateValidateMethod());
            }

            if (this.applyToMetaType.AllFields.Any())
            {
                this.innerMembers.Add(CreateWithMethod());
            }

            this.innerMembers.AddRange(this.GetFieldVariables().Select(fv => CreatePropertyForField(fv.Key, fv.Value)));

            this.MergeFeature(new BuilderGen(this));
            this.MergeFeature(new RootedStructGen(this));
            this.MergeFeature(new DeltaGen(this));
            this.MergeFeature(new InterfacesGen(this));
            this.MergeFeature(new DefineWithMethodsPerPropertyGen(this));
            this.MergeFeature(new CollectionHelpersGen(this));
            this.MergeFeature(new TypeConversionGen(this));
            this.MergeFeature(new FastSpineGen(this));
            this.MergeFeature(new DeepMutationGen(this));

            // Define the constructor after merging all features since they can add to it.
            this.innerMembers.Add(CreateCtor());

            // Sort the members now that they're all added.
            this.innerMembers.Sort(StyleCop.Sort);

            var partialClass = SyntaxFactory.ClassDeclaration(applyTo.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                .WithMembers(SyntaxFactory.List(this.innerMembers));

            partialClass = this.mergedFeatures.Aggregate(partialClass, (acc, feature) => feature.ProcessApplyToClassDeclaration(acc));
            this.outerMembers.Add(partialClass);

            return this.outerMembers;
        }

        protected struct GenerationResult
        {
            public SyntaxList<MemberDeclarationSyntax> MembersOfGeneratedType { get; set; }

            public SyntaxList<MemberDeclarationSyntax> SiblingsOfGeneratedType { get; set; }

            public ImmutableArray<StatementSyntax> AdditionalCtorStatements { get; set; }
        }

        private static PropertyDeclarationSyntax CreatePropertyForField(FieldDeclarationSyntax field, VariableDeclaratorSyntax variable)
        {
            var xmldocComment = field.GetLeadingTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));

            var property = SyntaxFactory.PropertyDeclaration(field.Declaration.Type, variable.Identifier.ValueText.ToPascalCase())
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAccessorList(
                    SyntaxFactory.AccessorList(SyntaxFactory.List(new AccessorDeclarationSyntax[] {
                                SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration,
                                    SyntaxFactory.Block(
                                        SyntaxFactory.ReturnStatement(
                                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(), SyntaxFactory.IdentifierName(variable.Identifier))
                                        ))) })))
                .WithLeadingTrivia(xmldocComment); // TODO: modify the <summary> to translate "Some description" to "Gets some description."
            return property;
        }

        private void ValidateInput()
        {
            foreach (var field in this.GetFields())
            {
                if (!field.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword)))
                {
                    var location = field.GetLocation().GetLineSpan().StartLinePosition;
                    progress.Warning(
                        string.Format(CultureInfo.CurrentCulture, "Field '{0}' should be marked readonly.", field.Declaration.Variables.First().Identifier),
                        (uint)location.Line,
                        (uint)location.Character);
                }
            }
        }

        private IEnumerable<INamedTypeSymbol> TypesInInputDocument
        {
            get
            {
                return from declaration in this.inputDeclarations
                       let typeSymbol = declaration.DeclaredSymbol as INamedTypeSymbol
                       where typeSymbol != null
                       select typeSymbol;
            }
        }

        private MemberDeclarationSyntax CreateDefaultInstanceField()
        {
            // [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            // private static readonly <#= templateType.TypeName #> DefaultInstance = GetDefaultTemplate();
            var field = SyntaxFactory.FieldDeclaration(
                 SyntaxFactory.VariableDeclaration(
                     SyntaxFactory.IdentifierName(this.applyTo.Identifier.ValueText),
                     SyntaxFactory.SingletonSeparatedList(
                         SyntaxFactory.VariableDeclarator(DefaultInstanceFieldName.Identifier)
                             .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.InvocationExpression(GetDefaultTemplateMethodName, SyntaxFactory.ArgumentList()))))))
                 .WithModifiers(SyntaxFactory.TokenList(
                     SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                     SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                     SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
                 .WithAttributeLists(SyntaxFactory.SingletonList(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                     DebuggerBrowsableNeverAttribute))));
            return field;
        }

        private MemberDeclarationSyntax CreateCreateDefaultTemplatePartialMethod()
        {
            // /// <summary>Provides defaults for fields.</summary>
            // /// <param name="template">The struct to set default values on.</param>
            // static partial void CreateDefaultTemplate(ref Template template);
            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                CreateDefaultTemplateMethodName.Identifier)
                .WithParameterList(SyntaxFactory.ParameterList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("template"))
                            .WithType(NestedTemplateTypeName)
                            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword))))))
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                    SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private MemberDeclarationSyntax CreateGetDefaultTemplateMethod()
        {
            IdentifierNameSyntax templateVarName = SyntaxFactory.IdentifierName("template");
            var body = SyntaxFactory.Block(
                // var template = new Template();
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        varType,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                templateVarName.Identifier,
                                null,
                                SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(NestedTemplateTypeName, SyntaxFactory.ArgumentList(), null)))))),
                // CreateDefaultTemplate(ref template);
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        CreateDefaultTemplateMethodName,
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.RefKeyword), templateVarName))))),
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.IdentifierName(applyTo.Identifier),
                        SyntaxFactory.ArgumentList(Syntax.JoinSyntaxNodes(
                            SyntaxKind.CommaToken,
                            ImmutableArray.Create(SyntaxFactory.Argument(SyntaxFactory.DefaultExpression(IdentityFieldTypeSyntax)))
                                .AddRange(this.applyToMetaType.AllFields.Select(f => SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, templateVarName, SyntaxFactory.IdentifierName(f.Name.ToPascalCase())))))
                                .Add(SyntaxFactory.Argument(SyntaxFactory.NameColon(SkipValidationParameterName), SyntaxFactory.Token(SyntaxKind.None), SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))))),
                        null)));

            return SyntaxFactory.MethodDeclaration(SyntaxFactory.IdentifierName(applyTo.Identifier.ValueText), GetDefaultTemplateMethodName.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(
                     SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                     SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithBody(body);
        }

        private MemberDeclarationSyntax CreateTemplateStruct()
        {
            return SyntaxFactory.StructDeclaration(NestedTemplateTypeName.Identifier)
                .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(
                    this.applyToMetaType.AllFields.Select(f =>
                        SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(
                            GetFullyQualifiedSymbolName(f.Type),
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator(f.Name.ToPascalCase()))))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword)))))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                .WithLeadingTrivia(
                    SyntaxFactory.ElasticCarriageReturnLineFeed,
                    SyntaxFactory.Trivia(
                        SyntaxFactory.PragmaWarningDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.DisableKeyword), true)
                        .WithErrorCodes(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0649)
                                .WithTrailingTrivia(SyntaxFactory.Space, SyntaxFactory.Comment("// field initialization is optional in user code")))))
                        .WithEndOfDirectiveToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.EndOfDirectiveToken, SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed)))))
                .WithTrailingTrivia(
                    SyntaxFactory.Trivia(
                        SyntaxFactory.PragmaWarningDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.RestoreKeyword), true)
                        .WithErrorCodes(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0649))))
                        .WithEndOfDirectiveToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.EndOfDirectiveToken, SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed)))),
                    SyntaxFactory.ElasticCarriageReturnLineFeed);
        }

        private MemberDeclarationSyntax CreateCtor()
        {
            BlockSyntax body = SyntaxFactory.Block(
                // this.someField = someField;
                this.GetFieldVariables().Select(f => SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        Syntax.ThisDot(SyntaxFactory.IdentifierName(f.Value.Identifier)),
                        SyntaxFactory.IdentifierName(f.Value.Identifier)))));

            if (!this.applyToMetaType.HasAncestor)
            {
                body = body.WithStatements(
                    body.Statements.Insert(0,
                        // this.identity = identity;
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                Syntax.ThisDot(IdentityParameterName),
                                IdentityParameterName))));
            }

            if (!this.isAbstract)
            {
                body = body.AddStatements(
                    // if (!skipValidation)
                    SyntaxFactory.IfStatement(
                        SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, SkipValidationParameterName),
                        // this.Validate();
                        SyntaxFactory.Block(
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.InvocationExpression(
                                    Syntax.ThisDot(ValidateMethodName),
                                    SyntaxFactory.ArgumentList())))));
            }

            body = body.AddStatements(this.additionalCtorStatements.ToArray());

            var ctor = SyntaxFactory.ConstructorDeclaration(
                this.applyTo.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)))
                .WithParameterList(
                    CreateParameterList(this.applyToMetaType.AllFields, ParameterStyle.Required)
                    .PrependParameter(RequiredIdentityParameter)
                    .AddParameters(SyntaxFactory.Parameter(SkipValidationParameterName.Identifier).WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)))))
                .WithBody(body);

            if (this.applyToMetaType.HasAncestor)
            {
                ctor = ctor.WithInitializer(
                    SyntaxFactory.ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        this.CreateArgumentList(this.applyToMetaType.InheritedFields, ArgSource.Argument)
                            .PrependArgument(SyntaxFactory.Argument(SyntaxFactory.NameColon(IdentityParameterName), SyntaxFactory.Token(SyntaxKind.None), IdentityParameterName))
                            .AddArguments(SyntaxFactory.Argument(SyntaxFactory.NameColon(SkipValidationParameterName), SyntaxFactory.Token(SyntaxKind.None), SkipValidationParameterName))));
            }

            return ctor;
        }

        private MethodDeclarationSyntax CreateWithMethod()
        {
            var method = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName(this.applyTo.Identifier),
                WithMethodName.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithParameterList(CreateParameterList(this.applyToMetaType.AllFields, ParameterStyle.Optional))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.CastExpression(
                            SyntaxFactory.IdentifierName(this.applyTo.Identifier),
                            SyntaxFactory.InvocationExpression(
                                Syntax.ThisDot(WithCoreMethodName),
                                this.CreateArgumentList(this.applyToMetaType.AllFields, ArgSource.Argument))))));

            if (!this.applyToMetaType.LocalFields.Any())
            {
                method = Syntax.AddNewKeyword(method);
            }

            return method;
        }

        private IEnumerable<MethodDeclarationSyntax> CreateWithCoreMethods()
        {
            if (this.applyToMetaType.LocalFields.Any())
            {
                var method = SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.IdentifierName(this.applyTo.Identifier),
                    WithCoreMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
                    .WithParameterList(this.CreateParameterList(this.applyToMetaType.AllFields, ParameterStyle.Optional));
                if (this.isAbstract)
                {
                    method = method
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.AbstractKeyword))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                }
                else
                {
                    method = method
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
                        .WithBody(SyntaxFactory.Block(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.InvocationExpression(
                                    Syntax.ThisDot(WithFactoryMethodName),
                                    this.CreateArgumentList(this.applyToMetaType.AllFields, ArgSource.OptionalArgumentOrProperty, OptionalStyle.Always)
                                    .AddArguments(SyntaxFactory.Argument(SyntaxFactory.NameColon(IdentityParameterName), SyntaxFactory.Token(SyntaxKind.None), Syntax.OptionalFor(Syntax.ThisDot(IdentityPropertyName))))))));
                }

                yield return method;
            }

            if (!this.applyToSymbol.IsAbstract)
            {
                foreach (var ancestor in this.applyToMetaType.Ancestors.Where(a => a.LocalFields.Any()))
                {
                    var overrideMethod = SyntaxFactory.MethodDeclaration(
                        SyntaxFactory.IdentifierName(ancestor.TypeSymbol.Name),
                        WithCoreMethodName.Identifier)
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                            SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
                        .WithParameterList(this.CreateParameterList(ancestor.AllFields, ParameterStyle.Optional))
                        .WithBody(SyntaxFactory.Block(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.InvocationExpression(
                                    Syntax.ThisDot(WithFactoryMethodName),
                                    this.CreateArgumentList(ancestor.AllFields, ArgSource.Argument)))));
                    yield return overrideMethod;
                }
            }
        }

        private MemberDeclarationSyntax CreateWithFactoryMethod()
        {
            // (field.IsDefined && field.Value != this.field)
            Func<MetaField, ExpressionSyntax> isChanged = v =>
                SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.LogicalAndExpression,
                        Syntax.OptionalIsDefined(v.NameAsField),
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.NotEqualsExpression,
                            Syntax.OptionalValue(v.NameAsField),
                            Syntax.ThisDot(SyntaxFactory.IdentifierName(v.Name.ToPascalCase())))));
            var anyChangesExpression = this.applyToMetaType.AllFields.Select(isChanged).ChainBinaryExpressions(SyntaxKind.LogicalOrExpression);

            // /// <summary>Returns a new instance of this object with any number of properties changed.</summary>
            // private TemplateType WithFactory(...)
            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName(this.applyTo.Identifier),
                WithFactoryMethodName.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                .WithParameterList(CreateParameterList(this.applyToMetaType.AllFields, ParameterStyle.Optional).AddParameters(OptionalIdentityParameter))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.IfStatement(
                        anyChangesExpression,
                        SyntaxFactory.Block(
                            // return new TemplateType(...)
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.IdentifierName(applyTo.Identifier),
                                    CreateArgumentList(this.applyToMetaType.AllFields, ArgSource.OptionalArgumentOrProperty)
                                        .PrependArgument(SyntaxFactory.Argument(SyntaxFactory.NameColon(IdentityParameterName), SyntaxFactory.Token(SyntaxKind.None), Syntax.OptionalGetValueOrDefault(SyntaxFactory.IdentifierName(IdentityParameterName.Identifier), Syntax.ThisDot(IdentityPropertyName))))
                                        .AddArguments(DoNotSkipValidationArgument),
                                    null))),
                        SyntaxFactory.ElseClause(SyntaxFactory.Block(
                            SyntaxFactory.ReturnStatement(SyntaxFactory.ThisExpression()))))));
        }

        private MemberDeclarationSyntax CreateCreateMethod()
        {
            var fields = this.GetFields();
            var body = SyntaxFactory.Block();
            if (fields.Any())
            {
                body = body.AddStatements(
                    // var identity = Optional.For(NewIdentity());
                    SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(
                        varType,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(IdentityParameterName.Identifier)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(Syntax.OptionalFor(SyntaxFactory.InvocationExpression(NewIdentityMethodName, SyntaxFactory.ArgumentList()))))))),
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                DefaultInstanceFieldName,
                                WithFactoryMethodName),
                            CreateArgumentList(this.applyToMetaType.AllFields, ArgSource.OptionalArgumentOrTemplate, asOptional: OptionalStyle.Always)
                                .AddArguments(SyntaxFactory.Argument(SyntaxFactory.NameColon(IdentityParameterName), SyntaxFactory.Token(SyntaxKind.None), IdentityParameterName)))));
            }
            else
            {
                body = body.AddStatements(
                    SyntaxFactory.ReturnStatement(DefaultInstanceFieldName));
            }

            var method = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName(applyTo.Identifier),
                CreateMethodName.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithParameterList(CreateParameterList(this.applyToMetaType.AllFields, ParameterStyle.OptionalOrRequired))
                .WithBody(body);

            if (this.applyToMetaType.Ancestors.Any(a => !a.TypeSymbol.IsAbstract && a.AllFields.Count() == this.applyToMetaType.AllFields.Count()))
            {
                method = Syntax.AddNewKeyword(method);
            }

            return method;
        }

        private MethodDeclarationSyntax CreateValidateMethod()
        {
            //// /// <summary>Normalizes and/or validates all properties on this object.</summary>
            //// /// <exception type="ArgumentException">Thrown if any properties have disallowed values.</exception>
            //// partial void Validate();
            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                ValidateMethodName.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private static MemberDeclarationSyntax CreateLastIdentityProducedField()
        {
            // /// <summary>The last identity assigned to a created instance.</summary>
            // private static int lastIdentityProduced;
            return SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(LastIdentityProducedFieldName.Identifier))))
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        }

        private static MemberDeclarationSyntax CreateIdentityField()
        {
            return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(
                    IdentityFieldTypeSyntax,
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(IdentityParameterName.Identifier))))
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
        }

        private static MemberDeclarationSyntax CreateIdentityProperty()
        {
            return SyntaxFactory.PropertyDeclaration(
                IdentityFieldTypeSyntax,
                IdentityPropertyName.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.InternalKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        SyntaxFactory.Block(SyntaxFactory.ReturnStatement(Syntax.ThisDot(IdentityParameterName)))));
        }

        private static MemberDeclarationSyntax CreateNewIdentityMethod()
        {
            // protected static <#= templateType.RequiredIdentityField.TypeName #> NewIdentity() {
            //     return (<#= templateType.RequiredIdentityField.TypeName #>)System.Threading.Interlocked.Increment(ref lastIdentityProduced);
            // }
            return SyntaxFactory.MethodDeclaration(
                IdentityFieldTypeSyntax,
                NewIdentityMethodName.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.CastExpression(
                            IdentityFieldTypeSyntax,
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.ParseName("System.Threading.Interlocked.Increment"),
                                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(
                                    null,
                                    SyntaxFactory.Token(SyntaxKind.RefKeyword),
                                    LastIdentityProducedFieldName))))))));
        }

        private static IEnumerable<MetaField> SortRequiredFieldsFirst(IEnumerable<MetaField> fields)
        {
            return fields.Where(f => f.IsRequired).Concat(fields.Where(f => !f.IsRequired));
        }

        private IEnumerable<FieldDeclarationSyntax> GetFields()
        {
            return this.applyTo.ChildNodes().OfType<FieldDeclarationSyntax>();
        }

        private IEnumerable<KeyValuePair<FieldDeclarationSyntax, VariableDeclaratorSyntax>> GetFieldVariables()
        {
            foreach (var field in this.GetFields())
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    yield return new KeyValuePair<FieldDeclarationSyntax, VariableDeclaratorSyntax>(field, variable);
                }
            }
        }

        private ParameterListSyntax CreateParameterList(IEnumerable<MetaField> fields, ParameterStyle style)
        {
            if (style == ParameterStyle.OptionalOrRequired)
            {
                fields = SortRequiredFieldsFirst(fields);
            }

            Func<MetaField, bool> isOptional = f => style == ParameterStyle.Optional || (style == ParameterStyle.OptionalOrRequired && !f.IsRequired);
            Func<ParameterSyntax, MetaField, ParameterSyntax> setTypeAndDefault = (p, f) => isOptional(f)
                ? Syntax.Optional(p.WithType(GetFullyQualifiedSymbolName(f.Type)))
                : p.WithType(GetFullyQualifiedSymbolName(f.Type));
            return SyntaxFactory.ParameterList(
                Syntax.JoinSyntaxNodes(
                    SyntaxKind.CommaToken,
                    fields.Select(f => setTypeAndDefault(SyntaxFactory.Parameter(SyntaxFactory.Identifier(f.Name)), f))));
        }

        private ArgumentListSyntax CreateArgumentList(IEnumerable<MetaField> fields, ArgSource source = ArgSource.Property, OptionalStyle asOptional = OptionalStyle.None)
        {
            Func<MetaField, ArgSource> fieldSource = f => (source == ArgSource.OptionalArgumentOrTemplate && f.IsRequired) ? ArgSource.Argument : source;
            Func<MetaField, bool> optionalWrap = f => asOptional != OptionalStyle.None && (asOptional == OptionalStyle.Always || !f.IsRequired);
            Func<MetaField, ExpressionSyntax> dereference = f =>
            {
                var name = SyntaxFactory.IdentifierName(f.Name);
                var propertyName = SyntaxFactory.IdentifierName(f.Name.ToPascalCase());
                switch (fieldSource(f))
                {
                    case ArgSource.Property:
                        return Syntax.ThisDot(propertyName);
                    case ArgSource.Argument:
                        return name;
                    case ArgSource.OptionalArgumentOrProperty:
                        return Syntax.OptionalGetValueOrDefault(name, Syntax.ThisDot(propertyName));
                    case ArgSource.OptionalArgumentOrTemplate:
                        return Syntax.OptionalGetValueOrDefault(name, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, DefaultInstanceFieldName, propertyName));
                    case ArgSource.Missing:
                        return SyntaxFactory.DefaultExpression(Syntax.OptionalOf(GetFullyQualifiedSymbolName(f.Type)));
                    default:
                        throw Assumes.NotReachable();
                }
            };

            return SyntaxFactory.ArgumentList(Syntax.JoinSyntaxNodes(
                SyntaxKind.CommaToken,
                fields.Select(f =>
                    SyntaxFactory.Argument(
                        SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(f.Name)),
                        SyntaxFactory.Token(SyntaxKind.None),
                        Syntax.OptionalForIf(dereference(f), optionalWrap(f))))));
        }

        private static bool IsFieldRequired(IFieldSymbol fieldSymbol)
        {
            return IsAttributeApplied<RequiredAttribute>(fieldSymbol);
        }

        private static bool IsAttributeApplied<T>(ISymbol symbol) where T : Attribute
        {
            return symbol?.GetAttributes().Any(a => IsOrDerivesFrom<T>(a.AttributeClass)) ?? false;
        }

        private static bool IsOrDerivesFrom<T>(INamedTypeSymbol type)
        {
            if (type != null)
            {
                if (type.Name == typeof(T).Name)
                {
                    // Don't sweat accuracy too much at this point.
                    return true;
                }

                return IsOrDerivesFrom<T>(type.BaseType);
            }

            return false;
        }

        private static bool IsAttribute<T>(INamedTypeSymbol type)
        {
            if (type != null)
            {
                if (type.Name == typeof(T).Name)
                {
                    // Don't sweat accuracy too much at this point.
                    return true;
                }

                return IsAttribute<T>(type.BaseType);
            }

            return false;
        }

        private static bool HasAttribute<T>(INamedTypeSymbol type)
        {
            return type?.GetAttributes().Any(a => IsAttribute<T>(a.AttributeClass)) ?? false;
        }

        private static NameSyntax GetFullyQualifiedSymbolName(INamespaceOrTypeSymbol symbol)
        {
            if (symbol == null || string.IsNullOrEmpty(symbol.Name))
            {
                return null;
            }

            var parent = GetFullyQualifiedSymbolName(symbol.ContainingSymbol as INamespaceOrTypeSymbol);
            SimpleNameSyntax leafName = SyntaxFactory.IdentifierName(symbol.Name);
            var typeSymbol = symbol as INamedTypeSymbol;
            if (typeSymbol != null && typeSymbol.IsGenericType)
            {
                leafName = SyntaxFactory.GenericName(symbol.Name)
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(Syntax.JoinSyntaxNodes<TypeSyntax>(
                        SyntaxKind.CommaToken,
                        typeSymbol.TypeArguments.Select(GetFullyQualifiedSymbolName))));
            }

            return parent != null
                ? (NameSyntax)SyntaxFactory.QualifiedName(parent, leafName)
                : leafName;
        }

        private static SyntaxToken[] GetModifiersForAccessibility(INamedTypeSymbol template)
        {
            switch (template.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) };
                case Accessibility.Protected:
                    return new[] { SyntaxFactory.Token(SyntaxKind.ProtectedKeyword) };
                case Accessibility.Internal:
                    return new[] { SyntaxFactory.Token(SyntaxKind.InternalKeyword) };
                case Accessibility.ProtectedOrInternal:
                    return new[] { SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.InternalKeyword) };
                case Accessibility.Private:
                    return new[] { SyntaxFactory.Token(SyntaxKind.PrivateKeyword) };
                default:
                    throw new NotSupportedException();
            }
        }

        public class Options
        {
            public Options() { }

            public bool GenerateBuilder { get; set; }

            public bool Delta { get; set; }

            public bool DefineInterface { get; set; }

            public bool DefineRootedStruct { get; set; }

            public bool DefineWithMethodsPerProperty { get; set; }
        }

        protected abstract class FeatureGenerator
        {
            protected readonly CodeGen generator;
            protected readonly List<MemberDeclarationSyntax> innerMembers = new List<MemberDeclarationSyntax>();
            protected readonly List<MemberDeclarationSyntax> siblingMembers = new List<MemberDeclarationSyntax>();
            protected readonly List<BaseTypeSyntax> baseTypes = new List<BaseTypeSyntax>();
            protected readonly List<StatementSyntax> additionalCtorStatements = new List<StatementSyntax>();
            protected readonly MetaType applyTo;

            protected FeatureGenerator(CodeGen generator)
            {
                this.generator = generator;
                this.applyTo = generator.applyToMetaType;
            }

            public abstract bool IsApplicable { get; }

            protected virtual BaseTypeSyntax[] AdditionalApplyToBaseTypes
            {
                get { return this.baseTypes.ToArray(); }
            }

            public GenerationResult Generate()
            {
                if (this.IsApplicable)
                {
                    this.GenerateCore();
                }

                return new GenerationResult
                {
                    MembersOfGeneratedType = SyntaxFactory.List(this.innerMembers),
                    SiblingsOfGeneratedType = SyntaxFactory.List(this.siblingMembers),
                    AdditionalCtorStatements = this.additionalCtorStatements.ToImmutableArray(),
                };
            }

            public virtual ClassDeclarationSyntax ProcessApplyToClassDeclaration(ClassDeclarationSyntax applyTo)
            {
                var additionalApplyToBaseTypes = this.AdditionalApplyToBaseTypes;
                if (additionalApplyToBaseTypes != null && additionalApplyToBaseTypes.Length > 0)
                {
                    applyTo = applyTo.WithBaseList(
                        (applyTo.BaseList ?? SyntaxFactory.BaseList()).AddTypes(additionalApplyToBaseTypes));
                }

                return applyTo;
            }

            protected abstract void GenerateCore();
        }

        protected struct MetaType
        {
            private CodeGen generator;

            public MetaType(CodeGen codeGen, INamedTypeSymbol typeSymbol)
            {
                this.generator = codeGen;
                this.TypeSymbol = typeSymbol;
            }

            public CodeGen Generator
            {
                get { return this.generator; }
            }

            public INamedTypeSymbol TypeSymbol { get; private set; }

            public NameSyntax TypeSyntax
            {
                get { return GetFullyQualifiedSymbolName(this.TypeSymbol); }
            }

            public bool IsDefault
            {
                get { return this.TypeSymbol == null; }
            }

            public IEnumerable<MetaField> LocalFields
            {
                get
                {
                    var that = this;
                    return this.TypeSymbol?.GetMembers().OfType<IFieldSymbol>().Select(f => new MetaField(that, f)) ?? ImmutableArray<MetaField>.Empty;
                }
            }

            public IEnumerable<MetaField> AllFields
            {
                get
                {
                    foreach (var field in this.InheritedFields)
                    {
                        yield return field;
                    }

                    foreach (var field in this.LocalFields)
                    {
                        yield return field;
                    }
                }
            }

            public IEnumerable<MetaField> InheritedFields
            {
                get
                {
                    if (this.TypeSymbol == null)
                    {
                        yield break;
                    }

                    foreach (var field in this.Ancestor.AllFields)
                    {
                        yield return field;
                    }
                }
            }

            public MetaType Ancestor
            {
                get
                {
                    return HasAttribute<GenerateImmutableAttribute>(this.TypeSymbol.BaseType)
                        ? new MetaType(this.generator, this.TypeSymbol.BaseType)
                        : default(MetaType);
                }
            }

            public IEnumerable<MetaType> Ancestors
            {
                get
                {
                    var ancestor = this.Ancestor;
                    while (!ancestor.IsDefault)
                    {
                        yield return ancestor;
                        ancestor = ancestor.Ancestor;
                    }
                }
            }

            public bool HasAncestor
            {
                get { return !this.Ancestor.IsDefault; }
            }

            public IEnumerable<MetaType> Descendents
            {
                get
                {
                    var that = this;
                    return from type in this.generator.TypesInInputDocument
                           where type != that.TypeSymbol
                           let metaType = new MetaType(that.generator, type)
                           where metaType.Ancestors.Any(a => a.TypeSymbol == that.TypeSymbol)
                           select metaType;
                }
            }

            public MetaField RecursiveField
            {
                get
                {
                    var rootOrThisType = this.RootAncestorOrThisType;
                    return this.LocalFields.SingleOrDefault(f => f.IsCollection && !f.IsDefinitelyNotRecursive && rootOrThisType.IsAssignableFrom(f.ElementType));
                }
            }

            public MetaType RecursiveType
            {
                get { return !this.RecursiveField.IsDefault ? this.FindMetaType((INamedTypeSymbol)this.RecursiveField.ElementType) : default(MetaType); }
            }

            public bool IsRecursiveType
            {
                get
                {
                    var that = this;
                    return this.GetTypeFamily().Any(t => that.Equals(t.RecursiveType));
                }
            }

            /// <summary>Gets the type that contains the collection of this (or a base) type.</summary>
            public MetaType RecursiveParent
            {
                get
                {
                    var that = this;
                    var result = this.GetTypeFamily().SingleOrDefault(t => !t.RecursiveType.IsDefault && t.RecursiveType.IsAssignableFrom(that.TypeSymbol));
                    return result;
                }
            }

            public bool IsRecursiveParent
            {
                get { return this.Equals(this.RecursiveParent); }
            }

            public bool IsRecursive
            {
                get
                {
                    var rootOrThisType = this.RootAncestorOrThisType;
                    return this.LocalFields.Count(f => f.IsCollection && rootOrThisType.IsAssignableFrom(f.ElementType) && !f.IsDefinitelyNotRecursive) == 1;
                }
            }

            public MetaType RootAncestorOrThisType
            {
                get
                {
                    MetaType current = this;
                    while (!current.Ancestor.IsDefault)
                    {
                        current = current.Ancestor;
                    }

                    return current;
                }
            }

            public bool ChildrenAreSorted
            {
                get
                {
                    // Not very precise, but it does the job for now.
                    return this.RecursiveField.Type.Name == nameof(ImmutableSortedSet<int>);
                }
            }

            public bool ChildrenAreOrdered
            {
                get
                {
                    // Not very precise, but it does the job for now.
                    var namedType = this.RecursiveField.Type as INamedTypeSymbol;
                    return namedType != null && namedType.AllInterfaces.Any(iface => iface.Name == nameof(IReadOnlyList<int>));
                }
            }

            public IEnumerable<MetaField> GetFieldsBeyond(MetaType ancestor)
            {
                if (ancestor.TypeSymbol == this.TypeSymbol)
                {
                    return ImmutableList.Create<MetaField>();
                }

                return ImmutableList.CreateRange(this.LocalFields)
                    .InsertRange(0, this.Ancestor.GetFieldsBeyond(ancestor));
            }

            public bool IsAssignableFrom(ITypeSymbol type)
            {
                if (type == null)
                {
                    return false;
                }

                return type == this.TypeSymbol
                    || this.IsAssignableFrom(type.BaseType);
            }

            public HashSet<MetaType> GetTypeFamily()
            {
                var set = new HashSet<MetaType>();
                var furthestAncestor = this.Ancestors.LastOrDefault();
                if (furthestAncestor.IsDefault)
                {
                    furthestAncestor = this;
                }

                set.Add(furthestAncestor);
                foreach (var relative in furthestAncestor.Descendents)
                {
                    set.Add(relative);
                }

                return set;
            }

            public override bool Equals(object obj)
            {
                if (obj is MetaType)
                {
                    return this.Equals((MetaType)obj);
                }

                return false;
            }

            public bool Equals(MetaType other)
            {
                return this.generator == other.generator
                    && this.TypeSymbol == other.TypeSymbol;
            }

            public override int GetHashCode()
            {
                return this.TypeSymbol?.GetHashCode() ?? 0;
            }

            private MetaType FindMetaType(INamedTypeSymbol type)
            {
                return new MetaType(this.generator, type);
            }
        }

        protected struct MetaField
        {
            private readonly MetaType metaType;

            public MetaField(MetaType type, IFieldSymbol symbol)
            {
                this.metaType = type;
                this.Symbol = symbol;
            }

            public string Name
            {
                get { return this.Symbol.Name; }
            }

            public IdentifierNameSyntax NameAsProperty
            {
                get { return SyntaxFactory.IdentifierName(this.Symbol.Name.ToPascalCase()); }
            }

            public IdentifierNameSyntax NameAsField
            {
                get { return SyntaxFactory.IdentifierName(this.Symbol.Name); }
            }

            public INamespaceOrTypeSymbol Type
            {
                get { return this.Symbol.Type; }
            }

            public bool IsGeneratedImmutableType
            {
                get { return !this.TypeAsGeneratedImmutable.IsDefault; }
            }

            public MetaType TypeAsGeneratedImmutable
            {
                get
                {
                    return IsAttributeApplied<GenerateImmutableAttribute>(this.Type)
                        ? new MetaType(this.metaType.Generator, (INamedTypeSymbol)this.Type)
                        : default(MetaType);
                }
            }

            public bool IsRequired
            {
                get { return IsFieldRequired(this.Symbol); }
            }

            public bool IsCollection
            {
                get { return IsCollectionType(this.Symbol.Type); }
            }

            public MetaType DeclaringType
            {
                get { return new MetaType(this.metaType.Generator, this.Symbol.ContainingType); }
            }

            public bool IsRecursiveCollection
            {
                get { return this.IsCollection && !this.DeclaringType.RecursiveType.IsDefault && this.ElementType == this.DeclaringType.RecursiveType.TypeSymbol; }
            }

            public bool IsDefinitelyNotRecursive
            {
                get { return IsAttributeApplied<NotRecursiveAttribute>(this.Symbol); }
            }

            /// <summary>
            /// Gets a value indicating whether this field is defined on the template type
            /// (as opposed to a base type).
            /// </summary>
            public bool IsLocallyDefined
            {
                get { return this.Symbol.ContainingType == this.metaType.Generator.applyToSymbol; }
            }

            public IFieldSymbol Symbol { get; private set; }

            public DistinguisherAttribute Distinguisher
            {
                get { return null; /* TODO */ }
            }

            public ITypeSymbol ElementType
            {
                get { return GetTypeOrCollectionMemberType(this.Symbol.Type); }
            }

            public bool IsDefault
            {
                get { return this.Symbol == null; }
            }

            public bool IsAssignableFrom(ITypeSymbol type)
            {
                if (type == null)
                {
                    return false;
                }

                var that = this;
                return type == this.Symbol.Type
                    || this.IsAssignableFrom(type.BaseType)
                    || type.Interfaces.Any(i => that.IsAssignableFrom(i));
            }

            private static ITypeSymbol GetTypeOrCollectionMemberType(ITypeSymbol collectionOrOtherType)
            {
                ITypeSymbol memberType;
                if (TryGetCollectionElementType(collectionOrOtherType, out memberType))
                {
                    return memberType;
                }

                return collectionOrOtherType;
            }

            private static bool TryGetCollectionElementType(ITypeSymbol collectionType, out ITypeSymbol elementType)
            {
                collectionType = GetCollectionType(collectionType);
                var arrayType = collectionType as IArrayTypeSymbol;
                if (arrayType != null)
                {
                    elementType = arrayType.ElementType;
                    return true;
                }

                var namedType = collectionType as INamedTypeSymbol;
                if (namedType != null)
                {
                    if (namedType.IsGenericType && namedType.TypeArguments.Length == 1)
                    {
                        elementType = namedType.TypeArguments[0];
                        return true;
                    }
                }

                elementType = null;
                return false;
            }

            private static ITypeSymbol GetCollectionType(ITypeSymbol type)
            {
                if (type is IArrayTypeSymbol)
                {
                    return type;
                }

                var namedType = type as INamedTypeSymbol;
                if (namedType != null)
                {
                    if (namedType.IsGenericType && namedType.TypeArguments.Length == 1)
                    {
                        var collectionType = namedType.AllInterfaces.FirstOrDefault(i => i.Name == nameof(IReadOnlyCollection<int>));
                        if (collectionType != null)
                        {
                            return collectionType;
                        }
                    }
                }

                return null;
            }

            private static bool IsCollectionType(ITypeSymbol type)
            {
                return GetCollectionType(type) != null;
            }
        }

        private enum ParameterStyle
        {
            Required,
            Optional,
            OptionalOrRequired,
        }

        private enum ArgSource
        {
            Property,
            Argument,
            OptionalArgumentOrProperty,
            OptionalArgumentOrTemplate,
            Missing,
        }

        private enum OptionalStyle
        {
            None,
            WhenNotRequired,
            Always,
        }
    }
}
