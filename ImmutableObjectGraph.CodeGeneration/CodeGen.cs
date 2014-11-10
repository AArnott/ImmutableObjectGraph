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

    internal static class CodeGen
    {
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
            Requires.NotNull(nodes, "nodes");

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

        internal static ExpressionSyntax ThisDot(SimpleNameSyntax memberAccess)
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ThisExpression(),
                memberAccess);
        }
    }
}
