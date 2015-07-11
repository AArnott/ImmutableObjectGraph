[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
abstract partial class L1 { }

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
abstract partial class L2 : L1 { }

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class L3 : L2
{
    string foo;
}