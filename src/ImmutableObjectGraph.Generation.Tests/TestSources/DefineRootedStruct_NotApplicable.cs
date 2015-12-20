namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [GenerateImmutable(GenerateBuilder = true, DefineRootedStruct = true)]
    public partial class Node
    {
        readonly string name;
        readonly ImmutableHashSet<string> tags;
    }

    [GenerateImmutable(GenerateBuilder = true)]
    public partial class Tree
    {
        readonly ImmutableSortedSet<Tree> children;
        readonly Node node;
    }
}
