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
        protected class CollectionHelpersGen : FeatureGenerator
        {
            internal static readonly IdentifierNameSyntax KeyParameterName = SyntaxFactory.IdentifierName("key");
            internal static readonly IdentifierNameSyntax ValuesParameterName = SyntaxFactory.IdentifierName("values");
            internal static readonly IdentifierNameSyntax ValueParameterName = SyntaxFactory.IdentifierName("value");
            private static readonly IdentifierNameSyntax SyncImmediateChildToCurrentVersionMethodName = SyntaxFactory.IdentifierName("SyncImmediateChildToCurrentVersion");

            public CollectionHelpersGen(CodeGen generator)
                : base(generator)
            {
            }

            public override bool IsApplicable
            {
                get { return this.generator.options.DefineWithMethodsPerProperty; }
            }

            protected override void GenerateCore()
            {
                if (this.generator.applyToMetaType.IsRecursiveParent)
                {
                    this.innerMembers.Add(this.CreateSyncImmediateChildToCurrentVersionMethod());
                }

                foreach (var field in this.generator.applyToMetaType.AllFields)
                {
                    var distinguisher = field.Distinguisher;
                    string suffix = distinguisher != null ? distinguisher.CollectionModifierMethodSuffix : null;
                    string plural = suffix != null ? (this.generator.PluralService.Singularize(field.Name.ToPascalCase()) + this.generator.PluralService.Pluralize(suffix)) : field.Name.ToPascalCase();
                    string singular = this.generator.PluralService.Singularize(field.Name.ToPascalCase()) + suffix;

                    if (field.IsCollection)
                    {
                        // With[Plural] methods
                        MethodDeclarationSyntax paramsArrayMethod = this.CreateParamsElementArrayMethod(
                            field,
                            SyntaxFactory.IdentifierName("With" + plural),
                            SyntaxFactory.IdentifierName(nameof(CollectionExtensions.ResetContents)));
                        this.innerMembers.Add(paramsArrayMethod);
                        this.innerMembers.Add(CreateIEnumerableFromParamsArrayMethod(field, paramsArrayMethod));

                        // Add[Plural] methods
                        paramsArrayMethod = this.CreateParamsElementArrayMethod(
                            field,
                            SyntaxFactory.IdentifierName("Add" + plural),
                            SyntaxFactory.IdentifierName(nameof(CollectionExtensions.AddRange)));
                        this.innerMembers.Add(paramsArrayMethod);
                        this.innerMembers.Add(CreateIEnumerableFromParamsArrayMethod(field, paramsArrayMethod));

                        // Add[Singular] method
                        MethodDeclarationSyntax singleMethod = this.CreateSingleElementMethod(
                            field,
                            SyntaxFactory.IdentifierName("Add" + singular),
                            SyntaxFactory.IdentifierName(nameof(ICollection<int>.Add)));
                        this.innerMembers.Add(singleMethod);

                        // Remove[Plural] methods
                        paramsArrayMethod = this.CreateParamsElementArrayMethod(
                            field,
                            SyntaxFactory.IdentifierName("Remove" + plural),
                            SyntaxFactory.IdentifierName(nameof(CollectionExtensions.RemoveRange)),
                            passThroughChildSync: field.IsRecursiveCollection);
                        this.innerMembers.Add(paramsArrayMethod);
                        this.innerMembers.Add(CreateIEnumerableFromParamsArrayMethod(field, paramsArrayMethod));
                        this.innerMembers.Add(CreateClearMethod(field, SyntaxFactory.IdentifierName("Remove" + plural)));

                        // Remove[Singular] method
                        singleMethod = this.CreateSingleElementMethod(
                            field,
                            SyntaxFactory.IdentifierName("Remove" + singular),
                            SyntaxFactory.IdentifierName(nameof(ICollection<int>.Remove)),
                            passThroughChildSync: field.IsRecursiveCollection);
                        this.innerMembers.Add(singleMethod);
                    }
                    else if (field.IsDictionary)
                    {
                        // Add[Singular] method
                        MethodDeclarationSyntax singleMethod = this.CreateKeyValueMethod(
                            field,
                            SyntaxFactory.IdentifierName("Add" + singular),
                            SyntaxFactory.IdentifierName(nameof(IImmutableDictionary<int, int>.Add)));
                        this.innerMembers.Add(singleMethod);

                        // Set[Singular] method
                        singleMethod = this.CreateKeyValueMethod(
                            field,
                            SyntaxFactory.IdentifierName("Set" + singular),
                            SyntaxFactory.IdentifierName(nameof(IImmutableDictionary<int, int>.SetItem)));
                        this.innerMembers.Add(singleMethod);

                        // Remove[Singular] method
                        singleMethod = this.CreateSingleElementMethod(
                            field,
                            SyntaxFactory.IdentifierName("Remove" + singular),
                            SyntaxFactory.IdentifierName(nameof(IImmutableDictionary<int, int>.Remove)),
                            elementParameterName: KeyParameterName,
                            elementType: field.ElementKeyType);
                        this.innerMembers.Add(singleMethod);
                    }
                }
            }

            private MethodDeclarationSyntax CreateSyncImmediateChildToCurrentVersionMethod()
            {
                var childParameterName = SyntaxFactory.IdentifierName("child");
                var childType = GetFullyQualifiedSymbolName(this.generator.applyToMetaType.RecursiveField.ElementType);
                var currentValueVarName = SyntaxFactory.IdentifierName("currentValue");

                return SyntaxFactory.MethodDeclaration(
                    childType,
                    SyncImmediateChildToCurrentVersionMethodName.Identifier)
                    .AddParameterListParameters(SyntaxFactory.Parameter(childParameterName.Identifier).WithType(childType))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
                    .WithBody(SyntaxFactory.Block(
                        // ElementTypeName currentValue;
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(
                            childType,
                            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(currentValueVarName.Identifier)))),
                        // if (!this.TryFindImmediateChild(child.<#= templateType.RequiredIdentityField.NamePascalCase #>, out currentValue)) {
                        SyntaxFactory.IfStatement(
                            SyntaxFactory.PrefixUnaryExpression(
                                SyntaxKind.LogicalNotExpression,
                                SyntaxFactory.InvocationExpression(
                                    Syntax.ThisDot(SyntaxFactory.IdentifierName(nameof(RecursiveTypeExtensions.TryFindImmediateChild))),
                                    SyntaxFactory.ArgumentList(Syntax.JoinSyntaxNodes(
                                        SyntaxKind.CommaToken,
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                childParameterName,
                                                IdentityPropertyName)),
                                        SyntaxFactory.Argument(
                                            null,
                                            SyntaxFactory.Token(SyntaxKind.OutKeyword),
                                            currentValueVarName))))),
                            SyntaxFactory.Block(
                                SyntaxFactory.ThrowStatement(SyntaxFactory.ObjectCreationExpression(
                                    Syntax.GetTypeSyntax(typeof(ArgumentException)),
                                    SyntaxFactory.ArgumentList(),
                                    null)))),
                        SyntaxFactory.ReturnStatement(currentValueVarName)));
            }

            private MethodDeclarationSyntax CreateMethodStarter(SyntaxToken name, MetaField field)
            {
                var method = SyntaxFactory.MethodDeclaration(
                    GetFullyQualifiedSymbolName(this.generator.applyToSymbol),
                    name)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

                if (!field.IsLocallyDefined)
                {
                    method = Syntax.AddNewKeyword(method);
                }

                return method;
            }

            private MethodDeclarationSyntax AddMethodBody(MethodDeclarationSyntax containingMethod, MetaField field, Func<ExpressionSyntax, InvocationExpressionSyntax> mutatingInvocationFactory)
            {
                var returnExpression = field.IsLocallyDefined
                    ? (ExpressionSyntax)SyntaxFactory.InvocationExpression( // this.With(field: this.field.SomeOperation(someArgs))
                        Syntax.ThisDot(WithMethodName),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.NameColon(field.Name),
                                NoneToken,
                                mutatingInvocationFactory(Syntax.ThisDot(field.NameAsField))))))
                    : SyntaxFactory.CastExpression( // (TemplateType)base.SameMethod(sameArgs)
                        GetFullyQualifiedSymbolName(this.generator.applyToSymbol),
                        SyntaxFactory.InvocationExpression(
                            Syntax.BaseDot(SyntaxFactory.IdentifierName(containingMethod.Identifier)),
                            SyntaxFactory.ArgumentList(
                                Syntax.JoinSyntaxNodes(
                                    SyntaxKind.CommaToken,
                                    containingMethod.ParameterList.Parameters.Select(p => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Identifier)))))));

                return containingMethod.WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ReturnStatement(returnExpression)));
            }

            private MethodDeclarationSyntax CreateParamsElementArrayMethod(MetaField field, IdentifierNameSyntax methodName, SimpleNameSyntax collectionMutationMethodName, bool passThroughChildSync = false)
            {
                var paramsArrayMethod = CreateMethodStarter(methodName.Identifier, field)
                    .WithParameterList(CreateParamsElementArrayParameters(field));

                var lambdaParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("v"));
                var argument = passThroughChildSync
                    ? (ExpressionSyntax)Syntax.EnumerableExtension(
                        SyntaxFactory.IdentifierName(nameof(Enumerable.Select)),
                        ValuesParameterName,
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(Syntax.ThisDot(SyncImmediateChildToCurrentVersionMethodName)))))
                    : ValuesParameterName;

                paramsArrayMethod = this.AddMethodBody(
                    paramsArrayMethod,
                    field,
                    receiver => SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            receiver,
                            collectionMutationMethodName),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(argument)))));

                return paramsArrayMethod;
            }

            private MethodDeclarationSyntax CreateSingleElementMethod(MetaField field, IdentifierNameSyntax methodName, SimpleNameSyntax collectionMutationMethodName, bool passThroughChildSync = false, IdentifierNameSyntax elementParameterName = null, ITypeSymbol elementType = null)
            {
                elementParameterName = elementParameterName ?? ValueParameterName;

                var paramsArrayMethod = CreateMethodStarter(methodName.Identifier, field)
                    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(elementParameterName.Identifier)
                        .WithType(GetFullyQualifiedSymbolName(elementType ?? field.ElementType)))));

                var argument = passThroughChildSync
                    ? (ExpressionSyntax)SyntaxFactory.InvocationExpression(
                        Syntax.ThisDot(SyncImmediateChildToCurrentVersionMethodName),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(elementParameterName))))
                    : elementParameterName;

                paramsArrayMethod = this.AddMethodBody(
                    paramsArrayMethod,
                    field,
                    receiver => SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            receiver,
                            collectionMutationMethodName),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(argument)))));

                return paramsArrayMethod;
            }

            private MethodDeclarationSyntax CreateKeyValueMethod(MetaField field, IdentifierNameSyntax methodName, SimpleNameSyntax collectionMutationMethodName)
            {
                var paramsArrayMethod = CreateMethodStarter(methodName.Identifier, field)
                    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
                        new ParameterSyntax[] {
                            SyntaxFactory.Parameter(KeyParameterName.Identifier).WithType(GetFullyQualifiedSymbolName(field.ElementKeyType)),
                            SyntaxFactory.Parameter(ValueParameterName.Identifier).WithType(GetFullyQualifiedSymbolName(field.ElementValueType)),
                        })));

                paramsArrayMethod = this.AddMethodBody(
                    paramsArrayMethod,
                    field,
                    receiver => SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            receiver,
                            collectionMutationMethodName),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new ArgumentSyntax[] {
                            SyntaxFactory.Argument(KeyParameterName),
                            SyntaxFactory.Argument(ValueParameterName),
                        }))));

                return paramsArrayMethod;
            }

            private MemberDeclarationSyntax CreateClearMethod(MetaField field, IdentifierNameSyntax methodName)
            {
                var method = CreateMethodStarter(methodName.Identifier, field)
                    .WithParameterList(SyntaxFactory.ParameterList());

                method = this.AddMethodBody(
                    method,
                    field,
                    receiver => SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            receiver,
                            SyntaxFactory.IdentifierName(nameof(ICollection<int>.Clear))),
                        SyntaxFactory.ArgumentList()));

                return method;
            }

            internal static ParameterListSyntax CreateParamsElementArrayParameters(MetaField field)
            {
                return SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                     SyntaxFactory.Parameter(ValuesParameterName.Identifier)
                            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ParamsKeyword)))
                            .WithType(SyntaxFactory.ArrayType(GetFullyQualifiedSymbolName(field.ElementType))
                                .AddRankSpecifiers(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression()))))));
            }

            internal static MethodDeclarationSyntax CreateIEnumerableFromParamsArrayMethod(MetaField field, MethodDeclarationSyntax paramsArrayMethod)
            {
                return paramsArrayMethod
                    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(ValuesParameterName.Identifier)
                            .WithType(Syntax.IEnumerableOf(GetFullyQualifiedSymbolName(field.ElementType))))));
            }
        }
    }
}
