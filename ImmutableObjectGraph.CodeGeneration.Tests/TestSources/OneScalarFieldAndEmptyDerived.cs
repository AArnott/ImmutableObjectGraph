[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true)]
partial class NonEmptyBase
{
    readonly bool oneField;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true)]
partial class EmptyDerivedFromNonEmptyBase : NonEmptyBase
{
}
