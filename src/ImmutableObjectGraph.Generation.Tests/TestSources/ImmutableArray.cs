namespace TestSources // Don't use ImmutableObjectGraph namespace so that we're testing that we fully qualify type names.
{
    [ImmutableObjectGraph.Generation.GenerateImmutable]
    partial class Node
    {
        readonly System.Collections.Immutable.ImmutableArray<Node> children;
    }
}
