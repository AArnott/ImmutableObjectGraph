namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [GenerateImmutable]
    abstract partial class RootRecursive
    {
    }

    [GenerateImmutable]
    abstract partial class RecursiveContainer : RootRecursive
    {
        readonly ImmutableList<RootRecursive> children;
    }

    [GenerateImmutable]
    partial class ContainerOfNonRecursiveCollection : RootRecursive
    {
        [NotRecursive]
        readonly ImmutableList<NonRecursiveElement> metadata;
    }

    [GenerateImmutable]
    partial class NonRecursiveElement : RootRecursive
    {
        readonly string name;
        readonly string value;
    }
}
