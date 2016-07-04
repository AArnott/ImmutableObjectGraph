namespace ImmutableObjectGraph.Generation
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
    using Validation;
    using LookupTableHelper = RecursiveTypeExtensions.LookupTable<IRecursiveType, IRecursiveParentWithLookupTable<IRecursiveType>>;

    public partial class CodeGen
    {
        protected class BuilderGen : FeatureGenerator
        {
            private static readonly IdentifierNameSyntax BuilderTypeName = SyntaxFactory.IdentifierName("Builder");
            private static readonly IdentifierNameSyntax ToBuilderMethodName = SyntaxFactory.IdentifierName("ToBuilder");
            private static readonly IdentifierNameSyntax ToImmutableMethodName = SyntaxFactory.IdentifierName("ToImmutable");
            private static readonly IdentifierNameSyntax CreateBuilderMethodName = SyntaxFactory.IdentifierName("CreateBuilder");
            private static readonly IdentifierNameSyntax ImmutableFieldName = SyntaxFactory.IdentifierName("immutable");
            private static readonly TypeSyntax INotifyPropertyChanged = SyntaxFactory.ParseTypeName("System.ComponentModel.INotifyPropertyChanged");
            private static readonly IdentifierNameSyntax OnPropertyChangedMethodName = SyntaxFactory.IdentifierName("OnPropertyChanged");

            public BuilderGen(CodeGen generator)
                : base(generator)
            {
            }

            public override bool IsApplicable
            {
                get { return this.generator.options.GenerateBuilder; }
            }

            protected override void GenerateCore()
            {
                this.innerMembers.Add(this.CreateToBuilderMethod());

                if (!this.generator.isAbstract)
                {
                    this.innerMembers.Add(this.CreateCreateBuilderMethod());
                }

                var builderMembers = new List<MemberDeclarationSyntax>();
                builderMembers.Add(this.CreateImmutableField());
                builderMembers.AddRange(this.CreateMutableFields());
                builderMembers.Add(this.CreateConstructor());
                builderMembers.AddRange(this.CreateMutableProperties());
                builderMembers.Add(this.CreateToImmutableMethod());
                var builderType = SyntaxFactory.ClassDeclaration(BuilderTypeName.Identifier)
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                if (this.generator.applyToMetaType.HasAncestor)
                {
                    builderType = builderType
                        .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                            SyntaxFactory.SimpleBaseType(SyntaxFactory.QualifiedName(
                                GetFullyQualifiedSymbolName(this.generator.applyToMetaType.Ancestor.TypeSymbol),
                                BuilderTypeName)))))
                        .WithModifiers(builderType.Modifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.NewKeyword)));
                }
                else
                {
                    builderType = builderType
                        .AddBaseListTypes(SyntaxFactory.SimpleBaseType(INotifyPropertyChanged));
                    builderMembers.Add(this.CreatePropertyChangedEvent());
                    builderMembers.Add(this.CreateOnPropertyChangedMethod());
                }

                builderType = builderType
                    .WithMembers(SyntaxFactory.List(builderMembers));

                this.innerMembers.Add(builderType);
            }

            protected MethodDeclarationSyntax CreateToBuilderMethod()
            {
                var method = SyntaxFactory.MethodDeclaration(
                    BuilderTypeName,
                    ToBuilderMethodName.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                        SyntaxFactory.ObjectCreationExpression(
                            BuilderTypeName,
                            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.ThisExpression()))),
                            null)))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

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
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                        SyntaxFactory.ObjectCreationExpression(
                            BuilderTypeName,
                            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(DefaultInstanceFieldName))),
                            null)))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

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

                foreach (var field in this.generator.applyToMetaType.LocalFields)
                {
                    fields.Add(SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(
                        this.GetFieldTypeForBuilder(field),
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(field.Name))))
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
                foreach (var field in this.generator.applyToMetaType.LocalFields)
                {
                    if (!field.IsGeneratedImmutableType)
                    {
                        body = body.AddStatements(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            Syntax.ThisDot(field.NameAsField),
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, immutableParameterName, field.NameAsField))));
                    }
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

                foreach (var field in this.generator.applyToMetaType.LocalFields)
                {
                    var thisField = Syntax.ThisDot(field.NameAsField);
                    var optionalFieldNotYetDefined = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, Syntax.OptionalIsDefined(thisField));
                    var getterBlock = field.IsGeneratedImmutableType
                        ? SyntaxFactory.Block(
                            // if (!this.fieldName.IsDefined) {
                            SyntaxFactory.IfStatement(
                                optionalFieldNotYetDefined,
                                SyntaxFactory.Block(
                                    // this.fieldName = this.immutable.fieldName?.ToBuilder();
                                    SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        thisField,
                                        SyntaxFactory.ConditionalAccessExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                Syntax.ThisDot(ImmutableFieldName),
                                                field.NameAsField),
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberBindingExpression(ToBuilderMethodName),
                                                SyntaxFactory.ArgumentList())))))),
                            SyntaxFactory.ReturnStatement(Syntax.OptionalValue(thisField)))
                        : SyntaxFactory.Block(SyntaxFactory.ReturnStatement(thisField));
                    var setterValueArg = SyntaxFactory.IdentifierName("value");
                    var setterCondition = field.IsGeneratedImmutableType ?
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.LogicalOrExpression,
                            optionalFieldNotYetDefined,
                            SyntaxFactory.BinaryExpression(
                                SyntaxKind.NotEqualsExpression,
                                Syntax.OptionalValue(thisField),
                                setterValueArg)) :
                        HasEqualityOperators(field.Symbol.Type) ?
                            SyntaxFactory.BinaryExpression(
                                SyntaxKind.NotEqualsExpression,
                                thisField,
                                setterValueArg) :
                        null;
                    var setterSignificantBlock = SyntaxFactory.Block(
                        SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            thisField,
                            setterValueArg)),
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.ThisExpression(),
                                    OnPropertyChangedMethodName))));
                    var setterBlock = setterCondition != null ? 
                        SyntaxFactory.Block(
                            SyntaxFactory.IfStatement(
                                setterCondition,
                                setterSignificantBlock)) :
                        setterSignificantBlock;

                    var property = SyntaxFactory.PropertyDeclaration(
                        this.GetPropertyTypeForBuilder(field),
                        field.Name.ToPascalCase())
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new AccessorDeclarationSyntax[]
                        {
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, getterBlock),
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, setterBlock),
                        })));
                    properties.Add(property);
                }

                return properties;
            }

            protected NameSyntax GetPropertyTypeForBuilder(MetaField field)
            {
                var typeBasis = GetFullyQualifiedSymbolName(field.Type);
                return field.IsGeneratedImmutableType
                    ? SyntaxFactory.QualifiedName(typeBasis, BuilderTypeName)
                    : typeBasis;
            }

            protected NameSyntax GetFieldTypeForBuilder(MetaField field)
            {
                var typeBasis = GetFullyQualifiedSymbolName(field.Type);
                return field.IsGeneratedImmutableType
                    ? Syntax.OptionalOf(SyntaxFactory.QualifiedName(typeBasis, BuilderTypeName))
                    : typeBasis;
            }

            protected EventFieldDeclarationSyntax CreatePropertyChangedEvent()
            {
                var handler = SyntaxFactory.ParseTypeName("System.ComponentModel.PropertyChangedEventHandler");
                return SyntaxFactory.EventFieldDeclaration(
                    SyntaxFactory.VariableDeclaration(handler)
                        .AddVariables(SyntaxFactory.VariableDeclarator(nameof(System.ComponentModel.INotifyPropertyChanged.PropertyChanged))))
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
            }

            protected MethodDeclarationSyntax CreateOnPropertyChangedMethod()
            {
                var callerMemberName = SyntaxFactory.ParseName("System.Runtime.CompilerServices.CallerMemberNameAttribute");
                var propertyNameParameterName = SyntaxFactory.IdentifierName("propertyName");
                var evt = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    SyntaxFactory.IdentifierName(nameof(System.ComponentModel.INotifyPropertyChanged.PropertyChanged)));
                var invokeMethod = SyntaxFactory.ConditionalAccessExpression(
                    evt,
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName(nameof(System.ComponentModel.PropertyChangedEventHandler.Invoke))),
                        SyntaxFactory.ArgumentList().AddArguments(
                            SyntaxFactory.Argument(SyntaxFactory.ThisExpression()),
                            SyntaxFactory.Argument(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.ParseTypeName("System.ComponentModel.PropertyChangedEventArgs"),
                                    SyntaxFactory.ArgumentList().AddArguments(
                                        SyntaxFactory.Argument(propertyNameParameterName)),
                                    null)))));
                var body = SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(invokeMethod));
                return SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    OnPropertyChangedMethodName.Identifier)
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(propertyNameParameterName.Identifier)
                            .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)))
                            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxFactory.Attribute(callerMemberName)))
                            .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(string.Empty)))))
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                        SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
                    .WithBody(body);
            }

            protected MethodDeclarationSyntax CreateToImmutableMethod()
            {
                // var fieldName = this.fieldName.IsDefined ? this.fieldName.Value?.ToImmutable() : this.immutable.FieldName;
                var body = SyntaxFactory.Block(
                    from field in this.generator.applyToMetaType.AllFields
                    where field.IsGeneratedImmutableType
                    let thisField = Syntax.ThisDot(field.NameAsField) // this.fieldName
                    let thisFieldValue = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, thisField, SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph.Optional<int>.Value))) // this.fieldName.Value
                    select SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(varType))
                        .AddDeclarationVariables(
                            SyntaxFactory.VariableDeclarator(field.Name).WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.ConditionalExpression(
                                        Syntax.OptionalIsDefined(thisField), // this.fieldName.IsDefined
                                        SyntaxFactory.InvocationExpression( // this.fieldName.Value?.ToImmutable()
                                            SyntaxFactory.ConditionalAccessExpression(thisFieldValue, SyntaxFactory.MemberBindingExpression(ToImmutableMethodName)),
                                            SyntaxFactory.ArgumentList()),
                                        SyntaxFactory.MemberAccessExpression( // this.immutable.FieldName
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            Syntax.ThisDot(ImmutableFieldName),
                                            field.NameAsProperty))))));

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
                            SyntaxFactory.ArgumentList(
                                Syntax.JoinSyntaxNodes(
                                    SyntaxKind.CommaToken,
                                    this.generator.applyToMetaType.AllFields.Select(
                                        f => SyntaxFactory.Argument(Syntax.OptionalFor(f.IsGeneratedImmutableType ? SyntaxFactory.IdentifierName(f.Name) : Syntax.ThisDot(SyntaxFactory.IdentifierName(f.Name.ToPascalCase())))))))));
                }
                else
                {
                    // this.immutable
                    returnExpression = Syntax.ThisDot(ImmutableFieldName);
                }

                body = body.AddStatements(
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
    }
}
