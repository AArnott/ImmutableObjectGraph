[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
abstract partial class AbstractNonEmpty
{
    bool oneField;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class EmptyDerivedFromAbstract : AbstractNonEmpty
{
}
