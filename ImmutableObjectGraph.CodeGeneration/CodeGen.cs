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
    using Microsoft.CodeAnalysis.Text;
    using Microsoft.ImmutableObjectGraph_SFG;
    using Validation;

    public class CodeGen
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

        private SemanticModel semanticModel;
        private INamedTypeSymbol applyToSymbol;
        private ImmutableArray<DeclarationInfo> inputDeclarations;
        private MetaType applyToMetaType;
        private bool isAbstract;
        private TypeSyntax applyToTypeName;
        private List<IFeatureGenerator> mergedFeatures = new List<IFeatureGenerator>();

        private CodeGen(ClassDeclarationSyntax applyTo, Document document, IProgressAndErrors progress, Options options, CancellationToken cancellationToken)
        {
            this.applyTo = applyTo;
            this.document = document;
            this.progress = progress;
            this.options = options ?? new Options();
            this.cancellationToken = cancellationToken;
        }

        public static async Task<IReadOnlyList<MemberDeclarationSyntax>> GenerateAsync(ClassDeclarationSyntax applyTo, Document document, IProgressAndErrors progress, Options options, CancellationToken cancellationToken)
        {
            Requires.NotNull(applyTo, "applyTo");
            Requires.NotNull(document, "document");
            Requires.NotNull(progress, "progress");

            var instance = new CodeGen(applyTo, document, progress, options, cancellationToken);
            return await instance.GenerateAsync();
        }

        private void MergeFeature(IFeatureGenerator featureGenerator)
        {
            var typeConversionMembers = featureGenerator.Generate();
            this.innerMembers.AddRange(typeConversionMembers.MembersOfGeneratedType);
            this.outerMembers.AddRange(typeConversionMembers.SiblingsOfGeneratedType);
            this.mergedFeatures.Add(featureGenerator);
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

            if (!this.applyToMetaType.HasAncestor)
            {
                this.innerMembers.Add(CreateLastIdentityProducedField());
                this.innerMembers.Add(CreateIdentityField());
                this.innerMembers.Add(CreateIdentityProperty());
                this.innerMembers.Add(CreateNewIdentityMethod());
            }

            this.innerMembers.Add(CreateCtor());
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

            if (this.options.GenerateBuilder)
            {
                this.MergeFeature(new BuilderGen(this));
            }

            if (this.options.DefineRootedStruct)
            {
                this.MergeFeature(new RootedStructGen(this));
            }

            if (this.options.Delta)
            {
                this.MergeFeature(new DeltaGen(this));
            }

            if (this.options.DefineInterface)
            {
                this.MergeFeature(new InterfacesGen(this));
            }

            if (this.options.DefineWithMethodsPerProperty)
            {
                this.MergeFeature(new DefineWithMethodsPerPropertyGen(this));
            }

            this.MergeFeature(new TypeConversionGen(this));
            this.MergeFeature(new CollectionHelpersGen(this));

            this.innerMembers.Sort(StyleCop.Sort);

            this.outerMembers.Add(SyntaxFactory.ClassDeclaration(applyTo.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                .WithMembers(SyntaxFactory.List(this.innerMembers)));

            foreach (var mergedFeature in this.mergedFeatures.OfType<IFeatureGeneratorWithPostProcessing>())
            {
                mergedFeature.PostProcess();
            }

            return this.outerMembers;
        }

        protected struct GenerationResult
        {
            public SyntaxList<MemberDeclarationSyntax> MembersOfGeneratedType { get; set; }

            public SyntaxList<MemberDeclarationSyntax> SiblingsOfGeneratedType { get; set; }
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
                    // if (!skipValidation.Value)
                    SyntaxFactory.IfStatement(
                        SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, Syntax.OptionalValue(SkipValidationParameterName)),
                        // this.Validate();
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.InvocationExpression(
                                Syntax.ThisDot(ValidateMethodName),
                                SyntaxFactory.ArgumentList()))));
            }

            var ctor = SyntaxFactory.ConstructorDeclaration(
                this.applyTo.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)))
                .WithParameterList(
                    CreateParameterList(this.applyToMetaType.AllFields, ParameterStyle.Required)
                    .PrependParameter(RequiredIdentityParameter)
                    .AddParameters(Syntax.Optional(SyntaxFactory.Parameter(SkipValidationParameterName.Identifier).WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword))))))
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
            Func<IFieldSymbol, ExpressionSyntax> isChanged = v =>
                SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.LogicalAndExpression,
                        Syntax.OptionalIsDefined(SyntaxFactory.IdentifierName(v.Name)),
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.NotEqualsExpression,
                            Syntax.OptionalValue(SyntaxFactory.IdentifierName(v.Name)),
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
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.IdentifierName(applyTo.Identifier),
                                    CreateArgumentList(this.applyToMetaType.AllFields, ArgSource.OptionalArgumentOrProperty)
                                        .PrependArgument(SyntaxFactory.Argument(SyntaxFactory.NameColon(IdentityParameterName), SyntaxFactory.Token(SyntaxKind.None), Syntax.OptionalGetValueOrDefault(SyntaxFactory.IdentifierName(IdentityParameterName.Identifier), Syntax.ThisDot(IdentityPropertyName)))),
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

        private ParameterListSyntax CreateParameterList(IEnumerable<IFieldSymbol> fields, ParameterStyle style)
        {
            if (style == ParameterStyle.OptionalOrRequired)
            {
                ////fields = SortRequiredFieldsFirst(fields);
            }

            Func<IFieldSymbol, bool> isOptional = f => style == ParameterStyle.Optional || (style == ParameterStyle.OptionalOrRequired && !IsFieldRequired(f));
            Func<ParameterSyntax, IFieldSymbol, ParameterSyntax> setTypeAndDefault = (p, f) => isOptional(f)
                ? Syntax.Optional(p.WithType(GetFullyQualifiedSymbolName(f.Type)))
                : p.WithType(GetFullyQualifiedSymbolName(f.Type));
            return SyntaxFactory.ParameterList(
                Syntax.JoinSyntaxNodes(
                    SyntaxKind.CommaToken,
                    fields.Select(f => setTypeAndDefault(SyntaxFactory.Parameter(SyntaxFactory.Identifier(f.Name)), f))));
        }

        private ArgumentListSyntax CreateArgumentList(IEnumerable<IFieldSymbol> fields, ArgSource source = ArgSource.Property, OptionalStyle asOptional = OptionalStyle.None)
        {
            Func<IFieldSymbol, ArgSource> fieldSource = f => (source == ArgSource.OptionalArgumentOrTemplate && IsFieldRequired(f)) ? ArgSource.Argument : source;
            Func<IFieldSymbol, bool> optionalWrap = f => asOptional != OptionalStyle.None && (asOptional == OptionalStyle.Always || !IsFieldRequired(f));
            Func<IFieldSymbol, ExpressionSyntax> dereference = f =>
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

        private bool IsFieldRequired(IFieldSymbol fieldSymbol)
        {
            return fieldSymbol?.GetAttributes().Any(a => IsOrDerivesFrom<RequiredAttribute>(a.AttributeClass)) ?? false;
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

        protected class DeltaGen : IFeatureGenerator
        {
            private static readonly IdentifierNameSyntax EnumValueNone = SyntaxFactory.IdentifierName("None");
            private static readonly IdentifierNameSyntax EnumValueType = SyntaxFactory.IdentifierName("Type");
            private static readonly IdentifierNameSyntax EnumValuePositionUnderParent = SyntaxFactory.IdentifierName("PositionUnderParent");
            private static readonly IdentifierNameSyntax EnumValueParent = SyntaxFactory.IdentifierName("Parent");
            private static readonly IdentifierNameSyntax EnumValueAll = SyntaxFactory.IdentifierName("All");

            private readonly CodeGen generator;
            private readonly string enumTypeName;

            public DeltaGen(CodeGen generator)
            {
                this.generator = generator;
                this.enumTypeName = generator.applyToMetaType.TypeSymbol.Name + "ChangedProperties";
            }

            public GenerationResult Generate()
            {
                var outerMembers = new List<MemberDeclarationSyntax>();

                outerMembers.Add(this.CreateChangedPropertiesEnum());

                return new GenerationResult { SiblingsOfGeneratedType = SyntaxFactory.List(outerMembers) };
            }

            private EnumDeclarationSyntax CreateChangedPropertiesEnum()
            {
                var fields = this.generator.applyToMetaType.Concat(this.generator.applyToMetaType.Descendents)
                    .SelectMany(t => t.LocalFields)
                    ////.Where(f => !f.IsRecursiveCollection) // TODO
                    .GroupBy(f => f.Name.ToPascalCase());
                int fieldsCount = 4 + fields.Count();
                int counter = 3;
                var fieldEnumValues = new List<EnumMemberDeclarationSyntax>();
                foreach (var field in fields)
                {
                    fieldEnumValues.Add(
                        SyntaxFactory.EnumMemberDeclaration(
                            SyntaxFactory.List<AttributeListSyntax>(),
                            SyntaxFactory.Identifier(field.Key),
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(string.Format(CultureInfo.InvariantCulture, "0x{0:x}", (ulong)Math.Pow(2, counter)), counter)))));
                    counter++;
                }

                var allEnumValue = SyntaxFactory.EnumMemberDeclaration(
                        SyntaxFactory.List<AttributeListSyntax>(),
                        EnumValueAll.Identifier,
                        SyntaxFactory.EqualsValueClause(
                            Syntax.ChainBinaryExpressions(
                                new[] { EnumValueType, EnumValuePositionUnderParent, EnumValueParent }.Concat(
                                fields.Select(f => SyntaxFactory.IdentifierName(f.Key))),
                                SyntaxKind.BitwiseOrExpression)));

                TypeSyntax enumBaseType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(
                    fieldsCount > 32
                        ? SyntaxKind.ULongKeyword
                        : SyntaxKind.UIntKeyword));

                var result = SyntaxFactory.EnumDeclaration(this.enumTypeName)
                    .AddModifiers(GetModifiersForAccessibility(this.generator.applyToSymbol))
                    .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(enumBaseType))))
                    .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(
                        SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Flags"))))))
                    .AddMembers(
                        SyntaxFactory.EnumMemberDeclaration(SyntaxFactory.List<AttributeListSyntax>(), EnumValueNone.Identifier, SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0x0", 0x0)))),
                        SyntaxFactory.EnumMemberDeclaration(SyntaxFactory.List<AttributeListSyntax>(), EnumValueType.Identifier, SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0x1", 0x1)))),
                        SyntaxFactory.EnumMemberDeclaration(SyntaxFactory.List<AttributeListSyntax>(), EnumValuePositionUnderParent.Identifier, SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0x2", 0x2)))),
                        SyntaxFactory.EnumMemberDeclaration(SyntaxFactory.List<AttributeListSyntax>(), EnumValueParent.Identifier, SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0x4", 0x4)))))
                    .AddMembers(fieldEnumValues.ToArray())
                    .AddMembers(allEnumValue);
                return result;
            }
        }

        protected class RootedStructGen : IFeatureGenerator
        {
            private readonly CodeGen codeGen;

            public RootedStructGen(CodeGen codeGen)
            {
                this.codeGen = codeGen;
            }

            public GenerationResult Generate()
            {
                var outerClassMembers = new List<MemberDeclarationSyntax>();
                var innerClassMembers = new List<MemberDeclarationSyntax>();

                return new GenerationResult
                {
                    MembersOfGeneratedType = SyntaxFactory.List(innerClassMembers),
                    SiblingsOfGeneratedType = SyntaxFactory.List(outerClassMembers),
                };
            }
        }

        protected class BuilderGen : IFeatureGenerator
        {
            private static readonly IdentifierNameSyntax BuilderTypeName = SyntaxFactory.IdentifierName("Builder");
            private static readonly IdentifierNameSyntax ToBuilderMethodName = SyntaxFactory.IdentifierName("ToBuilder");
            private static readonly IdentifierNameSyntax ToImmutableMethodName = SyntaxFactory.IdentifierName("ToImmutable");
            private static readonly IdentifierNameSyntax CreateBuilderMethodName = SyntaxFactory.IdentifierName("CreateBuilder");
            private static readonly IdentifierNameSyntax ImmutableFieldName = SyntaxFactory.IdentifierName("immutable");

            private readonly CodeGen generator;

            public BuilderGen(CodeGen generator)
            {
                this.generator = generator;
            }

            public GenerationResult Generate()
            {
                var outerClassMembers = new List<MemberDeclarationSyntax>
                {
                    this.CreateToBuilderMethod(),
                };

                if (!this.generator.isAbstract)
                {
                    outerClassMembers.Add(this.CreateCreateBuilderMethod());
                }

                var innerClassMembers = new List<MemberDeclarationSyntax>();
                innerClassMembers.Add(this.CreateImmutableField());
                innerClassMembers.AddRange(this.CreateMutableFields());
                innerClassMembers.Add(this.CreateConstructor());
                innerClassMembers.AddRange(this.CreateMutableProperties());
                innerClassMembers.Add(this.CreateToImmutableMethod());
                var builderType = SyntaxFactory.ClassDeclaration(BuilderTypeName.Identifier)
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                    .WithMembers(SyntaxFactory.List(innerClassMembers));
                if (this.generator.applyToMetaType.HasAncestor)
                {
                    builderType = builderType
                        .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                            SyntaxFactory.SimpleBaseType(SyntaxFactory.QualifiedName(
                                GetFullyQualifiedSymbolName(this.generator.applyToMetaType.Ancestor.TypeSymbol),
                                BuilderTypeName)))))
                        .WithModifiers(builderType.Modifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.NewKeyword)));
                }

                outerClassMembers.Add(builderType);
                return new GenerationResult { MembersOfGeneratedType = SyntaxFactory.List(outerClassMembers) };
            }

            protected MethodDeclarationSyntax CreateToBuilderMethod()
            {
                var method = SyntaxFactory.MethodDeclaration(
                    BuilderTypeName,
                    ToBuilderMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBody(SyntaxFactory.Block(
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.ObjectCreationExpression(
                                BuilderTypeName,
                                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.ThisExpression()))),
                                null))));

                if (this.generator.applyToMetaType.HasAncestor)
                {
                    method = Syntax.AddNewKeyword(method);
                }

                return method;
            }

            protected MethodDeclarationSyntax CreateCreateBuilderMethod()
            {
                var method = SyntaxFactory.MethodDeclaration(
                    BuilderTypeName,
                    CreateBuilderMethodName.Identifier)
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                    .WithBody(SyntaxFactory.Block(
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.ObjectCreationExpression(
                                BuilderTypeName,
                                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(DefaultInstanceFieldName))),
                                null))));

                if (this.generator.applyToMetaType.Ancestors.Any(a => !a.TypeSymbol.IsAbstract))
                {
                    method = Syntax.AddNewKeyword(method);
                }

                return method;
            }

            protected MemberDeclarationSyntax CreateImmutableField()
            {
                return SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName(this.generator.applyTo.Identifier),
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(ImmutableFieldName.Identifier))))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                    .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(DebuggerBrowsableNeverAttribute)));
            }

            protected IReadOnlyList<MemberDeclarationSyntax> CreateMutableFields()
            {
                var fields = new List<FieldDeclarationSyntax>();

                foreach (var field in this.generator.GetFields())
                {
                    fields.Add(field
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)))
                        .WithAttributeLists(SyntaxFactory.SingletonList(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(DebuggerBrowsableNeverAttribute)))));
                }

                return fields;
            }

            protected MemberDeclarationSyntax CreateConstructor()
            {
                var immutableParameterName = SyntaxFactory.IdentifierName("immutable");
                var body = SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        Syntax.ThisDot(ImmutableFieldName),
                        immutableParameterName)));
                foreach (var field in this.generator.GetFieldVariables())
                {
                    body = body.AddStatements(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        Syntax.ThisDot(SyntaxFactory.IdentifierName(field.Value.Identifier)),
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, immutableParameterName, SyntaxFactory.IdentifierName(field.Value.Identifier)))));
                }

                var ctor = SyntaxFactory.ConstructorDeclaration(BuilderTypeName.Identifier)
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(immutableParameterName.Identifier).WithType(SyntaxFactory.IdentifierName(this.generator.applyTo.Identifier)))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword))
                    .WithBody(body);

                if (this.generator.applyToMetaType.HasAncestor)
                {
                    ctor = ctor.WithInitializer(SyntaxFactory.ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(immutableParameterName)))));
                }

                return ctor;
            }

            protected IReadOnlyList<MemberDeclarationSyntax> CreateMutableProperties()
            {
                var properties = new List<PropertyDeclarationSyntax>();

                foreach (var field in this.generator.GetFieldVariables())
                {
                    var getterBlock = SyntaxFactory.Block(
                        SyntaxFactory.ReturnStatement(Syntax.ThisDot(SyntaxFactory.IdentifierName(field.Value.Identifier))));
                    var setterBlock = SyntaxFactory.Block(
                        SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            Syntax.ThisDot(SyntaxFactory.IdentifierName(field.Value.Identifier)),
                            SyntaxFactory.IdentifierName("value"))));

                    var property = CreatePropertyForField(field.Key, field.Value)
                        .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new AccessorDeclarationSyntax[]
                        {
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, getterBlock),
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, setterBlock),
                        })));
                    properties.Add(property);
                }

                return properties;
            }

            protected MethodDeclarationSyntax CreateToImmutableMethod()
            {
                ExpressionSyntax returnExpression;
                if (this.generator.applyToMetaType.AllFields.Any())
                {
                    // this.immutable = this.immutable.With(...)
                    returnExpression = SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        Syntax.ThisDot(ImmutableFieldName),
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                Syntax.ThisDot(ImmutableFieldName),
                                WithMethodName),
                            this.generator.CreateArgumentList(this.generator.applyToMetaType.AllFields, ArgSource.Property, OptionalStyle.Always)));
                }
                else
                {
                    // this.immutable
                    returnExpression = Syntax.ThisDot(ImmutableFieldName);
                }

                var body = SyntaxFactory.Block(
                    SyntaxFactory.ReturnStatement(returnExpression));

                // public TemplateType ToImmutable() { ... }
                var method = SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.IdentifierName(this.generator.applyTo.Identifier),
                    ToImmutableMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBody(body);

                if (this.generator.applyToMetaType.HasAncestor)
                {
                    method = Syntax.AddNewKeyword(method);
                }

                return method;
            }
        }

        protected class InterfacesGen : IFeatureGeneratorWithPostProcessing
        {
            private readonly CodeGen generator;

            public InterfacesGen(CodeGen generator)
            {
                this.generator = generator;
            }

            public GenerationResult Generate()
            {
                var iface = SyntaxFactory.InterfaceDeclaration(
                    "I" + this.generator.applyTo.Identifier.Text)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithMembers(
                        SyntaxFactory.List<MemberDeclarationSyntax>(
                            from field in this.generator.applyToMetaType.LocalFields
                            select SyntaxFactory.PropertyDeclaration(
                                GetFullyQualifiedSymbolName(field.Type),
                                field.Name.ToPascalCase())
                                .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(
                                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))))));
                if (this.generator.applyToMetaType.HasAncestor)
                {
                    iface = iface.WithBaseList(SyntaxFactory.BaseList(
                        SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(
                            SyntaxFactory.IdentifierName("I" + this.generator.applyToMetaType.Ancestor.TypeSymbol.Name)))));
                }

                return new GenerationResult()
                {
                    SiblingsOfGeneratedType = SyntaxFactory.SingletonList<MemberDeclarationSyntax>(iface)
                };
            }

            public void PostProcess()
            {
                var applyToPrimaryType = this.generator.outerMembers.OfType<ClassDeclarationSyntax>()
                    .First(c => c.Identifier.Text == this.generator.applyTo.Identifier.Text);
                var updatedPrimaryType = applyToPrimaryType.WithBaseList(
                    (applyToPrimaryType.BaseList ?? SyntaxFactory.BaseList()).AddTypes(SyntaxFactory.SimpleBaseType(
                        SyntaxFactory.IdentifierName("I" + this.generator.applyTo.Identifier.Text))));

                this.generator.outerMembers.Remove(applyToPrimaryType);
                this.generator.outerMembers.Add(updatedPrimaryType);
            }
        }

        protected class CollectionHelpersGen : IFeatureGenerator
        {
            private readonly CodeGen generator;

            public CollectionHelpersGen(CodeGen generator)
            {
                this.generator = generator;
            }

            public GenerationResult Generate()
            {
                var members = new List<MemberDeclarationSyntax>();

                foreach (var field in this.generator.applyToMetaType.LocalFields)
                {
                }

                return new GenerationResult()
                {
                    MembersOfGeneratedType = SyntaxFactory.List(members)
                };
            }
        }

        protected interface IFeatureGenerator
        {
            GenerationResult Generate();
        }

        protected interface IFeatureGeneratorWithPostProcessing : IFeatureGenerator
        {
            void PostProcess();
        }

        protected class TypeConversionGen : IFeatureGenerator
        {
            private static readonly IdentifierNameSyntax CreateWithIdentityMethodName = SyntaxFactory.IdentifierName("CreateWithIdentity");
            private readonly CodeGen generator;

            public TypeConversionGen(CodeGen generator)
            {
                this.generator = generator;
            }

            public GenerationResult Generate()
            {
                var members = new List<MemberDeclarationSyntax>();
                if (!this.generator.applyToSymbol.IsAbstract && (this.generator.applyToMetaType.HasAncestor || this.generator.applyToMetaType.Descendents.Any()))
                {
                    members.Add(this.CreateCreateWithIdentityMethod());
                }

                if (this.generator.applyToMetaType.HasAncestor && !this.generator.applyToMetaType.Ancestor.TypeSymbol.IsAbstract)
                {
                    members.Add(this.CreateToAncestorTypeMethod());
                }

                foreach (MetaType derivedType in this.generator.applyToMetaType.Descendents.Where(d => !d.TypeSymbol.IsAbstract))
                {
                    members.Add(this.CreateToDerivedTypeMethod(derivedType));

                    foreach (MetaType ancestor in this.generator.applyToMetaType.Ancestors)
                    {
                        members.Add(this.CreateToDerivedTypeOverrideMethod(derivedType, ancestor));
                    }
                }

                return new GenerationResult { MembersOfGeneratedType = SyntaxFactory.List(members) };
            }

            internal static IdentifierNameSyntax GetToTypeMethodName(string typeName)
            {
                return SyntaxFactory.IdentifierName("To" + typeName);
            }

            private MemberDeclarationSyntax CreateCreateWithIdentityMethod()
            {
                ExpressionSyntax returnExpression = DefaultInstanceFieldName;
                if (this.generator.applyToMetaType.LocalFields.Any())
                {
                    returnExpression = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            returnExpression,
                            WithFactoryMethodName),
                        this.generator.CreateArgumentList(this.generator.applyToMetaType.AllFields, ArgSource.OptionalArgumentOrTemplate, OptionalStyle.Always)
                            .AddArguments(OptionalIdentityArgument));
                }

                var method = SyntaxFactory.MethodDeclaration(
                    this.generator.applyToTypeName,
                    CreateWithIdentityMethodName.Identifier)
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                    .WithParameterList(
                        this.generator.CreateParameterList(
                            this.generator.applyToMetaType.AllFields,
                            ParameterStyle.OptionalOrRequired)
                        .AddParameters(OptionalIdentityParameter))
                    .WithBody(SyntaxFactory.Block(
                        SyntaxFactory.IfStatement(
                            SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, Syntax.OptionalIsDefined(IdentityParameterName)),
                            SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentityParameterName,
                                SyntaxFactory.InvocationExpression(NewIdentityMethodName, SyntaxFactory.ArgumentList())))),
                        SyntaxFactory.ReturnStatement(returnExpression)));

                // BUG: the condition should be if there are local fields on *any* ancestor
                // from the closest non-abstract ancestor (exclusive) to this type (inclusive).
                if (!this.generator.applyToMetaType.LocalFields.Any() && this.generator.applyToMetaType.Ancestors.Any(a => !a.TypeSymbol.IsAbstract))
                {
                    method = Syntax.AddNewKeyword(method);
                }

                return method;
            }

            private MemberDeclarationSyntax CreateToAncestorTypeMethod()
            {
                var ancestor = this.generator.applyToMetaType.Ancestor;
                var ancestorType = GetFullyQualifiedSymbolName(ancestor.TypeSymbol);
                return SyntaxFactory.MethodDeclaration(
                    ancestorType,
                    GetToTypeMethodName(this.generator.applyToMetaType.Ancestor.TypeSymbol.Name).Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBody(SyntaxFactory.Block(
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    ancestorType,
                                    CreateWithIdentityMethodName),
                                this.generator.CreateArgumentList(ancestor.AllFields, asOptional: OptionalStyle.WhenNotRequired)
                                    .AddArguments(RequiredIdentityArgumentFromProperty)))));
            }

            private MemberDeclarationSyntax CreateToDerivedTypeMethod(MetaType derivedType)
            {
                var derivedTypeName = GetFullyQualifiedSymbolName(derivedType.TypeSymbol);
                var thatLocal = SyntaxFactory.IdentifierName("that");
                var body = new List<StatementSyntax>();

                // var that = this as DerivedType;
                body.Add(SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        varType,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(thatLocal.Identifier)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.BinaryExpression(
                                        SyntaxKind.AsExpression,
                                        SyntaxFactory.ThisExpression(),
                                        derivedTypeName)))))));

                // this.GetType()
                var thisDotGetType = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(), SyntaxFactory.IdentifierName("GetType")),
                    SyntaxFactory.ArgumentList());

                // {0}.IsEquivalentTo(typeof(derivedType))
                var thisTypeIsEquivalentToDerivedType =
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            thisDotGetType,
                            SyntaxFactory.IdentifierName("IsEquivalentTo")),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(
                            SyntaxFactory.TypeOfExpression(derivedTypeName)))));

                var ifEquivalentTypeBlock = new List<StatementSyntax>();
                var fieldsBeyond = derivedType.GetFieldsBeyond(this.generator.applyToMetaType);
                if (fieldsBeyond.Any())
                {
                    Func<IFieldSymbol, ExpressionSyntax> isUnchanged = v =>
                        SyntaxFactory.ParenthesizedExpression(
                            this.generator.IsFieldRequired(v)
                                ? // ({0} == that.{1})
                                SyntaxFactory.BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    SyntaxFactory.IdentifierName(v.Name),
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        thatLocal,
                                        SyntaxFactory.IdentifierName(v.Name)))
                                : // (!{0}.IsDefined || {0}.Value == that.{1})
                                SyntaxFactory.BinaryExpression(
                                    SyntaxKind.LogicalOrExpression,
                                    SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, Syntax.OptionalIsDefined(SyntaxFactory.IdentifierName(v.Name))),
                                    SyntaxFactory.BinaryExpression(
                                        SyntaxKind.EqualsExpression,
                                        Syntax.OptionalValue(SyntaxFactory.IdentifierName(v.Name)),
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            thatLocal,
                                            SyntaxFactory.IdentifierName(v.Name.ToPascalCase())))));
                    var noChangesExpression = fieldsBeyond.Select(isUnchanged).ChainBinaryExpressions(SyntaxKind.LogicalAndExpression);

                    ifEquivalentTypeBlock.Add(SyntaxFactory.IfStatement(
                        noChangesExpression,
                        SyntaxFactory.ReturnStatement(thatLocal)));
                }
                else
                {
                    ifEquivalentTypeBlock.Add(SyntaxFactory.ReturnStatement(thatLocal));
                }

                // if (that != null && this.GetType().IsEquivalentTo(typeof(derivedType))) { ... }
                body.Add(SyntaxFactory.IfStatement(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.LogicalAndExpression,
                        SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, thatLocal, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        thisTypeIsEquivalentToDerivedType),
                    SyntaxFactory.Block(ifEquivalentTypeBlock)));

                // return DerivedType.CreateWithIdentity(...)
                body.Add(SyntaxFactory.ReturnStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            derivedTypeName,
                            CreateWithIdentityMethodName),
                        this.generator.CreateArgumentList(this.generator.applyToMetaType.AllFields, asOptional: OptionalStyle.WhenNotRequired)
                            .AddArguments(RequiredIdentityArgumentFromProperty)
                            .AddArguments(this.generator.CreateArgumentList(fieldsBeyond, ArgSource.Argument).Arguments.ToArray()))));

                return SyntaxFactory.MethodDeclaration(
                    derivedTypeName,
                    GetToTypeMethodName(derivedType.TypeSymbol.Name).Identifier)
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
                    .WithParameterList(this.generator.CreateParameterList(fieldsBeyond, ParameterStyle.OptionalOrRequired))
                    .WithBody(SyntaxFactory.Block(body));
            }

            private MemberDeclarationSyntax CreateToDerivedTypeOverrideMethod(MetaType derivedType, MetaType ancestor)
            {
                var derivedTypeName = GetFullyQualifiedSymbolName(derivedType.TypeSymbol);
                return SyntaxFactory.MethodDeclaration(
                    derivedTypeName,
                    GetToTypeMethodName(derivedType.TypeSymbol.Name).Identifier)
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
                    .WithParameterList(
                        this.generator.CreateParameterList(derivedType.GetFieldsBeyond(ancestor), ParameterStyle.OptionalOrRequired))
                    .WithBody(SyntaxFactory.Block(
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.BaseExpression(),
                                    GetToTypeMethodName(derivedType.TypeSymbol.Name)),
                                this.generator.CreateArgumentList(this.generator.applyToMetaType.GetFieldsBeyond(ancestor), ArgSource.OptionalArgumentOrProperty, OptionalStyle.WhenNotRequired)
                                    .AddArguments(this.generator.CreateArgumentList(derivedType.GetFieldsBeyond(this.generator.applyToMetaType), ArgSource.Argument).Arguments.ToArray())))));
            }
        }

        protected class DefineWithMethodsPerPropertyGen : IFeatureGenerator
        {
            private const string WithPropertyMethodPrefix = "With";
            private readonly CodeGen generator;

            public DefineWithMethodsPerPropertyGen(CodeGen codeGen)
            {
                this.generator = codeGen;
            }

            public GenerationResult Generate()
            {
                var valueParameterName = SyntaxFactory.IdentifierName("value");
                var insideMembers = new List<MemberDeclarationSyntax>();

                foreach (var field in this.generator.applyToMetaType.LocalFields)
                {
                    var withPropertyMethod = SyntaxFactory.MethodDeclaration(
                        GetFullyQualifiedSymbolName(this.generator.applyToSymbol),
                        WithPropertyMethodPrefix + field.Name.ToPascalCase())
                        .WithAdditionalAnnotations()
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddParameterListParameters(
                            SyntaxFactory.Parameter(valueParameterName.Identifier)
                                .WithType(GetFullyQualifiedSymbolName(field.Type)))
                        .WithBody(SyntaxFactory.Block(
                            SyntaxFactory.IfStatement(
                                SyntaxFactory.BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    valueParameterName,
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(), SyntaxFactory.IdentifierName(field.Name))),
                                SyntaxFactory.Block(
                                    SyntaxFactory.ReturnStatement(SyntaxFactory.ThisExpression()))),
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.ThisExpression(),
                                        WithMethodName),
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.NameColon(field.Name),
                                                SyntaxFactory.Token(SyntaxKind.None),
                                                Syntax.OptionalFor(valueParameterName))))))));

                    insideMembers.Add(withPropertyMethod);
                }

                foreach (var field in this.generator.applyToMetaType.InheritedFields)
                {
                    string withMethodName = WithPropertyMethodPrefix + field.Name.ToPascalCase();
                    var withPropertyMethod = SyntaxFactory.MethodDeclaration(
                        GetFullyQualifiedSymbolName(this.generator.applyToSymbol),
                        withMethodName)
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.NewKeyword),
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddParameterListParameters(
                            SyntaxFactory.Parameter(valueParameterName.Identifier)
                                .WithType(GetFullyQualifiedSymbolName(field.Type)))
                        .WithBody(SyntaxFactory.Block(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.CastExpression(
                                    GetFullyQualifiedSymbolName(this.generator.applyToSymbol),
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.BaseExpression(),
                                            SyntaxFactory.IdentifierName(withMethodName)),
                                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(valueParameterName))))))));

                    insideMembers.Add(withPropertyMethod);
                }

                return new GenerationResult
                {
                    MembersOfGeneratedType = SyntaxFactory.List(insideMembers),
                };
            }
        }

        protected struct MetaType
        {
            private CodeGen generator;

            public MetaType(CodeGen codeGen, INamedTypeSymbol typeSymbol)
            {
                this.generator = codeGen;
                this.TypeSymbol = typeSymbol;
            }

            public INamedTypeSymbol TypeSymbol { get; private set; }

            public bool IsDefault
            {
                get { return this.TypeSymbol == null; }
            }

            public IEnumerable<IFieldSymbol> LocalFields
            {
                get { return this.TypeSymbol?.GetMembers().OfType<IFieldSymbol>() ?? ImmutableArray<IFieldSymbol>.Empty; }
            }

            public IEnumerable<IFieldSymbol> AllFields
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

            public IEnumerable<IFieldSymbol> InheritedFields
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

            public IEnumerable<IFieldSymbol> GetFieldsBeyond(MetaType ancestor)
            {
                if (ancestor.TypeSymbol == this.TypeSymbol)
                {
                    return ImmutableList.Create<IFieldSymbol>();
                }

                return ImmutableList.CreateRange(this.LocalFields)
                    .InsertRange(0, this.Ancestor.GetFieldsBeyond(ancestor));
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
