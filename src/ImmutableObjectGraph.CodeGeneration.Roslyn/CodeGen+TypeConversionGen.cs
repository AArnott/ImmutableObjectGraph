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
    using Validation;
    using LookupTableHelper = RecursiveTypeExtensions.LookupTable<IRecursiveType, IRecursiveParentWithLookupTable<IRecursiveType>>;

    public partial class CodeGen
    {
        protected class TypeConversionGen : FeatureGenerator
        {
            private static readonly IdentifierNameSyntax CreateWithIdentityMethodName = SyntaxFactory.IdentifierName("CreateWithIdentity");

            public TypeConversionGen(CodeGen generator)
                : base(generator)
            {
            }

            public override bool IsApplicable
            {
                get { return true; }
            }

            protected override void GenerateCore()
            {
                if (!this.generator.applyToSymbol.IsAbstract && (this.generator.applyToMetaType.HasAncestor || this.generator.applyToMetaType.Descendents.Any()))
                {
                    this.innerMembers.Add(this.CreateCreateWithIdentityMethod());
                }

                if (this.generator.applyToMetaType.HasAncestor && !this.generator.applyToMetaType.Ancestor.TypeSymbol.IsAbstract)
                {
                    this.innerMembers.Add(this.CreateToAncestorTypeMethod());
                }

                // Only generate derived type conversion methods if we have something to add:
                // either because we're the most base class, or because we have fields to add.
                if (this.generator.applyToMetaType.LocalFields.Any() || !this.generator.applyToMetaType.Ancestors.Any())
                {
                    foreach (MetaType derivedType in this.generator.applyToMetaType.Descendents.Where(d => !d.TypeSymbol.IsAbstract))
                    {
                        this.innerMembers.Add(this.CreateToDerivedTypeMethod(derivedType));

                        if (this.generator.applyToMetaType.LocalFields.Any())
                        {
                            foreach (MetaType ancestor in this.generator.applyToMetaType.Ancestors)
                            {
                                this.innerMembers.Add(this.CreateToDerivedTypeOverrideMethod(derivedType, ancestor));
                            }
                        }
                    }
                }
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

                // {0}.Equals(typeof(derivedType))
                var thisTypeIsEquivalentToDerivedType =
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            thisDotGetType,
                            SyntaxFactory.IdentifierName(nameof(Type.Equals))),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(
                            SyntaxFactory.TypeOfExpression(derivedTypeName)))));

                var ifEquivalentTypeBlock = new List<StatementSyntax>();
                var fieldsBeyond = derivedType.GetFieldsBeyond(this.generator.applyToMetaType);
                if (fieldsBeyond.Any())
                {
                    Func<MetaField, ExpressionSyntax> isUnchanged = v =>
                        SyntaxFactory.ParenthesizedExpression(
                            v.IsRequired
                                ? // ({0} == that.{1})
                                SyntaxFactory.BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    v.NameAsField,
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        thatLocal,
                                        v.NameAsProperty))
                                : // (!{0}.IsDefined || {0}.Value == that.{1})
                                SyntaxFactory.BinaryExpression(
                                    SyntaxKind.LogicalOrExpression,
                                    SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, Syntax.OptionalIsDefined(v.NameAsField)),
                                    SyntaxFactory.BinaryExpression(
                                        SyntaxKind.EqualsExpression,
                                        Syntax.OptionalValue(v.NameAsField),
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            thatLocal,
                                            v.NameAsProperty))));
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
                        // return base.ToDerivedType(args);
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
    }
}
