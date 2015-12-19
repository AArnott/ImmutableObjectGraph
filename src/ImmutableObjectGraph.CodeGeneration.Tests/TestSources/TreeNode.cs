namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System.Collections.Immutable;

    [GenerateImmutable(DefineWithMethodsPerProperty = true)]
    partial class TreeNode
    {
        readonly string caption;
        readonly string filePath;
        readonly bool visible;
        readonly ImmutableHashSet<string> attributes;
        readonly ImmutableList<TreeNode> children;
    }
}
