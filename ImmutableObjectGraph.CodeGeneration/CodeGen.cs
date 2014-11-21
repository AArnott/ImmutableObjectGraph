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

        private readonly ClassDeclarationSyntax applyTo;
        private readonly Document document;
        private readonly IProgressAndErrors progress;
        private readonly CancellationToken cancellationToken;

        private SemanticModel semanticModel;
        private bool isAbstract;

        private CodeGen(ClassDeclarationSyntax applyTo, Document document, IProgressAndErrors progress, CancellationToken cancellationToken)
        {
            this.applyTo = applyTo;
            this.document = document;
            this.progress = progress;
            this.cancellationToken = cancellationToken;
        }

        public static async Task<MemberDeclarationSyntax> GenerateAsync(ClassDeclarationSyntax applyTo, Document document, IProgressAndErrors progress, CancellationToken cancellationToken)
        {
            Requires.NotNull(applyTo, "applyTo");
            Requires.NotNull(document, "document");
            Requires.NotNull(progress, "progress");

            var instance = new CodeGen(applyTo, document, progress, cancellationToken);
            return await instance.GenerateAsync();
        }

        private async Task<MemberDeclarationSyntax> GenerateAsync()
        {
            this.semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            this.isAbstract = applyTo.Modifiers.Any(m => m.IsContextualKind(SyntaxKind.AbstractKeyword));

            ValidateInput();

            var fields = GetFields().ToList();
            var members = new List<MemberDeclarationSyntax>();
            members.Add(CreateLastIdentityProducedField());
            members.Add(CreateIdentityField());
            members.Add(CreateIdentityProperty());
            members.Add(CreateCtor());
            members.Add(CreateNewIdentityMethod());

            if (!isAbstract)
            {
                members.Add(CreateCreateMethod());
                if (fields.Count > 0)
                {
                    members.Add(CreateWithFactoryMethod());
                    members.Add(CreateWithMethod());
                    members.Add(CreateWithCoreMethod());
                }

                members.Add(CreateDefaultInstanceField());
                members.Add(CreateGetDefaultTemplateMethod());
                members.Add(CreateCreateDefaultTemplatePartialMethod());
                members.Add(CreateTemplateStruct());
                members.Add(CreateValidateMethod());
            }

            foreach (var field in fields)
            {
                foreach (var variable in field.Declaration.Variables)
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
                    members.Add(property);
                }
            }

            return SyntaxFactory.ClassDeclaration(applyTo.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                .WithMembers(SyntaxFactory.List(members));
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
                     SyntaxFactory.IdentifierName(applyTo.Identifier.ValueText),
                     SyntaxFactory.SingletonSeparatedList(
                         SyntaxFactory.VariableDeclarator(DefaultInstanceFieldName.Identifier)
                             .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.InvocationExpression(GetDefaultTemplateMethodName, SyntaxFactory.ArgumentList()))))))
                 .WithModifiers(SyntaxFactory.TokenList(
                     SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                     SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                     SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
                 .WithAttributeLists(SyntaxFactory.SingletonList(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                     SyntaxFactory.Attribute(
                         SyntaxFactory.ParseName(typeof(DebuggerBrowsableAttribute).FullName),
                         SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.AttributeArgument(
                             SyntaxFactory.MemberAccessExpression(
                                 SyntaxKind.SimpleMemberAccessExpression,
                                 SyntaxFactory.ParseName(typeof(DebuggerBrowsableState).FullName),
                                 SyntaxFactory.IdentifierName(nameof(DebuggerBrowsableState.Never)))))))))));
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
                                .AddRange(this.GetFieldVariables().Select(f => SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, templateVarName, SyntaxFactory.IdentifierName(f.Value.Identifier)))))
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
                    GetFields().Select(f => f.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword))))))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        }

        private MemberDeclarationSyntax CreateCtor()
        {
            BlockSyntax body = SyntaxFactory.Block(
                    ImmutableArray.Create<StatementSyntax>(
                        // this.identity = identity;
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                Syntax.ThisDot(IdentityParameterName),
                                IdentityParameterName)))
                    .AddRange(
                        // this.someField = someField;
                        this.GetFieldVariables().Select(f => SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                Syntax.ThisDot(SyntaxFactory.IdentifierName(f.Value.Identifier)),
                                SyntaxFactory.IdentifierName(f.Value.Identifier))))));

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

            return SyntaxFactory.ConstructorDeclaration(
                this.applyTo.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)))
                .WithParameterList(
                    CreateParameterList(ParameterStyle.Required)
                    .PrependParameter(RequiredIdentityParameter)
                    .AddParameters(Syntax.Optional(SyntaxFactory.Parameter(SkipValidationParameterName.Identifier).WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword))))))
                .WithBody(body);
        }

        private MethodDeclarationSyntax CreateWithMethod()
        {
            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName(this.applyTo.Identifier),
                WithMethodName.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithParameterList(CreateParameterList(ParameterStyle.Optional))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.CastExpression(
                            SyntaxFactory.IdentifierName(this.applyTo.Identifier),
                            SyntaxFactory.InvocationExpression(
                                Syntax.ThisDot(WithCoreMethodName),
                                this.CreateArgumentList(ArgSource.Argument))))));
        }

        private MethodDeclarationSyntax CreateWithCoreMethod()
        {
            var method = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName(this.applyTo.Identifier),
                WithCoreMethodName.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
                .WithParameterList(this.CreateParameterList(ParameterStyle.Optional));
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
                                this.CreateArgumentList(ArgSource.OptionalArgumentOrProperty, OptionalStyle.Always)
                                .AddArguments(SyntaxFactory.Argument(SyntaxFactory.NameColon(IdentityParameterName), SyntaxFactory.Token(SyntaxKind.None), Syntax.OptionalFor(Syntax.ThisDot(IdentityPropertyName))))))));
            }

            return method;
        }

        private MemberDeclarationSyntax CreateWithFactoryMethod()
        {
            // (field.IsDefined && field.Value != this.field)
            Func<VariableDeclaratorSyntax, ExpressionSyntax> isChanged = v =>
                SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.LogicalAndExpression,
                        Syntax.OptionalIsDefined(SyntaxFactory.IdentifierName(v.Identifier)),
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.NotEqualsExpression,
                            Syntax.OptionalValue(SyntaxFactory.IdentifierName(v.Identifier)),
                            Syntax.ThisDot(SyntaxFactory.IdentifierName(v.Identifier)))));
            var anyChangesExpression = this.GetFieldVariables().Select(fv => isChanged(fv.Value)).ChainBinaryExpressions(SyntaxKind.LogicalOrExpression);

            // /// <summary>Returns a new instance of this object with any number of properties changed.</summary>
            // private TemplateType WithFactory(...)
            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName(this.applyTo.Identifier),
                WithFactoryMethodName.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                .WithParameterList(CreateParameterList(ParameterStyle.Optional).AddParameters(OptionalIdentityParameter))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.IfStatement(
                        anyChangesExpression,
                        SyntaxFactory.Block(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.IdentifierName(applyTo.Identifier),
                                    CreateArgumentList(ArgSource.OptionalArgumentOrProperty)
                                        .PrependArgument(SyntaxFactory.Argument(SyntaxFactory.NameColon(IdentityParameterName), SyntaxFactory.Token(SyntaxKind.None), Syntax.OptionalGetValueOrDefault(SyntaxFactory.IdentifierName(IdentityParameterName.Identifier), Syntax.ThisDot(IdentityParameterName)))),
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
                            CreateArgumentList(ArgSource.OptionalArgumentOrTemplate, asOptional: OptionalStyle.Always)
                                .AddArguments(SyntaxFactory.Argument(SyntaxFactory.NameColon(IdentityParameterName), SyntaxFactory.Token(SyntaxKind.None), IdentityParameterName)))));
            }
            else
            {
                body = body.AddStatements(
                    SyntaxFactory.ReturnStatement(DefaultInstanceFieldName));
            }

            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName(applyTo.Identifier),
                CreateMethodName.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithParameterList(CreateParameterList(ParameterStyle.OptionalOrRequired))
                .WithBody(body);
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

        private ParameterListSyntax CreateParameterList(ParameterStyle style)
        {
            if (style == ParameterStyle.OptionalOrRequired)
            {
                ////fields = SortRequiredFieldsFirst(fields);
            }

            Func<FieldDeclarationSyntax, bool> isOptional = f => style == ParameterStyle.Optional || (style == ParameterStyle.OptionalOrRequired && !IsFieldRequired(f));
            Func<ParameterSyntax, FieldDeclarationSyntax, ParameterSyntax> setTypeAndDefault = (p, f) => isOptional(f)
                ? Syntax.Optional(p.WithType(f.Declaration.Type))
                : p.WithType(f.Declaration.Type);
            return SyntaxFactory.ParameterList(
                Syntax.JoinSyntaxNodes(
                    SyntaxKind.CommaToken,
                    this.GetFieldVariables().Select(f => setTypeAndDefault(SyntaxFactory.Parameter(f.Value.Identifier), f.Key))));
        }

        private ArgumentListSyntax CreateArgumentList(ArgSource source = ArgSource.Property, OptionalStyle asOptional = OptionalStyle.None)
        {
            Func<FieldDeclarationSyntax, ArgSource> fieldSource = f => (source == ArgSource.OptionalArgumentOrTemplate && IsFieldRequired(f)) ? ArgSource.Argument : source;
            Func<FieldDeclarationSyntax, bool> optionalWrap = f => asOptional != OptionalStyle.None && (asOptional == OptionalStyle.Always || !IsFieldRequired(f));
            Func<FieldDeclarationSyntax, VariableDeclaratorSyntax, ExpressionSyntax> dereference = (f, v) =>
            {
                var name = SyntaxFactory.IdentifierName(v.Identifier);
                switch (fieldSource(f))
                {
                    case ArgSource.Property:
                        return Syntax.ThisDot(name);
                    case ArgSource.Argument:
                        return name;
                    case ArgSource.OptionalArgumentOrProperty:
                        return Syntax.OptionalGetValueOrDefault(name, Syntax.ThisDot(name));
                    case ArgSource.OptionalArgumentOrTemplate:
                        return Syntax.OptionalGetValueOrDefault(name, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, DefaultInstanceFieldName, name));
                    case ArgSource.Missing:
                        return SyntaxFactory.DefaultExpression(Syntax.OptionalOf(f.Declaration.Type));
                    default:
                        throw Assumes.NotReachable();
                }
            };
            return SyntaxFactory.ArgumentList(Syntax.JoinSyntaxNodes(
                SyntaxKind.CommaToken,
                this.GetFieldVariables().Select(f =>
                    SyntaxFactory.Argument(
                        SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(f.Value.Identifier)),
                        SyntaxFactory.Token(SyntaxKind.None),
                        Syntax.OptionalForIf(dereference(f.Key, f.Value), optionalWrap(f.Key))))));
        }

        private bool IsFieldRequired(FieldDeclarationSyntax field)
        {
            var fieldSymbol = this.semanticModel.GetDeclaredSymbol(field.Declaration.Variables.First());
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
