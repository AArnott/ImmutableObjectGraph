[ImmutableObjectGraph.Generation.GenerateImmutable]
abstract partial class L1 { }

[ImmutableObjectGraph.Generation.GenerateImmutable]
abstract partial class L2 : L1
{
    // This in-between type intentionally has no members.
}

[ImmutableObjectGraph.Generation.GenerateImmutable]
partial class L31 : L2
{
    string foo;
}

[ImmutableObjectGraph.Generation.GenerateImmutable]
partial class L32 : L2
{
    bool baseField;
}

[ImmutableObjectGraph.Generation.GenerateImmutable]
partial class L4 : L32
{
    bool secondField;
}