[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
abstract partial class L1 { }

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
abstract partial class L2 : L1
{
    // This in-between type intentionally has no members.
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class L31 : L2
{
    string foo;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class L32 : L2
{
    bool baseField;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class L4 : L32
{
    bool secondField;
}