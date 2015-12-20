using System.Collections.Immutable;
using ImmutableObjectGraph;
using ImmutableObjectGraph.CodeGeneration;

[GenerateImmutable]
partial class SyntaxNode
{
    [Required]
    int startPosition;
    [Required]
    int length;
}

[GenerateImmutable]
partial class ElseBlock : SyntaxNode
{
    [Required]
    readonly string elseKeyword;
}

[GenerateImmutable]
partial class ElseIfBlock : SyntaxNode
{
    [Required]
    readonly string elseIfKeyword;
    [Required]
    readonly string thenKeyword;
}

[GenerateImmutable]
partial class IfNode : SyntaxNode
{
    [Required]
    readonly string ifKeyword;
    [Required]
    readonly string thenKeyword;
    readonly ImmutableList<ElseIfBlock> elseIfList;
    readonly ElseBlock elseBlock;
    [Required]
    readonly string endKeyword;
}
