using System.Collections.Immutable;
using ImmutableObjectGraph;
using ImmutableObjectGraph.Generation;

[GenerateImmutable]
partial class SyntaxNode
{
}

[GenerateImmutable]
partial class ElseIfBlock : SyntaxNode
{
}

[GenerateImmutable]
partial class IfNode : SyntaxNode
{
    readonly ImmutableList<ElseIfBlock> elseIfList;
}
