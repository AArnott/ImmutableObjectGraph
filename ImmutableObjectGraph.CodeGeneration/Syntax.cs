namespace ImmutableObjectGraph.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Validation;

    internal static class Syntax
    {
        internal static ParameterSyntax Optional(ParameterSyntax parameter)
        {
            return parameter
                .WithType(OptionalOf(parameter.Type))
                .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.DefaultExpression(OptionalOf(parameter.Type))));
        }

        internal static NameSyntax OptionalOf(TypeSyntax type)
        {
            return SyntaxFactory.QualifiedName(
                SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)),
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(nameof(Optional)),
                    SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(type))));
        }

        internal static MemberAccessExpressionSyntax OptionalIsDefined(ExpressionSyntax optionalOfTExpression)
        {
            return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, optionalOfTExpression, SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph.Optional<int>.IsDefined)));
        }

        internal static InvocationExpressionSyntax OptionalGetValueOrDefault(ExpressionSyntax optionalOfTExpression, ExpressionSyntax defaultValue)
        {
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, optionalOfTExpression, SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph.Optional<int>.GetValueOrDefault))),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(defaultValue))));
        }

        internal static MemberAccessExpressionSyntax OptionalValue(ExpressionSyntax optionalOfTExpression)
        {
            return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, optionalOfTExpression, SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph.Optional<int>.Value)));
        }

        internal static ExpressionSyntax OptionalFor(ExpressionSyntax expression)
        {
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph)),
                        SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph.Optional))),
                    SyntaxFactory.IdentifierName(nameof(ImmutableObjectGraph.Optional.For))),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(expression))));
        }

        internal static ExpressionSyntax OptionalForIf(ExpressionSyntax expression, bool isOptional)
        {
            return isOptional ? OptionalFor(expression) : expression;
        }

        internal static NameSyntax GetTypeSyntax(Type type)
        {
            Requires.NotNull(type, nameof(type));

            SimpleNameSyntax leafType = SyntaxFactory.IdentifierName(type.IsGenericType ? type.Name.Substring(0, type.Name.IndexOf('`')) : type.Name);
            if (type.IsGenericType)
            {
                leafType = SyntaxFactory.GenericName(
                    ((IdentifierNameSyntax)leafType).Identifier,
                    SyntaxFactory.TypeArgumentList(Syntax.JoinSyntaxNodes<TypeSyntax>(SyntaxKind.CommaToken, type.GenericTypeArguments.Select(GetTypeSyntax))));
            }

            if (type.Namespace != null)
            {
                NameSyntax namespaceName = null;
                foreach (string segment in type.Namespace.Split('.'))
                {
                    var segmentName = SyntaxFactory.IdentifierName(segment);
                    namespaceName = namespaceName == null
                        ? (NameSyntax)segmentName
                        : SyntaxFactory.QualifiedName(namespaceName, SyntaxFactory.IdentifierName(segment));
                }

                return SyntaxFactory.QualifiedName(namespaceName, leafType);
            }

            return leafType;
        }

        internal static NameSyntax IEnumerableOf(TypeSyntax typeSyntax)
        {
            return SyntaxFactory.QualifiedName(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName(nameof(System)),
                        SyntaxFactory.IdentifierName(nameof(System.Collections))),
                    SyntaxFactory.IdentifierName(nameof(System.Collections.Generic))),
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(nameof(IEnumerable<int>)),
                    SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(typeSyntax))));
        }

        internal static NameSyntax IEnumeratorOf(TypeSyntax typeSyntax)
        {
            return SyntaxFactory.QualifiedName(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName(nameof(System)),
                        SyntaxFactory.IdentifierName(nameof(System.Collections))),
                    SyntaxFactory.IdentifierName(nameof(System.Collections.Generic))),
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(nameof(IEnumerator<int>)),
                    SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(typeSyntax))));
        }

        internal static MethodDeclarationSyntax AddNewKeyword(MethodDeclarationSyntax method)
        {
            return method.WithModifiers(method.Modifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.NewKeyword)));
        }

        internal static SeparatedSyntaxList<T> JoinSyntaxNodes<T>(SyntaxKind tokenDelimiter, params T[] nodes)
            where T : SyntaxNode
        {
            return SyntaxFactory.SeparatedList<T>(JoinSyntaxNodes<T>(SyntaxFactory.Token(tokenDelimiter), nodes));
        }

        internal static SeparatedSyntaxList<T> JoinSyntaxNodes<T>(SyntaxKind tokenDelimiter, ImmutableArray<T> nodes)
            where T : SyntaxNode
        {
            return SyntaxFactory.SeparatedList<T>(JoinSyntaxNodes<T>(SyntaxFactory.Token(tokenDelimiter), nodes));
        }

        internal static SeparatedSyntaxList<T> JoinSyntaxNodes<T>(SyntaxKind tokenDelimiter, IEnumerable<T> nodes)
            where T : SyntaxNode
        {
            return SyntaxFactory.SeparatedList<T>(JoinSyntaxNodes<T>(SyntaxFactory.Token(tokenDelimiter), nodes.ToArray()));
        }

        internal static SyntaxNodeOrTokenList JoinSyntaxNodes<T>(SyntaxToken separatingToken, IReadOnlyList<T> nodes)
            where T : SyntaxNode
        {
            Requires.NotNull(nodes, nameof(nodes));

            switch (nodes.Count)
            {
                case 0:
                    return SyntaxFactory.NodeOrTokenList();
                case 1:
                    return SyntaxFactory.NodeOrTokenList(nodes[0]);
                default:
                    var nodesOrTokens = new SyntaxNodeOrToken[(nodes.Count * 2) - 1];
                    nodesOrTokens[0] = nodes[0];
                    for (int i = 1; i < nodes.Count; i++)
                    {
                        int targetIndex = i * 2;
                        nodesOrTokens[targetIndex - 1] = separatingToken;
                        nodesOrTokens[targetIndex] = nodes[i];
                    }

                    return SyntaxFactory.NodeOrTokenList(nodesOrTokens);
            }
        }

        internal static ParameterListSyntax PrependParameter(this ParameterListSyntax list, ParameterSyntax parameter)
        {
            return SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(parameter))
                .AddParameters(list.Parameters.ToArray());
        }

        internal static ArgumentListSyntax PrependArgument(this ArgumentListSyntax list, ArgumentSyntax argument)
        {
            return SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(argument))
                .AddArguments(list.Arguments.ToArray());
        }

        internal static ExpressionSyntax ThisDot(SimpleNameSyntax memberAccess)
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ThisExpression(),
                memberAccess);
        }

        internal static ExpressionSyntax BaseDot(SimpleNameSyntax memberAccess)
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.BaseExpression(),
                memberAccess);
        }

        internal static ExpressionSyntax ChainBinaryExpressions(this IEnumerable<ExpressionSyntax> expressions, SyntaxKind binaryOperator)
        {
            return expressions.Aggregate((ExpressionSyntax)null, (agg, e) => agg != null ? SyntaxFactory.BinaryExpression(binaryOperator, agg, e) : e);
        }
    }
}
