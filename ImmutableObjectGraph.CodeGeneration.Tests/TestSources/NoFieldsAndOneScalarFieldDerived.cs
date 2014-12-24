[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true)]
partial class Empty
{
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true)]
partial class NotSoEmptyDerived : Empty
{
    readonly bool oneField;
}
