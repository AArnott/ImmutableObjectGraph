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

    public class CodeGen
    {
        private static readonly TypeSyntax IdentityFieldTypeSyntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword));
        private static readonly TypeSyntax IdentityFieldOptionalTypeSyntax = SyntaxFactory.GenericName(SyntaxFactory.Identifier(nameof(Optional)), SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(IdentityFieldTypeSyntax)));
        private static readonly IdentifierNameSyntax IdentityParameterName = SyntaxFactory.IdentifierName("identity");
        private static readonly IdentifierNameSyntax IdentityPropertyName = SyntaxFactory.IdentifierName("Identity");
        private static readonly ParameterSyntax RequiredIdentityParameter = SyntaxFactory.Parameter(IdentityParameterName.Identifier).WithType(IdentityFieldTypeSyntax);
        private static readonly ParameterSyntax OptionalIdentityParameter = Syntax.Optional(RequiredIdentityParameter);
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

        private readonly ClassDeclarationSyntax applyTo;
        private readonly Document document;
        private readonly IProgressAndErrors progress;
        private readonly Options options;
        private readonly CancellationToken cancellationToken;

        private SemanticModel semanticModel;
        private INamedTypeSymbol applyToSymbol;
        private MetaType applyToMetaType;
        private bool isAbstract;

        private CodeGen(ClassDeclarationSyntax applyTo, Document document, IProgressAndErrors progress, Options options, CancellationToken cancellationToken)
        {
            this.applyTo = applyTo;
            this.document = document;
            this.progress = progress;
            this.options = options ?? new Options();
            this.cancellationToken = cancellationToken;
        }

        public static async Task<MemberDeclarationSyntax> GenerateAsync(ClassDeclarationSyntax applyTo, Document document, IProgressAndErrors progress, Options options, CancellationToken cancellationToken)
        {
            Requires.NotNull(applyTo, "applyTo");
            Requires.NotNull(document, "document");
            Requires.NotNull(progress, "progress");

            var instance = new CodeGen(applyTo, document, progress, options, cancellationToken);
            return await instance.GenerateAsync();
        }

        private async Task<MemberDeclarationSyntax> GenerateAsync()
        {
            this.semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            this.isAbstract = applyTo.Modifiers.Any(m => m.IsContextualKind(SyntaxKind.AbstractKeyword));

            this.applyToSymbol = this.semanticModel.GetDeclaredSymbol(this.applyTo, this.cancellationToken);
            this.applyToMetaType = new MetaType(this.applyToSymbol);

            ValidateInput();

            var members = new List<MemberDeclarationSyntax>();
            if (!this.applyToMetaType.HasAncestor)
            {
                members.Add(CreateLastIdentityProducedField());
                members.Add(CreateIdentityField());
                members.Add(CreateIdentityProperty());
                members.Add(CreateNewIdentityMethod());
            }

            members.Add(CreateCtor());
            members.AddRange(CreateWithCoreMethods());

            if (!isAbstract)
            {
                members.Add(CreateCreateMethod());
                if (this.applyToMetaType.AllFields.Any())
                {
                    members.Add(CreateWithFactoryMethod());
                }

                members.Add(CreateDefaultInstanceField());
                members.Add(CreateGetDefaultTemplateMethod());
                members.Add(CreateCreateDefaultTemplatePartialMethod());
                members.Add(CreateTemplateStruct());
                members.Add(CreateValidateMethod());
            }

            if (this.applyToMetaType.AllFields.Any())
            {
                members.Add(CreateWithMethod());
            }

            members.AddRange(this.GetFieldVariables().Select(fv => CreatePropertyForField(fv.Key, fv.Value)));

            if (this.options.GenerateBuilder)
            {
                var builderGenerator = new BuilderGen(this);
                var builderMembers = await builderGenerator.GenerateAsync();
                members.AddRange(builderMembers);
            }

            return SyntaxFactory.ClassDeclaration(applyTo.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                .WithMembers(SyntaxFactory.List(members));
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
                                .AddRange(this.applyToMetaType.AllFields.Select(f => SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, templateVarName, SyntaxFactory.IdentifierName(f.Name)))))
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
                                SyntaxFactory.VariableDeclarator(f.Name))))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword)))))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                .WithLeadingTrivia(
                    SyntaxFactory.LineFeed,
                    SyntaxFactory.Trivia(
                        SyntaxFactory.PragmaWarningDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.DisableKeyword), true)
                        .WithErrorCodes(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0649)
                                .WithTrailingTrivia(SyntaxFactory.Space, SyntaxFactory.Comment("// field initialization is optional in user code")))))
                        .WithEndOfDirectiveToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.EndOfDirectiveToken, SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)))))
                .WithTrailingTrivia(
                    SyntaxFactory.Trivia(
                        SyntaxFactory.PragmaWarningDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.RestoreKeyword), true)
                        .WithErrorCodes(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0649))))
                        .WithEndOfDirectiveToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.EndOfDirectiveToken, SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)))),
                    SyntaxFactory.LineFeed);
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

        public class Options
        {
            public Options() { }

            public bool GenerateBuilder { get; set; }
        }

        protected class BuilderGen
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

            public async Task<IReadOnlyList<MemberDeclarationSyntax>> GenerateAsync()
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
                return outerClassMembers;
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

        protected struct MetaType
        {
            public MetaType(INamedTypeSymbol typeSymbol)
            {
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
                        ? new MetaType(this.TypeSymbol.BaseType)
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
