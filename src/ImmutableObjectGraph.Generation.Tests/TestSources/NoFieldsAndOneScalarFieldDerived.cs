[ImmutableObjectGraph.Generation.GenerateImmutable(GenerateBuilder = true)]
partial class Empty
{
}

[ImmutableObjectGraph.Generation.GenerateImmutable(GenerateBuilder = true)]
partial class NotSoEmptyDerived : Empty
{
    readonly bool oneField;
}
