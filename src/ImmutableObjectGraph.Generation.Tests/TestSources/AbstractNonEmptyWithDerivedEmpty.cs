[ImmutableObjectGraph.Generation.GenerateImmutable(GenerateBuilder = true)]
abstract partial class AbstractNonEmpty
{
    bool oneField;
}

[ImmutableObjectGraph.Generation.GenerateImmutable(GenerateBuilder = true)]
partial class EmptyDerivedFromAbstract : AbstractNonEmpty
{
}
