[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class NonEmptyBase
{
    readonly bool oneField;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class EmptyDerivedFromNonEmptyBase : NonEmptyBase
{
}
