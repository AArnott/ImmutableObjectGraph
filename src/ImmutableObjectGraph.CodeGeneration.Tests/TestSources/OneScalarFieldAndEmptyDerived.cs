[ImmutableObjectGraph.Generation.GenerateImmutable(GenerateBuilder = true)]
partial class NonEmptyBase
{
    readonly bool oneField;
}

[ImmutableObjectGraph.Generation.GenerateImmutable(GenerateBuilder = true)]
partial class EmptyDerivedFromNonEmptyBase : NonEmptyBase
{
}
