namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System.Collections.Immutable;

    [GenerateImmutable]
    partial class Node
    {
        readonly ImmutableArray<Node> children;
    }
}
