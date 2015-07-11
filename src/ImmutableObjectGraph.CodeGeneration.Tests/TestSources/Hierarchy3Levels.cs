[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
abstract partial class L1 { }

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
abstract partial class L2 : L1
{
    // This in-between type intentionally has no members.
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class L3 : L2
{
    string foo;
}