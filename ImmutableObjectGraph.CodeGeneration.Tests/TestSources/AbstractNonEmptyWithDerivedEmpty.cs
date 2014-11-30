[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true)]
abstract partial class AbstractNonEmpty
{
    bool oneField;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true)]
partial class EmptyDerivedFromAbstract : AbstractNonEmpty
{
}
